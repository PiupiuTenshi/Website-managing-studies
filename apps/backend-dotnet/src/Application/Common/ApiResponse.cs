namespace RemoteAssignment.Application.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    ApiError? Error,
    string TraceId,
    string? Message)
{
    public static ApiResponse<T> Ok(T data, string traceId, string? message = null)
    {
        return new ApiResponse<T>(true, data, null, traceId, message);
    }

    public static ApiResponse<T> Fail(string code, string message, string userAction, string traceId, IReadOnlyList<string>? details = null)
    {
        return new ApiResponse<T>(
            false,
            default,
            new ApiError(code, message, userAction, details ?? []),
            traceId,
            null);
    }
}
