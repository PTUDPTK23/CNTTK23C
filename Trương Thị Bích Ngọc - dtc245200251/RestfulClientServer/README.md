# EduSubmit - Hệ Thống Nộp Bài Phân Tán (RESTful API)

## 1. Thông tin sinh viên
- **Họ và tên**: Trương Thị Bích Ngọc
- **Mã sinh viên**: DTC245200251
- **Học phần**: Phát triển ứng dụng phân tán - K23C

---

## 2. Cấu trúc Solution
```text
RestfulClientServer/
├── RestfulClientServer.slnx
├── .gitignore
├── requests.http
├── sv001.json
├── rong.json
├── README.md
└── src/
    ├── Submission.Api/
    │   ├── Program.cs
    │   └── Submission.Api.csproj
    └── Submission.Client/
        ├── Program.cs
        └── Submission.Client.csproj
```

---

## 3. Hợp đồng API (API Contract)
| Phương thức | URI | Mã thành công | Lỗi có thể gặp | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/assignments` | 200 OK | — | Lấy danh sách toàn bộ bài tập |
| `GET` | `/api/assignments/{id}` | 200 OK | 404 ASSIGNMENT_NOT_FOUND | Lấy chi tiết một bài tập theo Id |
| `POST` | `/api/assignments/{id}/submissions` | 201 Created + Location | 404 ASSIGNMENT_NOT_FOUND<br>400 ASSIGNMENT_CLOSED<br>400 INVALID_SUBMISSION<br>409 DUPLICATE_SUBMISSION | Nộp bài cho bài tập (tài nguyên con) |
| `GET` | `/api/submissions/{id}` | 200 OK | 404 SUBMISSION_NOT_FOUND | Đọc lại chi tiết lượt nộp qua Location |
| `DELETE` | `/api/submissions/{id}` | 204 No Content | 404 SUBMISSION_NOT_FOUND | Hủy/xóa lượt nộp bài |

---

## 4. Hướng dẫn chạy chương trình

### Khởi động Máy chủ (Submission.Api)
```powershell
cd RestfulClientServer
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

### Chạy Máy khách (Submission.Client)
Mở một cửa sổ Terminal mới:
```powershell
cd RestfulClientServer
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Kết quả thực thi 4 nấc của máy khách:
- **Nấc 1**: Liệt kê danh sách bài tập đang mở.
- **Nấc 2**: Gửi yêu cầu nộp bài, kiểm tra mã `201 Created` trước khi đọc body, in `Location`.
- **Nấc 3**: Đọc lại chi tiết bài vừa nộp từ URL trong `Location`.
- **Nấc 4**: Cố ý gửi dữ liệu sai để kiểm tra xử lý mã lỗi `400 BadRequest` và đọc `ApiError` an toàn mà không làm sập ứng dụng.

---

## 5. Trả lời 3 câu hỏi giải thích (Slide 90, 116)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời**: Trong kiến trúc REST, URI đóng vai trò định danh **tài nguyên** (Resource - là các thực thể dữ liệu như `assignments`, `submissions`). Hành động thao tác trên tài nguyên đó được quy định rõ ràng bởi các **phương thức HTTP chuẩn** (`GET` để lấy, `POST` để tạo mới, `PUT`/`PATCH` để sửa, `DELETE` để xóa).
- **Ví dụ từ code**: Thay vì dùng kiểu gọi hàm `POST /submitAssignment` hay `/api/createSubmission`, hệ thống dùng `POST /api/assignments/1/submissions`. Nhìn vào URI ta biết ngay đối tượng thao tác là tài nguyên con `submissions` thuộc bài tập `1`, còn hành động tạo mới được biểu đạt bằng động từ `POST`.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Trả lời**: Tính vô trạng thái (Stateless) có nghĩa là máy chủ không lưu giữ bất kỳ ngữ cảnh hội thoại (session) nào giữa các lần gọi của client. Mỗi yêu cầu HTTP gửi lên đều phải tự mang đầy đủ mọi thông tin cần thiết để server có thể hiểu và xử lý độc lập.
- **Ví dụ từ code**: Khi sinh viên nộp bài qua `POST /api/assignments/1/submissions`, thông tin định danh bài tập được mang trực tiếp trên URI (`assignmentId = 1`), dữ liệu người nộp và link kho mã nguồn được mang trọn vẹn trong body JSON (`studentCode`, `repositoryUrl`). Server không cần nhớ trước đó client đã "chọn" bài tập nào. Nhờ đó, bất kỳ instance server nào nhận yêu cầu cũng xử lý được như nhau.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời**: Hai tiến trình chạy độc lập trong hệ phân tán không chia sẻ chung bộ nhớ hay mã nguồn C#. Biên giới giao tiếp duy nhất giữa chúng là **hợp đồng thông điệp JSON** qua giao thức HTTP.
- **Ví dụ từ code**: Cả `Submission.Api` và `Submission.Client` đều khai báo riêng `record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt)`. Dù là hai kiểu dữ liệu khác nhau trong bộ nhớ của hai tiến trình, nhưng khi tuần tự hóa (serialize) sang chuỗi JSON hoặc giải tuần tự hóa (deserialize) từ JSON, chúng khớp hoàn toàn về tên thuộc tính (theo chuẩn camelCase) và kiểu dữ liệu. Điều này giúp hai bên tiến hóa độc lập mà không bị phụ thuộc vào mã nhị phân hay reference assembly của nhau.
