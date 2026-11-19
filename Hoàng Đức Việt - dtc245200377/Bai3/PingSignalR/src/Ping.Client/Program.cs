using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";

// 1. Khoi tao HubConnection voi co che tu dong ket noi lai
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// 2. Dang ky day du cac handler xu ly thong diep TRUOC khi StartAsync
connection.On<string>("EchoResponse", msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage", m => Console.WriteLine($"[Broadcast] {m}"));
connection.On<string>("GroupMessage", m => Console.WriteLine($"[Group] {m}"));
connection.On<string>("SystemMessage", m => Console.WriteLine($"[System] {m}"));

// Dang ky cac su kien vong doi ket noi
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

// 3. Mo ket noi SignalR
await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId?[..8]}");

// 4. Goi tuan tu 4 phuong thuc len Hub
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");

// 5. Cho thong diep va giu chuong trinh chay
Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
Console.ReadLine();
