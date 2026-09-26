namespace Dentalla.Contracts.Patients;

public sealed record PatientSummaryDto(
    Guid Id,
    string CardNumber,
    string FullName,
    DateOnly? BirthDate,
    string? Phone,
    string? Email,
    string? PrimaryDoctor,
    decimal Balance);
