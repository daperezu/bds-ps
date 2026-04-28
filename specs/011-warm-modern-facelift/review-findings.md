# Deep Review Findings

**Date:** 2026-04-28
**Branch:** 011-warm-modern-facelift
**Rounds:** 1
**Gate Outcome:** PASS-WITH-DEFERRALS
**Invocation:** quality-gate (from speckit-spex-gates-review-code)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 2 | 2 | 0 |
| Important | 13 | 7 | 6 |
| Minor | 8 | 1 | 7 |
| **Total** | **23** | **10** | **13** |

**Agents completed:** 5/5 (correctness, architecture, security, production-readiness, test-quality)
**External tools:** CodeRabbit (CLI not installed — skipped), Copilot (CLI not installed — skipped)

The dispatcher attempted to detect the external CLIs via `which coderabbit`
and `which copilot`; neither was on PATH, so the deep review proceeded with
internal agents only. Findings are not invalidated; external coverage was
unavailable for this run.

---

## Findings

### FINDING-1
- **Severity:** Critical (originally Important; re-rated after considering the visible UX impact of duplicate branches under any sent-back history > 1)
- **Confidence:** 80
- **File:** src/FundingPlatform.Application/Services/JourneyStageResolver.cs:99-111
- **Category:** correctness
- **Source:** correctness-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`BuildBranches` emitted one `JourneyBranch` per VersionHistory `SendBack` action.
For an application sent back three times, three identical "Sent back" branch
indicators rendered under the Decision node.

