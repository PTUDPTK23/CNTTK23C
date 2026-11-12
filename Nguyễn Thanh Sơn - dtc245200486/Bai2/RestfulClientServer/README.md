# BÁO CÁO THỰC HÀNH BÀI 2: RESTFUL API — HỆ THỐNG EDUSUBMIT (RESTFULCLIENTSERVER)

Học phần: **Phát triển ứng dụng phân tán**  
Sinh viên thực hiện: **Nguyễn Thanh Sơn**  
Mã sinh viên: **DTC245200486**  
Email: **dtc245200486@ictu.edu.vn**  
Cơ sở đào tạo: **Trường Đại học Công nghệ Thông tin và Truyền thông (ICTU)**

---

## 1. Giới Thiệu Dự Án EduSubmit (RestfulClientServer)

Dự án **EduSubmit** (`RestfulClientServer`) là một hệ thống phân tán thu nhỏ minh họa mô hình Client-Server dựa trên kiến trúc **RESTful API** chuẩn mực sử dụng nền tảng **.NET 10 (C# Minimal API)**.

Hệ thống bao gồm hai tiến trình phần mềm độc lập:
1. **`src/Submission.Api`**: Máy chủ Web API lắng nghe tại cổng `http://localhost:5000`, cung cấp các endpoint quản lý bài tập và tiếp nhận lượt nộp bài của sinh viên với cơ chế kiểm soát 3 cổng chặt chẽ (3-gate validation).
2. **`src/Submission.Client`**: Ứng dụng Console Client kết nối tới API qua `HttpClient`, thực thi chuỗi 4 nấc kiểm thử toàn diện, minh họa cơ chế phòng vệ chống crash khi server trả về mã lỗi 4xx.

### Danh mục tài nguyên và endpoint của hệ thống

| Phương thức | URI Endpoint | Chức năng nghiệp vụ | Mã phản hồi chính |
|-------------|--------------|---------------------|-------------------|
| `GET` | `/` | Kiểm tra trạng thái máy chủ (Health check) | `200 OK` |
| `GET` | `/api/assignments` | Lấy danh sách toàn bộ bài tập trong hệ thống | `200 OK` |
| `GET` | `/api/assignments/{assignmentId:int}` | Lấy thông tin chi tiết một bài tập theo Id | `200 OK`, `404 Not Found` |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | Nộp bài tập qua 3 cổng kiểm tra logic nghiêm ngặt | `201 Created`, `400 Bad Request`, `404 Not Found`, `415 Unsupported Media Type` |
| `GET` | `/api/submissions/{submissionId:guid}` | Đọc lại thông tin lượt nộp bài theo định danh Guid | `200 OK`, `404 Not Found` |

---

## 2. Hướng Dẫn Biên Dịch Và Khởi Chạy

### Yêu cầu môi trường
- .NET SDK 10.0 (hoặc tương thích .NET 8.0/9.0).
- Trình thông dịch dòng lệnh (Terminal / PowerShell / Bash).
- Công cụ kiểm thử: `curl` hoặc Visual Studio / VS Code REST Client (`requests.http`).

### Lệnh biên dịch Solution
Tại thư mục gốc `RestfulClientServer`:
```bash
dotnet build
```

### Lệnh khởi chạy Web API (`Submission.Api`)
```bash
# Chạy mặc định cổng 5000 (được cố định trong launchSettings.json và Program.cs)
dotnet run --project src/Submission.Api
```

### Lệnh khởi chạy Console Client (`Submission.Client`)
```bash
# Chạy với địa chỉ mặc định http://localhost:5000
dotnet run --project src/Submission.Client

# Hoặc truyền tham số URL tùy chọn qua dòng lệnh
dotnet run --project src/Submission.Client -- http://localhost:5000
```

---

## 3. Nhật Ký Lệnh CLI Và Kết Quả Đầu Ra Thực Tế (CLI Execution Logs)

### 3.1. Kết quả biên dịch Solution (`dotnet build`)

```text
$ dotnet build
  Determining projects to restore...
  All projects are up-to-date for restore.
  Submission.Client -> /Users/nguyenthanhson/Desktop/RestfulClientServer/src/Submission.Client/bin/Debug/net10.0/Submission.Client.dll
  Submission.Api -> /Users/nguyenthanhson/Desktop/RestfulClientServer/src/Submission.Api/bin/Debug/net10.0/Submission.Api.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.00
```

### 3.2. Nhật ký chạy máy khách (`Submission.Client`) thành công 4 nấc

```text
$ dotnet run --project src/Submission.Client -- http://localhost:5000
=== CHUONG TRINH CLIENT NOP BAI (Submission.Client) ===
Ket noi den API tai: http://localhost:5000

[Nac 1] Lay danh sach bai tap dang mo...
Danh sach bai tap dang mo:
  - Id: 1 | Tieu de: Bai tap REST API | Mo: True
  - Id: 2 | Tieu de: Bai tap Client | Mo: True

[Nac 2] Gui yeu cau nop bai cho bai tap Id=1...
Nop bai thanh cong! (HTTP 201 Created)
  - Header Location: /api/submissions/16e89324-a1ba-4567-9d42-581aadb7115a
  - Id luot nop: 16e89324-a1ba-4567-9d42-581aadb7115a
  - Ma sinh vien: SV001
  - Kho ma nguon: https://example.com/sv001/rest-api
  - Trang thai: received
  - Thoi gian: 2026-11-12 08:16:56 UTC

[Nac 3] Doc lai luot nop theo Location header: /api/submissions/16e89324-a1ba-4567-9d42-581aadb7115a...
Doc lai thanh cong (HTTP 200 OK):
  - Id: 16e89324-a1ba-4567-9d42-581aadb7115a
  - AssignmentId: 1
  - Ma sinh vien: SV001
  - Kho ma nguon: https://example.com/sv001/rest-api
  - Trang thai: received

[Nac 4] Co y gui yeu cau rong de kiem thu xu ly loi 400...
Nhan ma phan hoi: 400 BadRequest
  [ApiError] Ma loi: INVALID_SUBMISSION | Thong diep: Ma sinh vien va duong dan kho khong duoc de trong.
Xu ly loi an toan, ung dung khong bi crash.
```

### 3.3. Nhật ký kiểm thử khi Server tắt hoặc sai cổng (Lỗi mạng `Connection refused`)

```text
$ dotnet run --project src/Submission.Client -- http://localhost:5999
=== CHUONG TRINH CLIENT NOP BAI (Submission.Client) ===
Ket noi den API tai: http://localhost:5999

[Nac 1] Lay danh sach bai tap dang mo...

[LOI KET NOI MANG]: Khong the ket noi den may chu API (Connection refused (localhost:5999)).
```

### 3.4. Nhật ký thực thi kiểm thử với `curl`

#### Kiểm tra Health Check (`GET /`):
```text
$ curl -i http://localhost:5000/
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked

{"service":"Submission.Api","status":"running"}
```

#### Nộp bài hợp lệ với `sv001.json` (`POST /api/assignments/1/submissions`):
```text
$ curl -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d @sv001.json
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: /api/submissions/1b2f80ef-05f1-43e2-a700-045ada7620b4
Server: Kestrel
Transfer-Encoding: chunked

{"id":"1b2f80ef-05f1-43e2-a700-045ada7620b4","assignmentId":1,"studentCode":"SV001","repositoryUrl":"https://example.com/sv001/rest-api","status":"received","submittedAt":"2026-11-12T08:17:15.915234+00:00"}
```

#### Nộp bài vào bài tập đã đóng (`POST /api/assignments/3/submissions`):
```text
$ curl -i -X POST http://localhost:5000/api/assignments/3/submissions -H "Content-Type: application/json" -d @sv001.json
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked

{"code":"ASSIGNMENT_CLOSED","message":"Bai tap da dong."}
```

---

## 4. Bảng Phá Thử (Chaos Testing Table)

Bảng tổng hợp chi tiết toàn bộ các kịch bản kiểm thử biên, gửi dữ liệu lỗi, sai phương thức, sai tiêu đề và phân định rõ tầng xử lý (Handler nghiệp vụ vs Kestrel Framework):

| # | Kịch bản kiểm thử (Scenario) | Yêu cầu HTTP (Method + URI + Header/Body) | Mã trạng thái kỳ vọng | Tầng phản hồi | Cấu trúc nội dung phản hồi (Body) | Ý nghĩa nghiệp vụ / Phân tích hành vi |
|---|-----------------------------|-------------------------------------------|-----------------------|---------------|-----------------------------------|---------------------------------------|
| 1 | Kiểm tra trạng thái máy chủ | `GET /` | `200 OK` | Handler | `{"service":"Submission.Api","status":"running"}` | Xác nhận máy chủ API đang sống và lắng nghe tại cổng 5000 |
| 2 | Lấy danh sách toàn bộ bài tập | `GET /api/assignments` | `200 OK` | Handler | Mảng JSON các `AssignmentDto` | Trả về danh sách bài tập đã khởi tạo mẫu trong bộ nhớ |
| 3 | Lấy chi tiết bài tập tồn tại | `GET /api/assignments/1` | `200 OK` | Handler | `AssignmentDto` (Id: 1, Title: "Bai tap REST API"...) | Trả về thông tin bài tập tương ứng |
| 4 | Lấy bài tập không tồn tại | `GET /api/assignments/999` | `404 Not Found` | Handler | `{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | Handler tra cứu bộ nhớ không thấy, trả `ApiError` chuẩn |
| 5 | Lấy bài tập với Id không phải số | `GET /api/assignments/abc` | `404 Not Found` | Framework | Body rỗng (`Content-Length: 0`) | Ràng buộc tuyến `:int` không khớp, Framework chặn trước handler |
| 6 | Nộp bài hợp lệ (Cổng 1, 2, 3 đạt) | `POST /api/assignments/1/submissions` kèm `sv001.json` | `201 Created` | Handler | Header `Location: /api/submissions/{id}`, Body `SubmissionDto` | Server tự sinh Guid `Id`, trạng thái "received", gán thời gian UTC |
| 7 | Nộp bài với trường rỗng (Cổng 3) | `POST /api/assignments/1/submissions` kèm `rong.json` | `400 Bad Request` | Handler | `{"code":"INVALID_SUBMISSION","message":"Ma sinh vien va duong dan kho khong duoc de trong."}` | Cổng 3 chặn: chuỗi rỗng / whitespace bị từ chối |
| 8 | Nộp bài cho bài tập không tồn tại (Cổng 1) | `POST /api/assignments/999/submissions` kèm `sv001.json` | `404 Not Found` | Handler | `{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | Cổng 1 chặn: tài nguyên cha không tồn tại |
| 9 | Nộp bài vào bài tập đã đóng (Cổng 2) | `POST /api/assignments/3/submissions` kèm `sv001.json` | `400 Bad Request` | Handler | `{"code":"ASSIGNMENT_CLOSED","message":"Bai tap da dong."}` | Cổng 2 chặn: bài tập có `IsOpen == false`, từ chối nhận bài |
| 10 | Nộp bài thiếu Header Content-Type | `POST /api/assignments/1/submissions` (không gửi Content-Type) | `415 Unsupported Media Type` | Framework | Body rỗng | Kestrel từ chối tiếp nhận vì không xác định được định dạng MIME |
| 11 | Nộp bài với cú pháp JSON hỏng | `POST /api/assignments/1/submissions` kèm `{"studentCode":"SV001",` | `400 Bad Request` | Framework | Plaintext báo lỗi `BadHttpRequestException` từ `JsonReaderException` | Bộ phân tích cú pháp JSON của ASP.NET Core chặn trước handler |
| 12 | Đọc lại bài nộp theo Location ID hợp lệ | `GET /api/submissions/{guid}` | `200 OK` | Handler | `SubmissionDto` tương ứng | Đọc lại chính xác tài nguyên vừa tạo qua URL định danh duy nhất |
| 13 | Đọc bài nộp không tồn tại trong hệ thống | `GET /api/submissions/00000000-0000-0000-0000-000000000000` | `404 Not Found` | Handler | `{"code":"SUBMISSION_NOT_FOUND","message":"Khong tim thay luot nop."}` | Handler không tìm thấy Guid trong RAM, trả `ApiError` chuẩn |
| 14 | Đọc bài nộp với Guid sai định dạng | `GET /api/submissions/abc-xyz` | `404 Not Found` | Framework | Body rỗng | Ràng buộc tuyến `:guid` không khớp, Framework chặn trước handler |
| 15 | Sai phương thức HTTP trên URI tồn tại | `PUT /api/assignments/1` | `405 Method Not Allowed` | Framework | Header `Allow: GET`, Body rỗng | Endpoint chỉ đăng ký GET, gọi PUT bị Framework từ chối |
| 16 | Khởi động lại Server và đọc lại Guid cũ | Tắt API, bật lại, gọi `GET /api/submissions/{guid-cu}` | `404 Not Found` | Handler | `{"code":"SUBMISSION_NOT_FOUND","message":"Khong tim thay luot nop."}` | Minh họa tính chất lưu trữ tạm thời trong RAM (In-Memory) |
| 17 | Client kết nối khi Server tắt hoặc sai cổng | Chạy client với cổng `http://localhost:5999` | Không có mã HTTP | Mạng / OS | `HttpRequestException: Connection refused` | Lỗi mạng vật lý ở tầng giao vận (OS), không có thông điệp HTTP |

---

## 5. Trả Lời 3 Câu Hỏi Lý Thuyết Cốt Lõi (Slide 116)

### Câu hỏi 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?

**Trả lời:**
- Trong kiến trúc REST (Representational State Transfer), **URI (Uniform Resource Identifier)** đóng vai trò là địa chỉ định danh duy nhất cho một **tài nguyên** (Resource). Bản chất tài nguyên là thực thể thông tin hoặc tập hợp đối tượng dữ liệu trong thế giới thực, do đó tài nguyên phải được biểu diễn bằng **danh từ** (thường ở dạng số nhiều như `/api/assignments`, `/api/submissions`).
- Ngược lại, các thao tác nghiệp vụ cần tác động lên tài nguyên đó đã được chuẩn hóa trọn vẹn thông qua các **phương thức của giao thức HTTP** (HTTP Verbs):
  - `GET`: Lấy/đọc trạng thái biểu diễn của tài nguyên (an toàn và idempotent).
  - `POST`: Tạo mới một tài nguyên con hoặc xử lý nghiệp vụ làm thay đổi trạng thái hệ thống.
  - `PUT`: Cập nhật toàn bộ tài nguyên hoặc thay thế tại vị trí định danh.
  - `DELETE`: Xóa tài nguyên khỏi hệ thống.
- **Ý nghĩa thiết kế**:
  1. Tuân thủ nguyên lý **Giao diện đồng nhất (Uniform Interface)** — một trong 6 ràng buộc cốt lõi của kiến trúc REST.
  2. Giúp tập hợp URI trở nên trực quan, ngắn gọn, dễ đoán và có tính phân cấp logic cao (ví dụ: `POST /api/assignments/1/submissions` thể hiện rõ hành động tạo lượt nộp mới thuộc bài tập số 1).
  3. Tránh việc tùy tiện bùng nổ hàng loạt động từ mơ hồ và hỗn loạn trong URL như phong cách RPC cũ (ví dụ: tránh tạo các URI như `/submitAssignment`, `/createStudentSubmission`, `/getAssignmentDetail`).

---

### Câu hỏi 2: "Stateless" (phi trạng thái) thể hiện ở đâu trong code đã viết?

**Trả lời:**
Tính chất **Stateless (Phi trạng thái)** của REST yêu cầu máy chủ không được lưu giữ bất kỳ ngữ cảnh hội thoại, phiên làm việc (Session) hay trạng thái máy khách nào giữa hai lần gọi liên tiếp. Trong mã nguồn của `Submission.Api` và `Submission.Client`, tính chất này được thể hiện rõ nét qua các điểm sau:
1. **Thông điệp tự chứa đủ ngữ cảnh (Self-descriptive message)**:
   - Trong endpoint `POST /api/assignments/{assignmentId:int}/submissions`, toàn bộ ngữ cảnh bài tập cần nộp nằm ngay trên đường dẫn URI (`{assignmentId}`), còn thông tin sinh viên (`StudentCode`) và đường dẫn kho (`RepositoryUrl`) nằm đầy đủ trong Payload JSON gửi lên.
   - Khi Server nhận được request, nó không cần biết trước đó Client này là ai, vừa đăng nhập khi nào, hay đã gửi bao nhiêu request trước đó. Tất cả những gì Server cần để thẩm định hợp lệ và tạo lượt nộp đều nằm trọn vẹn trong chính request đó.
2. **Không sử dụng bất kỳ cơ chế phiên làm việc nào**:
   - Trong mã nguồn `Program.cs`, hệ thống hoàn toàn không kích hoạt dịch vụ session (`builder.Services.AddSession()`), không sử dụng cookie phiên (`Set-Cookie`), không dùng token trạng thái phụ thuộc tiến trình.
   - Client có thể thực hiện Nấc 1 (lấy danh sách bài tập), sau đó tiến trình Client có thể tắt đi, một client hoàn toàn khác gửi Nấc 2 (nộp bài), Server vẫn tiếp nhận và xử lý bình thường mà không đòi hỏi phiên kết nối liên tục.

---

### Câu hỏi 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

**Trả lời:**
- **Bản chất của DTO (Data Transfer Object)**:
  DTO trong kiến trúc mạng phân tán chỉ là **bản đặc tả cấu trúc dữ liệu của hợp đồng trao đổi (Contract Specification)**, hoàn toàn không phải là một lớp thực thi nhị phân dùng chung (Shared Binary DLL).
- **Cơ chế khớp dữ liệu qua giao thức chuẩn hóa (Wire Format)**:
  - Khi Server gửi dữ liệu, đối tượng DTO của Server được chuyển đổi (serialize) thành chuỗi văn bản theo định dạng tiêu chuẩn **JSON** qua giao thức mạng HTTP.
  - Khi Client nhận chuỗi JSON này, thư viện mạng (`System.Net.Http.Json`) phân tích cú pháp (deserialize) chuỗi JSON đó và ánh xạ vào các thuộc tính tương ứng của DTO phía Client dựa trên **tên trường** (khớp theo chuẩn lạc đà `camelCase` như `studentCode`, `repositoryUrl`, `isOpen`) và kiểu dữ liệu tương thích.
- **Ý nghĩa kiến trúc phân tán**:
  1. **Liên kết lỏng (Loose Coupling)**: Client và Server có thể phát triển hoàn toàn độc lập, sử dụng các phiên bản runtime khác nhau, thậm chí viết bằng hai ngôn ngữ lập trình khác nhau (ví dụ: Server viết bằng C# .NET, Client viết bằng Python hoặc JavaScript) mà vẫn trao đổi dữ liệu hoàn hảo.
  2. **Bảo mật và toàn vẹn dữ liệu (Over-posting protection)**: Việc không chia sẻ chung mã nguồn ngăn ngừa trường hợp Client vô tình hoặc cố ý can thiệp, gửi đè các trường nhạy cảm do Server độc quyền sinh ra (như `Guid Id`, trạng thái `status = "received"` hay thời gian `submittedAt`).

---

## 6. Lịch Sử Git Commits (Git Commit Discipline)

Lịch sử commit của repository thể hiện các bước triển khai tuần tự, rành mạch, tuân thủ đúng định dạng và nguyên tắc không commit file nhị phân rác:

```text
$ git log --oneline
f93a6f6 (HEAD -> main) Buoc 8: hien thuc Submission.Client va hoan thien README.md
5751edf Buoc 7: them requests.http kiem thu API
cb2e8bb Buoc 6: them GET /api/submissions/{guid}
5f66f1f Buoc 5: them POST nop bai va payload mau
cc224d4 Buoc 4: them GET /api/assignments/{id}
1cf40be Buoc 3: them AssignmentDto va GET /api/assignments
4fbc928 Buoc 2: them GET / kiem tra may chu
bc9020e Buoc 1: tao solution RestfulClientServer
```

---

## 7. Cấu Trúc Thư Mục Bàn Giao

```text
RestfulClientServer/
├── RestfulClientServer.slnx
├── .gitignore
├── sv001.json
├── rong.json
├── requests.http
├── README.md
└── src/
    ├── Submission.Api/
    │   ├── Program.cs
    │   ├── Submission.Api.csproj
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Properties/
    │       └── launchSettings.json
    └── Submission.Client/
        ├── Program.cs
        └── Submission.Client.csproj
```
