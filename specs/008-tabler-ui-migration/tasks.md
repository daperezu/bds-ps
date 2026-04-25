---
description: "Tasks for 008-tabler-ui-migration"
---

# Tasks: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Input**: Design documents in `/specs/008-tabler-ui-migration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. Constitution Principle III makes Playwright E2E tests non-negotiable. FR-019 mandates that every existing E2E test class for specs 001–007 continues to pass after the sweep; FR-020 mandates a new `RoleAwareSidebarTests` for the only genuinely new behavior.

**Organization**: Tasks are grouped by user story (US1, US2, US3) per spec.md priority order. Each phase is a checkpoint at which the build, the existing E2E suite, and the spec invariants for the work landed so far must all be green.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 — maps to the user stories in spec.md
- File paths are absolute-from-repo-root

## Path Conventions

- Web application layout per plan.md §Project Structure
- `src/FundingPlatform.Web/` for the entire production-code footprint of this feature
- `tests/FundingPlatform.Tests.E2E/` for Playwright tests
- `wwwroot/lib/tabler/` for the vendored Tabler.io static assets
- `Views/FundingAgreement/Document.cshtml` and `Views/FundingAgreement/_FundingAgreementLayout.cshtml` are **EXPLICITLY EXCLUDED** from every modify task per FR-015 / SC-007 — the spec mandates they stay byte-identical. Verified in T060 via `git diff`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Vendor Tabler.io into `wwwroot/lib/tabler/`, capture the pre-spec PDF baseline, and verify a clean baseline build before any view changes land.

- [ ] T001 Capture the pre-spec PDF baseline per `quickstart.md §"Pre-spec PDF baseline"`: on the `main` branch, log in as a reviewer, generate one funding-agreement PDF for an existing executed application, save it as `baseline-pre-008.pdf` outside the repo (or in a non-tracked path), then `git switch 008-tabler-ui-migration`. This baseline is the visual reference for SC-007 / FR-015 verification in T060.
- [X] T002 Run `dotnet build --nologo` at the repo root and confirm zero errors and zero warnings on branch `008-tabler-ui-migration` before making any changes; this is the baseline against which every subsequent task is measured.
- [X] T003 [P] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm zero failures on the baseline branch; this is the green starting state that FR-019 requires every subsequent task to preserve.
- [X] T004 Vendor Tabler.io per `research.md §R-002`: in a scratch directory outside the repo, run `npm install --no-save @tabler/core@latest`, then copy `node_modules/@tabler/core/dist/` into `src/FundingPlatform.Web/wwwroot/lib/tabler/dist/`. Commit the vendored files. Create `src/FundingPlatform.Web/wwwroot/lib/tabler/VERSION.txt` containing the version string and the date copied (one line each).
- [X] T005 Verify the vendored Tabler asset surface is present: `src/FundingPlatform.Web/wwwroot/lib/tabler/dist/css/tabler.min.css`, `dist/js/tabler.min.js`, `dist/css/tabler-icons.min.css`, and the icon font files exist and are non-empty. (Files are static; this is a one-time sanity check.)

**Checkpoint**: Tabler vendored; baseline build and tests are green; PDF baseline captured. No view changes yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the seven new presentation-layer record types and the `StatusVisualMap` helper, plus the `BasePage` E2E PageObject. These are needed by both US1 (sidebar entries, role gating) and US2/US3 (partial input shapes), so they belong in foundational scope rather than any single user story.

**⚠️ CRITICAL**: No US1 / US2 / US3 work should begin until this phase is complete; the partials and views built in later phases depend on these types.

