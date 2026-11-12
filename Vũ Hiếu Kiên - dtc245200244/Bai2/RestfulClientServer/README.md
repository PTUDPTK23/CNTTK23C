# Bài 2: RESTful API cho tương tác Client–Server (Catalog API & EduSubmit)

- **Họ và tên:** Vũ Hiếu Kiên
- **Mã sinh viên:** `dtc245200244`
- **Lớp:** CNTT K23C
- **Học phần:** Phát triển ứng dụng phân tán
- **Môi trường:** Windows 11 | .NET SDK `10.0.201` | ASP.NET Core Minimal API (Kestrel)

---

## 1. Cấu trúc thư mục nộp bài `Bai2/`

```text
Bai2/
├── README.md                          # Nhật ký thực hành, bảng phá thử & trả lời 3 câu hỏi
├── requests.http                      # Bộ kịch bản kiểm thử REST Client cho EduSubmit (cổng 5000)
├── CatalogApi/                        # Phần 2: Thực hành mẫu Catalog API (cổng 5100)
│   ├── CatalogApi.slnx
│   ├── .gitignore
│   ├── README.md
│   ├── requests.http                  # 8 ca kiểm thử Catalog API
│   ├── sp-hop-le.json
│   ├── sp-sai.json
│   └── src/
│       ├── Catalog.Api/Program.cs
│       └── Catalog.Client/Program.cs
└── RestfulClientServer/               # Phần 3: Hệ thống nộp bài EduSubmit (cổng 5000)
    ├── RestfulClientServer.slnx
    ├── .gitignore
    ├── README.md
    ├── requests.http                  # 13 ca kiểm thử EduSubmit (bao phủ 200, 201, 204, 400, 404, 409, 415)
    ├── sv001.json
    ├── rong.json
    └── src/
        ├── Submission.Api/Program.cs
        └── Submission.Client/Program.cs
```

---

## 2. Nhật ký thực hành và Kết quả chạy thực tế

### 2.1. Phần 2 — Catalog API (`CatalogApi` - Cổng `5100`)

#### Lệnh khởi tạo và biên dịch (Bước 1):
```powershell
cd CatalogApi
dotnet build CatalogApi.slnx
# Build succeeded. 0 Warning(s), 0 Error(s)
```

#### Chạy máy chủ `Catalog.Api` (Terminal 1) và kiểm thử bằng `curl.exe` (Terminal 2):
```powershell
# Terminal 1:
dotnet run --project src/Catalog.Api --urls http://localhost:5100

# Terminal 2:
curl.exe -i http://localhost:5100/
# -> HTTP/1.1 200 OK | {"service":"Catalog.Api","status":"running"}

curl.exe -i http://localhost:5100/api/products
# -> HTTP/1.1 200 OK | [{"id":1,"name":"Giao trinh Lap trinh phan tan",...},...]

curl.exe -i http://localhost:5100/api/products/1
# -> HTTP/1.1 200 OK | {"id":1,"name":"Giao trinh Lap trinh phan tan","description":"Giao trinh chinh thuc","price":150000}

curl.exe -i http://localhost:5100/api/products/999
# -> HTTP/1.1 404 Not Found | {"code":"PRODUCT_NOT_FOUND","message":"Khong tim thay san pham."}

curl.exe -i http://localhost:5100/api/products/abc
# -> HTTP/1.1 404 Not Found | Content-Length: 0 (rỗng - do ràng buộc tuyến :int)

curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-hop-le.json"
# -> HTTP/1.1 201 Created | Location: /api/products/4 | {"id":4,"name":"Lap trinh gRPC","description":"Tai lieu bai 5","price":80000}

curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-sai.json"
# -> HTTP/1.1 400 Bad Request | {"code":"INVALID_PRODUCT","message":"Ten san pham khong duoc de trong va gia phai lon hon 0."}

curl.exe -i -X POST http://localhost:5100/api/products -d "@sp-hop-le.json"
# -> HTTP/1.1 415 Unsupported Media Type
```

