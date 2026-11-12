# BÁO CÁO THỰC HÀNH BÀI 2: XÂY DỰNG RESTFUL API CHO TƯƠNG TÁC MÁY KHÁCH - MÁY CHỦ

- **Môn học:** Phát triển ứng dụng phân tán
- **Giảng viên hướng dẫn:** TS. Dương Thu Mây
- **Sinh viên thực hiện:** Nguyễn Tuấn Đạt
- **Mã sinh viên:** DTC245200452
- **Lớp:** CNTT K23C - Trường ĐH Công nghệ Thông tin & Truyền thông (ICTU)

---

## 1. Nhật ký lệnh thực hiện

```powershell
# 1. Khởi tạo Solution và các project
mkdir RestfulClientServer
cd RestfulClientServer
dotnet new sln -n RestfulClientServer
dotnet new web -n Submission.Api -o src/Submission.Api
dotnet new console -n Submission.Client -o src/Submission.Client
dotnet sln add src/Submission.Api/Submission.Api.csproj
dotnet sln add src/Submission.Client/Submission.Client.csproj
dotnet new gitignore

# 2. Khởi chạy máy chủ API tại cổng 5000 cố định
dotnet run --project src/Submission.Api --urls http://localhost:5000

# 3. Khởi chạy máy khách Console kiểm thử chuỗi 4 nấc
dotnet run --project src/Submission.Client -- http://localhost:5000
```

---

## 2. Kết quả chạy thực tế của máy khách (Console Client Output)

```text
=== KẾT NỐI TỚI MÁY CHỦ: http://localhost:5000 ===

Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client
Da tao luot nop: d8e5f642-000b-4876-920f-0402e6040845
Trang thai ban dau: received
Doc lai luot nop: dtc245200452 - https://github.com/dtc245200452-hub/Bai2
Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Ma sinh vien va link kho ma nguon khong duoc de trong.
```

---

## 3. Bảng phá thử (8 ca kiểm thử theo Slide 81 & 109)

| Ca | Yêu cầu | Mã HTTP mong đợi | Đơn vị trả lời | Ý nghĩa nghiệp vụ |
| :---: | :--- | :---: | :---: | :--- |
| **5a** | `POST /api/assignments/1/submissions` (body hợp lệ) | `201 Created` + Location | Handler | Tạo lượt nộp thành công, cấp URI đọc lại qua header Location. |
| **5b** | `POST /api/assignments/1/submissions` (body rỗng `""`) | `400 Bad Request` | Handler | Báo lỗi `INVALID_SUBMISSION`, máy khách gửi sai dữ liệu. |
| **5c** | `POST /api/assignments/999/submissions` (bài không tồn tại) | `404 Not Found` | Handler | Báo lỗi `ASSIGNMENT_NOT_FOUND`, tài nguyên cha không tồn tại. |
| **5d** | POST bỏ header `Content-Type: application/json` | `415 Unsupported Media Type` | Framework | Máy chủ từ chối vì không biết định dạng body. |
| **5e** | POST body JSON sai cú pháp (JSON hỏng) | `400 Bad Request` | Framework | Lỗi parse JSON của framework trước khi vào handler. |
| **5f** | POST body thiếu trường `repositoryUrl` | `400 Bad Request` | Handler | Báo lỗi `INVALID_SUBMISSION` do giá trị bị null. |
| **5g** | `PUT /api/assignments/1` | `405 Method Not Allowed` | Framework | Đường dẫn tồn tại nhưng phương thức HTTP không được hỗ trợ. |
| **5h** | `POST /api/assignments/3/submissions` (bài tập đã đóng) | `400 Bad Request` | Handler | Báo lỗi `ASSIGNMENT_CLOSED`, bài tập đã hết hạn nộp. |

---

## 4. Trả lời 3 câu hỏi giải thích (Slide 90, 116)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:**
  - Theo chuẩn kiến trúc REST, **URI đại diện cho danh từ/tài nguyên** (Resources) trong hệ thống (như `assignments`, `submissions`). Tài nguyên là thực thể lưu trữ và trao đổi dữ liệu.
  - **Hành động thao tác lên tài nguyên do các phương thức HTTP (HTTP Verbs) đảm nhận**: `GET` (đọc dữ liệu), `POST` (tạo mới tài nguyên), `PUT` (cập nhật toàn bộ), `PATCH` (cập nhật một phần), `DELETE` (xóa).
  - **Ví dụ trong mã nguồn:** Thay vì thiết kế dạng RPC chứa động từ như `/submitAssignment` hay `/doSubmit`, ta dùng:
    `POST /api/assignments/1/submissions`
    Client chỉ cần nhìn URI và Method là biết ngay: *"Thực hiện hành động tạo một lượt nộp mới thuộc bài tập số 1"*.

### Câu 2: "Stateless" (Phi trạng thái) thể hiện ở đâu trong code đã viết?
- **Trả lời:**
  - Nguyên tắc **Stateless** quy định rằng máy chủ **không lưu bất kỳ thông tin phiên (session context) nào của máy khách giữa hai lần gọi**. Mỗi một request gửi lên phải tự chứa đầy đủ thông tin độc lập để máy chủ hiểu và xử lý được.
  - **Thể hiện trong mã nguồn:**
    - Trong endpoint `POST /api/assignments/{assignmentId}/submissions`: Mã bài tập `assignmentId` được truyền trực tiếp trên URI; mã sinh viên `studentCode` và link `repositoryUrl` được truyền đầy đủ trong request body JSON.
    - Máy chủ không cần biết trước đó máy khách đã làm gì, không cần duy trì session/cookie; nó chỉ cần đọc dữ liệu có sẵn trong request gửi lên, kiểm tra 3 cổng nghiệp vụ và trả về kết quả tương ứng.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:**
  - **DTO (Data Transfer Object) chỉ đóng vai trò là hợp đồng dữ liệu dạng JSON** (Wire Contract) truyền tải qua mạng, không phải là lớp dùng chung trong cùng một bộ nhớ.
  - Client và Server là hai tiến trình độc lập (thậm chí có thể viết bằng hai ngôn ngữ lập trình khác nhau). Chúng giao tiếp bằng các chuỗi byte JSON qua HTTP. Khi tuần tự hóa (Serialize) và giải tuần tự hóa (Deserialize), hệ thống ánh xạ thông qua **tên thuộc tính và kiểu dữ liệu trong chuỗi JSON**.
  - Nhờ vậy, Client không cần tham chiếu (project reference) sang project Server. Điều này đảm bảo tính độc lập (loose coupling), cho phép một bên thay đổi code nội bộ mà không làm hỏng bên còn lại miễn là vẫn giữ đúng hợp đồng JSON.

