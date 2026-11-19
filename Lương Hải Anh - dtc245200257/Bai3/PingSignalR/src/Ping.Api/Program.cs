using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));

// 2. Ánh xạ Hub vào đường dẫn /hubs/ping
app.MapHub<PingHub>("/hubs/ping");

app.Run();

// Khai báo lớp PingHub ở cuối file
sealed class PingHub : Hub
{
    // Bước 3: Trả lời riêng người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Bước 5: Gửi cho tất cả mọi người đang kết nối
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    // Bước 5: Tham gia nhóm và báo cho cả nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Bước 5: Gửi tin nhắn vào nhóm
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }

    // Bước 6: Vòng đời khi có Client kết nối
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId[..8]} ket noi");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ket noi.");
        await base.OnConnectedAsync();
    }

    // Bước 6: Vòng đời khi Client ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId[..8]} ngat");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {Context.ConnectionId[..8]} da ngat ket noi.");
        await base.OnDisconnectedAsync(ex);
    }
}
 