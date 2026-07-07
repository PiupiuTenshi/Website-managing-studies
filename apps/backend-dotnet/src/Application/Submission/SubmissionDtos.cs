using System.Text.Json;

namespace RemoteAssignment.Application.Submission;

public sealed record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    JsonDocument? ContentJson,
    string Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? GradedAt,
    decimal? GradeScore,
    JsonDocument? FeedbackJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SubmissionFileDto(
    Guid Id,
    Guid SubmissionId,
    string FileName,
    long FileSize,
    string MimeType,
    string StorageKey,
    string Url,
    DateTimeOffset CreatedAt);

public sealed record DraftSubmissionRequest(
    JsonDocument? ContentJson);

public sealed record SubmitAssignmentRequest(
    JsonDocument? ContentJson);

public sealed record StudentAssignmentDto(
    Guid AssignmentId,
    string Title,
    string? Description,
    DateTimeOffset? DeadlineAt,
    bool AllowLateSubmission,
    string Status, // Assignment status: 'Published'
    string CreatedByName,
    DateTimeOffset CreatedAt,
    Guid? SubmissionId,
    string? SubmissionStatus, // Null if not started
    DateTimeOffset? SubmittedAt,
    decimal? GradeScore);
