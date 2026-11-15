using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham:");
foreach (var product in products ?? [])
{
    Console.WriteLine($"  [{product.Id}] {product.Name} - {product.Price:N0} VND");
}

var createRequest = new CreateProductRequest(
    "Nhap mon REST API",
    "Sach tham khao",
    120000);
using var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>()
        ?? throw new InvalidDataException("API tra 201 nhung khong co ProductDto trong body.");
    Console.WriteLine($"\nDa tao san pham moi: [{created.Id}] {created.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

using var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1));
Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    if (error is null)
    {
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}");
        return;
    }

    Console.WriteLine($"{error.Code}: {error.Message}");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
