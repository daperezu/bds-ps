namespace FundingPlatform.Application.SignedUploads.Commands;

public record ApproveSignedUploadCommand(
    int ApplicationId,
    string ReviewerUserId,
    bool IsAdministrator,
    bool IsReviewerAssigned,
    int SignedUploadId,
    string? Comment);
