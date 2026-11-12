using Catalog.Contracts;
using System.Net;
using System.Net.Http.Json;

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5100")
};

Console.WriteLine("=== BAT DAU KIEM THU TU CATALOG.CLIENT ===\n");

try
{
    // 1. GET danh sách ban đầu
    Console.WriteLine("1. [GET] Danh sach san pham ban dau:");
    var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
    products?.ForEach(p => Console.WriteLine($"   - [{p.Id}] {p.Name}: {p.Price:N0} VND"));

    // 2. POST tạo sản phẩm mới
    Console.WriteLine("\n2. [POST] Tao san pham moi:");
    var newProduct = new CreateProductRequest("Lap trinh Client-Server", "Sach thuc hanh", 110000);
    var postResponse = await client.PostAsJsonAsync("/api/products", newProduct);
    Console.WriteLine($"   - Ma phan hoi: {(int)postResponse.StatusCode} {postResponse.StatusCode}");
    var created = await postResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"   - Da tao: [{created?.Id}] {created?.Name} - Location: {postResponse.Headers.Location}");

    // 3. PUT cập nhật sản phẩm vừa tạo
    if (created is not null)
    {
        Console.WriteLine($"\n3. [PUT] Cap nhat gia cho san pham ID = {created.Id}:");
        var updateData = new UpdateProductRequest(created.Name + " (Ban moi)", created.Description, 135000);
        var putResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", updateData);
        var updated = await putResponse.Content.ReadFromJsonAsync<ProductDto>();
        Console.WriteLine($"   - Sau cap nhat: [{updated?.Id}] {updated?.Name} - Gia moi: {updated?.Price:N0} VND");

        // 4. DELETE xóa sản phẩm vừa tạo
        Console.WriteLine($"\n4. [DELETE] Xoa san pham ID = {created.Id}:");
        var deleteResponse = await client.DeleteAsync($"/api/products/{created.Id}");
        Console.WriteLine($"   - Ma phan hoi: {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}");
    }

    // 5. Thử gọi một ID không tồn tại để kiểm tra xử lý lỗi 404
    Console.WriteLine("\n5. [GET] Thu truy van ID = 999 (khong ton tai):");
    var notFoundResponse = await client.GetAsync("/api/products/999");
    if (notFoundResponse.StatusCode == HttpStatusCode.NotFound)
    {
        var error = await notFoundResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"   - Bat loi thanh cong (404): [{error?.Code}] {error?.Message}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LOI KET NOI] Khong the ket noi toi may chu API. Hay kiem tra Catalog.Api da chay chua!\nChi tiet: {ex.Message}");
}

Console.WriteLine("\n=== HOAN TAT ===");

// Khai báo DTO ở cuối tệp
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record UpdateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);