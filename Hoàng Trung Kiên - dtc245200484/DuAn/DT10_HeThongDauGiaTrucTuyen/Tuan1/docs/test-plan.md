# Kế hoạch kiểm tra

## 1. Mục tiêu

Xác minh Aurelia đúng đắn, bền vững và an ninh trong điều kiện đặt giá bình thường và cạnh tranh; rằng mọi tích hợp bắt buộc (gRPC, giao dịch PostgreSQL, RabbitMQ, SignalR, Audit Worker) thực sự chạy; và rằng frontend biên dịch và dùng API thực không có mock thay thế.

## 2. Phạm vi

### Trong phạm vi

- Quy tắc đặt giá domain (số tiền, khung giờ, vai trò, quyền sở hữu, thập phân, làm tròn).
- Hợp đồng sự kiện và tuần tự hóa JSON.
- Đường ghi lượt giá với PostgreSQL thực tế, gồm giao dịch, khóa dòng, tính nguyên tử của outbox và ủy quyền.
- Endpoint REST xác thực, sản phẩm, phiên đấu giá, người dùng và quản trị.
- Dịch vụ xác thực gRPC và vận chuyển của nó.
- Topology RabbitMQ, publish, consume, retry/DLQ và idempotency.
- Phân phối SignalR từ sự kiện broker tới client đã kết nối.
- Lưu trữ của Audit Worker.
- Vòng đời phiên (kích hoạt, kết thúc tự động, kết thúc thủ công, chọn người thắng).
- Build frontend sản xuất với TypeScript nghiêm ngặt và tích hợp API thực.
- Cấu hình Docker Compose (xác thực tĩnh; xác thực runtime phụ thuộc ảo hóa của máy chủ).

### Ngoài phạm vi

- Mở rộng thanh toán, vận chuyển, email, chống sniping.
- Ma trận trình duyệt/thiết bị và kiểm tra tải vượt quá bài kiểm tra đồng thời.
- Cứng hóa TLS/ingress cho sản xuất.

## 3. Cấp độ kiểm tra

| Cấp độ | Công cụ | Chứng minh điều gì |
| --- | --- | --- |
| Đơn vị | xUnit, C# thuần | `BidRules` và tuần tự hóa sự kiện đúng khi tách riêng |
| Tích hợp | xUnit + Npgsql + PostgreSQL thực | Ánh xạ EF, migration, giao dịch, khóa dòng, tính nguyên tử outbox |
| E2E dịch vụ | PowerShell/curl vào dịch vụ đang chạy | REST → gRPC → DB → RabbitMQ → SignalR/Audit |
| Build UI | `tsc -b && vite build` | Không lỗi kiểu; asset biên dịch được |
| UI thủ công | Trình duyệt | Trang hiển thị, biểu mẫu hoạt động, cập nhật realtime xuất hiện |
| Cấu hình tĩnh | Kiểm tra YAML, build | Compose wiring và Dockerfile nhất quán |

## 4. Môi trường

| Thành phần | Phiên bản / endpoint |
| --- | --- |
| Hệ điều hành | Windows 11 (máy xác minh) |
| .NET SDK | 10.0.202 |
| Node.js | 24.20.0 |
| PostgreSQL | 17.6 portable, `localhost:5433` |
| RabbitMQ | 4.0.5 portable + Erlang/OTP 27, AMQP `5673`, UI `15673` |
| API | `http://localhost:5100` |
| gRPC | `http://localhost:5101` |
| Hub Consumer | `http://localhost:5102` |
| Audit Worker | `http://localhost:5103` |
| Frontend | `http://localhost:5173` |
| Database kiểm tra | `auctiondb_test` (tự tạo) |

Docker Desktop ban đầu không khả dụng trên máy xác minh do ảo hóa phần cứng bị tắt. Các dịch vụ
đã được chạy cục bộ; artifacts Docker vẫn được cung cấp và xác thực tĩnh. (Sau này Docker Desktop
đã khả dụng và toàn bộ stack đã chạy lại bằng Compose để xác minh cuối — xem test-results.md.)

## 5. Dữ liệu kiểm tra

- Bài kiểm tra đơn vị dùng giá trị `AuctionSnapshot`/`BidderSnapshot` cố định và đồng hồ cố định (`2026-04-10T15:00Z`).
- Bài kiểm tra tích hợp tạo `auctiondb_test`, áp dụng migration thực và cắt bảng giữa các bài.
- E2E dùng tài khoản và phiên mẫu:
  - Quản trị: `admin@auction.local` / `Admin@123`
  - Người đấu giá: `alice@auction.local`, `bob@auction.local`, `chloe@auction.local` / `Bidder@123`
- Một phiên đang diễn ra (`Đồng hồ Chronograph Sapphire Đêm — Số 07`) dùng cho đường đặt giá hợp lệ.
- Phiên đã kết thúc và sắp diễn ra bao phủ đường từ chối và kết quả.