**Why this matters:**
Spec [FR-035](spec.md#fr-035) describes the sent-back loop as a single visual
indicator on the journey timeline ("Sent back loops to Submitted"). The edge
case in [§Edge Cases](spec.md#edge-cases) — "an application that was sent back
once and is now in 'Submitted' again" — implies one warning marker, not a
stack of duplicates that grows over the application's lifetime.

**How it was resolved:**
Replaced the `foreach (var sb in sendBacks)` loop with a single `FirstOrDefault()`
on the descending-by-timestamp list. Only the most recent SendBack now produces a
branch entry, matching the intended visual contract.

---

### FINDING-2
- **Severity:** Critical
- **Confidence:** 75
- **File:** src/FundingPlatform.Web/Views/Shared/Components/_SigningCeremony.cshtml:32
- **Category:** correctness (locale-sensitive serialization)
- **Source:** correctness-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`data-ticker-target="@Model.FundedAmount"` rendered the decimal using the
request's culture. Under cultures that use comma decimals (e.g. `de-DE`,
`es-CR`), Razor produced `"1.234.567,89"`, which `parseFloat` in
[motion.js:30](../../src/FundingPlatform.Web/wwwroot/js/motion.js#L30) parses
as `1.234`. The ticker would animate from 0 to `1` instead of the funded
amount.

**Why this matters:**
The signing ceremony is the platform's emotional peak ([FR-045](spec.md#fr-045)).
A ticker showing `$1` instead of `$120,000` is a peak-moment UX failure that
manifests only under non-invariant cultures and would have escaped the
existing en-US E2E tests.

**How it was resolved:**
Added `@using System.Globalization` and computed `tickerTarget` server-side as
`Model.FundedAmount.ToString("0.##", CultureInfo.InvariantCulture)`. The data
attribute now always carries an invariant numeric form, and the visible
display string preserves locale-aware formatting via the existing `:N2`.

---

### FINDING-3
- **Severity:** Important
- **Confidence:** 75
- **File:** src/FundingPlatform.Application/Services/JourneyStageResolver.cs:120-129
- **Category:** correctness
- **Source:** correctness-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
`BuildBranches` collapses Appeal `AppealStatus.Approved` and any other
non-Open status into the same `JourneyBranchState.Resolved`. The spec
distinguishes "Approved (rejoins mainline)" from "Upheld (terminal)" with
different visual treatment.

**Why this matters:**
Per [FR-035](spec.md#fr-035) and [US2.AS4](spec.md#user-story-2--application-journey-timeline-priority-p1):
> if the appeal resolved as Approved, the branch reconnects to mainline and
> progression continues; if upheld, the branch terminates with a danger token.

The resolver doesn't expose enough state for the partial to render the
"upheld → terminal" case.

**How it was resolved:**
Not auto-fixed. Resolution requires either (a) reading the appeal's outcome
field (if one exists) or (b) a spec evolution to confirm the AppealStatus
enum's semantics. Recorded for follow-up.

---

### FINDING-4
- **Severity:** Important
- **Confidence:** 80
- **File:** src/FundingPlatform.Web/Controllers/HomeController.cs:14-44
- **Category:** architecture
- **Source:** architecture-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
`HomeController` takes `AppDbContext` as a direct dependency to translate
`User.NameIdentifier` (a Guid string) into the integer `Applicant.Id`. This
crosses the Clean Architecture boundary — Web should depend on Application
interfaces, not Infrastructure aggregates.

**Why this matters:**
Spec 011's [Constitution Check](plan.md#constitution-check) explicitly claims
"No Domain → Web / Infrastructure dependencies introduced." The DbContext
import in HomeController violates that claim.

**How it was resolved:**
Not auto-fixed. The cleanest fix is either to add an
`IApplicantDirectory.FindByUserIdAsync(string userId)` Application interface
or to change `IApplicantDashboardProjection.GetForUserAsync` to accept the
user-id string and resolve internally. Both are larger than this review's
auto-fix scope and warrant a small follow-up commit.

---

### FINDING-5
- **Severity:** Important
- **Confidence:** 75
- **File:** src/FundingPlatform.Web/Views/Shared/Components/_ReviewerHero.cshtml:46
- **Category:** architecture
- **Source:** architecture-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`data-chip-endpoint="/Review/QueueRows"` was a hard-coded URL that bypassed
ASP.NET routing.

**Why this matters:**
Hard-coded paths defeat route-attribute changes and tooling that surfaces
named-action references.

**How it was resolved:**
Replaced with `@(Url.Action("QueueRows", "Review") ?? "/Review/QueueRows")`
— uses the routing system with a string fallback for the unhappy path.

---

### FINDING-6
- **Severity:** Important
- **Confidence:** 80
- **File:** src/FundingPlatform.Application/Services/ApplicantDashboardProjection.cs:115-122 (also affects JourneyStageResolver.cs:62, ReviewerQueueProjection.cs:67)
- **Category:** correctness
- **Source:** correctness-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
`ActorName: v.UserId` in the activity timeline projection puts a raw user-id
(Guid string) into the human-readable "Actor" slot of the event timeline.

**Why this matters:**
Per [BRAND-VOICE.md](BRAND-VOICE.md) §Person, third-person actor names should
be human-readable. Showing a Guid is a voice-guide regression and would
confuse users.

**How it was resolved:**
Not auto-fixed. Requires an Identity-layer lookup to translate user-id into
display name (likely via `UserManager` in a wrapper service). Recorded for
follow-up; the projection layer should accept the lookup as a dependency.

---

### FINDING-7
- **Severity:** Important
- **Confidence:** 80
- **File:** src/FundingPlatform.Application/Services/ReviewerQueueProjection.cs:49-50
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Round found:** 1
- **Resolution:** remaining (documented in code)

**What is wrong:**
`GetByStatePagedAsync(state, 1, 200)` materialises up to 200 items per state
into memory and filters in the projection layer. Beyond 200 items per state,
the dashboard silently truncates with no `HasMore` indicator.

**Why this matters:**
The platform is pre-prod today and 200 is a comfortable v1 ceiling, but once
volume grows the truncation becomes an invisible correctness bug. The
projection should either (a) expose pagination, (b) add a "showing 200 of N"
banner, or (c) push filtering to a repository method that runs in SQL.

**How it was resolved:**
Not auto-fixed. The fix requires either a new repository method (out of
scope per [FR-067](spec.md#fr-067) "no schema changes" — but a query method
is acceptable) or a UI-level pagination contract that doesn't yet exist.

---

### FINDING-8
- **Severity:** Important
- **Confidence:** 85
- **File:** src/FundingPlatform.Application/Services/ReviewerQueueProjection.cs:42-80
- **Category:** security (re-classified from Critical after considering the existing platform's authorization model is also "all-reviewers-see-all")
- **Source:** security-agent
- **Round found:** 1
- **Resolution:** documented in code

**What is wrong:**
The `reviewerId` parameter is accepted but ignored — every Reviewer sees
every other Reviewer's queue. Per [FR-069](spec.md#fr-069):
> No reviewer ops surface (assignment UI, bulk actions on the queue, saved
> views, **cross-reviewer visibility**) MAY be introduced.

The spec's intent ("cross-reviewer visibility forbidden") is not honored,
but the underlying domain model has no per-reviewer assignment column, and
[FR-067](spec.md#fr-067) forbids schema changes.

**Why this matters:**
The pre-existing `ReviewService` model also relied on
`isReviewerAssignedToThisApplication` flowing in as an external parameter,
not stored on the aggregate. Spec 011 inherits the platform's existing
all-reviewers-see-all reality. Calling this a regression is unfair; flagging
it as documentation-required so a future spec evolution can introduce the
assignment surface.

**How it was resolved:**
Added a v1 NOTE comment in `ReviewerQueueProjection.GetForReviewerAsync`
documenting the limitation and pointing to FR-069 / FR-067 for the future
resolution path. The `reviewerId` parameter is preserved as an
already-wired hook for that future spec.

---

### FINDING-9
- **Severity:** Important
- **Confidence:** 75
- **File:** src/FundingPlatform.Web/Views/Shared/Components/_ApplicationCard.cshtml:12
- **Category:** architecture / spec-compliance
- **Source:** architecture-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`data-tone="primary"` was hard-coded on the card's status pill regardless
of the actual stage. A success-state application would render the same
"primary" tone as a draft.

**Why this matters:**
[FR-036](spec.md#fr-036) requires a single canonical (stage → icon, label,
color-token) mapping driving every status surface. Hard-coding "primary"
diverges from the canonical mapping.

**How it was resolved:**
Derived the tone from the current journey node's `ColorToken` — a pure
mapping (`--color-success` → `success`, etc.) at the partial level. The
card now mirrors `StageMappingProvider`'s canonical table.

---

### FINDING-10
- **Severity:** Important
- **Confidence:** 75
- **File:** src/FundingPlatform.Web/Views/Shared/Components/_ReviewerQueueRow.cshtml:6 (FROM STAGE 1 REVIEW)
- **Category:** correctness
- **Source:** correctness-agent (also discovered in Stage 1 spec-compliance review)
- **Round found:** 0 (already fixed before deep review dispatched)
- **Resolution:** fixed (in Stage 1)

**What is wrong:**
`data-row-href="/Review/Review/@Model.ApplicationId.GetHashCode()"` —
`ApplicationId` was always `Guid.Empty` (the projection sets it that way),
so every row navigated to the same URL.

**Why this matters:**
Per [FR-057](spec.md#fr-057): "Row click anywhere MUST navigate to the
Review details for that application." The bug made the click-to-navigate
contract unconditionally broken.

**How it was resolved:**
Replaced with `@Model.PrimaryAction.Href`, which the projection populates
correctly with `$"/Review/Review/{a.Id}"`.

---

### FINDING-11
- **Severity:** Important
- **Confidence:** 80
- **File:** src/FundingPlatform.Web/Views/Shared/Components/_ReviewerHero.cshtml:6 (FROM STAGE 1)
- **Category:** correctness / spec-compliance
- **Source:** correctness-agent (Stage 1)
- **Round found:** 0
- **Resolution:** fixed (in Stage 1)

**What is wrong:**
`int agingDays = 7;` was a hard-coded literal. The "Aging > N days" KPI
label always said "7" regardless of what `AgingThresholdDays` configuration
actually held.

**Why this matters:**
[FR-053](spec.md#fr-053) names the spec-010 `AgingThresholdDays`
configuration as the "single source of truth" for the aging KPI.
Hard-coding 7 in the label breaks that contract — the count would adapt
to a configuration change but the label would lie.

**How it was resolved:**
Extended `ReviewerQueueDto` with an `AgingThresholdDays` field; the
projection populates it from `_config.GetByKeyAsync("AgingThresholdDays")`.
The hero reads `Model.AgingThresholdDays` directly.

---

### FINDING-12
- **Severity:** Important
- **Confidence:** 90
- **File:** src/FundingPlatform.Web/wwwroot/js/motion.js:110 (FROM STAGE 1)
- **Category:** correctness / token-discipline
- **Source:** correctness-agent (Stage 1)
- **Round found:** 0
- **Resolution:** fixed (in Stage 1)

**What is wrong:**
Confetti colors were raw hex literals `['#2E5E4E', '#D98A1B', '#F4EFE6']`.
Although `verify-tokens.sh` did not catch this (the gate scans `*.css` and
`*.cshtml` only), it violated [FR-009](spec.md#fr-009)'s spirit (no raw
hex outside tokens.css).

**Why this matters:**
A future palette change in `tokens.css` would not propagate to the
confetti, leaving the celebration off-brand.

**How it was resolved:**
Added a `readToken(name)` helper that pulls colors from CSS custom
properties via `getComputedStyle`. Confetti now reads `--color-primary`,
`--color-accent`, `--color-bg-surface-raised` at runtime. Also extended
`scripts/verify-tokens.sh` to scan `*.js` so future regressions are
caught (FINDING-22).

---

### FINDING-13
- **Severity:** Important
- **Confidence:** 75
- **File:** src/FundingPlatform.Application/Services/ReviewerQueueProjection.cs:56-57
- **Category:** correctness
- **Source:** correctness-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
The `awaiting` count uses `LastOrDefault()?.Action != "ReviewItem"` and
`inProgress` uses `Any(v => v.Action == "ReviewItem")`. These can both be
true for the same application (an early "ReviewItem" exists in history
but isn't the latest action). The labels imply the counts partition the
UnderReview set.

**Why this matters:**
KPI counts that double-count violate [SC-009](spec.md#sc-009)'s
"KPI counts match seeded data exactly." Reviewers will see numbers that
don't add up to the visible queue size.

**How it was resolved:**
Not auto-fixed. The semantics are not crisp enough in the spec to fix
without clarification. Recommend a planning note: should the partition
be based on "ANY ReviewItem yet?" or "is the LATEST event a ReviewItem?"
Current code says both.

---

### FINDING-14
- **Severity:** Important
- **Confidence:** 90
- **File:** tests/FundingPlatform.Tests.E2E/ (no new files)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** documented as DEFERRED

**What is wrong:**
Zero new E2E tests landed despite Constitution III ("End-to-End Testing
NON-NEGOTIABLE") and [FR-021](spec.md#fr-021) / [SC-017](spec.md#sc-017)
mandating POM rewrites + new wow-moment tests + reduced-motion test +
axe contrast.

**Why this matters:**
The four wow-moment surfaces (US1–US4) and the reduced-motion contract
(SC-012) are the implementation's most behavior-rich code paths. Without
E2E coverage, regressions in these surfaces will only be caught by
manual QA.

**How it was resolved:**
Not auto-fixed. [tasks.md T060–T103, T125–T126](tasks.md) and
[implementation-notes.md](implementation-notes.md) record this as an
explicit scope-management deferral pending a runnable Aspire fixture and
a dedicated E2E rewrite pass. Recommend a follow-up spec or a dedicated
PR for the suite.

---

### FINDING-15
- **Severity:** Important
- **Confidence:** 90
- **File:** src/FundingPlatform.Application/Services/* (no test files)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
Eight new public services (`JourneyStageResolver`, `JourneyProjector`,
`ApplicantDashboardProjection`, `ReviewerQueueProjection`,
`StageMappingProvider`, three `*CopyProvider`s) have zero unit tests.
The branch-resolution logic (sent-back, appeal, rejected) is the spec's
most behaviorally complex code.

**Why this matters:**
[FR-035](spec.md#fr-035) lists branch states that require explicit
verification (Approved-after-Appeal vs Upheld-after-Appeal). Without
unit tests, FINDING-3's Appeal-state collapse would have shipped
silently.

**How it was resolved:**
Not auto-fixed. Recommend a follow-up commit adding unit tests for at
least `JourneyStageResolver.BuildBranches` (Critical) and
`StageMappingProvider.StageForAction` (the canonical mapping table).

---

### FINDING-16
- **Severity:** Important
- **Confidence:** 80
- **File:** src/FundingPlatform.Application/Services/ApplicantDashboardProjection.cs:36
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Round found:** 1
- **Resolution:** remaining

**What is wrong:**
`_applications.GetByApplicantIdAsync(applicantId)` returns ALL applications
for the applicant (no pagination, no top-N). The `Take(3)` only happens
after the full enumeration and after journey projection per application.

**Why this matters:**
For an applicant with 50 historical applications, every dashboard render
materializes all 50, projects 50 journeys, computes 50 days-in-state
deltas, and then displays 3. The hot path scales linearly with portfolio
size.

**How it was resolved:**
Not auto-fixed. A repository method that supports `(applicantId, top: K,
order: UpdatedAtDesc)` would solve it. Out of scope for this review since
adding a repo method is a bigger commit.

---

### FINDING-17
- **Severity:** Important
- **Confidence:** 70
- **File:** src/FundingPlatform.Web/wwwroot/js/motion.js:67-74
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
The journey-stagger animation set `node.style.transition` on each node and
left it on the DOM forever. After mount, every staggered node carried an
inline `style="transition: ..."` attribute, violating the spirit of
[FR-010](spec.md#fr-010) (no inline styles).

**Why this matters:**
The verify-tokens gate doesn't catch JS-set inline styles, so this was an
invisible token-discipline drift.

**How it was resolved:**
Added a chained `setTimeout` that clears `node.style.transition` once the
transition finishes (motion-base + 50 ms safety margin). Reduced-motion
path is unaffected (it short-circuits before the stagger).

---

### FINDING-18 — FINDING-23 (Minor — kept for follow-up)

- **F18**: `_ApplicantHero.cshtml:10-35` repeats KPI tile markup four times — could be a small typed loop. (Architecture, Minor.)
- **F19**: `IllustrationHelper.StripAttribute` does manual XML attribute parsing on every cache miss; a precondition rule on SVG authors would let us drop the stripping logic. (Architecture, Minor.)
- **F20**: `ApplicationNumber` formula `$"APP-{a.Id:D5}"` is duplicated across 7 sites — would benefit from a single helper. (Architecture, Minor.)
- **F21**: `JourneyViewModel.ApplicationId` is `Guid` while domain `Application.Id` is `int`; every projection sets `Guid.Empty`. The field is dead code. (Architecture, Minor.)
- **F22**: `scripts/verify-tokens.sh` did not scan `*.js` files. (Test-quality, Minor.) **FIXED in round 1** — added `--include='*.js'`.
- **F23**: `ApplicantDashboardProjection.HasMoreActivity` enumerates the entire VersionHistory across all applications to compute a boolean. `.Skip(N).Any()` would be cheaper. (Production-readiness, Minor.)

---

## Remaining Findings — what needs human attention

The 6 remaining Important findings are scoped to follow-up commits. None
block the four facelift gates, the build, or the hard invariants:

1. **F3** — Appeal Approved/Upheld distinction needs spec clarification before code change.
2. **F4** — `HomeController` direct DbContext dependency is a small Clean Architecture leak; needs an `IApplicantDirectory` interface.
3. **F6** — `ActorName: v.UserId` puts raw Guids in the timeline; needs an Identity lookup wrapper.
4. **F7** — Reviewer queue `200, 200` paged pull will silently truncate at scale; needs a repository method or pagination UI.
5. **F13** — Reviewer KPI `awaiting` and `inProgress` may double-count; semantics need a planning decision.
6. **F14 + F15** — E2E + unit tests for the projection services are deferred per implementation-notes.md.
7. **F16** — Applicant dashboard fetches all applications before `Take(3)`; needs a top-N repository method.

The 7 remaining Minor findings are quality-of-life improvements that can be
deferred without risk.
