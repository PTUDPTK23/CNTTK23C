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
    var timeStr = m.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {timeStr}");
    
    if (m.Status == "failed")
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = prevColor;
        gradedNotification.TrySetResult();
    }
    else if (m.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");
}
catch (Exception ex)
{
    Console.WriteLine($"Loi ket noi SignalR: {ex.Message}");
    return;
}

// 1. REST: Lấy danh sách bài tập
List<AssignmentDto>? assignments = null;
try
{
    assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
}
catch (Exception ex)
{
    Console.WriteLine($"Loi doc danh sach bai tap: {ex.Message}");
    return;
}

var selected = assignments?.FirstOrDefault(a => a.IsOpen) ?? assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// 2. REST: Tạo lượt nộp bài
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repoUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop: {createResponse.StatusCode} - {err?.Code}: {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null)
{
    Console.WriteLine("Khong the doc thong tin luot nop tu phan hoi.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. Theo dõi qua SignalR: Gia nhập nhóm submission:{id}
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// 4. Chờ nhận trạng thái graded/failed hoặc timeout sau 10 giây
var completedTask = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completedTask != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. REST: Đối soát trạng thái cuối cùng từ nguồn sự thật
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
