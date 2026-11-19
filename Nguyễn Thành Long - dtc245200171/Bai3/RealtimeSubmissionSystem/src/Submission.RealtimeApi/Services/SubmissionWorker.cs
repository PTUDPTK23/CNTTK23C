using Microsoft.AspNetCore.SignalR;
using Submission.Api.Hubs;

namespace Submission.Api.Services;

public sealed class SubmissionWorker(
    SubmissionQueue queue,
    ISubmissionStore store,
    IHubContext<SubmissionHub> hub,
    ILogger<SubmissionWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Parallel.ForEachAsync(
            queue.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                CancellationToken = stoppingToken,
                MaxDegreeOfParallelism = 4
            },
            ProcessSubmissionAsync);

    private async ValueTask ProcessSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var processing = store.UpdateStatus(submissionId, "processing", "Grading has started.");
            if (processing is null)
            {
                logger.LogError("Queued submission {SubmissionId} was not found in the store.", submissionId);
                return;
            }

            await PublishStatusAsync(processing, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var failed = processing.RepositoryUrl.Contains("/fail", StringComparison.OrdinalIgnoreCase);
            var status = failed ? "failed" : "graded";
            var message = failed
                ? "The repository was rejected by the sample grader."
                : "Grading completed successfully.";
            var completed = store.UpdateStatus(submissionId, status, message);
            if (completed is null)
            {
                logger.LogError("Submission {SubmissionId} disappeared while grading.", submissionId);
                return;
            }

            await PublishStatusAsync(completed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed while grading submission {SubmissionId}.", submissionId);
            var failed = store.UpdateStatus(
                submissionId,
                "failed",
                "The grader encountered an internal error.");
            if (failed is not null)
            {
                await PublishStatusAsync(failed, cancellationToken);
            }
        }
    }

    private async Task PublishStatusAsync(
        Submission.Api.Models.SubmissionResponse submission,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Push] submission:{submission.Id:D} -> {submission.Status}");
        var msg = new Submission.Api.Models.SubmissionStatusMessage(
            submission.Id,
            submission.Status,
            submission.UpdatedAt,
            submission.Message,
            submission.AssignmentId,
            submission.StudentCode,
            submission.RepositoryUrl);

        await hub.Clients
            .Group($"submission:{submission.Id:D}")
            .SendAsync("SubmissionStatusChanged", msg, cancellationToken);
    }
}
