using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

app.MapGet("/", () => new { service = "PingApi", status = "running" });
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    public async Task Echo(string message)
        => await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId!, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage",
            $"{Context.ConnectionId![..Math.Min(8, Context.ConnectionId.Length)]} da tham gia nhom {groupName}");
    }

    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);

    public async Task SendToClient(string connectionId, string message)
        => await Clients.Client(connectionId).SendAsync("EchoResponse", message);

    public override async Task OnConnectedAsync()
    {
        var id = Context.ConnectionId!;
        Console.WriteLine($"[Hub] + {id[..Math.Min(8, id.Length)]} ket noi");
        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {id[..Math.Min(8, id.Length)]} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var id = Context.ConnectionId!;
        Console.WriteLine($"[Hub] - {id[..Math.Min(8, id.Length)]} ngat");
        await Clients.All.SendAsync(
            "SystemMessage",
            $"Client {id[..Math.Min(8, id.Length)]} da ngat ket noi.");
        await base.OnDisconnectedAsync(exception);
    }
}
