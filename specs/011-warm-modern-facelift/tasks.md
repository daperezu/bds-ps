---
description: "Task list for the Warm-Modern Facelift (spec 011)"
---

# Tasks: Warm-Modern Facelift

**Input**: Design documents from `/specs/011-warm-modern-facelift/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: REQUIRED. Constitution III mandates E2E coverage for every user story. FR-021 + SC-017 mandate the POM rewrite + new wow-moment tests + reduced-motion test + axe contrast.

**Organization**: Tasks are grouped by user story. US5 is the foundational story (Phase 2) — all other stories depend on it. US7 lives alongside US5 in Phase 2 since US1/US4/US6 consume the illustrations.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps task to user story (US1..US7). Setup/foundational/polish tasks may omit the label.
- File paths are absolute relative to repo root.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture pre-implementation baselines, vendor static assets, scaffold token gates.

- [ ] T001 Capture pre-implementation LCP and TBT baseline for the four wow-moment surfaces and the layout shell into `specs/011-warm-modern-facelift/perf-baseline.json` per FR-073 (use Playwright tracing or Lighthouse-CI; document the script in `scripts/capture-perf-baseline.mjs`)
- [ ] T002 [P] Vendor Fraunces variable font (Latin-1 + numerals subset) under `src/FundingPlatform.Web/wwwroot/lib/fonts/fraunces/` with WOFF2 + LICENSE file (SIL OFL)
- [ ] T003 [P] Vendor Inter variable font (Latin-1 + numerals subset) under `src/FundingPlatform.Web/wwwroot/lib/fonts/inter/` with WOFF2 + LICENSE file (SIL OFL)
- [ ] T004 [P] Vendor JetBrains Mono regular (Latin-1 + digits + symbols subset) under `src/FundingPlatform.Web/wwwroot/lib/fonts/jetbrains-mono/` with WOFF2 + LICENSE file (Apache 2.0)
- [ ] T005 [P] Vendor `canvas-confetti` (≤ 5 KB gzipped, MIT) under `src/FundingPlatform.Web/wwwroot/lib/canvas-confetti/canvas-confetti.min.js` with LICENSE file
- [ ] T006 Add `scripts/verify-tokens.sh` running the SC-001/SC-002/SC-003/FR-070 grep gates (raw hex, inline style=, hard-coded durations, value-bound token names) — fails non-zero on any violation
- [ ] T007 [P] Add `scripts/verify-illustrations.sh` checking each SVG under `wwwroot/lib/illustrations/` is ≤ 8 KB gzipped (SC-007) — runs `gzip -c <file> | wc -c`
- [ ] T008 [P] Add `scripts/verify-pdf-carveouts.sh` running `git diff main -- <carve-out files>` and asserting empty output (FR-020, SC-014)
- [ ] T009 Wire all four `scripts/verify-*.sh` into a single `scripts/verify-facelift.sh` aggregator and document its invocation in `quickstart.md` (already drafted)

---

## Phase 2: Foundational — US5 (Brand Identity, Design Tokens, Motion System) + US7 (Illustration Set)

**Purpose**: Land the design-token layer, motion system, font self-hosting, brand voice deliverable, and the 9-scene illustration set. **Blocks every other user story.**

**⚠️ CRITICAL**: No work on US1/US2/US3/US4/US6 can begin until this phase is complete.

### US5 — Brand identity & voice

- [ ] T010 [US5] Author `specs/011-warm-modern-facelift/BRAND-VOICE.md` per the outline in `research.md §1.5` (FR-004): tone, person, stage-aware patterns, banned constructs (ALL CAPS, exclamation marks except ceremony, "submit" CTAs, passive voice), examples, do/don't pairs
- [ ] T011 [P] [US5] Author `specs/011-warm-modern-facelift/SWEEP-CHECKLIST.md` listing every view in the FR-018 inventory with the seven swept criteria as check items (FR-023)
- [ ] T012 [P] [US5] Produce SVG wordmark for "Forge" using Fraunces 600 at `src/FundingPlatform.Web/wwwroot/lib/brand/wordmark.svg` (FR-005)
- [ ] T013 [P] [US5] Produce SVG abstract mark (rising open-arc, two-stroke `--color-primary` → `--color-accent`) at `src/FundingPlatform.Web/wwwroot/lib/brand/mark.svg`
- [ ] T014 [P] [US5] Produce favicon set (16, 32, 48, 180 px PNGs + .ico) under `src/FundingPlatform.Web/wwwroot/lib/brand/favicons/` (FR-005)

### US5 — Design tokens

- [ ] T015 [US5] Create `src/FundingPlatform.Web/wwwroot/css/tokens.css` declaring all CSS custom properties: `--color-*` (palette in research §1.2), `--space-*` (T-shirt scale on 8 px base), `--radius-*`, `--shadow-*`, `--type-*` (family + scale tokens from research §1.3), `--motion-*` and `--ease-*` (motion-catalog values), `--z-*` (FR-006)
- [ ] T016 [US5] Add `@font-face` declarations for Fraunces, Inter, JetBrains Mono inside `tokens.css` referencing the vendored WOFF2 files; declare `--font-display`, `--font-body`, `--font-mono` family tokens (FR-007)
- [ ] T017 [US5] Add the Tabler bridge `:root` block to `tokens.css` overriding the 12 properties from research §2.1 (`--tblr-primary`, `--tblr-primary-rgb`, `--tblr-secondary`, `--tblr-success`, `--tblr-warning`, `--tblr-danger`, `--tblr-info`, `--tblr-body-bg`, `--tblr-body-color`, `--tblr-border-color`, `--tblr-card-bg`, `--tblr-link-color`) (FR-008)
- [ ] T018 [US5] Add the `prefers-reduced-motion: reduce` media query to `tokens.css` clamping every `--motion-*` token to `0ms` and preserving `--motion-opacity-exempt: 150ms` (FR-015)
- [ ] T019 [US5] Re-template `src/FundingPlatform.Web/wwwroot/css/site.css` to consume only `var(--…)` tokens — remove the spec-008 raw `font-size: 2.5rem` literals and any other hex / px / ms values (FR-007, SC-001)
- [ ] T020 [US5] Update `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` to load `tokens.css` BEFORE `site.css` and BEFORE Tabler's CSS so the bridge overrides apply

### US5 — Motion system & token-only partial re-template

- [ ] T021 [US5] Create `src/FundingPlatform.Web/wwwroot/js/motion.js` with the number-ticker, journey-stagger, and reduced-motion guard helpers (no hard-coded durations — reads `--motion-*` via `getComputedStyle`)
- [ ] T022 [US5] Create `src/FundingPlatform.Web/wwwroot/js/facelift-init.js` that mounts `data-ticker-target` KPI tickers and the filter-chip reflow handler on `DOMContentLoaded`
- [ ] T023 [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_StatusPill.cshtml` to switch the enum-to-color mapping from raw color references to `--color-*-subtle` token references (FR-011)
- [ ] T024 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_PageHeader.cshtml` to reference only `var(--…)` tokens (FR-007)
- [ ] T025 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_DataTable.cshtml` to reference only `var(--…)` tokens (FR-007)
- [ ] T026 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_FormSection.cshtml` to reference only `var(--…)` tokens (FR-007)
- [ ] T027 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_DocumentCard.cshtml` to reference only `var(--…)` tokens
- [ ] T028 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_ActionBar.cshtml` to reference only `var(--…)` tokens
- [ ] T029 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_ConfirmDialog.cshtml` to reference only `var(--…)` tokens
- [ ] T030 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_KpiTile.cshtml` to reference only `var(--…)` tokens AND add the `data-ticker-target` attribute hook for the number-ticker (FR-026)
- [ ] T031 [P] [US5] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_EventTimeline.cshtml` to reference only `var(--…)` tokens AND add an optional `EventTimelineScope Scope` parameter (Application / Applicant / ReviewerQueue) per `contracts/partials.md`

