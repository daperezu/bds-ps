# Data Model: Signing Stage Wayfinding

**Feature:** 007-signing-wayfinding
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-23

## Scope

This feature introduces **no new domain entities, no new database tables, no new enums, no new repository interfaces**. It is a presentation-layer wayfinding fix on top of the existing 006 signing aggregate.

The only data-shape change is a single additive field that flows DTO → view-model → view to drive banner visibility on `/ApplicantResponse/Index/{id}`.

## Additive field

### `ApplicantResponseDto.HasFundingAgreement : bool`

- **Location:** `src/FundingPlatform.Application/DTOs/ApplicantResponseDto.cs`
- **Change:** add as a new constructor parameter (public record positional parameter). Append after `Items` to avoid reordering existing callers.
- **Semantics:** `true` iff the application has at least one row in its `FundingAgreements` collection. Derived at query time from the already-loaded application aggregate via `.Any()`; no extra database round-trip.
- **Consumer:** `ApplicantResponseController.BuildViewModel` pass-through into a new `bool HasFundingAgreement` property on `ApplicantResponseViewModel`, which the Razor view reads to decide whether to render the "ready to sign below" banner (when combined with `State == ResponseFinalized`).

### `ApplicantResponseViewModel.HasFundingAgreement : bool`

- **Location:** `src/FundingPlatform.Web/ViewModels/ApplicantResponseViewModel.cs`
- **Change:** add as a new public property with a default of `false` (safe default — banner hidden when absent).
- **Consumer:** `Views/ApplicantResponse/Index.cshtml` Razor conditional.

## No entity changes

| Asked | Answer |
|---|---|
| Does this feature add a domain entity? | No. |
| Does this feature modify a domain entity? | No. |
| Does this feature add a value object? | No. |
| Does this feature add an enum member? | No. |
| Does this feature add a table or column? | No. |
| Does this feature change a row's lifecycle? | No. |
| Does this feature add a repository interface or method? | No. The `.Any()` on the already-loaded agreement collection reuses what `ApplicantResponseService` already loads. |

## No audit-trail additions

The feature does not persist any new event. It is strictly a rendering/navigation change. Existing 006 audit entries (`AgreementDownloaded`, `SignedAgreementUploaded`, etc.) are unaffected.

## No state-machine changes

The application state machine from specs 001/002/004/006 is unchanged:
`Draft → Submitted → UnderReview → Resolved → ResponseFinalized → AgreementExecuted`

Banner visibility reads two of those states (`ResponseFinalized`, `AgreementExecuted`) but does not add, remove, or reinterpret any transition.

## No concurrency model changes

No `RowVersion` additions, no new optimistic-concurrency paths. The banner is rendered on each GET; its correctness depends only on the `ApplicantResponseService.GetResponseAsync` snapshot returning a consistent state + agreement-existence pair, which is already guaranteed by the single EF query.

## Summary

| Layer | New types | Modified types | New fields |
|---|---|---|---|
| Domain | — | — | — |
| Application (DTO) | — | `ApplicantResponseDto` | `HasFundingAgreement : bool` |
| Application (Service) | — | `ApplicantResponseService.GetResponseAsync` (populate the new field) | — |
| Web (ViewModel) | — | `ApplicantResponseViewModel` | `HasFundingAgreement : bool` |
| Web (Controller) | — | `ApplicantResponseController.BuildViewModel` (pass-through) | — |

Everything else (tab nav, panel embed, banner rendering) lives purely in Razor views and a shared partial.
