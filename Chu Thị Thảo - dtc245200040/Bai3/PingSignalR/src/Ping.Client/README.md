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