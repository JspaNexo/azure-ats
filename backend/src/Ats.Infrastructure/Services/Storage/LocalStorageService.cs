using Ats.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Ats.Infrastructure.Services.Storage;

public class LocalStorageService : IDocumentStorageService
{
    private readonly string _baseStoragePath;

    public LocalStorageService(IOptions<StorageOptions>? options = null, string? baseStoragePath = null)
    {
        string? configuredPath = options?.Value?.BasePath ?? baseStoragePath;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _baseStoragePath = Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        }
        else
        {
            _baseStoragePath = Path.Combine(AppContext.BaseDirectory, "storage");
        }

        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public LocalStorageService(string baseStoragePath)
        : this(null, baseStoragePath)
    {
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

        string normalizedBase = Path.GetFullPath(_baseStoragePath);
        if (!normalizedBase.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !normalizedBase.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            normalizedBase += Path.DirectorySeparatorChar;
        }

        // 1. Si es ruta absoluta
        if (Path.IsPathRooted(storagePath))
        {
            try
            {
                string fullPath = Path.GetFullPath(storagePath);
                if (fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignorar errores de sintaxis de ruta de otros SOs
            }

            // Si la ruta absoluta proviene de otro entorno (ej: Docker /app/Storage/archivo.pdf),
            // verificar de manera segura si el archivo existe dentro de _baseStoragePath
            string fileNameOnly = Path.GetFileName(storagePath);
            if (!string.IsNullOrWhiteSpace(fileNameOnly))
            {
                string candidateInBase = Path.GetFullPath(Path.Combine(normalizedBase, fileNameOnly));
                if (candidateInBase.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase) && File.Exists(candidateInBase))
                {
                    return candidateInBase;
                }
            }

            // Rutas absolutas externas fuera de base (ej: C:\Windows\System32\calc.exe) deben rechazarse
            return null;
        }

        // 2. Si es ruta relativa (prevenir path traversal como ../..)
        string resolvedRelative = Path.GetFullPath(Path.Combine(normalizedBase, storagePath));
        if (!resolvedRelative.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(resolvedRelative, Path.GetFullPath(_baseStoragePath), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return resolvedRelative;
    }
}

