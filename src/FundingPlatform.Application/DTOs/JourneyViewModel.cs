namespace FundingPlatform.Application.DTOs;

/// <summary>
/// Spec 011 — US2 journey timeline view model. Three rendering variants:
/// Full (mainline + branches + tooltips + event-log anchors), Mini (dots +
/// connector + current-stage label only), Micro (dots only, current enlarged).
/// </summary>
public sealed record JourneyViewModel(
    Guid ApplicationId,
    string ApplicationNumber,
    JourneyStage CurrentMainlineStage,
    IReadOnlyList<JourneyNode> Mainline,
    IReadOnlyList<JourneyBranch> Branches,
    JourneyVariant Variant);

public sealed record JourneyNode(
    JourneyStage Stage,
    JourneyNodeState State,
    DateTimeOffset? Timestamp,
    string? ActorName,
    string IconKey,
    string Label,
    string ColorToken,
    string? EventLogAnchorId);

public sealed record JourneyBranch(
    JourneyBranchKind Kind,
    JourneyStage AnchorStage,
    JourneyBranchState State,
    string Label,
    string ColorToken,
    DateTimeOffset Occurred,
    string? ActorName,
    string? EventLogAnchorId);

public enum JourneyStage
{
    Draft,
    Submitted,
    UnderReview,
    Decision,
    AgreementGenerated,
    Signed,
    Funded,
}

public enum JourneyNodeState { Completed, Current, Pending }

public enum JourneyBranchKind { SentBack, Rejected, Appeal }

public enum JourneyBranchState { Active, Resolved, Terminal }

public enum JourneyVariant { Full, Mini, Micro }
