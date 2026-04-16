namespace FundingPlatform.Application.DTOs;

public record ImpactDto(
    int Id,
    int ImpactTemplateId,
    string ImpactTemplateName,
    List<ImpactParameterValueDto> ParameterValues);

public record ImpactParameterValueDto(
    int Id,
    int ParameterId,
    string ParameterName,
    string ParameterDisplayLabel,
    string DataType,
    bool IsRequired,
    string? Value);
