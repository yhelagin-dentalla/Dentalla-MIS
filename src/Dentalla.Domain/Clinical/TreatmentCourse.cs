namespace Dentalla.Domain.Clinical;

public sealed class TreatmentCourse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? OwnerStaffProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? StartLocal { get; set; }
    public DateTime? EndLocal { get; set; }
    public DateTime? CreatedLocal { get; set; }
    public DateTime? ChangedLocal { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
