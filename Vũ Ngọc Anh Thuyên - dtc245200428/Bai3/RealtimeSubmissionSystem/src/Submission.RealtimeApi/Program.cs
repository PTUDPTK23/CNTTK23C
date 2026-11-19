using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung ung dung realtime", true),
    new(2, "Bai tap REST Client", "Tich hop REST va SignalR", true)
};
var submissions = new List<SubmissionDto>();
var gate = new object();
app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var a = assignments.FirstOrDefault(x => x.Id == assignmentId);
    return a is null ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap.")) : Results.Ok(a);
});
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hub) =>
{
    var a = assignments.FirstOrDefault(x => x.Id == assignmentId);
    if (a is null) return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    if (!a.IsOpen) return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode va RepositoryUrl khong duoc de trong."));
    var now = DateTimeOffset.UtcNow;
    var s = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(), request.RepositoryUrl.Trim(), "received", now, now);
    lock (gate) submissions.Add(s);
    _ = SimulateGradingAsync(s.Id, submissions, gate, hub);
    return Results.Created($"/api/submissions/{s.Id}", s);
});
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    lock (gate)
    {
        var s = submissions.FirstOrDefault(x => x.Id == id);
        return s is null ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop.")) : Results.Ok(s);
    }
});
app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}
static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(x => x.Id == id);
        if (i >= 0) { updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow }; list[i] = updated; }
    }
    if (updated is null) return;
    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    var group = SubmissionGroups.ForSubmission(id);
    await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", message);
    Console.WriteLine($"[Push] {group} -> {status}");
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
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
