using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kenh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

static string Short(Guid id) => $"{id.ToString()[..8]}-...";

var finishedNotification = new TaskCompletionSource();   // "cai chuong"
Guid? watching = null;

// On phai dang ky TRUOC StartAsync
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {Short(m.SubmissionId)} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (m.Status == "failed")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]} cham LOI (failed)");
        Console.ResetColor();
    }
    if (m.Status is "graded" or "failed") finishedNotification.TrySetResult();
});

// Noi lai: ConnectionId moi -> mat nhom -> Watch lai + doc lai bang REST
connection.Reconnected += async _ =>
{
    Console.WriteLine("[Lifecycle] Da ket noi lai.");
    if (watching is Guid id)
    {
        await connection.InvokeAsync("WatchSubmission", id.ToString());
        var s = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{id}");
        Console.WriteLine($"Trang thai doc lai sau khi noi lai: {s?.Status}");
        if (s?.Status is "graded" or "failed") finishedNotification.TrySetResult();
    }
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. REST: danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null) { Console.WriteLine("Khong co bai tap nao."); return; }
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// 2. REST: tao luot nop
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi {(int)createResponse.StatusCode}: {error?.Code} - {error?.Message}");
    return;
}
var created = (await createResponse.Content.ReadFromJsonAsync<SubmissionDto>())!;
Console.WriteLine($"Da tao luot nop: {Short(created.Id)}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. SignalR: vao nhom submission:{id} (SAU khi co id tu POST)
watching = created.Id;
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// 4. Cho chuong graded/failed hoac het 10 giay
var completed = await Task.WhenAny(finishedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != finishedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");

// 5. REST: chot trang thai (nguon su that)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(int Id, string Title, DateTimeOffset DueAt, bool IsClosed);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
