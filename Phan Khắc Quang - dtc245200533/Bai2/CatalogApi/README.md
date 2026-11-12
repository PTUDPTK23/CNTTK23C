# Catalog API — Thực hành RESTful API

## Thông tin sinh viên

- **Họ và tên:** Phan Khắc Quang
- **Mã sinh viên:** dtc245200533
- **Lớp:** CNTT K23C
- **Học phần:** Phát triển ứng dụng phân tán

---

## 1. Mô tả dự án

Dự án gồm hai ứng dụng giao tiếp qua giao thức HTTP theo kiến trúc RESTful:
- **Catalog.Api:** Dịch vụ máy chủ REST API viết bằng ASP.NET Core Minimal API, quản lý danh mục sản phẩm và trả về dữ liệu định dạng JSON. Dữ liệu được lưu trong bộ nhớ (in-memory).
- **Catalog.Client:** Ứng dụng dòng lệnh (Console) sử dụng `HttpClient` để gọi API và thực hiện các thao tác theo 4 nấc kiểm thử.

---

## 2. Cấu trúc thư mục

```text
Bai2/
└── CatalogApi/
    ├── CatalogApi.slnx
    ├── .gitignore
    ├── README.md
    ├── requests.http
    ├── sp-hop-le.json
    ├── sp-sai.json
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

## 3. Hướng dẫn chạy chương trình

Mở terminal tại thư mục `Bai2/CatalogApi`:

### Bước 1: Biên dịch toàn bộ Solution

```powershell
dotnet build
```

Kỳ vọng: `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Bước 2: Chạy máy chủ API (Terminal 1)

```powershell
dotnet run --project .\src\Catalog.Api --urls http://localhost:5100
```

Máy chủ sẵn sàng khi xuất hiện dòng:
```text
Now listening on: http://localhost:5100
Application started. Press Ctrl+C to shut down.
```

### Bước 3: Chạy máy khách Console (Terminal 2)

Mở một terminal thứ hai tại `Bai2/CatalogApi`:

```powershell
dotnet run --project .\src\Catalog.Client -- http://localhost:5100
```

Máy khách thực hiện 4 nấc nghiệp vụ:
1. **Nấc 1:** Lấy và in toàn bộ danh sách sản phẩm hiện có (`GET /api/products`).
2. **Nấc 2:** Tạo mới sản phẩm hợp lệ (`POST /api/products`), kiểm tra mã phản hồi `201 Created` và đọc `Location` header.
3. **Nấc 3:** Đọc chi tiết sản phẩm ID 1 (`GET /api/products/1`).
4. **Nấc 4:** Cố ý gửi dữ liệu sai (tên rỗng, giá âm), nhận và in mã lỗi `400 Bad Request` kèm đối tượng `ApiError`.

---

## 4. Danh sách Endpoint API

| Phương thức | Endpoint | Chức năng | Mã trạng thái HTTP |
| :--- | :--- | :--- | :--- |
| **GET** | `/` | Kiểm tra máy chủ (Health check) | 200 OK |
| **GET** | `/api/products` | Lấy danh sách toàn bộ sản phẩm | 200 OK |
| **GET** | `/api/products/{id:int}` | Lấy chi tiết sản phẩm theo ID | 200 OK / 404 Not Found |
| **POST** | `/api/products` | Tạo mới sản phẩm | 201 Created / 400 Bad Request / 415 Unsupported Media Type |
| **PUT** | `/api/products/{id:int}` | Cập nhật thông tin sản phẩm | 200 OK / 400 Bad Request / 404 Not Found |
| **DELETE** | `/api/products/{id:int}` | Xóa sản phẩm theo ID | 204 No Content / 404 Not Found |

---

## 5. Bảng 8 ca kiểm thử với requests.http (Bước 6)

