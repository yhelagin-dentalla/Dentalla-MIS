using System.Net;
using System.Text.Json;
using Dentalla.Application.Abstractions;
using Dentalla.Application.Audit;
using Dentalla.Application.Security;
using Dentalla.Contracts.Security;
using Dentalla.Domain.Security;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Api.Endpoints;

public static class RoleContextEndpoints
{
    private const string SwitchPermissionCode = "RBAC.SwitchRoleContext";

    public static IEndpointRouteBuilder MapRoleContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/dev-role-context-switch", async (
            HttpContext context,
            DevRoleContextSwitchRequest request,
            DentallaDbContext db,
            IAuditWriter audit,
            IEffectivePermissionService permissions,
            IServerClock clock,
            CancellationToken ct) =>
        {
            // The current desktop login is deliberately passwordless and loopback-only.
            // This endpoint is therefore development-only until the desktop is wired to
            // the authenticated bearer session created by /api/auth/login.
            if (!IsLoopback(context.Connection.RemoteIpAddress))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            if (request.StaffProfileId == Guid.Empty)
                return Results.BadRequest(new { error = "StaffProfileId is required." });

            if (!SystemRoleCode.All.Contains(request.ToRoleCode))
                return Results.BadRequest(new { error = $"Unknown role context '{request.ToRoleCode}'." });

            var actor = await (
                from user in db.UserAccounts.AsNoTracking()
                join staff in db.StaffProfiles.AsNoTracking() on user.StaffProfileId equals staff.Id
                where user.StaffProfileId == request.StaffProfileId && user.IsActive && staff.IsActive
                select new
                {
                    UserAccountId = user.Id,
                    StaffProfileId = staff.Id,
                    staff.DisplayName
                })
                .SingleOrDefaultAsync(ct);

            if (actor is null)
                return Results.NotFound(new { error = "Active Dentalla user for this staff profile was not found." });

            var now = clock.UtcNow;
            var isDirector = await db.UserRoleAssignments.AsNoTracking().AnyAsync(x =>
                x.UserAccountId == actor.UserAccountId
                && x.RoleCode == SystemRoleCode.Director
                && x.ValidFromUtc <= now
                && (x.ValidToUtc == null || x.ValidToUtc > now), ct);

            var canSwitch = isDirector
                            && await permissions.HasPermissionAsync(actor.UserAccountId, SwitchPermissionCode, ct);

            if (!canSwitch)
            {
                await audit.WriteAsync(
                    "Security.RoleContextSwitchDenied",
                    $"Role-context switch denied for {actor.DisplayName}: Director assignment or {SwitchPermissionCode} is not active.",
                    actorUserAccountId: actor.UserAccountId,
                    permissionCode: SwitchPermissionCode,
                    entityType: "StaffProfile",
                    entityId: actor.StaffProfileId.ToString("D"),
                    roleContext: request.FromRoleCode,
                    dataJson: JsonSerializer.Serialize(new { request.FromRoleCode, request.ToRoleCode }),
                    traceId: context.TraceIdentifier,
                    clientIp: context.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken: ct);

                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            await audit.WriteAsync(
                "Security.RoleContextChanged",
                $"{actor.DisplayName}: role context {request.FromRoleCode} -> {request.ToRoleCode}.",
                actorUserAccountId: actor.UserAccountId,
                permissionCode: SwitchPermissionCode,
                entityType: "StaffProfile",
                entityId: actor.StaffProfileId.ToString("D"),
                roleContext: request.ToRoleCode,
                dataJson: JsonSerializer.Serialize(new
                {
                    FromRoleCode = request.FromRoleCode,
                    ToRoleCode = request.ToRoleCode,
                    IdentityRole = SystemRoleCode.Director,
                    IsImpersonation = false
                }),
                traceId: context.TraceIdentifier,
                clientIp: context.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: ct);

            return Results.Ok(new RoleContextSwitchResponse(
                true,
                request.ToRoleCode,
                "Рабочий контекст переключён. Identity остаётся Director; профессиональные клинические допуски проверяются отдельно."));
        });

        return endpoints;
    }

    private static bool IsLoopback(IPAddress? address)
        => address is not null
           && (IPAddress.IsLoopback(address)
               || address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()));
}
