using Submission.Api.Endpoints;
using Submission.Api.Hubs;
using Submission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IAssignmentCatalog, AssignmentCatalog>();
builder.Services.AddSingleton<ISubmissionStore, SubmissionStore>();
builder.Services.AddSingleton<SubmissionQueue>();
builder.Services.AddHostedService<SubmissionWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();

app.MapGet("/", () => TypedResults.Ok(new { service = "SubmissionApi", status = "running" }))
    .WithName("GetSubmissionApiStatus")
    .WithSummary("Check that the submission API is running")
    .Produces(StatusCodes.Status200OK);

app.MapAssignmentEndpoints();
app.MapSubmissionEndpoints();
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();
