using Submission.Api.Models;

namespace Submission.Api.Services;

public sealed class AssignmentCatalog : IAssignmentCatalog
{
    private static readonly AssignmentResponse[] Assignments =
    [
        new(1, "Bai 3 SignalR", "Submit a repository URL for asynchronous grading.")
    ];

    public IReadOnlyList<AssignmentResponse> GetAll(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Assignments;
    }

    public bool Contains(int assignmentId) => Assignments.Any(item => item.Id == assignmentId);
}
