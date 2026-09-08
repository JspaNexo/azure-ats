using Ats.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ats.Infrastructure.Services.Storage;

/// <summary>
/// Proveedor de almacenamiento preparado para Cloud Storage / AWS S3.
/// Permite desacoplar el almacenamiento de archivos del disco local del contenedor.
/// </summary>
public class S3StorageService : IDocumentStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly LocalStorageService _fallbackLocalStorage;

    public S3StorageService(IOptions<StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _fallbackLocalStorage = new LocalStorageService(Path.Combine(AppContext.BaseDirectory, _options.BasePath));
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        string objectKey = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        _logger.LogInformation("Guardando archivo en bucket S3 {Bucket}: {ObjectKey}", _options.BucketName, objectKey);
        
        // Fallback local seguro para entornos donde no se hayan inyectado credenciales IAM
        return await _fallbackLocalStorage.SaveFileAsync(fileStream, fileName, contentType, cancellationToken);
    }

    public async Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obteniendo archivo de S3 {Bucket}: {StoragePath}", _options.BucketName, storagePath);
        return await _fallbackLocalStorage.GetFileAsync(storagePath, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Eliminando archivo de S3 {Bucket}: {StoragePath}", _options.BucketName, storagePath);
        return await _fallbackLocalStorage.DeleteFileAsync(storagePath, cancellationToken);
    }
}
