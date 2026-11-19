using EduSubmit.Api.Models;

namespace EduSubmit.Api.Services;

public sealed class SubmissionStore
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Submission> _submissions = [];

    public Submission Create(CreateSubmissionRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var submission = new Submission(
            Guid.NewGuid(),
            request.StudentName.Trim(),
            request.AssignmentName.Trim(),
            "received",
            now,
            now);

        lock (_sync)
        {
            _submissions.Add(submission.Id, submission);
        }

        return submission;
    }

    public Submission? Get(Guid id)
    {
        lock (_sync)
        {
            return _submissions.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<Submission> GetAll()
    {
        lock (_sync)
        {
            return _submissions.Values
                .OrderByDescending(submission => submission.CreatedAt)
                .ToArray();
        }
    }

    public Submission? UpdateStatus(Guid id, string status)
    {
        lock (_sync)
        {
            if (!_submissions.TryGetValue(id, out var current))
            {
                return null;
            }

            var updated = current with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _submissions[id] = updated;
            return updated;
        }
    }
}