- [X] T006 [P] Create `src/FundingPlatform.Web/Models/StatusVisual.cs` per `data-model.md §"StatusVisual (record)"`: `public sealed record StatusVisual(string Color, string Icon, string DisplayLabel);` in namespace `FundingPlatform.Web.Models`.
- [X] T007 [P] Create `src/FundingPlatform.Web/Models/ActionClass.cs` per `data-model.md §"ActionClass (enum)"`: `public enum ActionClass { Primary = 0, Secondary = 1, Destructive = 2, StateLocking = 3 }` in namespace `FundingPlatform.Web.Models`.
- [X] T008 [P] Create `src/FundingPlatform.Web/Models/ActionItem.cs` per `data-model.md §"ActionItem (record)"`: positional record with the eight fields (`Label`, `Class`, `Url?`, `FormController?`, `FormAction?`, `FormRouteValues?`, `Icon?`, `ConfirmDialogId?`).
- [X] T009 [P] Create `src/FundingPlatform.Web/Models/BreadcrumbItem.cs` per `data-model.md §"BreadcrumbItem (record)"`: `public sealed record BreadcrumbItem(string Label, string? Url = null);`.
- [X] T010 [P] Create `src/FundingPlatform.Web/Models/DocumentReference.cs` per `data-model.md §"DocumentReference (record)"`: positional record with the seven fields (`Filename`, `SizeBytes`, `DownloadUrl`, `Signer?`, `GeneratedAt?`, `SignedAt?`, `IconOverride?`).
- [X] T011 [P] Create `src/FundingPlatform.Web/Models/TimelineEvent.cs` per `data-model.md §"TimelineEvent (record)"`: positional record with the six fields (`At`, `Actor`, `Action`, `Detail?`, `Icon?`, `Color?`).
- [X] T012 [P] Create `src/FundingPlatform.Web/Models/SidebarEntry.cs` per `data-model.md §"SidebarEntry (record)"`: positional record with the five fields (`Label`, `Url`, `Icon`, `AllowedRoles[]`, `ShowToUnauthenticated = false`).
- [X] T013 [P] Create `src/FundingPlatform.Web/Models/PageHeaderViewModel.cs` per `contracts/README.md §1.1`: positional record with `Title`, `Subtitle?`, `Breadcrumbs?`, `PrimaryActions?`.
- [X] T014 [P] Create `src/FundingPlatform.Web/Models/StatusPillViewModel.cs` per `contracts/README.md §1.2`: `public sealed record StatusPillViewModel(object EnumValue);`.
- [X] T015 [P] Create `src/FundingPlatform.Web/Models/DataTableViewModel.cs` and `DataTableDensity.cs` per `contracts/README.md §1.3`: the record with five fields plus the `DataTableDensity { Comfortable, Compact }` enum, in the same file or two adjacent files (your call).
- [X] T016 [P] Create `src/FundingPlatform.Web/Models/FormSectionViewModel.cs` per `contracts/README.md §1.4`: positional record with `Label`, `Hint?`, `ForFieldName`, `BodyRenderer?`.
- [X] T017 [P] Create `src/FundingPlatform.Web/Models/EmptyStateViewModel.cs` per `contracts/README.md §1.5`: positional record with `Headline`, `Body`, `Icon = "ti ti-mood-empty"`, `PrimaryAction?`.
- [X] T018 [P] Create `src/FundingPlatform.Web/Models/ActionBarViewModel.cs` and `ActionBarAlignment.cs` per `contracts/README.md §1.6`: the record with `Actions`, `Alignment` plus the `ActionBarAlignment { Start, End, SpaceBetween }` enum.
- [X] T019 [P] Create `src/FundingPlatform.Web/Models/EventTimelineViewModel.cs` per `contracts/README.md §1.8`: positional record with `Events`, `ShowEmptyMessage = true`.
- [X] T020 [P] Create `src/FundingPlatform.Web/Models/ConfirmDialogViewModel.cs` per `contracts/README.md §1.9`: positional record with the nine fields (`Id`, `Title`, `IrreversibilityRationale`, `ConfirmLabel`, `CancelLabel = "Cancel"`, `ConfirmClass = ActionClass.Destructive`, `FormController = ""`, `FormAction = ""`, `FormRouteValues?`).
- [X] T021 [P] Create `src/FundingPlatform.Web/Helpers/StatusVisualMap.cs` per `data-model.md §"StatusVisualMap (static class)"` and `contracts/README.md §2`: static class with four `For(...)` overloads (one per supported enum: `ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`). Each overload uses an exhaustive `switch` expression returning the canonical `StatusVisual` per the four mapping tables in `contracts/README.md §2.1`–`§2.4`. Throw `ArgumentOutOfRangeException` on unhandled values (intentional fail-fast).
- [X] T022 Modify `src/FundingPlatform.Web/Views/_ViewImports.cshtml` to add `@using FundingPlatform.Web.Helpers` and `@using FundingPlatform.Web.Models` so Razor partials and views can reference `StatusVisualMap`, `ActionClass`, etc., without fully qualified names.
- [X] T023 [P] Create `tests/FundingPlatform.Tests.E2E/PageObjects/BasePage.cs` per `research.md §R-008`: an abstract or virtual base class exposing four Locator properties — `Sidebar`, `Topbar`, `PageTitle`, `BreadcrumbContainer` — anchored on `[data-testid="sidebar"]`, `[data-testid="topbar"]`, `[data-testid="page-title"]`, `[data-testid="breadcrumbs"]`. Other PageObjects will be retrofitted to inherit (or compose) this base in US1 / US2 tasks.

**Checkpoint**: All presentation-layer types compile. `dotnet build` is green. No view changes yet. The library is in place but unused.

---

## Phase 3: User Story 1 — Themed shell so every existing screen looks like one product (Priority: P1) 🎯 MVP

**Goal**: Replace `_Layout.cshtml` with a Tabler shell (collapsible sidebar with role-aware nav, topbar with brand and identity, breadcrumb slot, page-header slot). Add `_AuthLayout.cshtml` for Login/Register. Add the new `RoleAwareSidebarTests`. Update the existing PageObjects' shell selectors so the existing E2E suite continues to pass under the new shell. **No view-side markup changes** — every existing view continues to render unchanged inside the new shell via CSS cascade. This is the MVP per spec US1.

**Independent Test**: `RoleAwareSidebarTests` passes (four methods). The full existing E2E suite passes after the layout swap and the PageObject selector updates. Manual: walk every menu entry as each role and confirm clean console (quickstart.md §A.1, A.2, A.3 minus the partial-specific assertions, which arrive in US2).

### Tests for User Story 1 (write first; expect red before implementation)

- [ ] T024 [P] [US1] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/LoginPage.cs` with an `IsAuthShellVisible()` predicate that asserts the page contains a Tabler `page-center` chrome and **does not** contain `[data-testid="sidebar"]`. This page object's existing assertions stay intact.
- [ ] T025 [P] [US1] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/RegisterPage.cs` with the same `IsAuthShellVisible()` predicate as T024.
- [ ] T026 [US1] Create `tests/FundingPlatform.Tests.E2E/Tests/RoleAwareSidebarTests.cs` per `contracts/README.md §3 "Test contract"` with **four** `[Test]` methods:
  - `ApplicantSidebarShowsApplicantEntries` — log in as Applicant, navigate to `/`, assert sidebar contains exactly Home + My Applications, and does NOT contain Review Queue, Signing Inbox, or Admin.
  - `ReviewerSidebarShowsReviewerEntries` — log in as Reviewer, assert sidebar contains exactly Home + Review Queue + Signing Inbox, and does NOT contain My Applications or Admin.
  - `AdminSidebarShowsAdminEntries` — log in as Admin, assert sidebar contains Home + Review Queue + Signing Inbox + Admin.
  - `UnauthenticatedAuthShellOmitsSidebar` — visit `/Account/Login` and `/Account/Register` unauthenticated; assert no `[data-testid="sidebar"]` element is present and the auth shell is visible (use the `IsAuthShellVisible()` predicate from T024 / T025).
  All four MUST fail before T029–T033 implementation lands (red state).

