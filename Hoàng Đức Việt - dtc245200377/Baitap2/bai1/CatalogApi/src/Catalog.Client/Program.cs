using System.Net;
using System.Net.Http.Json;

var baseUrl = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

// Nac 1: GET /api/products
Console.WriteLine("=== Nac 1: GET /api/products ===");
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products") ?? [];
foreach (var item in products)
{
    Console.WriteLine($"#{item.Id}: {item.Name} - {item.Description} ({item.Price:N0})");
}

// Nac 2: POST /api/products (hop le)
Console.WriteLine();
Console.WriteLine("=== Nac 2: POST /api/products ===");
var newProductRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
var createResponse = await http.PostAsJsonAsync("/api/products", newProductRequest);
if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");
    if (created is not null)
    {
        Console.WriteLine($"Created: #{created.Id} - {created.Name} ({created.Price:N0})");
    }
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Create failed ({(int)createResponse.StatusCode}): {error?.Code} - {error?.Message}");
}

// Nac 3: GET /api/products/1
Console.WriteLine();
Console.WriteLine("=== Nac 3: GET /api/products/1 ===");
var detailResponse = await http.GetAsync("/api/products/1");
if (detailResponse.IsSuccessStatusCode)
{
    var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Ten san pham #1: {detail?.Name}");
}
else
{
    var error = await detailResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Khong lay duoc san pham #1 ({(int)detailResponse.StatusCode}): {error?.Code} - {error?.Message}");
}

// Nac 4: POST /api/products (du lieu khong hop le)
Console.WriteLine();
Console.WriteLine("=== Nac 4: POST /api/products (invalid) ===");
var invalidRequest = new CreateProductRequest("", "", -1);
var invalidResponse = await http.PostAsJsonAsync("/api/products", invalidRequest);
Console.WriteLine($"Status: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var apiError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Error Code: {apiError?.Code}");
    Console.WriteLine($"Error Message: {apiError?.Message}");
}
else
{
    var rawBody = await invalidResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"Unexpected response: {rawBody}");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
