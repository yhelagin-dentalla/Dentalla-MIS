using System.Text.Json;
using Dentalla.Application.Abstractions;
using Dentalla.Application.Audit;
using Dentalla.Application.Scheduling;
using Dentalla.Application.Security;
using Dentalla.Contracts.Scheduling;
using Dentalla.Domain.Scheduling;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Scheduling;

public sealed class SchedulingCommands(
    DentallaDbContext db,
    IEffectivePermissionService permissions,
    IAuditWriter audit,
    IServerClock clock) : ISchedulingCommands
{
    public async Task<AppointmentEditorOptionsDto> GetEditorOptionsAsync(
        Guid actorUserAccountId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(actorUserAccountId, "Appointment.View", cancellationToken);
        var now = clock.UtcNow;

        var doctors = await (
            from staff in db.StaffProfiles.AsNoTracking()
            join user in db.UserAccounts.AsNoTracking() on staff.Id equals user.StaffProfileId
            join role in db.UserRoleAssignments.AsNoTracking() on user.Id equals role.UserAccountId
            where staff.IsActive
                  && user.IsActive
                  && role.RoleCode == "Doctor"
                  && role.ValidFromUtc <= now
                  && (role.ValidToUtc == null || role.ValidToUtc > now)
            orderby staff.DisplayName
            select new AppointmentStaffOptionDto(staff.Id, staff.DisplayName))
            .Distinct()
            .ToListAsync(cancellationToken);

        var rooms = await db.Appointments
            .AsNoTracking()
            .Where(x => x.LegacyRoomId != null)
            .Select(x => x.LegacyRoomId!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new AppointmentEditorOptionsDto(doctors, rooms);
    }

    public async Task<AppointmentCommandResultDto> CreateAsync(
        Guid actorUserAccountId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(actorUserAccountId, "Appointment.Manage", cancellationToken);
        ValidateRequest(request.PatientId, request.StaffProfileId, request.StartLocal, request.EndLocal);

        if (!await db.Patients.AnyAsync(x => x.Id == request.PatientId, cancellationToken))
            throw new InvalidOperationException("Patient does not exist.");
        if (!await db.StaffProfiles.AnyAsync(x => x.Id == request.StaffProfileId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Selected doctor is not active.");

        await EnsureSlotAvailableAsync(null, request.StaffProfileId, request.RoomId, request.StartLocal, request.EndLocal, cancellationToken);

        var appointment = new Appointment(
            Guid.NewGuid(),
            request.PatientId,
            request.StaffProfileId,
            request.StartLocal,
            request.EndLocal,
            "Scheduled",
            request.RoomId,
            clock.UtcNow);

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            "Appointment.Created",
            "Appointment created in Dentalla scheduling workspace.",
            actorUserAccountId,
            permissionCode: "Appointment.Manage",
            entityType: "Appointment",
            entityId: appointment.Id.ToString(),
            dataJson: JsonSerializer.Serialize(new
            {
                appointment.PatientId,
                appointment.StaffProfileId,
                appointment.StartLocal,
                appointment.EndLocal,
                RoomId = appointment.LegacyRoomId
            }),
            cancellationToken: cancellationToken);

        return ToDto(appointment);
    }

    public async Task<AppointmentCommandResultDto?> UpdateAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(actorUserAccountId, "Appointment.Manage", cancellationToken);
        ValidateRequest(Guid.NewGuid(), request.StaffProfileId, request.StartLocal, request.EndLocal);

        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
            return null;
        if (appointment.StatusCode is "Fulfilled" or "Arrived")
            throw new InvalidOperationException("A started or completed appointment cannot be rescheduled.");
        if (!await db.StaffProfiles.AnyAsync(x => x.Id == request.StaffProfileId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Selected doctor is not active.");

        await EnsureSlotAvailableAsync(appointmentId, request.StaffProfileId, request.RoomId, request.StartLocal, request.EndLocal, cancellationToken);
        appointment.Reschedule(request.StartLocal, request.EndLocal, request.StaffProfileId, request.RoomId);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            "Appointment.Rescheduled",
            "Appointment date, time, doctor or room changed.",
            actorUserAccountId,
            permissionCode: "Appointment.Manage",
            entityType: "Appointment",
            entityId: appointment.Id.ToString(),
            dataJson: JsonSerializer.Serialize(new
            {
                appointment.StaffProfileId,
                appointment.StartLocal,
                appointment.EndLocal,
                RoomId = appointment.LegacyRoomId
            }),
            cancellationToken: cancellationToken);

        return ToDto(appointment);
    }

    public async Task<AppointmentCommandResultDto?> ConfirmAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(actorUserAccountId, "Appointment.Manage", cancellationToken);
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
            return null;
        if (appointment.StatusCode == "Fulfilled")
            throw new InvalidOperationException("Completed appointment cannot be confirmed again.");

        appointment.Confirm();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            "Appointment.Confirmed",
            "Appointment confirmed.",
            actorUserAccountId,
            permissionCode: "Appointment.Manage",
            entityType: "Appointment",
            entityId: appointment.Id.ToString(),
            cancellationToken: cancellationToken);
        return ToDto(appointment);
    }

    public async Task<AppointmentCommandResultDto?> CancelAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(actorUserAccountId, "Appointment.Manage", cancellationToken);
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
            return null;
        if (appointment.StatusCode == "Fulfilled")
            throw new InvalidOperationException("Completed appointment cannot be cancelled.");

        appointment.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            "Appointment.Cancelled",
            "Appointment cancelled. Physical deletion is not used.",
            actorUserAccountId,
            permissionCode: "Appointment.Manage",
            entityType: "Appointment",
            entityId: appointment.Id.ToString(),
            cancellationToken: cancellationToken);
        return ToDto(appointment);
    }

    private async Task EnsurePermissionAsync(Guid actorUserAccountId, string permissionCode, CancellationToken cancellationToken)
    {
        if (!await permissions.HasPermissionAsync(actorUserAccountId, permissionCode, cancellationToken))
            throw new UnauthorizedAccessException($"Permission '{permissionCode}' is required.");
    }

    private async Task EnsureSlotAvailableAsync(
        Guid? appointmentId,
        Guid staffProfileId,
        int? roomId,
        DateTime startLocal,
        DateTime endLocal,
        CancellationToken cancellationToken)
    {
        var overlap = await db.Appointments.AsNoTracking().AnyAsync(x =>
            x.Id != appointmentId
            && x.StatusCode != "Cancelled"
            && x.StartLocal < endLocal
            && x.EndLocal > startLocal
            && (x.StaffProfileId == staffProfileId || (roomId != null && x.LegacyRoomId == roomId)),
            cancellationToken);

        if (overlap)
            throw new InvalidOperationException("Selected doctor or room is already occupied in this interval.");
    }

    private static void ValidateRequest(Guid patientId, Guid staffProfileId, DateTime startLocal, DateTime endLocal)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient is required.");
        if (staffProfileId == Guid.Empty)
            throw new ArgumentException("Doctor is required.");
        if (endLocal <= startLocal)
            throw new ArgumentException("End time must be later than start time.");
    }

    private static AppointmentCommandResultDto ToDto(Appointment appointment)
        => new(
            appointment.Id,
            appointment.PatientId,
            appointment.StaffProfileId,
            appointment.StartLocal,
            appointment.EndLocal,
            appointment.StatusCode,
            appointment.LegacyRoomId);
}