### Implementation for User Story 1

- [ ] T027 [US1] Create `src/FundingPlatform.Web/Views/Shared/_AuthLayout.cshtml` per `research.md §R-005`: a Razor layout file with `<!DOCTYPE html>`, head referencing `~/lib/tabler/dist/css/tabler.min.css` and `~/lib/tabler/dist/css/tabler-icons.min.css`, body wrapped in Tabler's `page page-center` chrome (centered card pattern), `@RenderBody()` inside the card. **No sidebar, no topbar.** Include scripts `~/lib/tabler/dist/js/tabler.min.js` at end of body. Title from `@ViewData["Title"]`.
- [ ] T028 [US1] Modify `src/FundingPlatform.Web/Views/Account/Login.cshtml` to set `Layout = "_AuthLayout";` in the top `@{ }` block. Do NOT change form markup, controller post target, anti-forgery token, or any tag helper. (Form internals get re-skinned in US3.)
- [ ] T029 [US1] Modify `src/FundingPlatform.Web/Views/Account/Register.cshtml` to set `Layout = "_AuthLayout";` in the top `@{ }` block. Same constraint as T028 — no internal markup changes.
- [ ] T030 [US1] **REPLACE** `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` with the Tabler shell per `plan.md §Project Structure` and `contracts/README.md §3`:
  - `<!DOCTYPE html>`, head linking `~/lib/tabler/dist/css/tabler.min.css`, `~/lib/tabler/dist/css/tabler-icons.min.css`, `~/css/site.css`, `~/FundingPlatform.Web.styles.css`. Title from `@ViewData["Title"]`.
  - Body uses Tabler's `page` wrapper containing:
    - `<aside class="navbar navbar-vertical navbar-expand-lg navbar-dark" data-testid="sidebar">` — brand at top, role-aware nav entries (filter the canonical sidebar entry list per `contracts/README.md §3` against `User.IsInRole(...)`), each entry rendered as `<a class="nav-link" href="@entry.Url"><i class="@entry.Icon"></i> @entry.Label</a>` with a stable `data-testid="sidebar-entry-<slug>"` (slug derived from label).
    - `<header class="navbar navbar-expand-md d-print-none" data-testid="topbar">` — page title from `@ViewData["Title"]` exposed as `data-testid="page-title"`, current user identity, logout form (anti-forgery preserved verbatim).
    - `<div class="page-wrapper">` containing breadcrumb slot (`data-testid="breadcrumbs"`, populated by `@RenderSection("Breadcrumbs", required: false)`), `TempData` alerts (success / error) styled via Tabler `alert alert-dismissible`, and `<main role="main">@RenderBody()</main>`.
    - Footer.
  - Scripts `~/lib/tabler/dist/js/tabler.min.js`, jQuery (kept), `~/js/site.js`, `@RenderSection("Scripts", required: false)`.
  - Define the canonical sidebar entry list inline (or in a `_SidebarEntries.cshtml` partial referenced from this file — author's choice) per `contracts/README.md §3` table.
- [ ] T031 [US1] Modify `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml.css` to remove any styles that targeted the prior Bootstrap navbar but no longer apply, OR delete the file if no overrides remain. (Trivial; the Tabler-base shell does not need most of the prior overrides.)
- [ ] T032 [US1] Modify `src/FundingPlatform.Web/Views/Shared/Error.cshtml` to render its content inside the new shell with consistent error-page styling (a centered Tabler `empty` block with an error icon, the error code, and the message). Preserve the controller binding and the `RequestId` model field.
- [ ] T033 [US1] Modify `src/FundingPlatform.Web/wwwroot/css/site.css` to remove any project-specific overrides that conflict with Tabler defaults; keep the file as the single home for legitimate overrides going forward (FR-017 will be enforced in US3 grep checks).

### PageObject selector updates for User Story 1 (FR-019: existing E2E must keep passing)

These are needed because the shell DOM changes in T030 — the prior navbar selectors (`.navbar.navbar-dark.bg-primary`) no longer exist. Each PageObject is updated to inherit/compose `BasePage` (T023) so future shell changes touch one file, not many. Assertions on user-visible behavior remain identical (FR-019).

- [ ] T034 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicationPage.cs` to inherit from / compose `BasePage` (T023) and replace any prior shell-locator strings with calls into the base. Existing assertions on application-list rows, status badges (still raw Bootstrap badges in US1), and action links remain intact.
- [ ] T035 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewQueuePage.cs` similarly (inherit BasePage; preserve queue-row, tab-nav assertions from spec 007).
- [ ] T036 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs` similarly.
- [ ] T037 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/SigningReviewInboxPage.cs` similarly.
- [ ] T038 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicantResponsePage.cs` similarly (preserve banner predicates from spec 007).
- [ ] T039 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/SigningStagePanelPage.cs` similarly (panel selector remains unchanged because the panel partial has not yet been re-skinned in US1).
- [ ] T040 [P] [US1] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/AdminPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/ItemPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/QuotationPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/SupplierPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/AppealThreadPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementDownloadFlow.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementPanelPage.cs` to use `BasePage` for shell selectors. Most non-shell selectors do not change in US1; document anything that did need changing.
- [ ] T041 [US1] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm:
  - All four `RoleAwareSidebarTests` are now green (red→green transition from T026).
  - The entire prior E2E suite (specs 001–007 tests) is still green.
  If any prior test regresses, the regression IS the defect — diagnose and fix in this task before checkpoint.

**Checkpoint**: US1 is fully functional. The platform now lives inside the Tabler shell. Every existing view continues to render via CSS cascade. The role-aware sidebar test class passes. The full E2E suite is green. **MVP ship of 008 is defensible at this checkpoint** — visual consistency at the shell level is delivered, even though individual view markup is still pre-spec.

---

## Phase 4: User Story 2 — Reusable component library, with high-traffic surfaces refactored first (Priority: P2)

**Goal**: Build all nine reusable Razor partials per `contracts/README.md §1`. Refactor the five high-traffic surfaces (`Application/Index`, `Application/Details`, `Review/Index`, `Review/Review`, `Review/SigningInbox`) to consume the partials. After this story, the surfaces driving daily workflow speak one consistent visual language for status, headers, actions, and documents.

**Independent Test**: The five high-traffic views render with `_PageHeader`, `_StatusPill`, `_DataTable`, `_DocumentCard`, `_ActionBar`, `_ConfirmDialog`, and `_EmptyState` per `quickstart.md §A.1` (steps 2, 5, 6) and `§A.2` (steps 2–7). All existing E2E tests for these surfaces continue to pass after PageObject selector retargeting.

### Build the partial library (parallel-safe; each partial is its own file)

- [ ] T042 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_PageHeader.cshtml` per `contracts/README.md §1.1`. Model: `PageHeaderViewModel`. Renders Tabler `.page-header` block with title (`data-testid="page-title"`), optional subtitle (`data-testid="page-subtitle"`), optional breadcrumbs slot (`data-testid="breadcrumbs"`), optional actions slot (`data-testid="page-header-actions"` — internally invokes `_ActionBar` if `PrimaryActions` is non-null/empty). Long-title truncation via Tabler `text-truncate`.
- [ ] T043 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_StatusPill.cshtml` per `contracts/README.md §1.2`. Model: `StatusPillViewModel`. Pattern-matches `EnumValue` against the four supported enums and calls the appropriate `StatusVisualMap.For(...)` overload. Renders `<span class="badge bg-<color>" data-testid="status-pill" data-status-enum="<EnumTypeName>" data-status-value="<EnumValue>"><i class="<icon>"></i> <label></span>`. **Sole badge renderer** — invariant enforced by SC-006 grep.
- [ ] T044 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_DataTable.cshtml` per `contracts/README.md §1.3`. Model: `DataTableViewModel`. Wraps Tabler `<div class="card" data-testid="data-table">` containing `<table class="table card-table" data-testid="data-table-table">`. Adds `.table-sm` when `Density == Compact`. Renders inline `_EmptyState` (via `Html.PartialAsync`) inside `<div data-testid="data-table-empty">` when `Rows.Count == 0` and `EmptyState != null`.
- [ ] T045 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_FormSection.cshtml` per `contracts/README.md §1.4`. Model: `FormSectionViewModel` plus an inline body (use a `RenderBody`-equivalent via `@section` or a static helper extension `FormSectionScopeAsync`; implementation choice is the author's). Outputs Tabler form-group wrapper with label, optional hint (`<small class="form-hint">`), body slot, validation feedback (`<div class="invalid-feedback">` — same selector `asp-validation-for` writes to). `data-testid="form-section"`, `data-field="<ForFieldName>"`.
- [ ] T046 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_EmptyState.cshtml` per `contracts/README.md §1.5`. Model: `EmptyStateViewModel`. Renders Tabler `.empty` block with `<div class="empty-icon">`, `<p class="empty-title" data-testid="empty-state-headline">`, `<p class="empty-subtitle" data-testid="empty-state-body">`, optional `<div class="empty-action">`. Root `data-testid="empty-state"`.
- [ ] T047 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_ActionBar.cshtml` per `contracts/README.md §1.6`. Model: `ActionBarViewModel`. Outputs `<div class="btn-list" data-testid="action-bar">`. For each `ActionItem`, renders either an `<a>` (when `Url != null`) or a `<button>` inside a small `<form>` (when `FormController + FormAction` set), with classes per the canonical mapping (Primary→`btn btn-primary`, Secondary→`btn btn-outline-secondary`, Destructive→`btn btn-danger`, StateLocking→`btn btn-warning` + `ti ti-lock`). **Throw at render time** if an action with `Class == Destructive` or `StateLocking` lacks a `ConfirmDialogId` (FR-010 enforcement). Wire confirm-dialog actions with `data-bs-toggle="modal" data-bs-target="#<ConfirmDialogId>"`.
- [ ] T048 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_DocumentCard.cshtml` per `contracts/README.md §1.7`. Model: `DocumentReference`. Outputs Tabler card with file icon (default `ti ti-file-text`; PDF gets `ti ti-file-type-pdf` based on filename extension or explicit `IconOverride`), filename (`data-testid="document-card-filename"`), size formatted as MB/KB (`data-testid="document-card-size"`), optional metadata block (`data-testid="document-card-meta"` showing signer + signed-at OR generated-at), download link (`data-testid="document-card-download"`). Root `data-testid="document-card"`, `data-filename="<filename>"`. **Sole document renderer** — invariant enforced by SC-003/SC-006 grep.
- [ ] T049 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_EventTimeline.cshtml` per `contracts/README.md §1.8`. Model: `EventTimelineViewModel`. Outputs `<ul class="timeline" data-testid="event-timeline">` with one `<li class="timeline-event" data-testid="timeline-event" data-at="<ISO>">` per `TimelineEvent`, sorted descending by `At`. When `Events` empty AND `ShowEmptyMessage`, render small inline "No events yet" line.
- [ ] T050 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_ConfirmDialog.cshtml` per `contracts/README.md §1.9`. Model: `ConfirmDialogViewModel`. Outputs `<div class="modal modal-blur fade" id="<Id>" data-testid="confirm-dialog" data-confirm-id="<Id>">` with title, prominent `<p class="confirm-rationale" data-testid="confirm-rationale">`, cancel `<button data-testid="cancel-button">`, submit `<button type="submit" class="btn btn-<class>" data-testid="confirm-button">` inside a `<form>` posting to `(FormController, FormAction, FormRouteValues)`.

### Tests for User Story 2 (write first; expect red before view refactors)

- [ ] T051 [P] [US2] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicationPage.cs` with stable selectors targeting the new `data-testid` attributes that will appear in the application list and details views after T053–T054: `[data-testid="page-header"]`, `[data-testid="status-pill"]` (with `data-status-enum="ApplicationState"`), `[data-testid="data-table"]`, `[data-testid="document-card"]`, `[data-testid="action-bar"]`, `[data-testid="confirm-dialog"]`. Existing assertions remain; new helper methods make future tests easier.
- [ ] T052 [P] [US2] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewQueuePage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs`, and `tests/FundingPlatform.Tests.E2E/PageObjects/SigningReviewInboxPage.cs` with the same stable-selector helpers as T051, scoped to the surfaces they manage. (One coordinated commit across the three files since they share the partial vocabulary.)

### Refactor high-traffic views to consume partials

- [ ] T053 [US2] Refactor `src/FundingPlatform.Web/Views/Application/Index.cshtml` per `quickstart.md §A.1` step 2: replace the inline `<h2>` with `_PageHeader` (title "My Applications", primary action "Create Application" → `Application/Create`). Replace the table with `_DataTable` (caption "Your applications", columns [ID, Status, Items, Created, Last Updated, Submitted, Actions]). Each row's Status cell renders via `_StatusPill` (passing `app.State` as `ApplicationState`). When the list is empty, `_DataTable` renders `_EmptyState` ("No applications yet" + primary action "Create your first application"). **Preserve every controller binding and `app.*` property name verbatim.**
- [ ] T054 [US2] Refactor `src/FundingPlatform.Web/Views/Application/Details.cshtml` per `quickstart.md §A.1` step 4 and `§A.2` step 5: `_PageHeader` (title from `Model.ApplicationReference` or similar, subtitle showing applicant name, breadcrumbs `Home > My Applications > Details`, primary actions per current state — Submit / Add Item / etc.). `_StatusPill` for the application state. Items table via `_DataTable` with each item's `ItemReviewStatus` rendered through `_StatusPill`. Document references (quotations, generated funding agreement, signed PDFs) rendered via `_DocumentCard`. State-locking actions (Submit) wired to a `_ConfirmDialog`. Preserve the existing async-fetch funding-agreement panel embed (see spec 007 contract — embed structure unchanged in this spec; only the panel partial's internal markup is re-skinned in US3).
- [ ] T055 [US2] Refactor `src/FundingPlatform.Web/Views/Review/Index.cshtml` per `quickstart.md §A.2` step 2: `_PageHeader` (title "Review Queue", subtitle showing total count). Keep `_ReviewTabs` partial (will be re-styled in T058). `_DataTable` with `Density: Compact` for the queue. `_StatusPill` for `ApplicationState` per row. `_EmptyState` ("No applications awaiting review") replaces the prior `alert alert-info`. Preserve `ViewData["ActiveTab"] = "Initial"` from spec 007.
- [ ] T056 [US2] Refactor `src/FundingPlatform.Web/Views/Review/Review.cshtml` per `quickstart.md §A.2` step 3: `_PageHeader` (title from application reference, subtitle with applicant), `_StatusPill` for application state and per-item review state, `_DocumentCard` for quotations and generated PDFs, `_ActionBar` with Approve / Reject / NeedsInfo / SendBack actions classified per spec (Approve = StateLocking when finalizing the application, otherwise Primary; Reject = Destructive; NeedsInfo = Secondary). Wire each StateLocking and Destructive action to a `_ConfirmDialog` rendered at the bottom of the page.
- [ ] T057 [US2] Refactor `src/FundingPlatform.Web/Views/Review/SigningInbox.cshtml` per `quickstart.md §A.2` step 6: `_PageHeader` (title "Signing Inbox", subtitle showing count). Keep `_ReviewTabs` (re-styled in T058). `_DataTable` with `Density: Compact`. `_StatusPill` for `SignedUploadStatus` per row. `_EmptyState` ("No items awaiting signing review"). Preserve `ViewData["ActiveTab"] = "Signing"` from spec 007.
- [ ] T058 [US2] Restyle `src/FundingPlatform.Web/Views/Review/_ReviewTabs.cshtml` to use Tabler `nav nav-pills nav-fill` (or `nav-bordered` per Tabler convention) instead of vanilla Bootstrap pills. Preserve the two `<a>` tabs, their `Url.Action` targets, the `data-testid` attributes from spec 007 (`review-tab-initial`, `review-tab-signing`), and the `active`-class behavior driven by `ViewData["ActiveTab"]`. **Do not rename or remove the `data-testid` attributes** (FR-019: spec 007 tests depend on them).
- [ ] T059 [US2] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm:
  - All existing tests for specs 001–007 still pass (FR-019 honored).
  - The new test helpers in T051/T052 still compile (no new tests introduced yet — the partial-presence assertions live inside the existing test bodies as strengthened locators when convenient, but the spec does not mandate new tests for US2 beyond the sidebar test in US1).
  - Any test that broke due to selector drift gets repaired by retargeting via the new `data-testid` attributes — semantics MUST NOT relax (FR-019).

