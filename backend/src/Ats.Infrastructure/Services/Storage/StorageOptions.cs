namespace Ats.Infrastructure.Services.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "Local";
    public string BasePath { get; set; } = "storage";
    public string BucketName { get; set; } = "ats-cv-documents";
    public string Region { get; set; } = "us-east-1";
}
