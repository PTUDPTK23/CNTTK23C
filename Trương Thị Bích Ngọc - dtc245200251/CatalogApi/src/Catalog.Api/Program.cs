var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Trang chủ kiểm tra trạng thái
app.MapGet("/", () => Results.Ok(new {
    service = "Catalog.Api",
    status = "running"
}));

// 2. Dữ liệu mẫu lưu trong bộ nhớ
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// 3. GET /api/products: Lấy danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

// 4. GET /api/products/{id}: Lấy chi tiết một sản phẩm theo ID
app.MapGet("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// 5. POST /api/products: Tạo sản phẩm mới
app.MapPost("/api/products", (CreateProductRequest request) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// DTOs & Errors
record ProductDto(int Id, string Name, string Description, decimal Price);
record ApiError(string Code, string Message);
record CreateProductRequest(string Name, string? Description, decimal Price);