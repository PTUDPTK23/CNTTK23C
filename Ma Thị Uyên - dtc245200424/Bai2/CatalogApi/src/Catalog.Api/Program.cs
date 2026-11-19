var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Thêm dữ liệu mẫu[cite: 44]
var products = new List<ProductDto> {
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /: Kiểm tra máy chủ[cite: 39, 40]
app.MapGet("/", () => Results.Ok(new { service = "Catalog.Api", status = "running" }));

// Bước 3: API lấy danh sách sản phẩm[cite: 44]
app.MapGet("/api/products", () => Results.Ok(products));

// Bước 4: API lấy chi tiết một sản phẩm[cite: 46]
app.MapGet("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// Bước 5: API tạo mới sản phẩm (POST)[cite: 49, 50]
app.MapPost("/api/products", (CreateProductRequest request) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError("INVALID_PRODUCT", "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price);
    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// Khai báo các đối tượng dữ liệu DTO ở cuối tệp[cite: 43, 48]
record ProductDto (int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError (string Code, string Message);