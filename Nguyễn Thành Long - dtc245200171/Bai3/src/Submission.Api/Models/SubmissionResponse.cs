namespace Submission.Api.Models;

/// <summary>Reports the current state of an assignment submission.</summary>
public sealed record SubmissionResponse(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    string Message,
    DateTimeOffset UpdatedAt)
{
    public Guid SubmissionId => Id;
}

public sealed record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt,
    string? Message = null,
    int? AssignmentId = null,
    string? StudentCode = null,
    string? RepositoryUrl = null)
{
    public Guid Id => SubmissionId;
}

