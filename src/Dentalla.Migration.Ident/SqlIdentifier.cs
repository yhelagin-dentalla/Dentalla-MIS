namespace Dentalla.Migration.Ident;

internal static class SqlIdentifier
{
    public static string Quote(string value)
        => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";

    public static string RawTableName(string sourceSchema, string sourceTable)
        => string.Equals(sourceSchema, "dbo", StringComparison.OrdinalIgnoreCase)
            ? sourceTable
            : $"{sourceSchema}__{sourceTable}";
}
