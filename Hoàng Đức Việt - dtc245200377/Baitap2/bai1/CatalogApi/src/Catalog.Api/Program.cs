var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var gate = new Lock();
List<ProductDto> products =
[
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
];

app.MapGet("/", () => Results.Ok(new { service = "Catalog.Api", status = "running" }));

app.MapGet("/api/products", () =>
{
    lock (gate)
    {
        return Results.Ok(products.ToList());
    }
});

app.MapGet("/api/products/{id:int}", (int id) =>
{
    ProductDto? product;
    lock (gate)
    {
        product = products.FirstOrDefault(p => p.Id == id);
    }

    return product is not null
        ? Results.Ok(product)
        : Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."));
});

app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    ProductDto product;
    lock (gate)
    {
        var nextId = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1;
        product = new ProductDto(
            nextId,
            request.Name.Trim(),
            (request.Description ?? string.Empty).Trim(),
            request.Price);
        products.Add(product);
    }

    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
