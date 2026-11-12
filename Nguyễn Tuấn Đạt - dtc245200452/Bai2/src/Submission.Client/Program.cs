using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Nấc 1: Liệt kê danh sách bài tập đang mở
var listResponse = await http.GetAsync("/api/assignments");
if (!listResponse.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(listResponse);
    return;
}

var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments.Where(item => item.IsOpen))
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nấc 2: Gửi yêu cầu nộp bài, kiểm tra mã 201 trước khi đọc body
var selectedAssignmentId = assignments.FirstOrDefault(item => item.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("dtc245200452", "https://github.com/dtc245200452-hub/CNTTK23C"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// Nấc 3: Đọc lại thông tin lượt nộp vừa tạo thông qua địa chỉ trong header Location
var locationUri = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{created?.Id}";
var readResponse = await http.GetAsync(locationUri);
if (!readResponse.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(readResponse);
    return;
}

var fetched = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Doc lai luot nop: {fetched?.StudentCode} - {fetched?.RepositoryUrl}");

// Nấc 4: Cố ý gửi dữ liệu sai (thiếu mã sinh viên và link repo), nhận 400, đọc ApiError và không bị sập
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;

    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo lại bản sao DTO ở phía Client (chỉ chia sẻ hợp đồng JSON, không dùng chung lớp với Server)
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
