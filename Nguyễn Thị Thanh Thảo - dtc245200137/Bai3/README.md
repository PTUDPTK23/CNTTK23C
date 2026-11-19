# Bài thực hành 3: Giao tiếp thời gian thực với SignalR

## 1. Cấu trúc thư mục
- `PingSignalR/`: Solution 1 minh họa các kỹ thuật SignalR cơ bản (`Echo`, `Broadcast`, `Group`, vòng đời kết nối, `WithAutomaticReconnect`) chạy tại cổng `5200`[cite: 30].
- `RealtimeSubmissionSystem/`: Solution 2 ứng dụng EduSubmit thời gian thực kết hợp REST API và SignalR Hub (`/hubs/submissions`) chạy tại cổng `5000`, kèm tệp kiểm thử `requests.http` (9 ca)[cite: 56, 70, 72].
- `ChatRoomSignalR/`: Ứng dụng mở rộng phòng chat đa người dùng hỗ trợ chat nhóm, `/who`, `/pm`, `/join`[cite: 3].
- `minh chung bai3.docx`: Tài liệu tổng hợp hình ảnh minh chứng quá trình chạy thực tế[cite: 8].

---

## 2. Trả lời 3 câu hỏi đầu giờ (Slide 3 & Slide 65)

1. **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?**
   - **Có**, nếu máy khách mở sẵn một kết nối hai chiều lâu dài tới máy chủ (WebSocket thông qua thư viện SignalR)[cite: 74]. Máy chủ chủ động đẩy thông điệp qua `Clients.Group(...).SendAsync(...)` (hoặc dùng `IHubContext<THub>` khi gửi từ bên ngoài Hub như tác vụ chấm nền)[cite: 74].

2. **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?**
   - **REST** dùng để khởi tạo và đọc trạng thái tài nguyên chuẩn xác với mã trạng thái HTTP rõ ràng (`POST` tạo lượt nộp trả về `201`, `GET` đọc lại trạng thái hiện tại là nguồn sự thật)[cite: 25, 74].
   - **SignalR** bổ sung chiều máy chủ chủ động đẩy thông báo ngay khi trạng thái thay đổi (`received` $\rightarrow$ `processing` $\rightarrow$ `graded`), giúp loại bỏ việc hỏi lặp lại (polling) gây tốn băng thông và độ trễ cao[cite: 15, 25, 74]. SignalR không thay thế REST mà phối hợp với REST[cite: 25, 74].

3. **Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" (`graded`) thì sao?**
   - Thông điệp SignalR gửi trong lúc mất mạng sẽ bị mất (SignalR không lưu hàng đợi tin nhắn)[cite: 48, 74]. Khi có mạng lại, `WithAutomaticReconnect()` sẽ tự kết nối lại với một `ConnectionId` mới; máy khách cần đăng ký lại nhóm theo dõi (`WatchSubmission`) và gọi lại `GET /api/submissions/{id}` qua REST để lấy trạng thái cuối cùng chính xác nhất[cite: 74].

---

## 3. Kết quả chạy thực tế máy khách `Submission.RealtimeClient` (Bước 6)

```text
Da ket noi SignalR.
Chon bai tap: 1 Bai tap SignalR
Da tao luot nop: 820aff42-1170-4433-b86d-5d88b9ef1769
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 820aff42-1170-4433-b86d-5d88b9ef1769 -> processing luc 15:31:17
Thong bao SignalR: 820aff42-1170-4433-b86d-5d88b9ef1769 -> graded luc 15:31:19
Trang thai doc lai bang REST: graded