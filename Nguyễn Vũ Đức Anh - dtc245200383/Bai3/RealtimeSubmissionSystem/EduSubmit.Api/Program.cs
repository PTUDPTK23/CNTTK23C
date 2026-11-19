using EduSubmit.Api.Hubs;
using EduSubmit.Api.Models;
using EduSubmit.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<SubmissionQueue>();
builder.Services.AddHostedService<SubmissionProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/submissions", (SubmissionStore store) =>
    Results.Ok(store.GetAll()));

app.MapPost("/api/submissions", (
    CreateSubmissionRequest request,
    SubmissionStore store,
    SubmissionQueue queue) =>
{
    if (string.IsNullOrWhiteSpace(request.StudentName))
    {
        return Results.BadRequest(new { error = "StudentName is required." });
    }

    if (string.IsNullOrWhiteSpace(request.AssignmentName))
    {
        return Results.BadRequest(new { error = "AssignmentName is required." });
    }

    var submission = store.Create(request);
    queue.Enqueue(submission.Id);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{id:guid}", (Guid id, SubmissionStore store) =>
{
    var submission = store.Get(id);
    return submission is null ? Results.NotFound() : Results.Ok(submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();
