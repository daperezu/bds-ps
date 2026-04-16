namespace FundingPlatform.Application.Admin.Commands;

public record CreateImpactTemplateCommand(
    string Name,
    string? Description,
    List<ParameterDefinition> Parameters);

public record ParameterDefinition(
    string Name,
    string DisplayLabel,
    string DataType,
    bool IsRequired,
    string? ValidationRules,
    int SortOrder);
