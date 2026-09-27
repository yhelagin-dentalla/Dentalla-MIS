using Dentalla.Contracts.Clinical;

namespace Dentalla.Application.Clinical;

public interface IClinicalCommands
{
    Task<EncounterWorkspaceDto> StartEncounterAsync(Guid userAccountId, Guid patientId, StartEncounterRequest request, CancellationToken cancellationToken = default);
    Task<EncounterWorkspaceDto?> GetEncounterAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default);
    Task<EncounterWorkspaceDto?> UpdateNoteAsync(Guid userAccountId, Guid encounterId, UpdateClinicalNoteRequest request, CancellationToken cancellationToken = default);
    Task<EncounterWorkspaceDto?> SignNoteAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default);
    Task<EncounterWorkspaceDto?> CompleteEncounterAsync(Guid userAccountId, Guid encounterId, CancellationToken cancellationToken = default);
}
