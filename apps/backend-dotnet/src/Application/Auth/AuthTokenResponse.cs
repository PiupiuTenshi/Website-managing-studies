namespace RemoteAssignment.Application.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUserResponse User);