| STT | Phương thức | Endpoint | Mô tả ca kiểm thử | Kết quả mong đợi |
| :---: | :---: | :--- | :--- | :--- |
| **1** | GET | `/` | Kiểm tra máy chủ đang hoạt động | `200 OK` (status: running) |
| **2** | GET | `/api/products` | Lấy danh sách sản phẩm | `200 OK`, mảng JSON chứa các sản phẩm |
| **3** | GET | `/api/products/1` | Lấy sản phẩm có ID tồn tại | `200 OK`, thông tin sản phẩm ID = 1 |
| **4** | GET | `/api/products/999` | Lấy sản phẩm với ID không tồn tại | `404 Not Found` + đối tượng `ApiError` (`PRODUCT_NOT_FOUND`) |
| **5** | GET | `/api/products/abc` | ID không phải số nguyên (sai route constraint) | `404 Not Found` (body rỗng) |
| **6** | POST | `/api/products` | Tạo sản phẩm mới hợp lệ | `201 Created`, header `Location: /api/products/{id}` |
| **7** | POST | `/api/products` | Tạo sản phẩm với giá âm (dữ liệu sai) | `400 Bad Request` + `ApiError` (`INVALID_PRODUCT`) |
| **8** | POST | `/api/products` | Gửi request với `Content-Type: text/plain` (sai kiểu nội dung) | `415 Unsupported Media Type` |

---

## 6. Kiểm thử nhanh bằng lệnh curl

```powershell
# 1. Kiểm tra máy chủ
curl.exe -i http://localhost:5100/

# 2. Lấy danh sách sản phẩm
curl.exe -i http://localhost:5100/api/products

# 3. Lấy chi tiết sản phẩm 1
curl.exe -i http://localhost:5100/api/products/1

# 4. Tạo sản phẩm hợp lệ
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-hop-le.json"

# 5. Gửi sản phẩm sai (giá âm)
curl.exe -i -X POST http://localhost:5100/api/products -H "Content-Type: application/json" -d "@sp-sai.json"
```

---

## 7. Giải thích câu hỏi lý thuyết hệ phân tán

### 1. Vì sao máy khách có thể gặp lỗi Connection Refused?
- Khi ứng dụng máy khách gửi yêu cầu HTTP tới địa chỉ `http://localhost:5100`, hệ điều hành cần thiết lập kết nối TCP (3-way handshake) tới cổng 5100.
- Nếu tiến trình `Catalog.Api` chưa được khởi chạy, đã bị dừng, hoặc lắng nghe ở một cổng khác, không có tiến trình nào lắng nghe (listening) tại cổng 5100, ngăn xếp mạng TCP/IP sẽ gửi lại gói tin `RST` dẫn đến lỗi `Connection Refused`.

### 2. Dữ liệu sản phẩm được lưu ở đâu?
- Danh sách sản phẩm `products` được lưu trong bộ nhớ RAM của tiến trình máy chủ `Catalog.Api`.
- Máy khách không có quyền truy cập trực tiếp vào vùng nhớ này mà phải thông qua việc trao đổi các thông điệp HTTP chuẩn (serialization/deserialization JSON qua mạng).
- Khi tiến trình máy chủ tắt đi và khởi động lại, dữ liệu lưu tạm trong RAM sẽ trở về trạng thái khởi tạo ban đầu.

### 3. Nếu phía Client đổi tên thuộc tính `Name` thành `Ten` thì điều gì xảy ra?
- Ứng dụng vẫn có thể biên dịch thành công nếu cú pháp C# hợp lệ.
- Tuy nhiên, trong giao thức trao đổi, máy chủ trả về JSON có trường `"name"`.
- Trình phân giải JSON mặc định của .NET (`System.Text.Json`) khi ánh xạ vào thuộc tính `Ten` sẽ không tìm thấy khóa tương ứng (trừ khi có thuộc tính `[JsonPropertyName("name")]`), dẫn đến thuộc tính `Ten` nhận giá trị mặc định là `null`.
