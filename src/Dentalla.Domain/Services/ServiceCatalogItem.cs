namespace Dentalla.Domain.Services;

public sealed class ServiceCatalogItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? GroupName { get; set; }
    public bool IsHistorical { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
