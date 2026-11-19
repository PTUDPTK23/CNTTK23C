using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
bool runMulti = args.Any(a => a == "--multi");

// Kenh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var pendingTasks = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var timeStr = m.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {timeStr}");

    if (m.Status == "failed")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
        Console.ResetColor();
    }

    if (m.Status is "graded" or "failed")
    {
        if (pendingTasks.TryGetValue(m.SubmissionId, out var tcs))
        {
            tcs.TrySetResult(m.Status);
        }
    }
});

// Mo ket noi SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

if (runMulti)
{
    // Bai tap 3: Theo doi hai luot nop dong thoi tren cung mot ket noi
    await RunMultiSubmissionDemoAsync(http, connection, pendingTasks);
}
else
{
    // Luong chuan 8 buoc
    await RunStandardSubmissionAsync(http, connection, pendingTasks, studentCode);
}

// Ham chay luong chuan
static async Task RunStandardSubmissionAsync(
    HttpClient http,
    HubConnection connection,
    ConcurrentDictionary<Guid, TaskCompletionSource<string>> pendingTasks,
    string studentCode)
{
    // 1. Lay danh sach bai tap
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap nao dang mo.");
        return;
    }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    // 2. Tao luot nop qua REST
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop: {err?.Code} - {err?.Message}");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created!.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    // Chuẩn bị chuông chờ
    var tcs = new TaskCompletionSource<string>();
    pendingTasks[created.Id] = tcs;

    // 3. Gia nhap nhom theo doi tren SignalR
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

    // 4. Cho su kien hoac timeout 10s
    var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != tcs.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    // 5. Doc lai trang thai bang REST (nguon su that)
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

// Ham chay 2 luot nop dong thoi (Bai tap 2 & 3 van dung)
static async Task RunMultiSubmissionDemoAsync(
    HttpClient http,
    HubConnection connection,
    ConcurrentDictionary<Guid, TaskCompletionSource<string>> pendingTasks)
{
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null) return;

    // Tao luot 1 (hop le -> graded)
    var res1 = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr"));
    var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao SV001: {sub1!.Id.ToString()[..8]} (received)");

    // Tao luot 2 (URL khong https -> failed)
    var res2 = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV002", "http://example.com/sv002/failed"));
    var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao SV002: {sub2!.Id.ToString()[..8]} (received)");

    var tcs1 = new TaskCompletionSource<string>();
    var tcs2 = new TaskCompletionSource<string>();
    pendingTasks[sub1.Id] = tcs1;
    pendingTasks[sub2.Id] = tcs2;

    // Watch ca 2 tren cung mot ket noi
    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    await Task.WhenAll(
        Task.WhenAny(tcs1.Task, Task.Delay(10000)),
        Task.WhenAny(tcs2.Task, Task.Delay(10000))
    );

    var latest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var latest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    Console.WriteLine($"SV001: SignalR = {tcs1.Task.Result}, REST = {latest1?.Status}");
    Console.WriteLine($"SV002: SignalR = {tcs2.Task.Result}, REST = {latest2?.Status}");
}

// Records
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
