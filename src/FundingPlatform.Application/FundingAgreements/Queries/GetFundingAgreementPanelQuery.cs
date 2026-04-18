namespace FundingPlatform.Application.FundingAgreements.Queries;

public record GetFundingAgreementPanelQuery(
    int ApplicationId,
    string? UserId,
    bool IsAdministrator,
    bool IsReviewerAssigned);
