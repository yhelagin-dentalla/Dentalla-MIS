namespace Dentalla.Domain.Scheduling;

public sealed class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? StaffProfileId { get; private set; }
    public DateTime StartLocal { get; private set; }
    public DateTime EndLocal { get; private set; }
    public string StatusCode { get; private set; } = string.Empty;
    public int? LegacyRoomId { get; private set; }
    public DateTimeOffset ImportedAtUtc { get; private set; }

    private Appointment() { }

    public Appointment(
        Guid id,
        Guid patientId,
        Guid? staffProfileId,
        DateTime startLocal,
        DateTime endLocal,
        string statusCode,
        int? legacyRoomId,
        DateTimeOffset importedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Appointment id is required.", nameof(id));
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id is required.", nameof(patientId));
        if (endLocal <= startLocal) throw new ArgumentException("Appointment end must be later than start.", nameof(endLocal));

        Id = id;
        PatientId = patientId;
        StaffProfileId = staffProfileId;
        StartLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified);
        EndLocal = DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified);
        StatusCode = statusCode;
        LegacyRoomId = legacyRoomId;
        ImportedAtUtc = importedAtUtc;
    }
}
