using Npgsql;
using RemoteAssignment.Application.Health;

namespace RemoteAssignment.Infrastructure.Health;

internal sealed class PostgresDatabaseHealthCheck(DatabaseHealthOptions options) : IDatabaseHealthCheck
{
    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new DatabaseHealthResult(
                "not_configured",
                false,
                "ConnectionStrings:DefaultConnection is not configured.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            await using var connection = new NpgsqlConnection(options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return new DatabaseHealthResult(
                "ok",
                true,
                "PostgreSQL connection succeeded.",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or OperationCanceledException)
        {
            return new DatabaseHealthResult(
                "unhealthy",
                true,
                "PostgreSQL connection failed. Check the configured connection string and network access.",
                DateTimeOffset.UtcNow);
        }
    }
}
