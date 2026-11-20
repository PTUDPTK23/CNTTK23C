# Kết quả kiểm tra

Tài liệu này ghi lại kết quả **thực tế** của việc xây dựng và xác minh Aurelia vào ngày 20/11/2026, trên stack Docker Compose (PostgreSQL, RabbitMQ, API, gRPC, Hub Consumer, Audit Worker, frontend). Các mã lỗi và thông điệp hiển thị ở đây bằng tiếng Việt đúng như phiên bản đã triển khai.

## 1. Môi trường xác minh

| Thành phần | Giá trị |
| --- | --- |
| Hệ điều hành | Windows 11, Docker Desktop + Compose v2 |
| .NET SDK | 10.0.202 |
| Node.js | 24.20.0 |
| Container | `aurelia-postgres-1`, `aurelia-rabbitmq-1`, `aurelia-api-1`, `aurelia-grpc-1`, `aurelia-hubconsumer-1`, `aurelia-auditworker-1`, `aurelia-frontend-1` |
| Cổng | API `5100`, gRPC `5101`, Hub `5102`, Audit `5103`, PostgreSQL `5433`, RabbitMQ `5673`/`15673`, frontend `5173` |

Lệnh dựng lại stack (dữ liệu mẫu tiếng Việt được tạo lại từ đầu):

```powershell
cd D:\daugiaonline
docker compose down -v
docker compose up -d --build
```

Kết quả: toàn bộ ảnh (`aurelia-api`, `aurelia-grpc`, `aurelia-hubconsumer`, `aurelia-auditworker`, `aurelia-frontend`).build thành công; năm dịch vụ .NET và frontend đều ở trạng thái `Started`; PostgreSQL và RabbitMQ đạt `Healthy`.

## 2. Build

```powershell
dotnet build backend\OnlineAuction.slnx -v m
```

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:10.19
```

```powershell
cd frontend; npm run build
```

```
tsc -b && vite build
✓ 3226 modules transformed.
✓ built in 4.63s
```

Chỉ có cảnh báo kích thước chunk (> 500 kB) từ Vite; không có lỗi TypeScript. Cảnh báo này không ảnh hưởng chức năng (chunk chính chứa React, Framer Motion và Three.js).

## 3. Kiểm tra tự động

```powershell
dotnet test backend\OnlineAuction.slnx -v q
```

```
Passed!  - Failed:     0, Passed:    41, Skipped:     0, Total:    41, Duration: 6 s
```

Trong đó:

- Khoảng 25 bài `BidRulesTests` (quy tắc, biên, làm tròn).
- 6 bài `EventContractTests` (tuần tự hóa, camelCase, decimal, UTC, topology).
- 10 bài `BidServiceTests` tích hợp với PostgreSQL thực tế, kể cả cuộc đua hai lượt giá cùng mức.
- Phần fixture kiểm tra có fallback: nếu vai trò `postgres` không tồn tại (trường hợp container Compose chỉ có vai trò `auction`), fixture tự dùng thông tin của `auction` để tạo `auctiondb_test`. Nhờ đó 41 bài đều chạy được cả trong Compose lẫn cục bộ.

## 4. Sức khỏe dịch vụ

```bash
curl http://localhost:5100/health
curl http://localhost:5102/health
curl http://localhost:5103/health
```

```json
{"status":"healthy","service":"Auction.API"}
{"status":"healthy","service":"Auction.HubConsumer"}
{"status":"healthy","service":"Auction.AuditWorker"}
```

Frontend `http://localhost:5173` trả `200`.

## 5. Dữ liệu mẫu tiếng Việt

```bash
curl http://localhost:5100/api/auctions
```

Tổng 6 phiên; các tiêu đề trả về:

```
Chronograph Sapphire Đêm — Số 07        | active   | 15500
Obsidian 'Cực Quang' — Bản đúc duy nhất | active   | 7400
Katana Edo kèm giấy NBTHK               | active   | 13000
GT Coupé 1967 — Phục chế hoàn hảo       | active   | 85000
Nhẫn Ngọc Lục Bảo Hoàng Gia — 4.8ct     | ended    | 10200
Bản thảo 1897 — Bộ sưu tập tư nhân      | scheduled| 3200
```

Tên người dùng mẫu: Nguyễn Hoàng Nam (quản trị), Nguyễn Minh Anh, Trần Quốc Bảo, Lê Thu Hà (người đấu giá). Email và mật khẩu giữ nguyên như README.

