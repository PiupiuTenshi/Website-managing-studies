using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RemoteAssignment.Application.Auth;

namespace RemoteAssignment.Infrastructure.Auth;

internal sealed class AuthBootstrapService(
    DatabaseOptions databaseOptions,
    IPasswordHasher passwordHasher,
    IOptions<AuthOptions> authOptions,
    ILogger<AuthBootstrapService> logger) : IHostedService
{
    private static readonly string[] Permissions =
    [
        "accounts.manage",
        "accounts.lock",
        "assignments.manage",
        "submissions.create",
        "grades.view",
        "reports.child.view"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!authOptions.Value.Bootstrap.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
        {
            logger.LogWarning("Auth bootstrap skipped because the database connection string is not configured.");
            return;
        }

        await using var connection = new NpgsqlConnection(databaseOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var role in RoleNames.All)
        {
            await UpsertRoleAsync(connection, transaction, role, cancellationToken);
        }

        foreach (var permission in Permissions)
        {
            await UpsertPermissionAsync(connection, transaction, permission, cancellationToken);
        }

        await MapRolePermissionsAsync(connection, transaction, cancellationToken);

        var passwordHash = passwordHasher.Hash(authOptions.Value.Bootstrap.Password);
        await UpsertUserAsync(connection, transaction, "admin@example.test", "admin", "Phase 1 Admin", passwordHash, RoleNames.Admin, cancellationToken);
        await UpsertUserAsync(connection, transaction, "manager@example.test", "manager", "Phase 1 Manager", passwordHash, RoleNames.Manager, cancellationToken);
        await UpsertUserAsync(connection, transaction, "student@example.test", "student", "Phase 1 Student", passwordHash, RoleNames.Student, cancellationToken);
        await UpsertUserAsync(connection, transaction, "parent@example.test", "parent", "Phase 1 Parent", passwordHash, RoleNames.Parent, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Auth bootstrap completed for roles and development users.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task UpsertRoleAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string role, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into roles (name, description)
            values (@name, @description)
            on conflict (name) do update
            set description = excluded.description,
                updated_at = now()
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("name", role);
        command.Parameters.AddWithValue("description", $"{role} role");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertPermissionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string permission, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into permissions (name, description)
            values (@name, @description)
            on conflict (name) do update
            set description = excluded.description,
                updated_at = now()
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("name", permission);
        command.Parameters.AddWithValue("description", $"{permission} permission");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MapRolePermissionsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        var mappings = new Dictionary<string, string[]>
        {
            [RoleNames.Admin] = Permissions,
            [RoleNames.Manager] = ["assignments.manage", "grades.view"],
            [RoleNames.Student] = ["submissions.create", "grades.view"],
            [RoleNames.Parent] = ["reports.child.view", "grades.view"]
        };

        foreach (var (role, permissions) in mappings)
        {
            foreach (var permission in permissions)
            {
                await using var command = new NpgsqlCommand(
                    """
                    insert into role_permissions (role_id, permission_id)
                    select r.id, p.id
                    from roles r
                    cross join permissions p
                    where r.name = @role and p.name = @permission
                    on conflict (role_id, permission_id) do nothing
                    """,
                    connection,
                    transaction);
                command.Parameters.AddWithValue("role", role);
                command.Parameters.AddWithValue("permission", permission);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static async Task UpsertUserAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string email, string username, string fullName, string passwordHash, string role, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            with upserted_user as (
                insert into users (email, username, full_name, password_hash, status)
                values (@email, @username, @fullName, @passwordHash, 'Active')
                on conflict (email) do update
                set username = excluded.username,
                    full_name = excluded.full_name,
                    password_hash = excluded.password_hash,
                    status = 'Active',
                    locked_at = null,
                    locked_by = null,
                    lock_reason = null,
                    updated_at = now()
                returning id
            )
            insert into user_roles (user_id, role_id)
            select u.id, r.id
            from upserted_user u
            cross join roles r
            where r.name = @role
            on conflict (user_id, role_id) do nothing
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("fullName", fullName);
        command.Parameters.AddWithValue("passwordHash", passwordHash);
        command.Parameters.AddWithValue("role", role);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
