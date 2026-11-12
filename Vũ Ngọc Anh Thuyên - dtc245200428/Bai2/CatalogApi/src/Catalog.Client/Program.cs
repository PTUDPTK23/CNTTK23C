using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Bước 1: lấy và in danh sách.
var products =
    await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

Console.WriteLine("Danh sach san pham:");
foreach (var product in products ?? [])
{
    Console.WriteLine($"[{product.Id}] {product.Name} - {product.Price:N0} VND");
}

// Bước 2: tạo một sản phẩm.
var createRequest = new CreateProductRequest(
    "Nhap mon REST API", "Sach tham khao", 120000);

var createResponse =
    await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    Console.WriteLine("Tao san pham that bai:");
    await PrintApiErrorAsync(createResponse);
    return;
}

var created =
    await createResponse.Content.ReadFromJsonAsync<ProductDto>();

Console.WriteLine($"\nDa tao san pham: [{created?.Id}] {created?.Name}");

var location = createResponse.Headers.Location;
Console.WriteLine($"Doc lai tai: {location}");

// Bước 3: đọc lại sản phẩm vừa tạo theo Location.
if (location is not null)
{
    var readResponse = await http.GetAsync(location);

    if (readResponse.IsSuccessStatusCode)
    {
        var reread =
            await readResponse.Content.ReadFromJsonAsync<ProductDto>();

        Console.WriteLine($"Doc lai thanh cong: {reread?.Name}");
    }
    else
    {
        await PrintApiErrorAsync(readResponse);
    }
}

// Bước 4: cố ý gửi dữ liệu sai để kiểm tra client đọc được lỗi.
var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1));

Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} " +
    $"{invalidResponse.StatusCode}");

await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();

    if (error is null)
        Console.WriteLine($"HTTP {(int)response.StatusCode}: khong doc duoc ApiError");
    else
        Console.WriteLine($"{error.Code}: {error.Message}");
}

record ProductDto(int Id, string Name, string Description, decimal Price);

record CreateProductRequest(
    string Name, string? Description, decimal Price);

record ApiError(string Code, string Message);