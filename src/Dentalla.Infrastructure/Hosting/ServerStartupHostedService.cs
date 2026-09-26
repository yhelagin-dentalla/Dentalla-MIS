using Dentalla.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dentalla.Infrastructure.Hosting;

public sealed class ServerStartupHostedService(IFileStorage fileStorage, ILogger<ServerStartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await fileStorage.EnsureReadyAsync(cancellationToken);
        logger.LogInformation("Dentalla server storage ready at {StorageRoot}", fileStorage.RootPath);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
