namespace Dentalla.Domain.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorUserAccountId { get; private set; }
    public Guid? ActorSessionId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string? PermissionCode { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? RoleContext { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? DataJson { get; private set; }
    public string? TraceId { get; private set; }
    public string? ClientIp { get; private set; }

    private AuditEvent() { }

    public AuditEvent(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserAccountId,
        Guid? actorSessionId,
        string eventType,
        string description,
        string? permissionCode = null,
        string? entityType = null,
        string? entityId = null,
        string? roleContext = null,
        string? dataJson = null,
        string? traceId = null,
        string? clientIp = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Audit event id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("Audit event type is required.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Audit event description is required.", nameof(description));

        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActorUserAccountId = actorUserAccountId;
        ActorSessionId = actorSessionId;
        EventType = eventType.Trim();
        PermissionCode = string.IsNullOrWhiteSpace(permissionCode) ? null : permissionCode.Trim();
        EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();
        EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
        RoleContext = string.IsNullOrWhiteSpace(roleContext) ? null : roleContext.Trim();
        Description = description.Trim();
        DataJson = string.IsNullOrWhiteSpace(dataJson) ? null : dataJson;
        TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim();
        ClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp.Trim();
    }
}
