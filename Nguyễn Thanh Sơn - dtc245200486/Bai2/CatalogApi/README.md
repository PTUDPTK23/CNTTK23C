# Báo Cáo Thực Hành: Xây Dựng RESTful API Cho Quản Lý Sản Phẩm (CatalogApi)
**Học phần**: Lập trình phân tán / Kiến trúc phần mềm phân tán  
**Sinh viên thực hiện**: Nguyễn Thanh Sơn  
**Mã sinh viên**: DTC245200486  
**Email**: dtc245200486@ictu.edu.vn  
**Môi trường phát triển**: .NET 10 SDK (macOS arm64), C# Minimal API & Console Client  
**Cổng dịch vụ**: `http://localhost:5100`

---

## 1. Giới Thiệu Kiến Trúc & Cấu Trúc Dự Án

Dự án `CatalogApi` được xây dựng theo chuẩn kiến trúc Client-Server RESTful sử dụng .NET 10 Minimal API. Dự án tách biệt hoàn toàn giữa hai thành phần:
- **`src/Catalog.Api`**: Dịch vụ Web API chạy trên cổng `http://localhost:5100`, quản lý danh mục sản phẩm trong bộ nhớ, cung cấp các endpoint chuẩn REST:
  - `GET /`: Kiểm tra trạng thái máy chủ (Health Check).
  - `GET /api/products`: Truy vấn danh sách tất cả sản phẩm.
  - `GET /api/products/{id:int}`: Lấy chi tiết một sản phẩm theo mã định danh (Id).
  - `POST /api/products`: Tiếp nhận, xác thực dữ liệu và tạo mới sản phẩm.
- **`src/Catalog.Client`**: Ứng dụng Console Client độc lập tiêu thụ API thông qua `HttpClient` theo chuỗi 4 nấc kiểm thử an toàn, kiểm tra mã trạng thái HTTP trước khi đọc thân dữ liệu và bắt lỗi phòng thủ mà không làm sập ứng dụng.

### Cấu trúc thư mục giải pháp:
```text
CatalogApi/
├── CatalogApi.slnx
├── .gitignore
├── sp-hop-le.json
├── sp-sai.json
├── requests.http
├── README.md
└── src/
    ├── Catalog.Api/
    │   ├── Catalog.Api.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Properties/
    │       └── launchSettings.json
    └── Catalog.Client/
        ├── Catalog.Client.csproj
        └── Program.cs
```

---

## 2. Nhật Ký Lệnh CLI & Kết Quả Đầu Ra Thực Tế (CLI Execution Logs)

### 2.1. Biên dịch toàn bộ Solution
```bash
$ cd /Users/nguyenthanhson/Desktop/CatalogApi
$ dotnet build
```
*Kết quả đầu ra*:
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  Catalog.Client -> /Users/nguyenthanhson/Desktop/CatalogApi/src/Catalog.Client/bin/Debug/net10.0/Catalog.Client.dll
  Catalog.Api -> /Users/nguyenthanhson/Desktop/CatalogApi/src/Catalog.Api/bin/Debug/net10.0/Catalog.Api.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.05
```

---

### 2.2. Khởi chạy Web API máy chủ
```bash
$ dotnet run --project src/Catalog.Api
```
*Kết quả đầu ra*:
```text
Using launch settings from /Users/nguyenthanhson/Desktop/CatalogApi/src/Catalog.Api/Properties/launchSettings.json...
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5100
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
```

---

### 2.3. Thực thi máy khách `Catalog.Client` chuỗi 4 nấc
```bash
$ dotnet run --project src/Catalog.Client -- http://localhost:5100
```
*Kết quả đầu ra*:
```text
=== CATALOG CLIENT BAT DAU GIAO TIEP ===

[Nac 1] Lay danh sach san pham:
Danh sach san pham:
 [1] Giao trinh Lap trinh phan tan - 150.000 VND
 [2] Tai lieu RESTful API - 90.000 VND
 [3] Bai tap thuc hanh .NET - 120.000 VND

