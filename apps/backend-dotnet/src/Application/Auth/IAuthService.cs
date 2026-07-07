namespace RemoteAssignment.Application.Auth;

public interface IAuthService
{
    Task<AuthResult<AuthTokenResponse>> LoginAsync(LoginRequest request, AuthRequestContext context, CancellationToken cancellationToken);

    Task<AuthResult<AuthTokenResponse>> RefreshAsync(RefreshTokenRequest request, AuthRequestContext context, CancellationToken cancellationToken);

    Task<AuthResult<AuthUserResponse>> GetCurrentUserAsync(CurrentSessionRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutRequest request, AuthRequestContext context, CancellationToken cancellationToken);

    Task<AuthResult<AccountStatusResponse>> LockUserAsync(Guid userId, AccountLockRequest request, AuthRequestContext context, CancellationToken cancellationToken);

    Task<AuthResult<AccountStatusResponse>> UnlockUserAsync(Guid userId, AuthRequestContext context, CancellationToken cancellationToken);
}
