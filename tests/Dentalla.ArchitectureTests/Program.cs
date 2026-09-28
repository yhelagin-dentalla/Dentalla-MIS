using System.Text.RegularExpressions;

var failures = new List<string>();
var repo = FindRepositoryRoot();

// Historical exception list: these scripts predate the single-schema-owner rule and are frozen evidence.
var grandfatheredCanonicalDdl = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "greis_phase4b_import_clinical_services.sql",
    "greis_phase5b_import_treatment_courses.sql",
    "greis_phase6b_import_actual_receipts.sql"
};

var migrationDir = Path.Combine(repo, "scripts", "migration");
var ddlPattern = new Regex(@"\b(create|alter|drop)\s+(table|schema|index|constraint)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

foreach (var file in Directory.EnumerateFiles(migrationDir, "*.sql", SearchOption.AllDirectories))
{
    var name = Path.GetFileName(file);
    var sql = File.ReadAllText(file);
    if (ddlPattern.IsMatch(sql) && !grandfatheredCanonicalDdl.Contains(name))
    {
        failures.Add($"Canonical DDL detected in non-grandfathered import script: {Path.GetRelativePath(repo, file)}. Schema belongs to EF migrations.");
    }
}

var verificationPath = Path.Combine(repo, "scripts", "verification", "verify_greis_invariants.sql");
if (!File.Exists(verificationPath))
{
    failures.Add("Missing scripts/verification/verify_greis_invariants.sql.");
}
else
{
    var verification = File.ReadAllText(verificationPath);
    var requiredTokens = new[]
    {
        "15836",
        "30799",
        "92524139.0000",
        "27",
        "35",
        "5",
        "2387",
        "93880212.5000",
        "GREIS_ACTUAL_RECEIPTS_V1"
    };

    foreach (var token in requiredTokens)
    {
        if (!verification.Contains(token, StringComparison.Ordinal))
            failures.Add($"GREIS reconciliation invariant is missing: {token}");
    }
}

var remediationPath = Path.Combine(repo, "docs", "ARCHITECTURE-REMEDIATION.md");
if (!File.Exists(remediationPath))
    failures.Add("Missing architecture remediation policy document.");

if (failures.Count > 0)
{
    Console.Error.WriteLine("Dentalla architecture gate FAILED:");
    foreach (var failure in failures)
        Console.Error.WriteLine($" - {failure}");
    return 1;
}

Console.WriteLine("Dentalla architecture gate passed.");
Console.WriteLine(" - no new canonical DDL in migration import scripts");
Console.WriteLine(" - GREIS executable reconciliation invariants are present");
return 0;

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Dentalla.sln")))
            return current.FullName;
        current = current.Parent;
    }

    current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Dentalla.sln")))
            return current.FullName;
        current = current.Parent;
    }

    throw new InvalidOperationException("Dentalla repository root was not found.");
}
