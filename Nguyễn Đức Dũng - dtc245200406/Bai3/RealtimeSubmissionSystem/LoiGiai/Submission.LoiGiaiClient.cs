// RealtimeSubmissionSystem/LoiGiai/Submission.LoiGiaiClient.cs
// Lời giải Bài tập 2 & 3: Máy khách theo dõi 2 lượt nộp cùng lúc và xử lý trạng thái failed
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// BÀI TẬP 3: Dùng ConcurrentDictionary lưu TaskCompletionSource cho từng lượt nộp
var completionSources = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId.ToString()[..8]} -> {m.Status}");

    if (m.Status == "failed")
    {
        // BÀI TẬP 2: In cảnh báo màu đỏ khi trạng thái là failed
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]} cham LOI (failed)");
        Console.ForegroundColor = oldColor;
    }

    if (m.Status is "graded" or "failed")
    {
        if (completionSources.TryGetValue(m.SubmissionId, out var tcs))
        {
            tcs.TrySetResult(m.Status);
        }
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments!.First();

// Tạo lượt nộp 1: SV001 (hợp lệ - https://)
var sub1Res = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr"));
var sub1 = await sub1Res.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao SV001: {sub1!.Id.ToString()[..8]} ({sub1.Status})");

// Tạo lượt nộp 2: SV002 (không bắt đầu bằng https:// -> failed)
var sub2Res = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest("SV002", "http://example.com/sv002/signalr"));
var sub2 = await sub2Res.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao SV002: {sub2!.Id.ToString()[..8]} ({sub2.Status})");

var tcs1 = completionSources.GetOrAdd(sub1.Id, _ => new TaskCompletionSource<string>());
var tcs2 = completionSources.GetOrAdd(sub2.Id, _ => new TaskCompletionSource<string>());

// Watch cả 2 trên cùng 1 kết nối SignalR
await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

// Chờ cả 2 hoàn tất
await Task.WhenAll(
    Task.WhenAny(tcs1.Task, Task.Delay(TimeSpan.FromSeconds(10))),
    Task.WhenAny(tcs2.Task, Task.Delay(TimeSpan.FromSeconds(10)))
);

// Đọc lại từ REST để xác nhận tính nhất quán
var latest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
var latest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

Console.WriteLine($"SV001: SignalR = {(tcs1.Task.IsCompleted ? tcs1.Task.Result : "timeout")}, REST = {latest1?.Status}");
Console.WriteLine($"SV002: SignalR = {(tcs2.Task.IsCompleted ? tcs2.Task.Result : "timeout")}, REST = {latest2?.Status}");

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
