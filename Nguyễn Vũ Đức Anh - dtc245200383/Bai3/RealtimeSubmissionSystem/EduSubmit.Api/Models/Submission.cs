namespace EduSubmit.Api.Models;

public sealed record Submission(
    Guid Id,
    string StudentName,
    string AssignmentName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
