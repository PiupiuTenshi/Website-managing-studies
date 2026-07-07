namespace RemoteAssignment.Application.Health;

public interface IDatabaseHealthCheck
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken);
}
