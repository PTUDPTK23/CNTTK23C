using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", IsClosed: false),
    new(2, "Bai tap REST da dong", IsClosed: true)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND",
                $"Khong tim thay luot nop {submissionId}."))
            : Results.Ok(submission);
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    IResult (int assignmentId,
             CreateSubmissionRequest request,
             IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND",
                $"Khong tim thay bai tap {assignmentId}."));
        }

        if (assignment.IsClosed)
        {
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED",
                $"Bai tap {assignmentId} da dong."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION",
                "studentCode va repositoryUrl la bat buoc."));
        }

        if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repoUri) ||
            (repoUri.Scheme != Uri.UriSchemeHttp && repoUri.Scheme != Uri.UriSchemeHttps))
        {
            return Results.BadRequest(new ApiError("INVALID_REPOSITORY_URL",
                "repositoryUrl phai la URL http/https tuyet doi."));
        }

        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(),
            Status: "received",
            ReceivedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);

        lock (submissionsLock) { submissions.Add(submission); }

        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus;
    lock (gate)
    {
        var item = list.FirstOrDefault(s => s.Id == id);
        finalStatus = item is not null &&
                      !item.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "failed"
            : "graded";
    }

    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
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

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] submission:{updated.Id} -> {updated.Status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
             .SendAsync("SubmissionStatusChanged", msg);
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

record AssignmentDto(int Id, string Title, bool IsClosed);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
