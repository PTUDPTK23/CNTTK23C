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

    // Gửi thông báo tới TẤT CẢ client đang kết nối
    public async Task Broadcast(string user, string message)
    {
        Console.WriteLine($"[Broadcast] {user}: {message}");
        await Clients.All.SendAsync("ReceiveBroadcast", user, message);
    }

    // Tham gia một nhóm (Room)
    public async Task JoinRoom(string roomName, string user)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        Console.WriteLine($"[Room:{roomName}] {user} da tham gia.");
        await Clients.Group(roomName).SendAsync("ReceiveRoomMessage", roomName, "He thong", $"{user} da vao phong {roomName}");
    }

    // Gửi tin nhắn chỉ cho các thành viên trong nhóm đó
    public async Task SendToRoom(string roomName, string user, string message)
    {
        Console.WriteLine($"[Room:{roomName}] {user}: {message}");
        await Clients.Group(roomName).SendAsync("ReceiveRoomMessage", roomName, user, message);
    }
}