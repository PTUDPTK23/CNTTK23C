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

// Hub SignalR - buoc 5 bo sung Broadcast va Group
sealed class PingHub : Hub
{
    // Máy khách gọi Echo -> trả lời RIÊNG người gọi
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    // Phát rộng cho MỌI kết nối đang mở
    public async Task Broadcast(string message)
        => await Clients.All.SendAsync("BroadcastMessage", message);

    // Thêm kết nối vào nhóm và báo cho cả nhóm
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{Context.ConnectionId[..8]} da tham gia nhom {groupName}");
    }

    // Gửi thông điệp cho thành viên trong nhóm
    public async Task SendToGroup(string groupName, string message)
        => await Clients.Group(groupName).SendAsync("GroupMessage", message);
}