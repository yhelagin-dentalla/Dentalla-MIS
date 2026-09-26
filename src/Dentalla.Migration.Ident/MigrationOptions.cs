using Microsoft.Data.SqlClient;

namespace Dentalla.Migration.Ident;

internal sealed record MigrationOptions(
    string Command,
    string SourceConnectionString,
    string TargetConnectionString,
    bool ReplaceExisting,
    int? ChiefMedicalOfficerLegacyStaffId)
{
    public static MigrationOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault(x => !x.StartsWith("--", StringComparison.Ordinal))?.Trim().ToLowerInvariant()
                      ?? "inventory";

        var sourceServer = ReadValue(args, "--source-server") ?? "localhost";
        var sourceDatabase = ReadValue(args, "--source-database") ?? "PZ_TEST";
        var targetServer = ReadValue(args, "--target-server") ?? "localhost";
        var targetDatabase = ReadValue(args, "--target-database") ?? "Dentalla";

        var source = new SqlConnectionStringBuilder
        {
            DataSource = sourceServer,
            InitialCatalog = sourceDatabase,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ApplicationName = "Dentalla.Migration.Ident.Source",
            ConnectTimeout = 30
        }.ConnectionString;

        var target = new SqlConnectionStringBuilder
        {
            DataSource = targetServer,
            InitialCatalog = targetDatabase,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ApplicationName = "Dentalla.Migration.Ident.Target",
            ConnectTimeout = 30
        }.ConnectionString;

        return new MigrationOptions(
            command,
            source,
            target,
            args.Any(x => string.Equals(x, "--replace", StringComparison.OrdinalIgnoreCase)),
            ReadIntValue(args, "--chief-medical-officer-id") ?? 1);
    }

    public void Validate()
    {
        var source = new SqlConnectionStringBuilder(SourceConnectionString);
        var target = new SqlConnectionStringBuilder(TargetConnectionString);

        if (string.Equals(source.InitialCatalog, target.InitialCatalog, StringComparison.OrdinalIgnoreCase)
            && string.Equals(NormalizeServer(source.DataSource), NormalizeServer(target.DataSource), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source и target не могут указывать на одну и ту же БД.");
        }

        if (!string.Equals(NormalizeServer(source.DataSource), NormalizeServer(target.DataSource), StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "Текущий full raw snapshot рассчитан на PZ_TEST и Dentalla на одном SQL Server instance. " +
                "Для переноса между разными серверами будет добавлен bulk-copy transport.");
        }

        if (Command is not ("inventory" or "snapshot" or "verify" or "normalize-core"))
            throw new ArgumentException($"Неизвестная команда '{Command}'. Допустимо: inventory, snapshot, verify, normalize-core.");
    }

    private static string? ReadValue(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }


    private static int? ReadIntValue(string[] args, string key)
    {
        var value = ReadValue(args, key);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"Параметр {key} должен быть целым числом.");
    }

    private static string NormalizeServer(string value)
        => value.Trim().TrimEnd('\\').Replace("(local)", "localhost", StringComparison.OrdinalIgnoreCase);
}
