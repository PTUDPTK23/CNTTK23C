var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan",
        "Giao trinh chinh thuc", 150000),

    new(2, "Tai lieu RESTful API",
        "Huong dan thiet ke API", 90000),

    new(3, "Bai tap thuc hanh .NET",
        "Bo bai tap kem loi giai", 120000)
};

// Kiểm tra máy chủ
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// Lấy danh sách
app.MapGet("/api/products", () => Results.Ok(products));

// Lấy chi tiết
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."))
        : Results.Ok(product);
});

// Tạo sản phẩm mới
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Từ chối tên rỗng hoặc giá không dương
    if (string.IsNullOrWhiteSpace(request.Name)
        || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    // Máy chủ tự sinh Id
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price);

    products.Add(product);

    // Trả 201, Location và sản phẩm vừa tạo
    return Results.Created(
        $"/api/products/{product.Id}", product);
});

app.Run();

// DTO đầu ra
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price);

// DTO đầu vào: không có Id vì máy chủ tự sinh
record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price);

// Dạng lỗi chung
record ApiError(string Code, string Message);