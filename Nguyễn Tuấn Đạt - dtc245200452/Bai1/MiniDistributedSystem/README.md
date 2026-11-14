# MiniDistributedSystem – Lát cắt 1 của EduSubmit (Bài 1, Bước 9–12)

Hệ thống gồm hai tiến trình độc lập giao tiếp qua HTTP/JSON:
- **`Submission.Api`**: Máy chủ Minimal API cung cấp `GET /`, `GET /api/assignments`, `POST /api/submissions` và endpoint mở rộng `GET /api/submissions/{id:guid}` (trả `200 OK` hoặc `404 Not Found`).
- **`Submission.Client`**: Máy khách Console sử dụng `HttpClient` đọc địa chỉ API từ tham số dòng lệnh, gọi lần lượt lấy danh sách bài tập, nộp bài và đọc lại lượt nộp vừa tạo.

## Chạy thử trên 2 terminal

Terminal 1 (Máy chủ):

```powershell
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2 (Máy khách):

```powershell
dotnet run --project src/Submission.Client -- http://localhost:5000
```