#### Kết quả chạy máy khách `Catalog.Client` (4 nấc):
```powershell
dotnet run --project src/Catalog.Client -- http://localhost:5100
```
```text
Danh sach san pham:
 - [1] Giao trinh Lap trinh phan tan (150,000 VND)
 - [2] Tai lieu RESTful API (90,000 VND)
 - [3] Bai tap thuc hanh .NET (120,000 VND)

Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

Chi tiet san pham 1: Giao trinh Lap trinh phan tan

Thu gui du lieu sai: 400 BadRequest
INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.
```

#### Phiếu tự kiểm tra 10 ô (Bước 10 - Catalog API):
| # | Yêu cầu | Mong đợi | Kết quả thực tế | Tự đánh giá |
| :-: | :--- | :--- | :--- | :-: |
| 1 | `GET /` | `200` | `200 OK` + `{"service":"Catalog.Api","status":"running"}` | ✓ |
| 2 | `GET /api/products` | `200`, 3 phần tử | `200 OK` + mảng JSON 3 sản phẩm | ✓ |
| 3 | `GET /api/products/1` | `200` | `200 OK` + chi tiết sản phẩm 1 | ✓ |
| 4 | `GET /api/products/999` | `404 + ApiError` | `404 Not Found` + `PRODUCT_NOT_FOUND` (Handler trả) | ✓ |
| 5 | `GET /api/products/abc` | `404` rỗng | `404 Not Found`, `Content-Length: 0` (Framework trả) | ✓ |
| 6 | `POST` hợp lệ | `201 + Location` | `201 Created`, `Location: /api/products/4` | ✓ |
| 7 | `POST` giá âm | `400 + ApiError` | `400 Bad Request` + `INVALID_PRODUCT` | ✓ |
| 8 | `POST` thiếu `Content-Type` | `415` | `415 Unsupported Media Type` | ✓ |
| 9 | Máy khách chạy | Đủ 4 nấc | In danh sách -> tạo mới -> đọc lại -> bắt lỗi 400 | ✓ |
| 10 | Tắt máy chủ, chạy máy khách | Lỗi kết nối | `HttpRequestException: Connection refused (localhost:5100)` | ✓ |

---

### 2.2. Phần 3 — Hệ thống nộp bài EduSubmit (`RestfulClientServer` - Cổng `5000`)

#### Bảng hợp đồng API của EduSubmit:
| Phương thức | URI | Thành công | Lỗi có thể gặp |
| :--- | :--- | :--- | :--- |
| `GET` | `/` | `200 OK` + `{service, status}` | – |
| `GET` | `/api/assignments` | `200 OK` + danh sách bài tập | – |
| `GET` | `/api/assignments/{assignmentId:int}` | `200 OK` + chi tiết một bài tập | `404 ASSIGNMENT_NOT_FOUND` (hoặc `404` rỗng nếu sai kiểu `:int`) |
| `POST` | `/api/assignments/{assignmentId:int}/submissions` | `201 Created` + `Location` + `SubmissionDto` | `404 ASSIGNMENT_NOT_FOUND` · `400 ASSIGNMENT_CLOSED` · `400 INVALID_SUBMISSION` · `409 DUPLICATE_SUBMISSION` · `415` |
| `GET` | `/api/submissions/{submissionId:guid}` | `200 OK` + chi tiết lượt nộp | `404 SUBMISSION_NOT_FOUND` |
| `DELETE` *(mở rộng)* | `/api/submissions/{submissionId:guid}` | `204 No Content` (không body) | `404 SUBMISSION_NOT_FOUND` |

#### Lệnh chạy hệ thống 2 tiến trình EduSubmit:
- **Terminal 1 (Máy chủ API cổng 5000):**
  ```powershell
  cd RestfulClientServer
  dotnet run --project src/Submission.Api --urls http://localhost:5000
  ```
