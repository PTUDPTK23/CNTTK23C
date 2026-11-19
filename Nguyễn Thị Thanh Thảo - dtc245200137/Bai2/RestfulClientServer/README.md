# Bài 2: RESTful API cho tương tác Client - Server (EduSubmit)

## 1. Các lệnh đã chạy
- Khởi động máy chủ API (Terminal 1):
  `dotnet run --project src/Submission.Api --urls http://localhost:5000`
- Chạy máy khách Console (Terminal 2):
  `dotnet run --project src/Submission.Client -- http://localhost:5000`

## 2. Bảng kết quả kiểm thử (Phá thử)
| Ca test | Yêu cầu | Mã mong đợi | Kết quả thực tế |
| :--- | :--- | :--- | :--- |
| 1 | `GET /api/assignments` | `200 OK` | `200 OK` + Danh sách 3 bài tập |
| 2 | `GET /api/assignments/1` | `200 OK` | `200 OK` + Chi tiết bài tập 1 |
| 3 | `GET /api/assignments/999` | `404 Not Found` | `404 Not Found` + `ASSIGNMENT_NOT_FOUND` |
| 4 | `POST /api/assignments/1/submissions` (hợp lệ) | `201 Created` | `201 Created` + Header `Location` |
| 5 | `GET /api/submissions/{id}` (đọc lại theo Location) | `200 OK` | `200 OK` + Thông tin lượt nộp |
| 6 | `POST /api/assignments/1/submissions` (thiếu dữ liệu) | `400 Bad Request` | `400 Bad Request` + `INVALID_SUBMISSION` |
| 7 | `POST /api/assignments/3/submissions` (bài đã đóng) | `400 Bad Request` | `400 Bad Request` + `ASSIGNMENT_CLOSED` |

## 3. Trả lời 3 câu hỏi ôn tập
1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
   - URI dùng để định danh tài nguyên nghiệp vụ (ví dụ: `/api/assignments`, `/api/submissions`), còn phương thức HTTP (`GET`, `POST`, `PUT`, `DELETE`) thể hiện hành động tác động lên tài nguyên đó (như `GET` để đọc, `POST` để tạo mới) giúp giao diện API thống nhất và dễ đoán.
2. **"Stateless" thể hiện ở đâu trong code đã viết?**
   - Mỗi yêu cầu HTTP từ client gửi lên đều tự mang đầy đủ thông tin để server xử lý ngay lập tức (như `assignmentId` nằm trên URI, `studentCode` và `repositoryUrl` nằm trong body JSON). Máy chủ không lưu trạng thái phiên làm việc của sinh viên giữa các lần gọi.
3. **Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
   - Hai tiến trình giao tiếp qua mạng bằng thông điệp JSON chứ không dùng chung bộ nhớ hay tham chiếu trực tiếp mã nguồn. Chỉ cần tên thuộc tính và kiểu dữ liệu của hai bên khớp nhau theo hợp đồng JSON thì hệ thống sẽ tự động chuyển đổi (serialize/deserialize) chính xác.