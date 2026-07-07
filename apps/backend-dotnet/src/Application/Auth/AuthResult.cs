namespace RemoteAssignment.Application.Auth;

public sealed record AuthResult<T>(
    bool Success,
    T? Data,
    string? ErrorCode,
    string? Message)
{
    public static AuthResult<T> Ok(T data) => new(true, data, null, null);

    public static AuthResult<T> Fail(string errorCode, string message) => new(false, default, errorCode, message);
}
