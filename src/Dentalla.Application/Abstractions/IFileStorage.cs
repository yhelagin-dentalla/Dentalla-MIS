namespace Dentalla.Application.Abstractions;

public interface IFileStorage
{
    string RootPath { get; }
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
    Task<StoredFileInfo> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    bool Exists(string relativePath);
}

public sealed record StoredFileInfo(string RelativePath, long Length, string Sha256Hex);