## 6. Đường ống đấu giá đầy đủ (E-04…E-09, E-14)

Lệnh:

```powershell
cd frontend
node scripts/signalr-e2e.mjs --clients 2
```

Kết quả chính:

```json
{"step":"joined","hubUrl":"http://localhost:5100/hubs/auction","auctionId":"2f661483-e48d-44a9-ae34-f3cd1591a288","clients":2}
{"step":"bid-accepted","status":201,"amount":16000,"bid":{"bidId":"8486ec14-726e-40fb-a31a-fe1112378c26","amount":16000,"currentPrice":16000,"minimumNextBid":16500,"bidderName":"Nguyễn Minh Anh","message":"Lượt đặt giá của bạn đã được chấp nhận."}}
{
  "step": "signalr-broadcast",
  "clients": 2,
  "allReceived": true,
  "consistent": true,
  "priceBefore": 15500,
  "priceAfter": 16000,
  "bidCountAfter": 5
}
```

Hai client nhận cùng một payload `BidAccepted` (cùng `bidId`, `sequence: 5`, cùng tiêu đề phiên tiếng Việt).

### Bằng chứng gRPC

```text
Validated bid auction=2f661483-... bidder=65a21bce-... amount=16000.00 => True OK
```

### Bằng chứng outbox và kiểm toán

```sql
SELECT event_type, routing_key, published_at_utc IS NOT NULL AS published, attempts
FROM outbox_messages ORDER BY occurred_at_utc DESC LIMIT 5;
```

```
 BidAccepted | bid.accepted | t | 1
 BidAccepted | bid.accepted | t | 1
```

```sql
SELECT event_type, auction_id, user_id, occurred_at_utc, source
FROM audit_logs ORDER BY occurred_at_utc DESC LIMIT 5;
```

```
 BidAccepted | 2f661483-e48d-44a9-ae34-f3cd1591a288 | 65a21bce-... | 2026-11-20 05:07:17.876413+00 | rabbitmq
```

### Bằng chứng RabbitMQ

```bash
docker exec aurelia-rabbitmq-1 rabbitmqctl list_queues name messages consumers
```

```
auction.hub.dlq   0  0
auction.hub       0  1
auction.audit.dlq 0  0
auction.audit     0  1
```

Không có thông điệp tồn đọng, DLQ rỗng, mỗi queue có đúng một consumer.

## 7. Từ chối, đồng thời và endpoint quản trị

Lệnh:

```powershell
cd frontend
node scripts/e2e-check.mjs
```

Kết quả:

```json
{"step":"dang-nhap","bidder":true,"admin":true,"auction":"Chronograph Sapphire Đêm — Số 07"}
{"case":"E-10 lượt giá thấp","statuses":409,"codes":"BID_TOO_LOW","messages":"Giá đặt phải cao hơn giá hiện tại 16500.00."}
{"case":"E-11 chưa xác thực","statuses":401}
{"case":"E-12 quản trị viên","statuses":403}
{"case":"E-04/E-15 đồng thời 2 lượt","statuses":[201,409],"codes":["OK","BID_TOO_LOW"],"messages":["Lượt đặt giá của bạn đã được chấp nhận.","Giá đặt phải cao hơn giá hiện tại 17000.00."]}
{"step":"quan-tri","dashboard":200,"dashboardKeys":14,"tuChoiKhongPhaiAdmin":403,"profile":200,"profileName":"Nguyễn Minh Anh","soLuotDatGia":8,"danhSachNguoiDung":4,"nhatKyKiemToan":3}
```

Kết luận:

- Lượt giá thấp bị từ chối kèm thông điệp tiếng Việt và `minimumNextBid`.
- Yêu cầu không có JWT bị `401`; quản trị viên đặt giá bị `403` (quy tắc domain).
- Hai lượt cùng mức giá đồng thời: đúng một `201`, một `409` — khóa dòng hoạt động.
- `/api/admin/dashboard` trả `200` cho admin, `403` cho bidder.
- `/api/users/me`, `/api/users/me/bids`, `/api/admin/users`, `/api/admin/audit-logs` đều trả dữ liệu thật.

## 8. Kết thúc phiên và sự kiện AuctionEnded

Lệnh:

```powershell
cd frontend
node scripts/e2e-ended.mjs
```

Kết quả tóm tắt:

