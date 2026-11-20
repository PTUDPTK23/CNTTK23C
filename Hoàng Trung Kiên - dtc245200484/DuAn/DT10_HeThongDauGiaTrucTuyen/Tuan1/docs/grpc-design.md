# Dịch vụ xác thực giá gRPC

## 1. Mục đích

Bid Validation Service là cơ quan xác thực độc lập quyết định một lượt giá có được chấp nhận hay không trước khi API mở giao dịch ghi cơ sở dữ liệu. Đây là một tiến trình riêng với endpoint Kestrel riêng và quyền truy cập PostgreSQL chỉ đọc.

Sự tách biệt này minh họa một ranh giới dịch vụ thực tế: API không thể "tự tin tưởng chính nó", và việc xác thực có thể phát triển hoặc mở rộng độc lập.

## 2. Hợp đồng

File: `backend/BidValidation.Contracts/Protos/bid_validation.proto`

```proto
syntax = "proto3";
option csharp_namespace = "BidValidation.Contracts";
package bidvalidation;

service BidValidationService {
  rpc ValidateBid (BidValidationRequest) returns (BidValidationResponse);
  rpc GetHealth (HealthRequest) returns (HealthResponse);
}

message BidValidationRequest {
  string auction_id = 1;
  string bidder_id = 2;
  string amount = 3;              // decimal as a string
}

message BidValidationResponse {
  bool is_valid = 1;
  string code = 2;                // OK or a BidErrorCodes value
  string message = 3;
  string current_price = 4;
  string minimum_next_bid = 5;
  string server_time_utc = 6;
  string auction_status = 7;
}

message HealthRequest {}
message HealthResponse {
  string status = 1;
  string service = 2;
  string server_time_utc = 3;
}
```

### Tại sao số tiền là chuỗi

`double` của Protobuf không biểu diễn chính xác `decimal`. Dùng chuỗi và parse bằng
`decimal.Parse(..., InvariantCulture)` bảo toàn số xu và tránh lỗi dấu phẩy động nhị phân.

### Tại sao GUID là chuỗi

Hợp đồng giữ tính trung lập ngôn ngữ và khớp với biểu diễn REST/JSON. Dịch vụ kiểm tra định dạng GUID và trả lỗi có cấu trúc cho đầu vào sai.

## 3. Triển khai dịch vụ

`BidValidation.Service/Services/BidValidationGrpcService.cs`:

1. Parse `auction_id`, `bidder_id` và `amount`.
2. Tải phiên (với lượt giá cao nhất) và người đặt giá qua `AuctionDbContext` dùng `AsNoTracking()`.
3. Tạo `AuctionSnapshot` và `BidderSnapshot`.
4. Gọi `BidRules.Validate(snapshot, bidder, amount, utcNow)` — cùng hàm domain mà API dùng.
5. Trả `is_valid`, `code`, `message`, giá quyết định và thời gian máy chủ.

Dịch vụ dùng abstraction `IClock` chung (`SystemClock`) và không bao giờ ghi vào cơ sở dữ liệu.

## 4. Quy tắc xác thực

| Quy tắc | Mã |
| --- | --- |
| Phiên phải tồn tại | `AUCTION_NOT_FOUND` |
| Phiên đã hủy | `AUCTION_NOT_ACTIVE` |
| Trạng thái ended hoặc `now >= end_time_utc` | `AUCTION_ENDED` |
| Scheduled hoặc `now < start_time_utc` | `AUCTION_NOT_STARTED` |
| Bất kỳ trạng thái không active khác | `AUCTION_NOT_ACTIVE` |
| Người đặt giá phải tồn tại | `BIDDER_NOT_FOUND` |
| Người đặt giá phải hoạt động | `BIDDER_INACTIVE` |
| Quản trị viên không được đặt giá | `SELF_BID_NOT_ALLOWED` |
| Không đặt giá phiên do chính mình tạo | `SELF_BID_NOT_ALLOWED` |
| Số tiền > 0 và tối đa 2 chữ số thập phân | `INVALID_AMOUNT` |
| Số tiền > giá hiện tại | `BID_TOO_LOW` |
| Số tiền ≥ giá hiện tại + bước giá | `BID_STEP_TOO_SMALL` |

`current_price` là `max(start_price, auctions.current_price, highest recorded bid)`.

## 5. Tại sao xác thực chạy hai lần

Kiểm tra gRPC xảy ra trước giao dịch. Giữa lần kiểm tra đó và khóa dòng, một người đặt giá khác có thể thay đổi giá. Vì vậy API chạy lại `BidRules` **bên trong** giao dịch đã khóa và coi kết quả thứ hai là quyết định.

- Lần đầu cung cấp quyết định độc lập tại ranh giới dịch vụ và từ chối nhanh.
- Lần hai đảm bảo tính tuần tự.
- Cả hai gọi cùng mã `BidRules`, nên chúng không thể khác nhau về chính sách — chỉ khác về trạng thái quan sát được.

Kết quả gRPC là tiền xác thực, không phải đặt chỗ. Điều này là cố ý và được ghi trong mã phản hồi của API.

## 6. Xử lý lỗi và timeout

- GUID hoặc decimal sai định dạng → `is_valid=false` với `INVALID_AMOUNT` (hoặc thông điệp mô tả) thay vì ném exception.
- Lỗi cơ sở dữ liệu → gRPC `INTERNAL`; API ánh xạ lỗi vận chuyển thành `503 VALIDATION_UNAVAILABLE`.
- Client API dùng hạn chót 5 giây (`BidValidation:TimeoutSeconds`).
- Dịch vụ gRPC chỉ bật lỗi chi tiết cho chẩn đoán phát triển.

## 7. Hosting và cấu hình

| Thiết lập | Giá trị cục bộ |
| --- | --- |
| Địa chỉ | `http://localhost:5101` (chỉ HTTP/2) |
| Địa chỉ client API | `BidValidation:Address` |
| Cơ sở dữ liệu | `ConnectionStrings:Default` (dùng chỉ đọc) |
| Giao thức | HTTP/2 cleartext (h2c) cho cục bộ/Compose |

Chương trình:

```csharp
builder.WebHost.ConfigureKestrel(k => k.ListenAnyIP(port,
    l => l.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();
builder.Services.AddAuctionPersistence(builder.Configuration);
builder.Services.AddScoped<BidValidationEvaluator>();
app.MapGrpcService<BidValidationGrpcService>();
```

## 8. Kiểm tra

- `BidRulesTests` bao phủ mọi quy tắc, ranh giới và trường hợp làm tròn (~25 bài).
- `BidServiceTests` thực hiện đường đi của API gọi validator qua client gRPC thực tế.
- Xác minh thủ công (ghi trong `test-results.md`) đã lưu dòng log của dịch vụ:
  `Validated bid ... amount=16000.00 => True OK`.

## 9. Kiểm tra vận hành

```bash
# sức khỏe (grpcurl)
grpcurl -plaintext localhost:5101 bidvalidation.BidValidationService/GetHealth

# xác thực một lượt giá
grpcurl -plaintext -d '{
  "auction_id":"<guid>","bidder_id":"<guid>","amount":"16500.00"
}' localhost:5101 bidvalidation.BidValidationService/ValidateBid
```

Dịch vụ không mở endpoint HTTP/1.1 hay Swagger; cố tình chỉ dùng gRPC.
