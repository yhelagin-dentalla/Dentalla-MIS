using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Dentalla.Migration.Ident;

/// <summary>
/// Second pass for IDENT appointments.
///
/// Core normalization historically treated every non-cancelled IDENT reception as Scheduled
/// and only saw receptions that still had CurrentTimeTable slots. This pass is deliberately
/// source-factual and idempotent: it reads the immutable ident_raw snapshot, keeps the existing
/// slot-derived time when available, falls back to Receptions.PlanStart/PlanEnd when the slot was
/// removed (common for cancelled historical receptions), and derives status from IDENT facts.
///
/// Patient/Staff links are resolved through source-aware ExternalIdentifier mappings instead of
/// re-deriving canonical IDs from legacy IDs. This is required after patient identity resolution,
/// merges and intentional exclusions: a legacy IDENT patient ID is not a Dentalla primary key.
/// Receptions whose IDENT patient has no canonical Dentalla mapping are skipped and reported.
///
/// It only INSERTs/UPDATEs Dentalla-owned normalized Appointment rows and their source mapping.
/// It never modifies ident_raw and never deletes normalized data.
/// </summary>
internal sealed class AppointmentStatusNormalizationService(MigrationOptions options)
{
    private const string LegacySystemCode = "IDENT";

    public async Task NormalizeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(options.TargetConnectionString);
        await connection.OpenAsync(cancellationToken);

        var totalSourceReceptions = await ReadSourceReceptionCountAsync(connection, cancellationToken);
        var sourceRows = await ReadAppointmentsAsync(connection, cancellationToken);
        var skippedWithoutCanonicalPatient = totalSourceReceptions - sourceRows.Count;
        var unresolvedStaffLinks = sourceRows.Count(x => x.LegacyStaffId is not null && x.StaffProfileId is null);
        var now = DateTimeOffset.UtcNow;

        await ExecuteAsync(connection, """
            CREATE TABLE #AppointmentFactStage
            (
                Id uniqueidentifier NOT NULL,
                LegacyId int NOT NULL,
                PatientId uniqueidentifier NOT NULL,
                StaffProfileId uniqueidentifier NULL,
                StartLocal datetime2 NOT NULL,
                EndLocal datetime2 NOT NULL,
                StatusCode nvarchar(40) NOT NULL,
                LegacyRoomId int NULL,
                ImportedAtUtc datetimeoffset NOT NULL
            );

            CREATE TABLE #AppointmentExternalFactStage
            (
                Id uniqueidentifier NOT NULL,
                SystemCode nvarchar(40) NOT NULL,
                EntityType nvarchar(80) NOT NULL,
                ExternalId nvarchar(160) NOT NULL,
                InternalEntityId uniqueidentifier NOT NULL,
                ImportedAtUtc datetimeoffset NOT NULL
            );
            """, cancellationToken);

        var appointmentTable = CreateTable(
            ("Id", typeof(Guid)),
            ("LegacyId", typeof(int)),
            ("PatientId", typeof(Guid)),
            ("StaffProfileId", typeof(Guid)),
            ("StartLocal", typeof(DateTime)),
            ("EndLocal", typeof(DateTime)),
            ("StatusCode", typeof(string)),
            ("LegacyRoomId", typeof(int)),
            ("ImportedAtUtc", typeof(DateTimeOffset)));

        var externalTable = CreateTable(
            ("Id", typeof(Guid)),
            ("SystemCode", typeof(string)),
            ("EntityType", typeof(string)),
            ("ExternalId", typeof(string)),
            ("InternalEntityId", typeof(Guid)),
            ("ImportedAtUtc", typeof(DateTimeOffset)));

        foreach (var item in sourceRows)
        {
            var appointmentId = StableGuid("Appointment", item.LegacyReceptionId.ToString(CultureInfo.InvariantCulture));
            var end = item.EndLocal <= item.StartLocal ? item.StartLocal.AddMinutes(15) : item.EndLocal;

            appointmentTable.Rows.Add(
                appointmentId,
                item.LegacyReceptionId,
                item.PatientId,
                item.StaffProfileId is null ? (object)DBNull.Value : item.StaffProfileId.Value,
                item.StartLocal,
                end,
                item.StatusCode,
                item.LegacyRoomId is null ? (object)DBNull.Value : item.LegacyRoomId.Value,
                now);

            var externalId = item.LegacyReceptionId.ToString(CultureInfo.InvariantCulture);
            externalTable.Rows.Add(
                StableGuid("ExternalIdentifier", $"{LegacySystemCode}:Appointment:{externalId}"),
                LegacySystemCode,
                "Appointment",
                externalId,
                appointmentId,
                now);
        }

        await BulkCopyAsync(connection, "#AppointmentFactStage", appointmentTable, cancellationToken);
        await BulkCopyAsync(connection, "#AppointmentExternalFactStage", externalTable, cancellationToken);

