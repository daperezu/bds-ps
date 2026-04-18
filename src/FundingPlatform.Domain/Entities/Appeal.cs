using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class Appeal
{
    internal const int MaxMessageLength = 4000;

    private readonly List<AppealMessage> _messages = [];

    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public int ApplicantResponseId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public string OpenedByUserId { get; private set; } = string.Empty;
    public AppealStatus Status { get; private set; } = AppealStatus.Open;
    public AppealResolution? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<AppealMessage> Messages => _messages.AsReadOnly();

    private Appeal() { }

    public static Appeal Open(int applicationId, int applicantResponseId, string openedByUserId)
    {
        if (string.IsNullOrWhiteSpace(openedByUserId))
            throw new ArgumentException("OpenedByUserId is required.", nameof(openedByUserId));

        return new Appeal
        {
            ApplicationId = applicationId,
            ApplicantResponseId = applicantResponseId,
            OpenedByUserId = openedByUserId,
            OpenedAt = DateTime.UtcNow,
            Status = AppealStatus.Open
        };
    }

    public AppealMessage PostMessage(string authorUserId, string text)
    {
        if (Status == AppealStatus.Resolved)
            throw new InvalidOperationException("Cannot post messages on a resolved appeal.");

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Message text is required.");

        if (text.Length > MaxMessageLength)
            throw new InvalidOperationException(
                $"Message text exceeds {MaxMessageLength} characters.");

        if (string.IsNullOrWhiteSpace(authorUserId))
            throw new ArgumentException("AuthorUserId is required.", nameof(authorUserId));

        var message = new AppealMessage(authorUserId, text);
        _messages.Add(message);
        return message;
    }

    public void Resolve(string resolvedByUserId, AppealResolution resolution)
    {
        if (Status == AppealStatus.Resolved)
            throw new InvalidOperationException("Appeal is already resolved.");

        if (string.IsNullOrWhiteSpace(resolvedByUserId))
            throw new ArgumentException("ResolvedByUserId is required.", nameof(resolvedByUserId));

        Status = AppealStatus.Resolved;
        Resolution = resolution;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAt = DateTime.UtcNow;
    }
}
