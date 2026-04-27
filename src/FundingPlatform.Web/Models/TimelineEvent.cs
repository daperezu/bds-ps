namespace FundingPlatform.Web.Models;

public sealed record TimelineEvent(
    DateTimeOffset At,
    string Actor,
    string Action,
    string? Detail = null,
    string? Icon = null,
    string? Color = null);