**Checkpoint**: US2 is fully functional. The five high-traffic surfaces speak one visual language. The component library is in place and demonstrably consumed. Existing E2E suite remains green via stable `data-testid` selectors.

---

## Phase 5: User Story 3 — Remaining views swept; visual-consistency invariants enforced (Priority: P3)

**Goal**: Sweep the lower-traffic surfaces — `Account`, `Admin`, `ApplicantResponse`, `Item`, `Quotation`, `Home`, smaller `Review` and `FundingAgreement` views — to consume the partial library. Enforce the grep-clean invariants (no badge markup outside `_StatusPill`, no document refs outside `_DocumentCard`, no inline `style=`).

**Independent Test**: All `quickstart.md §C` grep checks return zero matches outside allowed locations. The full E2E suite passes (selectors retargeted as needed).

### Refactor remaining views (parallel-safe by file)

- [ ] T060 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Account/Login.cshtml` to use `_FormSection` for each input (Username, Password, Remember Me) and `_ActionBar` for Submit + secondary "Register" link. Layout already set to `_AuthLayout` in T028. Preserve every `asp-for`, `asp-validation-for`, anti-forgery token, post target.
- [ ] T061 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Account/Register.cshtml` analogously to T060.
- [ ] T062 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Admin/Index.cshtml` to use `_PageHeader` + a Tabler card grid (each card a link to a sub-section: Configuration, Impact Templates).
- [ ] T063 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Admin/Configuration.cshtml` to use `_PageHeader` + `_FormSection` per setting + `_ActionBar` (Save = Primary).
- [ ] T064 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Admin/ImpactTemplates.cshtml` to use `_PageHeader` (with primary action "Create Template") + `_DataTable` + `_EmptyState`.
- [ ] T065 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Admin/CreateTemplate.cshtml` to use `_PageHeader` + `_FormSection` per field + `_ActionBar` (Create = Primary, Cancel = Secondary).
- [ ] T066 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Admin/EditTemplate.cshtml` analogously to T065 (with Update = Primary, Delete = Destructive wired to `_ConfirmDialog`).
- [ ] T067 [P] [US3] Refactor `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml` to use `_PageHeader` (title from application reference, status-driven banner expressed via the subtitle slot per spec 007), `_StatusPill` for the application state, `_ActionBar` for response actions. Preserve the spec 007 banner predicates (`data-testid="signing-banner-ready"`, `data-testid="signing-banner-executed"`) verbatim — FR-019 prohibits removing them.
- [ ] T068 [P] [US3] Refactor `src/FundingPlatform.Web/Views/ApplicantResponse/Appeal.cshtml` to use `_PageHeader` + `_FormSection` (appeal text) + `_ActionBar` (Submit Appeal = Primary, Cancel = Secondary).
- [ ] T069 [P] [US3] Restyle `src/FundingPlatform.Web/Views/ApplicantResponse/_AppealMessage.cshtml` to use Tabler comment-row markup (avatar, name, timestamp, message body). Preserve the message structure and any data-test hooks already in use.
- [ ] T070 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Application/Create.cshtml` to use `_PageHeader` + `_FormSection` (description if present) + `_ActionBar` (Create Draft = Primary, Back = Secondary).
- [ ] T071 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Application/Edit.cshtml` analogously to T070.
- [ ] T072 [P] [US3] Restyle `src/FundingPlatform.Web/Views/Application/_FundingAgreementPanel.cshtml` to themed Tabler markup. Preserve the spec 006/007 panel selector contract (the panel is embedded on two host pages) and any `data-testid` hooks.
- [ ] T073 [P] [US3] Refactor `src/FundingPlatform.Web/Views/FundingAgreement/Details.cshtml` to use `_PageHeader` + `_StatusPill` for `SignedUploadStatus` + `_DocumentCard` for the generated PDF and any signed uploads + `_ActionBar` (Generate / Download / Approve / Reject classified per spec).
- [ ] T074 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Home/Index.cshtml` to use `_PageHeader` + a role-aware welcome card grid (different card sets for Applicant / Reviewer / Admin / unauthenticated). Preserve the existing controller bindings.
- [ ] T075 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Item/Add.cshtml` to use `_PageHeader` + `_FormSection` per field + `_ActionBar`.
- [ ] T076 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Item/Edit.cshtml` analogously to T075.
- [ ] T077 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Item/Impact.cshtml` to use `_PageHeader` + `_FormSection`.
- [ ] T078 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Quotation/Add.cshtml` to use `_PageHeader` + `_FormSection` (quotation amount, supplier, attached document) + `_ActionBar` (Save = Primary, Back = Secondary).
- [ ] T079 [P] [US3] Refactor `src/FundingPlatform.Web/Views/Review/GenerateAgreement.cshtml` to use `_PageHeader` + `_ActionBar` (Generate = StateLocking, Cancel = Secondary) wired to a `_ConfirmDialog`.

