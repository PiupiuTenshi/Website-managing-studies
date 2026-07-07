using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using RemoteAssignment.Application.Auth;
using RemoteAssignment.Application.Common;
using RemoteAssignment.Application.Health;
using RemoteAssignment.Infrastructure;
using RemoteAssignment.Infrastructure.Auth;
using RemoteAssignment.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys")))
    .SetApplicationName("RemoteAssignment.WebApi");

var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Admin));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole(RoleNames.Student));
    options.AddPolicy("ParentOnly", policy => policy.RequireRole(RoleNames.Parent));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole(RoleNames.Admin, RoleNames.Manager));
    options.AddPolicy("ParentOrAdmin", policy => policy.RequireRole(RoleNames.Admin, RoleNames.Manager, RoleNames.Parent));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Frontend:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            return;
        }

        policy.WithOrigins(allowedOrigins);
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", (IHostEnvironment environment, IConfiguration configuration) =>
{
    var hasDatabaseConnection = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"));

    return Results.Ok(new
    {
        status = "ok",
        service = "RemoteAssignment.WebApi",
        environment = environment.EnvironmentName,
        databaseConfigured = hasDatabaseConnection,
        checkedAt = DateTimeOffset.UtcNow
    });
});

app.MapGet("/health/database", async (
    IDatabaseHealthCheck databaseHealthCheck,
    CancellationToken cancellationToken) =>
{
    var result = await databaseHealthCheck.CheckAsync(cancellationToken);

    return result.Status == "unhealthy"
        ? Results.Problem(title: "Database health check failed", detail: result.Message, statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(result);
});

var auth = app.MapGroup("/api/auth");

auth.MapPost("/login", async (
    LoginRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LoginAsync(request, BuildContext(httpContext), cancellationToken);
    return ToHttpResult(result, httpContext, "Logged in.");
}).AllowAnonymous();

auth.MapPost("/refresh", async (
    RefreshTokenRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await authService.RefreshAsync(request, BuildContext(httpContext), cancellationToken);
    return ToHttpResult(result, httpContext, "Session refreshed.");
}).AllowAnonymous();

auth.MapPost("/logout", async (
    LogoutRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    await authService.LogoutAsync(request, BuildContext(httpContext), cancellationToken);
    return Results.Ok(ApiResponse<object>.Ok(new { loggedOut = true }, httpContext.TraceIdentifier, "Logged out."));
}).RequireAuthorization();

auth.MapGet("/me", async (
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentSession = BuildCurrentSessionRequest(httpContext.User);
    if (currentSession is null)
    {
        return Results.Unauthorized();
    }

    var result = await authService.GetCurrentUserAsync(currentSession, cancellationToken);
    return ToHttpResult(result, httpContext, "Current user loaded.");
}).RequireAuthorization();

app.MapPost("/api/users/{userId:guid}/lock", async (
    Guid userId,
    AccountLockRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LockUserAsync(userId, request, BuildContext(httpContext), cancellationToken);
    return ToHttpResult(result, httpContext, "Account locked.");
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/users/{userId:guid}/unlock", async (
    Guid userId,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await authService.UnlockUserAsync(userId, BuildContext(httpContext), cancellationToken);
    return ToHttpResult(result, httpContext, "Account unlocked.");
}).RequireAuthorization("AdminOnly");

app.MapOrganizationEndpoints();
app.MapAssignmentAuthoringEndpoints();
app.MapSubmissionEndpoints();

app.MapGet("/api/admin/area", [Authorize(Policy = "AdminOnly")] (HttpContext httpContext) =>
{
    return Results.Ok(ApiResponse<object>.Ok(new { area = "admin" }, httpContext.TraceIdentifier));
});

app.MapGet("/api/student/area", [Authorize(Policy = "StudentOnly")] (HttpContext httpContext) =>
{
    return Results.Ok(ApiResponse<object>.Ok(new { area = "student" }, httpContext.TraceIdentifier));
});

app.MapGet("/api/parent/area", [Authorize(Policy = "ParentOnly")] (HttpContext httpContext) =>
{
    return Results.Ok(ApiResponse<object>.Ok(new { area = "parent" }, httpContext.TraceIdentifier));
});

app.Run();

static AuthRequestContext BuildContext(HttpContext httpContext)
{
    return new AuthRequestContext(
        GetUserId(httpContext.User),
        httpContext.Connection.RemoteIpAddress?.ToString(),
        httpContext.Request.Headers.UserAgent.ToString());
}

static CurrentSessionRequest? BuildCurrentSessionRequest(ClaimsPrincipal user)
{
    var userId = GetUserId(user);
    var sessionIdText = user.FindFirstValue(TokenClaimNames.SessionId);
    var activeRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");

    if (!userId.HasValue || !Guid.TryParse(sessionIdText, out var sessionId) || string.IsNullOrWhiteSpace(activeRole))
    {
        return null;
    }

    return new CurrentSessionRequest(userId.Value, sessionId, activeRole);
}

static Guid? GetUserId(ClaimsPrincipal user)
{
    var userIdText = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    return Guid.TryParse(userIdText, out var userId) ? userId : null;
}

static IResult ToHttpResult<T>(AuthResult<T> result, HttpContext httpContext, string successMessage)
{
    if (result.Success && result.Data is not null)
    {
        return Results.Ok(ApiResponse<T>.Ok(result.Data, httpContext.TraceIdentifier, successMessage));
    }

    var statusCode = result.ErrorCode switch
    {
        AuthErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
        AuthErrorCodes.RefreshTokenInvalid => StatusCodes.Status401Unauthorized,
        AuthErrorCodes.SessionRevoked => StatusCodes.Status401Unauthorized,
        AuthErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        AuthErrorCodes.AccountLocked => StatusCodes.Status423Locked,
        AuthErrorCodes.AccountDisabled => StatusCodes.Status403Forbidden,
        AuthErrorCodes.RoleNotAllowed => StatusCodes.Status403Forbidden,
        AuthErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
        "USER_NOT_FOUND" => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status400BadRequest
    };

    var response = ApiResponse<T>.Fail(
        result.ErrorCode ?? "AUTH_ERROR",
        result.Message ?? "Authentication request failed.",
        "Please check your account status or contact an administrator.",
        httpContext.TraceIdentifier);

    return Results.Json(response, statusCode: statusCode);
}
