using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine($"=== Submission.Client ===");
Console.WriteLine($"Dang ket noi toi: {apiBaseUrl}\n");

// ── Nấc 1: Liệt kê danh sách bài tập ─────────────────────────────────────
Console.WriteLine("--- Nac 1: Lay danh sach bai tap ---");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? [])
{
    var openLabel = a.IsOpen ? "[Mo]" : "[Da dong]";
    Console.WriteLine($"  - {a.Id}: {a.Title} {openLabel}");
}

// Chọn bài tập đầu tiên còn mở
var selectedId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;

// ── Nấc 2: Nộp bài ────────────────────────────────────────────────────────
Console.WriteLine($"\n--- Nac 2: Nop bai cho bai tap ID={selectedId} ---");
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedId}/submissions",
    new CreateSubmissionRequest("SV001", "https://github.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"  Da tao luot nop: {created?.Id}");
Console.WriteLine($"  Trang thai ban dau: {created?.Status}");
Console.WriteLine($"  Location: {createResponse.Headers.Location}");

// ── Nấc 3: Đọc lại theo Location ──────────────────────────────────────────
Console.WriteLine("\n--- Nac 3: Doc lai luot nop qua Location ---");
var locationUrl = createResponse.Headers.Location?.ToString();
if (locationUrl is not null)
{
    var readResponse = await http.GetAsync(locationUrl);
    if (readResponse.StatusCode == HttpStatusCode.OK)
    {
        var submission = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"  StudentCode  : {submission?.StudentCode}");
        Console.WriteLine($"  RepositoryUrl: {submission?.RepositoryUrl}");
        Console.WriteLine($"  Status       : {submission?.Status}");
        Console.WriteLine($"  ReceivedAt   : {submission?.ReceivedAt:dd/MM/yyyy HH:mm:ss}");
    }
    else
        await PrintApiErrorAsync(readResponse);
}

// ── Nấc 4: Cố ý gửi sai để quan sát lỗi ─────────────────────────────────
Console.WriteLine("\n--- Nac 4: Thu gui yeu cau loi (kiem tra xu ly loi) ---");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedId}/submissions",
    new CreateSubmissionRequest("", ""));
Console.WriteLine($"  Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// ── Hàm chung đọc ApiError ────────────────────────────────────────────────
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"  {error?.Code}: {error?.Message}");
}

// ── Records (khai báo lại ở Client — chia sẻ hợp đồng JSON) ───────────────
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
