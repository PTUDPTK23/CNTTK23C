using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine($"=== KẾT NỐI CATALOG.API: {apiBaseUrl} ===\n");

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // Bước 1: lấy danh sách sản phẩm (Slide 56)
    Console.WriteLine("Danh sach san pham:");
    var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
    foreach (var p in products ?? [])
    {
        Console.WriteLine($"  [{p.Id}] {p.Name} - {p.Price:N0} VND");
    }

    // Bước 2: gửi POST tạo mới và kiểm tra mã trạng thái trước khi đọc body (Slide 57)
    var createRequest = new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000);
    var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
        Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
    }
    else
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"\nLoi: {error?.Code} - {error?.Message}");
        return;
    }

    // Bước 3: đọc lại chi tiết một sản phẩm (Slide 58)
    var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
    Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

    // Bước 4: cố ý gửi dữ liệu sai để xem lỗi 400 (Slide 58)
    Console.WriteLine("\nThu gui du lieu sai: 400 BadRequest");
    var badRequest = new CreateProductRequest("", null, -1);
    var badResponse = await http.PostAsJsonAsync("/api/products", badRequest);
    if (!badResponse.IsSuccessStatusCode)
    {
        var error = await badResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"{error?.Code}: {error?.Message}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LỖI MẠNG]: {ex.Message}");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
