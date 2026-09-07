using System.Text;
using Ats.Infrastructure.Services.Storage;
using Xunit;

namespace Ats.Tests;

public class LocalStorageServiceSecurityTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalStorageService _storageService;

    public LocalStorageServiceSecurityTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Ats_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _storageService = new LocalStorageService(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch { }
        }
    }

    [Fact]
    public async Task LegitimateFile_CanBeSavedAndReadAsync()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("Sample CV Content");
        using var stream = new MemoryStream(content);

        // Act
        string savedName = await _storageService.SaveFileAsync(stream, "cv.pdf", "application/pdf");
        var retrievedStream = await _storageService.GetFileAsync(savedName);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream!);
        string text = await reader.ReadToEndAsync();
        Assert.Equal("Sample CV Content", text);
    }

    [Theory]
    [InlineData("../../windows/system32/cmd.exe")]
    [InlineData("..\\..\\windows\\win.ini")]
    [InlineData("../../../etc/passwd")]
    [InlineData("subdir/../../../../secret.key")]
    public async Task PathTraversalWithRelativeParent_ShouldReturnNull(string traversalPath)
    {
        // Act
        var result = await _storageService.GetFileAsync(traversalPath);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("C:\\Windows\\System32\\calc.exe")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("/etc/shadow")]
    public async Task AbsolutePathOutsideBaseDirectory_ShouldReturnNull(string absolutePath)
    {
        // Act
        var result = await _storageService.GetFileAsync(absolutePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithTraversalPath_ShouldReturnFalseAndNotDelete()
    {
        // Arrange: Create a canary file outside the storage directory
        string outsideDir = Path.Combine(Path.GetTempPath(), $"Ats_Outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outsideDir);
        string canaryFile = Path.Combine(outsideDir, "canary.txt");
        await File.WriteAllTextAsync(canaryFile, "Critical System File");

        try
        {
            // Act: Attempt to delete via path traversal
            string maliciousPath = Path.Combine("..", Path.GetFileName(outsideDir), "canary.txt");
            bool deleted = await _storageService.DeleteFileAsync(maliciousPath);

            // Assert
            Assert.False(deleted);
            Assert.True(File.Exists(canaryFile));
        }
        finally
        {
            if (Directory.Exists(outsideDir))
            {
                Directory.Delete(outsideDir, true);
            }
        }
    }
}
