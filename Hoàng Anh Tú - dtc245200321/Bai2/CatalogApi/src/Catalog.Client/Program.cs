using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5256";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Nấc 1: lấy danh sách sản phẩm
var products = await http.GetFromJsonAsync<List<ProductDto>>(
    "/api/products");

Console.WriteLine("Danh sach san pham:");

foreach (var p in products ?? [])
{
    Console.WriteLine(
        $"[{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// Nấc 2: tạo sản phẩm và kiểm tra mã trước khi đọc body
var createRequest = new CreateProductRequest(
    "Nhap mon REST API",
    "Sach tham khao",
    120000);

using var createResponse = await http.PostAsJsonAsync(
    "/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created =
        await createResponse.Content.ReadFromJsonAsync<ProductDto>();

    Console.WriteLine(
        $"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");

    Console.WriteLine(
        $"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    Console.WriteLine(
        $"\nTao that bai: {(int)createResponse.StatusCode}");

    Console.WriteLine(
        await createResponse.Content.ReadAsStringAsync());

    return;
}

// Nấc 3: đọc chi tiết sản phẩm 1 theo slide
var detail = await http.GetFromJsonAsync<ProductDto>(
    "/api/products/1");

Console.WriteLine(
    $"\nChi tiet san pham 1: {detail?.Name}");

// Nấc 4: cố ý gửi tên rỗng và giá âm
using var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1));

Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} " +
    $"{invalidResponse.StatusCode}");

if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var invalidError =
        await invalidResponse.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"{invalidError?.Code}: {invalidError?.Message}");
}
else
{
    Console.WriteLine(
        await invalidResponse.Content.ReadAsStringAsync());
}

// Khai báo kiểu ở cuối tệp
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price);

record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price);

record ApiError(string Code, string Message);