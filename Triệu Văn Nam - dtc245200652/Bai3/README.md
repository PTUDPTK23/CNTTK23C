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
## Bước 5: Kiểm thử tự động kết nối lại (WithAutomaticReconnect)
![reconnect](anh/buoc5-reconnect.png)
- **Thấy:** Khi tắt máy chủ đột ngột, máy khách chuyển sang trạng thái `[!] Dang ket noi lai...`; ngay sau khi khởi động lại máy chủ, máy khách tự động phục hồi kết nối, nhận `ConnectionId` mới và tiếp tục gửi/nhận `Echo` bình thường
- **Giải thích:** Cấu hình `.WithAutomaticReconnect()` giúp `HubConnection` tự động thử kết nối lại theo các khoảng trễ mặc định (0s, 2s, 10s, 30s) và cấp phát `ConnectionId` mới khi phiên kết nối được khôi phục.
## Bước 6: Giao tiếp Broadcast (Clients.All) và theo nhóm (Groups)
![broadcast-group](anh/buoc6-broadcast-group.png)
- **Thấy:** Tin nhắn `/all` được phát quảng bá tới tất cả máy khách (`[Broadcast - Nam]: Chao ca lop`); khi tham gia phòng bằng `/join K23C` và gửi `/room K23C ...`, hệ thống thông báo vào phòng và chuyển tin nhắn tới đúng các thành viên thuộc nhóm `K23C`
- **Giải thích:** Máy chủ dùng `Clients.All.SendAsync` để gửi thông báo toàn cục, và dùng `Groups.AddToGroupAsync` kết hợp `Clients.Group(roomName).SendAsync` để phân tách luồng giao tiếp theo từng phòng riêng biệt.
