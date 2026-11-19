# BÁO CÁO THỰC HÀNH BÀI 2: RESTFUL API CHO TƯƠNG TÁC CLIENT-SERVER
## HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN (ICTU)
### Sinh viên: Hoàng Trang Hiên - MSSV: dtc245200494 - Lớp: K23C

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
- **Vị trí bài nộp:** `Hoàng Trang Hiên - dtc245200494/Bai2`

---

## 2. CẤU TRÚC TỔNG THỂ THƯ MỤC BÀI 2

Thư mục `Bai2` bao gồm đầy đủ cả hai phần theo yêu cầu bài học:
1. **Phần 2: Ứng dụng Catalog API (`CatalogApi/`)** theo đúng Bước 1–8 của bài giảng (Máy chủ `Catalog.Api` cổng 5100, máy khách `Catalog.Client` 4 nấc, và tệp `requests.http` 8 ca).
2. **Phần 3: Ứng dụng EduSubmit (`RestfulClientServer.slnx` & `src/`)** (Máy chủ `Submission.Api` cổng 5000, máy khách `Submission.Client` 4 nấc, và tệp `requests.http` kiểm thử toàn diện).

```text
Bai2/
├── CatalogApi/                             # PHẦN 2: CATALOG API (Cổng 5100)
│   ├── CatalogApi.slnx                     # Solution .NET 10 cho CatalogApi
│   ├── .gitignore                          # Loại bỏ bin/, obj/
│   ├── requests.http                       # Bộ 8 ca kiểm thử HTTP cho Catalog API
│   └── src/
│       ├── Catalog.Api/                    # Máy chủ Catalog.Api (Cổng 5100)
│       │   ├── Program.cs                  # 4 endpoint: GET /, GET /api/products, GET /api/products/{id}, POST /api/products
│       │   ├── Catalog.Api.csproj
│       │   └── Properties/launchSettings.json
│       └── Catalog.Client/                 # Máy khách Console Catalog.Client (Chuỗi 4 nấc)
│           ├── Program.cs
│           └── Catalog.Client.csproj
├── RestfulClientServer.slnx                # PHẦN 3: EDUSUBMIT API & CLIENT (Cổng 5000)
├── requests.http                           # Bộ 17 ca test HTTP chi tiết cho EduSubmit
├── sv001.json                              # Payload nộp bài hợp lệ
├── rong.json                               # Payload rỗng kiểm thử lỗi 400
├── README.md                               # Báo cáo thực hành chi tiết
└── src/
    ├── Submission.Api/                     # Máy chủ EduSubmit (Cổng 5000)
    │   ├── Program.cs                      # 4 endpoint + 3 cổng kiểm tra + bước 9 mở rộng
    │   ├── Submission.Api.csproj
    │   └── Properties/launchSettings.json
    └── Submission.Client/                  # Máy khách EduSubmit Console
        ├── Program.cs                      # Chuỗi 4 nấc gọi API EduSubmit
        └── Submission.Client.csproj
```

---

## 3. PHẦN 2: ỨNG DỤNG CATALOG API (BƯỚC 1–8)

### 3.1. Hợp đồng API của Catalog API (Cổng 5100)
| Phương thức | Tuyến URI | Mã thành công | Mã lỗi | Mô tả |
|:---|:---|:---|:---|:---|
| **GET** | `/` | 200 OK | – | Kiểm tra máy chủ hoạt động |
| **GET** | `/api/products` | 200 OK | – | Danh sách sản phẩm (3 sản phẩm ban đầu) |
| **GET** | `/api/products/{id:int}` | 200 OK | 404 PRODUCT_NOT_FOUND | Chi tiết một sản phẩm theo Id |
| **POST** | `/api/products` | 201 Created (+ Location) | 400 INVALID_PRODUCT<br>415 Unsupported Media Type | Tạo mới sản phẩm (giá > 0, tên không rỗng) |

### 3.2. Bộ 8 ca kiểm thử trong `CatalogApi/requests.http`
1. `GET {{api}}/` $\rightarrow$ 200 OK
2. `GET {{api}}/api/products` $\rightarrow$ 200 OK (3 sản phẩm)
3. `GET {{api}}/api/products/1` $\rightarrow$ 200 OK
4. `GET {{api}}/api/products/999` $\rightarrow$ 404 Not Found (`PRODUCT_NOT_FOUND`)
5. `GET {{api}}/api/products/abc` $\rightarrow$ 404 Not Found (Framework trả, body rỗng)
6. `POST {{api}}/api/products` hợp lệ $\rightarrow$ 201 Created kèm header `Location: /api/products/4`
7. `POST {{api}}/api/products` giá âm $\rightarrow$ 400 Bad Request (`INVALID_PRODUCT`)
8. `POST {{api}}/api/products` thiếu Content-Type $\rightarrow$ 415 Unsupported Media Type

