using Ats.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Ats.Infrastructure.Services.Storage;

public sealed class LocalStorageService : IDocumentStorageService
{
    private readonly string _basePath;
    private readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public LocalStorageService(IConfiguration configuration)
        : this(configuration["Storage:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "Storage")) { }

    public LocalStorageService(string basePath)
    {
        _basePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(basePath));
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (extension.Length > 11 || extension.Skip(1).Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("Extensión de archivo inválida.", nameof(fileName));

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = ResolvePath(storedName) ?? throw new IOException("Directorio de almacenamiento inválido.");
        try
        {
            await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, FileOptions.Asynchronous);
            await fileStream.CopyToAsync(destination, cancellationToken);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return storedName;
    }

    public Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storagePath);
        Stream? stream = path is not null && File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous)
            : null;
        return Task.FromResult(stream);
    }

    public Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storagePath);
        if (path is null || !File.Exists(path)) return Task.FromResult(false);
        File.Delete(path);
        return Task.FromResult(true);
    }

    private string? ResolvePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return null;
        try
        {
            var normalized = storagePath.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_basePath, normalized));
            if (!fullPath.StartsWith(_basePath + Path.DirectorySeparatorChar, _pathComparison)) return null;

            // Do not follow symbolic links or junctions out of the storage directory.
            for (var current = fullPath; current is not null; current = Path.GetDirectoryName(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return null;
                if (string.Equals(current, _basePath, _pathComparison)) break;
            }
            return fullPath;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
