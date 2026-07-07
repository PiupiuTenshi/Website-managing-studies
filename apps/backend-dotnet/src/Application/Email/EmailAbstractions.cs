namespace RemoteAssignment.Application.Email;

public sealed record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string BodyHtml
);

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken ct = default);
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";
    
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool UseTls { get; set; }
}