### 3.3. Kết quả chạy máy khách `Catalog.Client` (4 nấc)
```text
=== CATALOG CLIENT KET NOI TOI: http://localhost:5100 ===

--- [Nac 1] Danh sach san pham ---
  [1] Giao trinh Lap trinh phan tan - 150,000 VND
  [2] Tai lieu RESTful API - 90,000 VND
  [3] Bai tap thuc hanh .NET - 120,000 VND

--- [Nac 2] Tao san pham moi ---
Da tao san pham moi: [4] Nhap mon REST API
Doc lai tai: /api/products/4

--- [Nac 3] Doc lai chi tiet san pham 1 ---
Chi tiet san pham 1: [1] Giao trinh Lap trinh phan tan - 150,000 VND

--- [Nac 4] Thu gui du lieu sai ---
Thu gui du lieu sai: 400 BadRequest
  -> INVALID_PRODUCT: Ten san pham khong duoc de trong va gia phai lon hon 0.
```

---

## 4. PHẦN 3: ỨNG DỤNG EDUSUBMIT (ÁP DỤNG & MỞ RỘNG)

### 4.1. Hợp đồng API của EduSubmit (Cổng 5000)
| Phương thức | Tuyến URI | Mã thành công | Mã lỗi có thể gặp | Mô tả chức năng |
|:---|:---|:---|:---|:---|
| **GET** | `/` | 200 OK | – | Kiểm tra trạng thái hoạt động của API |
| **GET** | `/api/assignments` | 200 OK | – | Lấy danh sách tất cả bài tập |
| **GET** | `/api/assignments/{assignmentId:int}` | 200 OK | 404 ASSIGNMENT_NOT_FOUND | Lấy chi tiết bài tập |
| **POST** | `/api/assignments/{assignmentId:int}/submissions` | 201 Created (+ Location) | 404 ASSIGNMENT_NOT_FOUND<br>400 ASSIGNMENT_CLOSED<br>400 INVALID_SUBMISSION<br>409 DUPLICATE_SUBMISSION | Nộp bài qua 3 cổng kiểm tra + chống trùng lặp |
| **GET** | `/api/submissions/{submissionId:guid}` | 200 OK | 404 SUBMISSION_NOT_FOUND | Đọc lại chi tiết lượt nộp theo Id trong Location |
| **DELETE** | `/api/submissions/{submissionId:guid}` | 204 No Content | 404 SUBMISSION_NOT_FOUND | Rút lại (hủy) lượt nộp đã tạo (Bước 9 mở rộng) |

### 4.2. Bảng phá thử (Testing Matrix - 8 ca bước 5 + mở rộng)
| Ca | Phương thức & Tuyến URL | Tình huống / Payload | Mã mong đợi | Mã thực tế | Thành phần trả lời |
|:---|:---|:---|:---:|:---:|:---|
| **5a** | `POST /api/assignments/1/submissions` | `sv001.json` (Hợp lệ) | **201 Created** | **201 Created** | Handler (kèm Location) |
| **5b** | `POST /api/assignments/1/submissions` | `rong.json` (`studentCode`: `""`) | **400 Bad Request** | **400 Bad Request** | Handler (`INVALID_SUBMISSION`) |
| **5c** | `POST /api/assignments/999/submissions` | `sv001.json` | **404 Not Found** | **404 Not Found** | Handler (`ASSIGNMENT_NOT_FOUND`) |
| **5d** | `POST /api/assignments/1/submissions` | Bỏ header `Content-Type` | **415 Unsupported Media Type** | **415 Unsupported Media Type** | Framework |
| **5e** | `POST /api/assignments/1/submissions` | JSON dị tật (`{"studentCode": "SV001",`) | **400 Bad Request** | **400 Bad Request** | Framework |
| **5f** | `POST /api/assignments/1/submissions` | Thiếu trường `repositoryUrl` | **400 Bad Request** | **400 Bad Request** | Handler (`INVALID_SUBMISSION`) |
| **5g** | `PUT /api/assignments/1` | Không có body | **405 Method Not Allowed** | **405 Method Not Allowed** | Framework (`Allow: GET`) |
| **5h** | `POST /api/assignments/3/submissions` | Nộp vào bài tập đã đóng (`IsOpen == false`) | **400 Bad Request** | **400 Bad Request** | Handler (`ASSIGNMENT_CLOSED`) |
| **9c** | `POST /api/assignments/1/submissions` | Nộp lại cùng bài tập lần 2 | **409 Conflict** | **409 Conflict** | Handler (`DUPLICATE_SUBMISSION`) |
| **9d** | `DELETE /api/submissions/{id}` | Rút lại lượt nộp đã tạo | **204 No Content** | **204 No Content** | Handler (Body rỗng) |

