namespace Dentalla.Domain.Clinical;

public static class EncounterStatusCode
{
    public const string Open = "Open";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public sealed class Encounter
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid StaffProfileId { get; private set; }
    public Guid? OpenedByUserAccountId { get; private set; }
    public DateTime StartedLocal { get; private set; }
    public DateTime? EndedLocal { get; private set; }
    public string StatusCode { get; private set; } = EncounterStatusCode.Open;
    public DateTimeOffset ImportedAtUtc { get; private set; }

    private Encounter() { }

    public static Encounter Open(
        Guid id,
        Guid patientId,
        Guid appointmentId,
        Guid staffProfileId,
        Guid openedByUserAccountId,
        DateTime startedLocal)
    {
        if (id == Guid.Empty) throw new ArgumentException("Encounter id is required.", nameof(id));
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id is required.", nameof(patientId));
        if (appointmentId == Guid.Empty) throw new ArgumentException("Appointment id is required.", nameof(appointmentId));
        if (staffProfileId == Guid.Empty) throw new ArgumentException("Responsible clinician is required.", nameof(staffProfileId));
        if (openedByUserAccountId == Guid.Empty) throw new ArgumentException("Opening user is required.", nameof(openedByUserAccountId));

        return new Encounter
        {
            Id = id,
            PatientId = patientId,
            AppointmentId = appointmentId,
            StaffProfileId = staffProfileId,
            OpenedByUserAccountId = openedByUserAccountId,
            StartedLocal = DateTime.SpecifyKind(startedLocal, DateTimeKind.Unspecified),
            EndedLocal = null,
            StatusCode = EncounterStatusCode.Open,
            ImportedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Complete(DateTime endedLocal)
    {
        if (StatusCode != EncounterStatusCode.Open)
            throw new InvalidOperationException("Only an open encounter can be completed.");
        if (endedLocal < StartedLocal)
            throw new ArgumentException("Encounter end cannot be earlier than start.", nameof(endedLocal));

        EndedLocal = DateTime.SpecifyKind(endedLocal, DateTimeKind.Unspecified);
        StatusCode = EncounterStatusCode.Completed;
    }

    public void Cancel(DateTime endedLocal)
    {
        if (StatusCode != EncounterStatusCode.Open)
            throw new InvalidOperationException("Only an open encounter can be cancelled.");
        EndedLocal = DateTime.SpecifyKind(endedLocal < StartedLocal ? StartedLocal : endedLocal, DateTimeKind.Unspecified);
        StatusCode = EncounterStatusCode.Cancelled;
    }
}
