using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteAssignment.Application.Auth;

namespace RemoteAssignment.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<AuthOptions> options) : ITokenService
{
    private readonly AuthOptions _options = options.Value;

    public AccessTokenResult CreateAccessToken(AuthTokenUser user, Guid sessionId, TimeSpan lifetime)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(lifetime);
        var tokenId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Role, user.ActiveRole),
            new("role", user.ActiveRole),
            new(TokenClaimNames.SessionId, sessionId.ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim("roles", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), tokenId, expiresAt);
    }

    public SecretTokenResult CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);

        return new SecretTokenResult(token, HashSecret(token));
    }

    public string HashSecret(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
