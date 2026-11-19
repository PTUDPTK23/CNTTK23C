
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Kiem tra trang thai may chu
app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Anh xa SignalR Hub
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Hub SignalR - buoc 3 se bo sung phuong thuc Echo
sealed class PingHub : Hub
{
    // Máy khách gọi Echo -> trả lời RIÊNG người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }
}
