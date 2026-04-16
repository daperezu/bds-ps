namespace FundingPlatform.Domain.Entities;

public class Document
{
    public int Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }

    private Document() { }

    public Document(string originalFileName, string storagePath, long fileSize, string contentType)
    {
        OriginalFileName = originalFileName;
        StoragePath = storagePath;
        FileSize = fileSize;
        ContentType = contentType;
        UploadedAt = DateTime.UtcNow;
    }
}
