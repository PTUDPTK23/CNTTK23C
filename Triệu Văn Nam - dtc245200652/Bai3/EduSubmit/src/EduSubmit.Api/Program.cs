using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
var submissions = new ConcurrentDictionary<string, Submission>();

app.MapGet("/api/submissions", () => Results.Ok(submissions.Values));

app.MapGet("/api/submissions/{id}", (string id) =>
    submissions.TryGetValue(id, out var sub)
        ? Results.Ok(sub)
        : Results.NotFound(new { error = $"Khong tim thay bai nop '{id}'" }));

app.MapPost("/api/submissions", async (SubmissionRequest req, IHubContext<SubmissionHub> hub) =>
{
    if (string.IsNullOrWhiteSpace(req.StudentId) || string.IsNullOrWhiteSpace(req.Assignment))
        return Results.BadRequest(new { error = "Thieu StudentId hoac Assignment" });

    var id = "SUB-" + Random.Shared.Next(1000, 9999);
    var sub = new Submission(id, req.StudentId, req.Assignment, "pending", null);
    submissions[id] = sub;

    await hub.Clients.All.SendAsync("SubmissionUpdated", sub);

    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        var graded = sub with { Status = "graded", Score = 9.5 };
        submissions[id] = graded;
        await hub.Clients.All.SendAsync("SubmissionUpdated", graded);
    });

    return Results.Created($"/api/submissions/{id}", sub);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

record SubmissionRequest(string StudentId, string Assignment);
record Submission(string Id, string StudentId, string Assignment, string Status, double? Score);
sealed class SubmissionHub : Hub { }