using Dentalla.Contracts.Security;

namespace Dentalla.Application.Security;

public interface ILoginDirectoryReader
{
    Task<LoginDirectoryDto> ReadAsync(CancellationToken cancellationToken = default);
}