### Update remaining PageObjects to track new partial markup

- [ ] T080 [P] [US3] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/ItemPage.cs` to retarget any selectors that broke due to T075–T077; use `_FormSection`-anchored selectors (`[data-testid="form-section"][data-field="<name>"]`) where helpful. Preserve all assertions on user-visible behavior.
- [ ] T081 [P] [US3] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/QuotationPage.cs` analogously for T078.
- [ ] T082 [P] [US3] Modify `tests/FundingPlatform.Tests.E2E/PageObjects/AdminPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/AppealThreadPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/SupplierPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementDownloadFlow.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementPanelPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/LoginPage.cs`, `tests/FundingPlatform.Tests.E2E/PageObjects/RegisterPage.cs` to retarget any selectors that broke due to T060–T074. **No assertion semantics relaxed.**

### Enforce visual-consistency invariants

- [ ] T083 [US3] Run the four grep commands in `quickstart.md §C` and confirm each returns zero matches outside the allowed locations:
  - Badge markup outside `_StatusPill.cshtml` (and the PDF target files): zero matches.
  - Inline `style="..."` attributes in `Views/` (excluding the PDF target files): zero matches.
  - Bare PDF anchor-tag markup outside `_DocumentCard.cshtml` (and the PDF target files): zero matches.
  - `alert-info` outside `_Layout.cshtml` (which legitimately renders `TempData` alerts): zero matches.
  If any grep returns a match outside allowed locations, the offending view is incomplete — return to it in this task.
