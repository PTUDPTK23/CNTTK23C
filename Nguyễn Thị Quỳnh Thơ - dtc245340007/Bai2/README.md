# Báo cáo thực hành Bài 2: RESTful API cho tương tác client–server

## 1. Thông tin sinh viên & Bài nộp
- **Môn học:** Phát triển ứng dụng phân tán
- **Bài tập:** Bài 2 - RESTful API cho tương tác client–server
- **Solution:** `CatalogApi.slnx`

---

## 2. Nhật ký thực hiện & Lệnh đã chạy

### Bước 1: Dựng nền tảng (Solution & Projects)
- Tạo solution và 2 project máy chủ (Web API) + máy khách (Console App):
  ```powershell
  dotnet new sln -n CatalogApi
  dotnet new web -n Catalog.Api -o src/Catalog.Api
  dotnet new console -n Catalog.Client -o src/Catalog.Client
  dotnet sln add src/Catalog.Api/Catalog.Api.csproj
  dotnet sln add src/Catalog.Client/Catalog.Client.csproj
  dotnet new gitignore
  dotnet build
  ```
- Kết quả: `Build succeeded: 0 Warning(s), 0 Error(s)`.

### Bước 2: Endpoint kiểm tra máy chủ `GET /`
- Viết endpoint `GET /` trả về JSON `{"service":"Catalog.Api","status":"running"}`.
- Kiểm thử với `curl.exe -i http://localhost:5100/`:
  ```http
  HTTP/1.1 200 OK
  Content-Type: application/json; charset=utf-8
  {"service":"Catalog.Api","status":"running"}
  ```

### Bước 3: DTO và Endpoint danh sách `GET /api/products`
- Khai báo DTO `ProductDto`, danh sách trong RAM gồm 3 sản phẩm mẫu.
- Endpoint `GET /api/products` trả về mã `200 OK` kèm danh sách JSON.

### Bước 4: Endpoint chi tiết `GET /api/products/{id:int}`
- Hỗ trợ tham số định tuyến kiểu số nguyên `{id:int}`.
- Trả về:
  - `200 OK`: Khi tìm thấy sản phẩm.
  - `404 Not Found` kèm `ApiError { code: "PRODUCT_NOT_FOUND", message: "..." }`: Khi không tìm thấy id.
  - `404 Not Found` (body rỗng do framework): Khi truyền tham số không phải số (ví dụ `/abc`).

### Bước 5: Endpoint tạo mới `POST /api/products`
- Khai báo DTO riêng `CreateProductRequest` cho đầu vào (không chứa Id).
- Kiểm tra hợp lệ dữ liệu tại biên (tên rỗng hoặc giá <= 0 trả về `400 Bad Request` + `ApiError`).
- Sinh Id tự động `Max(p => p.Id) + 1`, lưu vào danh sách, trả về `201 Created` kèm header `Location: /api/products/{id}`.

### Bước 6: Tập kiểm thử `requests.http`
- Tạo file `requests.http` gom đầy đủ các ca kiểm thử: 200, 201, 400, 404, 405, 415.

### Bước 7 & 8: Máy khách `Catalog.Client` (4 nấc hoàn chỉnh)
- Sử dụng `HttpClient` gọi lần lượt 4 kịch bản:
  - **Nấc 1:** Lấy danh sách sản phẩm và in định dạng tiền tệ.
  - **Nấc 2:** Tạo mới sản phẩm, kiểm tra `StatusCode == HttpStatusCode.Created` trước khi giải mã body hoặc đọc header `Location`.
  - **Nấc 3:** Đọc lại chi tiết sản phẩm vừa tạo.
  - **Nấc 4:** Cố ý gửi dữ liệu sai để bắt mã `400 Bad Request` và đọc thông tin lỗi từ `ApiError`.

Kết quả chạy thực tế của máy khách:
```text
Danh sach san pham:
 [1] Giao trinh Lap trinh phan tan - 150,000 VND
 [2] Tai lieu RESTful API - 90,000 VND
 [3] Bai tap thuc hanh .NET - 120,000 VND

Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

Chi tiet san pham 1: Giao trinh Lap trinh phan tan

Thu gui du lieu sai: 400 BadRequest
INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.
```

---

## 3. Bảng phá thử nghiệm hệ phân tán (Bước 9)

| # | Thí nghiệm | Thao tác | Hiện tượng / Kết quả quan sát | Bài học rút ra |
|---|---|---|---|---|
| 1 | Chạy client lần 2 | Server giữ nguyên, chạy lại client | Danh sách có 4 sản phẩm; sản phẩm mới tạo có `Id = 5` | Trạng thái (state) nằm ở server, không nằm ở client. Dữ liệu thay đổi sau các thao tác ghi. |
| 2 | Tắt server | Tắt tiến trình API rồi chạy client | Báo lỗi `HttpRequestException: Connection refused` | Lỗi đặc trưng của hệ phân tán: bên kia có thể vắng mặt / ngừng hoạt động. |
| 3 | Khởi động lại server | Chạy lại API rồi GET danh sách | Danh sách quay về 3 sản phẩm ban đầu | Dữ liệu lưu trong RAM sẽ mất khi tiến trình kết thúc. |
| 4 | Sai cổng / URL | Gọi tới cổng sai (ví dụ 5999) | Báo lỗi `Connection refused (localhost:5999)` | Hai bên bắt buộc phải thống nhất địa chỉ dịch vụ (URL). |

