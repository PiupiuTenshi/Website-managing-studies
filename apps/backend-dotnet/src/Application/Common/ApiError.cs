namespace RemoteAssignment.Application.Common;

public sealed record ApiError(
    string Code,
    string Message,
    string UserAction,
    IReadOnlyList<string> Details);
