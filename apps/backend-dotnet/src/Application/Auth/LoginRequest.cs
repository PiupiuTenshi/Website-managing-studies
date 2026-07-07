namespace RemoteAssignment.Application.Auth;

public sealed record LoginRequest(
    string Login,
    string Password,
    string? Role);
