namespace RemoteAssignment.Application.Auth;

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AccountDisabled = "AUTH_ACCOUNT_DISABLED";
    public const string RoleNotAllowed = "AUTH_ROLE_NOT_ALLOWED";
    public const string RefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string SessionRevoked = "AUTH_SESSION_REVOKED";
    public const string Forbidden = "AUTH_FORBIDDEN";
    public const string ValidationFailed = "VALIDATION_FAILED";
}
