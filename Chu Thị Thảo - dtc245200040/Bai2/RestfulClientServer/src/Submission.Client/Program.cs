
using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 1. GET danh sach bai tap
Console.WriteLine("=== DANH SACH BAI TAP ===");

var assignmentsResponse = await http.GetAsync("/api/assignments");

if (!assignmentsResponse.IsSuccessStatusCode)
{
    Console.WriteLine($"Loi: {assignmentsResponse.StatusCode}");
    return;
}

var assignments =
    await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();

foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"{assignment.Id}. {assignment.Title}");
}

// 2. POST gui bai tap
Console.WriteLine("\n=== GUI BAI TAP ===");

var assignmentId = 1;

var request = new CreateSubmissionRequest(
    "SV001",
    "https://github.com/ChThao/PTUDPT-K23C"
);

var postResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{assignmentId}/submissions",
    request
);

Console.WriteLine($"HTTP Status: {(int)postResponse.StatusCode} {postResponse.StatusCode}");

if (postResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await postResponse.Content.ReadAsStringAsync();
    Console.WriteLine(error);
    return;
}

var created =
    await postResponse.Content.ReadFromJsonAsync<SubmissionDto>();

Console.WriteLine($"Submission ID: {created?.Id}");
Console.WriteLine($"Trang thai: {created?.Status}");

// 3. GET doc lai submission
Console.WriteLine("\n=== DOC LAI SUBMISSION ===");

if (created != null)
{
    var getResponse = await http.GetAsync(
        $"/api/submissions/{created.Id}"
    );

    Console.WriteLine($"HTTP Status: {(int)getResponse.StatusCode}");

    var result =
        await getResponse.Content.ReadFromJsonAsync<SubmissionDto>();

    Console.WriteLine($"Ma sinh vien: {result?.StudentCode}");
    Console.WriteLine($"Repository: {result?.RepositoryUrl}");
}

// 4. POST du lieu khong hop le
Console.WriteLine("\n=== KIEM THU DU LIEU LOI ===");

var invalidRequest = new CreateSubmissionRequest("", "");

var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{assignmentId}/submissions",
    invalidRequest
);

Console.WriteLine($"HTTP Status: {(int)invalidResponse.StatusCode}");

var invalidError = await invalidResponse.Content.ReadAsStringAsync();
Console.WriteLine(invalidError);

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt
);
