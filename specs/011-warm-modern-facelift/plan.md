# Implementation Plan: Warm-Modern Facelift

**Branch**: `011-warm-modern-facelift` | **Date**: 2026-04-28 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification at `specs/011-warm-modern-facelift/spec.md`

## Summary

Elevate the entire authenticated experience to a "warm-modern premium" finish by introducing a brand identity, a CSS custom-property design-token layer, a motion system that respects `prefers-reduced-motion`, four signature wow-moment surfaces (applicant home dashboard, application journey timeline, signing ceremony, reviewer queue dashboard), a 9-scene empty-state illustration set, and a sweep across every authenticated view. **Zero schema changes**: wow-moment data is sourced through Application-layer projections over `VersionHistory` (spec 010) and existing aggregates. PDF carve-out files (`Document.cshtml`, `_FundingAgreementLayout.cshtml`) remain byte-identical. The lift is delivered through edits to `wwwroot/`, `Views/`, `Application/` projection services, and a Playwright POM rewrite — no new managed dependencies, only static assets vendored under `wwwroot/lib/`.

## Technical Context

**Language/Version**: C# / .NET 10.0 (matches all prior specs).
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **No new managed dependencies.** New **static-asset** vendored dependencies only: Fraunces (display serif, SIL OFL), Inter (body sans, SIL OFL), JetBrains Mono (monospace, Apache 2.0), 9 in-house empty-state SVG illustrations, and `canvas-confetti` (≤ 5 KB gz, vendored as a static `.js`) — all served from `wwwroot/lib/`. Tabler.io static-asset bundle (vendored by spec 008) and Syncfusion HTML-to-PDF (vendored by spec 005) remain unchanged.
**Storage**: SQL Server (Aspire-managed for dev, dacpac schema management). **No schema changes** (FR-067). Wow-moment data flows through new Application-layer query/projection services (e.g., `IApplicantDashboardProjection`, `IReviewerQueueProjection`, `IJourneyProjector`) that read existing aggregates.
**Testing**: Playwright for .NET (NUnit). E2E POMs in `tests/FundingPlatform.Tests.E2E/PageObjects/` are rewritten against the new HTML — locator strategy migrates to ARIA roles + accessible names with `data-testid` only where role/name are insufficient. New POMs land for the four wow moments. A dedicated reduced-motion test exercises the catalog under Playwright's `reduceMotion` option. WCAG AA contrast validated via `axe-playwright` on the four wow-moment views and the layout shell.
**Target Platform**: ASP.NET MVC server-rendered web app, served by .NET Aspire-orchestrated Web project.
**Project Type**: Web application (single backend solution; no SPA framework).
**Performance Goals**: LCP and TBT on the four wow-moment surfaces stay within +10% of the pre-spec baseline (FR-073, SC-015). Combined incremental wire weight ≤ 400 KB gzipped (FR-074, SC-016). Subset budget (rough): Fraunces ~80 KB gz, Inter ~70 KB gz, JetBrains Mono ~50 KB gz (Latin-1 + numerals subset), 9 SVGs ≤ 72 KB gz, canvas-confetti ≤ 5 KB gz → comfortable margin under 400 KB.
**Constraints**: PDF byte-identity preserved (FR-020, SC-014); zero raw hex outside `tokens.css` and PDF carve-outs (FR-009, SC-001); zero inline `style=` (FR-010, SC-002); zero hard-coded animation durations outside `tokens.css` (FR-014, SC-003); WCAG AA color-contrast on all re-tokened surfaces (FR-075, SC-013); no real-time push (FR-068); no dark mode in v1 (FR-070, but tokens semantically named to be theme-able later); HTML restructuring is permitted and encouraged where it improves UX/UI (FR-019).
**Scale/Scope**: ~50 view files in the sweep inventory (Account, Home, Application, Item, Quotation, Supplier, Review, ApplicantResponse, FundingAgreement non-PDF, Admin including all spec-010 reports, Shared layout); 13 partials touched (8 spec-008 partials re-templated + 5 net-new wow-moment partials + the `_EventTimeline` queue-scoped overload); 4 wow-moment views; 9 SVG illustrations; 1 token file; 1 motion catalog; 1 voice-guide deliverable; ~20 Playwright POMs rewritten + 4 new POM bundles for the wow moments + 1 reduced-motion test.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ PASS | New projection services (`JourneyProjector`, `ApplicantDashboardProjection`, `ReviewerQueueProjection`) live in `FundingPlatform.Application`, not in views or controllers. Razor partials remain presentation-only and consume view models projected by the Application layer. No Domain → Web / Infrastructure dependencies introduced. |
| **II. Rich Domain Model** | ✅ PASS | No domain behavior is added or relocated. Existing entity behavior on `Application`, `VersionHistory`, `SignedUpload`, `Appeal` etc. is reused as-is. Branch resolution (Send-back loops, Appeal status) remains computed from existing aggregates. |
| **III. End-to-End Testing (NON-NEGOTIABLE)** | ✅ PASS | All seven user stories have Playwright coverage: existing E2E suites are rewritten against the new HTML; net-new POMs and tests for US1–US4; a dedicated reduced-motion test covers SC-012; `axe-playwright` covers SC-013. POM strategy upgrades to semantic locators per FR-021. |
| **IV. Schema-First (dacpac)** | ✅ PASS | FR-067 + SC-018 explicitly forbid dacpac edits. Wow-moment data is sourced via Application-layer projections over existing aggregates. The `speckit-spex-evolve` escape hatch is documented and unused. |
| **V. SDD** | ✅ PASS | This plan + tasks.md follow the spec-first workflow. User stories are independently testable and deliverable; US5 is the documented foundational dependency. |
| **VI. YAGNI / Simplicity** | ✅ PASS | Out-of-scope guardrails are explicit (FR-068..FR-071). Token names are semantic, not value-bound (preserves theme-ability without shipping dark mode). The single new static-asset addition (`canvas-confetti`) is documented and bounded (≤ 5 KB gz, single celebrational use). No general-purpose animation library, no SPA framework, no real-time push. |

