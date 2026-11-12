
# Bài tập 2 - RESTful API

## 1. CatalogAPI

### Catalog.Api
API quản lý sản phẩm.

Các chức năng:
- GET /api/products
- GET /api/products/{id}
- POST /api/products

### Catalog.Client
Console Client sử dụng HttpClient để gọi Catalog API.

### Chạy Catalog API
```powershell
dotnet run --project Catalog.Api --urls http://localhost:5100
```

### Chạy Catalog Client
```powershell
dotnet run --project Catalog.Client
```

## 2. Submission

### Submission.Api
API quản lý Assignment và Submission.

Các chức năng:
- GET /api/assignments
- GET /api/assignments/{id}
- POST /api/assignments/{id}/submissions
- GET /api/submissions/{id}

### Chạy Submission API
```powershell
dotnet run --project Submission.Api --urls http://localhost:5200
```

### Chạy Submission Client
```powershell
dotnet run --project Submission.Client
```

## 3. Kiểm thử API
Sử dụng REST Client trong Visual Studio Code:
- CatalogAPI/requests.http
- Submission/Submission.Api/Submission.http
