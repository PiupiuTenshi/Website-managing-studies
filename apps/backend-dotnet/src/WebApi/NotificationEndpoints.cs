using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Notification;
using System.Security.Claims;

namespace RemoteAssignment.WebApi;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").RequireAuthorization();

        group.MapGet("/", async (INotificationService notificationService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var notifications = await notificationService.GetUserNotificationsAsync(userId, ct);
            return Results.Ok(notifications);
        });

        group.MapGet("/unread-count", async (INotificationService notificationService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var count = await notificationService.GetUnreadCountAsync(userId, ct);
            return Results.Ok(new { Count = count });
        });

        group.MapPost("/{id:guid}/read", async (Guid id, INotificationService notificationService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var success = await notificationService.MarkAsReadAsync(userId, id, ct);
            if (!success) return Results.NotFound();

            return Results.NoContent();
        });
    }
}