## 6. Ca kiểm tra

### 6.1 Quy tắc domain (`BidRulesTests`)

| ID | Ca | Kỳ vọng |
| --- | --- | --- |
| R-01 | Lượt giá đúng bằng giá hợp lệ kế tiếp | chấp nhận |
| R-02 | Lượt giá cao hơn mức tối thiểu | chấp nhận |
| R-03 | Giá bằng giá hiện tại | `BID_TOO_LOW` |
| R-04 | Giá thấp hơn giá hiện tại | `BID_TOO_LOW` |
| R-05 | Giá cao hơn hiện tại nhưng thấp hơn bước giá | `BID_STEP_TOO_SMALL` |
| R-06 | Số tiền bằng 0 | `INVALID_AMOUNT` |
| R-07 | Số tiền âm | `INVALID_AMOUNT` |
| R-08 | Ba chữ số thập phân | `INVALID_AMOUNT` |
| R-09 | Làm tròn nửa xu | `RoundMoney` away from zero |
| R-10 | Phiên lên lịch | `AUCTION_NOT_STARTED` |
| R-11 | Active nhưng trước giờ mở | `AUCTION_NOT_STARTED` |
| R-12 | Thời gian đóng đúng bằng hiện tại | `AUCTION_ENDED` |
| R-13 | Trạng thái ended | `AUCTION_ENDED` |
| R-14 | Trạng thái cancelled | `AUCTION_NOT_ACTIVE` |
| R-15 | Thiếu phiên | `AUCTION_NOT_FOUND` |
| R-16 | Thiếu người đặt giá | `BIDDER_NOT_FOUND` |
| R-17 | Người đặt giá không hoạt động | `BIDDER_INACTIVE` |
| R-18 | Người đặt giá là quản trị viên | `SELF_BID_NOT_ALLOWED` |
| R-19 | Người tạo tự đặt giá phiên của mình | `SELF_BID_NOT_ALLOWED` |
| R-20 | Giá hiện tại fallback về giá khởi điểm | tối thiểu tính từ giá khởi điểm |
| R-21 | Làm tròn giá hợp lệ kế tiếp | hai chữ số thập phân |
| R-22 | Lượt đầu bằng start + increment | chấp nhận |
| R-23 | Đúng thời điểm mở cửa | chấp nhận |
| R-24 | Giá snapshot thấp hơn giá khởi điểm | bắt đầu từ giá khởi điểm |
| R-25 | Hằng số cửa sổ đặt giá tối đa | 90 ngày |

### 6.2 Hợp đồng (`EventContractTests`)

| ID | Ca | Kỳ vọng |
| --- | --- | --- |
| C-01 | Round-trip `BidAccepted` | bảo toàn mọi trường |
| C-02 | Round-trip `AuctionEnded` với winner null | bảo toàn null |
| C-03 | JSON dùng camelCase | tên thuộc tính đúng |
| C-04 | Độ chính xác decimal tồn tại qua JSON | xu chính xác |
| C-05 | Timestamp UTC tuần tự hóa với `Z`/offset | round-trip bằng nhau |
| C-06 | Hằng số topology | tên exchange/queue/routing ổn định |

### 6.3 Tích hợp lượt giá (`BidServiceTests`, PostgreSQL thực)

| ID | Ca | Kỳ vọng |
| --- | --- | --- |
| I-01 | Lượt hợp lệ lưu bid + participant + outbox trong một commit | mọi dòng tồn tại, giá cập nhật |
| I-02 | Hai lượt đồng thời cùng giá | đúng một `201`; một bị từ chối |
| I-03 | Các lượt tuần tự | số thứ tự tăng, giá tăng, số người tham gia tăng |
| I-04 | Giá thấp hơn bước giá dù pre-validator OK | `BID_STEP_TOO_SMALL` |
| I-05 | Quản trị viên | `SELF_BID_NOT_ALLOWED` |
| I-06 | Người đặt giá bị khóa | `BIDDER_INACTIVE` |
| I-07 | Phiên của chính mình | `SELF_BID_NOT_ALLOWED` |
| I-08 | Cửa sổ đã đóng | `AUCTION_ENDED` |
| I-09 | Phiên không tồn tại | `AUCTION_NOT_FOUND` |
| I-10 | Số tiền bằng 0 | bị từ chối trước khi xác thực |

### 6.4 E2E dịch vụ

