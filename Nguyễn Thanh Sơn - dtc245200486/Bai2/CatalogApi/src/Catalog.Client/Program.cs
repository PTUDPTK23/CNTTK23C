using System.Net;
using System.Net.Http.Json;

// 1. Nhan dia chi API tu tham so dong lenh (mac dinh cong 5100)
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("=== CATALOG CLIENT BAT DAU GIAO TIEP ===");

// --- NAC 1: Lay danh sach san pham ---
Console.WriteLine("\n[Nac 1] Lay danh sach san pham:");
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham:");
foreach (var p in products ?? [])
{
    Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// --- NAC 2: Tao san pham moi & Kiem tra ma 201 truoc khi doc body ---
Console.WriteLine("\n[Nac 2] Gui yeu cau tao san pham moi:");
var createRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Da tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
    return;
}

// --- NAC 3: Doc lai chi tiet mot san pham ---
Console.WriteLine("\n[Nac 3] Doc lai chi tiet san pham vua truy van:");
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"Chi tiet san pham 1: {detail?.Name} ({detail?.Price:N0} VND)");

// --- NAC 4: Co y gui du lieu sai de kiem tra viec bat va in ApiError khong crash ---
Console.WriteLine("\n[Nac 4] Co y gui du lieu sai de kiem thu phong thu:");
var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", -1));
Console.WriteLine($"Thu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

Console.WriteLine("\n=== HOAN THANH CHUOI 4 NAC AN TOAN ===");

// Hop dong du lieu phia Client (hoan toan doc lap voi Server)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
