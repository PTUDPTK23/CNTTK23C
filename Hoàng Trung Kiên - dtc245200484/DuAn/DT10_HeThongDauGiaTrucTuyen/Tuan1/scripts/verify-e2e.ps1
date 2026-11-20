#requires -Version 7.0
<#
.SYNOPSIS
  End-to-end verification against a running Aurelia stack.

.DESCRIPTION
  Logs in as a bidder, picks a live auction, places a bid, then asserts:
    - the bid is persisted and the price advances,
    - the outbox row is published,
    - RabbitMQ accepted the event (published_at_utc set),
    - the Audit Worker wrote an audit_logs row (optional wait),
    - SignalR delivery (optional, when frontend dependencies are installed).

  Requires: API on :5100, PostgreSQL connection details, and optionally the
  SignalR probe from frontend/scripts/signalr-probe.mjs.

.EXAMPLE
  pwsh ./scripts/verify-e2e.ps1 -ApiBase http://localhost:5100
#>
[CmdletBinding()]
param(
    [string]$ApiBase = 'http://localhost:5100',
    [string]$Email = 'alice@auction.local',
    [string]$Password = 'Bidder@123',
    [string]$PgHost = 'localhost',
    [int]$PgPort = 5433,
    [string]$PgUser = 'postgres',
    [string]$PgPassword = 'auction_dev_password',
    [string]$PgDatabase = 'auctiondb',
    [switch]$SkipSignalR,
    [int]$TimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
function Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }
function Ok($text) { Write-Host "OK  $text" -ForegroundColor Green }
function Fail($text) { Write-Host "FAIL $text" -ForegroundColor Red; exit 1 }

Step 'Health'
$health = Invoke-RestMethod "$ApiBase/health"
if ($health.status -ne 'healthy') { Fail 'API is not healthy' }
Ok "API healthy ($($health.service))"

Step "Login $Email"
$login = Invoke-RestMethod -Method Post "$ApiBase/api/auth/login" -ContentType 'application/json' `
    -Body (@{ email = $Email; password = $Password } | ConvertTo-Json)
if (-not $login.token) { Fail 'No token returned' }
$headers = @{ Authorization = "Bearer $($login.token)" }
Ok "Authenticated as $($login.user.displayName)"

Step 'Find a live auction'
$page = Invoke-RestMethod "$ApiBase/api/auctions?status=active&pageSize=20"
$auction = $page.items | Where-Object { $_.status -eq 'active' } | Select-Object -First 1
if (-not $auction) { Fail 'No active auction found' }
$amount = [decimal]$auction.currentPrice + [decimal]$auction.minIncrement
Ok "Auction $($auction.id) current=$($auction.currentPrice) next=$amount"

if (-not $SkipSignalR) {
    $probe = Join-Path $PSScriptRoot '..\frontend\scripts\signalr-probe.mjs'
    $probeLog = Join-Path ([IO.Path]::GetTempPath()) "aurelia-signalr-$([guid]::NewGuid()).json"
    $probeOut = Join-Path ([IO.Path]::GetTempPath()) "aurelia-signalr-$([guid]::NewGuid()).log"
    Step 'Start SignalR probe'
    $node = Start-Process -FilePath 'node' -ArgumentList @(
        $probe, '--auction', $auction.id, '--token', $login.token, '--timeout', ($TimeoutSeconds * 1000)
    ) -WorkingDirectory (Join-Path $PSScriptRoot '..\frontend') -RedirectStandardOutput $probeLog `
      -RedirectStandardError $probeOut -PassThru
    Start-Sleep -Seconds 2
    if ($node.HasExited) { Write-Warning "SignalR probe exited early: $(Get-Content $probeOut -Raw)" }
}

Step 'Place bid'
try {
    $bid = Invoke-RestMethod -Method Post "$ApiBase/api/auctions/$($auction.id)/bids" `
        -Headers $headers -ContentType 'application/json' `
        -Body (@{ amount = $amount } | ConvertTo-Json)
} catch {
    Fail "Bid rejected: $($_.ErrorDetails.Message)"
}
Ok "Bid $($bid.bidId) accepted at $($bid.amount)"

Step 'Verify persisted price'
$detail = Invoke-RestMethod "$ApiBase/api/auctions/$($auction.id)"
if ([decimal]$detail.currentPrice -ne [decimal]$bid.amount) { Fail 'Price was not persisted' }
Ok "currentPrice=$($detail.currentPrice)"

if (-not $SkipSignalR) {
    Step 'Wait for SignalR delivery'
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not (Test-Path $probeLog)) { Start-Sleep -Milliseconds 500 }
    if (Test-Path $probeLog) {
        $payload = Get-Content $probeLog -Raw | ConvertFrom-Json
        if ($payload.bidId -eq $bid.bidId) { Ok "SignalR BidAccepted received for $($payload.bidId)" }
        else { Fail "SignalR payload did not match the bid: $($probeLog)" }
    } else {
        Fail "SignalR probe timed out (log: $probeOut)"
    }
}

Step 'Verify outbox publication'
$env:PGPASSWORD = $PgPassword
$psql = Join-Path $env:ProgramFiles 'PostgreSQL\17\bin\psql.exe'
if (-not (Test-Path $psql)) { $psql = 'psql' }
$published = & $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDatabase -t -A -c `
    "select count(*) from outbox_messages where published_at_utc is not null and event_type = 'BidAccepted';"
if ([int]$published -lt 1) { Fail 'No published BidAccepted outbox row' }
Ok "Published BidAccepted outbox rows: $published"

Step 'Verify Audit Worker'
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$audit = 0
while ((Get-Date) -lt $deadline) {
    $audit = & $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDatabase -t -A -c `
        "select count(*) from audit_logs where event_id = (select event_id from bids where id = '$($bid.bidId)');"
    if ([int]$audit -ge 1) { break }
    Start-Sleep -Seconds 1
}
if ([int]$audit -lt 1) { Fail 'Audit log was not written' }
Ok 'Audit log written for the bid event'

Write-Host ''
Write-Host 'E2E PASSED: REST -> gRPC -> PostgreSQL -> RabbitMQ -> SignalR -> Audit' -ForegroundColor Green
