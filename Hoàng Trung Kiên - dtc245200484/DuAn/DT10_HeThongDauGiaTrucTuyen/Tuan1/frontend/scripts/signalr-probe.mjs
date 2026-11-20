#!/usr/bin/env node
// Connects to the real SignalR hub, joins one auction and prints the next
// BidAccepted / AuctionEnded payload. Used by the E2E verification run.
//
// Usage:
//   node scripts/signalr-probe.mjs --auction <guid> [--token <jwt>] [--timeout 60000]
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const args = process.argv.slice(2)
const get = (name, fallback) => {
  const index = args.indexOf(`--${name}`)
  return index >= 0 ? args[index + 1] : fallback
}

const auctionId = get('auction')
if (!auctionId) {
  console.error('Missing --auction <guid>')
  process.exit(2)
}

const hubUrl = get('hub', process.env.VITE_HUB_URL ?? 'http://localhost:5100/hubs/auction')
const token = get('token', process.env.AUCTION_TOKEN ?? '')
const timeoutMs = Number(get('timeout', '60000'))

const connection = new HubConnectionBuilder()
  .withUrl(hubUrl, token ? { accessTokenFactory: () => token } : {})
  .withAutomaticReconnect()
  .configureLogging(LogLevel.Warning)
  .build()

let settled = false
const finish = (code, payload) => {
  if (settled) return
  settled = true
  if (payload !== undefined) console.log(JSON.stringify(payload, null, 2))
  connection.stop().finally(() => process.exit(code))
}

connection.on('BidAccepted', (event) => finish(0, { event: 'BidAccepted', ...event }))
connection.on('AuctionEnded', (event) => finish(0, { event: 'AuctionEnded', ...event }))
connection.onreconnecting(() => console.error('SignalR reconnecting…'))
connection.onclose(() => console.error('SignalR closed'))

await connection.start()
await connection.invoke('JoinAuction', auctionId)
console.error(`Listening on ${hubUrl} for auction ${auctionId}…`)

setTimeout(() => finish(1, { error: 'timeout', auctionId }), timeoutMs)
