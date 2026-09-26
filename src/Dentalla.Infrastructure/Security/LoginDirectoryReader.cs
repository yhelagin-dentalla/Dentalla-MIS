using Dentalla.Application.Security;
using Dentalla.Contracts.Security;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Security;

public sealed class LoginDirectoryReader(DentallaDbContext db) : ILoginDirectoryReader
{
    private static readonly LoginRoleDto[] Roles =
    [
        new("Doctor", "Врач", "Клинический приём и медицинская карта"),
        new("Administrator", "Администратор", "Расписание, пациенты и расчёты"),
        new("ChiefMedicalOfficer", "Главный врач", "Качество, экспертиза и контроль"),
        new("Marketer", "Маркетолог", "Обращения, коммуникации и аналитика"),
        new("Director", "Директор", "Управление клиникой и настройки")
    ];

    public async Task<LoginDirectoryDto> ReadAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var rows = await (
            from user in db.UserAccounts.AsNoTracking()
            join staff in db.StaffProfiles.AsNoTracking() on user.StaffProfileId equals staff.Id
            join assignment in db.UserRoleAssignments.AsNoTracking() on user.Id equals assignment.UserAccountId
            where user.IsActive
                  && staff.IsActive
                  && assignment.ValidFromUtc <= now
                  && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
            select new
            {
                StaffId = staff.Id,
                staff.DisplayName,
                assignment.RoleCode
            })
            .ToListAsync(cancellationToken);

        var knownRoleCodes = Roles.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        var employees = rows
            .Where(x => knownRoleCodes.Contains(x.RoleCode))
            .GroupBy(x => new { x.StaffId, x.DisplayName })
            .Select(g => new LoginEmployeeDto(
                g.Key.StaffId.ToString(),
                g.Key.DisplayName,
                g.Select(x => x.RoleCode)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new LoginDirectoryDto(
            employees.Length > 0,
            "Dentalla.staff/security",
            employees.Length == 0
                ? "Нормализованный справочник входа пуст. Выполните Dentalla.Migration.Ident normalize-core."
                : $"Нормализованный справочник Dentalla: {employees.Length} сотрудников.",
            Roles,
            employees);
    }
}