[Nac 2] Gui yeu cau tao san pham moi:
Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

[Nac 3] Doc lai chi tiet san pham vua truy van:
Chi tiet san pham 1: Giao trinh Lap trinh phan tan (150.000 VND)

[Nac 4] Co y gui du lieu sai de kiem thu phong thu:
Thu gui du lieu sai: 400 BadRequest
INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.

=== HOAN THANH CHUOI 4 NAC AN TOAN ===
```

---

### 2.4. Kiểm thử các Endpoint qua cURL thực tế

#### Ca 1: Health check máy chủ
```bash
$ curl -i http://localhost:5100/
```
```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Thu, 12 Nov 2026 08:15:01 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"service":"Catalog.Api","status":"running"}
```

#### Ca 2: Tạo mới sản phẩm hợp lệ (`sp-hop-le.json`)
```bash
$ curl -i -X POST http://localhost:5100/api/products \
  -H "Content-Type: application/json" \
  -d @sp-hop-le.json
```
```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Thu, 12 Nov 2026 08:15:15 GMT
Server: Kestrel
Location: /api/products/5
Transfer-Encoding: chunked

{"id":5,"name":"Lap trinh gRPC","description":"Tai lieu bai 5","price":80000}
```

#### Ca 3: Tạo mới sản phẩm vi phạm quy tắc nghiệp vụ (`sp-sai.json`)
```bash
$ curl -i -X POST http://localhost:5100/api/products \
  -H "Content-Type: application/json" \
  -d @sp-sai.json
```
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
Date: Thu, 12 Nov 2026 08:15:20 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"code":"INVALID_PRODUCT","message":"Ten san pham khong duoc de trong va gia phai lon hon 0."}
```

#### Ca 4: Truy vấn sản phẩm không tồn tại (`Id = 999`)
```bash
$ curl -i http://localhost:5100/api/products/999
```
```http
HTTP/1.1 404 Not Found
Content-Type: application/json; charset=utf-8
Date: Thu, 12 Nov 2026 08:15:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"code":"PRODUCT_NOT_FOUND","message":"Khong tim thay san pham."}
```

#### Ca 5: Sai kiểu dữ liệu tham số route (`Id = abc`)
```bash
$ curl -i http://localhost:5100/api/products/abc
```
```http
HTTP/1.1 404 Not Found
Content-Length: 0
Date: Thu, 12 Nov 2026 08:15:45 GMT
Server: Kestrel
```

#### Ca 6: Gửi yêu cầu POST thiếu Header `Content-Type`
```bash
$ curl -i -X POST http://localhost:5100/api/products -d @sp-hop-le.json
```
```http
HTTP/1.1 415 Unsupported Media Type
Content-Length: 0
Date: Thu, 12 Nov 2026 08:15:50 GMT
Server: Kestrel
```

#### Ca 7: Gọi sai phương thức HTTP (`PUT /api/products/1`)
```bash
$ curl -i -X PUT http://localhost:5100/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"test"}'
```
```http
HTTP/1.1 405 Method Not Allowed
Content-Length: 0
Date: Thu, 12 Nov 2026 08:16:01 GMT
Server: Kestrel
Allow: GET
```

---

## 3. Bảng Phá Thử (Chaos Testing Matrix)

Bảng phân tích đối chiếu giữa kịch bản gửi sai, mã lỗi kỳ vọng, mã phản hồi thực tế và thành phần chịu trách nhiệm xử lý (Handler nghiệp vụ vs Kestrel/ASP.NET Core Framework):

