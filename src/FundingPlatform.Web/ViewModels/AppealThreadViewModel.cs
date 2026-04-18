using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Web.ViewModels;

public class AppealThreadViewModel
{
    public int ApplicationId { get; set; }
    public int AppealId { get; set; }
    public AppealStatus Status { get; set; }
    public AppealResolution? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<AppealMessageViewModel> Messages { get; set; } = [];
    public bool CanPostMessage { get; set; }
    public bool CanResolve { get; set; }
    public string? NewMessageText { get; set; }
}
