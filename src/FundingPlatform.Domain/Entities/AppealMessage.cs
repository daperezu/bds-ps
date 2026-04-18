namespace FundingPlatform.Domain.Entities;

public class AppealMessage
{
    public int Id { get; private set; }
    public int AppealId { get; private set; }
    public string AuthorUserId { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AppealMessage() { }

    internal AppealMessage(string authorUserId, string text)
    {
        AuthorUserId = authorUserId;
        Text = text;
        CreatedAt = DateTime.UtcNow;
    }
}
