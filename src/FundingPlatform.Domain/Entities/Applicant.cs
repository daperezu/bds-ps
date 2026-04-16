namespace FundingPlatform.Domain.Entities;

public class Applicant
{
    private readonly List<Application> _applications = [];

    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string LegalId { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public decimal? PerformanceScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Application> Applications => _applications.AsReadOnly();

    private Applicant() { }

    public Applicant(
        string userId,
        string legalId,
        string firstName,
        string lastName,
        string email,
        string? phone,
        decimal? performanceScore)
    {
        UserId = userId;
        LegalId = legalId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        PerformanceScore = performanceScore;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
