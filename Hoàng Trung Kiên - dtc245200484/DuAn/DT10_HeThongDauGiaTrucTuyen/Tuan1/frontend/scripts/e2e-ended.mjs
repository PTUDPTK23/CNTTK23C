// Kiểm tra E-13/E-14: quản trị viên đóng phiên → AuctionEnded qua SignalR → audit_logs.
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const api = process.env.API_URL ?? 'http://localhost:5100'
const hubUrl = process.env.VITE_HUB_URL ?? `${api}/hubs/auction`

async function login(email, password) {
  const response = await fetch(`${api}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
  if (!response.ok) throw new Error(`Đăng nhập ${response.status}`)
  return (await response.json()).token
}

const token = await login('admin@auction.local', 'Admin@123')
const headers = { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }

const listing = await fetch(`${api}/api/auctions?status=scheduled&pageSize=1`, { headers })
const page = await listing.json()
const auctionId = page.items?.[0]?.id
if (!auctionId) throw new Error('Không có phiên lên lịch để đóng')

const connection = new HubConnectionBuilder()
  .withUrl(hubUrl, { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .configureLogging(LogLevel.None)
  .build()
await connection.start()
await connection.invoke('JoinAuction', auctionId)

let ended
const endedReceived = new Promise((resolve) => {
  connection.on('AuctionEnded', (payload) => {
    ended = payload
    resolve()
  })
})

const endResponse = await fetch(`${api}/api/auctions/${auctionId}/end`, {
  method: 'POST',
  headers
})
const endBody = await endResponse.json()

await Promise.race([
  endedReceived,
  new Promise((_, reject) => setTimeout(() => reject(new Error('Hết thời gian chờ AuctionEnded')), 15000))
])
await connection.stop()

const result = await (await fetch(`${api}/api/auctions/${auctionId}/result`, { headers })).json()
const audit = await (await fetch(`${api}/api/admin/audit-logs?eventType=AuctionEnded`, { headers })).json()

console.log(JSON.stringify({
  step: 'ket-thuc-phien',
  auctionId,
  endStatus: endResponse.status,
  endBody,
  signalr: ended,
  result,
  auditCount: audit.total,
  lastAudit: audit.items?.[0]
}, null, 2))
