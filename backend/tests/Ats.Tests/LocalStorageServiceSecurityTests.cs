using System.Text;
using Ats.Infrastructure.Services.Storage;
using Xunit;

namespace Ats.Tests;

public class LocalStorageServiceSecurityTests : IDisposable
{
    private readonly string _fixtureDirectory;
    private readonly string _testDirectory;
    private readonly string _outsideFile;
    private readonly LocalStorageService _storageService;

    public LocalStorageServiceSecurityTests()
    {
        _fixtureDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"Ats_Storage_Tests_{Guid.NewGuid():N}"));
        _testDirectory = Path.Combine(_fixtureDirectory, "storage");
        Directory.CreateDirectory(_testDirectory);
        _outsideFile = Path.Combine(_fixtureDirectory, "canary.txt");
        File.WriteAllText(_outsideFile, "Content outside the allowed storage directory");
        _storageService = new LocalStorageService(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_fixtureDirectory))
        {
            Directory.Delete(_fixtureDirectory, true);
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
        await using var retrievedStream = await _storageService.GetFileAsync(savedName);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream!);
        string text = await reader.ReadToEndAsync();
        Assert.Equal("Sample CV Content", text);
    }

    [Theory]
    [InlineData("absolute")]
    [InlineData("forward-slashes")]
    [InlineData("backslashes")]
    public async Task ExistingFileOutsideStorage_CannotBeRead(string pathStyle)
    {
        var path = pathStyle switch
        {
            "absolute" => _outsideFile,
            "forward-slashes" => "../canary.txt",
            "backslashes" => "..\\canary.txt",
            _ => throw new ArgumentOutOfRangeException(nameof(pathStyle))
        };

        await using var result = await _storageService.GetFileAsync(path);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistingFileOutsideStorage_CannotBeDeleted()
    {
        var deleted = await _storageService.DeleteFileAsync("../canary.txt");

        Assert.False(deleted);
        Assert.True(File.Exists(_outsideFile));
    }
}