- [ ] T084 [US3] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm the entire E2E suite remains green after the lower-traffic sweep + PageObject retargeting. FR-019 invariant must hold.

**Checkpoint**: US3 is fully functional. The entire view tree (excluding the PDF target files) speaks one visual language. The grep invariants hold. The E2E suite is green. The MVP delivered at the US1 checkpoint is now polished across every surface.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Manual verification per `quickstart.md`, PDF parity check, viewport sanity, full final test sweep.

- [ ] T085 Run `dotnet test --nologo` (full suite — Unit, Integration, E2E) at the repo root and confirm zero failures across all three tiers. If any non-E2E test regresses, the regression is the defect (FR-016 forbids logic changes; any unit/integration regression therefore indicates an unintended logic touch).
- [ ] T086 Walk `quickstart.md §A.1` (Applicant golden path) end-to-end as the Applicant test user. Confirm every assertion. DevTools console must show zero errors and zero warnings.
- [ ] T087 Walk `quickstart.md §A.2` (Reviewer golden path) end-to-end as the Reviewer test user. Same console-clean requirement.
- [ ] T088 Walk `quickstart.md §A.3` (Admin golden path) end-to-end as the Admin test user. Same console-clean requirement.
- [ ] T089 Walk `quickstart.md §B` (Sidebar visibility) for all four user states. Manual sanity check that mirrors what `RoleAwareSidebarTests` automates.
- [ ] T090 Run the four grep commands from `quickstart.md §C` one final time and confirm each returns zero matches outside allowed locations. (Re-run of T083 to catch any drift introduced after.)
- [ ] T091 Execute `quickstart.md §D` PDF parity check: (a) `git diff main -- src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml src/FundingPlatform.Web/Views/FundingAgreement/_FundingAgreementLayout.cshtml` MUST report zero changes (FR-015 / SC-007 byte-identity); (b) generate a fresh PDF for the same application that produced `baseline-pre-008.pdf` (T001), save as `current-008.pdf`, open both side-by-side and confirm visual identity. If either check fails, diagnose immediately — likely a CSS leak or accidental modification of the PDF target.
- [ ] T092 Execute `quickstart.md §E` viewport sanity at desktop (1280×800) and mobile (360×740) viewports in both Chromium and Firefox. Confirm sidebar collapse, table responsive scroll, page-header truncation, and modal stacking all behave per Tabler defaults.

