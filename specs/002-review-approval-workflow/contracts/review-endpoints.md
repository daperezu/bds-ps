# Review Endpoints Contract

**Date**: 2026-04-15
**Feature**: specs/002-review-approval-workflow/

## Controller: ReviewController

**Route prefix**: `/Review`
**Authorization**: `[Authorize(Roles = "Reviewer")]`

### Review Queue

| Method | Route | Action | Returns |
|--------|-------|--------|---------|
| GET | `/Review` | `Index(int page = 1)` | Review queue page (paginated list of submitted applications) |

**Query parameters:**
- `page` (int, default 1): Page number for pagination

**View model** (`ReviewQueueViewModel`):
- `Applications`: List of `ReviewQueueItemViewModel`
- `CurrentPage`: int
- `TotalPages`: int
- `TotalCount`: int

**ReviewQueueItemViewModel**:
- `ApplicationId`: int
- `ApplicantName`: string (FirstName + LastName)
- `ApplicantPerformanceScore`: decimal?
- `SubmittedAt`: DateTime
- `ItemCount`: int

### Application Review

| Method | Route | Action | Returns |
|--------|-------|--------|---------|
| GET | `/Review/{id}` | `Review(int id)` | Full application review page |
| POST | `/Review/{id}/ReviewItem` | `ReviewItem(int id, ReviewItemViewModel model)` | Redirect back to review page |
| POST | `/Review/{id}/FlagEquivalence` | `FlagEquivalence(int id, FlagEquivalenceViewModel model)` | Redirect back to review page |
| POST | `/Review/{id}/SendBack` | `SendBack(int id)` | Redirect to queue |
| POST | `/Review/{id}/Finalize` | `Finalize(int id, bool force = false)` | Redirect to queue (or back to review page with warning) |

**ReviewApplicationViewModel**:
- `ApplicationId`: int
- `ApplicantName`: string
- `ApplicantPerformanceScore`: decimal?
- `State`: string
- `SubmittedAt`: DateTime?
- `Items`: List of `ReviewItemViewModel`
- `HasUnresolvedItems`: bool

**ReviewItemViewModel**:
- `ItemId`: int
- `ProductName`: string
- `CategoryName`: string
- `TechnicalSpecifications`: string
- `ReviewStatus`: string (Pending/Approved/Rejected/NeedsInfo)
- `ReviewComment`: string?
- `SelectedSupplierId`: int?
- `IsNotTechnicallyEquivalent`: bool
- `Quotations`: List of `ReviewQuotationViewModel`
- `RecommendedSupplierId`: int? (lowest price, null if tie)
- `ImpactTemplateName`: string?
- `ImpactParameters`: List of `ImpactParameterDisplayViewModel`

**ReviewQuotationViewModel**:
- `QuotationId`: int
- `SupplierId`: int
- `SupplierName`: string
- `SupplierLegalId`: string
- `Price`: decimal
- `ValidUntil`: DateOnly
- `DocumentFileName`: string
- `IsRecommended`: bool (true if lowest price and no tie)

**Review Item POST model**:
- `ItemId`: int
- `Decision`: string ("Approve" | "Reject" | "RequestMoreInfo")
- `Comment`: string? (plain text, max 2000 chars)
- `SelectedSupplierId`: int? (required when Decision = "Approve")

**Flag Equivalence POST model**:
- `ItemId`: int
- `IsNotEquivalent`: bool (true to flag, false to clear)
