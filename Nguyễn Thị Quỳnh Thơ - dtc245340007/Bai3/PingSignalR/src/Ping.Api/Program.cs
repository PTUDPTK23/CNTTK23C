
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "PingApi", status = "running" }));

// Anh xa Hub
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync(
            "EchoResponse", $"Echo: {message}");
    }

    // Broadcast
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    // JoinGroup
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // SendToGroup
    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);

    // 1. Khi Client ket noi
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[Hub] + {Context.ConnectionId[..8]} ket noi");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");

        await base.OnConnectedAsync();
    }

    // 2. Khi Client ngat ket noi
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine(
            $"[Hub] - {Context.ConnectionId[..8]} ngat");

        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ngat ket noi.");

        await base.OnDisconnectedAsync(ex);
    }
}
