using System.Net.Http.Json;

// Địa chỉ API lấy từ tham số dòng lệnh, mặc định cổng 5000
var baseAddress = args.Length > 0 ? args[0] : "http://localhost:5000";

using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };

Console.WriteLine($"=== KET NOI TOI SERVER: {baseAddress} ===");

try
{
    Console.WriteLine("\n1. Lay danh sach bai tap:");
    var assignments = await client.GetFromJsonAsync<List<Assignment>>("/api/assignments");
    if (assignments != null)
    {
        foreach (var a in assignments)
        {
            Console.WriteLine($"   - [{a.Id}] {a.Title}: {a.Description}");
        }
    }

    Console.WriteLine("\n2. Dang nop bai...");
    var newSubmission = new CreateSubmissionRequest(
        1, "dtc245201006", "https://github.com/minesan2297/EduSubmit-dtc245201006");
    var response = await client.PostAsJsonAsync("/api/submissions", newSubmission);

    // Ném lỗi nếu mã HTTP không phải 2xx
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine("   Da nop bai thanh cong!");
    Console.WriteLine($"   Ma luot nop: {result!.Id}");
    Console.WriteLine($"   Trang thai: {result.Status} luc {result.ReceivedAt}");

    Console.WriteLine("\n3. Doc lai luot nop tu Server qua Id:");
    var retrieved = await client.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    if (retrieved != null)
    {
        Console.WriteLine($"   Luot nop ton tai: Ma SV {retrieved.StudentCode}, Bai tap {retrieved.AssignmentId}");
    }
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[LOI GOI API]: {ex.Message}");
    Console.ResetColor();
}

// Client tự định nghĩa DTO riêng, không dùng chung code với API
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
