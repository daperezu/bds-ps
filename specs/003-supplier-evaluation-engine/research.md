# Research: Supplier Evaluation Engine

**Date**: 2026-04-16

## No Outstanding Unknowns

All technical decisions were resolved during brainstorming. No NEEDS CLARIFICATION markers exist in the spec or plan. This document records the key decisions and rationale for reference.

## Decision Log

### D1: Score Computation Location

**Decision**: Domain value object (`SupplierScore`) computed on-the-fly.
**Rationale**: Constitution mandates rich domain model (Principle II). Score is a pure function of supplier attributes + quotation prices -- no database dependency needed. Keeps scoring unit-testable in isolation.
**Alternatives considered**:
- Persisted score table: rejected (YAGNI, reporting is a future spec)
- Service-layer calculation: rejected (violates rich domain model principle)

### D2: Compliance Field Migration

**Decision**: Replace `ComplianceStatus` (string, max 100 chars) with three `bit` columns: `IsCompliantCCSS`, `IsCompliantHacienda`, `IsCompliantSICOP`, all defaulting to `0` (false).
**Rationale**: Existing ComplianceStatus contains unstructured text that cannot be reliably parsed. Conservative default (all false) is safe -- no existing data claims compliance in a structured way.
**Migration approach**: Schema change in Database project (.sql file). Drop column, add three columns with defaults. EF Core configuration updated to match.

### D3: Scoring Formula

**Decision**: Five equally-weighted binary factors, each worth 1 point (max 5).
**Rationale**: User explicitly chose equal weights. The formula is intentionally simple: more checks + lower price = better supplier. No normalization or weighted scaling needed.
**Factors**:
1. IsCompliantCCSS (1 point if true)
2. IsCompliantHacienda (1 point if true)
3. IsCompliantSICOP (1 point if true)
4. HasElectronicInvoice (1 point if true)
5. Price competitiveness (1 point if quotation price equals the minimum price across all quotations for the item)

### D4: Recommendation and Pre-selection

**Decision**: Replace current `ComputeRecommendedSupplierId` (lowest-price-only) with score-based recommendation.
**Rationale**: The existing method returns `null` on price ties and only considers price. The new system scores across 5 factors and handles ties explicitly (all tied = all recommended, pre-select by lowest supplier ID).
**Breaking change**: The current "Recommended - lowest price" badge text changes to show score. The recommendation logic is fundamentally different (multi-factor vs. price-only).

### D5: Pre-selection Persistence

**Decision**: Pre-selection is display-only until the reviewer explicitly approves. If the reviewer has already selected a supplier (via Item.SelectedSupplierId), that selection takes precedence over the score-based pre-selection.
**Rationale**: Respects reviewer autonomy. Once a reviewer has made a choice, re-opening the review should not overwrite it with the system recommendation.
