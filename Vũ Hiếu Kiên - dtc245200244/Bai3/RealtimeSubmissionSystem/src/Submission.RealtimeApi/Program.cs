using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai bai nop theo thoi gian thuc", true),
    new(2, "Bai tap REST API", "On lai API assignment va submission", true),
    new(3, "Bai tap da dong", "Bai tap mau da het han nop", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

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
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop bai."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac repository."));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(
    Guid submissionId,
    string repositoryUrl,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        await UpdateStatusAsync(submissionId, "processing", submissions, submissionsLock, hubContext);

        await Task.Delay(TimeSpan.FromSeconds(2));
        // Bai tap 2 (Slide 62): RepositoryUrl khong bat dau bang https:// -> failed
        var finalStatus = repositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "graded"
            : "failed";
        await UpdateStatusAsync(submissionId, finalStatus, submissions, submissionsLock, hubContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Grading Error] {submissionId}: {ex.Message}");
    }
}

static async Task UpdateStatusAsync(
    Guid submissionId,
    string status,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    SubmissionDto? updatedSubmission = null;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(item => item.Id == submissionId);
        if (index >= 0)
        {
            updatedSubmission = submissions[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            submissions[index] = updatedSubmission;
        }
    }

    if (updatedSubmission is null)
    {
        return;
    }

    var groupName = SubmissionGroups.ForSubmission(submissionId);
    var message = new SubmissionStatusMessage(
        updatedSubmission.Id,
        updatedSubmission.Status,
        updatedSubmission.UpdatedAt);

    Console.WriteLine($"[Push] {groupName} -> {updatedSubmission.Status}");
    await hubContext.Clients
        .Group(groupName)
        .SendAsync("SubmissionStatusChanged", message);
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)]} theo doi {groupName}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid submissionId) => ForSubmission(submissionId.ToString());
    public static string ForSubmission(string submissionId) => $"submission:{submissionId}";
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
