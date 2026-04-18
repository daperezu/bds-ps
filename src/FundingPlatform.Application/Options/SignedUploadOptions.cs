namespace FundingPlatform.Application.Options;

public class SignedUploadOptions
{
    public const string SectionName = "SignedUpload";

    public long MaxSizeBytes { get; set; } = 20L * 1024 * 1024;
}