        var before = await ReadStatusCountsAsync(connection, cancellationToken);

        await ExecuteAsync(connection, """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            BEGIN TRY
                MERGE scheduling.Appointments AS target
                USING #AppointmentFactStage AS source ON target.Id = source.Id
                WHEN MATCHED THEN UPDATE SET
                    PatientId = source.PatientId,
                    StaffProfileId = source.StaffProfileId,
                    StartLocal = source.StartLocal,
                    EndLocal = source.EndLocal,
                    StatusCode = source.StatusCode,
                    LegacyRoomId = source.LegacyRoomId,
                    ImportedAtUtc = source.ImportedAtUtc
                WHEN NOT MATCHED THEN INSERT
                    (Id, PatientId, StaffProfileId, StartLocal, EndLocal, StatusCode, LegacyRoomId, ImportedAtUtc)
                    VALUES
                    (source.Id, source.PatientId, source.StaffProfileId, source.StartLocal, source.EndLocal, source.StatusCode, source.LegacyRoomId, source.ImportedAtUtc);

                MERGE integration.ExternalIdentifiers AS target
                USING #AppointmentExternalFactStage AS source
                    ON target.SystemCode = source.SystemCode
                   AND target.EntityType = source.EntityType
                   AND target.ExternalId = source.ExternalId
                WHEN MATCHED THEN UPDATE SET
                    InternalEntityId = source.InternalEntityId,
                    ImportedAtUtc = source.ImportedAtUtc
                WHEN NOT MATCHED THEN INSERT
                    (Id, SystemCode, EntityType, ExternalId, InternalEntityId, ImportedAtUtc)
                    VALUES
                    (source.Id, source.SystemCode, source.EntityType, source.ExternalId, source.InternalEntityId, source.ImportedAtUtc);

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """, cancellationToken);

        var after = await ReadStatusCountsAsync(connection, cancellationToken);

