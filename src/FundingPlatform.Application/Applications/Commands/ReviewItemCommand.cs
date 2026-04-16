namespace FundingPlatform.Application.Applications.Commands;

public record ReviewItemCommand(
    int ApplicationId,
    int ItemId,
    string Decision,
    string? Comment,
    int? SelectedSupplierId);
