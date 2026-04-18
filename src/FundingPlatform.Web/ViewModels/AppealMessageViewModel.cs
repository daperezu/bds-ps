namespace FundingPlatform.Web.ViewModels;

public class AppealMessageViewModel
{
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string AuthorUserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsByApplicant { get; set; }
}
