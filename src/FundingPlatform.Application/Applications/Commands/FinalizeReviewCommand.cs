namespace FundingPlatform.Application.Applications.Commands;

public record FinalizeReviewCommand(int ApplicationId, bool Force);
