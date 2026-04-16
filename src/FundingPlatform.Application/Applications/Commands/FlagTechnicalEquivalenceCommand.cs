namespace FundingPlatform.Application.Applications.Commands;

public record FlagTechnicalEquivalenceCommand(
    int ApplicationId,
    int ItemId,
    bool IsNotEquivalent);
