using System.Data;
using System.Text.Json;
using Npgsql;
using RemoteAssignment.Application.Submission;
using RemoteAssignment.Application.Email;

namespace RemoteAssignment.Infrastructure.Submission;

internal sealed class PostgresSubmissionService(DatabaseOptions databaseOptions, IEmailService emailService) : ISubmissionService
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

    public async Task<IReadOnlyList<StudentAssignmentDto>> GetStudentAssignmentsAsync(Guid studentId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        var sql = """
            select a.id, a.title, a.description, a.deadline_at, a.allow_late_submission, a.status, 
                   u.full_name, a.created_at,
                   s.id as sub_id, s.status as sub_status, s.submitted_at, s.grade_score
            from assignments a
            inner join users u on a.created_by = u.id
            inner join assignment_targets at on a.id = at.assignment_id
            left join class_enrollments ce on at.target_type = 'ClassRoom' and at.target_id = ce.class_room_id and ce.deleted_at is null and ce.status = 'Active'
            left join submissions s on a.id = s.assignment_id and s.student_id = @studentId
            where a.deleted_at is null 
              and a.status = 'Published'
              and (
                   (at.target_type = 'Student' and at.target_id = @studentId)
                   or
                   (at.target_type = 'ClassRoom' and ce.student_id = @studentId)
              )
            group by a.id, a.title, a.description, a.deadline_at, a.allow_late_submission, a.status, 
                     u.full_name, a.created_at,
                     s.id, s.status, s.submitted_at, s.grade_score
            order by a.created_at desc
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("studentId", studentId);

        var result = new List<StudentAssignmentDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new StudentAssignmentDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetBoolean(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                reader.IsDBNull(11) ? null : reader.GetDecimal(11)));
        }

        return result;
    }

    public async Task<SubmissionDto?> GetSubmissionByAssignmentAsync(Guid assignmentId, Guid studentId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select id, assignment_id, student_id, content_json, status, submitted_at, graded_at, grade_score, feedback_json, created_at, updated_at
            from submissions
            where assignment_id = @assignmentId and student_id = @studentId
            """,
            connection);

        command.Parameters.AddWithValue("assignmentId", assignmentId);
        command.Parameters.AddWithValue("studentId", studentId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return ReadSubmission(reader);
        }

        return null;
    }

    public async Task<SubmissionDto?> DraftSubmissionAsync(Guid assignmentId, Guid studentId, DraftSubmissionRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        await using var command = new NpgsqlCommand(
            """
            insert into submissions (assignment_id, student_id, content_json, status)
            values (@assignmentId, @studentId, cast(@contentJson as jsonb), 'Draft')
            on conflict (assignment_id, student_id) do update 
            set content_json = cast(@contentJson as jsonb), updated_at = now()
            where submissions.status = 'Draft'
            returning id
            """,
            connection);

        command.Parameters.AddWithValue("assignmentId", assignmentId);
        command.Parameters.AddWithValue("studentId", studentId);
        command.Parameters.AddWithValue("contentJson", request.ContentJson is null ? DBNull.Value : JsonSerializer.Serialize(request.ContentJson));

        var idObj = await command.ExecuteScalarAsync(ct);
        if (idObj is Guid id)
        {
            return await GetSubmissionByAssignmentAsync(assignmentId, studentId, ct);
        }
        
        return null; // Could happen if status is not Draft anymore (e.g. already submitted)
    }

    public async Task<SubmissionDto?> SubmitAsync(Guid assignmentId, Guid studentId, SubmitAssignmentRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // 1. Get assignment deadline info to calculate status
            await using var assignCommand = new NpgsqlCommand(
                "select deadline_at, allow_late_submission from assignments where id = @assignmentId and status = 'Published' and deleted_at is null",
                connection, transaction);
            assignCommand.Parameters.AddWithValue("assignmentId", assignmentId);
            
            DateTimeOffset? deadlineAt = null;
            bool allowLate = false;
            
            await using (var assignReader = await assignCommand.ExecuteReaderAsync(ct))
            {
                if (!await assignReader.ReadAsync(ct))
                {
                    throw new Exception("Assignment not found or not published.");
                }
                deadlineAt = assignReader.IsDBNull(0) ? null : assignReader.GetFieldValue<DateTimeOffset>(0);
                allowLate = assignReader.GetBoolean(1);
            }

            var now = DateTimeOffset.UtcNow;
            var isLate = deadlineAt.HasValue && now > deadlineAt.Value;
            
            if (isLate && !allowLate)
            {
                throw new Exception("Assignment is past deadline and late submissions are not allowed.");
            }

            var status = isLate ? "Late" : "Submitted";

            // 2. Upsert submission
            await using var command = new NpgsqlCommand(
                """
                insert into submissions (assignment_id, student_id, content_json, status, submitted_at)
                values (@assignmentId, @studentId, cast(@contentJson as jsonb), @status, now())
                on conflict (assignment_id, student_id) do update 
                set content_json = cast(@contentJson as jsonb), status = @status, submitted_at = now(), updated_at = now()
                returning id
                """,
                connection, transaction);

            command.Parameters.AddWithValue("assignmentId", assignmentId);
            command.Parameters.AddWithValue("studentId", studentId);
            command.Parameters.AddWithValue("contentJson", request.ContentJson is null ? DBNull.Value : JsonSerializer.Serialize(request.ContentJson));
            command.Parameters.AddWithValue("status", status);

            await command.ExecuteScalarAsync(ct);
            await transaction.CommitAsync(ct);

            var submissionDto = await GetSubmissionByAssignmentAsync(assignmentId, studentId, ct);

            // Fetch teacher email to notify
            await using var teacherCommand = new NpgsqlCommand(
                "select u.email, u.full_name, a.title from assignments a inner join users u on a.created_by = u.id where a.id = @assignmentId",
                connection, transaction);
            teacherCommand.Parameters.AddWithValue("assignmentId", assignmentId);
            await using (var teacherReader = await teacherCommand.ExecuteReaderAsync(ct))
            {
                if (await teacherReader.ReadAsync(ct))
                {
                    var email = teacherReader.GetString(0);
                    var name = teacherReader.GetString(1);
                    var title = teacherReader.GetString(2);
                    
                    var message = new EmailMessage(
                        email, 
                        name, 
                        $"New Submission: {title}", 
                        $"<p>Hello {name},</p><p>A student has submitted their assignment for <b>{title}</b>.</p><p>Status: {status}</p>");
                    
                    // Fire and forget so we don't block
                    _ = Task.Run(() => emailService.SendEmailAsync(message, CancellationToken.None));
                }
            }

            return submissionDto;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<ManagerSubmissionDto>> GetSubmissionsForAssignmentAsync(Guid assignmentId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        // This query finds all students who were assigned this assignment,
        // and left joins their submissions.
        var sql = """
            with assigned_students as (
                select ce.student_id as student_id
                from assignment_targets at
                inner join class_enrollments ce on at.target_id = ce.class_room_id
                where at.assignment_id = @assignmentId and at.target_type = 'ClassRoom' and ce.deleted_at is null and ce.status = 'Active'
                union
                select at.target_id as student_id
                from assignment_targets at
                where at.assignment_id = @assignmentId and at.target_type = 'Student'
            )
            select s.id, @assignmentId as assignment_id, ast.student_id, u.full_name,
                   coalesce(s.status, 'Not Submitted') as status, s.submitted_at, s.graded_at, s.grade_score, s.created_at
            from assigned_students ast
            inner join users u on ast.student_id = u.id
            left join submissions s on ast.student_id = s.student_id and s.assignment_id = @assignmentId
            order by u.full_name
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("assignmentId", assignmentId);

        var result = new List<ManagerSubmissionDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ManagerSubmissionDto(
                reader.IsDBNull(0) ? Guid.Empty : reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                reader.IsDBNull(8) ? DateTimeOffset.MinValue : reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return result;
    }

    public async Task<SubmissionDto?> GetSubmissionDetailForManagerAsync(Guid submissionId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            select id, assignment_id, student_id, content_json, status, submitted_at, graded_at, grade_score, feedback_json, created_at, updated_at
            from submissions
            where id = @submissionId
            """,
            connection);

        command.Parameters.AddWithValue("submissionId", submissionId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return ReadSubmission(reader);
        }

        return null;
    }

    public async Task<SubmissionDto?> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            update submissions
            set grade_score = @gradeScore, feedback_json = cast(@feedbackJson as jsonb), status = 'Graded', graded_at = now(), updated_at = now()
            where id = @submissionId
            """,
            connection);

        command.Parameters.AddWithValue("submissionId", submissionId);
        command.Parameters.AddWithValue("gradeScore", request.GradeScore);
        command.Parameters.AddWithValue("feedbackJson", request.FeedbackJson is null ? DBNull.Value : JsonSerializer.Serialize(request.FeedbackJson));

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        if (rowsAffected > 0)
        {
             var updatedSubmission = await GetSubmissionDetailForManagerAsync(submissionId, ct);
             
             // Fetch student email to notify
             await using var studentCommand = new NpgsqlCommand(
                 "select u.email, u.full_name, a.title from submissions s inner join users u on s.student_id = u.id inner join assignments a on s.assignment_id = a.id where s.id = @submissionId",
                 connection);
             studentCommand.Parameters.AddWithValue("submissionId", submissionId);
             
             await using (var studentReader = await studentCommand.ExecuteReaderAsync(ct))
             {
                 if (await studentReader.ReadAsync(ct))
                 {
                     var email = studentReader.GetString(0);
                     var name = studentReader.GetString(1);
                     var title = studentReader.GetString(2);
                     
                     var message = new EmailMessage(
                         email, 
                         name, 
                         $"Assignment Graded: {title}", 
                         $"<p>Hello {name},</p><p>Your assignment <b>{title}</b> has been graded.</p><p>Score: {request.GradeScore}/100</p>");
                     
                     _ = Task.Run(() => emailService.SendEmailAsync(message, CancellationToken.None));
                 }
             }

             return updatedSubmission;
        }
        return null;
    }

    private static SubmissionDto ReadSubmission(NpgsqlDataReader reader)
    {
        return new SubmissionDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.IsDBNull(3) ? null : JsonDocument.Parse(reader.GetString(3)),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : JsonDocument.Parse(reader.GetString(8)),
            reader.GetFieldValue<DateTimeOffset>(9),
            reader.GetFieldValue<DateTimeOffset>(10));
    }
}
