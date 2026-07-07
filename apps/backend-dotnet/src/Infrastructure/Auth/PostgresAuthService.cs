using System.Data;
using System.Text.Json;
using Npgsql;
using RemoteAssignment.Application.Auth;

namespace RemoteAssignment.Infrastructure.Auth;

internal sealed class PostgresAuthService(
    DatabaseOptions databaseOptions,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions) : IAuthService
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    public async Task<AuthResult<AuthTokenResponse>> LoginAsync(LoginRequest request, AuthRequestContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.ValidationFailed, "Login and password are required.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var user = await FindUserByLoginAsync(connection, request.Login, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await WriteAuditLogAsync(connection, null, "auth.login_failed", null, null, context, new { login = request.Login }, cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.InvalidCredentials, "Email/username or password is incorrect.");
        }

        if (user.Status.Equals("Locked", StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuditLogAsync(connection, user.Id, "auth.login_blocked_locked", "users", user.Id, context, null, cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.AccountLocked, "This account is locked.");
        }

        if (!user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuditLogAsync(connection, user.Id, "auth.login_blocked_disabled", "users", user.Id, context, null, cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.AccountDisabled, "This account is not active.");
        }

        var activeRole = ResolveRole(user.Roles, request.Role);
        if (activeRole is null)
        {
            await WriteAuditLogAsync(connection, user.Id, "auth.login_failed_role", "users", user.Id, context, new { requestedRole = request.Role }, cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.RoleNotAllowed, "The requested role is not assigned to this user.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var response = await CreateSessionAndTokensAsync(connection, transaction, user, activeRole, cancellationToken);
        await WriteAuditLogAsync(connection, user.Id, "auth.login_success", "users", user.Id, context, new { role = activeRole }, cancellationToken, transaction);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<AuthTokenResponse>.Ok(response);
    }

    public async Task<AuthResult<AuthTokenResponse>> RefreshAsync(RefreshTokenRequest request, AuthRequestContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.ValidationFailed, "Refresh token is required.");
        }

        var refreshTokenHash = tokenService.HashSecret(request.RefreshToken);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var token = await FindRefreshTokenAsync(connection, transaction, refreshTokenHash, cancellationToken);

        if (token is null || token.UsedAt is not null || token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await WriteAuditLogAsync(connection, null, "auth.refresh_failed", null, null, context, null, cancellationToken, transaction);
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.RefreshTokenInvalid, "Refresh token is invalid or expired.");
        }

        var user = await FindUserByIdAsync(connection, transaction, token.UserId, cancellationToken);
        if (user is null || !user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            await RevokeSessionAsync(connection, transaction, token.SessionId, "User inactive during refresh.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.SessionRevoked, "Session is no longer active.");
        }

        if (!token.SessionStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.SessionRevoked, "Session is no longer active.");
        }

        var activeRole = ResolveRole(user.Roles, token.RoleName);
        if (activeRole is null)
        {
            await RevokeSessionAsync(connection, transaction, token.SessionId, "Role removed during refresh.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AuthTokenResponse>.Fail(AuthErrorCodes.RoleNotAllowed, "The session role is no longer assigned to this user.");
        }

        var response = await RotateRefreshTokenAsync(connection, transaction, token, user, activeRole, cancellationToken);
        await WriteAuditLogAsync(connection, user.Id, "auth.refresh_success", "user_sessions", token.SessionId, context, null, cancellationToken, transaction);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<AuthTokenResponse>.Ok(response);
    }

    public async Task<AuthResult<AuthUserResponse>> GetCurrentUserAsync(CurrentSessionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var user = await FindUserByIdAsync(connection, null, request.UserId, cancellationToken);
        if (user is null || !user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return AuthResult<AuthUserResponse>.Fail(AuthErrorCodes.SessionRevoked, "Session is no longer active.");
        }

        var sessionIsActive = await IsSessionActiveAsync(connection, request.SessionId, request.UserId, cancellationToken);
        if (!sessionIsActive)
        {
            return AuthResult<AuthUserResponse>.Fail(AuthErrorCodes.SessionRevoked, "Session is no longer active.");
        }

        var activeRole = ResolveRole(user.Roles, request.ActiveRole);
        if (activeRole is null)
        {
            return AuthResult<AuthUserResponse>.Fail(AuthErrorCodes.RoleNotAllowed, "The session role is no longer assigned to this user.");
        }

        return AuthResult<AuthUserResponse>.Ok(ToUserResponse(user, activeRole));
    }

    public async Task LogoutAsync(LogoutRequest request, AuthRequestContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var refreshTokenHash = tokenService.HashSecret(request.RefreshToken);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var token = await FindRefreshTokenAsync(connection, transaction, refreshTokenHash, cancellationToken);

        if (token is not null)
        {
            await using var revokeTokenCommand = new NpgsqlCommand(
                """
                update refresh_tokens
                set revoked_at = coalesce(revoked_at, now())
                where id = @id
                """,
                connection,
                transaction);
            revokeTokenCommand.Parameters.AddWithValue("id", token.Id);
            await revokeTokenCommand.ExecuteNonQueryAsync(cancellationToken);

            await RevokeSessionAsync(connection, transaction, token.SessionId, "Logout", cancellationToken);
            await WriteAuditLogAsync(connection, token.UserId, "auth.logout", "user_sessions", token.SessionId, context, null, cancellationToken, transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AuthResult<AccountStatusResponse>> LockUserAsync(Guid userId, AccountLockRequest request, AuthRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Locked by administrator." : request.Reason.Trim();
        var response = await SetUserLockAsync(connection, transaction, userId, "Locked", context.ActorUserId, reason, cancellationToken);
        if (response is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AccountStatusResponse>.Fail("USER_NOT_FOUND", "User was not found.");
        }

        await RevokeAllUserSessionsAsync(connection, transaction, userId, "Account locked.", cancellationToken);
        await WriteAuditLogAsync(connection, context.ActorUserId, "auth.account_locked", "users", userId, context, new { reason }, cancellationToken, transaction);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<AccountStatusResponse>.Ok(response);
    }

    public async Task<AuthResult<AccountStatusResponse>> UnlockUserAsync(Guid userId, AuthRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var response = await SetUserLockAsync(connection, transaction, userId, "Active", context.ActorUserId, null, cancellationToken);
        if (response is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthResult<AccountStatusResponse>.Fail("USER_NOT_FOUND", "User was not found.");
        }

        await WriteAuditLogAsync(connection, context.ActorUserId, "auth.account_unlocked", "users", userId, context, null, cancellationToken, transaction);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<AccountStatusResponse>.Ok(response);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        var connection = new NpgsqlConnection(databaseOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<AuthTokenResponse> CreateSessionAndTokensAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, UserRecord user, string activeRole, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var refreshToken = tokenService.CreateRefreshToken();
        var refreshTokenId = Guid.NewGuid();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenDays);
        var accessTokenLifetime = GetAccessTokenLifetime(activeRole);
        var accessToken = tokenService.CreateAccessToken(ToTokenUser(user, activeRole), sessionId, accessTokenLifetime);

        await using var sessionCommand = new NpgsqlCommand(
            """
            insert into user_sessions (id, user_id, role_name, status, last_used_at, expires_at, access_token_jti)
            values (@id, @userId, @roleName, 'Active', now(), @expiresAt, @accessTokenJti)
            """,
            connection,
            transaction);
        sessionCommand.Parameters.AddWithValue("id", sessionId);
        sessionCommand.Parameters.AddWithValue("userId", user.Id);
        sessionCommand.Parameters.AddWithValue("roleName", activeRole);
        sessionCommand.Parameters.AddWithValue("expiresAt", refreshExpiresAt);
        sessionCommand.Parameters.AddWithValue("accessTokenJti", accessToken.TokenId);
        await sessionCommand.ExecuteNonQueryAsync(cancellationToken);

        await InsertRefreshTokenAsync(connection, transaction, refreshTokenId, user.Id, sessionId, refreshToken.TokenHash, refreshExpiresAt, cancellationToken);

        return new AuthTokenResponse(accessToken.Token, refreshToken.Token, accessToken.ExpiresAt, refreshExpiresAt, ToUserResponse(user, activeRole));
    }

    private async Task<AuthTokenResponse> RotateRefreshTokenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, RefreshTokenRecord token, UserRecord user, string activeRole, CancellationToken cancellationToken)
    {
        var newRefreshToken = tokenService.CreateRefreshToken();
        var newRefreshTokenId = Guid.NewGuid();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenDays);
        var accessToken = tokenService.CreateAccessToken(ToTokenUser(user, activeRole), token.SessionId, GetAccessTokenLifetime(activeRole));

        await using var updateOldCommand = new NpgsqlCommand(
            """
            update refresh_tokens
            set used_at = now(), replaced_by_token_id = @newRefreshTokenId
            where id = @id and used_at is null
            """,
            connection,
            transaction);
        updateOldCommand.Parameters.AddWithValue("id", token.Id);
        updateOldCommand.Parameters.AddWithValue("newRefreshTokenId", newRefreshTokenId);
        await updateOldCommand.ExecuteNonQueryAsync(cancellationToken);

        await InsertRefreshTokenAsync(connection, transaction, newRefreshTokenId, user.Id, token.SessionId, newRefreshToken.TokenHash, refreshExpiresAt, cancellationToken);

        await using var sessionCommand = new NpgsqlCommand(
            """
            update user_sessions
            set last_used_at = now(), expires_at = @expiresAt, access_token_jti = @accessTokenJti
            where id = @sessionId
            """,
            connection,
            transaction);
        sessionCommand.Parameters.AddWithValue("sessionId", token.SessionId);
        sessionCommand.Parameters.AddWithValue("expiresAt", refreshExpiresAt);
        sessionCommand.Parameters.AddWithValue("accessTokenJti", accessToken.TokenId);
        await sessionCommand.ExecuteNonQueryAsync(cancellationToken);

        return new AuthTokenResponse(accessToken.Token, newRefreshToken.Token, accessToken.ExpiresAt, refreshExpiresAt, ToUserResponse(user, activeRole));
    }

    private static async Task InsertRefreshTokenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, Guid userId, Guid sessionId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into refresh_tokens (id, user_id, session_id, token_hash, expires_at)
            values (@id, @userId, @sessionId, @tokenHash, @expiresAt)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static TimeSpan GetAccessTokenLifetime(string role)
    {
        return role.Equals(RoleNames.Student, StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromHours(1)
            : TimeSpan.FromMinutes(15);
    }

    private static string? ResolveRole(IReadOnlyList<string> roles, string? requestedRole)
    {
        if (roles.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(requestedRole))
        {
            return roles[0];
        }

        return roles.FirstOrDefault(role => role.Equals(requestedRole, StringComparison.OrdinalIgnoreCase));
    }

    private static AuthTokenUser ToTokenUser(UserRecord user, string activeRole)
    {
        return new AuthTokenUser(user.Id, user.Email, user.FullName, activeRole, user.Roles);
    }

    private static AuthUserResponse ToUserResponse(UserRecord user, string activeRole)
    {
        return new AuthUserResponse(user.Id, user.Email, user.FullName, user.Status, activeRole, user.Roles);
    }

    private static async Task<UserRecord?> FindUserByLoginAsync(NpgsqlConnection connection, string login, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            select u.id, u.email, u.full_name, u.password_hash, u.status,
                   coalesce(array_agg(r.name order by r.name) filter (where r.name is not null), '{}') as roles
            from users u
            left join user_roles ur on ur.user_id = u.id
            left join roles r on r.id = ur.role_id
            where u.deleted_at is null
              and (lower(u.email) = lower(@login) or lower(coalesce(u.username, '')) = lower(@login))
            group by u.id, u.email, u.full_name, u.password_hash, u.status
            """,
            connection);
        command.Parameters.AddWithValue("login", login.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadUser(reader) : null;
    }

    private static async Task<UserRecord?> FindUserByIdAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid userId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            select u.id, u.email, u.full_name, u.password_hash, u.status,
                   coalesce(array_agg(r.name order by r.name) filter (where r.name is not null), '{}') as roles
            from users u
            left join user_roles ur on ur.user_id = u.id
            left join roles r on r.id = ur.role_id
            where u.deleted_at is null and u.id = @userId
            group by u.id, u.email, u.full_name, u.password_hash, u.status
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadUser(reader) : null;
    }

    private static UserRecord ReadUser(NpgsqlDataReader reader)
    {
        return new UserRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetFieldValue<string[]>(5));
    }

    private static async Task<RefreshTokenRecord?> FindRefreshTokenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string tokenHash, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            select rt.id, rt.user_id, rt.session_id, rt.expires_at, rt.used_at, rt.revoked_at,
                   us.status, us.role_name
            from refresh_tokens rt
            inner join user_sessions us on us.id = rt.session_id
            where rt.token_hash = @tokenHash
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tokenHash", tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RefreshTokenRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
            reader.GetString(6),
            reader.GetString(7));
    }

    private static async Task<bool> IsSessionActiveAsync(NpgsqlConnection connection, Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            update user_sessions
            set last_used_at = now()
            where id = @sessionId
              and user_id = @userId
              and status = 'Active'
              and revoked_at is null
              and expires_at > now()
            """,
            connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static async Task RevokeSessionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            update user_sessions
            set status = 'Revoked', revoked_at = coalesce(revoked_at, now()), revoked_reason = @reason
            where id = @sessionId and status = 'Active'
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("reason", reason);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RevokeAllUserSessionsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid userId, string reason, CancellationToken cancellationToken)
    {
        await using var sessionCommand = new NpgsqlCommand(
            """
            update user_sessions
            set status = 'Revoked', revoked_at = coalesce(revoked_at, now()), revoked_reason = @reason
            where user_id = @userId and status = 'Active'
            """,
            connection,
            transaction);
        sessionCommand.Parameters.AddWithValue("userId", userId);
        sessionCommand.Parameters.AddWithValue("reason", reason);
        await sessionCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var tokenCommand = new NpgsqlCommand(
            """
            update refresh_tokens
            set revoked_at = coalesce(revoked_at, now())
            where user_id = @userId and revoked_at is null
            """,
            connection,
            transaction);
        tokenCommand.Parameters.AddWithValue("userId", userId);
        await tokenCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<AccountStatusResponse?> SetUserLockAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid userId, string status, Guid? actorId, string? reason, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            update users
            set status = @status,
                locked_at = case when @status = 'Locked' then now() else null end,
                locked_by = case when @status = 'Locked' then @actorId else null end,
                lock_reason = case when @status = 'Locked' then @reason else null end,
                updated_at = now()
            where id = @userId and deleted_at is null
            returning id, status, locked_at, lock_reason
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("actorId", actorId.HasValue ? actorId.Value : DBNull.Value);
        command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AccountStatusResponse(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    private static async Task WriteAuditLogAsync(
        NpgsqlConnection connection,
        Guid? actorUserId,
        string action,
        string? entityType,
        Guid? entityId,
        AuthRequestContext context,
        object? metadata,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into audit_logs (actor_user_id, action, entity_type, entity_id, ip_address, user_agent, metadata)
            values (@actorUserId, @action, @entityType, @entityId, cast(@ipAddress as inet), @userAgent, cast(@metadata as jsonb))
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("actorUserId", actorUserId.HasValue ? actorUserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue("entityType", string.IsNullOrWhiteSpace(entityType) ? DBNull.Value : entityType);
        command.Parameters.AddWithValue("entityId", entityId.HasValue ? entityId.Value : DBNull.Value);
        command.Parameters.AddWithValue("ipAddress", string.IsNullOrWhiteSpace(context.IpAddress) ? DBNull.Value : context.IpAddress);
        command.Parameters.AddWithValue("userAgent", string.IsNullOrWhiteSpace(context.UserAgent) ? DBNull.Value : context.UserAgent);
        command.Parameters.AddWithValue("metadata", metadata is null ? "{}" : JsonSerializer.Serialize(metadata));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record UserRecord(
        Guid Id,
        string Email,
        string FullName,
        string PasswordHash,
        string Status,
        IReadOnlyList<string> Roles);

    private sealed record RefreshTokenRecord(
        Guid Id,
        Guid UserId,
        Guid SessionId,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? UsedAt,
        DateTimeOffset? RevokedAt,
        string SessionStatus,
        string RoleName);
}
