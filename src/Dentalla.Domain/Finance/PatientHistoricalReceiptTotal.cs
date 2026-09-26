namespace Dentalla.Domain.Finance;

/// <summary>
/// Source-aware historical aggregate of money actually received from a patient.
/// This is not a current patient balance and not a reconstructed cash ledger.
/// </summary>
public sealed class PatientHistoricalReceiptTotal
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string CalculationCode { get; set; } = string.Empty;
    public DateOnly? PeriodStartLocal { get; set; }
    public DateOnly? PeriodEndLocal { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset ImportedAtUtc { get; set; }
}
