using System.Collections.Concurrent;
using Submission.Api.Models;

namespace Submission.Api.Services;

public sealed class SubmissionStore : ISubmissionStore
{
    private readonly ConcurrentDictionary<Guid, SubmissionResponse> _submissions = new();

    public SubmissionResponse Create(int assignmentId, string studentCode, string repositoryUrl)
    {
        var submission = new SubmissionResponse(
            Guid.NewGuid(),
            assignmentId,
            studentCode,
            repositoryUrl,
            "received",
            "Submission received.",
            DateTimeOffset.UtcNow);
        _submissions[submission.Id] = submission;
        return submission;
    }

    public SubmissionResponse? Get(Guid submissionId) =>
        _submissions.TryGetValue(submissionId, out var submission) ? submission : null;

    public SubmissionResponse? UpdateStatus(Guid submissionId, string status, string message)
    {
        while (_submissions.TryGetValue(submissionId, out var current))
        {
            var updated = current with
            {
                Status = status,
                Message = message,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            if (_submissions.TryUpdate(submissionId, updated, current))
            {
                return updated;
            }
        }

        return null;
    }
}
