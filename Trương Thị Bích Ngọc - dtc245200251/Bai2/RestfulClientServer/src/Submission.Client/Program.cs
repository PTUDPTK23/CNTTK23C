using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("=== EDU SUBMIT CLIENT ===");

// Nấc 1: Liệt kê danh sách bài tập đang mở
Console.WriteLine("\nDanh sach bai tap dang mo:");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in (assignments ?? []).Where(x => x.IsOpen)) {
    Console.WriteLine($" - {a.Id}: {a.Title}");
}

// Nấc 2: Nộp bài cho bài tập 1, kiểm tra StatusCode == Created trước khi đọc body
var selectedAssignmentId = assignments?.FirstOrDefault(x => x.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    Console.WriteLine("\n[LOI]: Nop bai that bai!");
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai luot nop: {created?.StudentCode} - {created?.RepositoryUrl}");

// Nấc 3: Đọc lại lượt nộp theo header Location hoặc Id
if (createResponse.Headers.Location != null) {
    var detailResponse = await http.GetAsync(createResponse.Headers.Location);
    if (detailResponse.IsSuccessStatusCode) {
        var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai qua Location [{createResponse.Headers.Location}]: {detail?.StudentCode} ({detail?.Status})");
    }
}

// Nấc 4: Thử gửi dữ liệu sai để quan sát lỗi 400 BadRequest và in ApiError
Console.WriteLine("\nThu gui yeu cau loi: 400 BadRequest");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// DTOs phía Client (bản sao hợp đồng JSON với Submission.Api)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt
);
record ApiError(string Code, string Message);
