using EduSubmit.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EduSubmit.Api.Services;

public sealed class SubmissionProcessor(
    SubmissionQueue queue,
    SubmissionStore store,
    IHubContext<SubmissionHub> hubContext,
    ILogger<SubmissionProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var submissionId in queue.ReadAllAsync(stoppingToken))
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            await UpdateAndNotifyAsync(submissionId, "processing", stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            await UpdateAndNotifyAsync(submissionId, "graded", stoppingToken);
        }
    }

    private async Task UpdateAndNotifyAsync(
        Guid submissionId,
        string status,
        CancellationToken cancellationToken)
    {
        var submission = store.UpdateStatus(submissionId, status);
        if (submission is null)
        {
            logger.LogWarning(
                "Submission {SubmissionId} disappeared before status {Status}.",
                submissionId,
                status);
            return;
        }

        await hubContext.Clients
            .Group(SubmissionHub.GroupName(submissionId))
            .SendAsync("SubmissionStatusChanged", submission, cancellationToken);
    }
}
