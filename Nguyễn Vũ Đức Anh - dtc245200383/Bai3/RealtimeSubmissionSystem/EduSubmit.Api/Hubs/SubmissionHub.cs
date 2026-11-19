using EduSubmit.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace EduSubmit.Api.Hubs;

public sealed class SubmissionHub(SubmissionStore store) : Hub
{
    public static string GroupName(Guid submissionId) =>
        $"submission:{submissionId}";

    public async Task WatchSubmission(Guid submissionId)
    {
        if (store.Get(submissionId) is null)
        {
            throw new HubException("Submission was not found.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(submissionId));
    }
}
