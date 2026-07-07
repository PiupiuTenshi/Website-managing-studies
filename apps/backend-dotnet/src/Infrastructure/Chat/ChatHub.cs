using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteAssignment.Application.Chat;

namespace RemoteAssignment.Infrastructure.Chat;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        // When connected, we can automatically add them to rooms they are part of
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var rooms = await _chatService.GetUserRoomsAsync(userId, Context.ConnectionAborted);
            foreach (var room in rooms)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());
            }
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _chatService.JoinRoomAsync(roomId, userId, Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        }
    }

    public async Task SendMessage(Guid roomId, string message)
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var savedMessage = await _chatService.SaveMessageAsync(roomId, userId, message, Context.ConnectionAborted);
            
            // Broadcast to everyone in the room
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", savedMessage);
        }
    }
}
