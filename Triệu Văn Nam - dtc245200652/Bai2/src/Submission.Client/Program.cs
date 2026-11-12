using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Nấc 1: Liệt kê danh sách bài tập đang mở
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var openAssignments = (assignments ?? []).Where(a => a.IsOpen).ToList();

Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in openAssignments)
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nấc 2: Nộp bài và kiểm tra 201 trước khi đọc body
var selectedAssignmentId = openAssignments.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// Nấc 3: Đọc lại lượt nộp vừa tạo theo đúng địa chỉ trong header Location
var location = createResponse.Headers.Location?.ToString();
if (!string.IsNullOrWhiteSpace(location))
{
    var fetched = await http.GetFromJsonAsync<SubmissionDto>(location);
    Console.WriteLine($"Doc lai luot nop: {fetched?.StudentCode} - {fetched?.RepositoryUrl}");
}

// Nấc 4: Cố ý gửi yêu cầu sai để bắt lỗi 400 qua ApiError mà không sập
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));
Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo bản sao DTO ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);