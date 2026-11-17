
using System.Net.Http.Json;
using System.Text.Json;

var baseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5100";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

Console.WriteLine("==================================");
Console.WriteLine("       CATALOG CLIENT");
Console.WriteLine("==================================");
Console.WriteLine($"API: {baseUrl}");

try
{
    // 1. GET danh sach san pham
    Console.WriteLine("\n1. GET /api/products");

    var products = await httpClient.GetFromJsonAsync<List<ProductDto>>(
        "/api/products", jsonOptions);

    if (products is not null)
    {
        foreach (var product in products)
        {
            Console.WriteLine(
                $"{product.Id}. {product.Name} - {product.Price:N0} VND");
        }
    }

    // 2. POST san pham hop le
    Console.WriteLine("\n2. POST /api/products - Du lieu hop le");

    var newProduct = new CreateProductRequest(
        "San pham tao tu Catalog.Client",
        "Kiem tra giao tiep HTTP Client",
        250000);

    var createResponse = await httpClient.PostAsJsonAsync(
        "/api/products", newProduct);

    Console.WriteLine($"HTTP Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");

    if (!createResponse.IsSuccessStatusCode)
    {
        Console.WriteLine(await createResponse.Content.ReadAsStringAsync());
        return;
    }

    var createdProduct =
        await createResponse.Content.ReadFromJsonAsync<ProductDto>(jsonOptions);

    Console.WriteLine($"Created ID: {createdProduct?.Id}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    // 3. GET lai san pham vua tao
    if (createdProduct is not null)
    {
        Console.WriteLine($"\n3. GET /api/products/{createdProduct.Id}");

        var getResponse = await httpClient.GetAsync(
            $"/api/products/{createdProduct.Id}");

        Console.WriteLine($"HTTP Status: {(int)getResponse.StatusCode} {getResponse.StatusCode}");

        var productAgain =
            await getResponse.Content.ReadFromJsonAsync<ProductDto>(jsonOptions);

        if (productAgain is not null)
        {
            Console.WriteLine($"Name: {productAgain.Name}");
            Console.WriteLine($"Price: {productAgain.Price:N0} VND");
        }
    }

    // 4. POST du lieu khong hop le
    Console.WriteLine("\n4. POST /api/products - Du lieu khong hop le");

    var invalidProduct = new CreateProductRequest(
        "",
        "San pham co ten rong",
        -100);

    var invalidResponse = await httpClient.PostAsJsonAsync(
        "/api/products", invalidProduct);

    Console.WriteLine($"HTTP Status: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    var errorBody = await invalidResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"Response: {errorBody}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("\nKHONG THE KET NOI DEN API.");
    Console.WriteLine($"Chi tiet: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    Console.WriteLine("\nYEU CAU API BI HUY HOAC QUA THOI GIAN CHO.");
    Console.WriteLine($"Chi tiet: {ex.Message}");
}

record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price);

record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price);
