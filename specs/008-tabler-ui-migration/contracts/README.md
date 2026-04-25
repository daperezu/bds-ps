# Contracts: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Spec:** [../spec.md](../spec.md)
**Plan:** [../plan.md](../plan.md)
**Date:** 2026-04-25

This file enumerates the contracts between the new presentation-layer components and their callers. There are no HTTP/REST/RPC contracts in this feature — the contracts are: (a) Razor partial input shapes, (b) the canonical status mapping, (c) the canonical sidebar entry list, (d) the canonical action-class visual treatment, and (e) DOM stability contracts (`data-testid` attributes) that E2E tests rely on.

---

## 1. Razor partial contracts

Each partial lives at `src/FundingPlatform.Web/Views/Shared/Components/<name>.cshtml` and is invoked via `@await Html.PartialAsync("_<name>", model)` (or `@Html.Partial(...)` in non-async contexts).

### 1.1 `_PageHeader`

**Input model:** `PageHeaderViewModel` (record, `FundingPlatform.Web.Models`)

```csharp
public sealed record PageHeaderViewModel(
    string Title,
    string? Subtitle = null,
    IReadOnlyList<BreadcrumbItem>? Breadcrumbs = null,
    IReadOnlyList<ActionItem>? PrimaryActions = null);
```

**Renders:** A Tabler page header (`.page-header` block) with title, optional subtitle, optional breadcrumb trail, and optional right-aligned primary actions (if present, rendered via `_ActionBar`).

**DOM contract:**
- Root: `<div class="page-header" data-testid="page-header">`
- Title: `<h2 class="page-title" data-testid="page-title">`
- Subtitle: `<div class="page-subtitle" data-testid="page-subtitle">` (omitted if null)
- Breadcrumbs: `<ol class="breadcrumb" data-testid="breadcrumbs">` (omitted if null/empty)
- Actions slot: `<div class="page-header-actions" data-testid="page-header-actions">` (omitted if null/empty)

**Edge cases:**
- Long titles: truncate with `text-truncate` (CSS) per spec edge case.

---

### 1.2 `_StatusPill`

**Input model:** Boxed enum value via a thin wrapper:

```csharp
public sealed record StatusPillViewModel(object EnumValue);
```

The partial pattern-matches `EnumValue` against the four supported enums and calls the appropriate `StatusVisualMap.For(...)` overload.

**Renders:** A Tabler badge with the canonical color and icon for the enum value, plus the human-readable label.

**DOM contract:**
- Root: `<span class="badge bg-<color>" data-testid="status-pill" data-status-enum="<EnumTypeName>" data-status-value="<EnumValue>">`
- Icon: `<i class="ti ti-<name>"></i>` (always present)
- Label: text node after icon

**Invariant (FR-006, SC-002, SC-006):** This partial is the **only** place in `Views/` that produces badge markup for status enums. A grep for `class="badge` outside `_StatusPill.cshtml` MUST return zero results.

**Behavior on unknown enum:** `StatusVisualMap` throws `ArgumentOutOfRangeException`. The partial does not catch — fail-fast in Razor renders a 500 in dev, which is the desired signal that a new enum value was added without updating the mapping.

---

### 1.3 `_DataTable`

**Input model:** Generic-ish convention via a `dynamic` model with these expected properties:

```csharp
public sealed record DataTableViewModel(
    string Caption,
    IReadOnlyList<string> ColumnHeaders,
    IReadOnlyList<IReadOnlyList<IHtmlContent>> Rows,
    DataTableDensity Density = DataTableDensity.Comfortable,
    EmptyStateViewModel? EmptyState = null);

public enum DataTableDensity { Comfortable = 0, Compact = 1 }
```

**Renders:** A Tabler `<div class="card">` containing a `<table class="table card-table">` with the given headers and pre-rendered cell content (callers compose cells with whatever Razor markup they need, including `_StatusPill`, `_DocumentCard`, etc.). When `Rows` is empty, renders the inline `_EmptyState` partial instead of an empty table.

**Density:** `Compact` adds `.table-sm`. Reviewer surfaces (`Review/Index`, `Review/SigningInbox`, `Review/Review`) default to `Compact`. Applicant and Admin surfaces default to `Comfortable`.

**DOM contract:**
- Root card: `<div class="card" data-testid="data-table">`
- Table: `<table class="table card-table" data-testid="data-table-table">`
- Empty state: rendered inside `<div data-testid="data-table-empty">` when present

---

### 1.4 `_FormSection`