**Checkpoint**: Every spec-mandated invariant is verified. SC-001 through SC-008 met. Spec 008 is ready to merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 baseline capture is independent of code; T002–T003 require no changes; T004–T005 vendor assets. T002, T003, T004, T005 can run in parallel after T001.
- **Foundational (Phase 2)**: All T006–T020 record types are independent files — fully parallel. T021 depends on T006 (StatusVisual). T022 depends on T021. T023 PageObject base is independent of the rest.
- **US1 (Phase 3)**: depends on Phase 2 (uses `SidebarEntry` for the sidebar, sidebar test depends on the new shell). T024–T025 page-object extensions can be parallel; T026 test class depends on them. T027 (`_AuthLayout`) is independent; T028–T029 (Login/Register Layout switches) depend on T027. T030 (`_Layout` rewrite) is the central change; T031–T033 (CSS, Error, site.css) follow it. T034–T040 PageObject updates depend on T030. T041 (test run) depends on everything in US1.
- **US2 (Phase 4)**: depends on Phase 2 (record types) and Phase 3 (shell exists). T042–T050 partials are fully parallel. T051–T052 page-object selector helpers parallel to partials. T053–T058 view refactors depend on the partials they consume — most are parallel-safe (different view files), but each view depends on its specific partials being merged first. T059 test run gates the phase.
- **US3 (Phase 5)**: depends on Phase 4 (partial library exists). T060–T079 view refactors are parallel-safe by file. T080–T082 PageObject updates depend on the corresponding view refactors being merged. T083 grep verification gates the phase. T084 test run gates the phase.
- **Polish (Phase N)**: depends on US3 completion. T085–T092 are sequential manual verifications; T091 specifically requires `baseline-pre-008.pdf` from T001.

### User Story Dependencies

- **US1 (P1)**: depends on Phase 1 + Phase 2. MVP candidate on its own — at the US1 checkpoint, the system has a unified shell and the role-aware sidebar test class, even though individual view markup is still pre-spec.
- **US2 (P2)**: depends on Phase 1 + Phase 2 + US1 (every refactored view renders inside the new shell). Touches the five high-traffic surfaces.
- **US3 (P3)**: depends on Phase 1 + Phase 2 + US1 + US2 (lower-traffic surfaces use the same partials built in US2). Cleanup-style story.

### Within Each User Story

- US1: tests written first (T024–T026 expected red); shell built (T027–T033); selectors updated (T034–T040); test run (T041 green).
- US2: partials built (T042–T050) in parallel; PageObject helpers extended (T051–T052); high-traffic views refactored (T053–T058) per parallel/sequential flags; test run (T059 green).
- US3: lower-traffic views refactored (T060–T079) in parallel; PageObjects retargeted (T080–T082); grep invariants enforced (T083 green); test run (T084 green).

### Parallel Opportunities

