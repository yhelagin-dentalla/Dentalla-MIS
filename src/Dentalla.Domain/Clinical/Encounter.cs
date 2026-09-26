namespace Dentalla.Domain.Clinical;

public sealed class Encounter
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid StaffProfileId { get; set; }
    public DateTime StartedLocal { get; set; }
    public DateTime EndedLocal { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
