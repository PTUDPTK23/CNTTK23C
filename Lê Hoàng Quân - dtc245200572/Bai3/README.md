# Bài 3 – Phần 2: PingSignalR

## 1. Thông tin sinh viên

- **Họ và tên:** Lê Hoàng Quân
- **MSSV:** dtc245200572

## 2. Nhật ký thực hành `PingSignalR` (Bước 1 đến Bước 7)

### Bước 1: Tạo solution, project và build thử

![Bước 1 - Build](anh/buoc1-build.png)

- **Thấy gì:** `dotnet build` báo `Build succeeded. 0 Warning(s), 0 Error(s)` cho `Ping.Api` và `Ping.Client`.
- **Giải thích nguyên nhân:** Hai project được tạo đúng cấu hình; `Ping.Client` đã cài gói `Microsoft.AspNetCore.SignalR.Client`.

### Bước 2: Cấu hình Hub và kiểm tra bước bắt tay `negotiate`

![Bước 2 - Negotiate](anh/buoc2-negotiate.png)

- **Thấy gì:** Gọi `curl` tới `/hubs/ping/negotiate` nhận JSON có `connectionId` và mảng `availableTransports` gồm `WebSockets`, `ServerSentEvents`, `LongPolling`.
- **Giải thích nguyên nhân:** Endpoint Hub đã được ánh xạ tại `/hubs/ping`; SignalR dùng bước `negotiate` để tạo kết nối và thương lượng phương thức truyền tải.

### Bước 3 & 4: Phương thức `Echo` và máy khách gọi `InvokeAsync`

![Bước 4 - Echo](anh/buoc4-echo.png)

- **Thấy gì:** Máy khách kết nối thành công, gọi `Echo` và nhận phản hồi `[Echo] Echo: Xin chao SignalR tu An`.
- **Giải thích nguyên nhân:** `InvokeAsync` gọi phương thức Hub `Echo`; Hub gửi kết quả riêng về máy gọi thông qua `Clients.Caller`.

### Bước 5: Phát rộng (`Broadcast`) và Nhóm (`Group`)

![Bước 5 - Nhóm](anh/buoc5-nhom.png)

- **Thấy gì:** Hai máy khách An và Bình thấy tin `[Group]` của nhóm `lop-hoc-01` và tin `[Broadcast]` của nhau; An không nhận được tin `[Echo]` của Bình.
- **Giải thích nguyên nhân:** Tin nhóm được gửi tới các thành viên của nhóm, còn `Broadcast` gửi cho tất cả máy khách. `Echo` chỉ trả lời máy khách đã gọi qua `Clients.Caller`.

### Bước 6: Vòng đời kết nối và tự động kết nối lại (`WithAutomaticReconnect`)

![Bước 6 - Nối lại](anh/buoc6-noilai.png)

- **Thấy gì:** Khi tắt rồi bật lại máy chủ, máy khách in `[Lifecycle] Dang ket noi lai...` và tự kết nối lại với một `ConnectionId` mới.
- **Giải thích nguyên nhân:** `WithAutomaticReconnect` thử kết nối lại khi kết nối đang hoạt động bị ngắt; kết nối mới được cấp `ConnectionId` mới.

### Bước 7: Ba thí nghiệm lỗi điển hình

#### Lỗi 1 – Máy chủ chưa chạy

![Bước 7 - Lỗi 1](anh/buoc7-loi1.png)

- **Làm gì:** Dừng máy chủ rồi chạy máy khách.
- **Thấy gì:** Máy khách ném `HttpRequestException: Connection refused (localhost:5200)` ngay lập tức.
- **Giải thích vì sao:** `StartAsync()` thất bại ở lần kết nối ban đầu; `WithAutomaticReconnect` chỉ xử lý kết nối đã thiết lập rồi bị mất, không tự thử lại lần kết nối đầu tiên.

#### Lỗi 2 – Sai đường dẫn Hub `/hub/ping` thiếu chữ `s`

![Bước 7 - Lỗi 2](anh/buoc7-loi2.png)

- **Làm gì:** Đổi đường dẫn Hub của máy khách thành `/hub/ping`, thiếu chữ `s` so với `/hubs/ping`.
- **Thấy gì:** Máy khách báo lỗi `404 (Not Found)` ngay ở bước bắt tay `negotiate`.
- **Giải thích vì sao:** Đường dẫn máy khách gọi không khớp với tuyến được ánh xạ bằng `MapHub<PingHub>("/hubs/ping")`.

#### Lỗi 3 – Sai tên thông điệp chiều xuống `EchoRespone`

![Bước 7 - Lỗi 3](anh/buoc7-loi3.png)

- **Làm gì:** Đăng ký máy khách lắng nghe `EchoRespone` thay vì tên đúng `EchoResponse`.
- **Thấy gì:** Không có lỗi biên dịch hay lỗi runtime, nhưng máy khách không in dòng `[Echo]`.
- **Giải thích vì sao:** Tên thông điệp SignalR được đối chiếu theo chuỗi; khi tên không khớp, máy khách bỏ qua thông điệp mà không báo lỗi.
