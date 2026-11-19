using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var assignments = new Dictionary<int, AssignmentResponse>
{
    [1] = new(1, "Bài tập REST API", "Tạo API quản lý bài nộp.", true),
    [2] = new(2, "Bài tập đã đóng", "Assignment này không nhận bài nộp.", false)
};
var submissions = new ConcurrentDictionary<Guid, SubmissionResponse>();

var app = builder.Build();

app.MapGet("/api/assignments", () =>
        TypedResults.Ok<IReadOnlyList<AssignmentResponse>>(assignments.Values.ToList()))
    .WithName("GetAssignments")
    .WithSummary("Get all assignments")
    .Produces<IReadOnlyList<AssignmentResponse>>();

app.MapGet("/api/assignments/{id:int}", Results<Ok<AssignmentResponse>, NotFound<ApiError>> (
        int id, CancellationToken cancellationToken) =>
    {
        _ = cancellationToken;
        return assignments.TryGetValue(id, out var assignment)
            ? TypedResults.Ok(assignment)
            : TypedResults.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Assignment không tồn tại."));
    })
    .WithName("GetAssignmentById")
    .WithSummary("Get an assignment by ID")
    .Produces<AssignmentResponse>()
    .Produces<ApiError>(StatusCodes.Status404NotFound);

app.MapPost("/api/assignments/{id:int}/submissions",
        Results<Created<SubmissionResponse>, NotFound<ApiError>, BadRequest<ApiError>> (
            int id, CreateSubmissionRequest? request, CancellationToken cancellationToken) =>
        {
            _ = cancellationToken;

            if (!assignments.TryGetValue(id, out var assignment))
            {
                return TypedResults.NotFound(
                    new ApiError("ASSIGNMENT_NOT_FOUND", "Assignment không tồn tại."));
            }

            if (!assignment.IsOpen)
            {
                return TypedResults.BadRequest(
                    new ApiError("ASSIGNMENT_CLOSED", "Assignment đã đóng, không thể nộp bài."));
            }

            if (request is null
                || string.IsNullOrWhiteSpace(request.StudentName)
                || request.StudentName.Length > 100
                || string.IsNullOrWhiteSpace(request.Content)
                || request.Content.Length > 2000)
            {
                return TypedResults.BadRequest(
                    new ApiError("INVALID_SUBMISSION", "StudentName và Content là bắt buộc; giới hạn lần lượt 100 và 2000 ký tự."));
            }

            var submission = new SubmissionResponse(
                Guid.NewGuid(),
                assignment.Id,
                request.StudentName.Trim(),
                request.Content.Trim(),
                DateTimeOffset.UtcNow);
            submissions[submission.Id] = submission;

            return TypedResults.Created($"/api/submissions/{submission.Id}", submission);
        })
    .WithName("CreateAssignmentSubmission")
    .WithSummary("Submit work to an open assignment")
    .Produces<SubmissionResponse>(StatusCodes.Status201Created)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .Produces<ApiError>(StatusCodes.Status404NotFound);

app.MapGet("/api/submissions/{submissionId:guid}",
        Results<Ok<SubmissionResponse>, NotFound<ApiError>> (
            Guid submissionId, CancellationToken cancellationToken) =>
        {
            _ = cancellationToken;
            return submissions.TryGetValue(submissionId, out var submission)
                ? TypedResults.Ok(submission)
                : TypedResults.NotFound(
                    new ApiError("SUBMISSION_NOT_FOUND", "Submission không tồn tại."));
        })
    .WithName("GetSubmissionById")
    .WithSummary("Get a submission by ID")
    .Produces<SubmissionResponse>()
    .Produces<ApiError>(StatusCodes.Status404NotFound);

app.Run();

/// <summary>Assignment information returned by the API.</summary>
public sealed record AssignmentResponse(int Id, string Title, string Description, bool IsOpen);

/// <summary>Data provided by a client when creating a submission.</summary>
public sealed record CreateSubmissionRequest(string? StudentName, string? Content);

/// <summary>Submission information returned by the API.</summary>
public sealed record SubmissionResponse(
    Guid Id,
    int AssignmentId,
    string StudentName,
    string Content,
    DateTimeOffset SubmittedAt);

/// <summary>A machine-readable API error.</summary>
public sealed record ApiError(string Code, string Message);
