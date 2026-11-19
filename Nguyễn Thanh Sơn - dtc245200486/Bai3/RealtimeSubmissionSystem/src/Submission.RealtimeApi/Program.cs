using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai bai nop theo thoi gian thuc", true),
    new(2, "Bai tap REST API", "On lai API assignment va submission", true)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
(int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop bai."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac repository."));

    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Tác vụ nền: mô phỏng chấm bài sau khi đã trả kết quả 201
    _ = SimulateGradingAsync(submission, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(
    SubmissionDto submission,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    var submissionId = submission.Id;

    // Chuyển sang processing sau 1 giây
    await Task.Delay(1000);
    await UpdateStatusAsync(submissionId, "processing", submissions, submissionsLock, hubContext);

    // Chuyển sang graded (hoặc failed nếu repo không hợp lệ) sau 2 giây tiếp theo
    await Task.Delay(2000);
    var finalStatus = submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(submissionId, finalStatus, submissions, submissionsLock, hubContext);
}

static async Task UpdateStatusAsync(
    Guid submissionId,
    string newStatus,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    SubmissionDto? updated;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(item => item.Id == submissionId);
        if (index == -1) return;

        updated = submissions[index] with
        {
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        submissions[index] = updated;
    }

    Console.WriteLine($"[Push] submission:{submissionId} -> {newStatus}");
    await hubContext.Clients.Group($"submission:{submissionId}")
        .SendAsync("SubmissionStatusChanged", new SubmissionStatusMessage(
            updated.Id,
            updated.Status,
            updated.UpdatedAt));
}

class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"submission:{submissionId}");
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
