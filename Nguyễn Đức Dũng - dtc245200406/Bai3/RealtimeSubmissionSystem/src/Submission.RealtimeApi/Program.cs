using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", true),
    new(2, "Bai tap da dong", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    SubmissionDto? submission;
    lock (submissionsLock)
    {
        submission = submissions.Find(item => item.Id == submissionId);
    }

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.Find(item => item.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu studentCode hoac repositoryUrl."));
        }

        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(),
            "received",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto updated;
    lock (gate)
    {
        var i = list.FindIndex(item => item.Id == id);
        updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        list[i] = updated;
    }

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id)).SendAsync("SubmissionStatusChanged", msg);
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission: {submissionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, bool IsOpen);
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
