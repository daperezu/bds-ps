namespace FundingPlatform.Application.DTOs;

public record ImpactTemplateDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    List<ImpactTemplateParameterDto> Parameters);

public record ImpactTemplateParameterDto(
    int Id,
    string Name,
    string DisplayLabel,
    string DataType,
    bool IsRequired,
    string? ValidationRules,
    int SortOrder);
