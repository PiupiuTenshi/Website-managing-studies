using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Common;
using RemoteAssignment.Application.Submission;

namespace RemoteAssignment.WebApi;

public static class SubmissionEndpoints
{
    public static void MapSubmissionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/student/assignments").RequireAuthorization("StudentOnly");

        api.MapGet("/", async (ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await service.GetStudentAssignmentsAsync(userId, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<StudentAssignmentDto>>.Ok(result, httpContext.TraceIdentifier));
        });

        api.MapGet("/{id:guid}/submission", async (Guid id, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await service.GetSubmissionByAssignmentAsync(id, userId, ct);
            // It's okay to return null if they haven't submitted yet
            return Results.Ok(ApiResponse<SubmissionDto?>.Ok(result, httpContext.TraceIdentifier));
        });

        api.MapPost("/{id:guid}/submission/draft", async (Guid id, DraftSubmissionRequest request, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await service.DraftSubmissionAsync(id, userId, request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<SubmissionDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });

        api.MapPost("/{id:guid}/submission/submit", async (Guid id, SubmitAssignmentRequest request, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            try
            {
                var result = await service.SubmitAsync(id, userId, request, ct);
                return result is not null
                    ? Results.Ok(ApiResponse<SubmissionDto>.Ok(result, httpContext.TraceIdentifier))
                    : Results.BadRequest();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<object>.Fail("SUBMIT_FAILED", ex.Message, "", httpContext.TraceIdentifier));
            }
        });

        // Manager APIs
        var managerApi = app.MapGroup("/api/manager/assignments").RequireAuthorization("ManagerOrAdmin");

        managerApi.MapGet("/{assignmentId:guid}/submissions", async (Guid assignmentId, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GetSubmissionsForAssignmentAsync(assignmentId, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ManagerSubmissionDto>>.Ok(result, httpContext.TraceIdentifier));
        });

        managerApi.MapGet("/submissions/{submissionId:guid}", async (Guid submissionId, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GetSubmissionDetailForManagerAsync(submissionId, ct);
            return result is not null
                ? Results.Ok(ApiResponse<SubmissionDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.NotFound();
        });

        managerApi.MapPost("/submissions/{submissionId:guid}/grade", async (Guid submissionId, GradeSubmissionRequest request, ISubmissionService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GradeSubmissionAsync(submissionId, request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<SubmissionDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });
    }
}
