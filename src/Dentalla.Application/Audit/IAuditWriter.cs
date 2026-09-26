namespace Dentalla.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(
        string eventType,
        string description,
        Guid? actorUserAccountId = null,
        Guid? actorSessionId = null,
        string? permissionCode = null,
        string? entityType = null,
        string? entityId = null,
        string? roleContext = null,
        string? dataJson = null,
        string? traceId = null,
        string? clientIp = null,
        CancellationToken cancellationToken = default);
}
