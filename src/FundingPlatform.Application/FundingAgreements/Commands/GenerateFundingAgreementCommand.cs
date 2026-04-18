namespace FundingPlatform.Application.FundingAgreements.Commands;

public record GenerateFundingAgreementCommand(
    int ApplicationId,
    string UserId,
    bool IsAdministrator,
    bool IsReviewerAssigned);