| # | Ca kiểm thử | Yêu cầu gửi | Mã kỳ vọng | Mã thực tế | Thành phần xử lý | Lý giải kỹ thuật chi tiết |
|---|---|---|---|---|---|---|
| 1 | **Tạo mới hợp lệ** | `POST /api/products` kèm `sp-hop-le.json` | `201 Created` | `201 Created` | **Handler** | Dữ liệu hợp lệ vượt qua kiểm tra biên, Server tự sinh Id = Max(Id)+1, thêm vào bộ nhớ và trả về kèm Header `Location: /api/products/{id}`. |
| 2 | **Tên rỗng / Giá âm** | `POST /api/products` kèm `sp-sai.json` | `400 Bad Request` | `400 Bad Request` | **Handler** | Handler kiểm tra `string.IsNullOrWhiteSpace(request.Name) \|\| request.Price <= 0` vi phạm, trả về `ApiError("INVALID_PRODUCT", ...)`. |
| 3 | **Sản phẩm không có** | `GET /api/products/999` | `404 Not Found` | `404 Not Found` | **Handler** | Khớp route `:int`, Handler tìm kiếm `FirstOrDefault` trả về `null` và chủ động trả `ApiError("PRODUCT_NOT_FOUND", ...)`. |
| 4 | **Sai kiểu route** | `GET /api/products/abc` | `404 Not Found` | `404 Not Found` | **Framework** | Ràng buộc tuyến `:int` từ chối chuỗi `abc`, ASP.NET routing ngắt ngay trước khi gọi Handler, trả về thân rỗng (`Content-Length: 0`). |
| 5 | **Thiếu Content-Type** | `POST /api/products` thiếu header | `415 Unsupported Media Type` | `415 Unsupported Media Type` | **Framework** | Kestrel middleware yêu cầu định danh định dạng thân yêu cầu để parse JSON; thiếu header bị từ chối ngay tại tầng biên HTTP. |
| 6 | **JSON sai cú pháp** | `POST /api/products` JSON đứt gãy | `400 Bad Request` | `400 Bad Request` | **Framework** | `System.Text.Json` gặp lỗi cú pháp khi giải mã payload sang DTO `CreateProductRequest`, ném `BadHttpRequestException`. |
| 7 | **Sai phương thức** | `PUT /api/products/1` | `405 Method Not Allowed` | `405 Method Not Allowed` | **Framework** | Tuyến URL có tồn tại nhưng Server chỉ đăng ký phương thức `GET`, Routing engine trả về 405 kèm header `Allow: GET`. |
| 8 | **Máy chủ không bật** | Client gọi khi API tắt | `Connection Refused` | `Connection Refused` | **OS Socket Stack** | Tầng mạng ném ngoại lệ `HttpRequestException: Connection refused (localhost:5100)` do không có tiến trình nào lắng nghe cổng. |

---

## 4. Giải Đáp Đầy Đủ 3 Câu Hỏi Lý Thuyết Trọng Tâm

### Câu hỏi 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
> **Trả lời**:  
> - **Nguyên lý cốt lõi của REST**: Trong kiến trúc REST (Representational State Transfer) do Roy Fielding đề xuất, **URI (Uniform Resource Identifier)** được sinh ra với vai trò duy nhất là **định danh tài nguyên** (Resource). Vì tài nguyên đại diện cho các thực thể hoặc tập hợp thực thể trong miền nghiệp vụ (như sản phẩm, người dùng, bài tập), chúng bắt buộc phải được đặt tên bằng các **danh từ** (ví dụ: `/api/products`, `/api/users`).
> - **Chuẩn hóa thao tác bằng HTTP Verbs**: Các hành động thao tác trên tài nguyên đã được chuẩn hóa trong đặc tả giao thức HTTP:
>   - `GET`: Lấy biểu diễn của tài nguyên (Safe - an toàn, Idempotent - bất biến khi lặp lại).
>   - `POST`: Tạo mới tài nguyên con bên dưới tập hợp (Unsafe, Non-idempotent).
>   - `PUT`: Thay thế hoặc cập nhật toàn bộ tài nguyên (Idempotent).
>   - `DELETE`: Xóa tài nguyên (Idempotent).
> - **Minh chứng từ dự án `CatalogApi`**:
>   Thay vì thiết kế dạng RPC (Remote Procedure Call) tùy tiện như `/createProduct`, `/getProductById?id=1`, `/deleteProduct`, hệ thống quy tụ về một danh từ duy nhất `/api/products`:
>   - `GET /api/products`: Đọc toàn bộ danh sách.
>   - `POST /api/products`: Thêm mới một sản phẩm.
>   - `GET /api/products/1`: Đọc chi tiết sản phẩm 1.
>   Thiết kế này giúp giao diện API trực quan, đơn giản hóa cấu trúc định tuyến, giảm thiểu phân mảnh endpoint và tận dụng tối đa hạ tầng cache/proxy có sẵn của Internet.

