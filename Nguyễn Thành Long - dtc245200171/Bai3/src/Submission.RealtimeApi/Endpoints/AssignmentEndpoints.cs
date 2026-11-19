using Microsoft.AspNetCore.Http.HttpResults;
using Submission.Api.Models;
using Submission.Api.Services;

namespace Submission.Api.Endpoints;

public static class AssignmentEndpoints
{
    public static WebApplication MapAssignmentEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/assignments",
                (IAssignmentCatalog catalog, CancellationToken cancellationToken) =>
                    TypedResults.Ok(catalog.GetAll(cancellationToken)))
            .WithName("GetAssignments")
            .WithSummary("List available assignments")
            .Produces<IReadOnlyList<AssignmentResponse>>(StatusCodes.Status200OK);

        app.MapPost(
                "/api/assignments/{assignmentId:int}/submissions",
                Results<Created<SubmissionResponse>, ValidationProblem, NotFound, ProblemHttpResult> (
                    int assignmentId,
                    CreateSubmissionRequest request,
                    IAssignmentCatalog catalog,
                    ISubmissionStore store,
                    SubmissionQueue queue,
                    CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var errors = new Dictionary<string, string[]>();
                    if (string.IsNullOrWhiteSpace(request.StudentCode))
                    {
                        errors[nameof(request.StudentCode)] = ["StudentCode is required."];
                    }

                    if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repositoryUri) ||
                        repositoryUri.Scheme is not ("http" or "https"))
                    {
                        errors[nameof(request.RepositoryUrl)] =
                            ["RepositoryUrl must be an absolute HTTP or HTTPS URL."];
                    }

                    if (errors.Count > 0)
                    {
                        return TypedResults.ValidationProblem(errors);
                    }

                    if (!catalog.Contains(assignmentId))
                    {
                        return TypedResults.NotFound();
                    }

                    var submission = store.Create(
                        assignmentId,
                        request.StudentCode!.Trim(),
                        repositoryUri!.AbsoluteUri);
                    if (!queue.Enqueue(submission.Id))
                    {
                        return TypedResults.Problem(
                            title: "Submission queue unavailable",
                            statusCode: StatusCodes.Status503ServiceUnavailable);
                    }

                    return TypedResults.Created($"/api/submissions/{submission.Id:D}", submission);
                })
            .WithName("CreateSubmission")
            .WithSummary("Queue an assignment submission for grading")
            .WithDescription("Returns a submission resource with status received and processes it asynchronously.")
            .Produces<SubmissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}
