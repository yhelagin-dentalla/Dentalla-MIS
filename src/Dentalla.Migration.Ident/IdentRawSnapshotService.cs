using System.Data;
using Microsoft.Data.SqlClient;

namespace Dentalla.Migration.Ident;

internal sealed class IdentRawSnapshotService(MigrationOptions options)
{
    private readonly SqlConnectionStringBuilder _sourceBuilder = new(options.SourceConnectionString);
    private readonly SqlConnectionStringBuilder _targetBuilder = new(options.TargetConnectionString);

    public async Task InventoryAsync(CancellationToken cancellationToken = default)
    {
        await using var source = new SqlConnection(options.SourceConnectionString);
        await source.OpenAsync(cancellationToken);

        Console.WriteLine($"Source: {source.DataSource} / {source.Database}");
        Console.WriteLine();

        const string sql = """
            SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                SUM(CASE WHEN p.index_id IN (0,1) THEN p.rows ELSE 0 END) AS LegacyRowCount
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            LEFT JOIN sys.partitions p ON p.object_id = t.object_id
            WHERE t.is_ms_shipped = 0
            GROUP BY s.name, t.name
            ORDER BY s.name, t.name;
            """;

        await using var command = new SqlCommand(sql, source);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        long totalRows = 0;
        var count = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            count++;
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var rows = reader.IsDBNull(2) ? 0L : Convert.ToInt64(reader.GetValue(2));
            totalRows += rows;
            Console.WriteLine($"{schema,-18} {table,-55} {rows,12:N0}");
        }