**Input model:** Razor partial that takes `FormSectionViewModel` and uses `@RenderSection`-equivalent via a delegate or the `BodyContent` field:

```csharp
public sealed record FormSectionViewModel(
    string Label,
    string? Hint = null,
    string ForFieldName = "",
    Func<dynamic, IHtmlContent>? BodyRenderer = null);
```

In practice the partial is invoked with the body inline:

```razor
@{ var section = new FormSectionViewModel("Item description", Hint: "Up to 500 characters", ForFieldName: nameof(Model.Description)); }
@await Html.PartialAsync("_FormSection", section)
{
    <textarea asp-for="Description" class="form-control"></textarea>
    <span asp-validation-for="Description" class="invalid-feedback"></span>
}
```

(Helper extension `FormSectionScopeAsync` handles the body rendering; details are an implementation choice during /speckit-implement, not a spec contract.)

**Renders:** A Tabler form-group wrapper with label, optional hint text below the label, body slot for the actual input, and validation feedback styling that pairs with `asp-validation-for`.

**Compatibility (FR-012):** The existing `asp-validation-for` and `asp-for` tag helpers continue to drive validation message text. `_FormSection` only re-styles the surrounding container.

**DOM contract:**
- Root: `<div class="mb-3" data-testid="form-section" data-field="<ForFieldName>">`
- Label: `<label class="form-label">`
- Hint: `<small class="form-hint">` (omitted if null)
- Validation: `<div class="invalid-feedback">` (Tabler convention — same selector as `asp-validation-for` writes)

---

### 1.5 `_EmptyState`

**Input model:**

```csharp
public sealed record EmptyStateViewModel(
    string Headline,
    string Body,
    string Icon = "ti ti-mood-empty",
    ActionItem? PrimaryAction = null);
```

**Renders:** Tabler empty state pattern — centered icon, headline, one-sentence body, optional primary action button.

**DOM contract:**
- Root: `<div class="empty" data-testid="empty-state">`
- Icon: `<div class="empty-icon">`
- Headline: `<p class="empty-title" data-testid="empty-state-headline">`
- Body: `<p class="empty-subtitle" data-testid="empty-state-body">`
- Action: `<div class="empty-action">` (omitted if null)

**Invariant (FR-011, SC-004):** Bare `<div class="alert alert-info">No items yet</div>` markup MUST NOT appear in `Views/`. Grep for `alert-info` MUST only show actual user feedback alerts (TempData rendered in `_Layout`), never empty-data messaging.

---

### 1.6 `_ActionBar`

**Input model:**

```csharp
public sealed record ActionBarViewModel(
    IReadOnlyList<ActionItem> Actions,
    ActionBarAlignment Alignment = ActionBarAlignment.End);

public enum ActionBarAlignment { Start = 0, End = 1, SpaceBetween = 2 }
```

**Renders:** A Tabler `<div class="btn-list">` (or page-actions wrapper when used in `_PageHeader`) containing one button per `ActionItem`. Each button's classes derive from `ActionItem.Class` per the canonical mapping below.

**Canonical class-to-Tabler treatment (per FR-009 / R-007):**

| `ActionClass` | Button classes | Default icon | Confirms? |
|---------------|----------------|--------------|-----------|
| `Primary` | `btn btn-primary` | none | no |
| `Secondary` | `btn btn-outline-secondary` | none | no |
| `Destructive` | `btn btn-danger` | `ti ti-trash` | yes (FR-010) — `ConfirmDialogId` required |
| `StateLocking` | `btn btn-warning` | `ti ti-lock` | yes (FR-010) — `ConfirmDialogId` required |

**Validation (FR-010):** `_ActionBar` MUST throw at render time if `Destructive` or `StateLocking` action lacks a `ConfirmDialogId`. Fail-fast keeps the spec-mandated invariant honest.

**DOM contract:**
- Root: `<div class="btn-list" data-testid="action-bar" data-alignment="<start|end|space-between>">`
- Each action: `<button|a class="btn btn-<class>" data-testid="action-item" data-action-class="<class>" data-confirm-dialog-id="<id?>">`

---

### 1.7 `_DocumentCard`

**Input model:** `DocumentReference` (defined in [data-model.md](data-model.md)).

**Renders:** A Tabler card showing file icon, filename (bold), file size formatted as `MB`/`KB`, optional signer + signed-at timestamp, optional generated-at timestamp, and a download link/button.

