var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Bước 2: Endpoint kiểm tra trạng thái hoạt động của máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.Run();
