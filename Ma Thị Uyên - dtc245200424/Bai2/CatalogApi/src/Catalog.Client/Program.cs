using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Nấc 1: Lấy danh sách
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham:");
foreach (var p in products ?? []) {
    Console.WriteLine($" [{p.Id}] {p.Name} {p.Price:N0} VND");
}

// Nấc 2: Tạo mới
var createRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);
if (createResponse.StatusCode == HttpStatusCode.Created) {
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
}

// Nấc 3: Đọc lại chi tiết
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

// Nấc 4: Gửi dữ liệu lỗi
var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", -1));
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"\nThu gui du lieu sai ({invalidResponse.StatusCode}): {invalidError?.Code} - {invalidError?.Message}");

// Khai báo DTO cho Client
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
