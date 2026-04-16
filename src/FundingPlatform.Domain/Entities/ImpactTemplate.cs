namespace FundingPlatform.Domain.Entities;

public class ImpactTemplate
{
    private readonly List<ImpactTemplateParameter> _parameters = [];

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ImpactTemplateParameter> Parameters => _parameters.AsReadOnly();

    private ImpactTemplate() { }

    public ImpactTemplate(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddParameter(ImpactTemplateParameter parameter)
    {
        _parameters.Add(parameter);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearParameters()
    {
        _parameters.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}
