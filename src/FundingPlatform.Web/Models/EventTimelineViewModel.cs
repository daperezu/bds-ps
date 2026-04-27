namespace FundingPlatform.Web.Models;

public sealed record EventTimelineViewModel(
    IReadOnlyList<TimelineEvent> Events,
    bool ShowEmptyMessage = true);
