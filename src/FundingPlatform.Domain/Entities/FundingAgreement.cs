namespace FundingPlatform.Domain.Entities;

public class FundingAgreement
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }
    public string GeneratedByUserId { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = [];

    private FundingAgreement() { }

    internal FundingAgreement(
        int applicationId,
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string generatedByUserId)
    {
        Validate(fileName, contentType, size, storagePath, generatedByUserId);

        ApplicationId = applicationId;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        StoragePath = storagePath;
        GeneratedAtUtc = DateTime.UtcNow;
        GeneratedByUserId = generatedByUserId;
    }

    internal void Replace(
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string regeneratingUserId)
    {
        Validate(fileName, contentType, size, storagePath, regeneratingUserId);

        FileName = fileName;
        ContentType = contentType;
        Size = size;
        StoragePath = storagePath;
        GeneratedAtUtc = DateTime.UtcNow;
        GeneratedByUserId = regeneratingUserId;
    }

    private static void Validate(
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("FundingAgreement requires a non-empty file name.");

        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"FundingAgreement content type must be 'application/pdf' (was '{contentType}').");

        if (size <= 0)
            throw new InvalidOperationException("FundingAgreement size must be greater than zero.");

        if (string.IsNullOrWhiteSpace(storagePath))
            throw new InvalidOperationException("FundingAgreement requires a non-empty storage path.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("FundingAgreement requires a non-empty generating user id.");
    }
}
