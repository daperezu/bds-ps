namespace FundingPlatform.Application.SignedUploads.Queries;

public record GetSignedAgreementDownloadQuery(
    int ApplicationId,
    int SignedUploadId,
    string? UserId,
    bool IsAdministrator,
    bool IsReviewerAssigned);
