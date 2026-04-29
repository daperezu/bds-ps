namespace FundingPlatform.Application.DTOs;

/// <summary>Spec 011 US4 — reviewer queue dashboard view model (data-model.md §3).</summary>
public sealed record ReviewerQueueDto(
    string FirstName,
    ReviewerKpiSnapshot Kpis,
    ReviewerFilter ActiveFilter,
    IReadOnlyList<ReviewerActivityEvent> RecentActivity,
    bool HasMoreActivity,
    IReadOnlyList<ReviewerQueueRowDto> Rows,
    int AgingThresholdDays);

public sealed record ReviewerKpiSnapshot(
    int AwaitingYourReview,
    int InProgress,
    int Aging,
    int DecidedThisMonth);

public enum ReviewerFilter { All, AwaitingMe, Aging, SentBack, Appealing }

public sealed record ReviewerQueueRowDto(
    Guid ApplicationId,
    string ApplicationNumber,
    string ProjectName,
    string ApplicantName,
    string? ApplicantAvatarUrl,
    JourneyViewModel JourneyMicro,
    int DaysInCurrentState,
    DateTimeOffset LastActivity,
    ContextualAction PrimaryAction);

public sealed record ReviewerActivityEvent(
    DateTimeOffset Occurred,
    string Title,
    string ApplicantName,
    string ApplicationNumber,
    string DeepLinkHref);
