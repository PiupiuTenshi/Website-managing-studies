using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RemoteAssignment.Application.Health;
using RemoteAssignment.Application.Auth;
using RemoteAssignment.Infrastructure.Auth;
using RemoteAssignment.Infrastructure.Health;
using RemoteAssignment.Application.Organization;
using RemoteAssignment.Infrastructure.Organization;

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
        services.AddHostedService<AuthBootstrapService>();

        return services;
    }
}
