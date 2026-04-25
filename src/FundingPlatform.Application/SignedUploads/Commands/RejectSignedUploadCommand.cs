namespace FundingPlatform.Application.SignedUploads.Commands;

public record RejectSignedUploadCommand(
    int ApplicationId,
    string ReviewerUserId,
    bool IsAdministrator,
    bool IsReviewerAssigned,
    int SignedUploadId,
    string Comment);
