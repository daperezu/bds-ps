# Data Model — Warm-Modern Facelift

**Schema changes: NONE** (FR-067, SC-018). This file documents the **view-model projections** that flow from the existing aggregates (`Application`, `VersionHistory`, `SignedUpload`, `Appeal`, `Quotation`, etc.) through the new Application-layer projection services into the Razor partials.

All types listed are **C# records or POCOs** in `FundingPlatform.Application/DTOs/` unless otherwise noted.

---

## 1. Journey View Model (US2)

Consumed by `_ApplicationJourney` partial (Full / Mini / Micro variants). Source: `IJourneyProjector.Project(Application)`.

```csharp
public sealed record JourneyViewModel(
    Guid ApplicationId,
    string ApplicationNumber,
    JourneyStage CurrentMainlineStage,
    IReadOnlyList<JourneyNode> Mainline,        // Draft → Submitted → Under Review → Decision → Agreement Generated → Signed → Funded
    IReadOnlyList<JourneyBranch> Branches,      // 0..n active sub-tracks (Sent back / Rejected / Appeal)
    JourneyVariant Variant);                    // Full / Mini / Micro

public sealed record JourneyNode(
    JourneyStage Stage,
    JourneyNodeState State,                     // Completed / Current / Pending
    DateTimeOffset? Timestamp,                  // null when Pending
    string? ActorName,                          // null when Pending or system
    string IconKey,                             // resolved via IStageMappingProvider
    string Label,
    string ColorToken,                          // e.g. "--color-primary"
    string? EventLogAnchorId);                  // anchor to scroll-to-event (FR-039)

public sealed record JourneyBranch(
    JourneyBranchKind Kind,                     // SentBack / Rejected / Appeal
    JourneyStage AnchorStage,                   // The mainline stage this branch attaches to
    JourneyBranchState State,                   // Active / Resolved / Terminal
    string Label,
    string ColorToken,                          // status-token color
    DateTimeOffset Occurred,
    string? ActorName,
    string? EventLogAnchorId);

public enum JourneyStage { Draft, Submitted, UnderReview, Decision, AgreementGenerated, Signed, Funded }
public enum JourneyNodeState { Completed, Current, Pending }
public enum JourneyBranchKind { SentBack, Rejected, Appeal }
public enum JourneyBranchState { Active, Resolved, Terminal }
public enum JourneyVariant { Full, Mini, Micro }
```

### Branch Resolution Rules

- **Sent back loop**: `Application.VersionHistory` entries with `Action = "SendBack"` produce one `JourneyBranch(Kind=SentBack, State=Resolved, AnchorStage=Decision)` per occurrence; the most recent is rendered if multiple loops exist (with a `LoopCount` annotation in tooltip copy if > 1).
- **Rejected**: When `Application.Status` is `Rejected` and no `Appeal` exists → `JourneyBranch(Kind=Rejected, State=Terminal, AnchorStage=Decision)`. Mainline rendering past Decision is suppressed.
- **Appeal**: When an `Appeal` aggregate exists for the application:
  - Appeal open → `JourneyBranch(Kind=Appeal, State=Active, AnchorStage=Decision)`.
  - Appeal resolved Approved → `JourneyBranch(Kind=Appeal, State=Resolved, AnchorStage=Decision)`; mainline continues.
  - Appeal upheld → `JourneyBranch(Kind=Appeal, State=Terminal, AnchorStage=Decision)`; mainline past Decision suppressed.
- **Both Appeal AND Sent-back loop in history**: both `JourneyBranch` entries returned in the list; partial renders both sub-tracks, Appeal nearer the timeline.
- **Mainline progression**: A node is `Completed` if its corresponding `VersionHistory` action exists (mapping table in `IStageMappingProvider`); the latest completed stage is upgraded to `Current` for rendering; downstream nodes are `Pending`.

### Variant Behavior

| Variant | Renders | Used in |
|---------|---------|---------|
| `Full` | Mainline + branches + tooltips + event-log anchors | Application detail, Review detail, Sign success page (header) |
| `Mini` | Dots + connector + current-stage label only; no per-node interaction | `_ApplicationCard` (US1) |
| `Micro` | Dots only; current dot enlarged; no labels, no tooltips | `_ReviewerQueueRow` (US4) |

---

## 2. Applicant Dashboard DTO (US1)

Consumed by `Views/Home/ApplicantDashboard.cshtml`. Source: `IApplicantDashboardProjection.GetForUser(string applicantUserId)`.

