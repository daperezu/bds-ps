# Review Guide: Warm-Modern Facelift

**Spec:** [spec.md](spec.md) | **Plan:** [plan.md](plan.md) | **Tasks:** [tasks.md](tasks.md) | **Research:** [research.md](research.md) | **Data model:** [data-model.md](data-model.md)
**Generated:** 2026-04-28

---

## What This Spec Does

After spec 008 gave the platform Tabler.io structural consistency, the surfaces still feel flat — defaults, not identity. This spec elevates the entire authenticated experience to a "warm-modern premium" finish appropriate for a funding agency serving entrepreneurs: a brand identity (proposed display name *Forge*, warm forest-green + amber palette, Fraunces/Inter/JetBrains Mono pairing, voice guide), a CSS custom-property design-token layer the spec-008 partials all consume, a token-driven motion system that respects `prefers-reduced-motion`, four signature wow-moments (applicant home dashboard, application journey timeline, signing ceremony, reviewer queue dashboard), a 9-scene illustration set, and a sweep across every authenticated view. **Zero schema changes** — wow-moment data is sourced from new Application-layer projections over existing aggregates.

**In scope:** Brand tokens + voice ([FR-001..FR-005](spec.md#functional-requirements)); CSS token layer + Tabler bridge + motion catalog ([FR-006..FR-016](spec.md#functional-requirements)); full-view sweep across ~50 view files ([FR-017..FR-023](spec.md#functional-requirements)); applicant home dashboard ([FR-024..FR-033](spec.md#functional-requirements)); journey timeline with three variants ([FR-034..FR-043](spec.md#functional-requirements)); signing ceremony ([FR-044..FR-051](spec.md#functional-requirements)); reviewer queue dashboard ([FR-052..FR-060](spec.md#functional-requirements)); 9 SVG illustrations + helper ([FR-061..FR-066](spec.md#functional-requirements)); cross-cutting brand sign-off, perf baseline, asset budget, WCAG AA ([FR-072..FR-075](spec.md#functional-requirements)).

**Out of scope (FR-067..FR-071):** dacpac edits; real-time push (SignalR/websockets); reviewer ops surface (assignment, bulk actions, saved views); dark mode toggle/storage in v1 (tokens *designed* to be theme-able); marketing-style hero, social proof, sound, haptics, sharing, achievements/streaks. Login/Register/ChangePassword/AccessDenied are swept but explicitly *not* signature moments. PDF templates are byte-frozen carve-outs.

## Bigger Picture

This spec sits at a critical inflection: the platform is pre-production and the team has chosen this moment to invest in a finish that the entrepreneur audience will read as serious craft on first contact. It builds on spec 008's partial discipline and spec 010's `VersionHistory` + `AgingThresholdDays` infrastructure — without those foundations, the journey timeline and reviewer queue would each need their own data layer. The `JourneyProjector` pattern in [contracts/projection-services.md](contracts/projection-services.md) is also the natural place a future communications/notifications spec would hook into.

The visual reference family (Mercury / Ramp / Notion) pulls toward a lot of warmth without sacrificing density. Worth noting: those products earn their feel partly through subtle motion choices that are hard to specify on paper. The motion catalog in [contracts/motion-catalog.md](contracts/motion-catalog.md) is a strong attempt to pin them — the question is whether 14 entries will hold the line in practice or whether implementers will reach for one more pattern and the catalog will drift.

The plan deliberately defers visual-regression tooling ([research.md §8](research.md)) — a reasonable carry-over from spec 008, but it does mean SC-006 and SC-019 are checklist-based human reviews. The semantic-locator upgrade in US6 ([research.md §10](research.md)) is the foundation that makes a future Percy/Chromatic adoption cheap; that bet is load-bearing for whether this spec stays auditable a year from now.

---

## Spec Review Guide (30 minutes)

> This guide focuses your 30 minutes on the parts of the plan that need human judgment most.

### Understanding the approach (8 min)

Read [plan.md Summary](plan.md#summary) and [Constitution Check](plan.md#constitution-check) for the technical posture, then [research.md §1.1–1.4](research.md) for the brand identity proposal. As you read, consider:

- Does *Forge* feel right for a funding agency serving entrepreneurs, or does the *Ascent* fallback fit better? ([research.md §1.1](research.md))
- The warm forest green primary `#2E5E4E` over warm off-white `#FAF7F2` — does this read "trustworthy + warm" to you, or does it veer agricultural? ([research.md §1.2](research.md))
- The plan claims zero schema changes are achievable because every wow-moment is a projection over existing aggregates ([plan.md §Constitution Check IV](plan.md#constitution-check)). Does the [data-model.md branch resolution rules](data-model.md#branch-resolution-rules) for "both Appeal AND Sent-back loop in history" actually hold against the existing `Appeal` + `VersionHistory` shapes?

### Key decisions that need your eyes (12 min)

**Brand display name selection** ([research.md §1.1](research.md), [tasks.md T010-T014](tasks.md), [tasks.md T133](tasks.md))

The spec proposes *Forge* but defers the actual choice to FR-072 user sign-off. Tasks T012–T014 currently produce assets named "Forge"; if the user picks *Ascent* or "keep `FundingPlatform`", a one-PR rename follows. The placeholder approach is pragmatic but means SVG wordmark + voice-guide prose are written before sign-off.
- Question for reviewer: Should the brand name be locked *before* T012 produces the wordmark, or is the rename PR cheap enough that placeholder-then-rename is fine?

**Ceremony view + TempData fresh-vs-bookmark mechanism** ([research.md §6](research.md), [tasks.md T077-T078](tasks.md), [data-model.md §4](data-model.md))

A dedicated `FundingAgreementController.SignCeremony(Guid)` action reads `TempData["CeremonyFresh"]` to decide whether to mount confetti + ticker + spring-fill, or render the bookmark-safe static summary. TempData survives one redirect and self-clears on read.
- Question for reviewer: Are there flows where a Sign POST could legitimately fail mid-flight and leave the user landing on the ceremony URL with stale TempData? E.g., partial PDF persistence + redirect race? The spec assumes the POST is atomic.

**Sibling `IJourneyStageResolver` (not extension of `IStatusDisplayResolver`)** ([research.md §7](research.md), [contracts/projection-services.md](contracts/projection-services.md), [tasks.md T032-T034](tasks.md))

A new resolver lives next to spec-008's status resolver, both depending on a shared `IStageMappingProvider` for the canonical mapping. The alternative — extending `IStatusDisplayResolver` — was rejected to keep branch logic isolated.
- Question for reviewer: Is "two resolvers + one provider" the long-term right shape, or will downstream consumers want a single resolver they can ask "give me the visual for this app right now"? If the latter, would a future merge be painful?

**HTML restructuring is permitted and encouraged** ([FR-019](spec.md#functional-requirements), [FR-021](spec.md#functional-requirements), [research.md §10](research.md))

POMs migrate to ARIA roles + accessible names; `data-testid` only as a fallback; CSS selectors banned outside test-internal helpers. ~20 existing POMs are rewritten.
- Question for reviewer: Will the POM rewrite scope (every existing POM under `tests/FundingPlatform.Tests.E2E/PageObjects/`, [tasks.md T104-T119, T121](tasks.md)) be tracked task-by-task during US6, or is "rewrite alongside view sweep" sufficient? The current task structure couples each sweep task with its POM rewrite — efficient if both land together, but if a sweep ships and the POM lags, the test suite breaks for the duration.

**LCP/TBT performance baseline as Day-1 task** ([FR-073](spec.md#functional-requirements), [tasks.md T001](tasks.md), [tasks.md T129](tasks.md))

A baseline is captured before any spec-011 changes land. SC-015 enforces no surface regresses LCP or TBT by more than +10%.
- Question for reviewer: Is +10% a forgiving threshold for a feature that adds ~200 KB of fonts + 9 SVGs + an animation runtime, or is it tight enough to actually catch regressions?

### Areas where I'm less certain (5 min)

- [tasks.md T071](tasks.md): "Verify Mini variant is correctly composed by `_ApplicationCard` (US1) and Micro variant by `_ReviewerQueueRow` (US4 — confirm in Phase 6)." This task is **verification-only**; the underlying mini/micro rendering is implemented in T066. Is that the intended atomicity, or should T071 be folded into T066 to reduce ceremony? I left it as-is because it surfaces the cross-story integration risk.
- [tasks.md T065](tasks.md): "`DaysInCurrentState` shared utility on `JourneyProjector` (or sibling helper)". The "(or sibling helper)" leaves placement ambiguous — [data-model.md §5](data-model.md) shows it as a static method but references `application.CreatedAt` without `application` being in scope. Implementer may need to thread the `Application` reference. Worth tightening?
- [research.md §5.1](research.md) commits to in-team SVG illustration with a Lucide/Tabler-icon fallback documented as a deviation. The fallback path is fine, but the spec is silent on *who* the in-team author is. If that capacity is unavailable, will the deviation be discovered late (during a US6 sweep task) or upfront?
- [contracts/motion-catalog.md](contracts/motion-catalog.md) lists 14 catalog entries. [FR-013](spec.md#functional-requirements) says new motion outside the catalog requires spec amendment. During the sweep ([tasks.md T104-T120](tasks.md)) implementers will likely encounter situations where an existing animation doesn't quite match any catalog entry. The plan doesn't pre-empt this — is "amend the catalog mid-implementation" the expected workflow?

### Risks and open questions (5 min)

- If `canvas-confetti` (or the chosen ≤ 5 KB-gz equivalent) is GPL-licensed or has a non-permissive license, [tasks.md T005](tasks.md) needs a license review before vendoring. The plan picks `canvas-confetti` ([plan.md Technical Context](plan.md#technical-context)) as MIT — is that confirmed for the version pinned, or pinned during T005?
- The Aging KPI source-of-truth contract ([FR-053](spec.md#functional-requirements), [data-model.md §6](data-model.md)) reads `AgingThresholdDays` once per request from `ISystemConfigurationRepository`. If a spec-010-level config caching layer exists (it usually does), is "once per request" actually the read pattern, or will the projection accidentally hit the cache and miss live updates that SC-010 verifies?
- [FR-074](spec.md#functional-requirements) sets the asset budget at 400 KB combined gzipped; [plan.md Technical Context](plan.md#technical-context) breaks it down to ~275 KB rough estimate, leaving 125 KB headroom. WOFF2 subsetting can vary 30%+ by Latin-1-only vs. Latin-extended choice. Is the subset definition pinned tightly enough in [research.md §1.3](research.md) to hit 80/70/50 KB confidently?
- The plan says POMs touched by removed surfaces are "deprecated" ([research.md §10](research.md)) — the tasks delete neither test files nor POMs. Will dead POMs accrue if the sweep replaces a view (e.g., Review/Index → QueueDashboard) without explicitly cleaning up `ReviewIndexPage.cs` if such a file exists? Worth a single explicit "delete deprecated POMs" task.
- [SC-021](spec.md#measurable-outcomes) requires designer/product first-paint review on the four SC-009 fixtures. [tasks.md T131](tasks.md) adds a `data-testid="dashboard-elements-on-first-paint"` marker — but this seems to be a *test* hook, not a *human review* hook. Does the marker actually serve SC-021's review intent, or is this a misfit?

## Prior Review Feedback

Spec-level review (REVIEW-SPEC.md) flagged three Important items, all resolved in the spec; three Optional items deferred to plan. Plan-level resolution of the Optional items:

| # | Reviewer | Original Concern | How Addressed | Spec/Plan Location |
|---|----------|-----------------|---------------|---------------|
| 1 | Claude (review-spec) | FR-039 OR-clause "scroll to (or highlight)" should pin behavior | Pinned to "scroll into view AND apply 1.5 s `.is-highlighted` class" — both required | [plan.md Open Questions Resolved Here](plan.md#open-questions-resolved-here), [tasks.md T068](tasks.md), [contracts/partials.md](contracts/partials.md) |
| 2 | Claude (review-spec) | FR-070 needs verification handle for theme-able tokens | Added greppable rule for value-bound names; runs in `verify-tokens.sh` | [research.md §3](research.md), [tasks.md T006](tasks.md), [tasks.md T047](tasks.md) |
| 3 | Claude (review-spec) | `canvas-confetti` should be documented in plan's Constitution Check | Logged explicitly under "Net technology additions" | [plan.md Constitution Check](plan.md#constitution-check) |

---
*Full context in linked [spec](spec.md), [plan](plan.md), and [tasks](tasks.md).*
