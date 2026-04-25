namespace FundingPlatform.Application.SignedUploads.Queries;

public record GetSigningStagePanelQuery(
    int ApplicationId,
    string? UserId,
    bool IsAdministrator,
    bool IsReviewerAssigned);
