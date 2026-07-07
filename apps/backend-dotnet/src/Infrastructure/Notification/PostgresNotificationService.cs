using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using RemoteAssignment.Application.Notification;

namespace RemoteAssignment.Infrastructure.Notification;

internal sealed class PostgresNotificationService(DatabaseOptions databaseOptions) : INotificationService
{
    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        var connection = new NpgsqlConnection(databaseOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "select id, title, message, is_read, created_at from notifications where user_id = @userId order by created_at desc limit 50", 
            connection);
            
        command.Parameters.AddWithValue("userId", userId);
        
        var list = new List<NotificationDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new NotificationDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetFieldValue<DateTimeOffset>(4)
            ));
        }
        return list;
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "update notifications set is_read = true where id = @id and user_id = @userId", 
            connection);
            
        command.Parameters.AddWithValue("id", notificationId);
        command.Parameters.AddWithValue("userId", userId);
        
        var rows = await command.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "select count(*) from notifications where user_id = @userId and is_read = false", 
            connection);
            
        command.Parameters.AddWithValue("userId", userId);
        
        var count = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(count);
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "insert into notifications (user_id, title, message) values (@userId, @title, @message)", 
            connection);
            
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("message", message);
        
        await command.ExecuteNonQueryAsync(ct);
    }
}
