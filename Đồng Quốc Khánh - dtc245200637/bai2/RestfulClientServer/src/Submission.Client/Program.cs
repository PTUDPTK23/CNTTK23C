using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Bước 1: Xem danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var a in assignments ?? [])
{
    Console.WriteLine($" [{a.Id}] {a.Title} ({(a.IsOpen ? "Dang mo" : "Da dong")})");
}

// Bước 2: Nộp bài hợp lệ cho bài tập số 1 -> 201 Created + Location
var submitRequest = new CreateSubmissionRequest("SV001", "https://github.com/sv001/rest-api");
var submitResponse = await http.PostAsJsonAsync("/api/assignments/1/submissions", submitRequest);

Uri? createdLocation = null;
if (submitResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await submitResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    createdLocation = submitResponse.Headers.Location;
    Console.WriteLine($"\nNop bai thanh cong! Ma luot nop: {created?.Id}");
    Console.WriteLine($"Header Location: {createdLocation}");
}
else
{
    var error = await submitResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nLoi nop bai: {error?.Code} - {error?.Message}");
    return;
}

// Bước 3: Đọc lại chi tiết lượt nộp theo đường dẫn Location -> 200 OK
if (createdLocation is not null)
{
    var detail = await http.GetFromJsonAsync<SubmissionDto>(createdLocation);
    Console.WriteLine($"\nDoc lai luot nop: SV {detail?.StudentCode} - Repo: {detail?.RepositoryUrl}");
}

// Bước 4: Cố ý nộp trùng lại bài tập 1 -> 409 Conflict
var duplicateResponse = await http.PostAsJsonAsync("/api/assignments/1/submissions", submitRequest);
Console.WriteLine($"\nThu nop trung bai: {(int)duplicateResponse.StatusCode} {duplicateResponse.StatusCode}");
var dupError = await duplicateResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{dupError?.Code}: {dupError?.Message}");

// Bước 5: Cố ý nộp vào bài tập đã đóng (ID = 3) -> 400 BadRequest
var closedResponse = await http.PostAsJsonAsync("/api/assignments/3/submissions", submitRequest);
Console.WriteLine($"\nThu nop bai da dong: {(int)closedResponse.StatusCode} {closedResponse.StatusCode}");
var closedError = await closedResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{closedError?.Code}: {closedError?.Message}");

// Khai báo bản sao DTO ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, DateTime SubmittedAt);
record ApiError(string Code, string Message);