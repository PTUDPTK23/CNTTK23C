using Submission.Api.Models;

namespace Submission.Api.Services;

public interface IAssignmentCatalog
{
    IReadOnlyList<AssignmentResponse> GetAll(CancellationToken cancellationToken);

    bool Contains(int assignmentId);
}
