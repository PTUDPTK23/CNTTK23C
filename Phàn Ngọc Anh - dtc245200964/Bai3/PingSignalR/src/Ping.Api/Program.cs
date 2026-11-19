using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Buoc 2: Dang ky dich vu SignalR vao DI
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "PingApi",
    status = "running"
}));

// Buoc 2: Anh xa hub vao tuyen /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

sealed class PingHub : Hub
{
    // Buoc 6: Vong doi ket noi
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId[..8]} ket noi");
        await Clients.All.SendAsync("SystemMessage", $"Client {Context.ConnectionId[..8]} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId[..8]} ngat");
        await Clients.All.SendAsync("SystemMessage", $"Client {Context.ConnectionId[..8]} da ngat ket noi.");
        await base.OnDisconnectedAsync(ex);
    }

    // Buoc 3: Phuong thuc Echo (Caller)
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Buoc 5: Broadcast (All)
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    // Buoc 5: Nhom (Group)
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage", $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }
}
