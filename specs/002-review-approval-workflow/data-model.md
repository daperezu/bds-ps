# Data Model: Review & Approval Workflow

**Date**: 2026-04-15
**Feature**: specs/002-review-approval-workflow/

## Entity Changes

This feature modifies existing entities only — no new entities are introduced.

### ApplicationState Enum (MODIFY)

```
ApplicationState
├── Draft = 0           (existing)
├── Submitted = 1       (existing)
├── UnderReview = 2     (NEW)
└── Resolved = 3        (NEW)
```

**State transitions**:
```
Draft ──Submit()──> Submitted ──StartReview()──> UnderReview ──Finalize()──> Resolved
                        ▲                            │
                        └────────SendBack()──────────┘
```

### ItemReviewStatus Enum (NEW)

```
ItemReviewStatus
├── Pending = 0
├── Approved = 1
├── Rejected = 2
└── NeedsInfo = 3
```

### Application Entity (MODIFY)

**New methods** (added to existing entity):

| Method | Transition | Validation |
|--------|-----------|------------|
| `StartReview()` | Submitted → UnderReview | Must be in Submitted state. Idempotent if already UnderReview. |
| `SendBack()` | UnderReview → Draft | Must be in UnderReview state. Resets all item ReviewStatus to Pending. Clears SubmittedAt. |
| `Finalize(bool force)` | UnderReview → Resolved | Must be in UnderReview state. If `force=false`, throws if unresolved items exist. If `force=true`, implicitly rejects unresolved items with system reason. |

**No new fields** on Application — state and RowVersion already exist.

### Item Entity (MODIFY)

**New fields:**

| Field | Type | Nullable | Default | Notes |
|-------|------|----------|---------|-------|
| `ReviewStatus` | ItemReviewStatus | No | Pending (0) | Review decision for this item |
| `ReviewComment` | string | Yes | null | Plain text reviewer comment, max 2000 chars |
| `SelectedSupplierId` | int | Yes | null | FK to Suppliers — which supplier was selected |
| `IsNotTechnicallyEquivalent` | bool | No | false | Whether quotations are flagged as not equivalent |

**New navigation property:**
- `SelectedSupplier` (Supplier?) — nullable reference to the selected supplier

**New methods:**

| Method | Effect | Validation |
|--------|--------|------------|
| `Approve(int supplierId, string? comment)` | Sets ReviewStatus=Approved, SelectedSupplierId, ReviewComment | Supplier must exist in item's quotations. Must not be flagged as not equivalent. |
| `Reject(string? comment)` | Sets ReviewStatus=Rejected, ReviewComment | None |
| `RequestMoreInfo(string? comment)` | Sets ReviewStatus=NeedsInfo, ReviewComment | None |
| `FlagNotEquivalent()` | Sets IsNotTechnicallyEquivalent=true, ReviewStatus=Rejected, ReviewComment="Rejected: quotations are not technically equivalent" | None |
| `ClearNotEquivalentFlag()` | Sets IsNotTechnicallyEquivalent=false, ReviewStatus=Pending, ReviewComment=null | None |
| `ResetReviewStatus()` | Sets ReviewStatus=Pending. Does NOT clear ReviewComment (preserves for next round). Clears SelectedSupplierId and IsNotTechnicallyEquivalent. | Called by Application.SendBack() |

### Database Schema Changes

**Items table (MODIFY)**:

```sql
ALTER TABLE [dbo].[Items]
ADD
    [ReviewStatus]                INT NOT NULL DEFAULT 0,
    [ReviewComment]               NVARCHAR(2000) NULL,
    [SelectedSupplierId]          INT NULL,
    [IsNotTechnicallyEquivalent]  BIT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_Items_Suppliers_SelectedSupplierId]
        FOREIGN KEY ([SelectedSupplierId]) REFERENCES [dbo].[Suppliers]([Id]);
```

Note: Since the Database project uses dacpac (declarative schema), these columns are added directly to the existing `Items.sql` table definition.

**SeedData.sql (MODIFY)**:

Add Reviewer role:
```sql
INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES (NEWID(), N'Reviewer', N'REVIEWER', NEWID());
```

### Entity Relationships

```
Application (1) ──── (*) Item
    │                      │
    │                      ├── ReviewStatus (enum field)
    │                      ├── ReviewComment (string field)
    │                      ├── IsNotTechnicallyEquivalent (bool field)
    │                      └── SelectedSupplierId (FK, nullable)
    │                              │
    │                              └──── (0..1) Supplier
    │
    ├── State: Draft | Submitted | UnderReview | Resolved
    └── RowVersion (optimistic concurrency)
```

### Validation Rules

1. **Approve item**: `IsNotTechnicallyEquivalent` must be `false`. `SelectedSupplierId` must reference a supplier that has a quotation on this item.
2. **Finalize review**: Application must be in `UnderReview` state. If any item has `ReviewStatus` of `Pending` or `NeedsInfo` and `force=false`, throw validation error listing unresolved items.
3. **Send back**: Application must be in `UnderReview` state.
4. **Start review**: Application must be in `Submitted` state (idempotent if already `UnderReview`).
5. **Flag not equivalent**: Automatically sets `ReviewStatus=Rejected`. Prevents subsequent `Approve()` calls.
