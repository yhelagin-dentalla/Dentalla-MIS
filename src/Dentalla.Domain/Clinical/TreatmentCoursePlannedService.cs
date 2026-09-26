namespace Dentalla.Domain.Clinical;

public sealed class TreatmentCoursePlannedService
{
    public Guid Id { get; set; }
    public Guid TreatmentCourseId { get; set; }
    public Guid ServiceCatalogItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? DiscountPercent { get; set; }
    public byte LegacyToothCode { get; set; }
    public string? DiagnosisCode { get; set; }
    public DateTime? CreatedLocal { get; set; }
    public DateTime? ChangedLocal { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
