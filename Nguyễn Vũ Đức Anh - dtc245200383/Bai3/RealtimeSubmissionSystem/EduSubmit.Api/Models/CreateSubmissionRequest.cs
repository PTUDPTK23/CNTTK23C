namespace EduSubmit.Api.Models;

public sealed record CreateSubmissionRequest(
    string StudentName,
    string AssignmentName);
