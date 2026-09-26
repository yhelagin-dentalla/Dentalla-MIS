namespace Dentalla.Domain.Clinical;

public sealed class PerformedService
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid StaffProfileId { get; set; }
    public Guid ServiceCatalogItemId { get; set; }

    public decimal Quantity { get; set; }
    public string? Tooth { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? Comment { get; set; }

    // GREIS visits_services.cost is the historical unit/source price.
    public decimal SourceUnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal? PrimeCost { get; set; }

    // In the audited GREIS dataset manipulation_ok=0 for every Fulfilled
    // service row. Preserve it as a legacy source flag only; it is not used
    // to decide whether a service was performed.
    public bool LegacyManipulationOk { get; set; }
    public short? ComplexityId { get; set; }
    public decimal? ComplexityValue { get; set; }

    public DateTimeOffset ImportedAtUtc { get; set; }
}
