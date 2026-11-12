using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== KET NOI DEN API: {apiBaseUrl} ===\n");

// Nấc 1: Lấy và in danh sách sản phẩm
Console.WriteLine("--- NAC 1: Lay danh sach san pham (GET /api/products) ---");
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham hien co:");
foreach (var p in products ?? [])
{
    Console.WriteLine($"  [{p.Id}] {p.Name} - {p.Price:N0} VND ({p.Description})");
}

// Nấc 2: Tạo sản phẩm mới, kiểm tra 201 Created và đọc header Location
Console.WriteLine("\n--- NAC 2: Tao san pham moi (POST /api/products) ---");
var createRequest = new CreateProductRequest(
    "Nhap mon REST API",
    "Sach tham khao cho sinh vien",
    120000);

using var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Da tao san pham moi thanh cong (Status {(int)createResponse.StatusCode} {createResponse.StatusCode}):");
    Console.WriteLine($"  Id: {created?.Id}");
    Console.WriteLine($"  Name: {created?.Name}");
    Console.WriteLine($"  Price: {created?.Price:N0} VND");
    Console.WriteLine($"  Location Header: {createResponse.Headers.Location}");
}
else
{
    Console.WriteLine($"Tao that bai: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine(await createResponse.Content.ReadAsStringAsync());
    return;
}

// Nấc 3: Đọc chi tiết sản phẩm 1 (GET /api/products/1)
Console.WriteLine("\n--- NAC 3: Doc chi tiet san pham 1 (GET /api/products/1) ---");
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
if (detail is not null)
{
    Console.WriteLine($"Chi tiet san pham 1:");
    Console.WriteLine($"  Id: {detail.Id}");
    Console.WriteLine($"  Name: {detail.Name}");
    Console.WriteLine($"  Description: {detail.Description}");
    Console.WriteLine($"  Price: {detail.Price:N0} VND");
}

// Nấc 4: Cố ý gửi dữ liệu sai để nhận lỗi 400 Bad Request + ApiError
Console.WriteLine("\n--- NAC 4: Gui du lieu sai de kiem tra bat loi 400 (POST /api/products) ---");
var invalidRequest = new CreateProductRequest("", "San pham khong hop le", -1);
using var invalidResponse = await http.PostAsJsonAsync("/api/products", invalidRequest);

Console.WriteLine($"Ket qua nhan duoc: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var apiError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Da bat loi ApiError thanh cong:");
    Console.WriteLine($"  Code: {apiError?.Code}");
    Console.WriteLine($"  Message: {apiError?.Message}");
}
else
{
    Console.WriteLine(await invalidResponse.Content.ReadAsStringAsync());
}

Console.WriteLine("\n=== HOAN THANH KIEM THU 4 NAC ===");

// Khai báo các record DTO
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
