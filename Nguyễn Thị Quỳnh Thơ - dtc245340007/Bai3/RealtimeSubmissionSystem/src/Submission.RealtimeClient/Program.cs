
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

// 1. Cau hinh URL va ma sinh vien
var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "SV001";

// 2. Kenh REST
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 3. Kenh SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// 4. Tao chuong cho trang thai graded
var gradedNotification = new TaskCompletionSource();

// 5. Dang ky nhan thong diep SignalR
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged", m =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {m.SubmissionId} -> {m.Status}");

        if (m.Status == "graded")
        {
            gradedNotification.TrySetResult();
        }
    });

// 6. Mo ket noi SignalR
await connection.StartAsync();

Console.WriteLine("Da ket noi SignalR.");

// 7. Lay danh sach bai tap bang REST
var assignments =
    await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments");

var selected = assignments?.FirstOrDefault();

if (selected == null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id}");

// 8. Tao luot nop bai bang REST
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode,
        $"https://example.com/{studentCode}/signalr"));

// 9. Kiem tra ma HTTP 201 Created
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"Loi: {error?.Code} - {error?.Message}");

    return;
}

// 10. Doc thong tin luot nop
var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();

if (created == null)
{
    Console.WriteLine("Khong doc duoc thong tin luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine(
    $"Trang thai REST ban dau: {created.Status}");

// 11. Theo doi luot nop bang SignalR
await connection.InvokeAsync(
    "WatchSubmission",
    created.Id.ToString());

// 12. Cho thong bao graded hoac het 10 giay
Console.WriteLine(
    "Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine(
        "Qua thoi gian cho thong bao graded.");
}

// 13. Doc lai trang thai bang REST
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}");

// ========================================
// CAC RECORD DU LIEU
// ========================================

record AssignmentDto
{
    public int Id { get; init; }
}

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
