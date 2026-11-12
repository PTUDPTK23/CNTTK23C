using System.Net.Http.Json;

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5100")
};

Console.WriteLine("=== 1. KIEM TRA MAY CHU (GET /) ===");
var health = await client.GetFromJsonAsync<HealthDto>("/");
Console.WriteLine($"Service: {health?.Service}, Status: {health?.Status}\n");

Console.WriteLine("=== 2. LAY DANH SACH SAN PHAM (GET /api/products) ===");
var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
foreach (var p in products ?? [])
{
    Console.WriteLine($"- [{p.Id}] {p.Name}: {p.Price:N0} VND");
}
Console.WriteLine();

Console.WriteLine("=== 3. TAO SAN PHAM MOI (POST /api/products) ===");
var newProduct = new CreateProductRequest("Thuc hanh HttpClient", "Goi API tu C#", 95000);
var postResponse = await client.PostAsJsonAsync("/api/products", newProduct);
Console.WriteLine($"Status POST: {(int)postResponse.StatusCode} {postResponse.StatusCode}");
var created = await postResponse.Content.ReadFromJsonAsync<ProductDto>();
Console.WriteLine($"Da tao: [{created?.Id}] {created?.Name} - Location: {postResponse.Headers.Location}\n");

Console.WriteLine("=== 4. DOC CHI TIET SAN PHAM KHONG TON TAI (GET /api/products/999) ===");
var notFoundResponse = await client.GetAsync("/api/products/999");
Console.WriteLine($"Status GET 999: {(int)notFoundResponse.StatusCode} {notFoundResponse.StatusCode}");
var error = await notFoundResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"Loi tra ve: {error?.Code} - {error?.Message}");

// DTO phía Client
record HealthDto(string Service, string Status);
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);