**Net technology additions**: zero managed dependencies. Static-asset additions (3 font families, 9 SVGs, `canvas-confetti`) are documented and budget-checked. **Constitution gate passes.** Re-evaluated after Phase 1 design — still PASS (no new constraints surfaced; no Complexity Tracking entries needed).

## Project Structure

### Documentation (this feature)

```text
specs/011-warm-modern-facelift/
├── plan.md                  # This file
├── research.md              # Phase 0 output: open-question resolutions
├── data-model.md            # Phase 1 output: view models + projection contracts (no schema)
├── quickstart.md            # Phase 1 output: how to develop and verify the facelift
├── contracts/               # Phase 1 output: partial parameter contracts + Razor helper signatures
│   ├── partials.md
│   ├── motion-catalog.md
│   ├── projection-services.md
│   └── illustration-helper.md
├── BRAND-VOICE.md           # FR-004 deliverable (voice guide; produced during US5 implementation)
├── SWEEP-CHECKLIST.md       # FR-023 deliverable (manual sweep verifier; produced during US5/US6)
├── REVIEW-SPEC.md           # Already produced
├── REVIEW-PLAN.md           # Generated by spex-gates-review-plan
├── REVIEW-CODE.md           # Generated by spex-gates-review-code
├── checklists/              # Already exists with requirements.md
└── spec.md                  # Already exists
```

### Source Code (repository root)

The existing solution layout is preserved. **No new projects.** Edits and net-new files cluster under `FundingPlatform.Web` (views, partials, wwwroot) and `FundingPlatform.Application` (projection services). E2E tests are rewritten under the existing `FundingPlatform.Tests.E2E` project.

