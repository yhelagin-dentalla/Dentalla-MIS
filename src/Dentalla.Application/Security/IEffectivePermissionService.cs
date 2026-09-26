namespace Dentalla.Application.Security;

public interface IEffectivePermissionService
{
    Task<IReadOnlyList<EffectivePermissionResult>> ResolveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        Guid userAccountId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
