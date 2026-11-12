# Catalog API — Thực hành RESTful API

## Thông tin sinh viên

- Họ tên: Hoàng Anh Tú
- MSSV: DTC245200321
- Lớp: CNTT K23C

## Mô tả

Ứng dụng gồm hai chương trình giao tiếp qua HTTP:

- Catalog.Api: máy chủ quản lý sản phẩm, trả dữ liệu JSON.
- Catalog.Client: máy khách console gọi API và in kết quả.

Dữ liệu được lưu tạm trong bộ nhớ máy chủ.

## Yêu cầu môi trường

- .NET 10 SDK.
- Visual Studio Code.
- Extension C# Dev Kit và REST Client.

## Cấu trúc thư mục

```text
CatalogApi/
├── CatalogApi.slnx
├── .gitignore
├── README.md
├── requests.http
├── sp-hop-le.json
├── sp-sai.json
└── src/
    ├── Catalog.Api/
    └── Catalog.Client/
```

## Hướng dẫn chạy

Mở terminal tại thư mục CatalogApi.

### Build

```powershell
dotnet build
```

Nếu API đang chạy, dừng bằng Ctrl+C trước khi build để tránh lỗi khóa tệp.

### Chạy máy chủ — Terminal 1

```powershell
dotnet run --project .\src\Catalog.Api --urls http://localhost:5256
```

Máy chủ sẵn sàng khi xuất hiện:

```text
Now listening on: http://localhost:5256
```

Giữ terminal này chạy trong khi sử dụng máy khách.

### Chạy máy khách — Terminal 2

Mở terminal khác tại thư mục CatalogApi:

```powershell
dotnet run --project .\src\Catalog.Client -- http://localhost:5256
```

Máy khách thực hiện bốn nấc:

1. Lấy và in danh sách sản phẩm.
2. Tạo sản phẩm, kiểm tra 201 trước khi đọc dữ liệu.
3. Đọc chi tiết sản phẩm ID 1.
4. Gửi dữ liệu sai, nhận và in lỗi 400 INVALID_PRODUCT.

Mỗi lần chạy Client thành công sẽ tạo thêm một sản phẩm.

## Các endpoint

| Phương thức | Đường dẫn              | Chức năng        | Mã phản hồi   |
| ----------- | ---------------------- | ---------------- | ------------- |
| GET         | /                      | Kiểm tra máy chủ | 200           |
| GET         | /api/products          | Lấy danh sách    | 200           |
| GET         | /api/products/{id:int} | Lấy chi tiết     | 200, 404      |
| POST        | /api/products          | Tạo sản phẩm     | 201, 400, 415 |

Tạo thành công trả 201 và header Location trỏ tới sản phẩm mới.

Lỗi nghiệp vụ có dạng:

```json
{
  "code": "INVALID_PRODUCT",
  "message": "Ten san pham khong duoc de trong va gia phai lon hon 0."
}
```

Lỗi do framework như sai kiểu ID hoặc kiểu nội dung không nhất thiết có dạng ApiError.

## Kiểm thử bằng REST Client

Chạy API, mở requests.http và bấm Send Request cho từng ca.

| STT | Ca kiểm tra                       | Kết quả mong đợi        |
| --- | --------------------------------- | ----------------------- |
| 1   | GET /                             | 200, trạng thái running |
| 2   | GET /api/products                 | 200, mảng sản phẩm      |
| 3   | GET /api/products/1               | 200, sản phẩm ID 1      |
| 4   | GET /api/products/999             | 404, PRODUCT_NOT_FOUND  |
| 5   | GET /api/products/abc             | 404, body rỗng          |
| 6   | POST dữ liệu hợp lệ               | 201, có Location        |
| 7   | POST giá âm                       | 400, INVALID_PRODUCT    |
| 8   | POST với Content-Type: text/plain | 415                     |

Sau khi API vừa khởi động, danh sách có 3 sản phẩm mẫu.
Các lần POST thành công sẽ làm tăng số sản phẩm.

## Kiểm thử bằng curl

Chạy tại thư mục CatalogApi khi máy chủ đang hoạt động.

### Lấy danh sách

```powershell
curl.exe -i http://localhost:5256/api/products
```

### Tạo sản phẩm hợp lệ

```powershell
curl.exe -i -X POST http://localhost:5256/api/products -H "Content-Type: application/json" -d "@sp-hop-le.json"
```

### Gửi giá âm

```powershell
curl.exe -i -X POST http://localhost:5256/api/products -H "Content-Type: application/json" -d "@sp-sai.json"
```

### Gửi không có header JSON

```powershell
curl.exe -i -X POST http://localhost:5256/api/products -d "@sp-hop-le.json"
```

## Thí nghiệm hệ phân tán

| Thí nghiệm                                          | Kết quả quan sát                    | Giải thích                                          |
| --------------------------------------------------- | ----------------------------------- | --------------------------------------------------- |
| Chạy Client lần hai khi API vẫn chạy                | Thấy sản phẩm cũ và tạo thêm ID mới | Trạng thái nằm ở máy chủ                            |
| Dừng API rồi chạy Client                            | Client gặp lỗi kết nối              | Máy chủ là tiến trình riêng, có thể không hoạt động |
| Khởi động lại API rồi GET danh sách                 | Chỉ còn 3 sản phẩm mẫu              | Dữ liệu trong bộ nhớ mất khi tiến trình dừng        |
| Client gọi localhost:5999 trong khi API chạy ở 5256 | Client gặp lỗi kết nối              | Hai bên phải thống nhất địa chỉ và cổng             |

## Giải thích

### Vì sao gọi API có thể bị Connection refused?

API chạy trong tiến trình khác. Nếu máy chủ chưa chạy, đã dừng
hoặc Client gọi sai cổng thì không thể thiết lập kết nối.

### Dữ liệu sản phẩm nằm ở đâu?

Danh sách products nằm trong bộ nhớ của Catalog.Api.
Client gửi yêu cầu HTTP để lấy dữ liệu, không trực tiếp đọc biến của API.

### Nếu Client đổi thuộc tính Name thành Ten thì sao?

Chương trình vẫn có thể biên dịch, nhưng JSON trả về có trường name,
không có trường ten. Với cách đọc JSON hiện tại, thuộc tính Ten
không nhận được tên sản phẩm và có thể mang giá trị null.

## Ảnh minh chứng

Bổ sung ảnh kết quả kiểm thử và thí nghiệm trong thư mục images.
