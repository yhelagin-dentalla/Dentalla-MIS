using Dentalla.Application.Abstractions;
using Dentalla.Application.Audit;
using Dentalla.Application.Clinical;
using Dentalla.Application.Security;
using Dentalla.Contracts.Clinical;
using Dentalla.Domain.Clinical;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Clinical;

public sealed class ClinicalCommands(
    DentallaDbContext db,
    IEffectivePermissionService permissions,
    IServerClock clock,
    IAuditWriter audit) : IClinicalCommands
{
    public async Task<EncounterWorkspaceDto> StartEncounterAsync(Guid userAccountId, Guid patientId, StartEncounterRequest request, CancellationToken cancellationToken = default)
    {
        await RequireAsync(userAccountId, "Clinical.Note.Edit", cancellationToken);

        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == request.AppointmentId && x.PatientId == patientId, cancellationToken)
            ?? throw new ArgumentException("Appointment for this patient was not found.");

        var existing = await db.Encounters.AsNoTracking().SingleOrDefaultAsync(x => x.AppointmentId == appointment.Id, cancellationToken);
        if (existing is not null)
            return await LoadDtoAsync(existing.Id, cancellationToken) ?? throw new InvalidOperationException("Existing encounter cannot be loaded.");

        var actor = await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userAccountId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        var responsibleStaffId = request.ResponsibleStaffProfileId ?? appointment.StaffProfileId ?? actor.StaffProfileId;
        if (responsibleStaffId == Guid.Empty)
            throw new InvalidOperationException("Responsible clinician must be selected before starting the encounter.");

        var staffExists = await db.StaffProfiles.AsNoTracking().AnyAsync(x => x.Id == responsibleStaffId, cancellationToken);
        if (!staffExists)
            throw new ArgumentException("Responsible clinician was not found.");

        var startedLocal = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var encounter = Encounter.Open(Guid.NewGuid(), patientId, appointment.Id, responsibleStaffId, userAccountId, startedLocal);
        var note = new ClinicalNote(Guid.NewGuid(), encounter.Id, patientId, responsibleStaffId, clock.UtcNow);

        db.Encounters.Add(encounter);
        db.ClinicalNotes.Add(note);
        if (appointment.StatusCode is "Scheduled" or "Confirmed")
            appointment.MarkArrived();

        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Clinical.EncounterStarted", "Начат приём пациента.", userAccountId, permissionCode: "Clinical.Note.Edit", entityType: "Encounter", entityId: encounter.Id.ToString("D"), cancellationToken: cancellationToken);
        return ToDto(encounter, note);
    }

    public async Task<EncounterWorkspaceDto?> GetEncounterAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default)
    {
        await RequireAsync(userAccountId, "Clinical.Record.View", cancellationToken);
        return await LoadDtoAsync(encounterId, cancellationToken);
    }

    public async Task<EncounterWorkspaceDto?> UpdateNoteAsync(Guid userAccountId, Guid encounterId, UpdateClinicalNoteRequest request, CancellationToken cancellationToken = default)
    {
        await RequireAsync(userAccountId, "Clinical.Note.Edit", cancellationToken);
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken);
        if (encounter is null) return null;
        if (encounter.StatusCode != EncounterStatusCode.Open) throw new InvalidOperationException("Clinical note can be edited only while the encounter is open.");

        var note = await db.ClinicalNotes.SingleAsync(x => x.EncounterId == encounterId, cancellationToken);
        note.UpdateText(request.Text, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Clinical.NoteUpdated", "Обновлён дневник приёма.", userAccountId, permissionCode: "Clinical.Note.Edit", entityType: "ClinicalNote", entityId: note.Id.ToString("D"), cancellationToken: cancellationToken);
        return ToDto(encounter, note);
    }

    public async Task<EncounterWorkspaceDto?> SignNoteAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default)
    {
        await RequireAsync(userAccountId, "Clinical.Note.Sign", cancellationToken);
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken);
        if (encounter is null) return null;
        var note = await db.ClinicalNotes.SingleAsync(x => x.EncounterId == encounterId, cancellationToken);
        note.Sign(userAccountId, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Clinical.NoteSigned", "Подписан дневник приёма.", userAccountId, permissionCode: "Clinical.Note.Sign", entityType: "ClinicalNote", entityId: note.Id.ToString("D"), cancellationToken: cancellationToken);
        return ToDto(encounter, note);
    }

    public async Task<EncounterWorkspaceDto?> CompleteEncounterAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default)
    {
        await RequireAsync(userAccountId, "Clinical.Note.Edit", cancellationToken);
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken);
        if (encounter is null) return null;
        var note = await db.ClinicalNotes.SingleAsync(x => x.EncounterId == encounterId, cancellationToken);
        if (note.StatusCode != ClinicalNoteStatusCode.Signed)
            throw new InvalidOperationException("Sign the clinical note before completing the encounter.");

        encounter.Complete(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified));
        var appointment = await db.Appointments.SingleAsync(x => x.Id == encounter.AppointmentId, cancellationToken);
        appointment.Complete();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Clinical.EncounterCompleted", "Приём завершён.", userAccountId, permissionCode: "Clinical.Note.Edit", entityType: "Encounter", entityId: encounter.Id.ToString("D"), cancellationToken: cancellationToken);
        return ToDto(encounter, note);
    }

    private async Task RequireAsync(Guid userAccountId, string permissionCode, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(userAccountId, permissionCode, ct))
            throw new UnauthorizedAccessException();
    }

    private async Task<EncounterWorkspaceDto?> LoadDtoAsync(Guid encounterId, CancellationToken ct)
    {
        var encounter = await db.Encounters.AsNoTracking().SingleOrDefaultAsync(x => x.Id == encounterId, ct);
        if (encounter is null) return null;
        var note = await db.ClinicalNotes.AsNoTracking().SingleOrDefaultAsync(x => x.EncounterId == encounterId, ct);
        if (note is null) return null;
        return ToDto(encounter, note);
    }

    private static EncounterWorkspaceDto ToDto(Encounter encounter, ClinicalNote note)
        => new(encounter.Id, encounter.PatientId, encounter.AppointmentId, encounter.StaffProfileId, encounter.StartedLocal, encounter.EndedLocal, encounter.StatusCode,
            new ClinicalNoteDto(note.Id, note.EncounterId, note.Text, note.StatusCode, note.Version, note.UpdatedAtUtc, note.SignedAtUtc));
}
