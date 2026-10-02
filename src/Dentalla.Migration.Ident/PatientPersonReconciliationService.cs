using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Dentalla.Migration.Ident;

/// <summary>
/// Stage 02 gate for the additive Patient -> Person transition.
/// This service never guesses a person's name by splitting Patient.FullName.
/// IDENT Persons is the authoritative source for name components during legacy backfill.
/// </summary>
internal sealed class PatientPersonReconciliationService(MigrationOptions options)
{
    private const string LegacySystemCode = "IDENT";

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(options.TargetConnectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureSchemaAsync(connection, cancellationToken);

        var rows = await ReadRowsAsync(connection, cancellationToken);
        if (rows.Count == 0)
            throw new InvalidOperationException("Patient/Person reconciliation found no IDENT patients. Backfill was not executed.");

        var duplicatePatientIds = rows.GroupBy(x => x.PatientId).Where(x => x.Count() != 1).ToArray();
        if (duplicatePatientIds.Length != 0)
            throw new InvalidOperationException($"Reconciliation gate failed: {duplicatePatientIds.Length} Patient ids map to multiple IDENT persons.");

        var duplicatePersonIds = rows.GroupBy(x => x.PersonId).Where(x => x.Count() != 1).ToArray();
        if (duplicatePersonIds.Length != 0)
            throw new InvalidOperationException($"Reconciliation gate failed: {duplicatePersonIds.Length} canonical Person ids map to multiple patients.");

        var missingName = rows.Count(x => string.IsNullOrWhiteSpace(x.LastName) && string.IsNullOrWhiteSpace(x.FirstName));
        if (missingName != 0)
            Console.WriteLine($"WARNING: {missingName:N0} IDENT persons have neither surname nor first name. They are preserved, not merged or guessed.");

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var row in rows)
            {
                await UpsertPersonAsync(connection, transaction, row, cancellationToken);
                await LinkPatientAsync(connection, transaction, row, cancellationToken);
                await UpsertExternalIdentifierAsync(connection, transaction, row, cancellationToken);
            }

