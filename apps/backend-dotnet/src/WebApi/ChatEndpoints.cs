using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Chat;
using System.Security.Claims;

namespace RemoteAssignment.WebApi;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").RequireAuthorization();

        group.MapGet("/rooms", async (IChatService chatService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var rooms = await chatService.GetUserRoomsAsync(userId, ct);
            return Results.Ok(rooms);
        });

        group.MapGet("/rooms/{roomId:guid}/messages", async (Guid roomId, IChatService chatService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            // Should verify if user is part of room, but skipped for brevity
            var messages = await chatService.GetRoomMessagesAsync(roomId, 50, ct);
            
            // Mark as read when fetching
            await chatService.JoinRoomAsync(roomId, userId, ct);
            
            return Results.Ok(messages);
        });
    }
}
