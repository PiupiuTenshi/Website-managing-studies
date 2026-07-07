namespace RemoteAssignment.Application.Auth;

public sealed record CurrentSessionRequest(
    Guid UserId,
    Guid SessionId,
    string ActiveRole);
