namespace RemoteAssignment.Application.Auth;

public sealed record AccessTokenResult(
    string Token,
    Guid TokenId,
    DateTimeOffset ExpiresAt);