---

## 4. Trả lời 3 câu hỏi giải thích

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:** Trong kiến trúc RESTful API, URI đại diện cho **tài nguyên** (Resource) - là các thực thể, danh từ trong hệ thống (ví dụ: `/api/products`, `/api/assignments`). Các hành động thao tác lên tài nguyên đó đã được chuẩn hóa bởi các động từ/phương thức của giao thức HTTP (GET để đọc, POST để tạo mới, PUT/PATCH để cập nhật, DELETE để xóa). 
- Nếu đưa động từ vào URI (như `/getProduct`, `/createProduct`), API sẽ mang tính chất "gọi hàm từ xa" (RPC), làm phá vỡ giao diện thống nhất (Uniform Interface) của REST và gây khó khăn cho việc quản lý bộ đệm (caching) cũng như mở rộng hệ thống.

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Trả lời:** Tính chất vô trạng thái (Stateless) thể hiện ở chỗ: **Mỗi yêu cầu từ client gửi lên server đều tự mang đầy đủ toàn bộ thông tin cần thiết để server hiểu và xử lý**, server không lưu bất kỳ trạng thái phiên (session) hay lịch sử cuộc hội thoại nào giữa các lần gọi.
- Minh chứng trong code:
  - Khi client gửi `POST /api/products`, toàn bộ dữ liệu cần tạo (`Name`, `Description`, `Price`) đều được đóng gói đầy đủ trong body JSON `CreateProductRequest`.
  - Khi client gửi `GET /api/products/1`, tham số định danh `id` được gửi trực tiếp qua URI. Server không cần biết trước đó client đã làm gì, có ai đăng nhập trên server hay không. Bất kỳ máy chủ API nào (dù được cân bằng tải) đều có thể xử lý được độc lập.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:** DTO (Data Transfer Object) chỉ đóng vai trò là bản đặc tả **hợp đồng trao đổi dữ liệu** (Data Contract) trên đường truyền mạng thông qua chuẩn trung gian là **JSON**. Hai chương trình `Catalog.Api` và `Catalog.Client` là hai tiến trình độc lập, không dùng chung bộ nhớ và không bắt buộc phải tham chiếu chung thư viện code.
- Nhờ serializer (như `System.Text.Json`), các thuộc tính trong C# record được ánh xạ tự động sang các trường JSON (theo quy ước camelCase). Miễn là tên các thuộc tính và kiểu dữ liệu tương thích với cấu trúc JSON quy ước, client và server hoàn toàn có thể tự định nghĩa DTO riêng mà dữ liệu vẫn được chuyển đổi một cách chính xác.

---

## 5. Phiếu tự kiểm tra (Bước 10)

| # | Yêu cầu kiểm tra | Mã mong đợi | Kết quả kiểm tra | Đánh giá |
|---|---|---|---|---|
| 1 | `GET /` | 200 OK | `{"service":"Catalog.Api","status":"running"}` | Đạt (✓) |
| 2 | `GET /api/products` | 200 OK | Trả về mảng JSON 3 sản phẩm ban đầu | Đạt (✓) |
| 3 | `GET /api/products/1` | 200 OK | Trả về thông tin chi tiết sản phẩm 1 | Đạt (✓) |
| 4 | `GET /api/products/999` | 404 Not Found | Trả về mã lỗi `PRODUCT_NOT_FOUND` kèm `ApiError` | Đạt (✓) |
| 5 | `GET /api/products/abc` | 404 Not Found | Tuyến không khớp (`:int`), trả về 404 body rỗng | Đạt (✓) |
| 6 | `POST /api/products` (hợp lệ) | 201 Created | Sinh Id tiếp theo, kèm header `Location` | Đạt (✓) |
| 7 | `POST /api/products` (giá âm / tên rỗng) | 400 Bad Request | Bị chặn ở biên, trả về `INVALID_PRODUCT` | Đạt (✓) |
| 8 | `POST /api/products` (thiếu Content-Type) | 415 Unsupported Media Type | Framework chặn trước handler | Đạt (✓) |
| 9 | Máy khách `Catalog.Client` chạy đủ 4 nấc | In đủ 4 nấc | Không sập khi gặp 400, in thông tin lỗi thân thiện | Đạt (✓) |
| 10 | Tắt máy chủ và chạy máy khách | Bắt ngoại lệ kết nối | Báo lỗi kết nối mạng (Connection refused) | Đạt (✓) |
