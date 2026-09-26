using Dentalla.Domain.Security;
using Dentalla.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dentalla.Infrastructure.Persistence;

public sealed class ReferenceDataSeeder(
    DentallaDbContext db,
    ILogger<ReferenceDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // PermissionDefinition is application-owned reference data. Metadata may be
        // refreshed from code, but identifiers never change silently.
        var existingDefinitions = await db.PermissionDefinitions
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken);

        foreach (var source in PermissionCatalog.Definitions)
        {
            if (!existingDefinitions.TryGetValue(source.Code, out var existing))
            {
                db.PermissionDefinitions.Add(new PermissionDefinition(
                    source.Code,
                    source.Area,
                    source.Name,
                    source.IsSensitive,
                    source.IsClinicalPrivilegeBound));
                continue;
            }

            // Reference metadata is intentionally not made user-editable in v1.
            db.Entry(existing).CurrentValues.SetValues(source);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Role defaults are only inserted when missing. Director may later change
        // a role profile in the application; startup must never overwrite that choice.
        var existingRolePermissions = await db.RolePermissions
            .Select(x => new { x.RoleCode, x.PermissionCode })
            .ToListAsync(cancellationToken);

        var existingKeys = existingRolePermissions
            .Select(x => $"{x.RoleCode}\u001f{x.PermissionCode}")
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var source in PermissionCatalog.RoleDefaults)
        {
            var key = $"{source.RoleCode}\u001f{source.PermissionCode}";
            if (existingKeys.Contains(key))
                continue;

            db.RolePermissions.Add(new RolePermission(
                source.RoleCode,
                source.PermissionCode,
                source.IsAllowed));
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dentalla security reference data ready. Permission definitions={DefinitionCount}; new role permissions={AddedRolePermissions}",
            PermissionCatalog.Definitions.Count,
            added);
    }
}