**DOM contract:**
- Root: `<div class="card document-card" data-testid="document-card" data-filename="<filename>">`
- Icon: `<i class="ti ti-<icon>">` (default `ti ti-file-text`; PDF gets `ti ti-file-type-pdf`)
- Filename: `<div class="document-card-filename" data-testid="document-card-filename">`
- Size: `<div class="document-card-size" data-testid="document-card-size">`
- Metadata: `<div class="document-card-meta" data-testid="document-card-meta">` (omitted if no signer/timestamps)
- Download: `<a class="btn btn-sm btn-secondary" data-testid="document-card-download">`

**Invariant (FR-008, SC-003, SC-006):** `_DocumentCard` is the **only** place in `Views/` that renders references to uploaded or generated files. Grep for `<a.*\.pdf"` (case-insensitive) and for `<a.*download` MUST only match this partial's source.

---

### 1.8 `_EventTimeline`

**Input model:**

```csharp
public sealed record EventTimelineViewModel(
    IReadOnlyList<TimelineEvent> Events,
    bool ShowEmptyMessage = true);
```

**Renders:** A Tabler timeline pattern (`<ul class="timeline">`) with one entry per `TimelineEvent`, sorted descending by `At`. When `Events` is empty and `ShowEmptyMessage` is true, renders a small inline "no events yet" line (a degenerate empty state, not the full `_EmptyState` block).

**DOM contract:**
- Root: `<ul class="timeline" data-testid="event-timeline">`
- Each entry: `<li class="timeline-event" data-testid="timeline-event" data-at="<ISO>">`

---

### 1.9 `_ConfirmDialog`

**Input model:**

```csharp
public sealed record ConfirmDialogViewModel(
    string Id,
    string Title,
    string IrreversibilityRationale,
    string ConfirmLabel,
    string CancelLabel = "Cancel",
    ActionClass ConfirmClass = ActionClass.Destructive,
    string FormController = "",
    string FormAction = "",
    object? FormRouteValues = null);
```

**Renders:** A Tabler modal (`<div class="modal modal-blur fade" id="<Id>">`) with the title, the one-line `IrreversibilityRationale` displayed prominently, a cancel button, and a submit button styled per `ConfirmClass`. The submit button is the primary action that, when clicked, posts the form to `(FormController, FormAction, FormRouteValues)`.

**Wiring:** Action buttons in `_ActionBar` with `Destructive` or `StateLocking` class reference the dialog by `data-bs-toggle="modal" data-bs-target="#<ConfirmDialogId>"`.

**DOM contract:**
- Root: `<div class="modal" id="<Id>" data-testid="confirm-dialog" data-confirm-id="<Id>">`
- Rationale: `<p class="confirm-rationale" data-testid="confirm-rationale">`
- Confirm button: `<button type="submit" class="btn btn-<class>" data-testid="confirm-button">`
- Cancel button: `<button type="button" class="btn-link" data-testid="cancel-button">`