```text
src/
├── FundingPlatform.AppHost/                  # Aspire host — unchanged
├── FundingPlatform.Application/              # ← NEW projection services for dashboards/journey
│   ├── Services/
│   │   ├── JourneyProjector.cs               # NEW (FR-043)
│   │   ├── ApplicantDashboardProjection.cs   # NEW (FR-024..FR-033)
│   │   └── ReviewerQueueProjection.cs        # NEW (FR-052..FR-060)
│   ├── DTOs/
│   │   ├── ApplicantDashboardDto.cs          # NEW
│   │   ├── ReviewerQueueDto.cs               # NEW
│   │   └── JourneyViewModel.cs               # NEW (Full / Mini / Micro variants)
│   └── …existing                              # unchanged
├── FundingPlatform.Database/                 # ZERO edits (FR-067, SC-018)
├── FundingPlatform.Domain/                   # ZERO edits
├── FundingPlatform.Infrastructure/           # ZERO edits (projections live in Application; no new repos)
├── FundingPlatform.ServiceDefaults/          # unchanged
└── FundingPlatform.Web/
    ├── Controllers/                          # ← edits: HomeController routes by role; ReviewController.Index swaps view
    ├── Views/
    │   ├── Account/                          # SWEEP target (Login / Register / ChangePassword / AccessDenied)
    │   ├── Admin/                            # SWEEP target (Index / Users / Roles / SystemConfigurations / Reports)
    │   ├── ApplicantResponse/                # SWEEP target
    │   ├── Application/                      # SWEEP target; Details hosts US2 Full
    │   ├── FundingAgreement/                 # SWEEP target EXCEPT Document.cshtml + _FundingAgreementLayout.cshtml (carve-outs)
    │   │   └── Sign/                         # NEW or refactored: ceremony view per US3
    │   ├── Home/                             # REPLACED for Applicant role per US1
    │   │   └── ApplicantDashboard.cshtml     # NEW
    │   ├── Item/Quotation/Supplier/          # SWEEP targets
    │   ├── Review/                           # Index REPLACED per US4; Details gets US2 embed
    │   │   └── QueueDashboard.cshtml         # NEW (replaces Review/Index)
    │   └── Shared/
    │       ├── _Layout.cshtml                # SWEEP target
    │       ├── _AuthLayout.cshtml            # SWEEP target
    │       └── Components/
    │           ├── _ActionBar.cshtml         # RE-TEMPLATED (token-only)
    │           ├── _ApplicantHero.cshtml     # NEW (FR-032)
    │           ├── _ApplicationCard.cshtml   # NEW (FR-032)
    │           ├── _ApplicationJourney.cshtml# NEW (FR-034 — Full/Mini/Micro variants)
    │           ├── _ConfirmDialog.cshtml     # RE-TEMPLATED
    │           ├── _DataTable.cshtml         # RE-TEMPLATED
    │           ├── _DocumentCard.cshtml      # RE-TEMPLATED
    │           ├── _EmptyState.cshtml        # RE-TEMPLATED + extended (illustration param, FR-064)
    │           ├── _EventTimeline.cshtml     # RE-TEMPLATED + queue-scoped overload (FR-029, FR-055)
    │           ├── _FormSection.cshtml       # RE-TEMPLATED
    │           ├── _KpiTile.cshtml           # RE-TEMPLATED + ticker-aware (FR-026)
    │           ├── _PageHeader.cshtml        # RE-TEMPLATED
    │           ├── _ResourcesStrip.cshtml    # NEW (FR-032)
    │           ├── _ReviewerHero.cshtml      # NEW (FR-059)
    │           ├── _ReviewerQueueRow.cshtml  # NEW (FR-059)
    │           ├── _SigningCeremony.cshtml   # NEW (FR-051)
    │           └── _StatusPill.cshtml        # RE-TEMPLATED + token-subtle mapping (FR-011)
    ├── Helpers/
    │   └── IllustrationHelper.cs             # NEW (Razor helper, FR-063)
    └── wwwroot/
        ├── css/
        │   ├── tokens.css                    # NEW (FR-006)
        │   └── site.css                      # RE-TEMPLATED (token-only consumers, FR-007)
        ├── js/
        │   ├── motion.js                     # NEW (number ticker, journey stagger, ceremony hooks, all token-driven)
        │   └── facelift-init.js              # NEW (filter chip reflow, KPI ticker mount)
        └── lib/
            ├── fonts/                        # NEW: Fraunces, Inter, JetBrains Mono (subsetted)
            ├── illustrations/                # NEW: 9 SVGs
            └── canvas-confetti/              # NEW: vendored ≤ 5 KB gz

tests/
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── Applicant/                        # NEW directory for wow-moment POMs
    │   │   └── ApplicantDashboardPage.cs     # NEW (US1)
    │   ├── Reviewer/
    │   │   └── ReviewerQueueDashboardPage.cs # NEW (US4)
    │   ├── Signing/
    │   │   └── CeremonyPage.cs               # NEW (US3)
    │   └── …existing POMs                    # REWRITTEN against new HTML and semantic locators (FR-021)
    └── Tests/
        ├── Applicant/ApplicantDashboardTests.cs       # NEW (US1)
        ├── Application/JourneyTimelineTests.cs        # NEW (US2)
        ├── FundingAgreement/SigningCeremonyTests.cs   # NEW (US3)
        ├── Review/ReviewerQueueDashboardTests.cs      # NEW (US4)
        ├── Accessibility/ContrastTests.cs             # NEW (axe-playwright; SC-013)
        ├── Motion/ReducedMotionTests.cs               # NEW (Playwright reduceMotion; SC-012)
        └── …existing tests                              # rewritten where the sweep changed HTML
```

