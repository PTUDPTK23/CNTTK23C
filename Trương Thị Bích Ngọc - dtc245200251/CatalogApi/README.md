# Catalog API - Quản lý Danh mục Sản phẩm (RESTful API)

## 1. Thông tin sinh viên
- **Họ và tên**: Trương Thị Bích Ngọc
- **Mã sinh viên**: DTC245200251
- **Học phần**: Phát triển ứng dụng phân tán - K23C

---

## 2. Cấu trúc Solution
```text
CatalogApi/
├── CatalogApi.slnx
├── .gitignore
├── requests.http
├── sp-hop-le.json
├── sp-sai.json
├── README.md
└── src/
    ├── Catalog.Api/
    │   ├── Program.cs
    │   └── Catalog.Api.csproj
    └── Catalog.Client/
        ├── Program.cs
        └── Catalog.Client.csproj
```

---

## 3. Hợp đồng API (API Contract)
| Phương thức | URI | Mã thành công | Lỗi có thể gặp | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/` | 200 OK | — | Kiểm tra máy chủ đang chạy |
| `GET` | `/api/products` | 200 OK | — | Lấy danh sách toàn bộ sản phẩm |
| `GET` | `/api/products/{id}` | 200 OK | 404 PRODUCT_NOT_FOUND<br>404 rỗng (id không phải số) | Lấy chi tiết một sản phẩm theo Id |
| `POST` | `/api/products` | 201 Created + Location | 400 INVALID_PRODUCT<br>415 Unsupported Media Type | Thêm mới một sản phẩm |

---

## 4. Hướng dẫn chạy và kiểm thử

### Chạy Server (Catalog.Api)
```powershell
cd CatalogApi
dotnet run --project src/Catalog.Api --urls http://localhost:5100
```

### Chạy Client (Catalog.Client)
```powershell
cd CatalogApi
dotnet run --project src/Catalog.Client -- http://localhost:5100
```

Kết quả 4 nấc của máy khách:
1. **Nấc 1**: In danh sách 3 sản phẩm mẫu từ máy chủ.
2. **Nấc 2**: Gửi POST tạo sản phẩm mới, kiểm tra `StatusCode == 201 Created` trước khi đọc body, in đường dẫn đọc lại trong header `Location`.
3. **Nấc 3**: Đọc lại chi tiết sản phẩm 1 qua `GET /api/products/1`.
4. **Nấc 4**: Cố ý gửi dữ liệu không hợp lệ (tên rỗng, giá âm) để nhận `400 BadRequest` và đọc mã lỗi từ `ApiError`.
