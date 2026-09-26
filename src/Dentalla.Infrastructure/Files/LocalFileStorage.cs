using System.Security.Cryptography;
using Dentalla.Application.Abstractions;
using Dentalla.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Dentalla.Infrastructure.Files;

public sealed class LocalFileStorage(IOptions<DentallaServerOptions> options) : IFileStorage
{
    public string RootPath { get; } = Path.GetFullPath(options.Value.FileStorageRoot);

    public Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(Path.Combine(RootPath, "documents"));
        Directory.CreateDirectory(Path.Combine(RootPath, "media"));
        Directory.CreateDirectory(Path.Combine(RootPath, "audio"));
        Directory.CreateDirectory(Path.Combine(RootPath, "temp"));
        Directory.CreateDirectory(Path.Combine(RootPath, "exports"));
        return Task.CompletedTask;
    }

    public async Task<StoredFileInfo> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var tempPath = fullPath + ".upload-" + Guid.NewGuid().ToString("N");
        long length;
        string hash;

        try
        {
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var sha = SHA256.Create())
            {
                var buffer = new byte[1024 * 128];
                length = 0;
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    length += read;
                }
                sha.TransformFinalBlock([], 0, 0);
                hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
                await output.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, fullPath, overwrite: true);
            return new StoredFileInfo(NormalizeRelative(relativePath), length, hash);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolveSafePath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public bool Exists(string relativePath) => File.Exists(ResolveSafePath(relativePath));

    private string ResolveSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("Relative path is required.", nameof(relativePath));
        var normalized = NormalizeRelative(relativePath);
        var fullPath = Path.GetFullPath(Path.Combine(RootPath, normalized));
        var rootWithSeparator = RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path escapes Dentalla file storage root.");
        return fullPath;
    }

    private static string NormalizeRelative(string relativePath) => relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