**Structure Decision**: Single-solution web application. Wow-moment view models are produced by new Application-layer projection services and consumed by new Razor partials. The PDF carve-outs remain isolated (no styling drift). Static assets land under `wwwroot/lib/` per the spec-008 vendored-assets pattern.

## Open Questions Resolved Here

The spec carried 10 open questions to planning. Each is decided in `research.md`. The headlines:

| # | Question | Decision (see research.md) |
|---|----------|---------------------------|
| 1 | Brand display name | Recommend **Forge** as the primary, *Ascent* as fallback, `FundingPlatform` as no-rename option. Final selection is FR-072 user sign-off; `BRAND-VOICE.md` and asset filenames assume placeholder until sign-off. |
| 2 | Exact hex values | Pinned palette in `research.md` §1.2; values cross-checked for WCAG AA on warm-off-white background. |
| 3 | Type-scale ramp | 8-step ramp (display-xl → micro), Fraunces only at display-lg/xl, all other ramp steps Inter. Density-aware leading values pinned. |
| 4 | Tabler `--tblr-*` bridge inventory | Override the 12 most-used Tabler tokens (`--tblr-primary`, `--tblr-bg-surface`, `--tblr-border-color`, status colors); leave the long tail to Tabler defaults to keep the bridge maintainable. |
| 5 | Illustration source | In-team SVG production using the documented style discipline; no commission, no adapted-from-open. Fallback path documented if designer unavailable. |
| 6 | Ceremony view-vs-partial | New full controller action `FundingAgreementController.SignCeremony(Guid)` rendering a dedicated view `Sign/Ceremony.cshtml` that composes `_SigningCeremony` partial. View is preferred so URLs are bookmark-able and back-end variant logic is testable. |
| 7 | Ceremony fresh-vs-bookmark mechanism | **TempData with a one-shot key**. The `Sign` POST sets `TempData["CeremonyFresh"] = true`; the redirect target reads-and-clears. Bookmark re-visits see `null` and render the non-animated summary. Rationale: TempData survives one redirect, dies after; no session storage; no URL pollution. |
| 8 | Journey-stage resolver placement | **Sibling resolver**: a new `IJourneyStageResolver` lives next to spec-008's `IStatusDisplayResolver`. Both resolvers depend on a shared `StageMappingProvider` which encodes the canonical (stage → icon, label, color-token) table. This avoids overloading the spec-008 resolver and keeps the journey's branch logic isolated. |
| 9 | Visual-regression tooling | **Defer.** Manual SWEEP-CHECKLIST per spec; semantic-selector upgrade in US6 keeps a future Percy/Chromatic/Playwright-screenshot adoption cheap. |
| 10 | Login/Register tone | **Clean focused single-CTA**. No marketing hero on auth surfaces (FR-018 already specifies this). Auth pages get the new `_AuthLayout` re-templating, the new tokens, and brand-aligned copy — no decorative illustrations, no off-brand hero copy. |

