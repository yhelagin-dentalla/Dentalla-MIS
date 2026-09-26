using Dentalla.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dentalla.Infrastructure.Persistence;

public sealed class DatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    IOptions<DentallaServerOptions> options,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyDatabaseMigrationsOnStartup && !options.Value.SeedReferenceDataOnStartup)
            return;

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DentallaDbContext>();

        if (options.Value.ApplyDatabaseMigrationsOnStartup)
        {
            logger.LogInformation("Applying Dentalla database migrations...");
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Dentalla database migrations completed.");
        }

        if (options.Value.SeedReferenceDataOnStartup)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<ReferenceDataSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
