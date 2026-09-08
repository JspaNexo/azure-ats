using Ats.Application.Common.Interfaces;

namespace Ats.Infrastructure.Services.Storage;

public class LocalStorageService : IDocumentStorageService
{
    private readonly string _baseStoragePath;

    public LocalStorageService(string? baseStoragePath = null)
    {
        _baseStoragePath = baseStoragePath ?? Path.Combine(AppContext.BaseDirectory, "storage");
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        string filePath = Path.Combine(_baseStoragePath, uniqueFileName);

        using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (fileStream.CanSeek)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }
        await fileStream.CopyToAsync(destinationStream, cancellationToken);

        return uniqueFileName;
    }

    public async Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        string? fullPath = ResolveAndValidatePath(storagePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        await using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        {
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Position = 0;

        return memoryStream;
    }

    public Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        string? fullPath = ResolveAndValidatePath(storagePath);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private string? ResolveAndValidatePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return null;
        }

        string fullPath;
        if (Path.IsPathRooted(storagePath))
        {
            fullPath = Path.GetFullPath(storagePath);
        }
        else
        {
            fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, storagePath));
        }

        string normalizedBase = Path.GetFullPath(_baseStoragePath);
        if (!normalizedBase.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !normalizedBase.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            normalizedBase += Path.DirectorySeparatorChar;
        }

        // Must reside within the base directory or match it directly
        if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, Path.GetFullPath(_baseStoragePath), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }
}