- **Terminal 2 (Máy khách Console 4 nấc):**
  ```powershell
  dotnet run --project src/Submission.Client -- http://localhost:5000
  ```
- **Kết quả thực tế trên Terminal 2:**
  ```text
  Danh sach bai tap dang mo:
  - 1: Bai tap REST API
  - 2: Bai tap Client

  Da tao luot nop: 0e9d6d59-ff06-42f6-bc0f-7296929f4522
  Trang thai ban dau: received
  Location: /api/submissions/0e9d6d59-ff06-42f6-bc0f-7296929f4522
  Doc lai luot nop: SV001 - https://example.com/sv001/rest-api

  Thu gui yeu cau loi: 400 BadRequest
  INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
  ```

---

## 3. Bảng phá thử (Chaos & Negative Testing)

### 3.1. Bảng 8 ca kiểm thử ở Bước 5 (`5a` – `5h`) trên EduSubmit:
| Ca | Yêu cầu thử nghiệm | Mã dự đoán | Kết quả thực tế | Ai trả lời? | Giải thích ý nghĩa |
| :-: | :--- | :-: | :--- | :-: | :--- |
| **5a** | `POST /api/assignments/1/submissions` với `sv001.json` | `201` | `201 Created` + Header `Location: /api/submissions/{guid}` + `SubmissionDto` | **Handler** | Dữ liệu qua đủ 3 cổng kiểm tra; Server tự sinh `Guid`, `Status="received"`, `ReceivedAt` và trả kèm đường dẫn đọc lại. |
| **5b** | `POST /api/assignments/1/submissions` với `rong.json` | `400` | `400 Bad Request` + `{"code":"INVALID_SUBMISSION","message":"Thieu ma sinh vien hoac kho ma nguon."}` | **Handler** | Cổng kiểm tra thứ 3 bắt lỗi thiếu `StudentCode` hoặc `RepositoryUrl`, trả về `ApiError` thống nhất. |
| **5c** | `POST /api/assignments/999/submissions` với `sv001.json` | `404` | `404 Not Found` + `{"code":"ASSIGNMENT_NOT_FOUND","message":"Khong tim thay bai tap."}` | **Handler** | Cổng kiểm tra thứ 1 phát hiện tài nguyên cha (bài tập `999`) không tồn tại. |
| **5d** | Như `5a` nhưng bỏ header `Content-Type: application/json` | `415` | `415 Unsupported Media Type` (body rỗng, không có `ApiError`) | **Framework** | ASP.NET Core chặn trước khi vào handler vì không biết body gửi lên thuộc định dạng JSON. |
| **5e** | Gửi body JSON hỏng cú pháp: `{"studentCode":"SV001",` | `400` | `400 Bad Request` (body text của framework, không có `ApiError`) | **Framework** | Bộ đọc JSON (JsonSerializer) của framework báo lỗi cú pháp trước khi gọi hàm handler. |
| **5f** | Gửi body thiếu trường: `{"studentCode":"SV001"}` | `400` | `400 Bad Request` + `{"code":"INVALID_SUBMISSION",...}` | **Handler** | Trường `RepositoryUrl` bị gán `null`, bị cổng kiểm tra thứ 3 (`IsNullOrWhiteSpace`) trong handler chặn lại. |
| **5g** | `PUT /api/assignments/1` | `405` | `405 Method Not Allowed` (Header `Allow: GET`) | **Framework** | Bộ định tuyến thấy URI `/api/assignments/1` có tồn tại nhưng chỉ đăng ký `MapGet`, không hỗ trợ động từ `PUT`. |
| **5h** | `POST /api/assignments/3/submissions` (bài tập có `IsOpen = false`) | `400` | `400 Bad Request` + `{"code":"ASSIGNMENT_CLOSED","message":"Bai tap da dong, khong nhan luot nop moi."}` | **Handler** | Cổng kiểm tra thứ 2 chặn lại do vi phạm quy tắc nghiệp vụ: bài tập đã khóa nộp. |