### US5 — Stage mapping & journey-stage resolver scaffolding

- [ ] T032 [US5] Create `src/FundingPlatform.Application/Services/StageMappingProvider.cs` implementing `IStageMappingProvider` with the canonical mainline + branch tables from `contracts/projection-services.md` (FR-036)
- [ ] T033 [US5] Create `src/FundingPlatform.Application/Services/JourneyStageResolver.cs` implementing `IJourneyStageResolver` (depends on `IStageMappingProvider`)
- [ ] T034 [US5] Register `IStageMappingProvider`, `IJourneyStageResolver` in DI inside `FundingPlatform.Application/DependencyInjection.cs` (or whichever spec-008 service registration extension exists; create one if not)

### US7 — Illustration set & helper

- [ ] T035 [US7] Create the `Illustration` Razor extension method at `src/FundingPlatform.Web/Helpers/IllustrationHelper.cs` per `contracts/illustration-helper.md` (FR-063): inline SVG load, decorative vs informational accessibility, scene-key registry
- [ ] T036 [P] [US7] Author `seed.svg` (seedling/sprout) at `src/FundingPlatform.Web/wwwroot/lib/illustrations/seed.svg` per style discipline (FR-061, FR-062)
- [ ] T037 [P] [US7] Author `folders-stack.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/folders-stack.svg`
- [ ] T038 [P] [US7] Author `open-envelope.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/open-envelope.svg`
- [ ] T039 [P] [US7] Author `connected-nodes.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/connected-nodes.svg`
- [ ] T040 [P] [US7] Author `calm-horizon.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/calm-horizon.svg`
- [ ] T041 [P] [US7] Author `soft-bar-chart.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/soft-bar-chart.svg`
- [ ] T042 [P] [US7] Author `off-center-compass.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/off-center-compass.svg`
- [ ] T043 [P] [US7] Author `gentle-disconnected-wires.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/gentle-disconnected-wires.svg`
- [ ] T044 [P] [US7] Author `magnifier-on-empty.svg` at `src/FundingPlatform.Web/wwwroot/lib/illustrations/magnifier-on-empty.svg`
- [ ] T045 [US7] Re-template `src/FundingPlatform.Web/Views/Shared/Components/_EmptyState.cshtml` to require an `IllustrationSceneKey` parameter (with the icon-only fallback path preserved for `_AuthLayout` AccessDenied) and add the entrance animation hooks (FR-064, FR-065)
- [ ] T046 [US7] Run `scripts/verify-illustrations.sh` and confirm all 9 SVGs satisfy the ≤ 8 KB gz budget (SC-007)

