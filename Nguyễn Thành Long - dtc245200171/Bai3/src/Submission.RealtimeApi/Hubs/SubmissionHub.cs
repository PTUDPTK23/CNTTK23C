using Microsoft.AspNetCore.SignalR;
using Submission.Api.Services;

namespace Submission.Api.Hubs;

public sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var normalizedId = submissionId.StartsWith("submission:", StringComparison.OrdinalIgnoreCase)
            ? submissionId
            : $"submission:{submissionId}";

        var shortConn = Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)];
        Console.WriteLine($"[Hub] {shortConn} theo doi {normalizedId}");

        return Groups.AddToGroupAsync(Context.ConnectionId, normalizedId, Context.ConnectionAborted);
    }
}
