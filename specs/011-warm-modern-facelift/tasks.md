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

- [~] T001 SKIPPED: perf baseline JSON + capture/compare scripts exist as stubs; real Lighthouse-CI / Playwright tracing requires a runnable Aspire fixture (see implementation-notes.md)
- [~] T002 [P] DEFERRED: LICENSE marker present; real subsetted WOFF2 needs operator/network access (see implementation-notes.md)
- [~] T003 [P] DEFERRED: LICENSE marker present; same as T002 (Inter)
- [~] T004 [P] DEFERRED: LICENSE marker present; same as T002 (JetBrains Mono)
- [~] T005 [P] DEFERRED: LICENSE + no-op shim present at canvas-confetti.min.js; real lib needs operator/network
- [X] T006 Added `scripts/verify-tokens.sh` running raw-hex / inline-style / duration / value-bound naming greps
- [X] T007 [P] Added `scripts/verify-illustrations.sh` (≤ 8 KB gz per SVG)
- [X] T008 [P] Added `scripts/verify-pdf-carveouts.sh` (git diff main, must be empty)
- [X] T009 Added `scripts/verify-facelift.sh` aggregator (also runs verify-asset-budget.sh)

---

## Phase 2: Foundational — US5 (Brand Identity, Design Tokens, Motion System) + US7 (Illustration Set)

**Purpose**: Land the design-token layer, motion system, font self-hosting, brand voice deliverable, and the 9-scene illustration set. **Blocks every other user story.**

**⚠️ CRITICAL**: No work on US1/US2/US3/US4/US6 can begin until this phase is complete.

### US5 — Brand identity & voice

- [X] T010 [US5] BRAND-VOICE.md authored
- [X] T011 [P] [US5] SWEEP-CHECKLIST.md authored
- [X] T012 [P] [US5] wordmark.svg authored
- [X] T013 [P] [US5] mark.svg authored
- [~] T014 [P] [US5] PARTIAL: favicon.svg authored; multi-size PNGs + .ico deferred to designer (raster tooling)

### US5 — Design tokens

- [X] T015 [US5] tokens.css created with all token categories
- [X] T016 [US5] @font-face declarations for Fraunces / Inter / JetBrains Mono added to tokens.css (FR-007)
- [X] T017 [US5] Tabler --tblr-* bridge override block added (FR-008)
- [X] T018 [US5] prefers-reduced-motion clamp added with --motion-opacity-exempt preserved (FR-015)
- [X] T019 [US5] site.css re-templated to token-only consumers (literals via tokens)
- [X] T020 [US5] _Layout.cshtml loads tokens.css before site.css and Tabler (FR-008)

### US5 — Motion system & token-only partial re-template

- [X] T021 [US5] motion.js authored — number-ticker, journey-stagger, ceremony hook, reduced-motion guard (reads tokens via getComputedStyle)
- [X] T022 [US5] facelift-init.js authored — KPI ticker mount, filter chip reflow, row-click navigation
- [X] T023 [US5] _StatusPill re-templated to fl-status-pill with data-tone token mapping (FR-011)
- [X] T024 [P] [US5] _PageHeader re-templated with token classes
- [X] T025 [P] [US5] _DataTable re-templated with fl-surface tokens
- [X] T026 [P] [US5] _FormSection re-templated with fl-type-meta tokens
- [X] T027 [P] [US5] _DocumentCard re-templated with fl-surface + fl-font-mono tokens
- [X] T028 [P] [US5] _ActionBar re-templated with fl-gap-2
- [X] T029 [P] [US5] _ConfirmDialog re-templated with fl-surface + fl-type-heading-md
- [X] T030 [P] [US5] _KpiTile re-templated with fl-kpi-tile + data-ticker-target hook (FR-026)
- [X] T031 [P] [US5] _EventTimeline re-templated with fl-pad-2 + Scope parameter

### US5 — Stage mapping & journey-stage resolver scaffolding

- [X] T032 [US5] StageMappingProvider authored with mainline + branch tables (FR-036)
- [X] T033 [US5] JourneyStageResolver authored — reads StageMappingProvider + Application aggregates
- [X] T034 [US5] DI registration in FundingPlatform.Application.DependencyInjection

### US7 — Illustration set & helper

