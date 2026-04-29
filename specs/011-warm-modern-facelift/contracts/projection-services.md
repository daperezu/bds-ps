# Projection Services (Application Layer)

All projection services live in `src/FundingPlatform.Application/Services/`, are registered in DI from `FundingPlatform.Web/Program.cs` (or the Application layer's `ServiceCollectionExtensions`), and are presentation-shape providers — they do not mutate state.

---

## `IStageMappingProvider`

Single source of truth for the canonical (stage → icon, label, color-token) table consumed by `IStatusDisplayResolver` and `IJourneyStageResolver` (FR-036).

```csharp
public interface IStageMappingProvider
{
    IReadOnlyList<StageMapping> GetMainline();
    IReadOnlyDictionary<JourneyBranchKind, StageMapping> GetBranches();
    bool IsStageTransition(string versionHistoryAction);
    JourneyStage? StageForAction(string versionHistoryAction);
}

public sealed record StageMapping(
    JourneyStage Stage,
    string IconKey,
    string Label,
    string ColorToken,
    string SubtleColorToken);
```

**Mainline mapping (canonical)**:

| Stage | IconKey | Label | ColorToken | SubtleColorToken |
|-------|---------|-------|------------|------------------|
| Draft | `pencil` | Draft | `--color-text-secondary` | `--color-bg-surface-raised` |
| Submitted | `send` | Submitted | `--color-info` | `--color-info-subtle` |
| UnderReview | `eye` | Under Review | `--color-primary` | `--color-primary-subtle` |
| Decision | `gavel` | Decision | `--color-primary` | `--color-primary-subtle` |
| AgreementGenerated | `file-signature` | Agreement Generated | `--color-primary` | `--color-primary-subtle` |
| Signed | `signature` | Signed | `--color-success` | `--color-success-subtle` |
| Funded | `check-circle` | Funded | `--color-success` | `--color-success-subtle` |

**Branch mapping**:

| BranchKind | IconKey | Label | ColorToken | SubtleColorToken |
|-----------|---------|-------|------------|------------------|
| SentBack | `arrow-back-up` | Sent back | `--color-warning` | `--color-warning-subtle` |
| Rejected | `x-circle` | Rejected | `--color-danger` | `--color-danger-subtle` |
| Appeal | `scale` | Appeal | `--color-info` | `--color-info-subtle` |

---

## `IJourneyStageResolver` (sibling to spec-008's `IStatusDisplayResolver`)

```csharp
public interface IJourneyStageResolver
{
    JourneyStage ResolveCurrent(Application application);
    IReadOnlyList<JourneyNode> BuildMainline(Application application);
    IReadOnlyList<JourneyBranch> BuildBranches(Application application);
}
```

Implementation reads `IStageMappingProvider` and `Application.VersionHistory` aggregates. The implementation depends on `IAppealRepository` (existing) for appeal status only (no new repos).

---

## `IJourneyProjector`

Top-level service consumed by views and other projection services.

```csharp
public interface IJourneyProjector
{
    JourneyViewModel Project(Application application, JourneyVariant variant);

    // For dashboards / queues that need many at once. Avoids N+1 over VersionHistory.
    IReadOnlyDictionary<Guid, JourneyViewModel> ProjectMany(
        IReadOnlyCollection<Application> applications,
        JourneyVariant variant);
}
```

**Internal collaborators**: `IJourneyStageResolver`, `IStageMappingProvider`.

---

## `IApplicantDashboardProjection`

Drives the applicant home dashboard view (US1).

```csharp
public interface IApplicantDashboardProjection
{
    Task<ApplicantDashboardDto> GetForUserAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<ActivityEvent>> GetRecentActivityAsync(string userId, int max, CancellationToken ct);
}
```

**Behavior**:
- Loads applicant + their applications + version histories with a single eager-load query (filter by `Application.ApplicantUserId == userId`, sorted by latest `VersionHistory.OccurredAt`).
- Computes `KpiSnapshot` from the loaded set.
- Identifies the highest-priority `AwaitingAction` across applications (priority order: agreement-to-sign > sent-back-response > draft-to-submit > none).
- Calls `IJourneyProjector.ProjectMany(applications, JourneyVariant.Mini)` for the cards.
- Computes `DaysInCurrentState` per card via the shared utility.
- Reads recent activity events ordered by `OccurredAt` desc, capped at 10 (FR-029).

**Voice-guide compliance**: copy strings (CTA labels, awaiting-action message templates) live in a small `IApplicantCopyProvider` so they can be tweaked without recompiling projections; the provider is itself reviewed against `BRAND-VOICE.md`.

---

## `IReviewerQueueProjection`

Drives the reviewer queue dashboard (US4).

```csharp
public interface IReviewerQueueProjection
{
    Task<ReviewerQueueDto> GetForReviewerAsync(string reviewerId, ReviewerFilter filter, CancellationToken ct);
    Task<IReadOnlyList<ReviewerQueueRowDto>> GetRowsAsync(string reviewerId, ReviewerFilter filter, CancellationToken ct);
}
```

**Behavior**:
- Loads applications assigned to or visible by `reviewerId` per existing review-assignment scope.
- Computes `ReviewerKpiSnapshot` (Aging count uses `ISystemConfigurationRepository.GetInt("AgingThresholdDays", 7)` — single source per FR-053, SC-010).
- Applies the `filter` predicate to produce `Rows`.
- Calls `IJourneyProjector.ProjectMany(filteredApps, JourneyVariant.Micro)` for inline timelines.
- Recent activity feed: queue-scoped events from `VersionHistory`, capped at 5 (FR-055).

**Filter predicates**:

| ReviewerFilter | Predicate |
|---------------|-----------|
| `All` | (no filter) |
| `AwaitingMe` | `Application.Status == UnderReview && AssignedReviewerUserId == reviewerId && !ReviewerHasActed` |
| `Aging` | `DaysInCurrentState > AgingThresholdDays` |
| `SentBack` | `Application.VersionHistory.Any(v => v.Action == "SendBack" && v.OccurredAt >= currentStateEnteredAt)` |
| `Appealing` | `appealRepository.HasOpenAppealFor(application.Id)` |

**Partial endpoint**: `ReviewController.QueueRows(ReviewerFilter filter)` returns `_ReviewerQueueRows.cshtml` (a tbody-only partial) for the chip-reflow contract (data-model.md §3).

---

## `ICopyProvider` (small content layer)

Centralizes the user-facing strings that the projections produce so the voice-guide review can lint them in one place. Three implementations:

- `IApplicantCopyProvider` — applicant-facing strings (welcome, awaiting-action, empty-state, CTA labels).
- `IReviewerCopyProvider` — reviewer-facing strings (queue welcome, filter labels, empty-state).
- `ICeremonyCopyProvider` — variant-aware ceremony hero/subhead copy.

These providers are tiny static-table classes (no I/O); their value is grouping copy in one greppable place.

---

## DI Registration

Add to `FundingPlatform.Application.DependencyInjection` (or wherever the spec-008 services are registered):

```csharp
services.AddSingleton<IStageMappingProvider, StageMappingProvider>();
services.AddScoped<IJourneyStageResolver, JourneyStageResolver>();
services.AddScoped<IJourneyProjector, JourneyProjector>();
services.AddScoped<IApplicantDashboardProjection, ApplicantDashboardProjection>();
services.AddScoped<IReviewerQueueProjection, ReviewerQueueProjection>();
services.AddSingleton<IApplicantCopyProvider, ApplicantCopyProvider>();
services.AddSingleton<IReviewerCopyProvider, ReviewerCopyProvider>();
services.AddSingleton<ICeremonyCopyProvider, CeremonyCopyProvider>();
```

---

## Constitution Compliance

- Services live in Application layer. Razor partials (Web layer) consume DTOs only — never EF entities.
- No new repositories introduced. Reads use existing repos (`IApplicationRepository`, `IAppealRepository`, `ISystemConfigurationRepository`).
- No EF migrations, no schema edits (FR-067).
- Domain entities are unchanged.
