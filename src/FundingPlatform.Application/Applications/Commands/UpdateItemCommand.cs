namespace FundingPlatform.Application.Applications.Commands;

public record UpdateItemCommand(
    int ItemId,
    int ApplicationId,
    string ProductName,
    int CategoryId,
    string TechnicalSpecifications);
