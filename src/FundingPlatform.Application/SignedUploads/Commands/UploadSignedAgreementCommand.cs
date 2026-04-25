namespace FundingPlatform.Application.SignedUploads.Commands;

public record UploadSignedAgreementCommand(
    int ApplicationId,
    string UserId,
    int GeneratedVersion,
    string FileName,
    string ContentType,
    long Size,
    Stream Content);
