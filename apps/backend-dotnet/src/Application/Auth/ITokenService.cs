namespace RemoteAssignment.Application.Auth;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AuthTokenUser user, Guid sessionId, TimeSpan lifetime);

    SecretTokenResult CreateRefreshToken();

    string HashSecret(string token);
}
