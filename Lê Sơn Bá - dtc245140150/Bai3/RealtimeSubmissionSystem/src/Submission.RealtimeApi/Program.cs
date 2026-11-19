using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung kenh thoi gian thuc voi SignalR va ASP.NET Core"),
    new(2, "Bai tap REST API", "Thiet ke RESTful API chuan HTTP status codes")
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. Root test endpoint
app.MapGet("/", () => Results.Ok(new { service = "SubmissionApi", status = "running" }));

// 2. Lay danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. Lay thong tin luot nop (nguon su that REST)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    if (submissionId == Guid.Empty)
    {
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Submission ID khong hop le"));
    }

    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop {submissionId}"))
            : Results.Ok(submission);
    }
});

// 4. Tao luot nop bai moi
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra bai tap ton tai
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap {assignmentId}"));
    }

    // Kiem tra du lieu dau vao
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_REQUEST", "studentCode va repositoryUrl khong duoc de trong"));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khoi dong tac vu cham bai o nen (KHONG cho - non-blocking)
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Anh xa SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Các hàm phương thức cục bộ (local functions) trước khai báo class/record
async Task SimulateGradingAsync(
    Guid id,
    string repoUrl,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    // 1 giay sau: chuyen sang processing
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    // 2 giay sau: cham xong (graded hoac failed theo quy tac https://)
    await Task.Delay(TimeSpan.FromSeconds(2));
    string finalStatus = repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "graded" : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    Console.WriteLine($"[Push] submission:{id} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// Lop Hub mỏng: chi quan ly nhom submission:{id}
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        return Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Cac DTO va Model
record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
