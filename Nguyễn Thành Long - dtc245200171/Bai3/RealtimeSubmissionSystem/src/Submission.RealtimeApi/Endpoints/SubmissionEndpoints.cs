using Microsoft.AspNetCore.Http.HttpResults;
using Submission.Api.Models;
using Submission.Api.Services;

namespace Submission.Api.Endpoints;

public static class SubmissionEndpoints
{
    public static WebApplication MapSubmissionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/submissions/{submissionId:guid}",
                Results<Ok<SubmissionResponse>, NotFound> (
                    Guid submissionId,
                    ISubmissionStore store,
                    CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var submission = store.Get(submissionId);
                    return submission is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(submission);
                })
            .WithName("GetSubmission")
            .WithSummary("Get the latest state of a submission")
            .Produces<SubmissionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
