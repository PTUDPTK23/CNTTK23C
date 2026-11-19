var builder = WebApplication.CreateBuilder(args);

// Bắt lỗi toàn cục để chuẩn hóa thông báo lỗi hệ thống
builder.Services.AddProblemDetails();

var app = builder.Build();

// Xử lý môi trường ngoại lệ
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// Dữ liệu mẫu trong bộ nhớ
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /api/products: danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int}: chi tiết một sản phẩm
app.MapGet("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products: tạo mới từ body JSON
app.MapPost("/api/products", (CreateProductRequest request) => {
    // Kiểm tra tại biên hệ thống
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? string.Empty,
        Price: request.Price);

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

// PUT /api/products/{id:int}: cập nhật sản phẩm theo ID
app.MapPut("/api/products/{id:int}", (int id, CreateProductRequest request) => {
    var existingProduct = products.FirstOrDefault(p => p.Id == id);
    if (existingProduct is null) {
        return Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham de cap nhat."));
    }

    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError("INVALID_PRODUCT", "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    // Vì ProductDto là record bất biến, ta tạo bản cập nhật và thay thế vào vị trí cũ
    var updatedProduct = new ProductDto(
        Id: id,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? string.Empty,
        Price: request.Price
    );

    var index = products.FindIndex(p => p.Id == id);
    products[index] = updatedProduct;

    return Results.Ok(updatedProduct);
});

// DELETE /api/products/{id:int}: xóa sản phẩm theo ID
app.MapDelete("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null) {
        return Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham de xoa."));
    }

    products.Remove(product);
    return Results.NoContent(); // Trả về 204 No Content khi xóa thành công
});

app.MapGet("/", () => Results.Ok(new {
    service = "Catalog.Api",
    status = "running"
}));

app.Run();

// DTO: hình dạng dữ liệu đi qua mạng
record ProductDto(int Id, string Name, string Description, decimal Price); // đầu ra
record CreateProductRequest(string Name, string? Description, decimal Price); // đầu vào
record ApiError(string Code, string Message);