using Dentalla.Application.Abstractions;

namespace Dentalla.Infrastructure.Hosting;

public sealed class SystemServerClock : IServerClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
