namespace Submission.Api.Models;

/// <summary>Identifies the student and source repository for a submission.</summary>
public sealed record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
