# Implementation Plan: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Branch**: `008-tabler-ui-migration` | **Date**: 2026-04-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/008-tabler-ui-migration/spec.md`

## Summary

Pure presentation-layer feature. Vendor the Tabler.io open-source UI theme into `wwwroot/lib/tabler/`, replace the current `_Layout.cshtml` with a Tabler shell (collapsible sidebar with role-aware nav, topbar with brand and identity, breadcrumb slot, page-header slot), introduce a separate `_AuthLayout.cshtml` for the unauthenticated Login/Register pages, build nine reusable Razor partials under `Views/Shared/Components/`, add a single `StatusVisualMap` static helper that centralizes the canonical color+icon per domain status enum, and rewrite every view under `Views/{Account, Admin, ApplicantResponse, Application, FundingAgreement (excluding `Document.cshtml` + `_FundingAgreementLayout.cshtml`), Home, Item, Quotation, Review}/` to consume the new shell and partials.

Constitution Principle III (Playwright E2E NON-NEGOTIABLE) is honored by (a) all existing E2E test classes for specs 001–007 continuing to pass after PageObject selector touch-ups (FR-019) and (b) one new test class `RoleAwareSidebarTests.cs` covering the only genuinely new behavior — role-aware sidebar visibility (FR-020).

The PDF target files (`Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml`) are explicitly excluded and verified byte-identical via `git diff` (FR-015, SC-007). No domain, application, persistence, or authorization changes are introduced.

Total production-code footprint: ~28 view files modified (re-skinned), 1 layout replaced + 1 new auth layout, 9 new shared partials, 1 new static helper class, vendored Tabler dist assets, 1 new E2E test class + ~7 modified PageObjects to track new DOM selectors. No new NuGet packages.

## Technical Context

