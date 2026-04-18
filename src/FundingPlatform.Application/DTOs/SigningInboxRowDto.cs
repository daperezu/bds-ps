namespace FundingPlatform.Application.DTOs;

public record SigningInboxRowDto(
    int ApplicationId,
    string ApplicantDisplayName,
    int SignedUploadId,
    DateTime UploadedAtUtc,
    int GeneratedVersionAtUpload,
    bool VersionMatchesCurrent);