### US5 — Token-discipline gate

- [ ] T047 [US5] Run `scripts/verify-tokens.sh` from repo root and confirm all four greps return zero matches outside the carve-outs (SC-001, SC-002, SC-003, FR-070)

**Checkpoint**: Foundation ready — token + motion + brand-voice + illustrations exist; all spec-008 partials are token-only; the journey-stage resolver and illustration helper are wired. **All other user stories may now begin.**

---

## Phase 3: US1 — Applicant Home Dashboard (Priority: P1) 🎯 MVP

**Goal**: Replace the empty applicant home page with a dashboard that shows where the entrepreneur is in their funding journey and what to do next.

**Independent Test**: Log in as an applicant with one or more applications; the dashboard renders welcome hero, KPI strip, awaiting-action callout (when applicable), application cards (max 3 with "show all" link), recent activity feed (max 10 with show-more), and resources strip. Log in as an applicant with zero applications; the welcome scene with seed illustration, single CTA, and trust strip renders.

### Implementation for US1

- [ ] T048 [US1] Add view-model records `ApplicantDashboardDto`, `KpiSnapshot`, `AwaitingAction`, `ApplicationCardDto`, `ContextualAction`, `ActivityEvent` to `src/FundingPlatform.Application/DTOs/ApplicantDashboardDto.cs` per `data-model.md §2`
- [ ] T049 [US1] Implement `IApplicantCopyProvider` + `ApplicantCopyProvider` at `src/FundingPlatform.Application/Services/ApplicantCopyProvider.cs` (welcome strings, awaiting-action message templates, CTA labels — voice-guide compliant)
- [ ] T050 [US1] Implement `IJourneyProjector` + `JourneyProjector` at `src/FundingPlatform.Application/Services/JourneyProjector.cs` (single-app + ProjectMany batch path; depends on `IJourneyStageResolver` and `IStageMappingProvider`)
- [ ] T051 [US1] Implement `IApplicantDashboardProjection` + `ApplicantDashboardProjection` at `src/FundingPlatform.Application/Services/ApplicantDashboardProjection.cs` per `contracts/projection-services.md` (FR-024..FR-033)
- [ ] T052 [US1] Register `IApplicantCopyProvider`, `IJourneyProjector`, `IApplicantDashboardProjection` in DI
- [ ] T053 [P] [US1] Create `src/FundingPlatform.Web/Views/Shared/Components/_ApplicantHero.cshtml` rendering welcome strip + KPI strip + awaiting-action callout per `contracts/partials.md` (FR-025, FR-026, FR-027, FR-032)
- [ ] T054 [P] [US1] Create `src/FundingPlatform.Web/Views/Shared/Components/_ApplicationCard.cshtml` rendering rich card with embedded mini journey + status pill + days-in-state + last-activity + contextual action (FR-028, FR-032)
- [ ] T055 [P] [US1] Create `src/FundingPlatform.Web/Views/Shared/Components/_ResourcesStrip.cshtml` rendering 3 resources cards (FR-030)
- [ ] T056 [US1] Create `src/FundingPlatform.Web/Views/Home/ApplicantDashboard.cshtml` composing `_ApplicantHero`, application cards, `_EventTimeline` with `Scope=Applicant`, and `_ResourcesStrip` (FR-024)
- [ ] T057 [US1] Update `HomeController.Index` action to detect Applicant role and render `ApplicantDashboard.cshtml` with `ApplicantDashboardDto` from `IApplicantDashboardProjection`; non-applicant roles route to their existing landing surface (FR-024 + edge case "Applicant role demoted while viewing the dashboard")
- [ ] T058 [US1] Implement empty-state branch in `ApplicantDashboard.cshtml`: when `ActiveApplications.Count == 0`, render full-bleed welcome scene with `seed` illustration, Fraunces hero "Ready to apply for funding?", single "Start a new application" CTA, and 3-card trust strip (FR-031)
- [ ] T059 [US1] Wire up KPI ticker via `data-ticker-target` attributes on the four KPI tiles; capped at 60 frames, suppressed under reduced-motion (FR-026)

### Tests for US1

- [ ] T060 [P] [US1] Create new POM `tests/FundingPlatform.Tests.E2E/PageObjects/Applicant/ApplicantDashboardPage.cs` with semantic locators (ARIA roles + names; `data-testid` only as fallback) per research §10
- [ ] T061 [P] [US1] Add fixture `tests/FundingPlatform.Tests.E2E/Fixtures/ApplicantDashboardFixtures.cs` seeding the four reference fixtures (zero / 1 active / 2-3 active / many-with-show-all) for SC-009
- [ ] T062 [US1] Add `tests/FundingPlatform.Tests.E2E/Tests/Applicant/ApplicantDashboardTests.cs` covering: zero-applications welcome scene; 2 active apps render KPI counts + cards + activity feed; awaiting-action callout shows for sign-ready agreement (US1.AS3); KPI tickers animate to final values (and skip animation under reduced-motion fixture); show-all link present when > 3 apps

