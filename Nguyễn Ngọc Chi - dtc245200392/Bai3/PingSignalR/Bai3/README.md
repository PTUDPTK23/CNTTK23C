Phần 1: Thực hành cơ bản với PingSignalR
Mục tiêu của phần này là nắm vững cấu trúc cơ bản của SignalR gồm máy chủ, máy khách, các phạm vi gửi tin và vòng đời kết nối: 
    Bước 1 — Khởi tạo dự án: Tạo solution PingSignalR cùng 2 project là máy chủ Ping.Api và máy khách Ping.Client. Cài đặt gói Microsoft.AspNetCore.SignalR.Client phiên bản 10.0.15 cho máy khách và tạo tệp .gitignore để loại bỏ thư mục bin/, obj/.
        ![alt text](image.png)
    Bước 2 — Cấu hình Hub và bắt tay: Đăng ký dịch vụ AddSignalR() và ánh xạ PingHub vào đường dẫn /hubs/ping trên máy chủ. Dùng lệnh curl gọi bước bắt tay negotiate để kiểm tra máy chủ trả về connectionId cùng 3 cơ chế truyền tải dự phòng.
        ![alt text](image-1.png)
    Bước 3 & 4 — Giao tiếp hai chiều với Echo: Xây dựng phương thức Echo trên PingHub sử dụng Clients.Caller để phản hồi riêng cho người vừa gọi. Ở phía máy khách, khởi tạo HubConnectionBuilder, đăng ký hàm lắng nghe On<string>("EchoResponse", ...) trước khi gọi StartAsync(), sau đó dùng InvokeAsync("Echo", ...) để gửi thông điệp.   
        ![alt text](image-2.png) 
        ![alt text](image-3.png)
    Bước 5 — Phát rộng (All) và Nhóm (Group): Mở rộng PingHub với 3 phương thức Broadcast, JoinGroup và SendToGroup. Chạy đồng thời hai máy khách (An và Binh) để kiểm chứng việc chia sẻ tin nhắn nhóm/toàn cục mà không lộ tin nhắn riêng Echo.  
        ![alt text](image-4.png)
        ![alt text](image-5.png)
    Bước 6 — Vòng đời kết nối và tự động nối lại: Ghi đè OnConnectedAsync và OnDisconnectedAsync trên Hub để theo dõi trạng thái kết nối. Đăng ký các sự kiện Reconnecting, Reconnected và Closed trên máy khách để kiểm chứng cơ chế WithAutomaticReconnect() tự phục hồi kết nối  khi máy chủ tắt và bật lại.  
        ![alt text](image-6.png)
    Bước 7 — Thí nghiệm 3 lỗi điển hình: Cố ý tạo 3 tình huống lỗi để phân biệt lỗi ném ngoại lệ ngay lập tức với lỗi im lặng ở chiều đẩy dữ liệu xuống.  
    Phần 2: Áp dụng: EduSubmit nhận thông báo trạng thái thời gian thực
    Bước 1 — Khởi tạo Solution: Tạo solution RealtimeSubmissionSystem gồm máy chủ Submission.RealtimeApi (template Web) và máy khách Submission.RealtimeClient.   
        ![alt text](image.png)
        ![alt text](image-1.png)
    Bước 2 — Nền tảng REST API và đồng bộ luồng (lock): Triển khai các endpoint REST, bổ sung trường thời gian UpdatedAt và dùng khối lock (submissionsLock) để tránh xung đột dữ liệu khi nhiều luồng vừa đọc vừa ghi danh sách lượt nộp.  
    Bước 3 — Thiết kế SubmissionHub: Xây dựng lớp SubmissionHub gọn nhẹ chỉ chứa phương thức WatchSubmission(string submissionId) để đưa kết nối của máy khách vào nhóm submission:{id}.   
    Bước 4 — Chấm bài nền và đẩy tin qua IHubContext: Tiêm IHubContext<SubmissionHub> vào endpoint POST để kích hoạt hàm chạy ngầm SimulateGradingAsync. Tác vụ nền giả lập thời gian chấm, cập nhật trạng thái trong lock trước, sau đó đẩy thông điệp SubmissionStatusChanged chứa SubmissionStatusMessage về đúng nhóm.   
    Bước 5 & 6 — Lập trình máy khách kết hợp 2 kênh: Máy khách mở đồng thời kênh REST và SignalR, dùng TaskCompletionSource làm "chuông báo" đợi trạng thái graded. Luồng chạy tuân thủ nghiêm ngặt thứ tự: Mở kết nối SignalR --> Gọi POST tạo bài nộp để lấy Id --> Gọi WatchSubmission vào nhóm --> Chờ thông báo đẩy --> Gọi lại GET bằng REST để chốt trạng thái cuối cùng.   
        ![alt text](image.png)
    Bước 7 — Thực hiện 5 thí nghiệm phân tán: Kiểm chứng hệ thống trong 5 kịch bản để khẳng định nguyên tắc: "Push để nhanh, REST để chắc".   Bước 8 — Kiểm thử tự động và nộp bài: Tạo tệp requests.http gồm 9 kịch bản kiểm thử các mã trạng thái HTTP và bước bắt tay negotiate, hoàn thiện báo cáo README.md và tạo Pull Request nộp bài.   