- Phase 2: T006–T020 (15 record-type files) and T023 (PageObject base) can land in one parallel batch — 16 parallel tasks.
- US1 PageObject updates: T034–T040 are different files — full parallelism across 7 PageObject files.
- US2 partials: T042–T050 are different files — full parallelism across 9 partials.
- US2 view refactors: T053–T058 are different views — parallel-safe except where one view consumes another's output (none here).
- US3 view refactors: T060–T079 are different views — full parallelism across 20 refactors. PageObject updates T080–T082 follow the corresponding view changes.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Land all record types in one parallel batch:
Task: "T006 Create StatusVisual.cs"
Task: "T007 Create ActionClass.cs"
Task: "T008 Create ActionItem.cs"
Task: "T009 Create BreadcrumbItem.cs"
Task: "T010 Create DocumentReference.cs"
Task: "T011 Create TimelineEvent.cs"
Task: "T012 Create SidebarEntry.cs"
Task: "T013 Create PageHeaderViewModel.cs"
Task: "T014 Create StatusPillViewModel.cs"
Task: "T015 Create DataTableViewModel.cs + DataTableDensity.cs"
Task: "T016 Create FormSectionViewModel.cs"
Task: "T017 Create EmptyStateViewModel.cs"
Task: "T018 Create ActionBarViewModel.cs + ActionBarAlignment.cs"
Task: "T019 Create EventTimelineViewModel.cs"
Task: "T020 Create ConfirmDialogViewModel.cs"
Task: "T023 Create BasePage.cs (E2E)"
# Then sequential: T021 (StatusVisualMap depends on StatusVisual) → T022 (_ViewImports depends on T021's namespace).
```

## Parallel Example: Phase 4 (US2 partials)

```bash
# Land all nine partials in one parallel batch (each is its own .cshtml file):
Task: "T042 _PageHeader.cshtml"
Task: "T043 _StatusPill.cshtml"
Task: "T044 _DataTable.cshtml"
Task: "T045 _FormSection.cshtml"
Task: "T046 _EmptyState.cshtml"
Task: "T047 _ActionBar.cshtml"
Task: "T048 _DocumentCard.cshtml"
Task: "T049 _EventTimeline.cshtml"
Task: "T050 _ConfirmDialog.cshtml"
# Then high-traffic view refactors T053–T058 in parallel (different .cshtml files).
```

## Parallel Example: Phase 5 (US3 view sweep)

```bash
# Twenty independent view refactors — full parallelism by file:
Task: "T060 Login.cshtml"
Task: "T061 Register.cshtml"
Task: "T062 Admin/Index.cshtml"
Task: "T063 Admin/Configuration.cshtml"
Task: "T064 Admin/ImpactTemplates.cshtml"
Task: "T065 Admin/CreateTemplate.cshtml"
Task: "T066 Admin/EditTemplate.cshtml"
Task: "T067 ApplicantResponse/Index.cshtml"
Task: "T068 ApplicantResponse/Appeal.cshtml"
Task: "T069 ApplicantResponse/_AppealMessage.cshtml"
Task: "T070 Application/Create.cshtml"
Task: "T071 Application/Edit.cshtml"
Task: "T072 Application/_FundingAgreementPanel.cshtml"
Task: "T073 FundingAgreement/Details.cshtml"
Task: "T074 Home/Index.cshtml"
Task: "T075 Item/Add.cshtml"
Task: "T076 Item/Edit.cshtml"
Task: "T077 Item/Impact.cshtml"
Task: "T078 Quotation/Add.cshtml"
Task: "T079 Review/GenerateAgreement.cshtml"
# Then T080–T082 PageObject retargeting after the view changes merge.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 → baseline green + Tabler vendored.
2. Phase 2 → record types and helper land (no behavior change yet).
3. Phase 3 (US1) → new shell + auth shell + sidebar test class + selector updates.
4. **STOP and VALIDATE**: run full E2E suite; walk every menu entry as each role; confirm console-clean.
5. **Defensible MVP ship at this checkpoint** — the platform now feels like one product even though individual view markup is still pre-spec.

### Incremental Delivery

1. Phase 1 → vendored assets + baseline green.
2. Phase 2 → presentation-layer scaffolding.
3. US1 (Phase 3) → unified shell → demo / ship.
4. US2 (Phase 4) → high-traffic surfaces speak one visual language → demo / ship.
5. US3 (Phase 5) → lower-traffic surfaces swept; grep invariants hold → demo / ship.
6. Polish (Phase N) → quickstart walkthrough + PDF parity → final merge.

### Parallel Team Strategy

With multiple developers:

1. Whole team lands Phase 1 + Phase 2 together (most of Phase 2 is parallel-safe by file).
2. Once Phase 2 lands:
   - Dev A: US1 shell rewrite + selector updates (touches `_Layout.cshtml`, `_AuthLayout.cshtml`, all PageObjects, new sidebar test class).
   - Dev B / C: can begin building partials (T042–T050) in parallel even before US1 is fully merged, since partials are independent files.
3. Once US1 merges, Dev A pivots to US2 view refactors; Dev B / C continue and pick up US3 view refactors (parallel-safe by file).
4. Final dev runs Polish (Phase N) with full suite + manual quickstart walkthrough.

---

## Notes

- Every task names exact file paths.
- No task introduces a new NuGet package, a new project, or a new domain entity.
- No task modifies controller logic, view-model property names, validation rules, persistence, or authorization.
- `Views/FundingAgreement/Document.cshtml` and `Views/FundingAgreement/_FundingAgreementLayout.cshtml` are explicitly excluded from every modify task; T091 verifies them byte-identical via `git diff`.
- `data-testid` attributes added in Phase 2 / US1 / US2 / US3 are stable contracts that future visual refactors (Tabler version bumps, etc.) MUST NOT rename — see `contracts/README.md §5`.
- Commit after each task or logical group; each phase checkpoint should keep the build and the E2E suite green. The constitution Section "Commit Discipline" requires it.
- If during US2 or US3 a refactor genuinely breaks an existing E2E test in a way that cannot be fixed by retargeting (i.e., the test was asserting on visual structure that no longer exists), pause and surface the conflict — FR-019 prohibits weakening assertions on user-visible behavior.
- Total task count: 92 (T001–T092). Setup + Foundational: 23 tasks. US1: 18 tasks. US2: 18 tasks. US3: 25 tasks. Polish: 8 tasks.
