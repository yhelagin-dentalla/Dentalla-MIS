using Dentalla.Application.Abstractions;
using Dentalla.Application.Security;
using Dentalla.Infrastructure.Persistence;
using Dentalla.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Security;

public sealed class EffectivePermissionService(
    DentallaDbContext db,
    IServerClock clock) : IEffectivePermissionService
{
    public async Task<IReadOnlyList<EffectivePermissionResult>> ResolveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var user = await db.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userAccountId && x.IsActive, cancellationToken);
        if (user is null)
            return [];

        var definitions = await db.PermissionDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Area)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var activeRoles = await db.UserRoleAssignments
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId
                && x.ValidFromUtc <= now
                && (x.ValidToUtc == null || x.ValidToUtc > now))
            .Select(x => x.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<RolePermission> rolePermissions;
        if (activeRoles.Count == 0)
        {
            rolePermissions = [];
        }
        else
        {
            rolePermissions = await db.RolePermissions
                .AsNoTracking()
                .Where(x => activeRoles.Contains(x.RoleCode))
                .ToListAsync(cancellationToken);
        }

        var overrides = await db.UserPermissionOverrides
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId
                && x.ValidFromUtc <= now
                && (x.ValidToUtc == null || x.ValidToUtc > now))
            .OrderByDescending(x => x.ValidFromUtc)
            .ToListAsync(cancellationToken);

        var delegations = await db.DelegationGrants
            .AsNoTracking()
            .Where(x => x.GrantedToUserAccountId == userAccountId
                && x.RevokedAtUtc == null
                && x.ValidFromUtc <= now
                && x.ValidToUtc > now)
            .OrderByDescending(x => x.ValidFromUtc)
            .ToListAsync(cancellationToken);

        var results = new List<EffectivePermissionResult>(definitions.Count);

        foreach (var definition in definitions)
        {
            var matchingOverrides = overrides
                .Where(x => x.PermissionCode == definition.Code)
                .ToList();

            var explicitDeny = matchingOverrides.FirstOrDefault(x => !x.IsAllowed);
            if (explicitDeny is not null)
            {
                results.Add(Create(definition, false, "UserDeny", explicitDeny.ScopeType, explicitDeny.ScopeValue, explicitDeny.LimitAmount));
                continue;
            }

            var explicitAllow = matchingOverrides.FirstOrDefault(x => x.IsAllowed);
            if (explicitAllow is not null)
            {
                results.Add(Create(definition, true, "UserAllow", explicitAllow.ScopeType, explicitAllow.ScopeValue, explicitAllow.LimitAmount));
                continue;
            }

            var delegation = delegations.FirstOrDefault(x => x.PermissionCode == definition.Code);
            if (delegation is not null)
            {
                results.Add(Create(definition, true, "Delegation", delegation.ScopeType, delegation.ScopeValue, delegation.LimitAmount));
                continue;
            }

            var roleSources = rolePermissions
                .Where(x => x.PermissionCode == definition.Code && x.IsAllowed)
                .Select(x => x.RoleCode)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x)
                .ToArray();

            if (roleSources.Length > 0)
            {
                results.Add(Create(definition, true, $"Role:{string.Join(',', roleSources)}", "Clinic", null, null));
                continue;
            }

            results.Add(Create(definition, false, "DefaultDeny", "Clinic", null, null));
        }

        return results;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userAccountId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permissions = await ResolveAsync(userAccountId, cancellationToken);
        return permissions.Any(x => x.PermissionCode == permissionCode && x.IsAllowed);
    }

    private static EffectivePermissionResult Create(
        PermissionDefinition definition,
        bool isAllowed,
        string source,
        string scopeType,
        string? scopeValue,
        decimal? limitAmount)
        => new(
            definition.Code,
            definition.Name,
            definition.Area,
            isAllowed,
            source,
            scopeType,
            scopeValue,
            limitAmount,
            definition.IsSensitive,
            definition.IsClinicalPrivilegeBound);
}
