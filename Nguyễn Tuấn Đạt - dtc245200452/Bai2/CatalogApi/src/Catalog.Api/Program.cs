var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ (Slide 44)
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET / : Trạng thái hoạt động (Slide 39)
app.MapGet("/", () => Results.Ok(new { service = "Catalog.Api", status = "running" }));

// GET /api/products : Danh sách sản phẩm (Slide 44), hỗ trợ lọc keyword (Slide 104)
app.MapGet("/api/products", (string? keyword) =>
{
    var list = string.IsNullOrWhiteSpace(keyword)
        ? products
        : products.Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(list);
});

// GET /api/products/{id:int} : Chi tiết một sản phẩm (Slide 46)
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products : Tạo mới sản phẩm từ body JSON (Slide 49, 51, 104)
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiểm tra tại biên hệ thống
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    if (request.Name.Length > 100)
    {
        return Results.BadRequest(new ApiError(
            "NAME_TOO_LONG",
            "Ten san pham khong duoc vuot qua 100 ky tu."));
    }

    var nextId = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;
    var newProduct = new ProductDto(nextId, request.Name.Trim(), request.Description?.Trim() ?? "", request.Price);
    products.Add(newProduct);

    return Results.Created($"/api/products/{newProduct.Id}", newProduct);
});

// DELETE /api/products/{id:int} : Xóa sản phẩm (Slide 104)
app.MapDelete("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null)
    {
        return Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham de xoa."));
    }
    products.Remove(product);
    return Results.NoContent();
});

app.Run();

// DTO và Error records (Slide 43, 48)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
