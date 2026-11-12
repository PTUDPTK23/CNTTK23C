# BÁO CÁO THỰC HÀNH BÀI 2: RESTFUL API CHO TƯƠNG TÁC CLIENT-SERVER
## HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN (ICTU)
### Dự án: Hệ thống nộp bài trực tuyến thu nhỏ (EduSubmit API & Client)

---

## 1. THÔNG TIN SINH VIÊN VÀ HỒ SƠ NỘP BÀI
- **Họ và tên:** Hoàng Trang Hiên
- **Mã sinh viên (MSSV):** dtc245200494 (DTC245200494)
- **Lớp:** K23C (K23.K1.D2.2627.CNTT K23C)
- **Email:** dtc245200494@ictu.edu.vn
- **Tài khoản GitHub:** [dtc245200494-hue](https://github.com/dtc245200494-hue)
- **Kho lưu trữ lớp:** [PTUDPTK23/CNTTK23C](https://github.com/PTUDPTK23/CNTTK23C)
- **Giảng viên hướng dẫn:** TS. Dương Thu Mây (dtmay@ictu.edu.vn)
- **Tiêu đề Pull Request:** `Bai2 - dtc245200494 - Hoàng Trang Hiên`
- **Thư mục cấp cao nhất:** `Bai 02` (khi nộp vào repo lớp là `Hoàng Trang Hiên - dtc245200494/Bai2`)

---

## 2. KIẾN TRÚC VÀ CẤU TRÚC THƯ MỤC DỰ ÁN

Hệ thống EduSubmit được thiết kế theo phong cách kiến trúc **RESTful API**, hoạt động như một hệ phân tán thu nhỏ gồm 2 tiến trình độc lập trao đổi qua giao thức HTTP/1.1 và dữ liệu định dạng JSON:
1. **Submission.Api (Server):** Tiến trình Minimal API trên nền .NET 10 Kestrel, lắng nghe tại cổng cố định `http://localhost:5000`. Quản lý tài nguyên bài tập (`assignments`) và tài nguyên con lượt nộp (`submissions`), chuẩn hóa lỗi qua `ApiError { code, message }`.
2. **Submission.Client (Client):** Tiến trình Console Application gửi yêu cầu qua `HttpClient`, thực hiện kịch bản 4 nấc: Liệt kê -> Nộp bài -> Đọc lại theo Location -> Thử các trường hợp lỗi (không sập ứng dụng).

### Cây thư mục kho Git (Root: `Bai 02`):
```text
Bai 02/
├── .git/                                   # Kho Git cục bộ (Bai 02 là thư mục gốc cao nhất)
├── .gitignore                              # Cấu hình bỏ qua bin/, obj/, user cache
├── RestfulClientServer.slnx                # Solution .NET 10 quản lý cả 2 project
├── requests.http                           # Bộ test REST Client đầy đủ các ca 200, 201, 400, 404, 409, 204
├── sv001.json                              # Payload mẫu nộp bài hợp lệ cho curl
├── rong.json                               # Payload mẫu rỗng để kiểm tra lỗi 400
├── README.md                               # Nhật ký thực hành, bảng phá thử và giải thích 3 câu hỏi
└── src/
    ├── Submission.Api/                     # Máy chủ Minimal API (Port 5000)
    │   ├── Program.cs                      # Toàn bộ endpoint và quy tắc nghiệp vụ kiểm tra 3 cổng
    │   ├── Submission.Api.csproj           # Cấu hình project Web API .NET 10
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Properties/
    │       └── launchSettings.json
    └── Submission.Client/                  # Máy khách dòng lệnh (Console)
        ├── Program.cs                      # Chuỗi 4 nấc gọi API, kiểm tra mã trạng thái, xử lý lỗi
        └── Submission.Client.csproj        # Cấu hình project Console .NET 10
```

---

## 3. BẢNG HỢP ĐỒNG API (API CONTRACT)

| Phương thức | Tuyến URI | Mã thành công | Mã lỗi có thể gặp | Mô tả chức năng |
|:---|:---|:---|:---|:---|
| **GET** | `/` | 200 OK | – | Kiểm tra trạng thái hoạt động của API |
| **GET** | `/api/assignments` | 200 OK | – | Lấy danh sách tất cả bài tập |
| **GET** | `/api/assignments/{assignmentId:int}` | 200 OK | 404 ASSIGNMENT_NOT_FOUND | Lấy chi tiết một bài tập theo mã |
| **POST** | `/api/assignments/{assignmentId:int}/submissions` | 201 Created (+ Location) | 404 ASSIGNMENT_NOT_FOUND<br>400 ASSIGNMENT_CLOSED<br>400 INVALID_SUBMISSION<br>409 DUPLICATE_SUBMISSION | Nộp bài cho bài tập (qua 3 cổng kiểm tra + chống trùng) |
| **GET** | `/api/submissions/{submissionId:guid}` | 200 OK | 404 SUBMISSION_NOT_FOUND | Đọc lại chi tiết lượt nộp theo Id trong Location |
| **DELETE** | `/api/submissions/{submissionId:guid}` | 204 No Content | 404 SUBMISSION_NOT_FOUND | Rút lại (hủy) lượt nộp đã tạo (Bước 9 mở rộng) |

*Ghi chú:* Mọi phản hồi lỗi do Handler trả về đều được đóng gói theo định dạng hợp đồng thống nhất:
```json
{
  "code": "TÊN_MÃ_LỖI",
  "message": "Thông điệp giải thích chi tiết lý do từ chối."
}
```

---

## 4. HƯỚNG DẪN BIÊN DỊCH VÀ CHẠY ỨNG DỤNG

### Bước chuẩn bị: Mở thư mục gốc trong terminal
```bash
cd "/home/hien1411/Documents/ứng dụng phân tán/Bai 02"
```

### 1. Biên dịch toàn bộ solution
```bash
dotnet build
```
*Kết quả:* Build succeeded, 0 Warning(s), 0 Error(s).

### 2. Khởi chạy Máy chủ API (Terminal 1)
Cố định cổng 5000 bằng cờ `--urls`:
```bash
dotnet run --project src/Submission.Api --urls http://localhost:5000
```
*Dấu hiệu thành công:* Terminal thông báo `Now listening on: http://localhost:5000`.

### 3. Khởi chạy Máy khách Client (Terminal 2)
```bash
dotnet run --project src/Submission.Client -- http://localhost:5000
```

---

## 5. NHẬT KÝ 10 BƯỚC THỰC HIỆN VÀ LỊCH SỬ COMMIT GIT

Theo đúng quy định chuẩn của bài học, mỗi bước thực hiện tương ứng với một commit nguyên tử trên Git:

1. **Commit 1 (`5108899`):** `Buoc 1: tao solution RestfulClientServer, 2 project va .gitignore`
   - Khởi tạo repo Git tại `Bai 02`.
   - Tạo `RestfulClientServer.slnx`, project `src/Submission.Api` (web), `src/Submission.Client` (console).
   - Thêm `.gitignore` chuẩn .NET để loại trừ `bin/` và `obj/`.
2. **Commit 2 (`1b3d653`):** `Buoc 2: chay khung Submission.Api cong 5000`
   - Cấu hình endpoint gốc `GET /` trả về JSON `{"service":"Submission.Api","status":"running"}` với mã 200 OK.
3. **Commit 3 (`bf3d5c8`):** `Buoc 3: DTO va GET /api/assignments`
   - Khai báo các DTO `AssignmentDto`, `ApiError`. Tạo dữ liệu mẫu trong bộ nhớ (2 bài tập mở, 1 bài tập đã đóng).
   - Khai báo endpoint `GET /api/assignments` trả về 200 OK và danh sách bài tập.
4. **Commit 4 (`e6555fd`):** `Buoc 4: GET /api/assignments/{id}`
   - Ràng buộc tuyến `{assignmentId:int}`.
   - Trả về 200 OK nếu tìm thấy, 404 kèm `ApiError("ASSIGNMENT_NOT_FOUND", ...)` nếu không tìm thấy bài tập.
5. **Commit 5 (`15f985b`):** `Buoc 5: POST /api/assignments/{id}/submissions voi 3 cong kiem tra`
   - Khai báo DTO đầu vào `CreateSubmissionRequest` (chỉ gồm `StudentCode`, `RepositoryUrl`).
   - Xây dựng 3 cổng kiểm tra theo đúng thứ tự nghiệp vụ: (1) Bài tập tồn tại? -> (2) Bài tập còn mở? -> (3) Body hợp lệ?
   - Máy chủ tự sinh `Id` (Guid), `Status` ("received"), `ReceivedAt` (UtcNow). Trả về 201 Created kèm header `Location`.
   - Tạo sẵn tệp test `sv001.json` và `rong.json`.
6. **Commit 6 (`f1dff28`):** `Buoc 6: GET /api/submissions/{submissionId} theo Location`
   - Khai báo endpoint `GET /api/submissions/{submissionId:guid}` phục vụ việc đọc lại lượt nộp theo header Location.
   - Trả về 200 OK nếu có, 404 `SUBMISSION_NOT_FOUND` nếu không tìm thấy.
7. **Commit 7 (`ada5a80`):** `Buoc 7: Submission.Client chuoi 4 nac`
   - Xây dựng máy khách console thực hiện chuỗi 4 nấc:
     - Nấc 1: Lấy danh sách bài tập.
     - Nấc 2: Nộp bài, kiểm tra mã 201 trước khi đọc body, đọc `Location` header.
     - Nấc 3: Đọc lại lượt nộp từ URL trong `Location`.
     - Nấc 4: Cố ý gửi dữ liệu sai, dùng `PrintApiErrorAsync` đọc lỗi 400/404, không làm sập chương trình.
   - Bổ sung khối bắt ngoại lệ mạng `HttpRequestException` để phân biệt lỗi kết nối mạng và lỗi HTTP phản hồi từ API.
8. **Commit 8 (`2560925`):** `Buoc 8: requests.http gom cac ca test`
   - Tạo tệp `requests.http` gom đầy đủ các ca kiểm thử cho VS Code REST Client.
9. **Commit 9 (`a0d46f1`):** `Buoc 9: endpoint mo rong 409 Conflict va DELETE 204`
   - Mở rộng nghiệp vụ: Kiểm tra trùng lặp sinh viên nộp cùng một bài tập -> trả về 409 Conflict (`DUPLICATE_SUBMISSION`).
   - Thêm endpoint `DELETE /api/submissions/{submissionId:guid}` cho phép rút lại bài nộp -> trả về 204 No Content (không body).
10. **Commit 10:** `Buoc 10: README.md nhat ky thuc hanh, bang pha thu va 3 cau tra loi`
   - Hoàn thiện hồ sơ báo cáo nộp bài.

---

## 6. BẢNG PHÁ THỬ (TESTING MATRIX - CÁC CA KIỂM THỬ THỰC TẾ)

Bảng đối chiếu 8 ca kiểm thử bắt buộc tại Bước 5 và các ca kiểm thử mở rộng tại Bước 9:

| Ca | Phương thức & Tuyến URL | Body gửi kèm / Tình huống | Mã mong đợi | Mã thực tế | Thành phần phản hồi | Phân tích cơ chế & Ý nghĩa |
|:---|:---|:---|:---:|:---:|:---:|:---|
| **5a** | `POST /api/assignments/1/submissions` | `sv001.json` (Hợp lệ) | **201 Created** | **201 Created** | Handler | Vượt qua 3 cổng kiểm tra; server sinh Guid, trả Location header `/api/submissions/{id}` |
| **5b** | `POST /api/assignments/1/submissions` | `rong.json` (`studentCode`: `""`) | **400 Bad Request** | **400 Bad Request** | Handler | Cổng 3 chặn: Thiếu mã sinh viên hoặc link repo; trả `ApiError: INVALID_SUBMISSION` |
| **5c** | `POST /api/assignments/999/submissions` | `sv001.json` | **404 Not Found** | **404 Not Found** | Handler | Cổng 1 chặn: Tài nguyên cha không tồn tại; trả `ApiError: ASSIGNMENT_NOT_FOUND` |
| **5d** | `POST /api/assignments/1/submissions` | Không có header `Content-Type` | **415 Unsupported Media Type** | **415 Unsupported Media Type** | Framework | Kestrel/ASP.NET Core không nhận diện được định dạng payload; body rỗng |
| **5e** | `POST /api/assignments/1/submissions` | JSON dị tật (`{"studentCode": "SV001",`) | **400 Bad Request** | **400 Bad Request** | Framework | Bộ phân tích cú pháp JSON của ASP.NET Core thất bại trước khi vào hàm handler |
| **5f** | `POST /api/assignments/1/submissions` | Thiếu trường `repositoryUrl` | **400 Bad Request** | **400 Bad Request** | Handler | `request.RepositoryUrl` là null; cổng 3 phát hiện và trả `INVALID_SUBMISSION` |
| **5g** | `PUT /api/assignments/1` | Không có body | **405 Method Not Allowed** | **405 Method Not Allowed** | Framework | URI tồn tại nhưng không hỗ trợ phương thức PUT; server trả kèm header `Allow: GET` |
| **5h** | `POST /api/assignments/3/submissions` | `sv001.json` (bài tập 3 đã đóng) | **400 Bad Request** | **400 Bad Request** | Handler | Cổng 2 chặn: Bài tập có `IsOpen == false`; trả `ApiError: ASSIGNMENT_CLOSED` |
| **4c** | `GET /api/assignments/abc` | Không có body | **404 Not Found** | **404 Not Found** | Framework | Không khớp ràng buộc kiểu `:int` trên route; framework trả 404 body rỗng |
| **6a** | `GET /api/submissions/{id}` | Id lấy từ Location ca 5a | **200 OK** | **200 OK** | Handler | Tìm thấy lượt nộp; trả về JSON `SubmissionDto` |
| **6b** | `GET /api/submissions/{guid-rỗng}` | Guid `00000000-0000-...` | **404 Not Found** | **404 Not Found** | Handler | Không tìm thấy trong bộ nhớ; trả `ApiError: SUBMISSION_NOT_FOUND` |
| **9c** | `POST /api/assignments/1/submissions` | Gửi lại `sv001.json` lần 2 | **409 Conflict** | **409 Conflict** | Handler | [Mở rộng] Trùng lặp sinh viên nộp cùng bài; trả `ApiError: DUPLICATE_SUBMISSION` |
| **9d** | `DELETE /api/submissions/{id}` | Id của lượt nộp đã tạo | **204 No Content** | **204 No Content** | Handler | [Mở rộng] Rút lại bài nộp thành công; phản hồi có mã 204 và không có body |
| **9e** | `DELETE /api/submissions/{id}` | Gửi lại DELETE lần 2 | **404 Not Found** | **404 Not Found** | Handler | Lượt nộp đã bị xóa ở ca 9d; không còn tồn tại để xóa tiếp |

---

## 7. TRẢ LỜI 3 CÂU HỎI TRỌNG TÂM CỦA BÀI HỌC

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Nguyên lý thiết kế:**
  Trong phong cách kiến trúc REST (Representational State Transfer), mọi thực thể trong hệ thống được coi là các **tài nguyên (Resources)**. URI (Uniform Resource Identifier) được sinh ra để định danh và định vị các tài nguyên đó trong không gian mạng, vì vậy URI bắt buộc phải là **danh từ** (thường dùng danh từ số nhiều để chỉ tập hợp tài nguyên như `/api/assignments`, `/api/submissions`).
  Ngược lại, các thao tác hay hành động tác động lên tài nguyên đã được chuẩn hóa bởi giao thức HTTP thông qua bộ các **phương thức (HTTP Verbs)**:
  - `GET`: Đọc, truy vấn tài nguyên (an toàn và lũy đẳng).
  - `POST`: Tạo mới tài nguyên cấp dưới hoặc xử lý nghiệp vụ (không an toàn, không lũy đẳng).
  - `PUT`: Thay thế toàn bộ tài nguyên (lũy đẳng).
  - `DELETE`: Xóa tài nguyên (lũy đẳng).
- **So sánh đối chiếu giữa kiểu RPC (gọi hàm từ xa) và kiểu REST:**
  - *Kiểu RPC / Gọi hàm (cần tránh):* Đưa hành động (động từ) vào URI, ví dụ:
    - `GET /getAssignments`
    - `POST /submitAssignment`
    - `POST /deleteSubmission?id=...`
    Cách làm này làm bùng nổ số lượng URI, khó đoán, không tận dụng được cơ chế phân tầng (caching, proxies) của hạ tầng web.
  - *Kiểu RESTful (chuẩn mực trong bài):*
    - Xem danh sách: `GET /api/assignments`
    - Chi tiết bài tập: `GET /api/assignments/{id}`
    - Nộp bài tập 1: `POST /api/assignments/1/submissions`
    - Rút lượt nộp: `DELETE /api/submissions/{id}`
  - *Mối quan hệ cha - con (Sub-resource):*
    Khi một tài nguyên phụ thuộc vào tài nguyên cha, URI thể hiện rõ cấu trúc phân cấp: `/api/assignments/{assignmentId}/submissions`. Nhìn vào URI, người dùng và hệ thống biết ngay yêu cầu này tạo một lượt nộp nằm trong bài tập `{assignmentId}`, không cần phải mở body ra mới biết.

---

### Câu 2: Tính chất "Stateless" (Phi trạng thái) thể hiện ở đâu trong mã nguồn đã viết?
- **Nguyên lý:**
  Stateless là ràng buộc cốt lõi của REST: Máy chủ **không lưu trữ bất kỳ trạng thái phiên (session state)** nào của máy khách giữa các lần gửi yêu cầu. Mỗi yêu cầu HTTP gửi từ client lên server phải **tự mang đầy đủ mọi thông tin cần thiết** để server hiểu và xử lý độc lập.
- **Biểu hiện cụ thể trong mã nguồn của EduSubmit:**
  1. *Ngữ cảnh tài nguyên nằm trên URI:* Khi nộp bài, `assignmentId` được truyền trực tiếp trên URI (`/api/assignments/1/submissions`). Server không cần nhớ trước đó client vừa mở xem trang bài tập nào.
  2. *Dữ liệu nghiệp vụ tự thân trong Body:* Toàn bộ dữ liệu của lượt nộp (`studentCode`, `repositoryUrl`) được gói gọn hoàn chỉnh trong payload JSON của `CreateSubmissionRequest`. Máy chủ không cần lưu cookie hay phiên làm việc kiểu `Session["currentUser"] = "DTC245200494"`.
  3. *Không có trạng thái phụ thuộc:* Giả sử client gọi `POST` nộp bài ở bước 2, rồi 10 phút sau mới gọi `GET /api/submissions/{id}` ở bước 3, server vẫn xử lý bình thường vì mã Guid trên URI là định danh duy nhất toàn cầu, độc lập với kết nối TCP hay phiên trước đó.
  4. *Khả năng mở rộng ngang (Horizontal Scaling):* Do server hoàn toàn phi trạng thái, nếu triển khai hệ thống sau một bộ cân bằng tải (Load Balancer) với 5 máy chủ API chạy song song, yêu cầu nộp bài có thể vào Server A và yêu cầu đọc lại có thể vào Server B mà hệ thống vẫn hoạt động chính xác (miễn là dùng chung tầng lưu trữ).

---

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Nguyên lý Decoupling (Ghép nối lỏng lẻo):**
  Trong kiến trúc phân tán thực tế, client và server là hai tiến trình độc lập, thậm chí có thể được phát triển bởi các nhóm khác nhau, bằng các ngôn ngữ khác nhau (ví dụ: Server viết bằng C#, Mobile App viết bằng Flutter/Dart, Web Client viết bằng React/TypeScript).
  Họ **không bao giờ chia sẻ chung một assembly `.dll` hay project reference** (trong bài, `Submission.Client.csproj` hoàn toàn không tham chiếu tới `Submission.Api.csproj`).
- **Hợp đồng dữ liệu qua dây (Data Contract Over the Wire):**
  Phương tiện giao tiếp duy nhất giữa Client và Server là văn bản JSON được truyền qua mạng. Hai bên khớp nhau là nhờ **Hợp đồng định dạng JSON**:
  - Tên thuộc tính JSON (theo chuẩn `camelCase`: `id`, `assignmentId`, `studentCode`, `repositoryUrl`, `status`, `receivedAt`).
  - Kiểu dữ liệu tương thích (`string`, `int`, `guid`, `boolean`, `ISO 8601 datetime`).
- **Bảo mật và toàn vẹn nghiệp vụ qua việc tách riêng DTO:**
  Tại máy chủ, chúng ta có 2 DTO khác nhau cho cùng một thực thể lượt nộp:
  ```csharp
  // DTO đầu vào (Client gửi lên Server)
  record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);

  // DTO đầu ra (Server trả về cho Client)
  record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
  ```
  Nếu server và client dùng chung một lớp Model duy nhất có chứa thuộc tính `Status` hay `Id`, một máy khách gian lận có thể cố ý gửi payload:
  ```json
  {
    "id": "11111111-...",
    "studentCode": "DTC245200494",
    "repositoryUrl": "https://...",
    "status": "graded_10_points"
  }
  ```
  Nhờ việc tách riêng DTO đầu vào, server **không tin tưởng máy khách**. Server chỉ tiếp nhận `StudentCode` và `RepositoryUrl`. Còn `Id`, `Status` (khởi tạo là "received"), `AssignmentId` (lấy từ URL) và `ReceivedAt` (UtcNow) đều do **chính máy chủ sinh ra và kiểm soát**.

---

## 8. KẾT QUẢ CHẠY THỰC TẾ CỦA CHƯƠNG TRÌNH

### 1. Đầu ra khi chạy Máy chủ `Submission.Api`:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/hien1411/Documents/ứng dụng phân tán/Bai 02
```

### 2. Đầu ra khi chạy Máy khách `Submission.Client`:
```text
=== CLIENT EDUSUBMIT KET NOI TOI: http://localhost:5000 ===

--- [Nac 1] Liet ke danh sach bai tap ---
Danh sach bai tap:
  - [1] Bai tap REST API (Dang mo): Thiet ke endpoint...
  - [2] Bai tap Client (Dang mo): Viet console client...
  - [3] Bai tap Da Dong (Da dong): Bai tap nay da het han nop.

--- [Nac 2] Nop bai tap ---
Gui luot nop cho bai tap 1...
Da tao luot nop thanh cong!
  - Ma luot nop (Id): 04a37b53-7a0b-4f47-a244-54dab0560481
  - Ma sinh vien    : DTC245200494
  - Kho ma nguon    : https://github.com/dtc245200494-hue/CNTTK23C
  - Trang thai      : received
  - Thoi gian nhan  : 2026-11-12 08:26:02 UTC
  - Header Location : /api/submissions/04a37b53-7a0b-4f47-a244-54dab0560481

--- [Nac 3] Doc lai luot nop theo Location ---
Goi GET toi: /api/submissions/04a37b53-7a0b-4f47-a244-54dab0560481...
Doc lai thanh cong tu server:
  - Id          : 04a37b53-7a0b-4f47-a244-54dab0560481
  - AssignmentId: 1
  - Sinh vien   : DTC245200494
  - Repo URL    : https://github.com/dtc245200494-hue/CNTTK23C
  - Trang thai  : received

--- [Nac 4] Thu gui cac yeu cau loi ---
Ca 4.1: Gui body rong (thieu StudentCode & RepositoryUrl)...
Ma phan hoi: 400 BadRequest
  -> ApiError: [INVALID_SUBMISSION] Thieu ma sinh vien hoac kho ma nguon.

Ca 4.2: Nop bai vao bai tap 999 khong ton tai...
Ma phan hoi: 404 NotFound
  -> ApiError: [ASSIGNMENT_NOT_FOUND] Khong tim thay bai tap.

Ca 4.3: GET /api/assignments/999 (dung GetAsync, khong bi sap boi 404)...
Ma phan hoi: 404 NotFound
  -> ApiError: [ASSIGNMENT_NOT_FOUND] Khong tim thay bai tap.
```

### 3. Đầu ra khi kiểm tra khả năng xử lý lỗi mạng của Máy khách (kết nối sai cổng 5999):
```text
=== CLIENT EDUSUBMIT KET NOI TOI: http://localhost:5999 ===

--- [Nac 1] Liet ke danh sach bai tap ---

[LOI MANG] Khong the ket noi toi may chu tai http://localhost:5999.
Chi tiet: Connection refused (localhost:5999)
```

---
*Báo cáo hoàn thành ngày 12/11/2026 bởi sinh viên Hoàng Trang Hiên (DTC245200494).*

