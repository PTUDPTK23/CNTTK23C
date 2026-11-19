using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();                       // truoc builder.Build()
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", DateTimeOffset.UtcNow.AddDays(7), false),
    new(2, "Bai tap REST (da dong)", DateTimeOffset.UtcNow.AddDays(-1), true)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();                  // tac vu nen + HTTP cung sua

app.MapGet("/", () => Results.Ok(new { service = "RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

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

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest? request, IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)                                              // 404
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (assignment.IsClosed)                                             // 400 CLOSED
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (request is null || string.IsNullOrWhiteSpace(request.StudentCode)
            || string.IsNullOrWhiteSpace(request.RepositoryUrl))             // 400 INVALID
            return Results.BadRequest(new ApiError("INVALID_REQUEST", "Thieu studentCode hoac repositoryUrl."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            request.StudentCode!, request.RepositoryUrl!, "received", now, now);

        lock (submissionsLock) { submissions.Add(submission); }

        // Bai tap 2: url khong bat dau bang https:// -> failed
        var willFail = !request.RepositoryUrl!.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        // khoi dong cham bai o nen, KHONG cho
        _ = SimulateGradingAsync(submission.Id, willFail, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);       // 201
    });

app.MapHub<SubmissionHub>("/hubs/submissions");      // truoc app.Run()

app.Run();

static async Task SimulateGradingAsync(Guid id, bool willFail, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, willFail ? "failed" : "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)                                              // 1. cap nhat du lieu truoc
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // 2. toi thieu
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);                                     // 3. dung nhom
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, DateTimeOffset DueAt, bool IsClosed);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