**Language/Version**: C# / .NET 10.0 (matches all prior specs)
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **NEW vendored static-asset dependency**: Tabler.io open-source build (CSS + JS) and Tabler Icons. No new NuGet packages, no new managed dependencies.
**Storage**: SQL Server (Aspire-managed for dev, dacpac schema management). **No schema changes.**
**Testing**: Playwright for .NET (NUnit) for E2E. Existing PageObjects under `tests/FundingPlatform.Tests.E2E/PageObjects/` updated to track shell DOM changes (new sidebar/topbar selectors) without weakening assertions. New `RoleAwareSidebarTests.cs` adds four `[Test]` methods (one per role + one for the unauthenticated shell). Existing unit/integration tests are unaffected.
**Target Platform**: Linux/Windows server (Aspire-orchestrated). Same as specs 001–007. UI verified against Chromium and Firefox at 1280×800 and 360×740 viewports.
**Project Type**: Web application (server-side ASP.NET MVC; no SPA framework introduced).
**Performance Goals**: No new request paths. The themed shell adds CSS (~120 KB) and JS (~80 KB) loaded once, served from `wwwroot/lib/tabler/`. Page render budget for views unchanged versus pre-spec (no new server-side work; Razor partial rendering is comparable in cost to the inline markup it replaces).
**Constraints**: FR-016 forbids any controller, view-model, validation, persistence, or authorization change. FR-015 forbids modification of the PDF target files. FR-006 forbids any badge markup outside `_StatusPill`. FR-008 forbids file references outside `_DocumentCard`. FR-017 forbids inline `style=` attributes. FR-019 forbids relaxing existing E2E assertions on user-visible behavior — selectors may change, semantics may not.
**Scale/Scope**: ~28 view files re-skinned, 1 layout replaced, 1 new auth layout, 9 new shared partials, 1 new helper class, vendored asset bundle, 1 new E2E test class, ~7 modified PageObjects. Single-spec sweep enabled by pre-prod status (no real applicants or disbursements depend on this system today).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | All changes confined to Web layer (views, partials, layout, one helper class) and `wwwroot/` static assets. No Domain, Application, or Infrastructure changes. The new `StatusVisualMap` helper lives under `FundingPlatform.Web/Helpers/` and consumes `FundingPlatform.Domain.Enums.*` read-only — that's the existing inward dependency direction. |
| II. Rich Domain Model | PASS (N/A) | No domain entities, value objects, enums, or methods are added or modified. The visual mapping is pure presentation; it does not encode any business rule. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | FR-019 mandates all existing Playwright E2E test classes for specs 001–007 continue to pass, with PageObject selector updates allowed but no weakening of user-visible-behavior assertions. FR-020 mandates a new `RoleAwareSidebarTests.cs` covering the role-aware sidebar (the only genuinely new user-visible behavior). Each new test is independently runnable per Constitution III. |
| IV. Schema-First Database Management | PASS (N/A) | No schema changes. No dacpac edits. No EF migrations (prohibited regardless). |
| V. Specification-Driven Development | PASS | Spec is SOUND (see `REVIEW-SPEC.md` iteration 2). Plan flows from spec; `research.md` resolves nine planning decisions; `data-model.md` enumerates the StatusVisualMap and partial contracts (no domain entities); `contracts/README.md` documents partial parameters, the status mapping table, the sidebar entry table, and the action-class enum; `quickstart.md` walks the manual smoke procedure for the three role golden paths plus sidebar visibility plus PDF parity. Tasks generated next by `/speckit-tasks`. |
| VI. Simplicity and Progressive Complexity | PASS | No new NuGet packages. Razor partials chosen over ASP.NET Core View Components (which would require backing C# classes) because partials are simpler and sufficient. The reusable component library is grown with discipline — a partial is extracted only when 2+ views need it (recorded as an Assumption in the spec). Tabler vendored locally as static assets, not pulled at runtime. The status mapping is a single static class, not an interface-and-implementation pair. No background jobs, no new roles, no new aggregates. Complexity Tracking table is empty. |

**Gate result: PASS — proceed to Phase 0.**

**Post-design re-check (after Phase 1 — `data-model.md`, `contracts/README.md`, `quickstart.md` all generated):** Still PASS. The seven new presentation-layer record types (`StatusVisual`, `ActionClass`, `ActionItem`, `BreadcrumbItem`, `DocumentReference`, `TimelineEvent`, `SidebarEntry`, plus `PageHeaderViewModel`, `DataTableViewModel`, etc.) are parameter envelopes for the partials, not abstractions — each maps 1:1 to a partial's input shape. They live in `FundingPlatform.Web.Models` and are never persisted, never crossed into Application/Domain. No additional Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/008-tabler-ui-migration/
├── spec.md                     # Stakeholder-facing specification (SOUND)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — nine planning decisions resolved
├── data-model.md               # Phase 1 output — StatusVisualMap + partial contracts (no domain entities)
├── quickstart.md               # Phase 1 output — manual smoke procedure for golden paths + sidebar + PDF parity
├── contracts/
│   └── README.md               # Phase 1 output — partial parameter contracts, status mapping table, sidebar entry table, action-class enum
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── review_brief.md             # Reviewer-facing guide (4 areas of potential disagreement)
├── REVIEW-SPEC.md              # Formal spec soundness review (iteration 2 SOUND)
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
└── FundingPlatform.Web/
    ├── Helpers/
    │   └── StatusVisualMap.cs                                # NEW: static class mapping ApplicationState / ItemReviewStatus / AppealStatus / SignedUploadStatus → (TablerColor, TablerIcon) tuple. Sole source of truth for status visuals.
    ├── Models/
    │   └── StatusVisual.cs                                   # NEW: small record `(string Color, string Icon, string DisplayLabel)` returned by StatusVisualMap and consumed by _StatusPill.
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml                                # MODIFY (full rewrite): Tabler shell — collapsible left sidebar (role-aware nav + brand + footer), topbar (page title from ViewData["Title"], current user, logout), breadcrumb slot, content area. Removes prior Bootstrap-only navbar markup.
    │   │   ├── _Layout.cshtml.css                            # MODIFY: replace existing scoped CSS with Tabler-compatible scoped overrides (or delete if no overrides remain).
    │   │   ├── _AuthLayout.cshtml                            # NEW: Tabler auth-shell variant for Login / Register (no sidebar). Same theme assets, simpler chrome.
    │   │   ├── _ValidationScriptsPartial.cshtml              # MODIFY (light): keep jQuery validation includes; verify against Tabler form patterns.
    │   │   ├── Error.cshtml                                  # MODIFY: render inside the Tabler shell with consistent error-page styling.
    │   │   └── Components/                                   # NEW directory
    │   │       ├── _PageHeader.cshtml                        # NEW: title + subtitle + breadcrumbs slot + actions slot
    │   │       ├── _StatusPill.cshtml                        # NEW: accepts an enum value, queries StatusVisualMap, renders Tabler badge
    │   │       ├── _DataTable.cshtml                         # NEW: Tabler card-table wrapper with density flag; renders inline _EmptyState when no rows
    │   │       ├── _FormSection.cshtml                       # NEW: labeled section preserving asp-validation-for tag helpers
    │   │       ├── _EmptyState.cshtml                        # NEW: icon + headline + body + primary-action slot
    │   │       ├── _ActionBar.cshtml                         # NEW: enumerated primary / secondary / destructive / state-locking treatment per FR-009
    │   │       ├── _DocumentCard.cshtml                      # NEW: file icon + name + size + signer/timestamp metadata + download link as Tabler card; sole document renderer
    │   │       ├── _EventTimeline.cshtml                     # NEW: chronological audit-derived events on entity detail pages
    │   │       └── _ConfirmDialog.cshtml                     # NEW: Tabler modal pattern for state-locking and destructive actions per FR-010
    │   ├── Account/
    │   │   ├── Login.cshtml                                  # MODIFY: set Layout = "_AuthLayout"; rewrite using _PageHeader + _FormSection
    │   │   └── Register.cshtml                               # MODIFY: same pattern as Login
    │   ├── Admin/
    │   │   ├── Index.cshtml                                  # MODIFY: _PageHeader + Tabler card grid
    │   │   ├── Configuration.cshtml                          # MODIFY: _PageHeader + _FormSection
    │   │   ├── ImpactTemplates.cshtml                        # MODIFY: _PageHeader + _DataTable + _EmptyState
    │   │   ├── CreateTemplate.cshtml                         # MODIFY: _PageHeader + _FormSection + _ActionBar
    │   │   └── EditTemplate.cshtml                           # MODIFY: same pattern as CreateTemplate
    │   ├── ApplicantResponse/
    │   │   ├── Index.cshtml                                  # MODIFY: _PageHeader + status-driven banner via _PageHeader subtitle slot + _ActionBar
    │   │   ├── Appeal.cshtml                                 # MODIFY: _PageHeader + _FormSection + _ActionBar
    │   │   └── _AppealMessage.cshtml                         # MODIFY: themed comment-row markup; structure preserved
    │   ├── Application/
    │   │   ├── Index.cshtml                                  # MODIFY: _PageHeader + _DataTable + _EmptyState; _StatusPill replaces inline badges
    │   │   ├── Create.cshtml                                 # MODIFY: _PageHeader + _FormSection + _ActionBar
    │   │   ├── Edit.cshtml                                   # MODIFY: same pattern as Create
    │   │   ├── Details.cshtml                                # MODIFY: _PageHeader + _StatusPill + _DocumentCard for attachments + _ActionBar + _EventTimeline
    │   │   └── _FundingAgreementPanel.cshtml                 # MODIFY: themed panel markup; structure preserved (still embeddable per spec 006/007 contract)
    │   ├── FundingAgreement/
    │   │   ├── Details.cshtml                                # MODIFY: _PageHeader + _DocumentCard for the generated PDF + _ActionBar + _StatusPill for SignedUploadStatus
    │   │   ├── _FundingAgreementLayout.cshtml                # **DO NOT MODIFY** — PDF print target. Verified byte-identical via git diff (FR-015, SC-007).
    │   │   └── Document.cshtml                               # **DO NOT MODIFY** — PDF body. Verified byte-identical via git diff (FR-015, SC-007).
    │   ├── Home/
    │   │   └── Index.cshtml                                  # MODIFY: _PageHeader + role-aware welcome card grid
    │   ├── Item/
    │   │   ├── Add.cshtml                                    # MODIFY: _PageHeader + _FormSection + _ActionBar
    │   │   ├── Edit.cshtml                                   # MODIFY: same pattern as Add
    │   │   └── Impact.cshtml                                 # MODIFY: _PageHeader + _FormSection
    │   ├── Quotation/
    │   │   └── Add.cshtml                                    # MODIFY: _PageHeader + _FormSection + _ActionBar
    │   ├── Review/
    │   │   ├── Index.cshtml                                  # MODIFY: _PageHeader + _ReviewTabs (existing partial, themed) + _DataTable + _StatusPill + _EmptyState
    │   │   ├── Review.cshtml                                 # MODIFY: _PageHeader + _StatusPill + _DocumentCard + _ActionBar + _ConfirmDialog for state-locking actions
    │   │   ├── SigningInbox.cshtml                           # MODIFY: _PageHeader + _ReviewTabs + _DataTable + _StatusPill + _EmptyState
    │   │   ├── GenerateAgreement.cshtml                      # MODIFY: _PageHeader + _ActionBar + _ConfirmDialog
    │   │   └── _ReviewTabs.cshtml                            # MODIFY: re-style using Tabler nav-pills; preserve ActiveTab semantics from spec 007
    │   └── _ViewImports.cshtml                               # MODIFY: add `@using FundingPlatform.Web.Helpers` so Razor partials can call StatusVisualMap directly
    └── wwwroot/
        ├── lib/
        │   ├── bootstrap/                                    # KEEP: existing dir; Tabler builds atop Bootstrap 5 — references stay valid for any holdover markup. Removable in a future cleanup spec.
        │   └── tabler/                                       # NEW: vendored Tabler.io v1.x open-source build (`dist/css/tabler.min.css`, `dist/js/tabler.min.js`, `dist/css/tabler-icons.min.css`, fonts/, optional sprite). Source of truth: copied from npm `@tabler/core` dist after `npm install --no-save`.
        ├── css/
        │   └── site.css                                      # MODIFY: project-specific overrides only. No inline style= elsewhere (FR-017).
        └── js/
            └── site.js                                       # MODIFY (light): retain unobtrusive validation init; verify Tabler JS hooks don't conflict.

tests/
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── BasePage.cs (or LayoutPage.cs)                    # MODIFY (or NEW if absent): expose Sidebar / Topbar / PageTitle locators; central place for shell DOM changes so other PageObjects don't re-implement them.
    │   ├── ApplicationListPage.cs                            # MODIFY: update selectors to track _DataTable / _StatusPill markup
    │   ├── ApplicationDetailsPage.cs                         # MODIFY: update selectors to track _PageHeader / _StatusPill / _DocumentCard markup
    │   ├── ReviewQueuePage.cs                                # MODIFY: update selectors to track _DataTable / _StatusPill / _ReviewTabs markup
    │   ├── ReviewPage.cs                                     # MODIFY: update selectors to track _ActionBar / _ConfirmDialog markup
    │   ├── SigningInboxPage.cs                               # MODIFY: update selectors to track _DataTable / _StatusPill / _ReviewTabs markup
    │   ├── ApplicantResponsePage.cs                          # MODIFY: update selectors to track _PageHeader / banner / _ActionBar markup
    │   ├── SigningStagePanelPage.cs                          # MODIFY: update selectors to track themed panel markup
    │   ├── LoginPage.cs / RegisterPage.cs                    # MODIFY: update selectors to track _AuthLayout markup
    │   └── AdminPage.cs (and template pages)                 # MODIFY: update selectors to track _PageHeader / _DataTable markup
    └── Tests/
        └── RoleAwareSidebarTests.cs                          # NEW: four [Test] methods — ApplicantSidebarShowsApplicantEntries, ReviewerSidebarShowsReviewerEntries, AdminSidebarShowsAdminEntries, UnauthenticatedAuthShellOmitsSidebar
```

**Structure Decision**: Web-application layout (same as all prior specs). Feature is view-layer-only with one helper class. No new projects, no new layers, no new namespaces beyond `FundingPlatform.Web.Helpers` and `FundingPlatform.Web.Models` (the latter being the new `StatusVisual` record — added because there's no existing home for presentation-layer DTOs). Existing `bin/`, `obj/`, `Properties/`, `Services/`, `Controllers/`, `ViewModels/` directories are unaffected.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified.**

No violations. Nothing to track.
