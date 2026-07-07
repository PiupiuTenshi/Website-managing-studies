using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using RemoteAssignment.Application.Email;
using RemoteAssignment.Application.Notification;

namespace RemoteAssignment.Infrastructure.BackgroundJobs;

public class DeadlineReminderJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadlineReminderJob> _logger;

    public DeadlineReminderJob(IServiceProvider serviceProvider, ILogger<DeadlineReminderJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeadlineReminderJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing DeadlineReminderJob.");
            }

            // Run every 10 minutes (configurable in real app)
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    private async Task RunRemindersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<DatabaseOptions>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
            return;

        await using var connection = new NpgsqlConnection(databaseOptions.ConnectionString);
        await connection.OpenAsync(ct);

        // We find assignments that are ending in < 24h OR have ended in the last 24h.
        // We look for students assigned to these who have NO submission.
        // We check reminder_logs to avoid re-sending.

        var sql = """
            with assigned_students as (
                select a.id as assignment_id, a.title, a.deadline_at, ce.student_id as student_id
                from assignments a
                inner join assignment_targets at on a.id = at.assignment_id
                inner join class_enrollments ce on at.target_id = ce.class_room_id
                where at.target_type = 'ClassRoom' and ce.deleted_at is null and ce.status = 'Active' and a.deleted_at is null and a.status = 'Published' and a.deadline_at is not null
                union
                select a.id as assignment_id, a.title, a.deadline_at, at.target_id as student_id
                from assignments a
                inner join assignment_targets at on a.id = at.assignment_id
                where at.target_type = 'Student' and a.deleted_at is null and a.status = 'Published' and a.deadline_at is not null
            ),
            unsubmitted as (
                select ast.assignment_id, ast.title, ast.deadline_at, ast.student_id, u.email, u.full_name
                from assigned_students ast
                inner join users u on ast.student_id = u.id
                left join submissions s on ast.assignment_id = s.assignment_id and ast.student_id = s.student_id
                where s.id is null -- No submission yet
            )
            select assignment_id, title, deadline_at, student_id, email, full_name
            from unsubmitted
            where deadline_at > now() - interval '24 hours' and deadline_at < now() + interval '24 hours'
            """;

        var candidates = new List<(Guid AssignmentId, string Title, DateTimeOffset DeadlineAt, Guid StudentId, string Email, string FullName)>();

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                candidates.Add((
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetGuid(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));
            }
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var c in candidates)
        {
            var isLate = c.DeadlineAt < now;
            var reminderType = isLate ? "Overdue" : "UpcomingDeadline";
            
            // Check log
            await using var logCommand = new NpgsqlCommand(
                "select count(*) from reminder_logs where assignment_id = @a and student_id = @s and reminder_type = @t", 
                connection);
            logCommand.Parameters.AddWithValue("a", c.AssignmentId);
            logCommand.Parameters.AddWithValue("s", c.StudentId);
            logCommand.Parameters.AddWithValue("t", reminderType);

            var count = Convert.ToInt32(await logCommand.ExecuteScalarAsync(ct));
            if (count > 0)
                continue; // Already sent this type of reminder

            // Send Reminder
            var subject = isLate 
                ? $"[OVERDUE] Cảnh báo nộp bài: {c.Title}" 
                : $"[REMINDER] Sắp hết hạn nộp bài: {c.Title}";
            var messageText = isLate
                ? $"Bài tập <b>{c.Title}</b> đã quá hạn vào lúc {c.DeadlineAt.ToLocalTime():g}. Hãy nộp bài ngay nếu được phép!"
                : $"Bài tập <b>{c.Title}</b> sẽ hết hạn vào lúc {c.DeadlineAt.ToLocalTime():g}. Đừng quên nộp bài nhé!";

            _logger.LogInformation("Sending {ReminderType} reminder for Assignment {AssignmentId} to Student {StudentId}", reminderType, c.AssignmentId, c.StudentId);

            // Create notification
            await notificationService.CreateNotificationAsync(c.StudentId, subject, messageText, ct);

            // Send email
            var emailMsg = new EmailMessage(c.Email, c.FullName, subject, $"<p>Chào {c.FullName},</p><p>{messageText}</p>");
            try
            {
                await emailService.SendEmailAsync(emailMsg, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Email}, but will log reminder anyway to avoid spam loops.", c.Email);
            }

            // Insert log
            await using var insertLog = new NpgsqlCommand(
                "insert into reminder_logs (assignment_id, student_id, reminder_type) values (@a, @s, @t)", 
                connection);
            insertLog.Parameters.AddWithValue("a", c.AssignmentId);
            insertLog.Parameters.AddWithValue("s", c.StudentId);
            insertLog.Parameters.AddWithValue("t", reminderType);
            await insertLog.ExecuteNonQueryAsync(ct);
        }
    }
}
