# Bai2 - RestfulClientServer (EduSubmit)

Sinh viên: **\<Họ tên - MSSV\>** *(điền vào đây)*

Hệ phân tán thu nhỏ gồm 2 chương trình, 2 tiến trình, nói chuyện với nhau chỉ qua HTTP + JSON:

- `src/Submission.Api` — máy chủ Minimal API, cổng **5000**
- `src/Submission.Client` — máy khách console dùng `HttpClient`
- `requests.http` — bộ ca test (REST Client extension)

## Cách chạy

```powershell
# Terminal 1 - máy chủ
dotnet run --project src/Submission.Api --urls http://localhost:5000

# Terminal 2 - máy khách
dotnet run --project src/Submission.Client
```

Test bằng curl (gõ trong thư mục gốc RestfulClientServer):

```powershell
curl.exe -i http://localhost:5000/api/assignments
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@rong.json"
```

Hoặc mở `requests.http` trong VS Code, bấm **Send Request** ở từng yêu cầu.

## Hợp đồng API

| Phương thức | URI | Thành công | Lỗi có thể gặp |
|---|---|---|---|
| GET | /api/assignments | 200 danh sách | – |
| GET | /api/assignments/{assignmentId} | 200 một bài tập | 404 ASSIGNMENT_NOT_FOUND |
| POST | /api/assignments/{assignmentId}/submissions | 201 + Location | 404 ASSIGNMENT_NOT_FOUND · 400 ASSIGNMENT_CLOSED · 400 INVALID_SUBMISSION |
| GET | /api/submissions/{submissionId} | 200 lượt nộp | 404 SUBMISSION_NOT_FOUND |

Mọi lỗi do handler trả cùng dạng `{ "code": "...", "message": "..." }` (ApiError).

## Nhật ký kết quả thật

*(chèn ảnh chụp terminal của bạn vào đây — mỗi ảnh một mục)*

- GET /api/assignments → 200 OK + mảng 2 bài tập: **(chèn ảnh)**
- GET /api/assignments/999 → 404 + ASSIGNMENT_NOT_FOUND: **(chèn ảnh)**
- POST bài 1, sv001.json → 201 Created + Location: **(chèn ảnh)**
- POST bài 1, rong.json → 400 + INVALID_SUBMISSION: **(chèn ảnh)**
- POST bài 999 → 404 ASSIGNMENT_NOT_FOUND: **(chèn ảnh)**
- PUT /api/assignments/1 → 405 Method Not Allowed (framework trả): **(chèn ảnh)**
- Máy khách in đủ 4 nấc, không sập khi gặp 400: **(chèn ảnh)**

## Bảng phá thử (8 ca bước 5)

| Ca | Yêu cầu | Mã mong đợi | Mã nhận được | Ai trả |
|---|---|---|---|---|
| 5a | POST bài 1, body sv001.json | 201 + Location | 201 | Handler |
| 5b | POST bài 1, body rong.json | 400 INVALID_SUBMISSION | 400 | Handler |
| 5c | POST bài 999 | 404 ASSIGNMENT_NOT_FOUND | 404 | Handler |
| 5d | Bỏ header Content-Type | 415 | 415 | Framework |
| 5e | Body JSON hỏng | 400 (body văn bản) | 400 | Framework |
| 5f | Body thiếu repositoryUrl | 400 INVALID_SUBMISSION | 400 | Handler |
| 5g | PUT /api/assignments/1 | 405, Allow: GET | 405 | Framework |
| 5h | POST vào bài tập đã đóng | 400 ASSIGNMENT_CLOSED | 400 | Handler |

## Ba câu hỏi giải thích

**1. Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?**

URI chỉ định danh **tài nguyên** ("thứ" mà API quản lý): `/api/assignments` là tập bài tập, `/api/assignments/1/submissions` là các lượt nộp của bài tập 1. Hành động là phần biến đổi: GET đọc, POST tạo — nên `POST /api/assignments/1/submissions` tự nói lên "tạo một lượt nộp cho bài tập 1" mà không cần động từ `doSubmit` trong URI. Nhờ đó client đoán đúng cách dùng chỉ từ URI + phương thức + mã trạng thái.

**2. "Stateless" thể hiện ở đâu trong code đã viết?**

Mỗi yêu cầu tự mang đủ thông tin: `assignmentId` nằm trên URI, `studentCode` và `repositoryUrl` nằm trong body JSON. Máy chủ không nhớ "cuộc hội thoại" giữa hai yêu cầu — handler POST đọc mọi thứ nó cần từ chính yêu cầu đó, nên client gọi từ tiến trình nào, lần nào cũng được xử lý như yêu cầu đầu tiên. (Stateless không có nghĩa không lưu dữ liệu: bài tập và lượt nộp vẫn lưu trong `List`, chỉ là không lưu phiên.)

**3. Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?**

DTO là hợp đồng ở dạng JSON, không phải lớp dùng chung. Hai tiến trình không tham chiếu được code của nhau, chỉ khớp nhau qua **tên trường và kiểu dữ liệu trong JSON**: `record SubmissionDto(Guid Id, ...)` bên máy chủ sinh ra `{"id": "...", ...}`, và bản khai báo cùng tên bên máy_client đọc đúng các trường đó. Server sửa bên trong (thêm `GraderNote`) client không vỡ, miễn là các trường hợp đồng giữ nguyên.

## Giới hạn hiện tại (chủ ý, để phát triển ở các bài sau)

- Dữ liệu lưu trong `List` trong bộ nhớ → tắt máy chủ là mất (Bài sau: CSDL/EF Core)
- Trạng thái mãi là "received" → chưa có ai chấm (Bài 3: SignalR)
- Chưa xác thực → ai cũng nộp hộ được SV001 (mở rộng: JWT)
