// BasicWebApi - Bai 1 (Muc 8 / Thuc hanh 1.2): Luyen ky thuat ASP.NET Core Minimal API
// Chay: dotnet run --project src/BasicWebApi --urls http://localhost:5081

// Pha 1: Khoi tao builder va dang ky dich vu vao DI container truoc khi Build()
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GreetingService>();

// Pha 2: Build ung dung, anh xa cac endpoint va khoi chay may chu
var app = builder.Build();

// GET / : Kiem tra tinh trang hoat dong cua API
app.MapGet("/", () => Results.Ok(new
{
    application = "BasicWebApi",
    message = "ASP.NET Core Web API is running."
}));

// GET /hello/{name}?language=vi|en|ja : Doc tham so tu route va query string
app.MapGet("/hello/{name}", (string name, string? language) =>
{
    var message = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
        ? $"Hello, {name}!"
        : string.Equals(language, "ja", StringComparison.OrdinalIgnoreCase)
            ? $"Konnichiwa, {name}!"
            : $"Xin chào, {name}!";

    return Results.Ok(new GreetingResponse(message));
});

// POST /greetings : Nhan JSON body, validate dau vao va tiem GreetingService qua DI
app.MapPost("/greetings", (CreateGreetingRequest request, GreetingService service) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError("Name must not be empty."));
    }

    var greeting = service.Create(request.Name.Trim());
    return Results.Created($"/greetings/{greeting.Id}", greeting);
});

app.Run();

public sealed record CreateGreetingRequest(string? Name);
public sealed record GreetingResponse(string Message);
public sealed record ApiError(string Message);
public sealed record StoredGreeting(int Id, string Name, DateTimeOffset CreatedAt);

// Dich vu sinh loi chao voi bo dem ID tang dan an toan luong trong pham vi tien trinh
public sealed class GreetingService
{
    private int _lastId;

    public StoredGreeting Create(string name) =>
        new(Interlocked.Increment(ref _lastId), name, DateTimeOffset.UtcNow);
}
