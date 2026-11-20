# Tài liệu API

Base URL: `http://localhost:5100`  
Tham chiếu tương tác: `http://localhost:5100/swagger`  
Content type: `application/json` (trừ dịch vụ gRPC và SignalR hub)

## 1. Xác thực

Gửi JWT trả về từ lúc đăng nhập trên mỗi lời gọi được bảo vệ:

```http
Authorization: Bearer <token>
```

Token là HS256, ký bằng `Jwt:Key`, có hiệu lực 8 giờ và chứa claim vai trò `Admin` hoặc `Bidder`.

### Vai trò

| Vai trò | Quyền truy cập |
| --- | --- |
| Ẩn danh | Danh mục, phiên đấu giá, lượt giá, kết quả, thống kê công khai, đăng nhập/đăng ký |
| `Bidder` | Đặt giá, hồ sơ, lịch sử cá nhân |
| `Admin` | Bảng điều khiển, CRUD sản phẩm, tạo/kết thúc phiên, quản lý người dùng, nhật ký kiểm toán |

## 2. Định dạng lỗi

Lỗi ứng dụng được phát ra bởi `ExceptionHandlingMiddleware`:

```json
{
  "code": "BID_TOO_LOW",
  "message": "Giá đặt phải cao hơn giá hiện tại 15500.00.",
  "details": {
    "auctionId": "adbf403e-f39d-419a-95b9-7017e84877b0",
    "code": "BID_TOO_LOW",
    "currentPrice": 15500.00,
    "minimumNextBid": 16000.00,
    "serverTimeUtc": "2026-11-19T08:19:42Z"
  },
  "traceId": "0HN..."
}
```

Lỗi xác thực model dùng:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Dữ liệu gửi lên không hợp lệ.",
  "errors": { "amount": ["The field Amount must be between 0.01 and 999999999."] }
}
```

### Mã lỗi

| Mã | HTTP | Ý nghĩa |
| --- | --- | --- |
| `VALIDATION_ERROR` | 400 | Body/query sai định dạng hoặc không hợp lệ |
| `INVALID_CREDENTIALS` | 401 | Sai email/mật khẩu |
| `ACCOUNT_DISABLED` | 401 | Người dùng không hoạt động |
| `FORBIDDEN` | 403 | Sai vai trò hoặc thiếu quyền sở hữu |
| `SELF_BID_NOT_ALLOWED` | 403 | Quản trị viên đặt giá, hoặc đặt giá phiên của chính mình |
| `BIDDER_INACTIVE` | 403 | Tài khoản người đặt giá bị khóa |
| `AUCTION_NOT_FOUND` | 404 | Phiên không tồn tại |
| `BIDDER_NOT_FOUND` | 404 | Người đặt giá không tồn tại |
| `PRODUCT_NOT_FOUND` | 404 | Sản phẩm không tồn tại |
| `PRODUCT_IN_USE` | 409 | Sản phẩm đã được phiên tham chiếu |
| `EMAIL_IN_USE` | 409 | Email đăng ký đã tồn tại |
| `AUCTION_NOT_STARTED` | 409 | Phiên lên lịch / trước giờ mở |
| `AUCTION_ENDED` | 409 | Phiên đã kết thúc hoặc quá giờ đóng |
| `AUCTION_NOT_ACTIVE` | 409 | Phiên đã hủy/không hoạt động |
| `AUCTION_NOT_ENDABLE` | 409 | Đã kết thúc/đã hủy |
| `BID_TOO_LOW` | 409 | Số tiền ≤ giá hiện tại |
| `BID_STEP_TOO_SMALL` | 409 | Số tiền thấp hơn giá hiện tại + bước giá tối thiểu |
| `CONCURRENCY_CONFLICT` | 409 | Xung đột dòng/số thứ tự |
| `INVALID_AMOUNT` | 400 | Số tiền không dương hoặc quá 2 chữ số thập phân |
| `INVALID_ROLE` | 400 | Vai trò phải là `admin` hoặc `bidder` |
| `VALIDATION_UNAVAILABLE` | 503 | Không kết nối được validator gRPC |
| `INTERNAL_ERROR` | 500 | Lỗi máy chủ không mong muốn |

## 3. Sức khỏe

### `GET /health`

```json
{ "status": "healthy", "service": "Auction.API" }
```

Ba worker cũng mở cùng endpoint này trên cổng 5102 và 5103.

---

## 4. Endpoint xác thực

### `POST /api/auth/register`

Ẩn danh. Tạo người đấu giá và trả JWT.

```json
{
  "email": "new.user@example.com",
  "password": "Str0ngPass!",
  "displayName": "Người Dùng Mới"
}
```

`201 Created`

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAtUtc": "2026-11-19T16:19:30Z",
  "user": {
    "id": "d60130dc-8141-4475-a8cb-23ee16a14f3d",
    "email": "new.user@example.com",
    "displayName": "Người Dùng Mới",
    "role": "bidder",
    "createdAtUtc": "2026-11-19T08:18:27Z"
  }
}
```