        Console.WriteLine();
        Console.WriteLine("IDENT appointment factual-status normalization");
        Console.WriteLine($"Source Receptions with patient: {totalSourceReceptions:N0}");
        Console.WriteLine($"Mapped to canonical Dentalla Patient: {sourceRows.Count:N0}");
        Console.WriteLine($"Skipped without canonical Patient mapping: {skippedWithoutCanonicalPatient:N0}");
        Console.WriteLine($"Rows with unresolved StaffProfile mapping: {unresolvedStaffLinks:N0}");
        Console.WriteLine($"Before: {before}");
        Console.WriteLine($"After:  {after}");
        Console.WriteLine("Status rules: Cancelled = cancellation fact; Fulfilled = ReceptionEnded/CheckIssued; Arrived = ReceptionStarted/PatientAppeared; otherwise Scheduled.");
        Console.WriteLine("Historical receptions without CurrentTimeTable slots are retained using PlanStart/PlanEnd fallback.");
    }

    private static async Task<long> ReadSourceReceptionCountAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT_BIG(*)
            FROM ident_raw.Receptions
            WHERE ID_Patients IS NOT NULL;
            """;

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task<List<LegacyAppointmentFact>> ReadAppointmentsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH time_map AS
            (
                SELECT
                    ID,
                    TRY_CONVERT(time(0), TimeStartValue) AS StartTime,
                    LEAD(TRY_CONVERT(time(0), TimeStartValue)) OVER
                        (ORDER BY TRY_CONVERT(time(0), TimeStartValue), ID) AS NextStartTime
                FROM ident_raw.Times
            ),
            slots AS
            (
                SELECT
                    c.ID_Receptions AS LegacyReceptionId,
                    c.ID_Armchairs,
                    c.ID_Staffs AS SlotStaffId,
                    DATEADD(SECOND,
                        DATEDIFF(SECOND, CAST('00:00:00' AS time), tm.StartTime),
                        CONVERT(datetime2(0), c.DateOfWork)) AS StartLocal,
                    DATEADD(SECOND,
                        DATEDIFF(SECOND, CAST('00:00:00' AS time),
                            COALESCE(tm.NextStartTime, DATEADD(MINUTE, 15, tm.StartTime))),
                        CONVERT(datetime2(0), c.DateOfWork)) AS EndLocal
                FROM ident_raw.CurrentTimeTable AS c
                INNER JOIN time_map AS tm ON tm.ID = c.ID_Times
                WHERE c.ID_Receptions IS NOT NULL
                  AND ISNULL(c.IsHeaderCell, 0) = 0
                  AND tm.StartTime IS NOT NULL
            ),
            slot_bounds AS
            (
                SELECT
                    LegacyReceptionId,
                    MIN(StartLocal) AS StartLocal,
                    MAX(EndLocal) AS EndLocal,
                    MAX(SlotStaffId) AS SlotStaffId,
                    MIN(CONVERT(int, ID_Armchairs)) AS LegacyRoomId
                FROM slots
                GROUP BY LegacyReceptionId
            )
            SELECT
                r.ID AS LegacyReceptionId,
                r.ID_Patients AS LegacyPatientId,
                patient.Id AS PatientId,
                COALESCE(r.ID_Staffs, sb.SlotStaffId) AS LegacyStaffId,
                staff.Id AS StaffProfileId,
                COALESCE(sb.StartLocal, CONVERT(datetime2(0), r.PlanStart)) AS StartLocal,
                COALESCE(sb.EndLocal, CONVERT(datetime2(0), r.PlanEnd)) AS EndLocal,
                CASE
                    WHEN r.ReceptionCanceled IS NOT NULL
                      OR r.ID_ReceptionCancelReasons IS NOT NULL
                        THEN N'Cancelled'
                    WHEN r.ReceptionEnded IS NOT NULL
                      OR r.CheckIssued IS NOT NULL
                        THEN N'Fulfilled'
                    WHEN r.ReceptionStarted IS NOT NULL
                      OR r.PatientAppeared IS NOT NULL
                        THEN N'Arrived'
                    ELSE N'Scheduled'
                END AS StatusCode,
                COALESCE(sb.LegacyRoomId, CONVERT(int, r.ID_Armchairs)) AS LegacyRoomId
            FROM ident_raw.Receptions AS r
            LEFT JOIN slot_bounds AS sb
                ON sb.LegacyReceptionId = r.ID
            INNER JOIN integration.ExternalIdentifiers AS patientMap
                ON patientMap.SystemCode = N'IDENT'
               AND patientMap.EntityType = N'Patient'
               AND patientMap.ExternalId = CONVERT(nvarchar(160), r.ID_Patients)
            INNER JOIN dbo.Patients AS patient
                ON patient.Id = patientMap.InternalEntityId
            LEFT JOIN integration.ExternalIdentifiers AS staffMap
                ON staffMap.SystemCode = N'IDENT'
               AND staffMap.EntityType = N'StaffProfile'
               AND staffMap.ExternalId = CONVERT(nvarchar(160), COALESCE(r.ID_Staffs, sb.SlotStaffId))
            LEFT JOIN staff.StaffProfiles AS staff
                ON staff.Id = staffMap.InternalEntityId
            WHERE r.ID_Patients IS NOT NULL
              AND COALESCE(sb.StartLocal, CONVERT(datetime2(0), r.PlanStart)) IS NOT NULL
            ORDER BY COALESCE(sb.StartLocal, CONVERT(datetime2(0), r.PlanStart)), r.ID;
            """;

        var result = new List<LegacyAppointmentFact>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            int? legacyStaffId = reader.IsDBNull(3) ? null : reader.GetInt32(3);
            Guid? staffProfileId = reader.IsDBNull(4) ? null : reader.GetGuid(4);

            result.Add(new LegacyAppointmentFact(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetGuid(2),
                legacyStaffId,
                staffProfileId,
                DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Unspecified),
                DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Unspecified),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetInt32(8)));
        }

        return result;
    }

    private static async Task<string> ReadStatusCountsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT STRING_AGG(CONCAT(StatusCode, N'=', Cnt), N'; ')
            FROM
            (
                SELECT a.StatusCode, COUNT_BIG(*) AS Cnt
                FROM scheduling.Appointments AS a
                INNER JOIN integration.ExternalIdentifiers AS ei
                    ON ei.InternalEntityId = a.Id
                   AND ei.SystemCode = N'IDENT'
                   AND ei.EntityType = N'Appointment'
                GROUP BY a.StatusCode
            ) AS x;
            """;

        await using var command = new SqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? "<none>";
    }

    private static DataTable CreateTable(params (string Name, Type Type)[] columns)
    {
        var table = new DataTable { Locale = CultureInfo.InvariantCulture };
        foreach (var column in columns)
            table.Columns.Add(column.Name, column.Type);
        return table;
    }

    private static async Task BulkCopyAsync(
        SqlConnection connection,
        string destinationTable,
        DataTable table,
        CancellationToken cancellationToken)
    {
        if (table.Rows.Count == 0)
            return;

        using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, null)
        {
            DestinationTableName = destinationTable,
            BatchSize = 5000,
            BulkCopyTimeout = 0
        };

        foreach (DataColumn column in table.Columns)
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulk.WriteToServerAsync(table, cancellationToken);
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Guid StableGuid(string scope, string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"Dentalla|IDENT|{scope}|{value}"));
        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private sealed record LegacyAppointmentFact(
        int LegacyReceptionId,
        int LegacyPatientId,
        Guid PatientId,
        int? LegacyStaffId,
        Guid? StaffProfileId,
        DateTime StartLocal,
        DateTime EndLocal,
        string StatusCode,
        int? LegacyRoomId);
}
