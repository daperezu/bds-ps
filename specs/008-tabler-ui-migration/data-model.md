# Data Model: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-25

## Domain entities

**No new domain entities. No modifications to existing entities.** This is a pure presentation-layer feature. `FundingPlatform.Domain.Entities/`, `FundingPlatform.Domain.ValueObjects/`, and `FundingPlatform.Domain.Enums/` are unchanged. EF configurations and the dacpac are unchanged.

The four existing domain enums consumed by the new presentation-layer code are referenced read-only:

| Enum | Project | Values | How consumed |
|------|---------|--------|-------------|
| `ApplicationState` | `FundingPlatform.Domain.Enums` | `Draft`, `Submitted`, `UnderReview`, `Resolved`, `AppealOpen`, `ResponseFinalized`, `AgreementExecuted` | `StatusVisualMap.For(ApplicationState)` |
| `ItemReviewStatus` | `FundingPlatform.Domain.Enums` | `Pending`, `Approved`, `Rejected`, `NeedsInfo` | `StatusVisualMap.For(ItemReviewStatus)` |
| `AppealStatus` | `FundingPlatform.Domain.Enums` | `Open`, `Resolved` | `StatusVisualMap.For(AppealStatus)` |
| `SignedUploadStatus` | `FundingPlatform.Domain.Enums` | `Pending`, `Superseded`, `Withdrawn`, `Rejected`, `Approved` | `StatusVisualMap.For(SignedUploadStatus)` |

## Presentation-layer types (new, all in Web layer)

These are the only new types introduced by this feature. None are persisted, none touch the Application or Domain layer.

### `StatusVisual` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record StatusVisual(string Color, string Icon, string DisplayLabel);
```

| Field | Type | Notes |
|-------|------|-------|
| `Color` | `string` | Tabler color token (e.g., `"bg-primary"`, `"bg-success"`, `"bg-warning"`, `"bg-danger"`, `"bg-info"`, `"bg-secondary"`). |
| `Icon` | `string` | Tabler icon class (e.g., `"ti ti-clock"`, `"ti ti-check"`, `"ti ti-alert-triangle"`). |
| `DisplayLabel` | `string` | Human-readable label rendered in the badge text. Allows "AgreementExecuted" → "Agreement Executed", etc. |

**Validation:** None — this is a presentation-layer projection of an enum.
**Lifecycle:** Constructed by `StatusVisualMap`, consumed by `_StatusPill.cshtml`, immediately discarded.
**Persistence:** None.

### `StatusVisualMap` (static class)

Namespace: `FundingPlatform.Web.Helpers`

```csharp
public static class StatusVisualMap
{
    public static StatusVisual For(ApplicationState s) => /* mapping per Contracts */;
    public static StatusVisual For(ItemReviewStatus s) => /* mapping per Contracts */;
    public static StatusVisual For(AppealStatus s) => /* mapping per Contracts */;
    public static StatusVisual For(SignedUploadStatus s) => /* mapping per Contracts */;
}
```

**Behavior:** One overload per supported enum. Each overload is a `switch` expression returning the canonical `StatusVisual` per the mapping table in [`contracts/README.md`](contracts/README.md). Throws `ArgumentOutOfRangeException` on unhandled enum values (so adding a new enum value triggers a compile-time gap that is caught by unit tests if any are added — see "Optional unit-testability" below).

**Validation:** Exhaustive `switch` per enum. Not adding new enum cases will fail closed.
**Lifecycle:** Pure function. No state.
**Persistence:** None.

### `ActionClass` (enum)

Namespace: `FundingPlatform.Web.Models`

```csharp
public enum ActionClass
{
    Primary = 0,
    Secondary = 1,
    Destructive = 2,
    StateLocking = 3
}
```

**Behavior:** Used as the `Class` field on the `_ActionBar` partial's input record. Directly drives the Tabler CSS classes and the optional lock-icon decoration per FR-009 / R-007.
**Validation:** None.
**Lifecycle:** Compile-time constant.

### `ActionItem` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record ActionItem(
    string Label,
    ActionClass Class,
    string? Url = null,
    string? FormController = null,
    string? FormAction = null,
    object? FormRouteValues = null,
    string? Icon = null,
    string? ConfirmDialogId = null);
```

