using Dentalla.Application.Abstractions;
using Dentalla.Application.Audit;
using Dentalla.Domain.Audit;
using Dentalla.Infrastructure.Persistence;

namespace Dentalla.Infrastructure.Audit;

public sealed class AuditWriter(DentallaDbContext db, IServerClock clock) : IAuditWriter
{
    public async Task WriteAsync(
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
        CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            clock.UtcNow,
            actorUserAccountId,
            actorSessionId,
            eventType,
            description,
            permissionCode,
            entityType,
            entityId,
            roleContext,
            dataJson,
            traceId,
            clientIp));

        await db.SaveChangesAsync(cancellationToken);
    }
}
