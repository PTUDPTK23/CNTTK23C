using Submission.Api.Models;

namespace Submission.Api.Services;

public interface ISubmissionStore
{
    SubmissionResponse Create(int assignmentId, string studentCode, string repositoryUrl);

    SubmissionResponse? Get(Guid submissionId);

    SubmissionResponse? UpdateStatus(Guid submissionId, string status, string message);
}
