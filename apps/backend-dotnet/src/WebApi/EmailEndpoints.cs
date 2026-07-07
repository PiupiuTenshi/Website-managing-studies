using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RemoteAssignment.Application.Email;

namespace RemoteAssignment.WebApi;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/smtp").RequireAuthorization("AdminOnly");

        group.MapPost("/test", async (TestEmailRequest request, IEmailService emailService, CancellationToken ct) =>
        {
            try
            {
                var message = new EmailMessage(request.ToEmail, request.ToName, request.Subject, request.BodyHtml);
                await emailService.SendEmailAsync(message, ct);
                return Results.Ok(new { Message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        });
    }
}

public sealed record TestEmailRequest(string ToEmail, string ToName, string Subject, string BodyHtml);
