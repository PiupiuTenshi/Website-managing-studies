namespace RemoteAssignment.Application.Health;

public sealed record DatabaseHealthResult(
    string Status,
    bool IsConfigured,
    string Message,
    DateTimeOffset CheckedAt);
