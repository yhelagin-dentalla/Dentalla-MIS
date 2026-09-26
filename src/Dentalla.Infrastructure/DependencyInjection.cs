using Dentalla.Application.Abstractions;
using Dentalla.Application.Audit;
using Dentalla.Application.Security;
using Dentalla.Application.Workspaces;
using Dentalla.Infrastructure.Audit;
using Dentalla.Infrastructure.Configuration;
using Dentalla.Infrastructure.Files;
using Dentalla.Infrastructure.Health;
using Dentalla.Infrastructure.Hosting;
using Dentalla.Infrastructure.Persistence;
using Dentalla.Infrastructure.Security;
using Dentalla.Infrastructure.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dentalla.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDentallaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DentallaServerOptions>(configuration.GetSection(DentallaServerOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Dentalla")
            ?? throw new InvalidOperationException("Connection string 'Dentalla' is required.");

        services.AddDbContext<DentallaDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IServerClock, SystemServerClock>();
        services.AddSingleton<PasswordHasher>();

        services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
        services.AddScoped<IEffectivePermissionService, EffectivePermissionService>();
        services.AddScoped<ILoginDirectoryReader, LoginDirectoryReader>();
        services.AddScoped<IWorkspaceScheduleReader, WorkspaceScheduleReader>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<ReferenceDataSeeder>();
        services.AddScoped<DatabaseStatusReader>();
        services.AddScoped<ServerReadinessProbe>();

        // Database first, then server storage. Hosted-service registration order is intentional.
        services.AddHostedService<DatabaseInitializationHostedService>();
        services.AddHostedService<ServerStartupHostedService>();

        return services;
    }
}
