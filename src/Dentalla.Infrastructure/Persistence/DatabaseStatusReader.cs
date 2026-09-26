using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Persistence;

public sealed class DatabaseStatusReader(DentallaDbContext db)
{
    public async Task<DatabaseStatus> ReadAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        var pending = canConnect
            ? (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray()
            : [];
        var applied = canConnect
            ? (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray()
            : [];

        string? serverVersion = null;
        if (canConnect)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "select cast(serverproperty('ProductVersion') as nvarchar(128))";
                serverVersion = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }

        return new DatabaseStatus(
            canConnect,
            connection.DataSource,
            connection.Database,
            serverVersion,
            applied,
            pending);
    }
}

public sealed record DatabaseStatus(
    bool CanConnect,
    string DataSource,
    string Database,
    string? ServerVersion,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> PendingMigrations);
