namespace RemoteAssignment.Application.Auth;

public sealed record AuthTokenUser(
    Guid Id,
    string Email,
    string FullName,
    string ActiveRole,
    IReadOnlyList<string> Roles);
