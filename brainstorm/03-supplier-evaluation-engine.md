# Brainstorm: Supplier Evaluation Engine

**Date:** 2026-04-16
**Status:** spec-created
**Spec:** specs/003-supplier-evaluation-engine/

## Problem Framing

With the core data model (spec 001) and review/approval workflow (spec 002) complete, reviewers can evaluate applications and select suppliers -- but they have no structured way to compare suppliers beyond eyeballing raw data. The only automated help is a lowest-price recommendation that returns nothing on price ties. The supplier evaluation engine adds a composite scoring system that ranks suppliers per item, making the review process faster and more consistent.

This is the third feature in the decomposition of the full Funding Request & Evaluation Platform SRS (item C in the decomposition: "the most complex business logic").

## Approaches Considered

### A: Domain-Computed Score (Selected)
- Score calculation lives in a domain value object (SupplierScore), computed on-the-fly from current supplier data
- No persistence of scores -- computed fresh each time the review screen loads
- Pros: Follows rich domain model, scores always reflect current data, no new tables, unit-testable in isolation
- Cons: Not queryable for reports (deferred to future reporting spec)

### B: Persisted Score Table
- Separate SupplierItemScore entity stored in database when application enters review
- Pros: Scores auditable, queryable, powers future reports
- Cons: Needs recalculation on supplier data changes, more schema, premature for current needs

### C: Service-Layer Calculation
- Score logic in ReviewService, no domain value object
- Pros: Quick to implement
- Cons: Breaks rich domain model principle (constitution violation), scatters business logic

## Decision

Selected **Approach A: Domain-Computed Score**. It keeps scoring logic where business rules belong (domain layer per constitution), avoids premature persistence (YAGNI), and always reflects the latest supplier data.

Key design decisions:
- **Scoring factors**: CCSS compliance, Hacienda compliance, SICOP compliance, electronic invoicing, price competitiveness -- all equal weight, 1 point each, max 5
- **Compliance model change**: Replace `ComplianceStatus` (free text) with three booleans: IsCompliantCCSS, IsCompliantHacienda, IsCompliantSICOP
- **Recommendation**: Highest scorer(s) flagged as "Recommended", all ties get the badge
- **Pre-selection**: Highest scorer pre-selected, ties broken by lowest supplier ID
- **Override**: Reviewer can change selection without justification
- **Visibility**: Scores internal to reviewers only, never shown to applicants
- **Price ties**: All suppliers sharing the lowest price get the price point

## Open Threads

None -- all design decisions resolved during brainstorming.
