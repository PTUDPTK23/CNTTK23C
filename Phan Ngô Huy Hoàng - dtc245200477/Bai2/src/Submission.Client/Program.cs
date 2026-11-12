using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Nac 1: Lay danh sach bai tap dang mo
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments ?? []) {
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nac 2: Nop bai, kiem tra 201 truoc khi doc body
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai luot nop: {created?.StudentCode} - {created?.RepositoryUrl}");

// Nac 3: Doc lai theo Location
if (createResponse.Headers.Location != null) {
    var locationResponse = await http.GetAsync(createResponse.Headers.Location);
    if (locationResponse.IsSuccessStatusCode) {
        var readBack = await locationResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai thanh cong: [{readBack?.Id}] Ma SV: {readBack?.StudentCode}");
    }
}

// Nac 4: Thu gui yeu cau loi 400
Console.WriteLine("\nThu gui yeu cau loi: 400 BadRequest");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Records ban sao o Client
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);