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

try
{
    // Nấc 1: Lấy và in danh sách sản phẩm hiện có (GET /api/products)
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
        await PrintApiErrorAsync(createResponse);
        return;
    }

    // Nấc 3: Đọc lại sản phẩm vừa tạo theo header Location
    Console.WriteLine("\n--- NAC 3: Doc lai san pham theo header Location ---");
    if (createResponse.Headers.Location is not null)
    {
        var detailResponse = await http.GetAsync(createResponse.Headers.Location);
        if (detailResponse.IsSuccessStatusCode)
        {
            var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDto>();
            Console.WriteLine($"Doc lai san pham thanh cong:");
            Console.WriteLine($"  Id: {detail?.Id}");
            Console.WriteLine($"  Name: {detail?.Name}");
            Console.WriteLine($"  Description: {detail?.Description}");
            Console.WriteLine($"  Price: {detail?.Price:N0} VND");
        }
        else
        {
            await PrintApiErrorAsync(detailResponse);
        }
    }

    // Nấc 4: Cố ý gửi dữ liệu sai để nhận lỗi 400 Bad Request + ApiError
    Console.WriteLine("\n--- NAC 4: Gui du lieu sai de kiem tra bat loi 400 (POST /api/products) ---");
    var invalidRequest = new CreateProductRequest("", "San pham khong hop le", -1);
    using var invalidResponse = await http.PostAsJsonAsync("/api/products", invalidRequest);

    Console.WriteLine($"Ket qua nhan duoc: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);

    Console.WriteLine("\n=== HOAN THANH KIEM THU 4 NAC ===");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[Loi mang] Khong the ket noi toi API tai {apiBaseUrl}: {ex.Message}");
    Console.WriteLine("Hay kiem tra tien trinh Catalog.Api da chay chua va co dung cong 5100 khong.");
}

// Ham ho tro doc ApiError tu phan hoi loi
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;

    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null && !string.IsNullOrEmpty(error.Code))
        {
            Console.WriteLine($"  ApiError: [{error.Code}] {error.Message}");
            return;
        }
    }
    catch
    {
        // Loi do framework tra ve (404/405/415...) co the khong co ApiError
    }

    Console.WriteLine($"  Framework tra ve: {(int)response.StatusCode} {response.StatusCode} (Khong co body ApiError)");
}

// Khai báo các record DTO (khớp hợp đồng JSON với máy chủ)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
