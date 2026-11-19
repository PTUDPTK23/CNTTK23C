using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== CATALOG CLIENT KET NOI TOI: {apiBaseUrl} ===\n");

// Nấc 1: Lấy danh sách sản phẩm
Console.WriteLine("--- [Nac 1] Danh sach san pham ---");
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
foreach (var p in products ?? []) {
    Console.WriteLine($"  [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// Nấc 2: Gửi POST tạo sản phẩm mới và kiểm tra mã trạng thái TRƯỚC khi đọc body
Console.WriteLine("\n--- [Nac 2] Tao san pham moi ---");
var createRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);
if (createResponse.StatusCode == HttpStatusCode.Created) {
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Da tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
} else {
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
    return;
}

// Nấc 3: Đọc lại chi tiết một sản phẩm
Console.WriteLine("\n--- [Nac 3] Doc lai chi tiet san pham 1 ---");
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"Chi tiet san pham 1: [{detail?.Id}] {detail?.Name} - {detail?.Price:N0} VND");

// Nấc 4: Cố ý gửi dữ liệu sai để quan sát lỗi 400
Console.WriteLine("\n--- [Nac 4] Thu gui du lieu sai ---");
var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", -1));
Console.WriteLine($"Thu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"  -> {invalidError?.Code}: {invalidError?.Message}");

record ProductDto(int Id, string Name, string? Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
