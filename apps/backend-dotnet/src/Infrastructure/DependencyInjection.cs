using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RemoteAssignment.Application.Health;
using RemoteAssignment.Application.Auth;
using RemoteAssignment.Infrastructure.Auth;
using RemoteAssignment.Infrastructure.Health;
using RemoteAssignment.Application.Organization;
using RemoteAssignment.Infrastructure.Organization;
using RemoteAssignment.Application.AssignmentAuthoring;
using RemoteAssignment.Infrastructure.AssignmentAuthoring;
using RemoteAssignment.Application.Submission;
using RemoteAssignment.Infrastructure.Submission;

namespace RemoteAssignment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("DefaultConnection");

        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.AddSingleton(new DatabaseOptions(databaseConnectionString));
        services.AddSingleton(new DatabaseHealthOptions(databaseConnectionString));
        services.AddScoped<IDatabaseHealthCheck, PostgresDatabaseHealthCheck>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, PostgresAuthService>();
        services.AddScoped<IOrganizationService, PostgresOrganizationService>();
        services.AddScoped<IAssignmentAuthoringService, PostgresAssignmentAuthoringService>();
        services.AddScoped<ISubmissionService, PostgresSubmissionService>();
        
        services.Configure<RemoteAssignment.Application.Email.SmtpOptions>(configuration.GetSection(RemoteAssignment.Application.Email.SmtpOptions.SectionName));
        services.AddScoped<RemoteAssignment.Application.Email.IEmailService, RemoteAssignment.Infrastructure.Email.MailKitEmailService>();
        
        services.AddHostedService<AuthBootstrapService>();

        return services;
    }
}