---

## 5. TRẢ LỜI 3 CÂU HỎI TRỌNG TÂM

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Nguyên lý:** Trong REST, URI đóng vai trò định danh **tài nguyên (Resources)**, vì thế URI bắt buộc là **danh từ** (như `/api/products`, `/api/assignments`, `/api/submissions`). Các hành động thao tác đã được chuẩn hóa bởi các phương thức HTTP (`GET` để truy vấn, `POST` để tạo mới, `PUT` để cập nhật, `DELETE` để xóa).
- **So sánh:** Kiểu gọi hàm từ xa (RPC) thường gắn động từ vào URI (như `/submitAssignment`, `/getProducts`) làm bùng nổ URI và phá vỡ tính nhất quán. Kiểu RESTful dùng `POST /api/assignments/1/submissions` vừa thể hiện rõ quan hệ cha-con (lượt nộp thuộc bài tập 1) vừa tận dụng được các chuẩn phân tầng của HTTP.

### Câu 2: Tính chất "Stateless" (Phi trạng thái) thể hiện ở đâu trong mã nguồn?
- **Nguyên lý:** Máy chủ không lưu bất kỳ phiên làm việc (`Session`) nào của máy khách giữa hai lần gọi. Mỗi request tự mang đầy đủ ngữ cảnh để máy chủ xử lý độc lập.
- **Minh chứng code:** Khi client gửi `POST /api/assignments/1/submissions`, tham số `assignmentId` nằm trực tiếp trên URL, còn mã sinh viên và link repo nằm trọn vẹn trong body JSON `CreateSubmissionRequest`. Máy chủ không cần lưu cookie hay session để nhớ client là ai.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Nguyên lý ghép nối lỏng lẻo (Decoupling):** Client và server là hai tiến trình độc lập, không chia sẻ DLL hay tham chiếu project của nhau. Hai bên khớp nhau thông qua **Hợp đồng dữ liệu JSON qua đường truyền (Over the wire)**: tên thuộc tính (chuẩn `camelCase`) và kiểu dữ liệu tương thích.
- **An toàn nghiệp vụ:** Việc tách riêng `CreateSubmissionRequest` (chỉ có `StudentCode`, `RepositoryUrl`) giúp ngăn chặn client can thiệp trái phép vào các trường do server quản lý như `Id` (sinh bởi `Guid.NewGuid()`), `Status` (khởi tạo `"received"`), hay `ReceivedAt` (UtcNow).

---

## 6. HƯỚNG DẪN CHẠY VÀ KIỂM THỬ

### 1. Chạy Catalog API (Phần 2 - Cổng 5100)
```bash
# Terminal 1: Chạy Server
cd "/home/hien1411/Documents/ứng dụng phân tán/Bai 02/CatalogApi"
dotnet run --project src/Catalog.Api --urls http://localhost:5100

# Terminal 2: Chạy Client
cd "/home/hien1411/Documents/ứng dụng phân tán/Bai 02/CatalogApi"
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

### 2. Chạy EduSubmit (Phần 3 - Cổng 5000)
```bash
# Terminal 1: Chạy Server
cd "/home/hien1411/Documents/ứng dụng phân tán/Bai 02"
dotnet run --project src/Submission.Api --urls http://localhost:5000

# Terminal 2: Chạy Client
cd "/home/hien1411/Documents/ứng dụng phân tán/Bai 02"
dotnet run --project src/Submission.Client -- http://localhost:5000
```

---
*Báo cáo hoàn thiện ngày 12/11/2026 bởi sinh viên Hoàng Trang Hiên (dtc245200494).*
