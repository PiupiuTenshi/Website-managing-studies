namespace RemoteAssignment.Application.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    string ActiveRole,
    IReadOnlyList<string> Roles);
