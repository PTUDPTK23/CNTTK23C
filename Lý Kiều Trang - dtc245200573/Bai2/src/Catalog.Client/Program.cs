using System.Net.Http.Json;
using System.Text.Json;

using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5278") };

Console.WriteLine("=== 1. GET DANH SACH SAN PHAM ===");
var list = await client.GetFromJsonAsync<List<Product>>("/api/products");
if (list != null)
{
    foreach (var p in list)
        Console.WriteLine($"- [{p.Id}] {p.Name} | Mo ta: {p.Description} | Gia: {p.Price}");
}

Console.WriteLine("\n=== 2. POST TAO SAN PHAM HOP LE (201 + Location) ===");
var newProduct = new Product(0, "Ban phim co AKKO", "Ban phim co khong day", 1250000);
var postRes = await client.PostAsJsonAsync("/api/products", newProduct);
Console.WriteLine($"Status: {(int)postRes.StatusCode} {postRes.StatusCode}");
Console.WriteLine($"Location: {postRes.Headers.Location}");
var created = await postRes.Content.ReadFromJsonAsync<Product>();
Console.WriteLine($"Created: {JsonSerializer.Serialize(created)}");

Console.WriteLine("\n=== 3. GET CHI TIET SAN PHAM ID = 1 ===");
var detail = await client.GetFromJsonAsync<Product>("/api/products/1");
Console.WriteLine($"Product 1: {detail?.Name} - {detail?.Description} - {detail?.Price}");

Console.WriteLine("\n=== 4. POST DU LIEU SAI DE DOC 400 + ApiError ===");
var invalidProduct = new Product(0, "San pham loi", "", -1);
var badRes = await client.PostAsJsonAsync("/api/products", invalidProduct);
Console.WriteLine($"Status: {(int)badRes.StatusCode} {badRes.StatusCode}");
var errorBody = await badRes.Content.ReadFromJsonAsync<JsonElement>();
Console.WriteLine($"ApiError: {errorBody}");

record Product(int Id, string Name, string Description, decimal Price);