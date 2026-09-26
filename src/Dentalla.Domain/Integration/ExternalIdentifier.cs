namespace Dentalla.Domain.Integration;

public sealed class ExternalIdentifier
{
    public Guid Id { get; private set; }
    public string SystemCode { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public Guid InternalEntityId { get; private set; }
    public DateTimeOffset ImportedAtUtc { get; private set; }

    private ExternalIdentifier() { }

    public ExternalIdentifier(
        Guid id,
        string systemCode,
        string entityType,
        string externalId,
        Guid internalEntityId,
        DateTimeOffset importedAtUtc)
    {
        Id = id;
        SystemCode = systemCode;
        EntityType = entityType;
        ExternalId = externalId;
        InternalEntityId = internalEntityId;
        ImportedAtUtc = importedAtUtc;
    }
}
