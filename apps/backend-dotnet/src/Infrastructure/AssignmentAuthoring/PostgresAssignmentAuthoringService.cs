using System.Data;
using System.Text.Json;
using Npgsql;
using RemoteAssignment.Application.AssignmentAuthoring;

namespace RemoteAssignment.Infrastructure.AssignmentAuthoring;

internal sealed class PostgresAssignmentAuthoringService(DatabaseOptions databaseOptions) : IAssignmentAuthoringService
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

    public async Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(Guid? subjectId, Guid? createdBy, string? status, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        var sql = """
            select a.id, a.subject_id, a.title, a.description, a.content_json, a.deadline_at, a.allow_late_submission, a.max_attempts, a.status, a.created_by, u.full_name, a.created_at, a.updated_at
            from assignments a
            left join users u on a.created_by = u.id
            where a.deleted_at is null
            """;

        if (subjectId.HasValue) sql += " and a.subject_id = @subjectId";
        if (createdBy.HasValue) sql += " and a.created_by = @createdBy";
        if (!string.IsNullOrWhiteSpace(status)) sql += " and a.status = @status";
        
        sql += " order by a.created_at desc";

        await using var command = new NpgsqlCommand(sql, connection);
        
        if (subjectId.HasValue) command.Parameters.AddWithValue("subjectId", subjectId.Value);
        if (createdBy.HasValue) command.Parameters.AddWithValue("createdBy", createdBy.Value);
        if (!string.IsNullOrWhiteSpace(status)) command.Parameters.AddWithValue("status", status);

        var result = new List<AssignmentDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(ReadAssignment(reader));
        }

        return result;
    }

    public async Task<AssignmentDto?> GetAssignmentAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select a.id, a.subject_id, a.title, a.description, a.content_json, a.deadline_at, a.allow_late_submission, a.max_attempts, a.status, a.created_by, u.full_name, a.created_at, a.updated_at
            from assignments a
            left join users u on a.created_by = u.id
            where a.id = @id and a.deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return ReadAssignment(reader);
        }

        return null;
    }

    public async Task<AssignmentDto?> CreateAssignmentAsync(CreateAssignmentRequest request, Guid createdBy, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            insert into assignments (subject_id, title, description, content_json, deadline_at, allow_late_submission, max_attempts, status, created_by)
            values (@subjectId, @title, @description, cast(@contentJson as jsonb), @deadlineAt, @allowLateSubmission, @maxAttempts, 'Draft', @createdBy)
            returning id
            """,
            connection);

        command.Parameters.AddWithValue("subjectId", request.SubjectId);
        command.Parameters.AddWithValue("title", request.Title);
        command.Parameters.AddWithValue("description", string.IsNullOrWhiteSpace(request.Description) ? DBNull.Value : request.Description);
        command.Parameters.AddWithValue("contentJson", request.ContentJson is null ? DBNull.Value : JsonSerializer.Serialize(request.ContentJson));
        command.Parameters.AddWithValue("deadlineAt", request.DeadlineAt.HasValue ? (object)request.DeadlineAt.Value : DBNull.Value);
        command.Parameters.AddWithValue("allowLateSubmission", request.AllowLateSubmission);
        command.Parameters.AddWithValue("maxAttempts", request.MaxAttempts.HasValue ? (object)request.MaxAttempts.Value : DBNull.Value);
        command.Parameters.AddWithValue("createdBy", createdBy);

        var idObj = await command.ExecuteScalarAsync(ct);
        if (idObj is Guid id)
        {
            return await GetAssignmentAsync(id, ct);
        }
        
        return null;
    }

    public async Task<AssignmentDto?> UpdateAssignmentAsync(Guid id, UpdateAssignmentRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update assignments
            set subject_id = @subjectId, title = @title, description = @description, content_json = cast(@contentJson as jsonb), 
                deadline_at = @deadlineAt, allow_late_submission = @allowLateSubmission, max_attempts = @maxAttempts, updated_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("subjectId", request.SubjectId);
        command.Parameters.AddWithValue("title", request.Title);
        command.Parameters.AddWithValue("description", string.IsNullOrWhiteSpace(request.Description) ? DBNull.Value : request.Description);
        command.Parameters.AddWithValue("contentJson", request.ContentJson is null ? DBNull.Value : JsonSerializer.Serialize(request.ContentJson));
        command.Parameters.AddWithValue("deadlineAt", request.DeadlineAt.HasValue ? (object)request.DeadlineAt.Value : DBNull.Value);
        command.Parameters.AddWithValue("allowLateSubmission", request.AllowLateSubmission);
        command.Parameters.AddWithValue("maxAttempts", request.MaxAttempts.HasValue ? (object)request.MaxAttempts.Value : DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        if (rowsAffected > 0)
        {
             return await GetAssignmentAsync(id, ct);
        }
        
        return null;
    }

    public async Task<bool> DeleteAssignmentAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update assignments
            set deleted_at = now(), updated_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        
        return rowsAffected > 0;
    }

    public async Task<AssignmentDto?> PublishAssignmentAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            update assignments
            set status = 'Published', updated_at = now()
            where id = @id and deleted_at is null and status = 'Draft'
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        if (rowsAffected > 0)
        {
             return await GetAssignmentAsync(id, ct);
        }
        return null;
    }
    
    public async Task<AssignmentDto?> ArchiveAssignmentAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            update assignments
            set status = 'Archived', updated_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        if (rowsAffected > 0)
        {
             return await GetAssignmentAsync(id, ct);
        }
        return null;
    }

    public async Task<IReadOnlyList<AssignmentTargetDto>> GetAssignmentTargetsAsync(Guid assignmentId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        // This query combines fetching target details from either class_rooms or users based on target_type
        await using var command = new NpgsqlCommand(
            """
            select at.id, at.assignment_id, at.target_type, at.target_id, 
                   case 
                        when at.target_type = 'ClassRoom' then c.name
                        when at.target_type = 'Student' then u.full_name
                        else 'Unknown'
                   end as target_name
            from assignment_targets at
            left join class_rooms c on at.target_id = c.id and at.target_type = 'ClassRoom'
            left join users u on at.target_id = u.id and at.target_type = 'Student'
            where at.assignment_id = @assignmentId
            """,
            connection);

        command.Parameters.AddWithValue("assignmentId", assignmentId);

        var result = new List<AssignmentTargetDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new AssignmentTargetDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetGuid(3),
                reader.IsDBNull(4) ? "Unknown" : reader.GetString(4)));
        }

        return result;
    }

    public async Task<bool> SetAssignmentTargetsAsync(Guid assignmentId, AssignTargetRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // 1. Delete existing targets
            await using var deleteCommand = new NpgsqlCommand("delete from assignment_targets where assignment_id = @assignmentId", connection, transaction);
            deleteCommand.Parameters.AddWithValue("assignmentId", assignmentId);
            await deleteCommand.ExecuteNonQueryAsync(ct);

            // 2. Insert new targets
            if (request.Targets.Count > 0)
            {
                // Simple batch insertion loop
                foreach (var target in request.Targets)
                {
                    await using var insertCommand = new NpgsqlCommand(
                        """
                        insert into assignment_targets (assignment_id, target_type, target_id)
                        values (@assignmentId, @targetType, @targetId)
                        """,
                        connection, transaction);
                    insertCommand.Parameters.AddWithValue("assignmentId", assignmentId);
                    insertCommand.Parameters.AddWithValue("targetType", target.TargetType);
                    insertCommand.Parameters.AddWithValue("targetId", target.TargetId);
                    await insertCommand.ExecuteNonQueryAsync(ct);
                }
            }

            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static AssignmentDto ReadAssignment(NpgsqlDataReader reader)
    {
        return new AssignmentDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : JsonDocument.Parse(reader.GetString(4)),
            reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
            reader.GetBoolean(6),
            reader.IsDBNull(7) ? null : reader.GetInt32(7),
            reader.GetString(8),
            reader.GetGuid(9),
            reader.IsDBNull(10) ? "Unknown" : reader.GetString(10),
            reader.GetFieldValue<DateTimeOffset>(11),
            reader.GetFieldValue<DateTimeOffset>(12));
    }
}
