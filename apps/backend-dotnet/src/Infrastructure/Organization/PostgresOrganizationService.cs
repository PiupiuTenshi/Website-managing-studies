using System.Data;
using Npgsql;
using RemoteAssignment.Application.Organization;

namespace RemoteAssignment.Infrastructure.Organization;

internal sealed class PostgresOrganizationService(DatabaseOptions databaseOptions) : IOrganizationService
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

    public async Task<IReadOnlyList<GradeLevelDto>> ListGradeLevelsAsync(CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select id, name, sort_order
            from grade_levels
            order by sort_order
            """,
            connection);

        var result = new List<GradeLevelDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new GradeLevelDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2)));
        }

        return result;
    }

    public async Task<GradeLevelDto?> CreateGradeLevelAsync(CreateGradeLevelRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            insert into grade_levels (name, sort_order)
            values (@name, @sortOrder)
            returning id, name, sort_order
            """,
            connection);

        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("sortOrder", request.SortOrder);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new GradeLevelDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2));
        }

        return null;
    }

    public async Task<IReadOnlyList<SubjectDto>> ListSubjectsAsync(CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select id, name, code
            from subjects
            order by name
            """,
            connection);

        var result = new List<SubjectDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new SubjectDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return result;
    }

    public async Task<SubjectDto?> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            insert into subjects (name, code)
            values (@name, @code)
            returning id, name, code
            """,
            connection);

        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("code", request.Code);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new SubjectDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2));
        }

        return null;
    }

    public async Task<IReadOnlyList<ClassRoomDto>> ListClassRoomsAsync(Guid? gradeLevelId, Guid? managerId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        var sql = """
            select c.id, c.name, c.grade_level_id, g.name, c.manager_id, u.full_name
            from class_rooms c
            inner join grade_levels g on c.grade_level_id = g.id
            left join users u on c.manager_id = u.id
            where c.deleted_at is null
            """;

        if (gradeLevelId.HasValue)
        {
            sql += " and c.grade_level_id = @gradeLevelId";
        }

        if (managerId.HasValue)
        {
            sql += " and c.manager_id = @managerId";
        }

        sql += " order by g.sort_order, c.name";

        await using var command = new NpgsqlCommand(sql, connection);
        
        if (gradeLevelId.HasValue) command.Parameters.AddWithValue("gradeLevelId", gradeLevelId.Value);
        if (managerId.HasValue) command.Parameters.AddWithValue("managerId", managerId.Value);

        var result = new List<ClassRoomDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ClassRoomDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return result;
    }

    public async Task<ClassRoomDto?> GetClassRoomAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select c.id, c.name, c.grade_level_id, g.name, c.manager_id, u.full_name
            from class_rooms c
            inner join grade_levels g on c.grade_level_id = g.id
            left join users u on c.manager_id = u.id
            where c.id = @id and c.deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new ClassRoomDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetString(5));
        }

        return null;
    }

    public async Task<ClassRoomDto?> CreateClassRoomAsync(CreateClassRoomRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            insert into class_rooms (name, grade_level_id, manager_id)
            values (@name, @gradeLevelId, @managerId)
            returning id
            """,
            connection);

        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("gradeLevelId", request.GradeLevelId);
        command.Parameters.AddWithValue("managerId", request.ManagerId.HasValue ? (object)request.ManagerId.Value : DBNull.Value);

        var idObj = await command.ExecuteScalarAsync(ct);
        if (idObj is Guid id)
        {
            return await GetClassRoomAsync(id, ct);
        }
        
        return null;
    }

    public async Task<ClassRoomDto?> UpdateClassRoomAsync(Guid id, UpdateClassRoomRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update class_rooms
            set name = @name, grade_level_id = @gradeLevelId, manager_id = @managerId, updated_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("gradeLevelId", request.GradeLevelId);
        command.Parameters.AddWithValue("managerId", request.ManagerId.HasValue ? (object)request.ManagerId.Value : DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        if (rowsAffected > 0)
        {
             return await GetClassRoomAsync(id, ct);
        }
        
        return null;
    }

    public async Task<bool> DeleteClassRoomAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update class_rooms
            set deleted_at = now(), updated_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        
        return rowsAffected > 0;
    }

    public async Task<IReadOnlyList<ClassEnrollmentDto>> ListEnrollmentsAsync(Guid classRoomId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select ce.id, ce.class_room_id, ce.student_id, u.full_name, u.email, ce.status
            from class_enrollments ce
            inner join users u on ce.student_id = u.id
            where ce.class_room_id = @classRoomId and ce.deleted_at is null
            order by u.full_name
            """,
            connection);

        command.Parameters.AddWithValue("classRoomId", classRoomId);

        var result = new List<ClassEnrollmentDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ClassEnrollmentDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return result;
    }

    public async Task<ClassEnrollmentDto?> EnrollStudentAsync(Guid classRoomId, EnrollStudentRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            insert into class_enrollments (class_room_id, student_id, status)
            values (@classRoomId, @studentId, 'Active')
            on conflict (class_room_id, student_id) do update 
            set deleted_at = null, status = 'Active'
            returning id, class_room_id, student_id, status
            """,
            connection);

        command.Parameters.AddWithValue("classRoomId", classRoomId);
        command.Parameters.AddWithValue("studentId", request.StudentId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var enrollmentId = reader.GetGuid(0);
            // We could just return the data here, but joining with User to get full name is cleaner
            // We'll close this reader and run a targeted query, or just fetch the user name if needed.
            // Let's do a simple sub-query by reloading it from ListEnrollments (lazy, but works for now as it's a small dataset, or we can just query the specific one).
        }
        
        // Let's get the full DTO
        if (connection.State == ConnectionState.Open)
        {
           await connection.CloseAsync(); // Close and reopen or just dispose the reader. The reader is disposed in the using block but to execute another command on the same connection we need to ensure the reader is fully consumed/closed.
        }
        
        // Let's just use the ListEnrollments to find the newly enrolled.
        var list = await ListEnrollmentsAsync(classRoomId, ct);
        return list.FirstOrDefault(e => e.StudentId == request.StudentId);
    }

    public async Task<bool> RemoveEnrollmentAsync(Guid classRoomId, Guid studentId, CancellationToken ct)
    {
         await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update class_enrollments
            set deleted_at = now()
            where class_room_id = @classRoomId and student_id = @studentId and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("classRoomId", classRoomId);
        command.Parameters.AddWithValue("studentId", studentId);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        
        return rowsAffected > 0;
    }

    public async Task<IReadOnlyList<ParentStudentLinkDto>> ListLinksByParentAsync(Guid parentId, CancellationToken ct)
    {
         await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select psl.id, psl.parent_id, p.full_name, psl.student_id, s.full_name, psl.relationship, psl.status
            from parent_student_links psl
            inner join users p on psl.parent_id = p.id
            inner join users s on psl.student_id = s.id
            where psl.parent_id = @parentId and psl.deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("parentId", parentId);

        var result = new List<ParentStudentLinkDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ParentStudentLinkDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6)));
        }

        return result;
    }

    public async Task<IReadOnlyList<ParentStudentLinkDto>> ListLinksByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select psl.id, psl.parent_id, p.full_name, psl.student_id, s.full_name, psl.relationship, psl.status
            from parent_student_links psl
            inner join users p on psl.parent_id = p.id
            inner join users s on psl.student_id = s.id
            where psl.student_id = @studentId and psl.deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("studentId", studentId);

        var result = new List<ParentStudentLinkDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ParentStudentLinkDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6)));
        }

        return result;
    }

    public async Task<ParentStudentLinkDto?> LinkParentAsync(LinkParentRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            insert into parent_student_links (parent_id, student_id, relationship, status)
            values (@parentId, @studentId, @relationship, 'Active')
            on conflict (parent_id, student_id) do update 
            set deleted_at = null, status = 'Active', relationship = @relationship
            returning id
            """,
            connection);

        command.Parameters.AddWithValue("parentId", request.ParentId);
        command.Parameters.AddWithValue("studentId", request.StudentId);
        command.Parameters.AddWithValue("relationship", request.Relationship ?? (object)DBNull.Value);

        var idObj = await command.ExecuteScalarAsync(ct);
        
        var list = await ListLinksByStudentAsync(request.StudentId, ct);
        return list.FirstOrDefault(l => l.ParentId == request.ParentId);
    }

    public async Task<bool> UnlinkParentAsync(Guid linkId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            update parent_student_links
            set deleted_at = now()
            where id = @id and deleted_at is null
            """,
            connection);

        command.Parameters.AddWithValue("id", linkId);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        
        return rowsAffected > 0;
    }
}
