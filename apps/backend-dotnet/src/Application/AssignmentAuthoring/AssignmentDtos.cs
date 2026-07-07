using System.Text.Json;

namespace RemoteAssignment.Application.AssignmentAuthoring;

public sealed record AssignmentDto(
    Guid Id,
    Guid SubjectId,
    string Title,
    string? Description,
    JsonDocument? ContentJson,
    DateTimeOffset? DeadlineAt,
    bool AllowLateSubmission,
    int? MaxAttempts,
    string Status,
    Guid CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AssignmentTargetDto(
    Guid Id,
    Guid AssignmentId,
    string TargetType,
    Guid TargetId,
    string TargetName);

public sealed record CreateAssignmentRequest(
    Guid SubjectId,
    string Title,
    string? Description,
    JsonDocument? ContentJson,
    DateTimeOffset? DeadlineAt,
    bool AllowLateSubmission,
    int? MaxAttempts);

public sealed record UpdateAssignmentRequest(
    Guid SubjectId,
    string Title,
    string? Description,
    JsonDocument? ContentJson,
    DateTimeOffset? DeadlineAt,
    bool AllowLateSubmission,
    int? MaxAttempts);

public sealed record AssignTargetRequest(
    IReadOnlyList<AssignmentTargetInput> Targets);

public sealed record AssignmentTargetInput(
    string TargetType, // "ClassRoom" or "Student"
    Guid TargetId);