Lỗi: `400 VALIDATION_ERROR`, `409 EMAIL_IN_USE`.

### `POST /api/auth/login`

Ẩn danh.

```json
{ "email": "alice@auction.local", "password": "Bidder@123" }
```

`200 OK` — cấu trúc giống đăng ký. Lỗi: `401 INVALID_CREDENTIALS`, `401 ACCOUNT_DISABLED`.

### `POST /api/auth/logout`

Cần người dùng đã xác thực bất kỳ. `204 No Content`. JWT không có trạng thái; client phải hủy token.

---

## 5. Endpoint sản phẩm

### `GET /api/products`

Ẩn danh. Query: `q`, `category`, `page` (mặc định 1), `pageSize` (mặc định 12).

```json
{
  "items": [
    {
      "id": "…",
      "name": "Đồng hồ Chronograph Sapphire Đêm",
      "description": "…",
      "category": "Đồng hồ",
      "imageUrl": "/media/watch.svg",
      "startingPrice": 12000.00,
      "createdAtUtc": "…",
      "updatedAtUtc": "…",
      "auctionCount": 1
    }
  ],
  "page": 1,
  "pageSize": 12,
  "total": 10,
  "totalPages": 1
}
```

### `GET /api/products/categories`

Ẩn danh. Trả `["Nghệ thuật","Xe cộ","Sưu tầm", …]`.

### `GET /api/products/{id}`

Ẩn danh. Trả một `ProductDto`; `404 PRODUCT_NOT_FOUND`.

### `POST /api/products` — Admin

```json
{
  "name": "Bình hoa hiếm",
  "description": "Gốm nặn tay, có chữ ký.",
  "category": "Nghệ thuật",
  "imageUrl": "/media/sculpture.svg",
  "startingPrice": 2500.00
}
```

`201 Created` + `Location`. Lỗi: `400`, `403`.

### `PUT /api/products/{id}` — Admin

Body giống lúc tạo. `200 OK`.

### `DELETE /api/products/{id}` — Admin

`204 No Content`; `409 PRODUCT_IN_USE` khi đã gắn với phiên.

---

## 6. Endpoint phiên đấu giá

### `GET /api/auctions`

Ẩn danh. Query: `status` (`scheduled|active|ended|cancelled`), `category`, `q`,
`sort` (`ending|newest|price_asc|price_desc|bids`), `page`, `pageSize`.

Trả `PagedResult<AuctionSummaryDto>` với `serverTimeUtc` trên mỗi item.

### `GET /api/auctions/stats`

Ẩn danh.

```json
{ "totalAuctions": 6, "activeAuctions": 3, "totalBids": 12, "totalBidders": 3 }
```

### `GET /api/auctions/{id}`

Ẩn danh. Trả `AuctionDetailDto`:

```json
{
  "id": "adbf403e-f39d-419a-95b9-7017e84877b0",
  "title": "Chronograph Sapphire Đêm — Số 07",
  "productId": "…",
  "productName": "Đồng hồ Chronograph Sapphire Đêm",
  "category": "Đồng hồ",
  "imageUrl": "/media/watch.svg",
  "productDescription": "…",
  "startingPrice": 12000.00,
  "currentPrice": 16000.00,
  "minIncrement": 500.00,
  "minimumNextBid": 16500.00,
  "startTimeUtc": "…",
  "endTimeUtc": "…",
  "status": "active",
  "bidCount": 5,
  "participantCount": 3,
  "highestBidderName": "Nguyễn Minh Anh",
  "sellerName": "Quản trị Aurelia",
  "createdAtUtc": "…",
  "endedAtUtc": null,
  "winner": null,
  "recentBids": [
    {
      "id": "…",
      "auctionId": "…",
      "auctionTitle": null,
      "amount": 16000.00,
      "bidderId": "…",
      "bidderName": "Nguyễn Minh Anh",
      "placedAtUtc": "…",
      "sequence": 5,
      "isWinning": false
    }
  ],
  "serverTimeUtc": "2026-11-19T08:21:40Z"
}
```

