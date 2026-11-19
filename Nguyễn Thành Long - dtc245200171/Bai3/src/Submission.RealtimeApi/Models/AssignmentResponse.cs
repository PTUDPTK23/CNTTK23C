namespace Submission.Api.Models;

/// <summary>Describes an assignment accepted by the submission service.</summary>
public sealed record AssignmentResponse(int Id, string Title, string Description);
