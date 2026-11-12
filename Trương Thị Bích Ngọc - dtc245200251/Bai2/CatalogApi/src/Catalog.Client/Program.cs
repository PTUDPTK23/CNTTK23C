using System.Net.Http.Json;

// Khởi tạo HttpClient trỏ tới địa chỉ của Catalog.Api đang chạy ở cổng 5100
var client = new HttpClient {
    BaseAddress = new Uri("http://localhost:5100")
};

Console.WriteLine("=== DANG KET NOI TOI CATALOG.API ===");

try {
    // 1. Gọi API lấy danh sách sản phẩm (GET /api/products)
    Console.WriteLine("\n[1] Dang goi GET /api/products...");
    var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
    
    if (products is not null) {
        Console.WriteLine($"=> Lay thanh cong {products.Count} san pham:");
        foreach (var p in products) {
            Console.WriteLine($" - [{p.Id}] {p.Name} | Gia: {p.Price} VND");
        }
    }

    // 2. Gọi API lấy chi tiết một sản phẩm theo ID (GET /api/products/{id})
    Console.WriteLine("\n[2] Dang goi GET /api/products/1 (Xem chi tiet san pham ID = 1)...");
    var singleProduct = await client.GetFromJsonAsync<ProductDto>("/api/products/1");
    
    if (singleProduct is not null) {
        Console.WriteLine($"=> Tim thay san pham: [{singleProduct.Id}] {singleProduct.Name} - Mo ta: {singleProduct.Description} | Gia: {singleProduct.Price} VND");
    }

    // 3. Gọi API tạo mới sản phẩm (POST /api/products)
    Console.WriteLine("\n[3] Dang goi POST /api/products de tao moi...");
    var newProductRequest = new CreateProductRequest(
        Name: "Lap trinh Microservices",
        Description: "Sach chuyen de nang cao",
        Price: 200000
    );

    var response = await client.PostAsJsonAsync("/api/products", newProductRequest);
    
    if (response.IsSuccessStatusCode) {
        var createdProduct = await response.Content.ReadFromJsonAsync<ProductDto>();
        Console.WriteLine($"=> Tao san pham thanh cong! ID moi la: {createdProduct?.Id}");
    } else {
        Console.WriteLine($"=> Tao that bai. Ma loi: {response.StatusCode}");
    }

} catch (Exception ex) {
    Console.WriteLine($"[LOI KET NOI]: Khong the ket noi toi Api. Hay chac chan rang Catalog.Api dang chay!\nChi tiet: {ex.Message}");
}

// DTOs (Phải khớp với bên Api)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);