        Console.WriteLine();
        Console.WriteLine($"User tables: {count:N0}");
        Console.WriteLine($"Rows (sys.partitions): {totalRows:N0}");
    }

    public async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var source = new SqlConnection(options.SourceConnectionString);
        await using var target = new SqlConnection(options.TargetConnectionString);
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        Console.WriteLine($"IDENT raw snapshot: {source.DataSource}/{source.Database} -> {target.DataSource}/{target.Database}");
        Console.WriteLine("Source access: READ ONLY by migration code; writes execute only in target Dentalla.");
        Console.WriteLine();

        await EnsureMigrationInfrastructureAsync(target, cancellationToken);
        var tables = await ReadTablesAsync(source, cancellationToken);

        if (!options.ReplaceExisting && await RawLayerContainsTablesAsync(target, cancellationToken))
        {
            throw new InvalidOperationException(
                "Schema ident_raw уже содержит таблицы. Для повторяемого обновления snapshot запусти с --replace. " +
                "Рабочие таблицы Dentalla эта операция не затрагивает.");
        }

        var runId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;
        await InsertRunAsync(target, runId, startedAt, tables.Count, cancellationToken);

        long sourceRowsTotal = 0;
        long targetRowsTotal = 0;
        var copied = 0;
        var failed = 0;

        try
        {
            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var targetTable = SqlIdentifier.RawTableName(table.SchemaName, table.TableName);
                var tableStarted = DateTimeOffset.UtcNow;
                Console.Write($"[{copied + failed + 1,3}/{tables.Count,3}] {table.SchemaName}.{table.TableName} -> ident_raw.{targetTable} ... ");

                try
                {
                    var sourceRows = await CountRowsAsync(source, table.SchemaName, table.TableName, cancellationToken);
                    sourceRowsTotal += sourceRows;

                    await CopyTableAsync(target, table.SchemaName, table.TableName, targetTable, cancellationToken);
                    var targetRows = await CountRowsAsync(target, "ident_raw", targetTable, cancellationToken);
                    targetRowsTotal += targetRows;

                    if (sourceRows != targetRows)
                        throw new DataException($"Row count mismatch: source={sourceRows}, target={targetRows}.");

                    await InsertTableResultAsync(
                        target, runId, table.SchemaName, table.TableName, targetTable,
                        sourceRows, targetRows, "Completed", tableStarted, DateTimeOffset.UtcNow, null, cancellationToken);

                    copied++;
                    Console.WriteLine($"OK ({targetRows:N0})");
                }
                catch (Exception ex)
                {
                    failed++;
                    await InsertTableResultAsync(
                        target, runId, table.SchemaName, table.TableName, targetTable,
                        null, null, "Failed", tableStarted, DateTimeOffset.UtcNow, ex.Message, cancellationToken);
                    Console.WriteLine("FAILED");
                    Console.WriteLine("      " + ex.Message);
                }
            }

            await CaptureObjectDefinitionsAsync(source, target, runId, cancellationToken);

            var status = failed == 0 ? "Completed" : "CompletedWithErrors";
            await CompleteRunAsync(target, runId, status, copied, failed, sourceRowsTotal, targetRowsTotal, null, cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"Snapshot run: {runId}");
            Console.WriteLine($"Tables copied: {copied:N0}; failed: {failed:N0}");
            Console.WriteLine($"Rows copied: {targetRowsTotal:N0}");
            Console.WriteLine(failed == 0
                ? "RESULT: FULL RAW SNAPSHOT COMPLETED."
                : "RESULT: SNAPSHOT COMPLETED WITH ERRORS. See migration.IdentSnapshotTables.");

            if (failed != 0)
                Environment.ExitCode = 2;
        }
        catch (Exception ex)
        {
            await CompleteRunAsync(target, runId, "Failed", copied, failed, sourceRowsTotal, targetRowsTotal, ex.ToString(), cancellationToken);
            throw;
        }
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await using var source = new SqlConnection(options.SourceConnectionString);
        await using var target = new SqlConnection(options.TargetConnectionString);
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        var tables = await ReadTablesAsync(source, cancellationToken);
        var mismatches = 0;
        long sourceTotal = 0;
        long targetTotal = 0;

        foreach (var table in tables)
        {
            var targetTable = SqlIdentifier.RawTableName(table.SchemaName, table.TableName);
            var sourceRows = await CountRowsAsync(source, table.SchemaName, table.TableName, cancellationToken);
            var exists = await TargetTableExistsAsync(target, targetTable, cancellationToken);
            var targetRows = exists ? await CountRowsAsync(target, "ident_raw", targetTable, cancellationToken) : -1;

            sourceTotal += sourceRows;
            if (targetRows >= 0)
                targetTotal += targetRows;

            var ok = exists && sourceRows == targetRows;
            if (!ok)
                mismatches++;

            Console.WriteLine($"{(ok ? "OK " : "BAD")} {table.SchemaName}.{table.TableName,-55} source={sourceRows,10:N0} target={(exists ? targetRows.ToString("N0") : "MISSING"),10}");
        }

        Console.WriteLine();
        Console.WriteLine($"Source rows: {sourceTotal:N0}");
        Console.WriteLine($"Target rows: {targetTotal:N0}");
        Console.WriteLine($"Mismatches: {mismatches:N0}");

        if (mismatches != 0)
            Environment.ExitCode = 3;
    }

    private async Task CopyTableAsync(
        SqlConnection target,
        string sourceSchema,
        string sourceTable,
        string targetTable,
        CancellationToken cancellationToken)
    {
        var sourceDatabase = SqlIdentifier.Quote(_sourceBuilder.InitialCatalog);
        var sourceObject = $"{sourceDatabase}.{SqlIdentifier.Quote(sourceSchema)}.{SqlIdentifier.Quote(sourceTable)}";
        var targetObject = $"{SqlIdentifier.Quote("ident_raw")}.{SqlIdentifier.Quote(targetTable)}";

        var sql = $"""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            IF OBJECT_ID(N'ident_raw.{targetTable.Replace("'", "''", StringComparison.Ordinal)}', N'U') IS NOT NULL
                DROP TABLE {targetObject};
            SELECT * INTO {targetObject} FROM {sourceObject};
            COMMIT TRANSACTION;
            """;

        await using var command = new SqlCommand(sql, target)
        {
            CommandTimeout = 0
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<SourceTable>> ReadTablesAsync(SqlConnection source, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.name, t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name;
            """;

        var result = new List<SourceTable>();
        await using var command = new SqlCommand(sql, source);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SourceTable(reader.GetString(0), reader.GetString(1)));
        return result;
    }

    private static async Task<long> CountRowsAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT_BIG(*) FROM {SqlIdentifier.Quote(schema)}.{SqlIdentifier.Quote(table)};";
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<bool> RawLayerContainsTablesAsync(SqlConnection target, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM sys.tables t
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name = N'ident_raw'
            ) THEN 1 ELSE 0 END;
            """;
        await using var command = new SqlCommand(sql, target);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task<bool> TargetTableExistsAsync(SqlConnection target, string targetTable, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN OBJECT_ID(N'ident_raw.' + @tableName, N'U') IS NULL THEN 0 ELSE 1 END;
            """;
        await using var command = new SqlCommand(sql, target);
        command.Parameters.AddWithValue("@tableName", targetTable);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task EnsureMigrationInfrastructureAsync(SqlConnection target, CancellationToken cancellationToken)
    {
        const string sql = """
            IF SCHEMA_ID(N'ident_raw') IS NULL EXEC(N'CREATE SCHEMA ident_raw AUTHORIZATION dbo;');
            IF SCHEMA_ID(N'migration') IS NULL EXEC(N'CREATE SCHEMA migration AUTHORIZATION dbo;');

            IF OBJECT_ID(N'migration.IdentSnapshotRuns', N'U') IS NULL
            BEGIN
                CREATE TABLE migration.IdentSnapshotRuns
                (
                    RunId uniqueidentifier NOT NULL CONSTRAINT PK_IdentSnapshotRuns PRIMARY KEY,
                    SourceServer nvarchar(256) NOT NULL,
                    SourceDatabase nvarchar(128) NOT NULL,
                    TargetServer nvarchar(256) NOT NULL,
                    TargetDatabase nvarchar(128) NOT NULL,
                    StartedAtUtc datetimeoffset NOT NULL,
                    CompletedAtUtc datetimeoffset NULL,
                    Status nvarchar(40) NOT NULL,
                    SourceTableCount int NOT NULL,
                    CopiedTableCount int NOT NULL CONSTRAINT DF_IdentSnapshotRuns_Copied DEFAULT(0),
                    FailedTableCount int NOT NULL CONSTRAINT DF_IdentSnapshotRuns_Failed DEFAULT(0),
                    SourceRowCount bigint NULL,
                    TargetRowCount bigint NULL,
                    ErrorMessage nvarchar(max) NULL
                );
            END;

            IF OBJECT_ID(N'migration.IdentSnapshotTables', N'U') IS NULL
            BEGIN
                CREATE TABLE migration.IdentSnapshotTables
                (
                    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_IdentSnapshotTables PRIMARY KEY,
                    RunId uniqueidentifier NOT NULL,
                    SourceSchema nvarchar(128) NOT NULL,
                    SourceTable nvarchar(128) NOT NULL,
                    TargetSchema nvarchar(128) NOT NULL,
                    TargetTable nvarchar(128) NOT NULL,
                    SourceRowCount bigint NULL,
                    TargetRowCount bigint NULL,
                    Status nvarchar(40) NOT NULL,
                    StartedAtUtc datetimeoffset NOT NULL,
                    CompletedAtUtc datetimeoffset NULL,
                    ErrorMessage nvarchar(max) NULL,
                    CONSTRAINT FK_IdentSnapshotTables_Run FOREIGN KEY(RunId)
                        REFERENCES migration.IdentSnapshotRuns(RunId)
                );
                CREATE INDEX IX_IdentSnapshotTables_RunId
                    ON migration.IdentSnapshotTables(RunId, Status);
            END;

            IF OBJECT_ID(N'migration.IdentObjectDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE migration.IdentObjectDefinitions
                (
                    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_IdentObjectDefinitions PRIMARY KEY,
                    RunId uniqueidentifier NOT NULL,
                    SourceSchema nvarchar(128) NOT NULL,
                    ObjectName nvarchar(128) NOT NULL,
                    ObjectType nvarchar(80) NOT NULL,
                    Definition nvarchar(max) NULL,
                    CONSTRAINT FK_IdentObjectDefinitions_Run FOREIGN KEY(RunId)
                        REFERENCES migration.IdentSnapshotRuns(RunId)
                );
                CREATE INDEX IX_IdentObjectDefinitions_RunId
                    ON migration.IdentObjectDefinitions(RunId, ObjectType);
            END;
            """;

        await using var command = new SqlCommand(sql, target) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertRunAsync(
        SqlConnection target,
        Guid runId,
        DateTimeOffset startedAt,
        int sourceTableCount,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO migration.IdentSnapshotRuns
            (
                RunId, SourceServer, SourceDatabase, TargetServer, TargetDatabase,
                StartedAtUtc, Status, SourceTableCount, CopiedTableCount, FailedTableCount
            )
            VALUES
            (
                @RunId, @SourceServer, @SourceDatabase, @TargetServer, @TargetDatabase,
                @StartedAtUtc, N'Running', @SourceTableCount, 0, 0
            );
            """;

        await using var command = new SqlCommand(sql, target);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@SourceServer", _sourceBuilder.DataSource);
        command.Parameters.AddWithValue("@SourceDatabase", _sourceBuilder.InitialCatalog);
        command.Parameters.AddWithValue("@TargetServer", _targetBuilder.DataSource);
        command.Parameters.AddWithValue("@TargetDatabase", _targetBuilder.InitialCatalog);
        command.Parameters.AddWithValue("@StartedAtUtc", startedAt);
        command.Parameters.AddWithValue("@SourceTableCount", sourceTableCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertTableResultAsync(
        SqlConnection target,
        Guid runId,
        string sourceSchema,
        string sourceTable,
        string targetTable,
        long? sourceRows,
        long? targetRows,
        string status,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO migration.IdentSnapshotTables
            (
                RunId, SourceSchema, SourceTable, TargetSchema, TargetTable,
                SourceRowCount, TargetRowCount, Status, StartedAtUtc, CompletedAtUtc, ErrorMessage
            )
            VALUES
            (
                @RunId, @SourceSchema, @SourceTable, N'ident_raw', @TargetTable,
                @SourceRowCount, @TargetRowCount, @Status, @StartedAtUtc, @CompletedAtUtc, @ErrorMessage
            );
            """;

        await using var command = new SqlCommand(sql, target);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@SourceSchema", sourceSchema);
        command.Parameters.AddWithValue("@SourceTable", sourceTable);
        command.Parameters.AddWithValue("@TargetTable", targetTable);
        command.Parameters.AddWithValue("@SourceRowCount", (object?)sourceRows ?? DBNull.Value);
        command.Parameters.AddWithValue("@TargetRowCount", (object?)targetRows ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@StartedAtUtc", startedAt);
        command.Parameters.AddWithValue("@CompletedAtUtc", completedAt);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CompleteRunAsync(
        SqlConnection target,
        Guid runId,
        string status,
        int copied,
        int failed,
        long sourceRows,
        long targetRows,
        string? error,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE migration.IdentSnapshotRuns
            SET CompletedAtUtc = @CompletedAtUtc,
                Status = @Status,
                CopiedTableCount = @CopiedTableCount,
                FailedTableCount = @FailedTableCount,
                SourceRowCount = @SourceRowCount,
                TargetRowCount = @TargetRowCount,
                ErrorMessage = @ErrorMessage
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, target);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@CompletedAtUtc", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@CopiedTableCount", copied);
        command.Parameters.AddWithValue("@FailedTableCount", failed);
        command.Parameters.AddWithValue("@SourceRowCount", sourceRows);
        command.Parameters.AddWithValue("@TargetRowCount", targetRows);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CaptureObjectDefinitionsAsync(
        SqlConnection source,
        SqlConnection target,
        Guid runId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                s.name AS SchemaName,
                o.name AS ObjectName,
                o.type_desc AS ObjectType,
                m.definition
            FROM sys.objects o
            INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
            LEFT JOIN sys.sql_modules m ON m.object_id = o.object_id
            WHERE o.is_ms_shipped = 0
              AND o.type IN ('V','P','FN','IF','TF','TR')
            UNION ALL
            SELECT
                SCHEMA_NAME(sy.schema_id),
                sy.name,
                N'SYNONYM',
                sy.base_object_name
            FROM sys.synonyms sy
            ORDER BY SchemaName, ObjectName;
            """;

        await using var readCommand = new SqlCommand(sql, source);
        await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);

        const string insertSql = """
            INSERT INTO migration.IdentObjectDefinitions
                (RunId, SourceSchema, ObjectName, ObjectType, Definition)
            VALUES
                (@RunId, @SourceSchema, @ObjectName, @ObjectType, @Definition);
            """;

        while (await reader.ReadAsync(cancellationToken))
        {
            await using var insert = new SqlCommand(insertSql, target);
            insert.Parameters.AddWithValue("@RunId", runId);
            insert.Parameters.AddWithValue("@SourceSchema", reader.GetString(0));
            insert.Parameters.AddWithValue("@ObjectName", reader.GetString(1));
            insert.Parameters.AddWithValue("@ObjectType", reader.GetString(2));
            insert.Parameters.AddWithValue("@Definition", reader.IsDBNull(3) ? DBNull.Value : reader.GetString(3));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private sealed record SourceTable(string SchemaName, string TableName);
}
