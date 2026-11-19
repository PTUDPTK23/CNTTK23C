# Bài 2 - EduSubmit RESTful API

- Họ tên: Hoàng Anh Tú
- MSSV: DTC245200321
- Công nghệ: .NET 10, ASP.NET Core Minimal API, HttpClient.

## 1. Mô tả

Ứng dụng gồm hai chương trình:

- Submission.Api: quản lý bài tập và tiếp nhận lượt nộp.
- Submission.Client: liệt kê bài tập đang mở, nộp bài,
  đọc lại theo Location và thử gửi dữ liệu không hợp lệ.

Dữ liệu được lưu trong bộ nhớ và mất khi API khởi động lại.

## 2. Cách chạy

Chạy các lệnh từ thư mục RestfulClientServer.

### Biên dịch solution

```powershell
dotnet build
```

### Terminal 1: chạy API

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

### Terminal 2: chạy Client

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```

### Kiểm thử bằng REST Client

Mở requests.http trong VS Code và bấm Send Request.

Chạy request 4 trước request 5 để lấy ID lượt nộp vừa tạo.
Giữ API chạy giữa hai request.

## 3. Hợp đồng API

| Phương thức | URI                                         | Kết quả           |
| ----------- | ------------------------------------------- | ----------------- |
| GET         | /api/assignments                            | 200               |
| GET         | /api/assignments/{assignmentId}             | 200 hoặc 404      |
| POST        | /api/assignments/{assignmentId}/submissions | 201, 400 hoặc 404 |
| GET         | /api/submissions/{submissionId}             | 200 hoặc 404      |

Khi tạo lượt nộp thành công, API trả 201 Created,
dữ liệu lượt nộp và header Location.

Các lỗi nghiệp vụ có dạng:

```json
{
  "code": "INVALID_SUBMISSION",
  "message": "Thieu ma sinh vien hoac kho ma nguon."
}
```

## 4. Bảng kiểm thử

Chỉ điền kết quả thực tế sau khi đã chạy từng ca.

| Ca thử                     | Mong đợi                 | Thực tế         |
| -------------------------- | ------------------------ | --------------- |
| GET danh sách bài tập      | 200                      | Chưa ghi        |
| GET bài tập 1              | 200                      | Chưa ghi        |
| GET bài tập 999            | 404 ASSIGNMENT_NOT_FOUND | Chưa ghi        |
| POST dữ liệu hợp lệ        | 201 và Location          | Chưa ghi        |
| GET lượt nộp vừa tạo       | 200, đúng ID             | Chưa ghi        |
| POST dữ liệu rỗng          | 400 INVALID_SUBMISSION   | Chưa ghi        |
| GET lượt nộp không tồn tại | 404 SUBMISSION_NOT_FOUND | Chưa ghi        |
| POST vào bài tập 999       | 404 ASSIGNMENT_NOT_FOUND | Chưa ghi        |
| POST vào bài tập 3 đã đóng | 400 ASSIGNMENT_CLOSED    | Chưa ghi        |
| POST thiếu repositoryUrl   | 400 INVALID_SUBMISSION   | Chưa ghi        |
| POST thiếu Content-Type    | 415                      | 415             |
| POST JSON sai cú pháp      | 400                      | 400             |
| PUT vào /api/assignments/1 | 405, Allow: GET          | 405, Allow: GET |

Các lỗi nghiệp vụ do handler trả có body ApiError.
Lỗi do framework như 415, JSON hỏng và 405 có thể
trả văn bản hoặc body rỗng.

## 5. Trả lời câu hỏi

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

URI xác định tài nguyên, còn phương thức HTTP diễn tả thao tác
trên tài nguyên đó.

Ví dụ:

- GET /api/assignments: đọc danh sách bài tập.
- POST /api/assignments/1/submissions: tạo lượt nộp cho bài tập 1.

### Câu 2: Stateless thể hiện ở đâu trong code?

Mỗi yêu cầu tự mang đủ thông tin để API xử lý.

Khi nộp bài, assignmentId nằm trên URI; studentCode và
repositoryUrl nằm trong body. API không cần nhớ phiên làm việc
của client từ yêu cầu trước.

Stateless không có nghĩa là server không được lưu dữ liệu.
Các danh sách assignments và submissions vẫn lưu dữ liệu ứng dụng.

### Câu 3: Vì sao client và server khai báo DTO riêng mà vẫn khớp nhau?

Hai chương trình trao đổi JSON qua HTTP.
Chúng khớp nhau nhờ cấu trúc JSON, tên trường và kiểu dữ liệu
tương thích, không cần tham chiếu project của nhau.

Ví dụ, JSON có studentCode và repositoryUrl được ánh xạ
vào CreateSubmissionRequest ở phía API.
