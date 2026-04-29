using FundingPlatform.Application.DTOs;

namespace FundingPlatform.Application.Services;

/// <summary>
/// Spec 011 (FR-036) — single source of truth for the canonical
/// (stage → icon, label, color-token) mapping consumed by IStatusDisplayResolver
/// and IJourneyStageResolver.
/// </summary>
public interface IStageMappingProvider
{
    IReadOnlyList<StageMapping> GetMainline();
    IReadOnlyDictionary<JourneyBranchKind, StageMapping> GetBranches();
    bool IsStageTransition(string versionHistoryAction);
    JourneyStage? StageForAction(string versionHistoryAction);
    StageMapping ForStage(JourneyStage stage);
}

public sealed record StageMapping(
    JourneyStage Stage,
    string IconKey,
    string Label,
    string ColorToken,
    string SubtleColorToken);

public sealed class StageMappingProvider : IStageMappingProvider
{
    private static readonly IReadOnlyList<StageMapping> Mainline = new List<StageMapping>
    {
        new(JourneyStage.Draft,              "ti ti-pencil",         "Draft",                 "--color-text-secondary", "--color-bg-surface-raised"),
        new(JourneyStage.Submitted,          "ti ti-send",           "Submitted",             "--color-info",           "--color-info-subtle"),
        new(JourneyStage.UnderReview,        "ti ti-eye",            "Under Review",          "--color-primary",        "--color-primary-subtle"),
        new(JourneyStage.Decision,           "ti ti-gavel",          "Decision",              "--color-primary",        "--color-primary-subtle"),
        new(JourneyStage.AgreementGenerated, "ti ti-file-signature", "Agreement Generated",   "--color-primary",        "--color-primary-subtle"),
        new(JourneyStage.Signed,             "ti ti-signature",      "Signed",                "--color-success",        "--color-success-subtle"),
        new(JourneyStage.Funded,             "ti ti-circle-check",   "Funded",                "--color-success",        "--color-success-subtle"),
    };

    private static readonly IReadOnlyDictionary<JourneyBranchKind, StageMapping> Branches =
        new Dictionary<JourneyBranchKind, StageMapping>
        {
            [JourneyBranchKind.SentBack] = new(JourneyStage.Decision, "ti ti-arrow-back-up", "Sent back", "--color-warning", "--color-warning-subtle"),
            [JourneyBranchKind.Rejected] = new(JourneyStage.Decision, "ti ti-x-circle",      "Rejected",  "--color-danger",  "--color-danger-subtle"),
            [JourneyBranchKind.Appeal]   = new(JourneyStage.Decision, "ti ti-scale",         "Appeal",    "--color-info",    "--color-info-subtle"),
        };

    private static readonly IReadOnlyDictionary<string, JourneyStage> ActionToStage =
        new Dictionary<string, JourneyStage>(StringComparer.OrdinalIgnoreCase)
        {
            ["Created"]                   = JourneyStage.Draft,
            ["Submitted"]                 = JourneyStage.Submitted,
            ["StartReview"]               = JourneyStage.UnderReview,
            ["Finalize"]                  = JourneyStage.Decision,
            ["AgreementGenerated"]        = JourneyStage.AgreementGenerated,
            ["AgreementRegenerated"]      = JourneyStage.AgreementGenerated,
            ["AgreementExecuted"]         = JourneyStage.Signed,
            ["Funded"]                    = JourneyStage.Funded,
        };

    public IReadOnlyList<StageMapping> GetMainline() => Mainline;
    public IReadOnlyDictionary<JourneyBranchKind, StageMapping> GetBranches() => Branches;

    public bool IsStageTransition(string versionHistoryAction)
        => !string.IsNullOrWhiteSpace(versionHistoryAction)
           && ActionToStage.ContainsKey(versionHistoryAction);

    public JourneyStage? StageForAction(string versionHistoryAction)
    {
        if (string.IsNullOrWhiteSpace(versionHistoryAction)) return null;
        return ActionToStage.TryGetValue(versionHistoryAction, out var stage) ? stage : (JourneyStage?)null;
    }

    public StageMapping ForStage(JourneyStage stage)
    {
        foreach (var m in Mainline)
        {
            if (m.Stage == stage) return m;
        }
        throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown JourneyStage");
    }
}
