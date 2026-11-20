// Kiểm tra E2E: đăng nhập → từ chối lượt giá thấp/không xác thực/quản trị → đồng thời → admin → hồ sơ.
const API = process.env.API_URL ?? 'http://localhost:5100'

async function json(path, { token, method = 'GET', body } = {}) {
  const response = await fetch(API + path, {
    method,
    headers: {
      ...(body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    ...(body ? { body: JSON.stringify(body) } : {})
  })
  const text = await response.text()
  let data
  try {
    data = JSON.parse(text)
  } catch {
    data = text
  }
  return { status: response.status, data }
}

async function login(email, password) {
  const result = await json('/api/auth/login', { method: 'POST', body: { email, password } })
  if (result.status !== 200) throw new Error(`Đăng nhập thất bại: ${JSON.stringify(result)}`)
  return result.data.token
}

const bidder = await login('alice@auction.local', 'Bidder@123')
const admin = await login('admin@auction.local', 'Admin@123')
const auctions = await json('/api/auctions?status=active')
const auction = auctions.data.items[0]
const amount = auction.minimumNextBid
const bidPath = `/api/auctions/${auction.id}/bids`

console.log(JSON.stringify({ step: 'dang-nhap', bidder: Boolean(bidder), admin: Boolean(admin), auction: auction.title }))

const cases = {
  'E-10 lượt giá thấp': await json(bidPath, { token: bidder, method: 'POST', body: { amount: 1 } }),
  'E-11 chưa xác thực': await json(bidPath, { method: 'POST', body: { amount } }),
  'E-12 quản trị viên': await json(bidPath, { token: admin, method: 'POST', body: { amount } }),
  'E-04/E-15 đồng thời 2 lượt': null
}

const concurrent = await Promise.all([
  json(bidPath, { token: bidder, method: 'POST', body: { amount } }),
  json(bidPath, { token: bidder, method: 'POST', body: { amount } })
])
cases['E-04/E-15 đồng thời 2 lượt'] = concurrent

for (const [name, result] of Object.entries(cases)) {
  const statuses = Array.isArray(result) ? result.map((r) => r.status) : result.status
  const codes = Array.isArray(result)
    ? result.map((r) => r.data?.code ?? 'OK')
    : result.data?.code ?? 'OK'
  const messages = Array.isArray(result)
    ? result.map((r) => r.data?.message ?? r.data?.bidId)
    : result.data?.message ?? result.data?.bidId
  console.log(JSON.stringify({ case: name, statuses, codes, messages }))
}

const dashboard = await json('/api/admin/dashboard', { token: admin })
const withBidder = await json('/api/admin/dashboard', { token: bidder })
const profile = await json('/api/users/me', { token: bidder })
const myBids = await json('/api/users/me/bids', { token: bidder })
const adminUsers = await json('/api/admin/users', { token: admin })
const audit = await json('/api/admin/audit-logs', { token: admin })

console.log(JSON.stringify({
  step: 'quan-tri',
  dashboard: dashboard.status,
  dashboardKeys: Object.keys(dashboard.data ?? {}).length,
  tuChoiKhongPhaiAdmin: withBidder.status,
  profile: profile.status,
  profileName: profile.data?.displayName,
  soLuotDatGia: myBids.data?.total,
  danhSachNguoiDung: adminUsers.data?.total,
  nhatKyKiemToan: audit.data?.total
}))
