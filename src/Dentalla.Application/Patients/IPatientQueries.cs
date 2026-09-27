using Dentalla.Contracts.Patients;

namespace Dentalla.Application.Patients;

public interface IPatientQueries
{
    Task<IReadOnlyList<PatientSearchItemDto>> SearchAsync(
        string? query,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<PatientWorkspaceDto?> GetWorkspaceAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
