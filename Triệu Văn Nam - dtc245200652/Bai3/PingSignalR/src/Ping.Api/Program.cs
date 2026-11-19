using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"[+] Ket noi moi: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[-] Ngat ket noi: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }

    public async Task Echo(string message)
    {
        Console.WriteLine($"[Echo] {Context.ConnectionId}: {message}");
        await Clients.Caller.SendAsync("Echo", $"Echo: {message}");
    }
}