            await VerifyAsync(connection, transaction, rows.Count, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        Console.WriteLine("Stage 02 Patient -> Person reconciliation passed.");
        Console.WriteLine($"IDENT patients reconciled: {rows.Count:N0}");
        Console.WriteLine("Patient ids preserved: YES");
        Console.WriteLine("Name components sourced from ident_raw.Persons: YES");
        Console.WriteLine("Automatic duplicate merge: NO");
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN
                OBJECT_ID(N'dbo.Persons', N'U') IS NOT NULL AND
                COL_LENGTH(N'dbo.Patients', N'PersonId') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Persons', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Patients', N'U') IS NOT NULL AND
                OBJECT_ID(N'integration.ExternalIdentifiers', N'U') IS NOT NULL
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        await using var command = new SqlCommand(sql, connection);
        var ready = Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (!ready)
            throw new InvalidOperationException("Stage 02 Person schema is not present. Apply the EF migration before running patient-person reconciliation.");
    }

    private static async Task<List<Row>> ReadRowsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Existing Patient identity comes from the already-imported IDENT ExternalIdentifier.
        // This is deliberate: no Patient Guid is regenerated during Stage 02.
        const string sql = """
            SELECT
                p.Id AS PatientId,
                TRY_CONVERT(int, ei.ExternalId) AS LegacyPersonId,
                LEFT(ISNULL(LTRIM(RTRIM(ip.Surname)), N''), 150) AS LastName,
                LEFT(ISNULL(LTRIM(RTRIM(ip.Name)), N''), 150) AS FirstName,
                LEFT(ISNULL(LTRIM(RTRIM(ip.Patronymic)), N''), 150) AS MiddleName,
                COALESCE(
                    TRY_CONVERT(date, NULLIF(ip.Birthday, N''), 104),
                    TRY_CONVERT(date, NULLIF(ip.Birthday, N''), 23),
                    TRY_CONVERT(date, NULLIF(ip.Birthday, N''))) AS BirthDate
            FROM dbo.Patients AS p
            INNER JOIN integration.ExternalIdentifiers AS ei
                ON ei.EntityType = N'Patient'
               AND ei.EntityId = p.Id
               AND ei.SourceSystem = N'IDENT'
            INNER JOIN ident_raw.Patients AS ipa
                ON ipa.ID_Persons = TRY_CONVERT(int, ei.ExternalId)
            INNER JOIN ident_raw.Persons AS ip
                ON ip.ID = ipa.ID_Persons
            WHERE TRY_CONVERT(int, ei.ExternalId) IS NOT NULL
            ORDER BY p.Id;
            """;

        var result = new List<Row>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var patientId = reader.GetGuid(0);
            var legacyPersonId = reader.GetInt32(1);
            result.Add(new Row(
                patientId,
                StableGuid("Person", legacyPersonId.ToString(CultureInfo.InvariantCulture)),
                legacyPersonId,
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5))));
        }
        return result;
    }

    private static async Task UpsertPersonAsync(SqlConnection connection, SqlTransaction transaction, Row row, CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE dbo.Persons AS target
            USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
            WHEN MATCHED THEN UPDATE SET
                LastName = @LastName,
                FirstName = @FirstName,
                MiddleName = @MiddleName,
                BirthDate = @BirthDate
            WHEN NOT MATCHED THEN INSERT (Id, LastName, FirstName, MiddleName, BirthDate)
                VALUES (@Id, @LastName, @FirstName, @MiddleName, @BirthDate);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Id", row.PersonId);
        command.Parameters.AddWithValue("@LastName", row.LastName);
        command.Parameters.AddWithValue("@FirstName", row.FirstName);
        command.Parameters.AddWithValue("@MiddleName", row.MiddleName);
        command.Parameters.AddWithValue("@BirthDate", row.BirthDate is null ? DBNull.Value : row.BirthDate.Value.ToDateTime(TimeOnly.MinValue));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task LinkPatientAsync(SqlConnection connection, SqlTransaction transaction, Row row, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Patients
            SET PersonId = @PersonId
            WHERE Id = @PatientId
              AND (PersonId IS NULL OR PersonId = @PersonId);

            IF @@ROWCOUNT <> 1
                THROW 51000, 'Patient is already linked to a different Person.', 1;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@PatientId", row.PatientId);
        command.Parameters.AddWithValue("@PersonId", row.PersonId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertExternalIdentifierAsync(SqlConnection connection, SqlTransaction transaction, Row row, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS
            (
                SELECT 1 FROM integration.ExternalIdentifiers
                WHERE EntityType = N'Person' AND SourceSystem = @SourceSystem AND ExternalId = @ExternalId
            )
            BEGIN
                INSERT INTO integration.ExternalIdentifiers (Id, EntityType, SourceSystem, ExternalId, EntityId)
                VALUES (@Id, N'Person', @SourceSystem, @ExternalId, @EntityId);
            END
            ELSE IF EXISTS
            (
                SELECT 1 FROM integration.ExternalIdentifiers
                WHERE EntityType = N'Person' AND SourceSystem = @SourceSystem AND ExternalId = @ExternalId AND EntityId <> @EntityId
            )
                THROW 51001, 'IDENT Person external identifier points to another canonical Person.', 1;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Id", StableGuid("ExternalIdentifier", $"Person:{row.LegacyPersonId}"));
        command.Parameters.AddWithValue("@SourceSystem", LegacySystemCode);
        command.Parameters.AddWithValue("@ExternalId", row.LegacyPersonId.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@EntityId", row.PersonId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task VerifyAsync(SqlConnection connection, SqlTransaction transaction, int expected, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COUNT_BIG(*) AS PatientCount,
                SUM(CASE WHEN p.PersonId IS NULL THEN 1 ELSE 0 END) AS Unlinked,
                COUNT_BIG(DISTINCT p.PersonId) AS DistinctPersons
            FROM dbo.Patients AS p
            INNER JOIN integration.ExternalIdentifiers AS ei
                ON ei.EntityType = N'Patient' AND ei.EntityId = p.Id AND ei.SourceSystem = N'IDENT';
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var patientCount = Convert.ToInt32(reader.GetInt64(0), CultureInfo.InvariantCulture);
        var unlinked = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
        var distinctPersons = Convert.ToInt32(reader.GetInt64(2), CultureInfo.InvariantCulture);

        if (patientCount != expected || unlinked != 0 || distinctPersons != expected)
            throw new InvalidOperationException($"Stage 02 reconciliation gate failed: expected={expected}, patients={patientCount}, unlinked={unlinked}, distinctPersons={distinctPersons}.");
    }

    private static Guid StableGuid(string entityType, string externalId)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"Dentalla|{LegacySystemCode}|{entityType}|{externalId}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    private sealed record Row(Guid PatientId, Guid PersonId, int LegacyPersonId, string LastName, string FirstName, string MiddleName, DateOnly? BirthDate);
}
