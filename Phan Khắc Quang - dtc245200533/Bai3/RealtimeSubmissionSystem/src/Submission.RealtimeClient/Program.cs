using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repoUrl = args.Length > 2 ? args[2] : $"https://example.com/{studentCode}/signalr";

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
    if (m.Status is "graded" or "failed")
    {
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. GET danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
Console.WriteLine($"Chon bai tap: {selected?.Id} - {selected?.Title}");

// 2. POST tạo lượt nộp
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected?.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repoUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {err?.Code} - {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created?.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// 3. Gia nhập nhóm theo dõi lượt nộp
await connection.InvokeAsync("WatchSubmission", created!.Id.ToString());

// 4. Chờ chuông reo (tối đa 10 giây)
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. GET đọc lại trạng thái từ REST
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

if (latest?.Status == "failed")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"!!! CANH BAO: luot nop {latest.Id} cham LOI (failed)");
    Console.ResetColor();
}

public record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
public record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
public record ApiError(string Code, string Message);
