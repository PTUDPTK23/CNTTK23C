using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Danh sach du lieu mau trong bo nho
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung kenh thoi gian thuc voi SignalR", true),
    new(2, "Bai tap Client", "Viet console client ket noi Hub", true),
    new(3, "Bai tap Cuoi ky (Da dong)", "Bai tap da het han nop", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. GET / : Kiem tra may chu dang chay
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. GET /api/assignments : Danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Chi tiet bai tap
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 3. POST /api/assignments/{assignmentId:int}/submissions : Tao luot nop
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra bai tap ton tai
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Kiem tra bai tap con mo
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    // Kiem tra du lieu hop le
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khoi dong cham bai o nen qua IHubContext, KHONG cho
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} : Doc lai luot nop (nguon su that)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// Anh xa hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Mo phong cham bai o nen (local functions)
static async Task SimulateGradingAsync(
    Guid id,
    string repoUrl,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    // Bai tap 2 van dung: URL khong bat dau bang https:// se la failed
    string finalStatus = repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "graded" : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(
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
            updated = list[i] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// Dinh nghia Hub
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Record models
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
