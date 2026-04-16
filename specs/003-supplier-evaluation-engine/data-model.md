# Data Model: Supplier Evaluation Engine

**Date**: 2026-04-16

## Entity Changes

### Supplier (MODIFIED)

**Removed fields:**
- `ComplianceStatus` (string?, max 100)

**Added fields:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| IsCompliantCCSS | bool | Yes | false | Supplier is compliant with CCSS (Caja Costarricense de Seguro Social) |
| IsCompliantHacienda | bool | Yes | false | Supplier is compliant with Hacienda (tax authority) |
| IsCompliantSICOP | bool | Yes | false | Supplier is registered in SICOP (procurement system) |

**Unchanged fields used in scoring:**
- `HasElectronicInvoice` (bool, already exists)

**Constructor change:**
Replace `string? complianceStatus` parameter with `bool isCompliantCCSS, bool isCompliantHacienda, bool isCompliantSICOP`.

### SupplierScore (NEW - Value Object)

Not persisted. Computed on-the-fly from Supplier + Quotation data.

| Property | Type | Description |
|----------|------|-------------|
| Total | int | Composite score (0-5) |
| IsCompliantCCSS | bool | CCSS compliance factor (1 point) |
| IsCompliantHacienda | bool | Hacienda compliance factor (1 point) |
| IsCompliantSICOP | bool | SICOP compliance factor (1 point) |
| HasElectronicInvoice | bool | E-invoice factor (1 point) |
| HasLowestPrice | bool | Price competitiveness factor (1 point) |
| IsRecommended | bool | True if this score equals the max score for the item |
| IsPreSelected | bool | True if this is the pre-selected supplier (highest score, lowest ID on tie) |

**Static factory method:**
```
ComputeForItem(quotations: List<(Quotation, Supplier)>) -> List<(int QuotationId, SupplierScore Score)>
```

Takes all quotations for a single item, computes scores for each, determines recommendations and pre-selection, returns results sorted by score descending.

## Database Schema Change

**File**: `src/FundingPlatform.Database/dbo/Tables/Suppliers.sql`

```sql
-- Remove:
--   [ComplianceStatus] NVARCHAR(100) NULL

-- Add:
    [IsCompliantCCSS]      BIT NOT NULL DEFAULT 0,
    [IsCompliantHacienda]  BIT NOT NULL DEFAULT 0,
    [IsCompliantSICOP]     BIT NOT NULL DEFAULT 0,
```

## DTO Changes

### SupplierDto (MODIFIED)

Remove: `ComplianceStatus` (string?)
Add: `IsCompliantCCSS` (bool), `IsCompliantHacienda` (bool), `IsCompliantSICOP` (bool)

### ReviewQuotationDto (MODIFIED)

Add:

| Field | Type | Description |
|-------|------|-------------|
| Score | int | Composite score (0-5) |
| ScoreCCSS | bool | CCSS compliance contributed to score |
| ScoreHacienda | bool | Hacienda compliance contributed to score |
| ScoreSICOP | bool | SICOP compliance contributed to score |
| ScoreElectronicInvoice | bool | E-invoice contributed to score |
| ScoreLowestPrice | bool | Lowest price contributed to score |
| IsPreSelected | bool | This supplier is pre-selected for approval |

Remove: `IsRecommended` (replaced by score-based `IsRecommended` computed from scores)

### ReviewItemDto (MODIFIED)

Remove: `RecommendedSupplierId` (int?) -- no longer needed, recommendation is per-quotation via score

### AddSupplierQuotationCommand (MODIFIED)

Remove: `ComplianceStatus` (string?)
Add: `IsCompliantCCSS` (bool), `IsCompliantHacienda` (bool), `IsCompliantSICOP` (bool)

## ViewModel Changes

### AddSupplierViewModel (MODIFIED)

Remove: `ComplianceStatus` (string?, max 200)
Add: `IsCompliantCCSS` (bool), `IsCompliantHacienda` (bool), `IsCompliantSICOP` (bool)
Display names: "CCSS Compliance", "Hacienda Compliance", "SICOP Registration"

### ReviewQuotationViewModel (MODIFIED)

Add: `Score` (int), `ScoreCCSS` (bool), `ScoreHacienda` (bool), `ScoreSICOP` (bool), `ScoreElectronicInvoice` (bool), `ScoreLowestPrice` (bool), `IsPreSelected` (bool)

### ReviewItemViewModel (MODIFIED)

Remove: `RecommendedSupplierId` (int?) -- recommendation now lives on each quotation via IsRecommended

## Relationships

No new relationships. Existing relationships unchanged:

```
Supplier 1──N Quotation N──1 Item N──1 Application
```

SupplierScore is a transient value object -- no database relationship.