```json
{
  "auctionId": "ba18ee5d-0a68-4ad7-92e1-31888431deb2",
  "endStatus": 200,
  "endBody": { "title": "Bản thảo 1897 — Bộ sưu tập tư nhân", "status": "ended", "hasWinner": false, "totalBids": 0 },
  "signalr": { "auctionTitle": "Bản thảo 1897 — Bộ sưu tập tư nhân", "totalBids": 0, "endedAtUtc": "2026-11-20T05:09:15.3021658Z" },
  "result": { "status": "ended", "hasWinner": false },
  "auditCount": 1
}
```

Bản ghi kiểm toán `AuctionEnded` có `eventId` duy nhất, `source: rabbitmq`, payload chứa tiêu đề phiên tiếng Việt. Trường hợp phiên có lượt giá được bao phủ bởi dữ liệu mẫu (`Nhẫn Ngọc Lục Bảo Hoàng Gia — 4.8ct`, người thắng Nguyễn Minh Anh, số tiền 10.200).

## 9. Giao diện người dùng

- `index.html` có `lang="vi"`, tiêu đề `Aurelia — Nhà đấu giá trực tuyến`.
- Tất cả trang (Home, Auctions, AuctionDetail, Login, Register, Profile, Admin gồm 5 trang) đã được rà soát: nhãn, menu, thông báo, placeholder, aria-label, trạng thái lỗi/trống và dữ liệu mẫu đều tiếng Việt; các định danh kỹ thuật (`BidAccepted`, endpoint, tên dịch vụ) giữ nguyên tiếng Anh.
- Quét JSX text nodes và thuộc tính `aria-label`/`placeholder`/`description` không còn chuỗi tiếng Anh human-readable.
- Kiểm tra trong trình duyệt qua accessibility snapshot: trang chủ, danh sách đấu giá, chi tiết phiên, đăng nhập, đăng ký, hồ sơ và cả năm trang quản trị đều hiển thị tiếng Việt; trang chi tiết báo `Đã kết nối kênh trực tiếp`, biểu mẫu đặt giá bị thay bằng thông báo `Tài khoản quản trị chỉ điều hành sàn và không thể đặt giá.` khi đăng nhập bằng admin. Console không có lỗi ở mọi trang đã kiểm tra.
- Sau khi đổi tên hiển thị demo sang tiếng Việt, nút đăng nhập nhanh trên trang Login cũng được cập nhật (`Người đấu giá · Nguyễn Minh Anh`, `Người đấu giá · Trần Quốc Bảo`) và ảnh frontend được rebuild.
- Scene 3D nhận diện cả danh mục tiếng Việt (`đồng hồ`, `trang sức`, `xe`, `nghệ thuật`, `sách`, `nhạc`, `nội thất`, `sưu tầm`) nên vật phẩm vẫn hiển thị đúng mô hình sau khi đổi dữ liệu mẫu.

## 10. Ma trận truy vết (cập nhật)

| Yêu cầu | Ca | Kết quả |
| --- | --- | --- |
| FR-AUTH-1..6 | E-02, E-11, E-12, profile | Đậm |
| FR-BID-1..9 | R-01..R-25, I-01..I-10, E-04, E-10..E-12, E-15 | Đậm |
| FR-RT-1..5 | E-08, E-14 | Đậm (2 client, payload nhất quán) |
| FR-MSG-1..6 | E-06, E-07, E-09, E-14, I-01 | Đậm |
| FR-CAT-1..7 | E-03, E-13, dữ liệu mẫu | Đậm |
| FR-ADM-1..4 | dashboard 200/403, users, audit | Đậm |
| NFR-1 | I-02, E-15 | Đậm (201 + 409) |
| NFR-2 | I-01, E-06 | Đậm (outbox published, attempts=1) |
| NFR-3 | E-11, E-12, 403 admin | Đậm |
| NFR-7 | Build + Compose | Đậm |
| NFR-8 | 41 bài tự động | Đậm |

## 11. Giới hạn đã biết

- E-16 (mô phỏng RabbitMQ downtime trong lúc commit) không được chạy lại trong lần xác minh Compose này; hành vi được bảo chứng bởi thiết kế outbox và bài I-01 (outbox được ghi trong cùng giao dịch với lượt giá). Outbox publisher có retry và ghi `last_error`.
- Cảnh báo chunk Vite > 500 kB vẫn còn; có thể tách further bằng dynamic import nếu cần tối ưu tải trang.
- Ảnh chụp màn hình không dùng được trong môi trường xác minh; việc kiểm tra UI được thực hiện qua nội dung trang (accessibility snapshot/console) và kiểm tra API trực tiếp.
