namespace FundingPlatform.Domain.Entities;

public class SystemConfiguration
{
    public int Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SystemConfiguration() { }

    public SystemConfiguration(string key, string value, string? description)
    {
        Key = key;
        Value = value;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
