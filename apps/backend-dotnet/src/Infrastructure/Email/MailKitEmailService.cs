using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RemoteAssignment.Application.Email;

namespace RemoteAssignment.Infrastructure.Email;

public class MailKitEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<SmtpOptions> options, ILogger<MailKitEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP is not configured. Skipping email sending to {ToEmail}", message.ToEmail);
            return;
        }

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            email.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
            email.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = message.BodyHtml };
            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            var secureSocketOptions = _options.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            
            await smtp.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, ct);
            if (!string.IsNullOrEmpty(_options.Username))
            {
                await smtp.AuthenticateAsync(_options.Username, _options.Password, ct);
            }
            
            await smtp.SendAsync(email, ct);
            await smtp.DisconnectAsync(true, ct);

            _logger.LogInformation("Successfully sent email to {ToEmail}", message.ToEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);
            throw;
        }
    }
}
