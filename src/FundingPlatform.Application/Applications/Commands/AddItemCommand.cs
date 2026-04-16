namespace FundingPlatform.Application.Applications.Commands;

public record AddItemCommand(
    int ApplicationId,
    string ProductName,
    int CategoryId,
    string TechnicalSpecifications);