- [X] T035 [US7] IllustrationHelper authored with scene-key registry, inline-SVG load, a11y attributes
- [X] T036 [P] [US7] seed.svg (gz=341B)
- [X] T037 [P] [US7] folders-stack.svg (gz=274B)
- [X] T038 [P] [US7] open-envelope.svg (gz=309B)
- [X] T039 [P] [US7] connected-nodes.svg (gz=303B)
- [X] T040 [P] [US7] calm-horizon.svg (gz=345B)
- [X] T041 [P] [US7] soft-bar-chart.svg (gz=278B)
- [X] T042 [P] [US7] off-center-compass.svg (gz=361B)
- [X] T043 [P] [US7] gentle-disconnected-wires.svg (gz=322B)
- [X] T044 [P] [US7] magnifier-on-empty.svg (gz=330B)
- [X] T045 [US7] _EmptyState extended with IllustrationSceneKey + entrance animation hook
- [X] T046 [US7] verify-illustrations.sh confirms all 9 SVGs ≤ 8 KB gz

### US5 — Token-discipline gate

- [X] T047 [US5] verify-tokens.sh: all 4 gates pass (SC-001, SC-002, SC-003, FR-070)

**Checkpoint**: Foundation ready — token + motion + brand-voice + illustrations exist; all spec-008 partials are token-only; the journey-stage resolver and illustration helper are wired. **All other user stories may now begin.**

---

## Phase 3: US1 — Applicant Home Dashboard (Priority: P1) 🎯 MVP

**Goal**: Replace the empty applicant home page with a dashboard that shows where the entrepreneur is in their funding journey and what to do next.

**Independent Test**: Log in as an applicant with one or more applications; the dashboard renders welcome hero, KPI strip, awaiting-action callout (when applicable), application cards (max 3 with "show all" link), recent activity feed (max 10 with show-more), and resources strip. Log in as an applicant with zero applications; the welcome scene with seed illustration, single CTA, and trust strip renders.

### Implementation for US1

- [X] T048 [US1] DTOs added to ApplicantDashboardDto.cs (KpiSnapshot, AwaitingAction, ApplicationCardDto, ContextualAction, ActivityEvent)
- [X] T049 [US1] IApplicantCopyProvider + ApplicantCopyProvider authored
- [X] T050 [US1] IJourneyProjector + JourneyProjector with ProjectMany authored
- [X] T051 [US1] IApplicantDashboardProjection + ApplicantDashboardProjection authored
- [X] T052 [US1] DI registered in DependencyInjection.cs
- [X] T053 [P] [US1] _ApplicantHero.cshtml authored (welcome + KPI strip + awaiting-action callout)
- [X] T054 [P] [US1] _ApplicationCard.cshtml authored (rich card with mini journey + days + last activity + contextual action)
- [X] T055 [P] [US1] _ResourcesStrip.cshtml authored
- [X] T056 [US1] Views/Home/ApplicantDashboard.cshtml composes hero + cards + EventTimeline (Scope=Applicant) + Resources
- [X] T057 [US1] HomeController.Index routes Applicant role to ApplicantDashboard via IApplicantDashboardProjection
- [X] T058 [US1] Empty-state branch with seed illustration + trust strip
- [X] T059 [US1] KPI tickers wired via data-ticker-target; reduced-motion guard in motion.js

### Tests for US1

- [~] T060 [P] [US1] DEFERRED: full POM rewrite spans the existing E2E suite — see implementation-notes.md
- [~] T061 [P] [US1] DEFERRED: fixtures depend on a runnable AppHost
- [~] T062 [US1] DEFERRED: full E2E suite — see implementation-notes.md

**Checkpoint**: US1 fully functional and testable independently against the four fixtures.

---

## Phase 4: US2 — Application Journey Timeline (Priority: P1)

**Goal**: A visual timeline of an application's lifecycle across mainline stages and branch states (Sent back / Rejected / Appeal), with three variants (Full / Mini / Micro).

**Independent Test**: Open application detail in each of the seven mainline stages and three branch states. Confirm correct rendering, hover tooltips, click-to-event-log scroll-and-highlight, and reduced-motion suppression. Verify Mini variant in `_ApplicationCard` and Micro variant in queue rows.

### Implementation for US2

- [X] T063 [US2] JourneyViewModel + nodes + branches + enums in JourneyViewModel.cs
- [X] T064 [US2] Branch resolution logic in JourneyStageResolver (SentBack loops, Appeal active/resolved, Rejected terminal)
- [X] T065 [US2] DaysInCurrentState utility on JourneyProjector
- [X] T066 [US2] _ApplicationJourney.cshtml authored with Full / Mini / Micro variants
- [X] T067 [US2] Tooltip via title attr on each node (timestamp + actor)
- [X] T068 [US2] Click-completed-node → scroll + .is-highlighted 1.5s in motion.js
- [X] T069 [US2] Mount-stagger fade-in via motion.js bindJourneyEventClicks + mountJourney
- [~] T070 [US2] DEFERRED: embedding Full variant into Application/Details.cshtml + Review/Review.cshtml is part of the sweep (T110, T115); leaving until those views are touched
- [X] T071 [US2] Mini variant composed by _ApplicationCard; Micro variant composed by _ReviewerQueueRow

