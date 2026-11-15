using System.Net;
using System.Net.Http.Json;

var baseUrl = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

// Nac 1: GET /api/assignments
Console.WriteLine("=== Nac 1: GET /api/assignments ===");
var listResponse = await http.GetAsync("/api/assignments");
if (listResponse.IsSuccessStatusCode)
{
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    foreach (var assignment in assignments)
    {
        var state = assignment.IsOpen ? "Mo" : "Dong";
        Console.WriteLine($"#{assignment.Id}: {assignment.Title} [{state}] - {assignment.Description}");
    }
}
else
{
    await PrintApiErrorAsync(listResponse);
}

// Nac 2: POST /api/assignments/1/submissions
Console.WriteLine();
Console.WriteLine("=== Nac 2: POST /api/assignments/1/submissions ===");
var createRequest = new CreateSubmissionRequest(
    "dtc245200377",
    "https://github.com/hoangducviet04012006-ux/CNTTK23C");
var createResponse = await http.PostAsJsonAsync("/api/assignments/1/submissions", createRequest);
Uri? createdLocation = null;

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    createdLocation = createResponse.Headers.Location;
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Status: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine($"Location: {createdLocation}");
    if (created is not null)
    {
        Console.WriteLine($"Da nop bai: Id={created.Id}, MSSV={created.StudentCode}, Status={created.Status}");
    }
}
else
{
    await PrintApiErrorAsync(createResponse);
}

// Nac 3: GET theo Location de doc lai luot nop vua tao
Console.WriteLine();
Console.WriteLine("=== Nac 3: GET theo Location ===");
if (createdLocation is not null)
{
    var detailResponse = await http.GetAsync(createdLocation);
    if (detailResponse.IsSuccessStatusCode)
    {
        var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        if (detail is not null)
        {
            Console.WriteLine($"Id: {detail.Id}");
            Console.WriteLine($"AssignmentId: {detail.AssignmentId}");
            Console.WriteLine($"StudentCode: {detail.StudentCode}");
            Console.WriteLine($"RepositoryUrl: {detail.RepositoryUrl}");
            Console.WriteLine($"Status: {detail.Status}");
            Console.WriteLine($"ReceivedAt: {detail.ReceivedAt:O}");
        }
    }
    else
    {
        await PrintApiErrorAsync(detailResponse);
    }
}
else
{
    Console.WriteLine("Khong co Location tu Nac 2 de doc lai.");
}

// Nac 4: Co y gui du lieu sai de kiem tra xu ly loi 400 BadRequest
Console.WriteLine();
Console.WriteLine("=== Nac 4: POST /api/assignments/1/submissions (invalid) ===");
var invalidRequest = new CreateSubmissionRequest("", "");
var invalidResponse = await http.PostAsJsonAsync("/api/assignments/1/submissions", invalidRequest);
if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
{
    await PrintApiErrorAsync(invalidResponse);
}
else
{
    Console.WriteLine($"Status khong mong doi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
    try
    {
        var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
        if (apiError is not null)
        {
            Console.WriteLine($"Error Code: {apiError.Code}");
            Console.WriteLine($"Error Message: {apiError.Message}");
            return;
        }
    }
    catch
    {
        // Phong truong hop loi tu framework khong tra body JSON ApiError
    }

    var raw = await response.Content.ReadAsStringAsync();
    Console.WriteLine(string.IsNullOrWhiteSpace(raw) ? "(Khong co noi dung loi)" : raw);
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
