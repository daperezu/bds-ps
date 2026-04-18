namespace FundingPlatform.Domain.Enums;

public enum ApplicationState
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Resolved = 3,
    AppealOpen = 4,
    ResponseFinalized = 5,
    AgreementExecuted = 6
}