### 3.2. Bảng 4 thí nghiệm hệ phân tán (Bước 9) & Mở rộng (`409 Conflict`, `204 No Content`):
| # | Thí nghiệm | Kết quả quan sát thực tế | Bài học về hệ phân tán |
| :-: | :--- | :--- | :--- |
| **TN1** | Chạy máy khách lần 2 khi máy chủ vẫn đang chạy | Với Catalog: tạo sản phẩm `[5]`. Với EduSubmit (đã bật mở rộng Bước 9): trả `409 Conflict` (`DUPLICATE_SUBMISSION`) do `SV001` đã nộp bài tập 1. | Trạng thái (`products` / `submissions`) nằm ở bộ nhớ máy chủ, tồn tại xuyên suốt giữa các lần máy khách gọi. |
| **TN2** | Tắt máy chủ (Ctrl+C ở Terminal 1) rồi chạy máy khách | `Unhandled exception. System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it.` | Lỗi đặc trưng chỉ có ở hệ phân tán: tiến trình phía bên kia mạng có thể vắng mặt. |
| **TN3** | Khởi động lại máy chủ rồi gọi `GET /api/submissions/{id_cu}` | Trả về `404 Not Found` (`SUBMISSION_NOT_FOUND`); danh sách sản phẩm quay về 3 phần tử gốc. | Dữ liệu lưu trong `List<T>` trên RAM sẽ mất khi tiến trình máy chủ kết thúc. |
| **TN4** | Chạy máy khách với cổng sai (`-- http://localhost:5999`) | `HttpRequestException: Connection refused (localhost:5999)` | Hai tiến trình độc lập chỉ nói chuyện được khi thống nhất địa chỉ kết nối (URL + cổng). |
| **9d–9f** | Gửi `DELETE /api/submissions/{id}` rồi nộp lại | `DELETE` lần 1 trả `204 No Content`; `DELETE` lần 2 trả `404 SUBMISSION_NOT_FOUND`; nộp `POST` lại trả `201 Created` với `Id` mới. | Minh họa tính lũy đẳng của `DELETE` và chu trình rút bài nộp -> nộp lại. |

---

## 4. Trả lời 3 câu hỏi giải thích lý thuyết (Slide 90 & 116)

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Nguyên tắc thiết kế hướng tài nguyên (Resource-Oriented) của REST:**
  - Trong kiến trúc REST, **URI (Uniform Resource Identifier)** dùng để định danh **"thứ" (tài nguyên / danh từ)** mà hệ thống quản lý, ví dụ: tập hợp bài tập (`/api/assignments`), một bài tập cụ thể (`/api/assignments/1`), các lượt nộp thuộc bài tập đó (`/api/assignments/1/submissions`), hay một lượt nộp cụ thể (`/api/submissions/{submissionId}`).
  - **Hành động (động từ)** thực hiện lên tài nguyên đó được giao cho **phương thức HTTP chuẩn** đảm nhận (`GET` để đọc, `POST` để tạo mới, `PUT` để thay thế, `PATCH` để sửa một phần, `DELETE` để xóa).
- **Ví dụ trực tiếp từ code đã viết:**
  - Thay vì tạo nhiều đường dẫn chứa động từ kiểu gọi hàm như `/getAssignments`, `/doSubmit`, `/deleteSubmission`, ta dùng chung hệ thống URI danh từ kết hợp với `app.MapGet("/api/assignments", ...)`, `app.MapPost("/api/assignments/{assignmentId:int}/submissions", ...)` và `app.MapDelete("/api/submissions/{submissionId:guid}", ...)`.
  - Cách làm này giúp **hợp đồng API nhất quán, giao diện thống nhất (Uniform Interface)**: máy khách chỉ cần nhìn cặp *(Phương thức HTTP + URI)* là hiểu ngay ngữ nghĩa thao tác, cũng như biết được tính chất **an toàn (safe)** và **lũy đẳng (idempotent)** của yêu cầu.

