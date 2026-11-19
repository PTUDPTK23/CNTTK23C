# Bài thực hành số 2: Thiết kế và lập trình RESTful API (EduSubmit)

## 1. Cấu trúc giải pháp
- `src/Submission.Api`: Máy chủ Web API (.NET 10 Minimal APIs) chạy tại `http://localhost:5000`.
- `src/Submission.Client`: Ứng dụng Console Client sử dụng `HttpClient` thực hiện chuỗi gọi API 4 bước.
- `requests.http`: Bộ 7 ca kiểm thử HTTP bao phủ các mã trạng thái `200`, `201`, `400`, `404`.

## 2. Hướng dẫn chạy chương trình
1. **Khởi chạy máy chủ (Terminal 1):**
   `dotnet run --project src/Submission.Api --urls http://localhost:5000`
2. **Chạy máy khách (Terminal 2):**
   `dotnet run --project src/Submission.Client -- http://localhost:5000`

## 3. Bảng kết quả kiểm thử API (`requests.http`)
| STT | Ca kiểm thử | Phương thức & URI | Mã trạng thái | Kết quả / Mã lỗi |
| :-: | :--- | :--- | :-: | :--- |
| 1 | Lấy danh sách bài tập | `GET /api/assignments` | `200 OK` | Trả về mảng JSON 3 bài tập |
| 2 | Xem chi tiết bài tập tồn tại | `GET /api/assignments/1` | `200 OK` | Trả về chi tiết bài tập Id = 1 |
| 3 | Xem chi tiết bài tập không tồn tại | `GET /api/assignments/999` | `404 Not Found` | `ASSIGNMENT_NOT_FOUND` |
| 4 | Nộp bài hợp lệ cho bài tập đang mở | `POST /api/assignments/1/submissions` | `201 Created` | Trả về DTO + header `Location` |
| 5 | Đọc lại lượt nộp vừa tạo qua `Location` | `GET /api/submissions/{id}` | `200 OK` | Trả về đúng lượt nộp vừa tạo |
| 6 | Nộp bài thiếu mã SV hoặc URL | `POST /api/assignments/1/submissions` | `400 Bad Request` | `INVALID_SUBMISSION` |
| 7 | Nộp bài vào bài tập đã hết hạn (`IsOpen = false`) | `POST /api/assignments/3/submissions` | `400 Bad Request` | `ASSIGNMENT_CLOSED` |

## 4. Trả lời câu hỏi thảo luận
1. **Vì sao khi tạo lượt nộp mới máy chủ trả về `201 Created` kèm header `Location` thay vì `200 OK`?**
   - Theo chuẩn HTTP/REST, `201 Created` khẳng định một tài nguyên mới (lượt nộp bài) đã được khởi tạo thành công trên máy chủ, phân biệt rõ với `200 OK` (chỉ đọc hoặc cập nhật tài nguyên có sẵn). Header `Location` cung cấp chính xác URI định danh (`/api/submissions/{id}`) để máy khách có thể truy xuất lại tài nguyên vừa tạo mà không cần tự ghép chuỗi URL.
2. **Sự khác nhau giữa mã lỗi `400 Bad Request` và `404 Not Found` trong quy trình nộp bài?**
   - `404 Not Found` xảy ra ở cổng kiểm tra định danh tài nguyên: bản thân đối tượng trên URI không tồn tại trên hệ thống (ví dụ bài tập `999` hoặc lượt nộp không có thật).
   - `400 Bad Request` xảy ra khi tài nguyên đích có tồn tại nhưng yêu cầu của máy khách vi phạm quy tắc nghiệp vụ hoặc dữ liệu đầu vào không hợp lệ (ví dụ nộp vào bài tập đã đóng `ASSIGNMENT_CLOSED`, hoặc bỏ trống trường bắt buộc `INVALID_SUBMISSION`).
3. **Vì sao máy khách phải kiểm tra `StatusCode` trước khi giải tuần tự hóa (deserialize) JSON sang `SubmissionDto`?**
   - Khi máy chủ trả về lỗi (`400` hoặc `404`), phần thân (body) của phản hồi mang cấu trúc của `ApiError` (`Code`, `Message`) chứ không phải `SubmissionDto`. Nếu máy khách gọi thẳng `GetFromJsonAsync<SubmissionDto>()` mà không kiểm tra `StatusCode`, `HttpClient` sẽ ném ngoại lệ `HttpRequestException` làm sập chương trình và bỏ lỡ thông báo lỗi nghiệp vụ chi tiết từ máy chủ.