| ID | Ca | Kỳ vọng |
| --- | --- | --- |
| E-01 | Sức khỏe API | `200` |
| E-02 | Đăng nhập alice | trả JWT |
| E-03 | Liệt kê phiên | dữ liệu mẫu, trạng thái đúng |
| E-04 | Đặt giá hợp lệ | `201`, giá cập nhật |
| E-05 | Log gRPC | `Validated bid ... => True OK` |
| E-06 | Dòng outbox | pending → published |
| E-07 | Queue RabbitMQ | số thông điệp, binding |
| E-08 | Client SignalR | nhận `BidAccepted` với cùng bid id |
| E-09 | API kiểm toán | tồn tại dòng `BidAccepted` |
| E-10 | Lượt giá không hợp lệ | `409 BID_TOO_LOW` kèm giá kế tiếp |
| E-11 | Đặt giá chưa xác thực | `401` |
| E-12 | Quản trị viên đặt giá | `403` |
| E-13 | Kết thúc phiên | `200`, có người thắng |
| E-14 | `AuctionEnded` realtime + kiểm toán | sự kiện nhận được và ghi kiểm toán |
| E-15 | Đặt giá HTTP đồng thời | đúng một lượt thành công |
| E-16 | RabbitMQ downtime | vẫn `201`; outbox chờ; publish sau |

### 6.5 Frontend

| ID | Ca | Kỳ vọng |
| --- | --- | --- |
| F-01 | `npm run build` | exit 0, không lỗi TypeScript |
| F-02 | Trang chủ | hero, thống kê, lô nổi bật render từ API |
| F-03 | Danh sách đấu giá | lọc, tìm kiếm, phân trang hoạt động |
| F-04 | Chi tiết | đếm ngược, biểu mẫu, chuyển 3D/ảnh, biểu đồ |
| F-05 | Đăng nhập/Đăng ký | kiểm tra, đăng nhập mẫu, chuyển hướng |
| F-06 | Hồ sơ | thống kê, tab, lịch sử |
| F-07 | Quản trị | bảng điều khiển, modal CRUD sản phẩm/phiên, người dùng, kiểm toán |
| F-08 | Không có nút chết | mọi thao tác thực hiện yêu cầu hoặc đổi trạng thái thực |

## 7. Thực thi

```powershell
# 1. Build mọi thứ
dotnet build backend\OnlineAuction.slnx -c Release

# 2. Kiểm tra đơn vị + tích hợp (PostgreSQL phải chạy)
$env:AUCTION_TEST_PG_ADMIN="Host=localhost;Port=5433;Username=postgres;Password=auction_dev_password;Database=postgres"
$env:AUCTION_TEST_PG="Host=localhost;Port=5433;Username=auction;Password=auction_dev_password;Database=auctiondb_test"
dotnet test backend\Auction.Tests\Auction.Tests.csproj

# 3. Build frontend
cd frontend; npm ci; npm run build

# 4. Khởi động hạ tầng + dịch vụ (cục bộ hoặc docker compose up --build)

# 5. Chạy danh sách E2E ở mục 6.4
```

## 8. Tiêu chí vào và ra

**Vào**

- Solution và frontend build được.
- PostgreSQL đến được với schema thực.
- RabbitMQ đến được với user/vhost mong đợi.
- Cả bốn dịch vụ khỏe.

**Ra**

- 100% bài kiểm tra tự động đậm.
- Mọi ca E2E đậm hoặc có giới hạn được ghi nhận và giải thích.
- Không lỗi `500` cho yêu cầu hợp lệ.
- Một lượt giá được chứng minh đi qua REST → gRPC → PostgreSQL → RabbitMQ → SignalR và → Audit Worker.
- Không còn lỗi Sev-1/Sev-2 đã biết.

## 9. Phân loại lỗi

| Mức | Định nghĩa | Ví dụ |
| --- | --- | --- |
| Sev-1 | Mất dữ liệu, sai tiền, vượt xác thực, luồng cốt lõi hỏng | lượt giá trùng được chấp nhận cùng giá |
| Sev-2 | Tính năng lớn hỏng không có cách giải quyết | SignalR không bao giờ giao lượt giá |
| Sev-3 | Lỗi chức năng/UI nhỏ có cách giải quyết | nhãn bộ lọc sai |
| Sev-4 | Thẩm mỹ | khoảng cách |

## 10. Truy vết

| Yêu cầu | Bài kiểm tra |
| --- | --- |
| FR-AUTH-1..6 | E-02, E-11, UI thủ công, xác thực API |
| FR-BID-1..9 | R-01..R-25, I-01..I-10, E-04..E-16 |
| FR-RT-1..5 | E-08, E-14, UI thủ công |
| FR-MSG-1..6 | E-06, E-07, E-09, E-14, I-01 |
| FR-CAT-1..7 | E-03, E-13, UI thủ công |
| FR-ADM-1..4 | UI thủ công, E-09, kiểm tra API |
| NFR-1 | I-02, E-15 |
| NFR-2 | I-01, E-16 |
| NFR-3 | E-11, E-12, bài kiểm tra xác thực |
| NFR-7 | build + chạy cục bộ + artifacts Compose |
| NFR-8 | 41 bài kiểm tra tự động |
