using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Common;
using RemoteAssignment.Application.Organization;

namespace RemoteAssignment.WebApi;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // --- Grade Levels ---
        var grades = api.MapGroup("/grade-levels").RequireAuthorization("ManagerOrAdmin");
        grades.MapGet("/", async (IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.ListGradeLevelsAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<GradeLevelDto>>.Ok(result, httpContext.TraceIdentifier));
        });
        grades.MapPost("/", async (CreateGradeLevelRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.CreateGradeLevelAsync(request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<GradeLevelDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest(ApiResponse<object>.Fail("CREATE_FAILED", "Could not create grade level", "Check input data", httpContext.TraceIdentifier));
        }).RequireAuthorization("AdminOnly");

        // --- Subjects ---
        var subjects = api.MapGroup("/subjects").RequireAuthorization("ManagerOrAdmin");
        subjects.MapGet("/", async (IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.ListSubjectsAsync(ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SubjectDto>>.Ok(result, httpContext.TraceIdentifier));
        });
        subjects.MapPost("/", async (CreateSubjectRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.CreateSubjectAsync(request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<SubjectDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest(ApiResponse<object>.Fail("CREATE_FAILED", "Could not create subject", "Check input data", httpContext.TraceIdentifier));
        }).RequireAuthorization("AdminOnly");

        // --- Class Rooms ---
        var classes = api.MapGroup("/classes").RequireAuthorization("ManagerOrAdmin");
        classes.MapGet("/", async (Guid? gradeLevelId, Guid? managerId, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            // Note: In a real app, a Manager might only be allowed to see their own classes.
            // We would enforce managerId = currentUserId here if the user is a Manager (not Admin).
            // For now, we trust the caller/policy.
            var result = await service.ListClassRoomsAsync(gradeLevelId, managerId, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ClassRoomDto>>.Ok(result, httpContext.TraceIdentifier));
        });
        classes.MapGet("/{id:guid}", async (Guid id, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.GetClassRoomAsync(id, ct);
            return result is not null
                ? Results.Ok(ApiResponse<ClassRoomDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.NotFound();
        });
        classes.MapPost("/", async (CreateClassRoomRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.CreateClassRoomAsync(request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<ClassRoomDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });
        classes.MapPut("/{id:guid}", async (Guid id, UpdateClassRoomRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.UpdateClassRoomAsync(id, request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<ClassRoomDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.NotFound();
        });
        classes.MapDelete("/{id:guid}", async (Guid id, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.DeleteClassRoomAsync(id, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });

        // Class Enrollments
        classes.MapGet("/{id:guid}/students", async (Guid id, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.ListEnrollmentsAsync(id, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ClassEnrollmentDto>>.Ok(result, httpContext.TraceIdentifier));
        });
        classes.MapPost("/{id:guid}/students", async (Guid id, EnrollStudentRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.EnrollStudentAsync(id, request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<ClassEnrollmentDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });
        classes.MapDelete("/{id:guid}/students/{studentId:guid}", async (Guid id, Guid studentId, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.RemoveEnrollmentAsync(id, studentId, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });

        // --- Parents & Links ---
        // A parent or an admin/manager can view students linked to a parent
        api.MapGet("/parents/{parentId:guid}/students", async (Guid parentId, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.ListLinksByParentAsync(parentId, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ParentStudentLinkDto>>.Ok(result, httpContext.TraceIdentifier));
        }).RequireAuthorization("ParentOrAdmin");

        var parentLinks = api.MapGroup("/parent-student-links").RequireAuthorization("ManagerOrAdmin");
        parentLinks.MapPost("/", async (LinkParentRequest request, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.LinkParentAsync(request, ct);
            return result is not null
                ? Results.Ok(ApiResponse<ParentStudentLinkDto>.Ok(result, httpContext.TraceIdentifier))
                : Results.BadRequest();
        });
        parentLinks.MapDelete("/{id:guid}", async (Guid id, IOrganizationService service, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await service.UnlinkParentAsync(id, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });
    }
}
