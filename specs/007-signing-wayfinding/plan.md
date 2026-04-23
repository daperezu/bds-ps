# Implementation Plan: Signing Stage Wayfinding

**Branch**: `007-signing-wayfinding` | **Date**: 2026-04-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/007-signing-wayfinding/spec.md`

## Summary

Close three discovery gaps left by spec 006 through a **pure presentation-layer change**. No new entities, no schema changes, no new NuGet packages, no new controller actions that return signing-stage data (FR-007).

Three concrete deliverables:

1. **Tab nav on `/Review`** — a shared `_ReviewTabs.cshtml` partial that ships a two-tab pill/nav header ("Initial Review Queue" / "Signing Inbox"), included at the top of both `Review/Index.cshtml` and `Review/SigningInbox.cshtml`. Each view sets `ViewData["ActiveTab"]`. No new routes; the two existing routes (`/Review`, `/Review/SigningInbox`) become the tab targets. Class-level `[Authorize(Roles = "Reviewer,Admin")]` on `ReviewController` already covers both tabs uniformly.

2. **Embedded signing panel + contextual banner on `/ApplicantResponse/Index/{id}`** — reuse the existing async-fetch embed pattern from `Views/Application/Details.cshtml:159-177` (placeholder `div` fetched from `/FundingAgreement/Panel/{id}`). The `ApplicantResponseDto` gains a `HasFundingAgreement` bool (derived at the service layer via a `.Any()` on the application's agreements). The view renders a state-driven banner above the panel: `ResponseFinalized` + `HasFundingAgreement` → "ready to sign below"; `AgreementExecuted` → "has been executed"; otherwise hidden.

3. **Quickstart prose sync** — targeted sentence-level edits in `specs/006-digital-signatures/quickstart.md` Journeys 1/2/4 so reviewer steps name "Review → Signing Inbox tab" and applicant steps name `/ApplicantResponse/Index/{id}`. No journey gains or loses steps.

Plus the non-negotiable: **Playwright E2E coverage (SC-007)** via a new `SigningWayfindingTests.cs` with three tests, extending the existing `ReviewQueuePage` and `ApplicantResponsePage` page objects with tab and banner assertions.

The feature is additive at the view layer and touches exactly one application-layer DTO + service path (to surface `HasFundingAgreement`). Total production-code footprint: ~6 files modified + 1 new partial + 1 new test class + 1 quickstart edit.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **No new dependencies introduced by this feature.**
**Storage**: SQL Server (Aspire-managed container for dev, dacpac schema management). **No schema changes.** No new storage subsystems.
**Testing**: Playwright for .NET (NUnit) for E2E per SC-007; existing unit/integration coverage for 006 signing behavior continues unchanged (SC-006). Page Object Model pattern extended with tab + banner locators on existing pages.
**Target Platform**: Linux/Windows server (Aspire-orchestrated). Same runtime as specs 001–006.
**Project Type**: Web application (server-side ASP.NET MVC; no SPA).
**Performance Goals**: No new request paths introduced. The two existing routes (`/Review`, `/Review/SigningInbox`) retain their existing latency budget (≤ 500 ms at p95). The embedded panel on `ApplicantResponse/Index` reuses the async-fetch pattern from Application/Details, so its render budget is the same (≤ 250 ms at p95 for the panel body). Banner rendering adds negligible server-side cost (state + bool check).
**Constraints**: FR-007 forbids new controller actions that return signing-stage data — the `/FundingAgreement/Panel/{id}` endpoint is the sole server source of signing-panel content. FR-005 forbids regression on the `/Application/Details/{id}` embed. FR-004 forbids divergence between the two embed sites — enforced by reusing the same async fetch + the same rendered partial path. The `[Authorize(Roles="Applicant")]` action-level gate on `ApplicantResponseController.Index` is kept as-is — US2 Scenario 4 ("reviewer/admin visits applicant's response page") is satisfied by the existing 403, not by new gating.
**Scale/Scope**: Single new partial; two existing views modified; one existing controller action modified to surface one bool; one new E2E test class with three tests; one targeted quickstart prose edit. No concurrency concerns — no state writes added.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | Changes confined to Web layer (views, one controller, one view-model field) plus a single additive DTO field surfaced from Application. No Domain changes. Dependencies point inward; no cross-layer leaks. |
| II. Rich Domain Model | PASS | No domain entities, enums, or methods are added or modified. The only Application-layer change is adding a derived `HasFundingAgreement` bool to the `ApplicantResponseDto` and populating it in the existing query service. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | SC-007 mandates Playwright coverage for the three wayfinding journeys. New `SigningWayfindingTests.cs` file, three `[Test]` methods per SC-007 sub-bullets. Page objects extended, not duplicated. Each test is independently runnable per Constitution III. |
| IV. Schema-First Database Management | PASS (N/A) | No schema changes. No dacpac edits. No EF migrations (prohibited anyway). |
| V. Specification-Driven Development | PASS | Spec SOUND (REVIEW-SPEC.md); plan flows from spec; research.md resolves the six planning decisions; data-model.md explicitly states "no new entities"; contracts/README.md enumerates the view-model and partial additions; quickstart.md walks the three wayfinding journeys. Tasks generated next by `/speckit-tasks`. |
| VI. Simplicity and Progressive Complexity | PASS | No background jobs, no new roles, no new NuGet packages, no new aggregates. Reuses the existing async-fetch pattern rather than inventing a new embedding mechanism. Deferred: pending-count badges (out of scope per spec); top-level nav entry (out of scope). |

**Gate result: PASS — proceed to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/007-signing-wayfinding/
├── spec.md                     # Stakeholder-facing specification (SOUND)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — six planning decisions resolved
├── data-model.md               # Phase 1 output — "no new entities" + the one DTO/view-model additive field
├── quickstart.md               # Phase 1 output — three manual wayfinding journeys mirroring SC-007
├── contracts/
│   └── README.md               # View-model additive contract; tab partial contract; banner visibility rules
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── review_brief.md             # Reviewer-facing guide
├── REVIEW-SPEC.md              # Formal spec soundness review
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
├── FundingPlatform.Application/
│   ├── DTOs/
│   │   └── ApplicantResponseDto.cs                                        # MODIFY: add `bool HasFundingAgreement` (constructor parameter)
│   └── Services/
│       └── ApplicantResponseService.cs                                    # MODIFY: populate HasFundingAgreement via a lightweight `.Any()` on the agreement collection already loaded, or via a single repository check
└── FundingPlatform.Web/
    ├── Controllers/
    │   └── ApplicantResponseController.cs                                 # MODIFY (tiny): pass-through of HasFundingAgreement from DTO into view-model (BuildViewModel)
    ├── ViewModels/
    │   └── ApplicantResponseViewModel.cs                                  # MODIFY: add `bool HasFundingAgreement { get; set; }`
    └── Views/
        ├── Review/
        │   ├── _ReviewTabs.cshtml                                         # NEW: shared tab-nav partial with two pills (Initial Review Queue / Signing Inbox) driven by ViewData["ActiveTab"]
        │   ├── Index.cshtml                                               # MODIFY: include `_ReviewTabs` above the existing queue table; set ViewData["ActiveTab"] = "Initial"
        │   └── SigningInbox.cshtml                                        # MODIFY: include `_ReviewTabs` above the existing rows; set ViewData["ActiveTab"] = "Signing"
        └── ApplicantResponse/
            └── Index.cshtml                                               # MODIFY: render state-driven banner above response form; embed the signing panel via async-fetch placeholder (same pattern as Views/Application/Details.cshtml:159-177)

specs/
└── 006-digital-signatures/
    └── quickstart.md                                                      # MODIFY: wayfinding-prose-only edits in Journeys 1, 2, 4 (no journey gains or loses steps)

tests/
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── ReviewQueuePage.cs                                             # MODIFY: add `ClickSigningInboxTab()` + `IsSigningInboxTabActive()` + `IsInitialQueueTabActive()`
    │   └── ApplicantResponsePage.cs                                       # MODIFY: add `IsReadyToSignBannerVisible()` + `IsAgreementExecutedBannerVisible()` + expose `SigningPanel` accessor (reusing SigningStagePanelPage against the embed on this page)
    └── Tests/
        └── SigningWayfindingTests.cs                                      # NEW: three [Test] methods — ReviewerReachesSigningInboxInTwoClicks, ApplicantSeesBannerAndPanelOnResponsePage, ApplicationDetailsEmbedStillRenders
```

**Structure Decision**: Web-application layout (same as all prior specs). The feature is view-layer-first; the only non-view changes are the additive `HasFundingAgreement` flag that flows DTO → view-model → view. No new projects, no new layers, no new namespaces.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified.**

No violations. Nothing to track.
