namespace FundingPlatform.Application.Applications.Commands;

public record SetItemImpactCommand(
    int ApplicationId,
    int ItemId,
    int ImpactTemplateId,
    Dictionary<int, string?> ParameterValues);
