using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Dentalla.Migration.Ident;

internal sealed class CoreNormalizationService(MigrationOptions options)
{
    private const string LegacySystemCode = "IDENT";

    public async Task NormalizeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(options.TargetConnectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsurePrerequisitesAsync(connection, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        Console.WriteLine("Dentalla core normalization");
        Console.WriteLine("Source: Dentalla.ident_raw");
        Console.WriteLine("Target: StaffProfile / UserAccount / UserRoleAssignment / Patient / Appointment");
        Console.WriteLine();

        var staff = await ReadStaffAsync(connection, cancellationToken);
        var roles = await ReadRolesAsync(connection, cancellationToken);
        if (options.ChiefMedicalOfficerLegacyStaffId is { } cmoId && staff.Any(x => x.LegacyId == cmoId))
            roles.Add(new LegacyRole(cmoId, "ChiefMedicalOfficer"));

        roles = roles.Distinct().ToList();
        var roleStaffIds = roles.Select(x => x.LegacyStaffId).ToHashSet();

        await NormalizeStaffAsync(connection, staff, roles, roleStaffIds, now, cancellationToken);
        Console.WriteLine($"StaffProfiles: {staff.Count:N0}; UserAccounts: {roleStaffIds.Count:N0}; role assignments: {roles.Count:N0}");

        var patients = await ReadPatientsAsync(connection, cancellationToken);
        await NormalizePatientsAsync(connection, patients, now, cancellationToken);
        Console.WriteLine($"Patients: {patients.Count:N0}");

        var appointments = await ReadAppointmentsAsync(connection, cancellationToken);
        await NormalizeAppointmentsAsync(connection, appointments, now, cancellationToken);
        Console.WriteLine($"Appointments: {appointments.Count:N0}");

        Console.WriteLine();
        Console.WriteLine("RESULT: CORE NORMALIZATION COMPLETED.");
        Console.WriteLine("Login directory now reads Dentalla staff/security tables and no longer reads ident_raw.");
    }

    private static async Task EnsurePrerequisitesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN
                OBJECT_ID(N'ident_raw.Staffs', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Persons', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Items', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.ProfessionNames', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Patients', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Receptions', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.CurrentTimeTable', N'U') IS NOT NULL AND
                OBJECT_ID(N'ident_raw.Times', N'U') IS NOT NULL AND
                OBJECT_ID(N'staff.StaffProfiles', N'U') IS NOT NULL AND
                OBJECT_ID(N'security.UserAccounts', N'U') IS NOT NULL AND
                OBJECT_ID(N'scheduling.Appointments', N'U') IS NOT NULL AND
                OBJECT_ID(N'integration.ExternalIdentifiers', N'U') IS NOT NULL
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        await using var command = new SqlCommand(sql, connection);
        var ready = Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (!ready)
        {
            throw new InvalidOperationException(
                "Не найден полный ident_raw snapshot или migration CoreLegacyNormalization ещё не применена. " +
                "Сначала запустите Dentalla.Api после сборки, затем выполните snapshot/verify при необходимости.");
        }
    }

    private static async Task<List<LegacyStaff>> ReadStaffAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                s.ID_Persons,
                LTRIM(RTRIM(CONCAT(
                    NULLIF(LTRIM(RTRIM(p.Surname)), N''), N' ',
                    NULLIF(LTRIM(RTRIM(p.Name)), N''), N' ',
                    NULLIF(LTRIM(RTRIM(p.Patronymic)), N'')))) AS DisplayName,
                CAST(CASE WHEN ISNULL(s.Archive, 0) = 0 THEN 1 ELSE 0 END AS bit) AS IsActive
            FROM ident_raw.Staffs AS s
            INNER JOIN ident_raw.Persons AS p ON p.ID = s.ID_Persons
            ORDER BY s.ID_Persons;
            """;

        var result = new List<LegacyStaff>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyId = reader.GetInt32(0);
            var displayName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = $"Сотрудник IDENT #{legacyId}";

            result.Add(new LegacyStaff(legacyId, displayName, reader.GetBoolean(2)));
        }

        return result;
    }

    private static async Task<List<LegacyRole>> ReadRolesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT
                s.ID_Persons,
                CASE pn.ID_Roles
                    WHEN 8 THEN N'Doctor'
                    WHEN 7 THEN N'Administrator'
                    WHEN 104 THEN N'Marketer'
                    WHEN 9 THEN N'Director'
                END AS RoleCode
            FROM ident_raw.Staffs AS s
            INNER JOIN ident_raw.Items AS i ON i.ID_Staffs = s.ID_Persons
            INNER JOIN ident_raw.ProfessionNames AS pn ON pn.ID = i.ID_ProfessionNames
            WHERE ISNULL(s.Archive, 0) = 0
              AND ISNULL(pn.Archive, 0) = 0
              AND pn.ID_Roles IN (7, 8, 9, 104);
            """;

