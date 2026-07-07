namespace RemoteAssignment.Application.Auth;

public sealed record AccountStatusResponse(
    Guid UserId,
    string Status,
    DateTimeOffset? LockedAt,
    string? LockReason);
