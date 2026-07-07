using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Common;
using RemoteAssignment.Application.AssignmentAuthoring;
using RemoteAssignment.Application.Auth;

namespace RemoteAssignment.WebApi;

public static class AssignmentAuthoringEndpoints
{
    public static void MapAssignmentAuthoringEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/assignments").RequireAuthorization("ManagerOrAdmin");

        api.MapGet("/", async (Guid? subjectId, Guid? createdBy, string? status, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.ListAssignmentsAsync(subjectId, createdBy, status, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<AssignmentDto>>.Ok(result, httpContext.TraceIdentifier));
        });

        api.MapGet("/{id:guid}", async (Guid id, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GetAssignmentAsync(id, ct);
            return result is not null
                ? Results.Ok(ApiResponse<AssignmentDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.NotFound();
        });

        api.MapPost("/", async (CreateAssignmentRequest request, IAssignmentAuthoringService service, IAuthService auth, HttpContext httpContext, CancellationToken ct) =>
        {
            // Note: In real app, we get Current User ID from claims.
            // For MVP, we will extract it from the context or a token decoding.
            // Since we use PostgresAuthService, we can trust the HttpContext.User.
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await service.CreateAssignmentAsync(request, userId, ct);
            return result is not null
                ? Results.Ok(ApiResponse<AssignmentDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });

        api.MapPut("/{id:guid}", async (Guid id, UpdateAssignmentRequest request, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.UpdateAssignmentAsync(id, request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<AssignmentDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.NotFound();
        });

        api.MapDelete("/{id:guid}", async (Guid id, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.DeleteAssignmentAsync(id, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/{id:guid}/publish", async (Guid id, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.PublishAssignmentAsync(id, ct);
            return result is not null
                ? Results.Ok(ApiResponse<AssignmentDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest(ApiResponse<object>.Fail("PUBLISH_FAILED", "Could not publish. Maybe already published.", "", httpContext.TraceIdentifier));
        });

        api.MapGet("/{id:guid}/targets", async (Guid id, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GetAssignmentTargetsAsync(id, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<AssignmentTargetDto>>.Ok(result, httpContext.TraceIdentifier));
        });

        api.MapPost("/{id:guid}/targets", async (Guid id, AssignTargetRequest request, IAssignmentAuthoringService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.SetAssignmentTargetsAsync(id, request, ct);
            return result 
                ? Results.Ok(ApiResponse<object>.Ok(new { success = true }, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });
    }
}
