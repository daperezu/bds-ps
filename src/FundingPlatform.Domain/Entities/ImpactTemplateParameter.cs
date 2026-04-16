using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class ImpactTemplateParameter
{
    public int Id { get; private set; }
    public int ImpactTemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayLabel { get; private set; } = string.Empty;
    public ParameterDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? ValidationRules { get; private set; }
    public int SortOrder { get; private set; }

    public ImpactTemplate ImpactTemplate { get; private set; } = null!;

    private ImpactTemplateParameter() { }

    public ImpactTemplateParameter(
        string name,
        string displayLabel,
        ParameterDataType dataType,
        bool isRequired,
        string? validationRules,
        int sortOrder)
    {
        Name = name;
        DisplayLabel = displayLabel;
        DataType = dataType;
        IsRequired = isRequired;
        ValidationRules = validationRules;
        SortOrder = sortOrder;
    }
}
