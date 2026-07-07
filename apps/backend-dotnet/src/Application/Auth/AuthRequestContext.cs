namespace RemoteAssignment.Application.Auth;

public sealed record AuthRequestContext(
    Guid? ActorUserId,
    string? IpAddress,
    string? UserAgent);