---

### Câu hỏi 2: Tính chất "Stateless" (Phi trạng thái) thể hiện ở đâu trong mã nguồn?
> **Trả lời**:  
> - **Định nghĩa Stateless trong REST**: Máy chủ **không lưu trữ bất kỳ trạng thái phiên (Session state) hay ngữ cảnh hội thoại nào của máy khách** giữa các yêu cầu. Mỗi yêu cầu HTTP gửi từ Client lên Server phải tự chứa đựng đầy đủ 100% dữ liệu cần thiết để Server có thể phân tích và xử lý độc lập.
> - **Thể hiện trong mã nguồn `CatalogApi`**:
>   1. **Trong `POST /api/products`**: Toàn bộ thông tin sản phẩm (`Name`, `Description`, `Price`) được đóng gói đầy đủ trong body JSON của `CreateProductRequest`. Máy chủ không cần biết trước đó client là ai, đã gửi những gì; nó chỉ đọc body, thẩm định, ghi vào danh sách và trả về phản hồi là kết thúc giao dịch.
>   2. **Trong `GET /api/products/{id:int}`**: Yêu cầu truyền trực tiếp định danh sản phẩm ngay trên đường dẫn URL. Máy chủ không cần duy trì "sản phẩm đang được chọn" trong bộ nhớ phiên.
>   3. **Không dùng Cookie / Session ID**: Máy chủ Kestrel không thiết lập cookie phiên (`Set-Cookie`) và không giữ `HttpSession`.
> - **Ý nghĩa kiến trúc**: Tính chất Stateless cho phép hệ thống mở rộng quy mô theo chiều ngang (Scale-out) cực kỳ dễ dàng: ta có thể đặt 10 máy chủ `Catalog.Api` phía sau một bộ cân bằng tải (Load Balancer), và bất kỳ máy chủ nào cũng có thể xử lý trơn tru mọi yêu cầu của client mà không cần đồng bộ phiên bộ nhớ.

---

### Câu hỏi 3: Vì sao mỗi bên (API và Client) khai báo DTO riêng?
> **Trả lời**:  
> - **1. Tách biệt ranh giới và đảm bảo tính Ghép nối lỏng (Loose Coupling)**:
>   Trong kiến trúc phân tán thực thụ, Client và Server là hai hệ thống độc lập, có thể được viết bởi hai ngôn ngữ khác nhau và có chu kỳ phát hành khác nhau. DTO (Data Transfer Object) bản chất là **hợp đồng truyền tải dữ liệu qua dây mạng (Data Contract / Wire Contract)** dưới định dạng trung gian (như JSON), hoàn toàn không phải là một lớp thư viện nhị phân dùng chung (Shared Binary DLL). Nếu cả hai bên dùng chung một file `.csproj` chứa DTO, thì mỗi khi Server thay đổi mã nguồn, Client sẽ bị buộc phải cập nhật và biên dịch lại, phá vỡ tính độc lập của kiến trúc phân tán.
> - **2. Phân định rõ ràng trách nhiệm giữa Dữ liệu vào (Input) và Dữ liệu ra (Output)**:
>   - Phía Client khi tạo sản phẩm chỉ có thẩm quyền gửi `CreateProductRequest(Name, Description, Price)`, không có trường `Id`.
>   - Phía Server trả về `ProductDto(Id, Name, Description, Price)`, trong đó `Id` được sinh bởi máy chủ.
> - **3. Bảo mật kiến trúc (Chống Over-posting / Mass Assignment)**:
>   Việc tách riêng DTO ngăn chặn triệt để nguy cơ Client tự ý gửi thêm các trường nhạy cảm (như quyền quản trị viên, ngày tạo, trạng thái duyệt) mà Server không mong muốn tiếp nhận.

