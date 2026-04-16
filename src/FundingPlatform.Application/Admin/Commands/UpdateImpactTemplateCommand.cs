namespace FundingPlatform.Application.Admin.Commands;

public record UpdateImpactTemplateCommand(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    List<ParameterDefinition> Parameters);
