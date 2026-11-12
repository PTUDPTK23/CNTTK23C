using System.Net;
using System.Net.Http.Json;

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000")
};

try
{
    Console.WriteLine("Nac 1 - Danh sach bai tap");
    var assignments = await client.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments") ?? [];
    foreach (var assignment in assignments)
    {
        Console.WriteLine($"[{assignment.Id}] {assignment.Title} | Mo: {assignment.IsOpen}");
    }

    Console.WriteLine("\nNac 2 - Nop bai hop le");
    var request = new CreateSubmissionRequest("SV001", "https://github.com/student/edusubmit");
    using var createResponse = await client.PostAsJsonAsync("/api/assignments/1/submissions", request);
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Nop bai that bai: {(int)createResponse.StatusCode} {error?.Code}: {error?.Message}");
        return;
    }

    var submission = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    var location = createResponse.Headers.Location;
    if (submission is null || location is null)
    {
        Console.WriteLine("Server khong tra ve body hoac header Location hop le.");
        return;
    }

    Console.WriteLine($"Nop bai thanh cong. ID: {submission.Id}");
    Console.WriteLine($"Location: {location}");

    Console.WriteLine("\nNac 3 - Doc lai luot nop");
    using var readResponse = await client.GetAsync(location);
    if (readResponse.IsSuccessStatusCode)
    {
        var readSubmission = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine(readSubmission is null
            ? "Khong doc duoc chi tiet luot nop."
            : $"{readSubmission.StudentCode} | {readSubmission.Status} | {readSubmission.RepositoryUrl}");
    }
    else
    {
        Console.WriteLine($"Doc lai that bai: {(int)readResponse.StatusCode}");
    }

    Console.WriteLine("\nNac 4 - Gui du lieu sai de nhan 400");
    var invalidRequest = new CreateSubmissionRequest("", "");
    using var invalidResponse = await client.PostAsJsonAsync("/api/assignments/1/submissions", invalidRequest);
    if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
    {
        var error = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"{(int)invalidResponse.StatusCode} {error?.Code}: {error?.Message}");
    }
    else
    {
        Console.WriteLine($"Mong doi 400, nhan {(int)invalidResponse.StatusCode}.");
    }
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Khong ket noi duoc API tai {client.BaseAddress}: {exception.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);