using Dentalla.Contracts.Patients;

namespace Dentalla.Application.Patients;

public interface IPatientQueries
{
    Task<PatientSummaryDto?> GetSummaryAsync(Guid patientId, CancellationToken cancellationToken = default);
}
