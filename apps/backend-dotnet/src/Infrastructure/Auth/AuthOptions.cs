namespace RemoteAssignment.Infrastructure.Auth;

public sealed class AuthOptions
{
    public string Issuer { get; init; } = "remote-assignment";

    public string Audience { get; init; } = "remote-assignment-users";

    public string SigningKey { get; init; } = "dev-only-signing-key-change-before-deployment-32chars";

    public int RefreshTokenDays { get; init; } = 14;

    public BootstrapOptions Bootstrap { get; init; } = new();
}
