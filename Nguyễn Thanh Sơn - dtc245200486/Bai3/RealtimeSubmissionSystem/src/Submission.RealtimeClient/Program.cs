using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

if (studentCode.Equals("multi", StringComparison.OrdinalIgnoreCase) ||
    studentCode.Equals("baitap", StringComparison.OrdinalIgnoreCase))
{
    // BÀI TẬP NÂNG CAO: Theo dõi nhiều lượt nộp đồng thời & xử lý failed
    var submissionTasks = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
    {
        Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
        if (message.Status == "failed")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Canh bao] Luot nop {message.SubmissionId} bi tu choi / failed do URL khong hop le.");
            Console.ResetColor();
        }

        if (submissionTasks.TryGetValue(message.SubmissionId, out var tcs))
        {
            if (message.Status is "graded" or "failed")
            {
                tcs.TrySetResult(message.Status);
            }
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap nao de nop.");
        return;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    // Tạo lượt nộp 1: hợp lệ
    var req1 = new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr");
    var res1 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", req1);
    var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();

    // Tạo lượt nộp 2: không hợp lệ (http:// thay vì https://)
    var req2 = new CreateSubmissionRequest("SV002", "http://insecure-domain.org/sv002");
    var res2 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", req2);
    var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();

    if (sub1 is null || sub2 is null) return;

    Console.WriteLine($"Da tao luot nop 1: {sub1.Id}");
    Console.WriteLine($"Da tao luot nop 2: {sub2.Id}");

    var tcs1 = new TaskCompletionSource<string>();
    var tcs2 = new TaskCompletionSource<string>();
    submissionTasks[sub1.Id] = tcs1;
    submissionTasks[sub2.Id] = tcs2;

    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());
    Console.WriteLine("Dang cho thong bao trang thai 2 luot nop tu may chu...");

    await Task.WhenAll(
        Task.WhenAny(tcs1.Task, Task.Delay(TimeSpan.FromSeconds(10))),
        Task.WhenAny(tcs2.Task, Task.Delay(TimeSpan.FromSeconds(10)))
    );

    var rest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var rest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    Console.WriteLine($"Trang thai doc lai bang REST (SV001): {rest1?.Status}");
    Console.WriteLine($"Trang thai doc lai bang REST (SV002): {rest2?.Status}");
    return;
}

// =========================================================================
// CHẶNG B1 & B2 CHUẨN XÁC THEO HƯỚNG DẪN PDF TỪNG BƯỚC
// =========================================================================

var gradedNotification = new TaskCompletionSource();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "graded" || message.Status == "failed")
    {
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// REST: lấy danh sách bài tập
var assignmentsList = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignmentsList?.FirstOrDefault();
if (selectedAssignment is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}
Console.WriteLine($"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

// REST: tạo lượt nộp
var createRequest = new CreateSubmissionRequest(
    StudentCode: studentCode,
    RepositoryUrl: $"https://example.com/{studentCode.ToLower()}/signalr");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignment.Id}/submissions", createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var createdSubmission = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (createdSubmission is null)
{
    Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {createdSubmission.Id}");
Console.WriteLine($"Trang thai REST ban dau: {createdSubmission.Status}");

// SignalR: vào nhóm theo dõi lượt nộp này
await connection.InvokeAsync("WatchSubmission", createdSubmission.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Chờ "chuông" graded, tối đa 10 giây
var completed = await Task.WhenAny(gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// REST là nguồn sự thật: đọc lại trạng thái cuối
var latestSubmission = await
    http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{createdSubmission.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latestSubmission?.Status}");

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
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