---

### Câu 2: “Stateless” (Phi trạng thái) thể hiện ở đâu trong code đã viết?
- **Ý nghĩa của Stateless trong REST:**
  - Máy chủ **không lưu giữ trạng thái phiên hội thoại (session/conversation state) của máy khách** giữa hai lần gửi yêu cầu liên tiếp. Mỗi yêu cầu HTTP từ máy khách gửi lên phải **tự mang đầy đủ mọi thông tin cần thiết** để máy chủ hiểu và xử lý độc lập.
- **Thể hiện cụ thể trong code EduSubmit đã viết:**
  1. Ở yêu cầu nộp bài `POST /api/assignments/{assignmentId:int}/submissions`: máy khách không hề gọi một bước "chọn bài tập 1 lưu vào phiên" trước đó, mà truyền trực tiếp mã bài tập `assignmentId` ngay trên đường dẫn URI (`/api/assignments/1/submissions`) cùng với `StudentCode` và `RepositoryUrl` ngay trong body JSON (`CreateSubmissionRequest`).
  2. Ở yêu cầu đọc lại lượt nộp `GET /api/submissions/{submissionId:guid}`: máy khách truyền tường minh định danh `submissionId` (lấy từ header `Location`) trên URI thay vì hỏi máy chủ "lượt nộp vừa nãy của tôi đâu?".
- **Phân biệt với việc lưu dữ liệu:**
  - *Stateless không có nghĩa là máy chủ không lưu dữ liệu nghiệp vụ.* Máy chủ vẫn lưu danh sách `assignments` và `submissions` (trong `List<SubmissionDto>` hoặc CSDL), nhưng không ghi nhớ "máy khách A vừa mới gọi bước nào trước đó". Nhờ vậy, nếu sau này đặt nhiều máy chủ `Submission.Api` phía sau bộ cân bằng tải (Load Balancer), bất kỳ máy chủ nào cũng xử lý được yêu cầu tiếp theo của máy khách.

---

### Câu 3: Vì sao Client và Server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **DTO là hợp đồng dữ liệu ở biên hệ thống (Data Contract), không phải lớp C# dùng chung trong bộ nhớ:**
  - `Submission.Api` và `Submission.Client` là **hai chương trình độc lập chạy trên hai tiến trình riêng biệt** (thậm chí có thể viết bằng hai ngôn ngữ lập trình khác nhau và chạy trên hai máy tính khác nhau), không chia sẻ không gian bộ nhớ và project `Submission.Client.csproj` không hề tham chiếu (`ProjectReference`) tới `Submission.Api.csproj`.
  - Khi truyền qua mạng bằng HTTP, các đối tượng C# không đi qua mạng trực tiếp mà được tuần tự hóa (serialize) thành **chuỗi văn bản JSON**.
- **Cơ chế khớp nhau trong code đã viết:**
  - Ở phía Server (`Submission.Api/Program.cs`), khi trả `Results.Ok(assignment)` hoặc `Results.Created(..., submission)`, ASP.NET Core chuyển `record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, ...)` thành chuỗi JSON có các trường camelCase: `{"id":"...","assignmentId":1,"studentCode":"SV001",...}`.
  - Ở phía Client (`Submission.Client/Program.cs`), phương thức `ReadFromJsonAsync<SubmissionDto>()` đọc chuỗi JSON đó và ánh xạ ngược (deserialize) vào bản sao `record SubmissionDto(...)` của riêng Client dựa trên **tên thuộc tính (không phân biệt hoa thường mặc định trong Web/HttpClient) và kiểu dữ liệu tương thích** (`Guid`, `int`, `string`, `DateTimeOffset`).
  - Nhờ cơ chế này, miễn là hai bên tuân thủ đúng **hợp đồng JSON** (đúng tên trường và kiểu dữ liệu), cả hai bên hoàn toàn độc lập về mã nguồn và có thể tiến hóa riêng mà không làm vỡ nhau.