**Invariant (FR-010):** Every state-locking and destructive action invocation in the view tree MUST be wired to a `_ConfirmDialog` (enforced by `_ActionBar`'s render-time check above).

---

## 2. Canonical status mapping

Defined in `StatusVisualMap`. `_StatusPill` renders the result. **No other code path may produce a status badge** (FR-006, SC-002, SC-006).

### 2.1 `ApplicationState`

| Value | Color | Icon | Display label |
|-------|-------|------|---------------|
| `Draft` | `bg-secondary` | `ti ti-pencil` | "Draft" |
| `Submitted` | `bg-info` | `ti ti-send` | "Submitted" |
| `UnderReview` | `bg-primary` | `ti ti-eye` | "Under Review" |
| `Resolved` | `bg-success` | `ti ti-circle-check` | "Resolved" |
| `AppealOpen` | `bg-warning` | `ti ti-alert-triangle` | "Appeal Open" |
| `ResponseFinalized` | `bg-info` | `ti ti-checks` | "Response Finalized" |
| `AgreementExecuted` | `bg-success` | `ti ti-file-check` | "Agreement Executed" |

### 2.2 `ItemReviewStatus`

| Value | Color | Icon | Display label |
|-------|-------|------|---------------|
| `Pending` | `bg-secondary` | `ti ti-clock` | "Pending" |
| `Approved` | `bg-success` | `ti ti-check` | "Approved" |
| `Rejected` | `bg-danger` | `ti ti-x` | "Rejected" |
| `NeedsInfo` | `bg-warning` | `ti ti-help-circle` | "Needs Info" |

### 2.3 `AppealStatus`

| Value | Color | Icon | Display label |
|-------|-------|------|---------------|
| `Open` | `bg-warning` | `ti ti-alert-triangle` | "Open" |
| `Resolved` | `bg-success` | `ti ti-circle-check` | "Resolved" |

### 2.4 `SignedUploadStatus`

| Value | Color | Icon | Display label |
|-------|-------|------|---------------|
| `Pending` | `bg-secondary` | `ti ti-clock` | "Pending" |
| `Approved` | `bg-success` | `ti ti-check` | "Approved" |
| `Rejected` | `bg-danger` | `ti ti-x` | "Rejected" |
| `Superseded` | `bg-secondary` | `ti ti-arrow-back-up` | "Superseded" |
| `Withdrawn` | `bg-secondary` | `ti ti-arrow-back-up` | "Withdrawn" |

**Cross-enum consistency note:** Where a verb-state pair recurs across enums (e.g., "Approved" / "Rejected" / "Resolved"), the same color and icon are used. This is intentional and is the centralized-mapping payoff — a `Rejected` item review and a `Rejected` signed upload both render with the same red `ti ti-x`. Reviewers and applicants build a cross-feature visual vocabulary.

---

## 3. Canonical sidebar entry list

Defined in `_Layout.cshtml` (or in a small `SidebarEntries` static helper if cleaner). Renders the role-aware sidebar per FR-013, FR-005 user stories, and the new `RoleAwareSidebarTests` per FR-020.

| Order | Label | Url | Icon | AllowedRoles | ShowToUnauthenticated |
|------:|-------|-----|------|--------------|------------------------|
| 1 | "Home" | `/` | `ti ti-home` | (any authenticated) | false |
| 2 | "My Applications" | `/Application` | `ti ti-files` | `Applicant` | false |
| 3 | "Review Queue" | `/Review` | `ti ti-clipboard-check` | `Reviewer`, `Admin` | false |
| 4 | "Signing Inbox" | `/Review/SigningInbox` | `ti ti-pen` | `Reviewer`, `Admin` | false |
| 5 | "Admin" | `/Admin` | `ti ti-settings` | `Admin` | false |

**Behavior:**
- An entry renders only if the current user holds at least one of `AllowedRoles` (or the entry has no roles, indicating "any authenticated user").
- Unauthenticated visitors do not see the sidebar at all (they render inside `_AuthLayout`, which has no sidebar).
- The "Home" entry is always visible to authenticated users regardless of role.

**Test contract (RoleAwareSidebarTests):**
- `ApplicantSidebarShowsApplicantEntries`: Applicant sees Home + My Applications. Does NOT see Review Queue, Signing Inbox, Admin.
- `ReviewerSidebarShowsReviewerEntries`: Reviewer sees Home + Review Queue + Signing Inbox. Does NOT see My Applications (per current authorization model — applicants own apps, reviewers don't have an applicant inbox), Admin.
- `AdminSidebarShowsAdminEntries`: Admin sees Home + Review Queue + Signing Inbox + Admin. (Admins inherit Reviewer access.)
- `UnauthenticatedAuthShellOmitsSidebar`: Login and Register pages have NO `<aside class="navbar navbar-vertical">` element.

---

## 4. Action-class enum

Defined as `FundingPlatform.Web.Models.ActionClass` (see [data-model.md](data-model.md)). Mapping to Tabler classes per [Section 1.6 above](#16-_actionbar). This is the contract that prevents action-button drift across the codebase.

---

## 5. DOM stability contracts

The new partials emit `data-testid` attributes (and a few `data-status-enum`, `data-status-value`, `data-action-class`, `data-confirm-dialog-id` attributes) per the per-partial DOM contracts in Section 1. These are the stable selectors the E2E PageObjects target. Once shipped:

- `data-testid` attributes MUST NOT be renamed without coordinated PageObject updates.
- New partials introduced in future specs SHOULD follow the same `data-testid` convention.
- Pure CSS class changes (e.g., a Tabler version bump renaming `.empty-title` → `.empty-headline`) are tolerable as long as the `data-testid` selectors remain anchored.

This contract makes FR-019 enforceable: existing E2E assertions on user-visible behavior continue to work because PageObjects locate elements by `data-testid` rather than by CSS class structure.

---

## 6. What is NOT a contract here

- **HTTP / REST endpoints** — none added or modified. All controller actions, routes, and request/response shapes remain as defined by specs 001–007.
- **Database schema** — no changes. Dacpac unchanged.
- **Domain methods or events** — none added or modified.
- **Authorization rules** — `[Authorize(...)]` attributes on existing controllers are unchanged; the sidebar simply renders entries per the same role checks already in force.
