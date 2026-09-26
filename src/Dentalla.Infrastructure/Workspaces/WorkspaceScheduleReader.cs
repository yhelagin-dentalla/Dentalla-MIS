using Dentalla.Application.Workspaces;
using Dentalla.Contracts.Workspaces;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Workspaces;

public sealed class WorkspaceScheduleReader(DentallaDbContext db) : IWorkspaceScheduleReader
{
    public async Task<DayScheduleDto> ReadDayAsync(
        DateOnly date,
        Guid? staffProfileId,
        CancellationToken cancellationToken = default)
    {
        var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var end = start.AddDays(1);

        var query =
            from appointment in db.Appointments.AsNoTracking()
            join patient in db.Patients.AsNoTracking() on appointment.PatientId equals patient.Id
            join staff in db.StaffProfiles.AsNoTracking() on appointment.StaffProfileId equals (Guid?)staff.Id into staffJoin
            from staff in staffJoin.DefaultIfEmpty()
            where appointment.StartLocal >= start && appointment.StartLocal < end
            select new
            {
                Appointment = appointment,
                Patient = patient,
                DoctorName = staff == null ? null : staff.DisplayName
            };

        if (staffProfileId is not null)
            query = query.Where(x => x.Appointment.StaffProfileId == staffProfileId);

        var rows = await query
            .OrderBy(x => x.Appointment.StartLocal)
            .ThenBy(x => x.DoctorName)
            .Select(x => new AppointmentListItemDto(
                x.Appointment.Id,
                x.Patient.Id,
                x.Patient.FullName,
                x.Patient.CardNumber,
                x.Appointment.StaffProfileId,
                x.DoctorName,
                x.Appointment.StartLocal,
                x.Appointment.EndLocal,
                x.Appointment.StatusCode,
                x.Appointment.LegacyRoomId))
            .ToListAsync(cancellationToken);

        var totalPatients = await db.Patients.AsNoTracking().CountAsync(cancellationToken);
        var activeStaff = await db.StaffProfiles.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);

        return new DayScheduleDto(date, totalPatients, activeStaff, rows);
    }
}
