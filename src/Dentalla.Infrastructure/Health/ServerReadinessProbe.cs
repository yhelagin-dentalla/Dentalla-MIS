using Dentalla.Application.Abstractions;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Health;

public sealed class ServerReadinessProbe(DentallaDbContext dbContext, IFileStorage fileStorage)
{
    public async Task<ServerReadinessResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var dbOk = false;
        string? dbError = null;
        try
        {
            dbOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        var storageOk = false;
        string? storageError = null;
        try
        {
            await fileStorage.EnsureReadyAsync(cancellationToken);
            var probeFile = Path.Combine(fileStorage.RootPath, ".dentalla-write-probe");
            await File.WriteAllTextAsync(probeFile, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
            File.Delete(probeFile);
            storageOk = true;
        }
        catch (Exception ex)
        {
            storageError = ex.Message;
        }

        return new ServerReadinessResult(dbOk && storageOk, dbOk, storageOk, dbError, storageError);
    }
}

public sealed record ServerReadinessResult(bool Ready, bool Database, bool FileStorage, string? DatabaseError, string? FileStorageError);