---

### Bổ sung: Bài học từ 3 Thử Thách Phân Tán (Slide 61–62)
- **Thử thách A: Vì sao gọi hàm nội bộ không bị `Connection refused`, còn gọi HTTP thì có thể?**  
  *Giải thích*: Lời gọi hàm nội bộ diễn ra trong cùng một không gian địa chỉ bộ nhớ (Address Space) của một tiến trình CPU duy nhất nên luôn tồn tại; trong khi đó, gọi qua HTTP là giao tiếp qua dây mạng tới một tiến trình độc lập hoàn toàn. Tiến trình bên kia có thể chưa khởi động, bị tắt đột ngột, sai cổng kết nối hoặc đứt cáp mạng.
- **Thử thách B: Máy khách chạy 2 lần thấy dữ liệu khác nhau, dữ liệu đó sống ở đâu?**  
  *Giải thích*: Trạng thái dữ liệu sống ở máy chủ (trong RAM của tiến trình `Catalog.Api`). Khi máy khách 1 thêm sản phẩm số 4, máy khách 2 chạy sau sẽ nhìn thấy sản phẩm số 4. Điều này chứng minh máy khách chỉ là bên quan sát tạm thời, toàn bộ trạng thái nguồn (Single Source of Truth) do máy chủ lưu giữ.
- **Thử thách C: Nếu máy khách đổi tên trường thành `Ten` thì có lỗi biên dịch không? Chạy ra gì?**  
  *Giải thích*: Hoàn toàn không có lỗi biên dịch lúc build vì Client và Server độc lập nhau. Nhưng khi chạy runtime, trường `Ten` sẽ nhận giá trị `null` do JSON trả về từ Server có khóa `"name"`. Điều này chứng minh trình biên dịch không thể bảo vệ hợp đồng dữ liệu qua mạng.

---

## 5. Lịch Sử Git Commit (Git Commit History)

Toàn bộ các bước thực hiện đều được commit độc lập với thông điệp rõ ràng, tuân thủ tác giả: `Nguyen Thanh Son <dtc245200486@ictu.edu.vn>`:

```text
4a0256f Buoc 7: hien thuc Catalog.Client voi 4 buoc kiem thu
a9150c1 Buoc 6: them requests.http kiem thu API
11c4ad6 Buoc 5: them POST /api/products va payload mau
97f4d98 Buoc 4: them GET /api/products/{id}
4cb9258 Buoc 3: them ProductDto va GET /api/products
1a9cbfd Buoc 2: them GET / kiem tra may chu
4eff6e0 Buoc 1: tao solution CatalogApi
```

---

## 6. Hướng Dẫn Tái Hiện & Kiểm Thử Toàn Diện (Reproducibility Guide)

1. **Biên dịch Solution**:
   ```bash
   dotnet build /Users/nguyenthanhson/Desktop/CatalogApi
   ```
2. **Khởi chạy Web API**:
   ```bash
   dotnet run --project /Users/nguyenthanhson/Desktop/CatalogApi/src/Catalog.Api
   ```
3. **Chạy kiểm thử Console Client (tại một Terminal khác)**:
   ```bash
   dotnet run --project /Users/nguyenthanhson/Desktop/CatalogApi/src/Catalog.Client -- http://localhost:5100
   ```
4. **Kiểm thử với REST Client trong VS Code / Visual Studio**:
   Mở tệp `requests.http` tại thư mục gốc và nhấn nút `Send Request` trên từng ca kiểm thử.