```csharp
public sealed record ApplicantDashboardDto(
    string FirstName,
    KpiSnapshot Kpis,
    AwaitingAction? AwaitingAction,             // null when no action required
    IReadOnlyList<ApplicationCardDto> ActiveApplications, // empty when zero applications
    IReadOnlyList<ActivityEvent> RecentActivity,// max 10, capped per FR-029
    bool HasMoreActivity,
    bool HasMoreApplications);                  // true when count > 3, drives "show all" link

public sealed record KpiSnapshot(
    int Active,
    int InReview,
    int AwaitingApplicantAction,
    int Funded);

public sealed record AwaitingAction(
    Guid ApplicationId,
    string ApplicationNumber,
    string ProjectName,
    string SingleSentenceMessage,               // voice-guide compliant
    string PrimaryCtaLabel,
    string PrimaryCtaHref);

public sealed record ApplicationCardDto(
    Guid ApplicationId,
    string ApplicationNumber,
    string ProjectName,
    JourneyViewModel JourneyMini,               // pre-projected with Variant=Mini
    string CurrentStageLabel,
    int DaysInCurrentState,                     // computed from VersionHistory
    DateTimeOffset LastActivity,
    ContextualAction PrimaryAction);

public sealed record ContextualAction(
    string Label,                                // voice-guide compliant
    string Href,
    ContextualActionStyle Style);                // Primary / Secondary

public sealed record ActivityEvent(
    DateTimeOffset Occurred,
    string Title,                                // voice-guide compliant
    string? ActorName,
    string DeepLinkHref,                         // navigates to surface where event occurred
    string IconKey);
```

### Empty-State Render Rule (FR-031)

When `ActiveApplications.Count == 0`, the view skips the standard layout and renders the welcome scene:

