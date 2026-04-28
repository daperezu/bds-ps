using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Application.Services;

/// <summary>
/// Spec 011 (research §7) — sibling resolver that turns an Application aggregate
/// into the journey view-model parts. Reads StageMappingProvider as the canonical
/// table; depends on the existing aggregate (no new repos, no new domain methods).
/// </summary>
public interface IJourneyStageResolver
{
    JourneyStage ResolveCurrent(AppEntity application);
    IReadOnlyList<JourneyNode> BuildMainline(AppEntity application);
    IReadOnlyList<JourneyBranch> BuildBranches(AppEntity application);
}

public sealed class JourneyStageResolver : IJourneyStageResolver
{
    private readonly IStageMappingProvider _stageMappings;

    public JourneyStageResolver(IStageMappingProvider stageMappings)
    {
        _stageMappings = stageMappings;
    }

    public JourneyStage ResolveCurrent(AppEntity application)
    {
        // Walk version history in chronological order; the latest known transition
        // sets "current". When the current state is rejected with no appeal, we
        // still mark Decision as "current" because the rejection is a branch label,
        // not a mainline node.
        var current = JourneyStage.Draft;
        foreach (var ev in application.VersionHistory.OrderBy(v => v.Timestamp))
        {
            var s = _stageMappings.StageForAction(ev.Action);
            if (s.HasValue) current = s.Value;
        }

        // Funded — heuristic since a "Funded" version-history action may not exist
        // in legacy data; treat AgreementExecuted as the latest mainline node we can
        // confirm purely from version history. The projector layer can override
        // when richer data is available.
        return current;
    }

    public IReadOnlyList<JourneyNode> BuildMainline(AppEntity application)
    {
        var current = ResolveCurrent(application);
        var stageOrder = _stageMappings.GetMainline().Select(m => m.Stage).ToList();
        var currentIndex = stageOrder.IndexOf(current);

        // Build a stage→timestamp+actor lookup from version history.
        var stageTouches = new Dictionary<JourneyStage, (DateTimeOffset At, string? Actor)>();
        foreach (var ev in application.VersionHistory)
        {
            var s = _stageMappings.StageForAction(ev.Action);
            if (!s.HasValue) continue;
            // Latest wins so a stage with multiple actions surfaces the most recent.
            stageTouches[s.Value] = (new DateTimeOffset(ev.Timestamp, TimeSpan.Zero), ev.UserId);
        }

        var nodes = new List<JourneyNode>(stageOrder.Count);
        for (int i = 0; i < stageOrder.Count; i++)
        {
            var stage = stageOrder[i];
            var mapping = _stageMappings.ForStage(stage);
            JourneyNodeState state;
            if (i < currentIndex) state = JourneyNodeState.Completed;
            else if (i == currentIndex) state = JourneyNodeState.Current;
            else state = JourneyNodeState.Pending;

            DateTimeOffset? ts = null;
            string? actor = null;
            if (stageTouches.TryGetValue(stage, out var touch))
            {
                ts = touch.At;
                actor = touch.Actor;
            }

            var anchor = ts.HasValue ? $"event-{stage.ToString().ToLowerInvariant()}-{application.Id}" : null;
            nodes.Add(new JourneyNode(stage, state, ts, actor, mapping.IconKey, mapping.Label, mapping.ColorToken, anchor));
        }

        return nodes;
    }

    public IReadOnlyList<JourneyBranch> BuildBranches(AppEntity application)
    {
        var branches = new List<JourneyBranch>();

        // Sent-back loops (one entry per occurrence; render most recent in the partial).
        var sendBacks = application.VersionHistory
            .Where(v => string.Equals(v.Action, "SendBack", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.Timestamp)
            .ToList();
        foreach (var sb in sendBacks)
        {
            var mapping = _stageMappings.GetBranches()[JourneyBranchKind.SentBack];
            branches.Add(new JourneyBranch(
                JourneyBranchKind.SentBack,
                JourneyStage.Decision,
                JourneyBranchState.Resolved,
                mapping.Label,
                mapping.ColorToken,
                new DateTimeOffset(sb.Timestamp, TimeSpan.Zero),
                sb.UserId,
                $"event-sentback-{sb.Id}"));
        }

        // Appeals — based on Application.Appeals aggregate.
        foreach (var appeal in application.Appeals)
        {
            var mapping = _stageMappings.GetBranches()[JourneyBranchKind.Appeal];
            JourneyBranchState st;
            if (appeal.Status == AppealStatus.Open) st = JourneyBranchState.Active;
            else st = JourneyBranchState.Resolved;
            branches.Add(new JourneyBranch(
                JourneyBranchKind.Appeal,
                JourneyStage.Decision,
                st,
                mapping.Label,
                mapping.ColorToken,
                new DateTimeOffset(appeal.OpenedAt, TimeSpan.Zero),
                appeal.OpenedByUserId,
                $"event-appeal-{appeal.Id}"));
        }

        // Rejected — terminal state for spec-008's ApplicationState.Resolved when
        // no funding agreement was generated. We treat this conservatively: if the
        // application has resolved with no agreement and no appeal, surface the
        // rejected branch.
        var hasAgreement = application.FundingAgreement is not null;
        var hasOpenOrResolvedAppeal = application.Appeals.Count > 0;
        if (!hasAgreement && !hasOpenOrResolvedAppeal && application.State == ApplicationState.Resolved)
        {
            var mapping = _stageMappings.GetBranches()[JourneyBranchKind.Rejected];
            branches.Add(new JourneyBranch(
                JourneyBranchKind.Rejected,
                JourneyStage.Decision,
                JourneyBranchState.Terminal,
                mapping.Label,
                mapping.ColorToken,
                DateTimeOffset.UtcNow,
                null,
                null));
        }

        return branches;
    }
}