| Field | Type | Notes |
|-------|------|-------|
| `Label` | `string` | Visible button text. |
| `Class` | `ActionClass` | Drives styling per R-007. |
| `Url` | `string?` | When set, action renders as `<a>` link. |
| `FormController` / `FormAction` / `FormRouteValues` | `string?`/`string?`/`object?` | When set, action renders as a `<form>` POST. Mutually exclusive with `Url`. |
| `Icon` | `string?` | Optional icon override. Defaults are: `Primary` → none, `Secondary` → none, `Destructive` → `"ti ti-trash"`, `StateLocking` → `"ti ti-lock"`. |
| `ConfirmDialogId` | `string?` | Required for `Destructive` and `StateLocking` actions per FR-010. References a `_ConfirmDialog` partial rendered elsewhere on the page. |

**Validation:** A view-author convention enforces `(Url != null) XOR (FormController != null && FormAction != null)`. The `_ActionBar` partial throws on `Destructive`/`StateLocking` with no `ConfirmDialogId`.
**Lifecycle:** Constructed inline in the host view, passed to `_ActionBar` for rendering.

### `BreadcrumbItem` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record BreadcrumbItem(string Label, string? Url = null);
```

Used in the optional breadcrumbs slot of `_PageHeader`. The last item conventionally has `Url == null` (renders as plain text, "current page").

### `DocumentReference` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record DocumentReference(
    string Filename,
    long SizeBytes,
    string DownloadUrl,
    string? Signer = null,
    DateTimeOffset? GeneratedAt = null,
    DateTimeOffset? SignedAt = null,
    string? IconOverride = null);
```

Consumed by `_DocumentCard`. Sole permitted shape for a document reference per FR-008.

### `TimelineEvent` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record TimelineEvent(
    DateTimeOffset At,
    string Actor,
    string Action,
    string? Detail = null,
    string? Icon = null,
    string? Color = null);
```

Consumed by `_EventTimeline`. Constructed by host views from already-loaded audit data (no new repository queries are introduced by this feature; audit data is already exposed where needed).

### `SidebarEntry` (record)

Namespace: `FundingPlatform.Web.Models`

```csharp
public sealed record SidebarEntry(
    string Label,
    string Url,
    string Icon,
    string[] AllowedRoles,
    bool ShowToUnauthenticated = false);
```

Consumed by `_Layout.cshtml` to render the role-aware sidebar (FR-013). The full sidebar entry list is defined in [`contracts/README.md`](contracts/README.md). Each entry is filtered by checking the current user's roles against `AllowedRoles`.

## State and lifecycle

There are **no state machines, no transitions, no persistence-related rules** introduced by this feature. The presentation-layer types above are immutable records constructed per request and discarded at the end of the response.

## Existing entities (referenced read-only)

The following existing types are referenced by the new partials' host views and remain unchanged:

- `ApplicationListViewModel`, `ApplicationDetailsViewModel`, `CreateApplicationViewModel`, `ReviewQueueViewModel`, `ReviewApplicationViewModel`, `ApplicantResponseViewModel`, `FundingAgreementDetailsViewModel`, etc. — view models used by Razor views; their property names and shapes remain identical (FR-016).
- `IFileStorageService`, `ApplicationService`, `ReviewService`, `FundingAgreementService`, etc. — application services consumed by controllers; not touched.
- `Application`, `Item`, `Quotation`, `FundingAgreement`, `SignedUpload`, `Appeal`, `ApplicantResponse` — domain entities consumed via existing repositories; not touched.

## Optional unit-testability (not in scope, recorded for future)

`StatusVisualMap` is a pure function and trivially unit-testable. This spec does not require unit tests for it (E2E coverage of the rendered badge is sufficient via FR-019/FR-020). A future spec adding new enum values can introduce a `StatusVisualMapTests.cs` if desired; the current spec leaves that as YAGNI.