- Seed/sprout illustration (US7 scene-key `seed`)
- Fraunces hero "Ready to apply for funding?"
- Single primary CTA "Start a new application"
- Three-card trust strip (How long it takes / What you'll need / How decisions are made)

---

## 3. Reviewer Queue DTO (US4)

Consumed by `Views/Review/QueueDashboard.cshtml`. Source: `IReviewerQueueProjection.GetForReviewer(string reviewerUserId, ReviewerFilter filter)`.

```csharp
public sealed record ReviewerQueueDto(
    string FirstName,
    ReviewerKpiSnapshot Kpis,
    ReviewerFilter ActiveFilter,                 // current chip selection
    IReadOnlyList<ReviewerActivityEvent> RecentActivity, // max 5, FR-055
    bool HasMoreActivity,
    IReadOnlyList<ReviewerQueueRowDto> Rows);    // empty when zero items in current filter

public sealed record ReviewerKpiSnapshot(
    int AwaitingYourReview,
    int InProgress,
    int Aging,                                   // count > AgingThresholdDays (spec-010 SystemConfiguration)
    int DecidedThisMonth);

public enum ReviewerFilter { All, AwaitingMe, Aging, SentBack, Appealing }

public sealed record ReviewerQueueRowDto(
    Guid ApplicationId,
    string ApplicationNumber,
    string ProjectName,
    string ApplicantName,
    string ApplicantAvatarUrl,                   // initials fallback when null/empty
    JourneyViewModel JourneyMicro,               // pre-projected with Variant=Micro
    int DaysInCurrentState,
    DateTimeOffset LastActivity,
    ContextualAction PrimaryAction);

public sealed record ReviewerActivityEvent(
    DateTimeOffset Occurred,
    string Title,
    string ApplicantName,
    string ApplicationNumber,
    string DeepLinkHref);
```

### Filter Reflow Contract (FR-054)

- Filter chip click triggers a **server-rendered partial replacement** via a small unobtrusive JS handler that calls `GET /Review/QueueRows?filter=Aging` and swaps `<tbody>` with the response. No full page reload. Reflow animates over `--motion-base`. Reduced-motion suppresses the animation.
- The KPI strip and the activity feed are NOT recomputed on chip change (their counts are filter-independent except for the `AwaitingYourReview` count on the active-chip context, which the filter doesn't change).

---

## 4. Signing Ceremony View Model (US3)

Consumed by `Sign/Ceremony.cshtml` and `_SigningCeremony` partial. Source: controller-level projection inside `FundingAgreementController.SignCeremony(Guid applicationId)`.

```csharp
public sealed record SigningCeremonyViewModel(
    Guid ApplicationId,
    SigningCeremonyVariant Variant,              // ApplicantOnly / FunderOnly / BothComplete (applicant-last) / BothComplete (funder-last)
    bool IsFresh,                                // true on first arrival via TempData; false on bookmark re-visit
    string ApplicantFirstName,
    string ProjectName,
    decimal FundedAmount,
    string CurrencyCode,                         // spec-010 currency rendering preserved
    DateOnly DisbursementDate,
    string ViewFundingDetailsHref,
    string DashboardHref);

public enum SigningCeremonyVariant
{
    ApplicantOnlySigned,                         // applicant signed, funder hasn't → no confetti
    FunderOnlySigned,                            // funder signed, applicant hasn't → no confetti, dignified
    BothCompleteApplicantLast,                   // both signed, applicant signed last → full celebration
    BothCompleteFunderLast                       // both signed, funder signed last (applicant signed first) → full celebration
}
```

### Variant → Hero Copy (FR-046)

| Variant | Headline (Fraunces, `--type-display-xl`) | Subhead (Inter, `--type-body`) | Confetti |
|---------|----------------------------------------|-------------------------------|----------|
| ApplicantOnlySigned | "You're signed." | "We're waiting on the funder. We'll email you when it's complete." | OFF |
| FunderOnlySigned | "Funder signature recorded." | "The applicant has been notified." | OFF |
| BothCompleteApplicantLast | "Your funding is locked in." | "Funds will be transferred by {DisbursementDate:MMM d, yyyy}." | ON (single-shot ≤ 2s) |
| BothCompleteFunderLast | "Your funding is locked in." | "Funds will be transferred by {DisbursementDate:MMM d, yyyy}." | ON |

When `IsFresh == false`, Confetti is forced OFF, ticker is forced OFF, and the static seal asset is rendered in place of the animated seal — regardless of variant.

---

## 5. Days-In-Current-State Calculation (FR-028, FR-056)

A small utility on `IJourneyProjector` (or a sibling helper):

```csharp
public static int DaysInCurrentState(IEnumerable<VersionHistory> history, DateTimeOffset asOfUtc)
{
    var lastTransition = history
        .Where(h => StageMappingProvider.IsStageTransition(h.Action))
        .OrderByDescending(h => h.OccurredAt)
        .FirstOrDefault();
    var since = lastTransition?.OccurredAt ?? application.CreatedAt;
    return Math.Max(0, (int)(asOfUtc - since).TotalDays);
}
```

The logic reuses spec-010's `VersionHistory` data; the projection caches the calculation per request to avoid repeated history scans across cards in the same dashboard.

---

## 6. Aging KPI Source-of-Truth (FR-053)

The Aging KPI count + the Aging filter both consult the same single source — the spec-010 `SystemConfiguration` row keyed `AgingThresholdDays`:

```csharp
var threshold = systemConfigurationRepository.GetInt("AgingThresholdDays", defaultValue: 7);
```

The `ReviewerQueueProjection` reads this once per request and threads it through both the KPI computation and the filter predicate so changing the configuration immediately propagates to both surfaces (verified by SC-010).

---

## 7. Resources Strip (FR-030)

Static, content-only:

```csharp
public sealed record ResourcesStripCard(string Title, string Body, string CtaLabel, string CtaHref);

public static class ResourcesStripContent
{
    public static IReadOnlyList<ResourcesStripCard> Cards { get; } = new[]
    {
        new ResourcesStripCard("How funding works", "...", "Read the guide", "/help/how-it-works"),
        new ResourcesStripCard("Submission tips",   "...", "See checklist",  "/help/submission-tips"),
        new ResourcesStripCard("Get help",          "...", "Contact us",      "/help/contact"),
    };
}
```

If a content management surface for these is desired in the future, the strip flips to a CMS-backed source. Out of scope for v1.

---

## 8. Trust Strip (FR-031)

Same shape as Resources strip:

```csharp
public sealed record TrustStripCard(string Title, string Body, string IconKey);

public static class TrustStripContent
{
    public static IReadOnlyList<TrustStripCard> Cards { get; } = new[]
    {
        new TrustStripCard("How long it takes", "...", "calendar"),
        new TrustStripCard("What you'll need", "...", "list-checks"),
        new TrustStripCard("How decisions are made", "...", "compass"),
    };
}
```

Final copy is voice-guide compliant; pinned during implementation.

---

## Validation Rules Summary

| Concern | Where validated | Notes |
|---------|----------------|-------|
| Variant value | `_ApplicationJourney` partial | Throws if `Variant` enum value out-of-range |
| Empty applicant dashboard | View | Renders welcome scene (FR-031) |
| Empty reviewer queue | View | Renders calm-horizon empty-state (FR-058) |
| Bookmark ceremony re-visit | Controller / view | `IsFresh = false` ⇒ static summary state |
| Currency rendering | View formatters | Uses spec-010 currency rendering as-is (no change) |
| Reduced-motion | `tokens.css` + small JS guards | Number ticker skipped, confetti not mounted, etc. |