        var result = new List<LegacyRole>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(1))
                result.Add(new LegacyRole(reader.GetInt32(0), reader.GetString(1)));
        }

        return result;
    }

    private static async Task<List<LegacyPatient>> ReadPatientsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                pa.ID_Persons,
                LEFT(ISNULL(LTRIM(RTRIM(pa.CardNumber)), N''), 64) AS CardNumber,
                LTRIM(RTRIM(CONCAT(
                    NULLIF(LTRIM(RTRIM(pe.Surname)), N''), N' ',
                    NULLIF(LTRIM(RTRIM(pe.Name)), N''), N' ',
                    NULLIF(LTRIM(RTRIM(pe.Patronymic)), N'')))) AS FullName,
                COALESCE(
                    TRY_CONVERT(date, NULLIF(pe.Birthday, N''), 104),
                    TRY_CONVERT(date, NULLIF(pe.Birthday, N''), 23),
                    TRY_CONVERT(date, NULLIF(pe.Birthday, N''))) AS BirthDate
            FROM ident_raw.Patients AS pa
            INNER JOIN ident_raw.Persons AS pe ON pe.ID = pa.ID_Persons
            ORDER BY pa.ID_Persons;
            """;

        var result = new List<LegacyPatient>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyId = reader.GetInt32(0);
            var card = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
            var name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = $"Пациент IDENT #{legacyId}";

            DateTime? birthDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
            result.Add(new LegacyPatient(legacyId, card, name, birthDate));
        }

        return result;
    }

    private static async Task<List<LegacyAppointment>> ReadAppointmentsAsync(SqlConnection connection, CancellationToken cancellationToken)
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
                    c.DateOfWork,
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
            )
            SELECT
                s.LegacyReceptionId,
                r.ID_Patients,
                COALESCE(MAX(r.ID_Staffs), MAX(s.SlotStaffId)) AS LegacyStaffId,
                MIN(s.StartLocal) AS StartLocal,
                MAX(s.EndLocal) AS EndLocal,
                CASE WHEN r.ID_ReceptionCancelReasons IS NOT NULL THEN N'Cancelled' ELSE N'Scheduled' END AS StatusCode,
                MIN(CONVERT(int, s.ID_Armchairs)) AS LegacyRoomId
            FROM slots AS s
            INNER JOIN ident_raw.Receptions AS r ON r.ID = s.LegacyReceptionId
            WHERE r.ID_Patients IS NOT NULL
            GROUP BY s.LegacyReceptionId, r.ID_Patients, r.ID_ReceptionCancelReasons
            ORDER BY MIN(s.StartLocal), s.LegacyReceptionId;
            """;

        var result = new List<LegacyAppointment>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var start = reader.GetDateTime(3);
            var end = reader.GetDateTime(4);
            if (end <= start)
                end = start.AddMinutes(15);

            result.Add(new LegacyAppointment(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                DateTime.SpecifyKind(start, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(end, DateTimeKind.Unspecified),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6)));
        }

        return result;
    }

    private static async Task NormalizeStaffAsync(
        SqlConnection connection,
        IReadOnlyCollection<LegacyStaff> staff,
        IReadOnlyCollection<LegacyRole> roles,
        IReadOnlySet<int> roleStaffIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, """
            CREATE TABLE #StaffStage
            (
                Id uniqueidentifier NOT NULL,
                LegacyId int NOT NULL,
                DisplayName nvarchar(300) NOT NULL,
                IsActive bit NOT NULL,
                CreatedAtUtc datetimeoffset NOT NULL,
                DisabledAtUtc datetimeoffset NULL
            );
            CREATE TABLE #UserStage
            (
                Id uniqueidentifier NOT NULL,
                StaffProfileId uniqueidentifier NOT NULL,
                LegacyId int NOT NULL,
                UserName nvarchar(120) NOT NULL,
                NormalizedUserName nvarchar(120) NOT NULL,
                CreatedAtUtc datetimeoffset NOT NULL
            );
            CREATE TABLE #RoleStage
            (
                Id uniqueidentifier NOT NULL,
                UserAccountId uniqueidentifier NOT NULL,
                RoleCode nvarchar(80) NOT NULL,
                ValidFromUtc datetimeoffset NOT NULL,
                GrantedByUserAccountId uniqueidentifier NOT NULL
            );
            CREATE TABLE #ExternalStage
            (
                Id uniqueidentifier NOT NULL,
                SystemCode nvarchar(40) NOT NULL,
                EntityType nvarchar(80) NOT NULL,
                ExternalId nvarchar(160) NOT NULL,
                InternalEntityId uniqueidentifier NOT NULL,
                ImportedAtUtc datetimeoffset NOT NULL
            );
            """, cancellationToken);

        var staffTable = CreateTable(("Id", typeof(Guid)), ("LegacyId", typeof(int)), ("DisplayName", typeof(string)),
            ("IsActive", typeof(bool)), ("CreatedAtUtc", typeof(DateTimeOffset)), ("DisabledAtUtc", typeof(DateTimeOffset)));
        var userTable = CreateTable(("Id", typeof(Guid)), ("StaffProfileId", typeof(Guid)), ("LegacyId", typeof(int)),
            ("UserName", typeof(string)), ("NormalizedUserName", typeof(string)), ("CreatedAtUtc", typeof(DateTimeOffset)));
        var roleTable = CreateTable(("Id", typeof(Guid)), ("UserAccountId", typeof(Guid)), ("RoleCode", typeof(string)),
            ("ValidFromUtc", typeof(DateTimeOffset)), ("GrantedByUserAccountId", typeof(Guid)));
        var externalTable = CreateTable(("Id", typeof(Guid)), ("SystemCode", typeof(string)), ("EntityType", typeof(string)),
            ("ExternalId", typeof(string)), ("InternalEntityId", typeof(Guid)), ("ImportedAtUtc", typeof(DateTimeOffset)));

        foreach (var item in staff)
        {
            var staffId = StableGuid("StaffProfile", item.LegacyId.ToString(CultureInfo.InvariantCulture));
            staffTable.Rows.Add(staffId, item.LegacyId, item.DisplayName, item.IsActive, now, item.IsActive ? (object)DBNull.Value : now);
            AddExternalRow(externalTable, "StaffProfile", item.LegacyId, staffId, now);

            if (!roleStaffIds.Contains(item.LegacyId))
                continue;

            var userId = StableGuid("UserAccount", item.LegacyId.ToString(CultureInfo.InvariantCulture));
            var userName = $"ident-{item.LegacyId.ToString(CultureInfo.InvariantCulture)}";
            userTable.Rows.Add(userId, staffId, item.LegacyId, userName, userName.ToUpperInvariant(), now);
            AddExternalRow(externalTable, "UserAccount", item.LegacyId, userId, now);
        }

        foreach (var role in roles)
        {
            if (!roleStaffIds.Contains(role.LegacyStaffId))
                continue;

            var userId = StableGuid("UserAccount", role.LegacyStaffId.ToString(CultureInfo.InvariantCulture));
            var assignmentId = StableGuid("UserRoleAssignment", $"{role.LegacyStaffId}:{role.RoleCode}");
            roleTable.Rows.Add(assignmentId, userId, role.RoleCode, now, userId);
        }

        await BulkCopyAsync(connection, "#StaffStage", staffTable, cancellationToken);
        await BulkCopyAsync(connection, "#UserStage", userTable, cancellationToken);
        await BulkCopyAsync(connection, "#RoleStage", roleTable, cancellationToken);
        await BulkCopyAsync(connection, "#ExternalStage", externalTable, cancellationToken);

        await ExecuteAsync(connection, """
            MERGE staff.StaffProfiles AS target
            USING #StaffStage AS source ON target.Id = source.Id
            WHEN MATCHED THEN UPDATE SET
                DisplayName = source.DisplayName,
                IsActive = source.IsActive,
                DisabledAtUtc = source.DisabledAtUtc
            WHEN NOT MATCHED THEN INSERT (Id, DisplayName, IsActive, CreatedAtUtc, DisabledAtUtc)
                VALUES (source.Id, source.DisplayName, source.IsActive, source.CreatedAtUtc, source.DisabledAtUtc);

            MERGE security.UserAccounts AS target
            USING #UserStage AS source ON target.Id = source.Id
            WHEN MATCHED THEN UPDATE SET
                StaffProfileId = source.StaffProfileId,
                UserName = source.UserName,
                NormalizedUserName = source.NormalizedUserName,
                IsActive = 1,
                DisabledAtUtc = NULL
            WHEN NOT MATCHED THEN INSERT
                (Id, StaffProfileId, UserName, NormalizedUserName, IsActive, CreatedAtUtc, DisabledAtUtc)
                VALUES (source.Id, source.StaffProfileId, source.UserName, source.NormalizedUserName, 1, source.CreatedAtUtc, NULL);

            UPDATE existing
            SET ValidToUtc = SYSUTCDATETIME()
            FROM security.UserRoleAssignments AS existing
            WHERE existing.Reason = N'IDENT import'
              AND existing.ValidToUtc IS NULL
              AND NOT EXISTS
              (
                  SELECT 1 FROM #RoleStage AS source
                  WHERE source.UserAccountId = existing.UserAccountId
                    AND source.RoleCode = existing.RoleCode
              );

            MERGE security.UserRoleAssignments AS target
            USING #RoleStage AS source
                ON target.UserAccountId = source.UserAccountId
               AND target.RoleCode = source.RoleCode
               AND target.Reason = N'IDENT import'
            WHEN MATCHED THEN UPDATE SET ValidToUtc = NULL
            WHEN NOT MATCHED THEN INSERT
                (Id, UserAccountId, RoleCode, ValidFromUtc, ValidToUtc, GrantedByUserAccountId, Reason)
                VALUES (source.Id, source.UserAccountId, source.RoleCode, source.ValidFromUtc, NULL, source.GrantedByUserAccountId, N'IDENT import');

            MERGE integration.ExternalIdentifiers AS target
            USING #ExternalStage AS source
                ON target.SystemCode = source.SystemCode
               AND target.EntityType = source.EntityType
               AND target.ExternalId = source.ExternalId
            WHEN MATCHED THEN UPDATE SET
                InternalEntityId = source.InternalEntityId,
                ImportedAtUtc = source.ImportedAtUtc
            WHEN NOT MATCHED THEN INSERT
                (Id, SystemCode, EntityType, ExternalId, InternalEntityId, ImportedAtUtc)
                VALUES (source.Id, source.SystemCode, source.EntityType, source.ExternalId, source.InternalEntityId, source.ImportedAtUtc);
            """, cancellationToken);
    }

    private static async Task NormalizePatientsAsync(
        SqlConnection connection,
        IReadOnlyCollection<LegacyPatient> patients,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, """
            CREATE TABLE #PatientStage
            (
                Id uniqueidentifier NOT NULL,
                LegacyId int NOT NULL,
                CardNumber nvarchar(64) NOT NULL,
                FullName nvarchar(300) NOT NULL,
                BirthDate date NULL
            );
            CREATE TABLE #PatientExternalStage
            (
                Id uniqueidentifier NOT NULL,
                SystemCode nvarchar(40) NOT NULL,
                EntityType nvarchar(80) NOT NULL,
                ExternalId nvarchar(160) NOT NULL,
                InternalEntityId uniqueidentifier NOT NULL,
                ImportedAtUtc datetimeoffset NOT NULL
            );
            """, cancellationToken);

        var patientTable = CreateTable(("Id", typeof(Guid)), ("LegacyId", typeof(int)), ("CardNumber", typeof(string)),
            ("FullName", typeof(string)), ("BirthDate", typeof(DateTime)));
        var externalTable = CreateTable(("Id", typeof(Guid)), ("SystemCode", typeof(string)), ("EntityType", typeof(string)),
            ("ExternalId", typeof(string)), ("InternalEntityId", typeof(Guid)), ("ImportedAtUtc", typeof(DateTimeOffset)));

        foreach (var item in patients)
        {
            var patientId = StableGuid("Patient", item.LegacyId.ToString(CultureInfo.InvariantCulture));
            patientTable.Rows.Add(patientId, item.LegacyId, item.CardNumber, item.FullName, item.BirthDate is null ? (object)DBNull.Value : item.BirthDate.Value.Date);
            AddExternalRow(externalTable, "Patient", item.LegacyId, patientId, now);
        }

        await BulkCopyAsync(connection, "#PatientStage", patientTable, cancellationToken);
        await BulkCopyAsync(connection, "#PatientExternalStage", externalTable, cancellationToken);

        await ExecuteAsync(connection, """
            MERGE dbo.Patients AS target
            USING #PatientStage AS source ON target.Id = source.Id
            WHEN MATCHED THEN UPDATE SET
                CardNumber = source.CardNumber,
                FullName = source.FullName,
                BirthDate = source.BirthDate
            WHEN NOT MATCHED THEN INSERT (Id, CardNumber, FullName, BirthDate)
                VALUES (source.Id, source.CardNumber, source.FullName, source.BirthDate);

            MERGE integration.ExternalIdentifiers AS target
            USING #PatientExternalStage AS source
                ON target.SystemCode = source.SystemCode
               AND target.EntityType = source.EntityType
               AND target.ExternalId = source.ExternalId
            WHEN MATCHED THEN UPDATE SET
                InternalEntityId = source.InternalEntityId,
                ImportedAtUtc = source.ImportedAtUtc
            WHEN NOT MATCHED THEN INSERT
                (Id, SystemCode, EntityType, ExternalId, InternalEntityId, ImportedAtUtc)
                VALUES (source.Id, source.SystemCode, source.EntityType, source.ExternalId, source.InternalEntityId, source.ImportedAtUtc);
            """, cancellationToken);
    }

    private static async Task NormalizeAppointmentsAsync(
        SqlConnection connection,
        IReadOnlyCollection<LegacyAppointment> appointments,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, """
            CREATE TABLE #AppointmentStage
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
            CREATE TABLE #AppointmentExternalStage
            (
                Id uniqueidentifier NOT NULL,
                SystemCode nvarchar(40) NOT NULL,
                EntityType nvarchar(80) NOT NULL,
                ExternalId nvarchar(160) NOT NULL,
                InternalEntityId uniqueidentifier NOT NULL,
                ImportedAtUtc datetimeoffset NOT NULL
            );
            """, cancellationToken);

        var appointmentTable = CreateTable(("Id", typeof(Guid)), ("LegacyId", typeof(int)), ("PatientId", typeof(Guid)),
            ("StaffProfileId", typeof(Guid)), ("StartLocal", typeof(DateTime)), ("EndLocal", typeof(DateTime)),
            ("StatusCode", typeof(string)), ("LegacyRoomId", typeof(int)), ("ImportedAtUtc", typeof(DateTimeOffset)));
        var externalTable = CreateTable(("Id", typeof(Guid)), ("SystemCode", typeof(string)), ("EntityType", typeof(string)),
            ("ExternalId", typeof(string)), ("InternalEntityId", typeof(Guid)), ("ImportedAtUtc", typeof(DateTimeOffset)));

        foreach (var item in appointments)
        {
            var appointmentId = StableGuid("Appointment", item.LegacyReceptionId.ToString(CultureInfo.InvariantCulture));
            var patientId = StableGuid("Patient", item.LegacyPatientId.ToString(CultureInfo.InvariantCulture));
            object staffId = item.LegacyStaffId is null
                ? DBNull.Value
                : StableGuid("StaffProfile", item.LegacyStaffId.Value.ToString(CultureInfo.InvariantCulture));

            appointmentTable.Rows.Add(
                appointmentId,
                item.LegacyReceptionId,
                patientId,
                staffId,
                item.StartLocal,
                item.EndLocal,
                item.StatusCode,
                item.LegacyRoomId is null ? (object)DBNull.Value : item.LegacyRoomId.Value,
                now);

            AddExternalRow(externalTable, "Appointment", item.LegacyReceptionId, appointmentId, now);
        }

        await BulkCopyAsync(connection, "#AppointmentStage", appointmentTable, cancellationToken);
        await BulkCopyAsync(connection, "#AppointmentExternalStage", externalTable, cancellationToken);

        await ExecuteAsync(connection, """
            MERGE scheduling.Appointments AS target
            USING #AppointmentStage AS source ON target.Id = source.Id
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
                VALUES (source.Id, source.PatientId, source.StaffProfileId, source.StartLocal, source.EndLocal, source.StatusCode, source.LegacyRoomId, source.ImportedAtUtc);

            MERGE integration.ExternalIdentifiers AS target
            USING #AppointmentExternalStage AS source
                ON target.SystemCode = source.SystemCode
               AND target.EntityType = source.EntityType
               AND target.ExternalId = source.ExternalId
            WHEN MATCHED THEN UPDATE SET
                InternalEntityId = source.InternalEntityId,
                ImportedAtUtc = source.ImportedAtUtc
            WHEN NOT MATCHED THEN INSERT
                (Id, SystemCode, EntityType, ExternalId, InternalEntityId, ImportedAtUtc)
                VALUES (source.Id, source.SystemCode, source.EntityType, source.ExternalId, source.InternalEntityId, source.ImportedAtUtc);
            """, cancellationToken);
    }

    private static DataTable CreateTable(params (string Name, Type Type)[] columns)
    {
        var table = new DataTable { Locale = CultureInfo.InvariantCulture };
        foreach (var column in columns)
            table.Columns.Add(column.Name, column.Type);
        return table;
    }

    private static void AddExternalRow(DataTable table, string entityType, int legacyId, Guid internalId, DateTimeOffset now)
    {
        var externalId = legacyId.ToString(CultureInfo.InvariantCulture);
        table.Rows.Add(
            StableGuid("ExternalIdentifier", $"{LegacySystemCode}:{entityType}:{externalId}"),
            LegacySystemCode,
            entityType,
            externalId,
            internalId,
            now);
    }

    private static async Task BulkCopyAsync(SqlConnection connection, string destinationTable, DataTable table, CancellationToken cancellationToken)
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

    private sealed record LegacyStaff(int LegacyId, string DisplayName, bool IsActive);
    private sealed record LegacyRole(int LegacyStaffId, string RoleCode);
    private sealed record LegacyPatient(int LegacyId, string CardNumber, string FullName, DateTime? BirthDate);
    private sealed record LegacyAppointment(
        int LegacyReceptionId,
        int LegacyPatientId,
        int? LegacyStaffId,
        DateTime StartLocal,
        DateTime EndLocal,
        string StatusCode,
        int? LegacyRoomId);
}
