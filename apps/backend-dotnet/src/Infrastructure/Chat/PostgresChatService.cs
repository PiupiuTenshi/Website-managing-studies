using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using RemoteAssignment.Application.Chat;

namespace RemoteAssignment.Infrastructure.Chat;

internal sealed class PostgresChatService(DatabaseOptions databaseOptions) : IChatService
{
    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var connection = new NpgsqlConnection(databaseOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<ChatRoomDto?> GetRoomByReferenceAsync(Guid referenceId, string type, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT id, name, type, reference_id FROM chat_rooms WHERE reference_id = @ref AND type = @type LIMIT 1", connection);
        command.Parameters.AddWithValue("ref", referenceId);
        command.Parameters.AddWithValue("type", type);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new ChatRoomDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3)
            );
        }
        return null;
    }

    public async Task<ChatRoomDto> CreateRoomAsync(string name, string type, Guid? referenceId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand(
            "INSERT INTO chat_rooms (id, name, type, reference_id) VALUES (@id, @name, @type, @ref)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("ref", referenceId.HasValue ? (object)referenceId.Value : DBNull.Value);
        
        await command.ExecuteNonQueryAsync(ct);
        
        return new ChatRoomDto(id, name, type, referenceId);
    }

    public async Task<IReadOnlyList<ChatRoomDto>> GetUserRoomsAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            @"SELECT cr.id, cr.name, cr.type, cr.reference_id 
              FROM chat_rooms cr
              INNER JOIN chat_participants cp ON cr.id = cp.chat_room_id
              WHERE cp.user_id = @userId", connection);
        command.Parameters.AddWithValue("userId", userId);

        var list = new List<ChatRoomDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ChatRoomDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3)
            ));
        }
        return list;
    }

    public async Task JoinRoomAsync(Guid chatRoomId, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            @"INSERT INTO chat_participants (chat_room_id, user_id) 
              VALUES (@roomId, @userId)
              ON CONFLICT (chat_room_id, user_id) 
              DO UPDATE SET last_read_at = NOW()", connection);
        command.Parameters.AddWithValue("roomId", chatRoomId);
        command.Parameters.AddWithValue("userId", userId);
        
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<ChatMessageDto> SaveMessageAsync(Guid chatRoomId, Guid senderId, string content, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        var id = Guid.NewGuid();
        
        // Ensure sender is in the room
        await JoinRoomAsync(chatRoomId, senderId, ct);
        
        await using var command = new NpgsqlCommand(
            "INSERT INTO chat_messages (id, chat_room_id, sender_id, content) VALUES (@id, @roomId, @senderId, @content)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("roomId", chatRoomId);
        command.Parameters.AddWithValue("senderId", senderId);
        command.Parameters.AddWithValue("content", content);
        await command.ExecuteNonQueryAsync(ct);
        
        // Fetch sender name
        await using var userCmd = new NpgsqlCommand("SELECT full_name FROM users WHERE id = @senderId", connection);
        userCmd.Parameters.AddWithValue("senderId", senderId);
        var senderName = (string)(await userCmd.ExecuteScalarAsync(ct) ?? "Unknown");
        
        return new ChatMessageDto(id, chatRoomId, senderId, senderName, content, DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetRoomMessagesAsync(Guid chatRoomId, int limit = 50, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            @"SELECT m.id, m.chat_room_id, m.sender_id, u.full_name, m.content, m.created_at
              FROM chat_messages m
              INNER JOIN users u ON m.sender_id = u.id
              WHERE m.chat_room_id = @roomId
              ORDER BY m.created_at DESC
              LIMIT @limit", connection);
        command.Parameters.AddWithValue("roomId", chatRoomId);
        command.Parameters.AddWithValue("limit", limit);

        var list = new List<ChatMessageDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ChatMessageDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5)
            ));
        }
        
        // Reverse so they are in chronological order
        list.Reverse();
        return list;
    }
}
