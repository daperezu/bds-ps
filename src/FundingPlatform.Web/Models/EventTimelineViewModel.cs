namespace FundingPlatform.Web.Models;

public enum EventTimelineScope
{
    Application = 0,
    Applicant = 1,
    ReviewerQueue = 2,
}

public sealed record EventTimelineViewModel(
    IReadOnlyList<TimelineEvent> Events,
    bool ShowEmptyMessage = true,
    EventTimelineScope Scope = EventTimelineScope.Application);
