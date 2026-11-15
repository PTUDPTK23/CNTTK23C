using System.Net;
using System.Net.Http.Json;

var baseAddress = args.Length > 0 ? args[0] : "http://localhost:5140";
using var http = new HttpClient { BaseAddress = new Uri(baseAddress) };

using var assignmentsResponse = await http.GetAsync("/api/assignments");
if (assignmentsResponse.StatusCode != HttpStatusCode.OK)
{
    Console.Error.WriteLine($"Không lấy được assignments: {(int)assignmentsResponse.StatusCode}.");
    return 1;
}

var assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentResponse>>()
    ?? throw new InvalidOperationException("API trả về danh sách assignments rỗng hoặc không hợp lệ.");
var openAssignment = assignments.FirstOrDefault(assignment => assignment.IsOpen);
if (openAssignment is null)
{
    Console.Error.WriteLine("Không có assignment nào đang mở.");
    return 1;
}

Console.WriteLine("Assignments đang mở:");
Console.WriteLine($"[{openAssignment.Id}] {openAssignment.Title}");

var request = new CreateSubmissionRequest(
    "Lê Hoàng Quân",
    "Bài nộp minh họa từ Submission.Client.");
using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{openAssignment.Id}/submissions", request);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.Error.WriteLine(
        $"Tạo submission thất bại: {(int)createResponse.StatusCode} {error?.Code}: {error?.Message}");
    return 1;
}

var createdSubmission = await createResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
    ?? throw new InvalidOperationException("API không trả về submission sau HTTP 201.");
var location = createResponse.Headers.Location
    ?? throw new InvalidOperationException("HTTP 201 không có Location header.");

Console.WriteLine($"Đã tạo submission {createdSubmission.Id}.");
Console.WriteLine($"Location: {location}");

using var readResponse = await http.GetAsync(location);
if (readResponse.StatusCode != HttpStatusCode.OK)
{
    Console.Error.WriteLine($"Đọc submission thất bại: {(int)readResponse.StatusCode}.");
    return 1;
}

var savedSubmission = await readResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
    ?? throw new InvalidOperationException("API không trả về submission đã lưu.");
Console.WriteLine($"Đọc lại thành công: {savedSubmission.StudentName} - {savedSubmission.Content}");

var invalidRequest = new CreateSubmissionRequest(" ", "");
using var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{openAssignment.Id}/submissions", invalidRequest);
if (invalidResponse.StatusCode != HttpStatusCode.BadRequest)
{
    Console.Error.WriteLine(
        $"Mong đợi HTTP 400, nhận {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}.");
    return 1;
}

var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>()
    ?? throw new InvalidOperationException("API không trả về ApiError cho HTTP 400.");
Console.WriteLine($"Dữ liệu sai: HTTP 400 - {invalidError.Code}: {invalidError.Message}");

return 0;

sealed record AssignmentResponse(int Id, string Title, string Description, bool IsOpen);
sealed record CreateSubmissionRequest(string StudentName, string Content);
sealed record SubmissionResponse(
    Guid Id,
    int AssignmentId,
    string StudentName,
    string Content,
    DateTimeOffset SubmittedAt);
sealed record ApiError(string Code, string Message);