### `POST /api/auctions` — Admin

```json
{
  "productId": "…",
  "title": "Phiên đấu giá đồng hồ buổi tối",
  "startPrice": 5000.00,
  "minIncrement": 250.00,
  "startTimeUtc": "2026-11-20T10:00:00Z",
  "endTimeUtc": "2026-11-22T10:00:00Z"
}
```

`201 Created` kèm detail DTO. Lỗi: `400`, `403`.

### `POST /api/auctions/{id}/bids` — Bidder

```json
{ "amount": 16500.00 }
```

`201 Created`

```json
{
  "bidId": "1ff752fe-9691-4e41-9a43-1444556a375a",
  "auctionId": "adbf403e-f39d-419a-95b9-7017e84877b0",
  "amount": 16500.00,
  "currentPrice": 16500.00,
  "minimumNextBid": 17000.00,
  "bidderName": "Nguyễn Minh Anh",
  "placedAtUtc": "2026-11-19T08:21:39Z",
  "message": "Lượt đặt giá của bạn đã được chấp nhận."
}
```

Endpoint này chạy xác thực gRPC và giao dịch PostgreSQL tuần tự như mô tả trong
[architecture.md](architecture.md). Xem bảng lỗi cho tất cả mã từ chối.

### `GET /api/auctions/{id}/bids`

Ẩn danh. Query: `page`, `pageSize` (tối đa 100). Trả `PagedResult<BidDto>`, mới nhất trước.

### `GET /api/auctions/{id}/result`

Ẩn danh.

```json
{
  "auctionId": "…",
  "title": "Nhẫn Ngọc Lục Bảo Hoàng Gia — 4.8ct",
  "status": "ended",
  "hasWinner": true,
  "winnerId": "…",
  "winnerName": "Nguyễn Minh Anh",
  "winningAmount": 10200.00,
  "winningBidId": "…",
  "totalBids": 6,
  "endedAtUtc": "2026-11-19T06:00:00Z"
}
```

### `GET /api/auctions/{id}/participants`

Ẩn danh. Trả `[{ "userId", "displayName", "joinedAtUtc", "lastBidAtUtc", "bidCount" }]`.

### `POST /api/auctions/{id}/end` — Admin

Đóng ngay, xác định người thắng và trả `AuctionResultDto`.
Lỗi: `404`, `409 AUCTION_NOT_ENDABLE`, `403`.

---

## 7. Endpoint người dùng (đã xác thực)

| Method | Path | Mô tả |
| --- | --- | --- |
| `GET` | `/api/users/me` | Hồ sơ + `totalBids`, `auctionsJoined`, `auctionsWon` |
| `GET` | `/api/users/me/bids` | Lượt giá của người hiện tại, phân trang (`page`, `pageSize`) |
| `GET` | `/api/users/me/auctions` | Phiên người dùng đã tham gia |
| `GET` | `/api/users/me/wins` | Phiên người dùng đã thắng |

Ví dụ hồ sơ:

```json
{
  "id": "d60130dc-8141-4475-a8cb-23ee16a14f3d",
  "email": "alice@auction.local",
  "displayName": "Nguyễn Minh Anh",
  "role": "bidder",
  "createdAtUtc": "2026-11-19T08:18:27Z",
  "totalBids": 5,
  "auctionsJoined": 3,
  "auctionsWon": 1
}
```

---

## 8. Endpoint quản trị — chỉ Admin

### `GET /api/admin/dashboard`

