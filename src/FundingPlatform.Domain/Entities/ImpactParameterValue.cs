namespace FundingPlatform.Domain.Entities;

public class ImpactParameterValue
{
    public int Id { get; private set; }
    public int ImpactId { get; private set; }
    public int ImpactTemplateParameterId { get; private set; }
    public string? Value { get; private set; }

    public Impact Impact { get; private set; } = null!;
    public ImpactTemplateParameter ImpactTemplateParameter { get; private set; } = null!;

    private ImpactParameterValue() { }

    public ImpactParameterValue(int impactTemplateParameterId, string? value)
    {
        ImpactTemplateParameterId = impactTemplateParameterId;
        Value = value;
    }
}
