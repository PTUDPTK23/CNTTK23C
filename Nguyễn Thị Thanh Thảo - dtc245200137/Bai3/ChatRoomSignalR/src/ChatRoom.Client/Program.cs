using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5300";
var userName = args.Length > 1 ? args[1] : "User";
var defaultRoom = args.Length > 2 ? args[2] : "chung";

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/chat")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("SystemMessage", msg =>
    Console.WriteLine($"[He thong] {msg}"));

connection.On<string, string, string, string>("ReceiveMessage", (room, sender, msg, time) =>
    Console.WriteLine($"[{room}][{time}] {sender}: {msg}"));

connection.On<string, string, string>("ReceivePrivate", (sender, msg, time) =>
    Console.WriteLine($"[Tin rieng tu {sender} - {time}]: {msg}"));

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Trang thai] Mat ket noi, dang thu ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    Console.WriteLine("[Trang thai] Da ket noi lai! Dang vao lai phong...");
    await connection.InvokeAsync("JoinRoom", userName, defaultRoom);
};

await connection.StartAsync();
Console.WriteLine($"=== Xin chao {userName}! Da ket noi den ChatRoomApi ===");
await connection.InvokeAsync("JoinRoom", userName, defaultRoom);

Console.WriteLine("Cac lenh ho tro: /join <phong> | /pm <ten> <noi-dung> | /who | /exit");
Console.WriteLine("Hoac nhap tin nhan truc tiep de gui vao phong hien tai.\n");

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
    {
        var newRoom = input[6..].Trim();
        if (!string.IsNullOrEmpty(newRoom))
        {
            defaultRoom = newRoom;
            await connection.InvokeAsync("JoinRoom", userName, defaultRoom);
        }
    }
    else if (input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input[4..].Trim().Split(' ', 2);
        if (parts.Length < 2)
            Console.WriteLine("[Loi cu phap] Dung: /pm <ten-nguoi-nhan> <noi-dung>");
        else
            await connection.InvokeAsync("SendPrivateMessage", parts[0], parts[1]);
    }
    else if (input.Equals("/who", StringComparison.OrdinalIgnoreCase))
    {
        await connection.InvokeAsync("GetOnlineUsers");
    }
    else
    {
        await connection.InvokeAsync("SendMessageToRoom", input);
    }
}