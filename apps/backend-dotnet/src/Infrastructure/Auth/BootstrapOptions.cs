namespace RemoteAssignment.Infrastructure.Auth;

public sealed class BootstrapOptions
{
    public bool Enabled { get; init; }

    public string Password { get; init; } = "ChangeMe123!";
}
