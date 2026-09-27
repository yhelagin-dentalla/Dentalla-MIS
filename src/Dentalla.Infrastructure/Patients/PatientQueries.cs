using Dentalla.Application.Patients;
using Dentalla.Contracts.Patients;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Patients;

public sealed class PatientQueries(DentallaDbContext db) : IPatientQueries
{
    public async Task<IReadOnlyList<PatientSearchItemDto>> SearchAsync(
        string? query,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var normalized = query?.Trim();

        var patients = db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            patients = patients.Where(x =>
                x.FullName.Contains(normalized) ||
                x.CardNumber.Contains(normalized));
        }

        return await patients
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.CardNumber)
            .Take(take)
            .Select(x => new PatientSearchItemDto(
                x.Id,
                x.CardNumber,
                x.FullName,
                x.BirthDate,
                db.Appointments
                    .Where(a => a.PatientId == x.Id)
                    .OrderByDescending(a => a.StartLocal)
                    .Select(a => (DateTime?)a.StartLocal)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientWorkspaceDto?> GetWorkspaceAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
            return null;

        var sourceIds = await db.ExternalIdentifiers
            .AsNoTracking()
            .Where(x => x.EntityType == "Patient" && x.InternalEntityId == patientId)
            .OrderBy(x => x.SystemCode)
            .ThenBy(x => x.ExternalId)
            .Select(x => new PatientSourceIdDto(x.SystemCode, x.ExternalId))
            .ToListAsync(cancellationToken);

        var appointments = await (
                from appointment in db.Appointments.AsNoTracking()
                join staff in db.StaffProfiles.AsNoTracking()
                    on appointment.StaffProfileId equals (Guid?)staff.Id into staffJoin
                from staff in staffJoin.DefaultIfEmpty()
                where appointment.PatientId == patientId
                orderby appointment.StartLocal descending
                select new AppointmentProjection(
                    appointment.Id,
                    appointment.StartLocal,
                    appointment.EndLocal,
                    appointment.StatusCode,
                    staff == null ? null : staff.DisplayName,
                    appointment.LegacyRoomId))
            .ToListAsync(cancellationToken);

        var encounters = await db.Encounters
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var encounterByAppointment = encounters
            .GroupBy(x => x.AppointmentId)
            .ToDictionary(x => x.Key, x => x.First());

        var serviceStats = await db.PerformedServices
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .GroupBy(x => x.EncounterId)
            .Select(x => new
            {
                EncounterId = x.Key,
                Count = x.Count(),
                Amount = x.Sum(s => s.FinalAmount)
            })
            .ToListAsync(cancellationToken);

        var serviceStatsByEncounter = serviceStats.ToDictionary(x => x.EncounterId);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var legacyDetails = await db.LegacyAppointmentDetails
            .AsNoTracking()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.SystemCode)
            .ThenBy(x => x.ExternalId)
            .ToListAsync(cancellationToken);

        var legacyCommentByAppointment = legacyDetails
            .Where(x => !string.IsNullOrWhiteSpace(x.LegacyComment))
            .GroupBy(x => x.AppointmentId)
            .ToDictionary(
                x => x.Key,
                x => string.Join(" • ", x.Select(d => d.LegacyComment!.Trim()).Distinct()));

        var visits = appointments.Select(appointment =>
        {
            encounterByAppointment.TryGetValue(appointment.Id, out var encounter);
            var count = 0;
            var amount = 0m;

            if (encounter is not null && serviceStatsByEncounter.TryGetValue(encounter.Id, out var stats))
            {
                count = stats.Count;
                amount = stats.Amount;
            }

            legacyCommentByAppointment.TryGetValue(appointment.Id, out var legacyComment);

            return new PatientVisitDto(
                appointment.Id,
                encounter?.Id,
                appointment.StartLocal,
                appointment.EndLocal,
                appointment.StatusCode,
                appointment.DoctorName,
                appointment.LegacyRoomId,
                legacyComment,
                count,
                amount);
        }).ToList();

        var services = await (
                from performed in db.PerformedServices.AsNoTracking()
                join encounter in db.Encounters.AsNoTracking()
                    on performed.EncounterId equals encounter.Id
                join item in db.ServiceCatalogItems.AsNoTracking()
                    on performed.ServiceCatalogItemId equals item.Id
                join staff in db.StaffProfiles.AsNoTracking()
                    on performed.StaffProfileId equals staff.Id into staffJoin
                from staff in staffJoin.DefaultIfEmpty()
                where performed.PatientId == patientId
                orderby encounter.StartedLocal descending, item.Name
                select new PatientServiceDto(
                    performed.Id,
                    performed.EncounterId,
                    item.Code,
                    item.Name,
                    performed.Quantity,
                    performed.Tooth,
                    performed.FinalAmount,
                    staff == null ? null : staff.DisplayName))
            .ToListAsync(cancellationToken);

        var treatmentCourses = await (
                from course in db.TreatmentCourses.AsNoTracking()
                join staff in db.StaffProfiles.AsNoTracking()
                    on course.OwnerStaffProfileId equals (Guid?)staff.Id into staffJoin
                from staff in staffJoin.DefaultIfEmpty()
                where course.PatientId == patientId
                orderby course.StartLocal descending, course.CreatedLocal descending
                select new PatientTreatmentCourseDto(
                    course.Id,
                    course.Name,
                    course.IsCompleted,
                    course.StartLocal,
                    course.EndLocal,
                    staff == null ? null : staff.DisplayName,
                    db.TreatmentCourseEncounters.Count(x => x.TreatmentCourseId == course.Id),
                    db.TreatmentCoursePlannedServices.Count(x => x.TreatmentCourseId == course.Id)))
            .ToListAsync(cancellationToken);

        var historicalReceipts = await db.PatientHistoricalReceiptTotals
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderBy(x => x.SourceSystem)
            .ThenBy(x => x.CalculationCode)
            .Select(x => new PatientHistoricalReceiptDto(
                x.SourceSystem,
                x.CalculationCode,
                x.PeriodStartLocal,
                x.PeriodEndLocal,
                x.Amount,
                x.CurrencyCode))
            .ToListAsync(cancellationToken);

        return new PatientWorkspaceDto(
            patient.Id,
            patient.CardNumber,
            patient.FullName,
            patient.BirthDate,
            sourceIds,
            visits,
            services,
            treatmentCourses,
            historicalReceipts,
            historicalReceipts.Sum(x => x.Amount));
    }

    private sealed record AppointmentProjection(
        Guid Id,
        DateTime StartLocal,
        DateTime EndLocal,
        string StatusCode,
        string? DoctorName,
        int? LegacyRoomId);
}