**Checkpoint**: US1 fully functional and testable independently against the four fixtures.

---

## Phase 4: US2 — Application Journey Timeline (Priority: P1)

**Goal**: A visual timeline of an application's lifecycle across mainline stages and branch states (Sent back / Rejected / Appeal), with three variants (Full / Mini / Micro).

**Independent Test**: Open application detail in each of the seven mainline stages and three branch states. Confirm correct rendering, hover tooltips, click-to-event-log scroll-and-highlight, and reduced-motion suppression. Verify Mini variant in `_ApplicationCard` and Micro variant in queue rows.

### Implementation for US2

- [ ] T063 [US2] Add view-model records `JourneyViewModel`, `JourneyNode`, `JourneyBranch`, and enums `JourneyStage`, `JourneyNodeState`, `JourneyBranchKind`, `JourneyBranchState`, `JourneyVariant` to `src/FundingPlatform.Application/DTOs/JourneyViewModel.cs` per `data-model.md §1`
- [ ] T064 [US2] Implement branch-resolution logic in `JourneyStageResolver` for Sent-back loops (count loops, render most-recent), Rejected (terminal when no Appeal), and Appeal (active / resolved-Approved-rejoins-mainline / upheld-terminal) per data-model §1
- [ ] T065 [US2] Add the `DaysInCurrentState` shared utility on `JourneyProjector` (or sibling helper); cache per-request to avoid repeated VersionHistory scans (FR-028, FR-056)
- [ ] T066 [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_ApplicationJourney.cshtml` with branching variant rendering: Full (all features), Mini (dots + connector + current label), Micro (dots only, current enlarged) per `contracts/partials.md` (FR-034, FR-037, FR-040, FR-041)
- [ ] T067 [US2] Add hover/focus tooltip behavior to Full variant nodes — sourcing timestamp + actor name from `JourneyNode` (FR-038)
- [ ] T068 [US2] Add click-to-event-log scroll-and-highlight behavior to Full variant completed nodes — both behaviors required: scroll into view AND apply `.is-highlighted` for 1.5 s (FR-039 with research-pinned both-behaviors decision)
- [ ] T069 [US2] Add mount stagger animation to Full variant in `motion.js` — completed nodes fill in with 60 ms stagger using `--ease-spring`, total duration capped at `--motion-slow`; suppressed under reduced-motion (FR-042)
- [ ] T070 [US2] Embed Full variant into `src/FundingPlatform.Web/Views/Application/Details.cshtml` (replace any existing status-pill-only display) and the existing review surfaces (`Views/Review/Details*` if present)
- [ ] T071 [US2] Verify Mini variant is correctly composed by `_ApplicationCard` (US1) and Micro variant by `_ReviewerQueueRow` (US4 — confirm in Phase 6)

### Tests for US2

- [ ] T072 [P] [US2] Create new POM `tests/FundingPlatform.Tests.E2E/PageObjects/Application/JourneyTimelineSection.cs` exposing semantic actions (e.g., `.NodeAt(stage).Tooltip`, `.ClickCompletedNode(stage)`) and used by both Application and Review detail POMs
- [ ] T073 [P] [US2] Add fixture `tests/FundingPlatform.Tests.E2E/Fixtures/JourneyFixtures.cs` covering all 7 mainline stages + 3 branch types (sent-back active, rejected terminal, appeal-active, appeal-resolved-approved, appeal-upheld, both-appeal-and-sent-back)
- [ ] T074 [US2] Add `tests/FundingPlatform.Tests.E2E/Tests/Application/JourneyTimelineTests.cs` covering: each mainline stage renders correct node states; sent-back sub-track at Decision; rejected sub-track terminal; appeal-resolved rejoins mainline; appeal-upheld terminal; click-completed-node scrolls + highlights; tooltip shows timestamp + actor; Mini variant shows dots+connector+current-label only; Micro variant shows dots only with current enlarged

**Checkpoint**: US2 functional across all states. Mini and Micro variants ready for US1 / US4 consumption.

---

## Phase 5: US3 — Signing Ceremony Moment (Priority: P1)

**Goal**: Replace the post-Sign flash-message with a peak-moment ceremony view (variant-aware copy, confetti when both signed, animated number ticker, calendar-anchored "what happens next").

**Independent Test**: Trigger each of the four signing variants. Confirm hero copy, confetti presence/absence, summary ticker, action footer. Bookmark the URL and re-visit; confirm non-animated summary state. Confirm reduced-motion path: confetti not mounted, static seal substituted, ticker shows final amount, focus moves to primary action.

### Implementation for US3

- [ ] T075 [US3] Add view-model record `SigningCeremonyViewModel` and enum `SigningCeremonyVariant` to `src/FundingPlatform.Application/DTOs/SigningCeremonyViewModel.cs` per `data-model.md §4`
- [ ] T076 [US3] Implement `ICeremonyCopyProvider` + `CeremonyCopyProvider` at `src/FundingPlatform.Application/Services/CeremonyCopyProvider.cs` (variant-aware hero/subhead copy per FR-046)
- [ ] T077 [US3] Add `FundingAgreementController.SignCeremony(Guid applicationId)` action returning `Sign/Ceremony.cshtml` with `SigningCeremonyViewModel` (research §6.1)
- [ ] T078 [US3] Update the existing `FundingAgreementController.Sign` POST success path to set `TempData["CeremonyFresh"] = true` and `RedirectToAction(nameof(SignCeremony), new { applicationId })` (research §6.2, FR-047)
- [ ] T079 [US3] Compute `SigningCeremonyVariant` server-side based on which signature was last (existing `SignedUpload` aggregates) — controller logic in `SignCeremony` action
- [ ] T080 [US3] Create `src/FundingPlatform.Web/Views/Shared/Components/_SigningCeremony.cshtml` rendering hero seal + variant-aware copy + funding summary card + "what happens next" card + action footer (FR-045, FR-051)
- [ ] T081 [US3] Create `src/FundingPlatform.Web/Views/FundingAgreement/Sign/Ceremony.cshtml` composing `_SigningCeremony` and threading `IsFresh` from TempData
- [ ] T082 [US3] Add ceremony-mount logic to `motion.js`: drawn-in seal animation, single-shot confetti via `canvas-confetti` (≤ 2 s), animated number ticker — all gated by `IsFresh` AND non-reduced-motion; otherwise render static final state and move focus to primary action (FR-045, FR-048)
- [ ] T083 [US3] Add aria-live="polite" announcement region to `_SigningCeremony` partial; mark confetti canvas (when present) `aria-hidden="true"`; bind ESC key to navigate to `DashboardHref` (FR-049)
- [ ] T084 [US3] Add static seal SVG asset at `src/FundingPlatform.Web/wwwroot/lib/brand/seal.svg` (used as confetti fallback under reduced-motion or bookmark re-visit)

### Tests for US3

- [ ] T085 [P] [US3] Create new POM `tests/FundingPlatform.Tests.E2E/PageObjects/Signing/CeremonyPage.cs` with semantic actions (`.HeroHeadline`, `.HasConfetti`, `.FundedAmountText`, `.PressEsc()`, etc.)
- [ ] T086 [P] [US3] Add fixture `tests/FundingPlatform.Tests.E2E/Fixtures/CeremonyFixtures.cs` seeding the four signing variants (applicant-only, funder-only, both-applicant-last, both-funder-last)
- [ ] T087 [US3] Add `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreement/SigningCeremonyTests.cs` covering: each of 4 variants renders correct hero copy + correct confetti presence; bookmark re-visit shows non-animated summary state without re-firing celebration; ESC navigates to dashboard; aria-live region announces signing event; static seal substitutes for animated seal under reduced-motion (smoke check; full reduced-motion verification in T127)

**Checkpoint**: US3 functional for all 4 variants + bookmark re-visit + reduced-motion behavior.

---

## Phase 6: US4 — Reviewer Queue Dashboard (Priority: P1)

**Goal**: Replace `Views/Review/Index` with a heads-up queue dashboard: welcome strip, KPI strip (Awaiting your review / In progress / Aging > N / Decided this month), filter chip strip with reflow, queue table with embedded micro journey timeline per row.

**Independent Test**: Log in as a reviewer with mixed-state assignments. Confirm dashboard layout, KPI counts, filter chip selection (no full reload), Aging KPI follows spec-010 `AgingThresholdDays` (verify by changing the seed value), inline micro journey on each row, hover lift, click-anywhere-row navigates to Review details. Log in with zero items → calm-horizon empty-state.

### Implementation for US4

- [ ] T088 [US4] Add view-model records `ReviewerQueueDto`, `ReviewerKpiSnapshot`, `ReviewerQueueRowDto`, `ReviewerActivityEvent` and enum `ReviewerFilter` to `src/FundingPlatform.Application/DTOs/ReviewerQueueDto.cs` per `data-model.md §3`
- [ ] T089 [US4] Implement `IReviewerCopyProvider` + `ReviewerCopyProvider` at `src/FundingPlatform.Application/Services/ReviewerCopyProvider.cs` (welcome string, filter chip labels, empty-state copy)
- [ ] T090 [US4] Implement `IReviewerQueueProjection` + `ReviewerQueueProjection` at `src/FundingPlatform.Application/Services/ReviewerQueueProjection.cs` per `contracts/projection-services.md` (FR-052..FR-060); reads `AgingThresholdDays` once per request from `ISystemConfigurationRepository`
- [ ] T091 [US4] Register `IReviewerCopyProvider`, `IReviewerQueueProjection` in DI
- [ ] T092 [P] [US4] Create `src/FundingPlatform.Web/Views/Shared/Components/_ReviewerHero.cshtml` rendering welcome strip + KPI strip + filter chip strip (FR-052..FR-054, FR-059)
- [ ] T093 [P] [US4] Create `src/FundingPlatform.Web/Views/Shared/Components/_ReviewerQueueRow.cshtml` rendering rich row with embedded `_ApplicationJourney` (Variant=Micro) replacing the status-pill column (FR-056, FR-059)
- [ ] T094 [P] [US4] Create `src/FundingPlatform.Web/Views/Review/_ReviewerQueueRows.cshtml` (tbody-only partial used by chip-reflow endpoint)
- [ ] T095 [US4] Create `src/FundingPlatform.Web/Views/Review/QueueDashboard.cshtml` composing `_ReviewerHero`, `_EventTimeline` with `Scope=ReviewerQueue`, `_DataTable` with density-forward styling (`--space-2` padding) and the queue rows partial (FR-052, FR-056, FR-060)
- [ ] T096 [US4] Update `ReviewController.Index` to render `QueueDashboard.cshtml` with `ReviewerQueueDto` from `IReviewerQueueProjection`
- [ ] T097 [US4] Add `ReviewController.QueueRows(ReviewerFilter filter)` returning `_ReviewerQueueRows.cshtml` partial for chip-reflow contract (FR-054)
- [ ] T098 [US4] Wire chip-click handler in `facelift-init.js` to GET `/Review/QueueRows?filter=…` and swap `<tbody>` content; animate row reflow with `--motion-base`; suppress animation under reduced-motion (FR-054)
- [ ] T099 [US4] Implement empty-state in `QueueDashboard.cshtml`: when `Rows.Count == 0`, render `_EmptyState` partial with `calm-horizon` illustration and "All clear — nothing's awaiting your review." (FR-058)
- [ ] T100 [US4] Implement row hover lift (`--shadow-md`) and row click-anywhere-navigates-to-Review-details behavior in `_ReviewerQueueRow.cshtml` (FR-057)

### Tests for US4

- [ ] T101 [P] [US4] Create new POM `tests/FundingPlatform.Tests.E2E/PageObjects/Reviewer/ReviewerQueueDashboardPage.cs` with semantic actions (`.AwaitingReviewCount`, `.SelectFilter(filter)`, `.RowAt(index).MicroJourney`, `.IsEmptyStateVisible`, etc.)
- [ ] T102 [P] [US4] Add fixture `tests/FundingPlatform.Tests.E2E/Fixtures/ReviewerQueueFixtures.cs` covering full queue, empty queue, only-aging, only-appealing, mixed-state
- [ ] T103 [US4] Add `tests/FundingPlatform.Tests.E2E/Tests/Review/ReviewerQueueDashboardTests.cs` covering: layout renders; KPI counts match seeded data; filter chip click reflows table without full page reload; Aging KPI follows configured threshold (test changes `AgingThresholdDays` seed and re-asserts); empty-state with calm-horizon illustration; row hover lift; row click navigates to Review details; chip-reflow animation suppressed under reduced-motion fixture

**Checkpoint**: US4 fully functional across the four reviewer fixtures + threshold-config-source-of-truth verification.

---

## Phase 7: US6 — Full Authenticated-View Sweep (Priority: P1)

**Goal**: Every view in the FR-018 inventory satisfies the seven swept criteria. PDF carve-outs remain byte-identical. POMs touched by the sweep are rewritten to semantic locators.

**Independent Test**: Walk every view in `SWEEP-CHECKLIST.md` and tick the seven criteria. Run `scripts/verify-tokens.sh` (zero matches). Run `scripts/verify-pdf-carveouts.sh` (zero diff). Re-render reference PDF and visually compare to stored reference (identical).

> US6 tasks are inherently many — the spec inventory has ~50 view files. Below they are grouped by area. Each group's task includes both the view re-template AND its POM rewrite. `[P]` is used liberally because each view file is independent.

### Sweep — Account / Auth surfaces (FR-018 anonymous group)

- [ ] T104 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Account/Login.cshtml` + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/LoginPage.cs` to clean focused single-CTA tone (FR-018, research §9); seven swept criteria
- [ ] T105 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Account/Register.cshtml` + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/RegisterPage.cs`
- [ ] T106 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml` + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/ChangePasswordPage.cs`
- [ ] T107 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml` (icon-only empty-state path retained — no decorative illustration on auth surfaces)
- [ ] T108 [P] [US6] Re-template `src/FundingPlatform.Web/Views/Shared/_AuthLayout.cshtml` per research §9 (token-only, no marketing hero)

### Sweep — Application area

- [ ] T109 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Application/Index.cshtml` (use `folders-stack` empty-state) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicationPage.cs` to semantic locators
- [ ] T110 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Application/Details.cshtml` (already hosts US2 Full per T070 — apply remaining swept criteria)
- [ ] T111 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Application/Create.cshtml`, `Edit.cshtml`, and `Delete.cshtml`

### Sweep — Item / Quotation / Supplier

- [ ] T112 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Item/*.cshtml` (Index uses `folders-stack`) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/ItemPage.cs`
- [ ] T113 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Quotation/*.cshtml` (Index uses `open-envelope`; spec-010 currency rendering preserved) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/QuotationPage.cs`
- [ ] T114 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Supplier/*.cshtml` (Index uses `connected-nodes`) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/SupplierPage.cs`

### Sweep — Review (non-Index) / ApplicantResponse / FundingAgreement (non-PDF)

- [ ] T115 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Review/Details.cshtml` (gets US2 Full embed) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs`
- [ ] T116 [P] [US6] Sweep `src/FundingPlatform.Web/Views/ApplicantResponse/*.cshtml` + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicantResponsePage.cs` and `AppealThreadPage.cs`
- [ ] T117 [P] [US6] Sweep `src/FundingPlatform.Web/Views/FundingAgreement/Index.cshtml`, `Generate.cshtml`, `Details.cshtml` (excluding `Document.cshtml` and `_FundingAgreementLayout.cshtml` carve-outs); rewrite `FundingAgreementPanelPage.cs`, `FundingAgreementDownloadFlow.cs`, `SigningStagePanelPage.cs`, `SigningReviewInboxPage.cs`

### Sweep — Admin (including spec-010 reports)

- [ ] T118 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Admin/*.cshtml` (Index, Users, Roles, SystemConfigurations) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/AdminPage.cs` and `Admin/AdminUserCreatePage.cs`
- [ ] T119 [P] [US6] Sweep `src/FundingPlatform.Web/Views/Admin/Reports/*.cshtml` (all spec-010 reports use `soft-bar-chart` empty-state) + rewrite `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminReportsPage.cs` and any per-report POM under `Admin/Reports/`

### Sweep — Shared layout & components leftovers

- [ ] T120 [US6] Sweep `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` and `Error.cshtml` (uses `gentle-disconnected-wires` for 500; `off-center-compass` for 404 if a 404 view exists)

### Sweep — POM common base + remaining

- [ ] T121 [P] [US6] Update `tests/FundingPlatform.Tests.E2E/PageObjects/BasePage.cs` to expose semantic-locator helpers (e.g., `Heading(name)`, `Button(name)`, `Link(name)`) used by all rewritten POMs
- [ ] T122 [P] [US6] Sweep verification — re-run `scripts/verify-tokens.sh`, `scripts/verify-illustrations.sh`, `scripts/verify-pdf-carveouts.sh` and confirm zero violations across the entire repo (SC-001, SC-002, SC-003, SC-007, SC-014)

### Sweep tests / verification

- [ ] T123 [US6] Run the SWEEP-CHECKLIST against every inventoried view and tick the seven swept criteria; record the completed checklist in the PR description (SC-006)
- [ ] T124 [US6] Verify PDF identity: regenerate the funding agreement PDF for a reference fixture and compare against the stored reference PDF (visually identical — SC-014). Document the result.

**Checkpoint**: Every authenticated view inherits the new tokens. PDF carve-outs preserved. POMs reformed to semantic locators. Token + illustration + carve-out gates pass clean.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Reduced-motion verification, contrast verification, performance regression check, schema-untouched verification, brand sign-off, asset-budget verification.

- [ ] T125 [P] Add `tests/FundingPlatform.Tests.E2E/Tests/Accessibility/ContrastTests.cs` running `axe-playwright` against the four wow-moment views and the layout shell (SC-013, FR-075). Vendor `axe-core` if not already present.
- [ ] T126 Add `tests/FundingPlatform.Tests.E2E/Tests/Motion/ReducedMotionTests.cs` running with `BrowserContextOptions { ReducedMotion = ReducedMotion.Reduce }` and asserting the catalog's reduced-motion behaviors (SC-012, FR-015): KPI tickers final values; journey timeline static; ceremony confetti not mounted; static seal present; empty-state entrance suppressed; chip reflow without animation.
- [ ] T127 [P] Asset-budget verification: confirm gzipped wire weight of fonts + 9 SVGs + canvas-confetti + brand assets ≤ 400 KB (SC-016, FR-074). Add `scripts/verify-asset-budget.sh`.
- [ ] T128 [P] Schema-unchanged verification: run `git diff --stat main -- src/FundingPlatform.Database/` and confirm empty output (SC-018, FR-067).
- [ ] T129 Performance verification: run `scripts/compare-perf.mjs specs/011-warm-modern-facelift/perf-baseline.json` and confirm no surface regresses LCP or TBT by more than +10% (SC-015, FR-073).
- [ ] T130 Voice-guide compliance: run a final pass over user-facing strings in all swept views against `BRAND-VOICE.md`; tick `SWEEP-CHECKLIST.md` voice-guide column for every view (SC-019). Update copy where any rule violates.
- [ ] T131 Add a `data-testid="dashboard-elements-on-first-paint"` marker (or equivalent) to the four SC-009 reference fixtures' dashboard shell so designer/product reviewers can run the SC-021 first-paint identifiability check; record review outcome in PR description.
- [ ] T132 Run full E2E suite with the `--EphemeralStorage=true` AppHost flag and confirm all previously-passing tests pass + the new wow-moment + reduced-motion + axe contrast tests pass (SC-017).
- [ ] T133 Add brand sign-off section to the PR description quoting the proposed name (per research §1.1), palette (research §1.2), and logo direction (research §1.4) — explicit user gate per FR-072 / SC-020.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — runs first.
- **Phase 2 (Foundational US5 + US7)**: Depends on Phase 1. **Blocks every other phase.**
- **Phase 3 (US1)**: Depends on Phase 2 completion. Independent of US2/US3/US4 once mini-journey is available (US2's mini variant is delivered in Phase 4, but US1's `_ApplicationCard` can stub the mini variant against a placeholder until T071 verifies it; or implement T066 Mini-variant pieces alongside US1 and merge).
- **Phase 4 (US2)**: Depends on Phase 2. Provides Mini variant for US1 and Micro variant for US4.
- **Phase 5 (US3)**: Depends on Phase 2. Independent of US1/US2/US4.
- **Phase 6 (US4)**: Depends on Phase 2 + Phase 4 (Micro variant from US2). May proceed against a placeholder Micro until T093 wires the real one.
- **Phase 7 (US6 sweep)**: Depends on Phase 2. Can run in parallel with Phases 3–6 (sweep tasks touch different files than wow-moment tasks). Sweep verification (T122..T124) MUST run after Phases 3–6 complete.
- **Phase 8 (Polish)**: Depends on Phases 3–7 complete.

### User Story Dependencies (deeper view)

- **US5** is foundational. Blocks all others.
- **US7** is independent of US5 by content but consumed by US1 / US4 / US6. Runs in parallel with US5 inside Phase 2.
- **US1** depends on US5 (tokens, partials), US7 (`seed`, plus other empty-states), and US2 Mini variant.
- **US2** depends on US5 only.
- **US3** depends on US5 only.
- **US4** depends on US5, US7 (`calm-horizon`), and US2 Micro variant.
- **US6** depends on US5 (token gate), US7 (illustration set), and may run alongside US1–US4 since each touches different views.

### Within Each User Story

- Application-layer DTOs and projection services come before view changes.
- View changes come before E2E tests.
- POM rewrite comes before tests that consume them.
- Commit after each task or logical group; checkpoint per user story.

### Parallel Opportunities (high-level)

- All Setup [P] tasks can run in parallel.
- US7 illustration authoring (T036–T044) can run in parallel.
- All spec-008 partial re-templates (T024–T031) can run in parallel after T015 (`tokens.css`) lands.
- After Phase 2 completes, **Phases 3, 4, 5, 7 can run in parallel.** Phase 6 starts when Phase 4 lands.
- US6 sweep tasks (T104–T120) are mostly [P] across non-overlapping view files.
- Polish gates (T125–T128) can run in parallel.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# After T010 (BRAND-VOICE) and T015 (tokens.css) land, fire all partial re-templates in parallel:
Task: "Re-template _PageHeader.cshtml to var(--…) tokens"
Task: "Re-template _DataTable.cshtml to var(--…) tokens"
Task: "Re-template _FormSection.cshtml to var(--…) tokens"
Task: "Re-template _DocumentCard.cshtml to var(--…) tokens"
Task: "Re-template _ActionBar.cshtml to var(--…) tokens"
Task: "Re-template _ConfirmDialog.cshtml to var(--…) tokens"
Task: "Re-template _KpiTile.cshtml to var(--…) tokens + ticker hook"
Task: "Re-template _EventTimeline.cshtml to var(--…) tokens + Scope param"

# Author all 9 illustrations in parallel (different files):
Task: "Author seed.svg per style discipline"
Task: "Author folders-stack.svg per style discipline"
# ...etc, all 9 SVG authors are independent...
```

## Parallel Example: After Phase 2

```bash
# Three parallel tracks, one per developer / agent stream:
Track A: Phase 3 (US1 — applicant home dashboard)
Track B: Phase 4 (US2 — journey timeline)
Track C: Phase 5 (US3 — signing ceremony)

# Phase 6 (US4) starts when Track B's micro variant lands (T066).
# Phase 7 (US6 sweep) starts immediately after Phase 2 — touches different files than Phases 3-5.
```

---

## Implementation Strategy

### MVP First — Track to a deployable applicant-home demo

1. Phase 1 (Setup) — vendored fonts, perf baseline, gates.
2. Phase 2 (Foundational US5 + US7) — tokens, motion, illustrations, voice guide.
3. Phase 4 (US2) — journey timeline component (Mini variant required by US1).
4. Phase 3 (US1) — applicant home dashboard.
5. **STOP and validate** — applicant flow looks warm-modern end-to-end.

### Incremental Delivery After MVP

1. Phase 5 (US3) → ceremony moment shipped.
2. Phase 6 (US4) → reviewer queue dashboard shipped.
3. Phase 7 (US6) → sweep across remaining views.
4. Phase 8 (Polish) → contrast, motion, perf, schema, brand sign-off, asset budget.

### Parallel Team Strategy

After Phase 2 completes:

- Dev A: Phase 4 (US2) → then Phase 6 (US4)
- Dev B: Phase 3 (US1) → then Phase 5 (US3)
- Dev C: Phase 7 (US6 sweep) — long-tail
- All converge in Phase 8 for the cross-cutting gates.

---

## Notes

- [P] tasks = different files, no cross-task dependencies.
- [Story] label maps task to its user story for traceability. Setup, foundational, and polish tasks may omit the label.
- Constitution III: every user story has E2E coverage (POM + Tests). No bypassing.
- Commit after each task or logical group of tasks; the spec-008 invariant of "checkpoint per story" applies here.
- PDF carve-out files (`Document.cshtml`, `_FundingAgreementLayout.cshtml`) MUST NOT be touched by ANY task. If a task incidentally requires editing them, escalate via `speckit-spex-evolve`.
- Brand sign-off (T133) is a merge gate. Not optional.
- Asset budget (T127) is a merge gate. Not optional.
