using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(); // "cái chuông"

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt:HH:mm:ss}");
    if (m.Status == "graded" || m.Status == "failed")
    {
        if (m.Status == "failed")
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]} cham LOI (failed)");
            Console.ForegroundColor = prevColor;
        }
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong tim thay bai tap nao tren may chu.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode.ToLower()}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Tham gia nhom theo doi luot nop nay
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// Cho chuong bao graded hoac timeout 10 giay
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// Chot lai trang thai bang REST (nguon su that)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Tu dong thoat sau khi hoan tat theo tieu chi slide 71

record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
