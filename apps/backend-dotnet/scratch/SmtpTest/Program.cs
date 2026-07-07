using System;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

class Program
{
    static async Task Main(string[] args)
    {
        var host = "smtp.gmail.com";
        var port = 587;
        var username = "italone1207@gmail.com";
        var password = "kdwhnzuiurvzujvd";

        Console.WriteLine($"Testing SMTP connection to {host}:{port} with user {username}...");

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Piupiu E-Learning", "italone1207@gmail.com"));
            email.To.Add(new MailboxAddress("Test User", "test@example.com"));
            email.Subject = "Test Email";
            email.Body = new TextPart("plain") { Text = "This is a test email." };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine("SUCCESS!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }
    }
}
