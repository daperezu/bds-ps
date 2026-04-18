namespace FundingPlatform.Application.SignedUploads.Commands;

public record ReplaceSignedUploadCommand(
    int ApplicationId,
    string UserId,
    int SignedUploadId,
    int GeneratedVersion,
    string FileName,
    string ContentType,
    long Size,
    Stream Content);