```json
{
  "totalProducts": 10,
  "totalAuctions": 6,
  "activeAuctions": 3,
  "scheduledAuctions": 1,
  "endedAuctions": 2,
  "totalUsers": 4,
  "totalBidders": 3,
  "totalBids": 12,
  "totalBidVolume": 128000.00,
  "bidsPerDay": [{ "date": "2026-11-19", "count": 4 }],
  "auctionsByStatus": [{ "status": "active", "count": 3 }],
  "topAuctions": [
    { "id": "…", "title": "…", "status": "active", "currentPrice": 85000.00,
      "bidCount": 3, "winnerName": null }
  ],
  "recentBids": [ /* BidDto */ ],
  "generatedAtUtc": "2026-11-19T08:20:00Z"
}
```

### `GET /api/admin/users`

Query `q`, `page`, `pageSize`. Trả `PagedResult<AdminUserDto>` gồm `isActive` và `bidCount`.

### `PUT /api/admin/users/{id}/role`

```json
{ "role": "admin" }
```

`200 OK` kèm người dùng đã cập nhật. Tự giáng quyền bị từ chối (`409`).

### `PUT /api/admin/users/{id}/status`

```json
{ "isActive": false }
```

`200 OK`. Tự khóa tài khoản bị từ chối (`409`).

### `GET /api/admin/audit-logs`

Query `eventType`, `page`, `pageSize`. Trả `PagedResult<AuditLogDto>`:

```json
{
  "id": "…",
  "eventId": "…",
  "eventType": "BidAccepted",
  "auctionId": "…",
  "userId": "…",
  "payload": "{\"eventId\":\"…\",\"amount\":16000.00,…}",
  "occurredAtUtc": "2026-11-19T08:21:39Z",
  "processedAtUtc": "2026-11-19T08:21:40Z"
}
```

### `GET /api/admin/products`

Alias tiện lợi của danh mục sản phẩm với `pageSize` mặc định 50.

---

## 9. Endpoint nội bộ (không công khai)

Các endpoint này yêu cầu `X-Internal-Api-Key` và bị loại khỏi Swagger. Chỉ `Auction.HubConsumer` gọi tới.

| Method | Path | Body |
| --- | --- | --- |
| `POST` | `/internal/hub/bid-accepted` | `BidAcceptedEvent` |
| `POST` | `/internal/hub/auction-ended` | `AuctionEndedEvent` |

Phản hồi thành công: `202 Accepted` kèm `{ "status": "broadcasted" }`. Sai khóa: `401`.

---

## 10. SignalR hub

URL: `http://localhost:5100/hubs/auction`

```ts
const connection = new HubConnectionBuilder()
  .withUrl(HUB_URL, { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build()

await connection.start()
await connection.invoke('JoinAuction', auctionId)

connection.on('BidAccepted', (evt) => { /* cập nhật giá + luồng lượt giá */ })
connection.on('AuctionEnded', (evt) => { /* hiển thị kết quả */ })
```

### Sự kiện server → client

| Sự kiện | Payload |
| --- | --- |
| `Connected` | `{ serverTimeUtc }` |
| `JoinedAuction` | `{ auctionId }` |
| `BidAccepted` | `{ auctionId, bidId, bidderId, bidderName, amount, previousPrice, currentPrice, minimumNextBid, placedAtUtc, sequence, auctionTitle }` |
| `AuctionEnded` | `{ auctionId, auctionTitle, winnerId, winnerName, winningBidId, winningAmount, totalBids, endedAtUtc }` |

### Phương thức client → server

| Phương thức | Tham số | Hiệu ứng |
| --- | --- | --- |
| `JoinAuction` | `auctionId` (chuỗi GUID) | Thêm connection vào `auction:{id}` |
| `LeaveAuction` | `auctionId` (chuỗi GUID) | Xóa khỏi nhóm |

Hub chỉ đọc: nó không mở phương thức nào có thể tạo hoặc chấp nhận lượt giá, nên client không thể
giả mạo sự kiện realtime. Mọi broadcast đều bắt nguồn từ sự kiện RabbitMQ.

---

## 11. Vỏ phân trang

```json
{
  "items": [],
  "page": 1,
  "pageSize": 12,
  "total": 0,
  "totalPages": 0
}
```

Trang được giới hạn ≥ 1; kích thước trang được giới hạn ở mức tối đa của endpoint (100 cho lượt giá).
