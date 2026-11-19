# Báo cáo thực hành Bài 3: Giao tiếp thời gian thực với SignalR

## Bước 1.1: Tạo solution và build PingSignalR
![alt text](buoc1.1-build.png.png)
- **Thấy:** Lệnh `dotnet build` báo `Build succeeded` với 0 lỗi.
- **Giải thích:** Khởi tạo thành công solution `PingSignalR` gồm 2 project `Ping.Api` (máy chủ) và `Ping.Client` (máy khách đã cài gói `Microsoft.AspNetCore.SignalR.Client` phiên bản 10.0.15).
## Bước 1.2: ghi danh project, thêm gói SignalR
![alt text](buoc1.2-build.png-1.png)
## Bước 2: negotiate
![negotiate](anh/buoc2-negotiate.png)
- **Thấy:** `curl` trả về `connectionId` cùng 3 cơ chế truyền tải (`WebSockets`, `ServerSentEvents`, `LongPolling`); truy cập trực tiếp `GET /hubs/ping` báo `400 Connection ID required`.
- **Giải thích:** Máy chủ báo các cách truyền tải hỗ trợ và hoàn tất bước bắt tay ban đầu để thương lượng giao thức kết nối SignalR.
## Bước 3 & 4: Cài đặt phương thức Echo và chạy máy khách Ping.Client
![echo](anh/buoc4-echo.png)
- **Thấy:** Máy khách kết nối thành công tới `/hubs/ping` với `ConnectionId` trùng khớp trên log máy chủ; khi gửi tin nhắn từ `Ping.Client`, máy chủ nhận và phản hồi lại ngay `<< Echo: <tin nhắn>`.
- **Giải thích:** Client gọi phương thức `Echo` trên Hub qua `InvokeAsync`, máy chủ xử lý và gọi ngược lại sự kiện `"Echo"` phía người gọi thông qua `Clients.Caller.SendAsync`.

