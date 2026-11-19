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

var gradedNotification = new TaskCompletionSource(); // "cai chuong"

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status}");
    if (m.Status == "graded") gradedNotification.TrySetResult();
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. REST: lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen) ?? assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}

// 2. REST: tao luot nop
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi nop bai ({(int)createResponse.StatusCode}): {err?.Code} - {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null) return;
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. SignalR: vao nhom theo doi luot nop
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// 4. Cho chuong graded hoac het 10 giay
var completed = await Task.WhenAny(gradedNotification.Task,
                                   Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");

// 5. REST: doc lai trang thai cuoi cung
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);