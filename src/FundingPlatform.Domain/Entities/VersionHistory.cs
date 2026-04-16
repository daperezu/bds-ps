namespace FundingPlatform.Domain.Entities;

public class VersionHistory
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    public Application Application { get; private set; } = null!;

    private VersionHistory() { }

    public VersionHistory(string userId, string action, string? details)
    {
        UserId = userId;
        Action = action;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