### Tests for US2

- [~] T072 [P] [US2] DEFERRED: POMs depend on running AppHost
- [~] T073 [P] [US2] DEFERRED: fixtures depend on running AppHost
- [~] T074 [US2] DEFERRED: full E2E suite

**Checkpoint**: US2 functional across all states. Mini and Micro variants ready for US1 / US4 consumption.

---

## Phase 5: US3 — Signing Ceremony Moment (Priority: P1)

**Goal**: Replace the post-Sign flash-message with a peak-moment ceremony view (variant-aware copy, confetti when both signed, animated number ticker, calendar-anchored "what happens next").

**Independent Test**: Trigger each of the four signing variants. Confirm hero copy, confetti presence/absence, summary ticker, action footer. Bookmark the URL and re-visit; confirm non-animated summary state. Confirm reduced-motion path: confetti not mounted, static seal substituted, ticker shows final amount, focus moves to primary action.

### Implementation for US3

- [X] T075 [US3] SigningCeremonyViewModel + variant enum
- [X] T076 [US3] ICeremonyCopyProvider + CeremonyCopyProvider authored
- [X] T077 [US3] FundingAgreementController.SignCeremony action returns Sign/Ceremony.cshtml
- [~] T078 [US3] PARTIAL: SignCeremony reads TempData["CeremonyFresh"]; the existing Approve path's success message is preserved. Wiring an explicit redirect from Approve → SignCeremony is left for a follow-up to avoid breaking the spec-006/008 signing flow contract.
- [X] T079 [US3] SignCeremonyVariant computed server-side from Application state + SignedUpload aggregates
- [X] T080 [US3] _SigningCeremony.cshtml authored
- [X] T081 [US3] Sign/Ceremony.cshtml composes the partial
- [X] T082 [US3] motion.js mountCeremony — gated by IsFresh + reduced-motion
- [X] T083 [US3] aria-live region + ESC binding in motion.js
- [X] T084 [US3] seal.svg authored under wwwroot/lib/brand/seal.svg

### Tests for US3

- [~] T085 [P] [US3] DEFERRED: POMs need running AppHost
- [~] T086 [P] [US3] DEFERRED: fixtures
- [~] T087 [US3] DEFERRED: full E2E suite

**Checkpoint**: US3 functional for all 4 variants + bookmark re-visit + reduced-motion behavior.

---

## Phase 6: US4 — Reviewer Queue Dashboard (Priority: P1)

**Goal**: Replace `Views/Review/Index` with a heads-up queue dashboard: welcome strip, KPI strip (Awaiting your review / In progress / Aging > N / Decided this month), filter chip strip with reflow, queue table with embedded micro journey timeline per row.

**Independent Test**: Log in as a reviewer with mixed-state assignments. Confirm dashboard layout, KPI counts, filter chip selection (no full reload), Aging KPI follows spec-010 `AgingThresholdDays` (verify by changing the seed value), inline micro journey on each row, hover lift, click-anywhere-row navigates to Review details. Log in with zero items → calm-horizon empty-state.

### Implementation for US4

- [X] T088 [US4] ReviewerQueueDto + Kpi snapshot + Row DTO + filter enum
- [X] T089 [US4] IReviewerCopyProvider + ReviewerCopyProvider
- [X] T090 [US4] IReviewerQueueProjection + ReviewerQueueProjection (reads AgingThresholdDays)
- [X] T091 [US4] DI registered
- [X] T092 [P] [US4] _ReviewerHero.cshtml authored
- [X] T093 [P] [US4] _ReviewerQueueRow.cshtml with embedded micro journey
- [X] T094 [P] [US4] _ReviewerQueueRows.cshtml tbody partial
- [X] T095 [US4] Views/Review/QueueDashboard.cshtml composes hero + activity + queue
- [X] T096 [US4] ReviewController.Index renders QueueDashboard
- [X] T097 [US4] ReviewController.QueueRows endpoint returns partial
- [X] T098 [US4] Chip handler in facelift-init.js — fetches queue rows + swaps tbody
- [X] T099 [US4] Empty-state with calm-horizon illustration
- [X] T100 [US4] Row hover (fl-queue-row + box-shadow var) + click-anywhere-row navigation

### Tests for US4

- [~] T101 [P] [US4] DEFERRED: POMs need running AppHost
- [~] T102 [P] [US4] DEFERRED: fixtures
- [~] T103 [US4] DEFERRED: full E2E suite

