# Bài 3: Giao tiếp thời gian thực với ASP.NET Core SignalR

## 1. Trả lời 3 câu hỏi đầu giờ
1. **Tại sao không dùng REST (HTTP polling) liên tục để chờ chấm bài?**
   - Hỏi liên tục (polling) gây lãng phí băng thông, tăng tải cho máy chủ khi có nhiều sinh viên cùng nộp bài và luôn có độ trễ giữa hai lần hỏi. SignalR giữ kết nối hai chiều để máy chủ chủ động đẩy trạng thái ngay khi có thay đổi.
2. **Tại sao cần định nghĩa nhóm theo từng lượt nộp (`submission:{id}`)?**
   - Để cô lập phạm vi nhận tin: máy chủ chỉ đẩy tiến trình chấm bài tới đúng sinh viên đang theo dõi lượt nộp đó, tránh gửi nhầm thông báo sang các sinh viên khác hoặc phát tán toàn hệ thống (`Clients.All`).
3. **Tại sao vẫn phải giữ các endpoint REST và đọc lại `GET /api/submissions/{id}` ở cuối?**
   - Thông điệp thời gian thực (SignalR) có thể bị lỡ nếu máy khách tham gia nhóm muộn hoặc mất mạng thoáng qua. REST đóng vai trò là **nguồn sự thật (source of truth)** lưu trạng thái bền vững để khởi tạo lượt nộp và chốt kết quả cuối cùng chính xác nhất.

---

## 2. Kết quả chạy máy khách (8 dòng - Bước 6)
text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap 1: REST API co ban
Da tao luot nop: 83b9203d-ef53-4eef-ac8b-b5a85b8ff60a
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 83b9203d-ef53-4eef-ac8b-b5a85b8ff60a -> processing luc 08:46:45
Thong bao SignalR: 83b9203d-ef53-4eef-ac8b-b5a85b8ff60a -> graded luc 08:46:47
Trang thai doc lai bang REST: graded


---

## 3. Nhật ký 5 thí nghiệm (Bước 7)

| STT | Thí nghiệm | Dự đoán | Kết quả thật | Giải thích |
|---|---|---|---|---|
| 1 | Chạy 2 máy khách `SV001` và `SV002` cùng lúc | Mỗi máy chỉ nhận thông báo của lượt nộp mình tạo | Mỗi máy khách chỉ thấy lượt nộp vừa tạo | Nhóm `submission:{id}` tách biệt người nhận |
| 2 | Chạy máy khách khi máy chủ chưa chạy | Báo lỗi không kết nối được tới cổng 5000 | `HttpRequestException: Connection refused (localhost:5000)` | `WithAutomaticReconnect()` chỉ tự thử lại sau khi lần kết nối đầu đã thành công |
| 3 | Sửa `WithUrl` thành `/hub/submissions` | Lỗi 404 khi bắt tay SignalR | `HttpRequestException: ... 404 (Not Found)` | Lỗi ở bước negotiate do không khớp đường dẫn `MapHub("/hubs/submissions")` |
| 4 | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không báo lỗi nhưng không nhận được thông báo | Không lỗi; 10 giây sau in `Qua thoi gian cho thong bao graded.` | Sai tên sự kiện chiều xuống khiến SignalR bỏ qua trong im lặng |
| 5 | Chờ 4 giây rồi mới gọi `WatchSubmission` | Lỡ thông báo SignalR nhưng REST vẫn đọc đúng | Không nhận tin nào; `Trang thai doc lai bang REST: graded` | Vào nhóm muộn sau khi server đã đẩy tin thì bị lỡ; REST là nguồn sự thật |