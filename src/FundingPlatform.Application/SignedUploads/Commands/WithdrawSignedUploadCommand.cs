namespace FundingPlatform.Application.SignedUploads.Commands;

public record WithdrawSignedUploadCommand(
    int ApplicationId,
    string UserId,
    int SignedUploadId);
