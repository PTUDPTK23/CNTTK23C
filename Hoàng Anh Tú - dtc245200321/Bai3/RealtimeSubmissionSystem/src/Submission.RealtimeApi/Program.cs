using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

// Kiem tra API dang chay.
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// Du lieu bai tap mau.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Theo doi trang thai luot nop", true),
    new(2, "Bai tap Client", "Viet console client", true),
    new(3, "Bai tap da dong", "Dung de kiem tra loi", false)
};

// Luu cac luot nop trong bo nho.
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// Lay danh sach bai tap.
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Lay chi tiet bai tap.
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

// Nop bai.
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hub, ILogger<SubmissionHub> logger) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));
    }

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

    _ = SimulateGradingAsync(submission, submissions, submissionsLock, hub, logger);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// Doc lai luot nop theo ID.
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    }

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay luot nop."));
    }

    return Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(SubmissionDto submission,
    List<SubmissionDto> submissions, object gate, IHubContext<SubmissionHub> hub,
    ILogger<SubmissionHub> logger)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submission.Id, "processing", submissions, gate, hub, logger);
    await Task.Delay(TimeSpan.FromSeconds(2));
    var finalStatus = submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(submission.Id, finalStatus, submissions, gate, hub, logger);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> submissions, object gate, IHubContext<SubmissionHub> hub,
    ILogger<SubmissionHub> logger)
{
    SubmissionDto updated;
    lock (gate)
    {
        var index = submissions.FindIndex(item => item.Id == id);
        if (index < 0)
        {
            return;
        }

        updated = submissions[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        submissions[index] = updated;
    }

    var group = SubmissionGroups.ForSubmission(id);
    var message = new SubmissionStatusMessage(id, updated.Status, updated.UpdatedAt);
    try
    {
        await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", message);
        Console.WriteLine($"[Push] {group} -> {status}");
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Could not publish status for {SubmissionId}", id);
    }
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
        {
            throw new HubException("Ma luot nop khong hop le.");
        }

        var group = SubmissionGroups.ForSubmission(id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo doi {group}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

// Cac DTO.
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

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