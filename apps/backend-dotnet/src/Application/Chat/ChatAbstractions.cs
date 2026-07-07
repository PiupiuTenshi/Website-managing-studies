using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteAssignment.Application.Chat;

public sealed record ChatRoomDto(
    Guid Id,
    string Name,
    string Type,
    Guid? ReferenceId
);

public sealed record ChatMessageDto(
    Guid Id,
    Guid ChatRoomId,
    Guid SenderId,
    string SenderName,
    string Content,
    DateTimeOffset CreatedAt
);

public interface IChatService
{
    // Retrieve a room by reference (e.g. ClassRoomId)
    Task<ChatRoomDto?> GetRoomByReferenceAsync(Guid referenceId, string type, CancellationToken ct = default);
    
    // Create a new room
    Task<ChatRoomDto> CreateRoomAsync(string name, string type, Guid? referenceId, CancellationToken ct = default);

    // Get list of rooms a user is part of
    Task<IReadOnlyList<ChatRoomDto>> GetUserRoomsAsync(Guid userId, CancellationToken ct = default);

    // Join a room (or update last_read_at)
    Task JoinRoomAsync(Guid chatRoomId, Guid userId, CancellationToken ct = default);

    // Save a message to DB
    Task<ChatMessageDto> SaveMessageAsync(Guid chatRoomId, Guid senderId, string content, CancellationToken ct = default);

    // Get message history
    Task<IReadOnlyList<ChatMessageDto>> GetRoomMessagesAsync(Guid chatRoomId, int limit = 50, CancellationToken ct = default);
}
