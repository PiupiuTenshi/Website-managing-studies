namespace RemoteAssignment.Application.Auth;

public sealed record SecretTokenResult(
    string Token,
    string TokenHash);