The three Optional review-spec recommendations are also adopted here:
- **FR-039 OR-clause**: pinned to "scroll into view AND apply a 1.5 s `.is-highlighted` class" — both behaviors required.
- **FR-070 verification handle**: token names MUST be semantic; `research.md §3` documents the naming rules; review-plan/review-code grep gates verify no value-bound names like `--color-white`.
- **canvas-confetti documentation**: this Constitution Check section explicitly logs the static-asset addition, even though it falls below the runtime-framework bar.

## Complexity Tracking

> No Constitution Check violations. Table omitted.

## Phase Plan

### Phase 0 — Research (`research.md`)

Resolves the 10 open questions above. Each entry follows the **Decision / Rationale / Alternatives considered** schema. Includes:
- Brand identity proposal (name candidates with reasoning, palette hex values pinned and contrast-checked, type-scale ramp, logo concept).
- Tabler bridge inventory (which `--tblr-*` to override, which to leave).
- Motion catalog finalized (each catalog entry with trigger, duration token, easing token, reduced-motion behavior).
- Token naming rules (semantic, not value-bound; greppable).
- Voice-guide outline (this feeds `BRAND-VOICE.md`).
- POM rewrite strategy (locator hierarchy: ARIA role+name → `data-testid` only as fallback).

### Phase 1 — Design Artifacts

- **`data-model.md`** — Documents view-model projections (no schema changes). Includes the `JourneyViewModel` shape (with branch markers), `ApplicantDashboardDto`, `ReviewerQueueDto`, and the projection services' contracts.
- **`contracts/partials.md`** — Parameter contracts for each new and re-templated partial (input model, optional flags, outputs).
- **`contracts/motion-catalog.md`** — Pinned motion catalog as a single source of truth for the implementation pass.
- **`contracts/projection-services.md`** — Service interfaces (`IJourneyProjector`, `IApplicantDashboardProjection`, `IReviewerQueueProjection`, `IJourneyStageResolver`, `IStageMappingProvider`).
- **`contracts/illustration-helper.md`** — `Illustration("scene-key")` helper signature + scene-key registry.
- **`quickstart.md`** — Developer guide: how to add or modify a partial, how to verify token compliance locally, how to run the wow-moment Playwright tests, how to run the reduced-motion test, how to capture/compare the LCP/TBT baseline.

After Phase 1, the agent context file is updated by `update-agent-context.sh claude` and the Constitution Check is re-evaluated (still PASS).

### Phase 2 — Tasks (out of this command)

`/speckit-tasks` will generate `tasks.md` organized by user story with:
- US5 (foundational) tasks first.
- US7 (illustration set) parallelizable with US5 token work.
- US1, US2, US4 dependent on US5; US2 dependent on `_StatusPill` re-template path.
- US3 dependent on US5 + US2 (Mini variant).
- US6 (sweep) is a long-tail story that runs in parallel with US1–US4 once US5 lands.
- POM rewrites and net-new tests anchor each user story's verification.
- LCP/TBT baseline capture (FR-073) runs as Day-1 task before US5 implementation.
