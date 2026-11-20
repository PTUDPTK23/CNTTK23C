#!/usr/bin/env node
// End-to-end realtime verification against the running stack.
//
// 1. Logs in through the REST API.
// 2. Opens one or more SignalR connections and joins an auction room.
// 3. Places a real bid through POST /api/auctions/{id}/bids.
// 4. Waits until every connection receives BidAccepted and prints evidence.
//
// Usage:
//   node scripts/signalr-e2e.mjs --auction <guid> [--clients 2] [--api http://localhost:5100]
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const args = process.argv.slice(2)
const get = (name, fallback) => {
  const index = args.indexOf(`--${name}`)
  return index >= 0 ? args[index + 1] : fallback
}

const api = get('api', process.env.API_URL ?? 'http://localhost:5100')
const hubUrl = get('hub', process.env.VITE_HUB_URL ?? `${api}/hubs/auction`)
const email = get('email', 'alice@auction.local')
const password = get('password', 'Bidder@123')
const clientCount = Number(get('clients', '2'))
const timeoutMs = Number(get('timeout', '30000'))

const fail = (message) => {
  console.error(`FAIL: ${message}`)
  process.exit(1)
}

const loginResponse = await fetch(`${api}/api/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
})
if (!loginResponse.ok) fail(`login returned ${loginResponse.status}`)
const session = await loginResponse.json()
const token = session.token
const auth = { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }

let auctionId = get('auction')
if (!auctionId) {
  const listing = await fetch(`${api}/api/auctions?status=active&pageSize=1`, { headers: auth })
  if (!listing.ok) fail(`listing returned ${listing.status}`)
  const page = await listing.json()
  auctionId = page.items?.[0]?.id
  if (!auctionId) fail('no active auction to bid on')
}

const detailResponse = await fetch(`${api}/api/auctions/${auctionId}`, { headers: auth })
if (!detailResponse.ok) fail(`auction detail returned ${detailResponse.status}`)
const before = await detailResponse.json()
const amount = Number(before.minimumNextBid)
if (!Number.isFinite(amount) || amount <= 0) fail('auction has no valid next bid amount')

const connections = []
for (let index = 0; index < clientCount; index += 1) {
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.None)
    .build()
  await connection.start()
  await connection.invoke('JoinAuction', auctionId)
  connections.push(connection)
}
console.log(JSON.stringify({ step: 'joined', hubUrl, auctionId, clients: connections.length }))

const received = new Array(connections.length).fill(null)
let resolveAll
const allReceived = new Promise((resolve) => {
  resolveAll = resolve
})
connections.forEach((connection, index) => {
  connection.on('BidAccepted', (payload) => {
    if (received[index]) return
    received[index] = payload
    if (received.every(Boolean)) resolveAll()
  })
})

const bidResponse = await fetch(`${api}/api/auctions/${auctionId}/bids`, {
  method: 'POST',
  headers: auth,
  body: JSON.stringify({ amount })
})
const bidBody = await bidResponse.text()
if (!bidResponse.ok) fail(`bid rejected with ${bidResponse.status}: ${bidBody}`)
const bid = JSON.parse(bidBody)
console.log(JSON.stringify({ step: 'bid-accepted', status: bidResponse.status, amount, bid }))

const timer = setTimeout(() => {
  console.error(`FAIL: ${received.filter(Boolean).length}/${connections.length} clients received BidAccepted`)
  connections.forEach((connection) => connection.stop())
  process.exit(1)
}, timeoutMs)

await allReceived
clearTimeout(timer)

const afterResponse = await fetch(`${api}/api/auctions/${auctionId}`, { headers: auth })
const after = await afterResponse.json()

const consistent = received.every(
  (event) => Number(event.amount) === amount && String(event.auctionId).toLowerCase() === auctionId.toLowerCase()
)

console.log(
  JSON.stringify(
    {
      step: 'signalr-broadcast',
      clients: connections.length,
      allReceived: received.every(Boolean),
      consistent,
      events: received,
      priceBefore: Number(before.currentPrice),
      priceAfter: Number(after.currentPrice),
      bidCountAfter: after.bidCount
    },
    null,
    2
  )
)

connections.forEach((connection) => connection.stop())
process.exit(consistent ? 0 : 1)
