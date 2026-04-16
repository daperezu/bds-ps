namespace FundingPlatform.Domain.Entities;

public class Impact
{
    private readonly List<ImpactParameterValue> _parameterValues = [];

    public int Id { get; private set; }
    public int ItemId { get; private set; }
    public int ImpactTemplateId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ImpactTemplate ImpactTemplate { get; private set; } = null!;
    public Item Item { get; private set; } = null!;

    public IReadOnlyList<ImpactParameterValue> ParameterValues => _parameterValues.AsReadOnly();

    private Impact() { }

    public Impact(int impactTemplateId, List<ImpactParameterValue> parameterValues)
    {
        ImpactTemplateId = impactTemplateId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _parameterValues.AddRange(parameterValues);
    }
}
