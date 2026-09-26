namespace Dentalla.Application.Abstractions;

public interface IServerClock
{
    DateTimeOffset UtcNow { get; }
}
