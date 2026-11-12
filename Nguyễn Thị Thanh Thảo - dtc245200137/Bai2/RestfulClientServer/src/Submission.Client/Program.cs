using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// NẤC 1: Liệt kê danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments ?? [])
{
    if (a.IsOpen)
        Console.WriteLine($"- {a.Id}: {a.Title}");
}

// NẤC 2: Nộp bài (kiểm tra mã trạng thái trước khi đọc body)
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://github.com/sv001/edusubmit-bai2"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
var location = createResponse.Headers.Location;
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Dia chi doc lai (Location): {location}");

// NẤC 3: Đọc lại lượt nộp vừa tạo từ địa chỉ Location
if (location is not null)
{
    var detailResponse = await http.GetAsync(location);
    if (detailResponse.IsSuccessStatusCode)
    {
        var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop: {detail?.StudentCode} -> {detail?.RepositoryUrl}");
    }
}

// NẤC 4: Cố ý gửi dữ liệu sai để quan sát lỗi 400
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// Hàm dùng chung đọc mọi lỗi theo chuẩn ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo bản sao DTO ở phía Client
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);