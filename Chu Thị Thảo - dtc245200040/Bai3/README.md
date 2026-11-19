# Bài 3: Giao tiếp thời gian thực với SignalR

## Bước 1: Tạo solution và 2 project PingSignalR
![build](images/buoc1-build.png)
Thấy: `Build succeeded` cho cả `Ping.Api` và `Ping.Client` (0 Error).
Giải thích: Tạo solution quản lý 2 tiến trình máy chủ (web) và máy khách (console), cài đặt gói `Microsoft.AspNetCore.SignalR.Client` phiên bản 10.0.15 cho máy khách và tạo tệp `.gitignore`.

## Bước 2: Cấu hình Hub và kiểm tra negotiate
![negotiate](images/buoc2-negotiate.png)
Thấy: Lệnh `curl.exe` gọi `/hubs/ping/negotiate` trả về `connectionId` và 3 cơ chế truyền tải.
Giải thích: Khi bắt tay, máy chủ báo cho máy khách các cơ chế truyền tải hỗ trợ theo thứ tự ưu tiên gồm `WebSockets`, `ServerSentEvents` và `LongPolling`.

## Bước 3-4: Phương thức Echo và kết nối máy khách
![echo](images/buoc4-echo.png)
Thấy: Máy khách `An` kết nối thành công và nhận về dòng `[Echo] Echo: Xin chao SignalR tu An`.
Giải thích: Máy khách gọi `InvokeAsync("Echo", ...)` lên hub; máy chủ dùng `Clients.Caller` để gửi trả thông điệp `EchoResponse` về riêng cho đúng kết nối vừa gọi.
## Bước 5: Broadcast và Group
![nhom](images/buoc5-nhom.png)
Thấy: Cửa sổ của An nhận được thông báo `[Group]` và `[Broadcast]` từ Bình gửi sang, nhưng không nhận được `[Echo]` của Bình.
Giải thích: `Clients.Caller` chỉ gửi riêng cho người gọi; `Clients.Group` gửi cho thành viên trong nhóm `lop-hoc-01`; `Clients.All` gửi cho mọi kết nối đang mở.

## Bước 6: Vòng đời kết nối và tự động nối lại
![noilai](images/buoc6-noilai.png)
Thấy: Khi tắt máy chủ, client hiện `[Lifecycle] Dang ket noi lai...`; khi bật lại máy chủ, client hiện `[Lifecycle] Da ket noi lai:` kèm một `ConnectionId` mới.
Giải thích: `WithAutomaticReconnect()` tự động thử kết nối lại theo lịch 0s, 2s, 10s, 30s và tạo phiên kết nối mới với mã `ConnectionId` mới.

## Bước 7: Ba thí nghiệm lỗi phân tán
### Thí nghiệm 1: Máy chủ chưa chạy
![loi1](images/buoc7-loi1.png)
Thấy: Ném ngoại lệ `HttpRequestException: Connection refused (localhost:5200)`.
Giải thích: `StartAsync()` lỗi ngay lần đầu; `WithAutomaticReconnect()` chỉ cứu các kết nối đã mở thành công trước đó.

### Thí nghiệm 2: Sai đường dẫn Hub (`/hub/ping`)
![loi2](images/buoc7-loi2.png)
Thấy: Báo lỗi `404 (Not Found)`.
Giải thích: Lỗi xảy ra ngay tại bước bắt tay `negotiate` vì đường dẫn không khớp với `MapHub("/hubs/ping")` phía máy chủ.

### Thí nghiệm 3: Sai tên thông điệp (`EchoRespone`)
![loi3](images/buoc7-loi3.png)
Thấy: Không báo lỗi biên dịch hay lỗi chạy, nhưng không in ra dòng `[Echo]`.
Giải thích: SignalR định tuyến thông điệp dựa trên tên chuỗi khớp chính xác; lệch tên ở chiều server gửi xuống sẽ bị client bỏ qua một cách im lặng.