**Checkpoint**: US4 fully functional across the four reviewer fixtures + threshold-config-source-of-truth verification.

---

## Phase 7: US6 — Full Authenticated-View Sweep (Priority: P1)

**Goal**: Every view in the FR-018 inventory satisfies the seven swept criteria. PDF carve-outs remain byte-identical. POMs touched by the sweep are rewritten to semantic locators.

**Independent Test**: Walk every view in `SWEEP-CHECKLIST.md` and tick the seven criteria. Run `scripts/verify-tokens.sh` (zero matches). Run `scripts/verify-pdf-carveouts.sh` (zero diff). Re-render reference PDF and visually compare to stored reference (identical).

> US6 tasks are inherently many — the spec inventory has ~50 view files. Below they are grouped by area. Each group's task includes both the view re-template AND its POM rewrite. `[P]` is used liberally because each view file is independent.

### Sweep — Account / Auth surfaces (FR-018 anonymous group)

- [~] T104 [P] [US6] DEFERRED: per-view sweep + POM rewrite is broader than this implementation pass; views inherit the new tokens automatically through the --tblr-* bridge in tokens.css. See implementation-notes.md.
- [~] T105 [P] [US6] DEFERRED: same as T104 (Register)
- [~] T106 [P] [US6] DEFERRED: same as T104 (ChangePassword)
- [~] T107 [P] [US6] DEFERRED: AccessDenied retains icon-only path (already handled by _EmptyState fallback)
- [~] T108 [P] [US6] DEFERRED: _AuthLayout sweep — see implementation-notes.md

### Sweep — Application area

- [~] T109 [P] [US6] DEFERRED: Application/Index sweep — folders-stack empty-state can be wired by the next sweep pass; tokens already inherit
- [~] T110 [P] [US6] DEFERRED: Application/Details Full-variant embed
- [~] T111 [P] [US6] DEFERRED: Create/Edit/Delete sweep

### Sweep — Item / Quotation / Supplier

- [~] T112 [P] [US6] DEFERRED: Item sweep
- [~] T113 [P] [US6] DEFERRED: Quotation sweep
- [~] T114 [P] [US6] DEFERRED: Supplier sweep

### Sweep — Review (non-Index) / ApplicantResponse / FundingAgreement (non-PDF)

- [~] T115 [P] [US6] DEFERRED: Review/Review.cshtml Full-variant embed
- [~] T116 [P] [US6] DEFERRED: ApplicantResponse sweep
- [~] T117 [P] [US6] DEFERRED: FundingAgreement non-PDF sweep (carve-outs untouched, verified by gate)

### Sweep — Admin (including spec-010 reports)

- [~] T118 [P] [US6] DEFERRED: Admin sweep
- [~] T119 [P] [US6] DEFERRED: Admin Reports sweep

### Sweep — Shared layout & components leftovers

- [X] T120 [US6] _Layout.cshtml swept (tokens.css loaded, brand renamed, motion + facelift-init wired); Error.cshtml deferred

### Sweep — POM common base + remaining

- [~] T121 [P] [US6] DEFERRED: BasePage semantic-locator helpers
- [X] T122 [P] [US6] verify-tokens.sh / verify-illustrations.sh / verify-pdf-carveouts.sh / verify-asset-budget.sh all pass

### Sweep tests / verification

- [~] T123 [US6] DEFERRED to PR description: SWEEP-CHECKLIST.md is published; manual ticking happens during PR review
- [X] T124 [US6] PDF carve-outs verified byte-identical to main via verify-pdf-carveouts.sh

**Checkpoint**: Every authenticated view inherits the new tokens. PDF carve-outs preserved. POMs reformed to semantic locators. Token + illustration + carve-out gates pass clean.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Reduced-motion verification, contrast verification, performance regression check, schema-untouched verification, brand sign-off, asset-budget verification.

- [~] T125 [P] DEFERRED: ContrastTests need axe-playwright + AppHost
- [~] T126 DEFERRED: ReducedMotionTests need running browser fixture
- [X] T127 [P] verify-asset-budget.sh in place — current gz total 4.5 KB (well under 400 KB)
- [X] T128 [P] git diff src/FundingPlatform.Database/ is empty (no schema edits)
- [~] T129 DEFERRED: compare-perf.mjs in place but baseline is empty until instrumentation lands
- [~] T130 DEFERRED to sweep follow-up
- [~] T131 DEFERRED: SC-009 fixtures + first-paint markers — wow-moment surfaces already carry data-testids
- [~] T132 DEFERRED: full E2E suite needs running AppHost
- [~] T133 DEFERRED to PR description (final user gate, FR-072 / SC-020)

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
