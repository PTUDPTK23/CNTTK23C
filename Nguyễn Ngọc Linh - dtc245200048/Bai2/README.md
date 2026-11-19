# Bài 2: RESTful API cho tương tác Client - Server (EduSubmit)

## 1. Lệnh khởi chạy hệ thống
- **Chạy máy chủ API (cổng 5000):**
  `dotnet run --project src/Submission.Api --urls http://localhost:5000`
- **Chạy máy khách Console:**
  `dotnet run --project src/Submission.Client -- http://localhost:5000`

## 2. Bảng kết quả kiểm thử (requests.http)
| Ca test | Phương thức & URI | Mã mong đợi | Kết quả thực tế |
| :--- | :--- | :--- | :--- |
| 1. Danh sách bài tập | `GET /api/assignments` | 200 OK | 200 OK (mảng 3 bài tập) |
| 2. Chi tiết bài tập 1 | `GET /api/assignments/1` | 200 OK | 200 OK (chi tiết bài tập 1) |
| 3. Bài tập không tồn tại | `GET /api/assignments/999` | 404 Not Found | 404 + `ASSIGNMENT_NOT_FOUND` |
| 4. Nộp bài hợp lệ | `POST /api/assignments/1/submissions` | 201 Created | 201 Created + header `Location` |
| 5. Đọc lại lượt nộp | `GET /api/submissions/{id}` | 200 OK | 200 OK (đúng lượt nộp vừa tạo) |
| 6. Nộp bài thiếu dữ liệu | `POST /api/assignments/1/submissions` | 400 Bad Request | 400 + `INVALID_SUBMISSION` |
| 7. Nộp bài tập đã đóng | `POST /api/assignments/3/submissions` | 400 Bad Request | 400 + `ASSIGNMENT_CLOSED` |

## 3. Trả lời câu hỏi giải thích

**Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**
- **Trả lời:** Trong kiến trúc REST, URI dùng để định danh tài nguyên (các đối tượng nghiệp vụ như `assignments`, `submissions`), còn hành động thao tác trên tài nguyên đó do các phương thức HTTP chuẩn đảm nhận (`GET` để đọc, `POST` để tạo mới).
- **Ví dụ trong code:** Dùng `POST /api/assignments/1/submissions` thay vì đặt động từ vào đường dẫn như `/submitAssignment`. Nhìn vào URI và phương thức, client dễ dàng đoán được ngữ nghĩa giao tiếp mà không cần đọc sâu tài liệu.

**Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?**
- **Trả lời:** Tính chất Stateless (phi trạng thái) thể hiện ở việc máy chủ `Submission.Api` không lưu trữ phiên làm việc (session) của client giữa các lần gọi. Mỗi yêu cầu HTTP từ client gửi lên phải tự mang đầy đủ mọi thông tin cần thiết để server hiểu và xử lý độc lập.
- **Ví dụ trong code:** Khi nộp bài qua `POST /api/assignments/{assignmentId}/submissions`, client gửi kèm `assignmentId` ngay trên URI cùng với `StudentCode` và `RepositoryUrl` trong JSON body. Khi đọc lại qua `GET /api/submissions/{submissionId}`, client truyền trực tiếp `submissionId` lên đường dẫn.

**Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**
- **Trả lời:** DTO (`AssignmentDto`, `CreateSubmissionRequest`, `SubmissionDto`, `ApiError`) đóng vai trò là bản hợp đồng dữ liệu ở biên giao tiếp qua định dạng JSON chứ không phải các lớp dùng chung trong cùng một bộ nhớ. Hai tiến trình `Submission.Api` và `Submission.Client` khớp nhau nhờ thống nhất tên trường (tự động chuyển đổi sang chuẩn camelCase trong JSON) và kiểu dữ liệu tương ứng, giúp hai bên phát triển độc lập mà không cần tham chiếu trực tiếp project của nhau.