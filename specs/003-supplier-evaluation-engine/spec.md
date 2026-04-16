# Feature Specification: Supplier Evaluation Engine

**Feature Branch**: `003-supplier-evaluation-engine`
**Created**: 2026-04-16
**Status**: Draft
**Input**: Per-item supplier scoring engine that ranks quotations by price competitiveness, regulatory compliance (CCSS, Hacienda, SICOP), and electronic invoicing capability

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reviewer Sees Scored and Ranked Supplier Quotations (Priority: P1)

A reviewer opens an application under review and sees each item's supplier quotations ranked by a composite score. The score combines price competitiveness, compliance status (CCSS, Hacienda, SICOP), and electronic invoicing. The highest-scoring supplier is flagged as "Recommended" and pre-selected for approval.

**Why this priority**: Without scoring, reviewers rely on manual comparison of raw data across multiple suppliers. This is the core value of the evaluation engine.

**Independent Test**: Can be tested by creating an application with multiple items, each having multiple supplier quotations with varying compliance and pricing, submitting it, then logging in as a reviewer and verifying scores, ranking, recommendation badge, and pre-selection.

**Acceptance Scenarios**:

1. **Given** an item with three quotations from suppliers with different compliance levels and prices, **When** a reviewer opens the application, **Then** each quotation shows a score out of 5 with visible breakdown, ordered highest-first, and the top scorer is marked "Recommended" and pre-selected.
2. **Given** an item with two quotations that tie for the highest score, **When** a reviewer opens the application, **Then** both are marked "Recommended" and the one with the lower supplier ID is pre-selected.
3. **Given** an item with a single quotation, **When** a reviewer opens the application, **Then** that quotation scores the price point automatically, is marked "Recommended", and is pre-selected.

---

### User Story 2 - Reviewer Overrides Recommended Supplier (Priority: P1)

A reviewer disagrees with the system recommendation and selects a different supplier for approval. The system accepts the override without requiring justification.

**Why this priority**: Reviewer autonomy is essential -- the scoring engine assists but does not dictate decisions.

**Independent Test**: Can be tested by opening an item with a pre-selected recommended supplier, selecting a different supplier, approving the item, and verifying the non-recommended supplier is saved as the selection.

**Acceptance Scenarios**:

1. **Given** an item with a pre-selected recommended supplier, **When** the reviewer selects a different supplier and approves the item, **Then** the non-recommended supplier is saved as the selected supplier.
2. **Given** an item where the reviewer has overridden the selection, **When** the reviewer views the item again before finalizing, **Then** the override persists (the manually selected supplier remains selected, not the recommended one).

---

### User Story 3 - Applicant Creates Supplier with Compliance Details (Priority: P1)

When an applicant adds a supplier to their application, they provide three compliance checkboxes (CCSS, Hacienda, SICOP) and the electronic invoicing flag instead of a free-text compliance field.

**Why this priority**: The scoring engine depends on structured compliance data. Without this change, there is nothing to score.

**Independent Test**: Can be tested by creating a supplier with various compliance combinations and verifying the booleans are persisted and displayed correctly.

**Acceptance Scenarios**:

1. **Given** an applicant adding a new supplier, **When** they check CCSS and Hacienda but not SICOP, **Then** the supplier is saved with IsCompliantCCSS=true, IsCompliantHacienda=true, IsCompliantSICOP=false.
2. **Given** an existing supplier created before this feature, **When** viewed in any context, **Then** all three compliance fields default to false (no data loss, conservative default).

---

### Edge Cases

- **Single quotation on an item**: that supplier gets the price point automatically (they are the lowest) and is recommended regardless of other factors.
- **All suppliers have identical attributes and price**: all score 5/5, all recommended, first by supplier ID pre-selected.
- **Quotation with expired ValidUntil**: still scored and displayed (expiration is informational, not a disqualifier in the current workflow).
- **Supplier data changes mid-review**: since scores are computed on-the-fly, re-opening the review reflects updated data. No stale scores.
- **All suppliers have zero compliance and different prices**: only the lowest-price supplier scores 1/5, others score 0/5. The system still recommends and pre-selects the 1/5 supplier.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST replace the existing Supplier.ComplianceStatus (string) field with three boolean fields: IsCompliantCCSS, IsCompliantHacienda, IsCompliantSICOP.
- **FR-002**: System MUST update the supplier creation and edit flows to capture three compliance checkboxes (CCSS, Hacienda, SICOP) instead of the free-text compliance field.
- **FR-003**: Existing suppliers with ComplianceStatus data MUST have all three boolean fields default to false (conservative migration, no structured data to preserve).
- **FR-004**: System MUST compute a composite score (0-5) for each quotation on an item using five equally-weighted factors: CCSS compliance (1 point), Hacienda compliance (1 point), SICOP compliance (1 point), electronic invoicing (1 point), and price competitiveness (1 point for the lowest price among the item's quotations).
- **FR-005**: When multiple quotations share the lowest price on an item, all tied quotations MUST receive the price competitiveness point.
- **FR-006**: Quotations on the review screen MUST be ordered by score descending (highest first).
- **FR-007**: The highest-scoring supplier(s) MUST be flagged as "Recommended" on the review screen.
- **FR-008**: When multiple suppliers tie for highest score, all tied suppliers MUST be flagged as "Recommended".
- **FR-009**: The highest-scoring supplier MUST be pre-selected for approval when a reviewer opens an item. Ties MUST be broken by lowest supplier ID (deterministic).
- **FR-010**: Reviewers MUST be able to change the pre-selected supplier to any other supplier without providing justification.
- **FR-011**: Scores MUST be computed on-the-fly in the domain layer (not persisted to the database).
- **FR-012**: Each quotation row on the review screen MUST display the score (e.g., "4/5") and a breakdown showing which factors contributed.
- **FR-013**: Scores MUST be visible only to reviewers, never to applicants.

### Key Entities

- **Supplier** (modified): Existing entity gains three boolean compliance fields (IsCompliantCCSS, IsCompliantHacienda, IsCompliantSICOP) replacing the ComplianceStatus string. HasElectronicInvoice already exists.
- **SupplierScore** (new, domain value object): Encapsulates the scoring calculation for a single quotation. Takes supplier compliance booleans, electronic invoice flag, quotation price, and the lowest price for the item. Produces a total score (0-5) and per-factor breakdown.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Reviewers see a composite score (0-5) with factor breakdown for every supplier quotation on the review screen.
- **SC-002**: The highest-scoring supplier is automatically flagged as "Recommended" and pre-selected for approval on every item.
- **SC-003**: Reviewers can override the pre-selected supplier in a single action with no additional prompts or required fields.
- **SC-004**: Supplier creation and edit forms display three compliance checkboxes (CCSS, Hacienda, SICOP) instead of a free-text field.
- **SC-005**: All scoring logic is encapsulated in a domain value object that can be unit-tested without database dependencies.

## Assumptions

- Existing suppliers in the database have unstructured ComplianceStatus values that cannot be reliably parsed into the three boolean fields; defaulting to false is acceptable.
- The current review screen already displays quotation rows per item with a recommendation badge; this feature enhances the existing UI rather than creating new screens.
- The existing supplier creation/edit flow already collects HasElectronicInvoice as a boolean; no changes needed for that field.
- Score computation for the typical number of quotations per item (2-5) adds negligible overhead to review screen load time.

## Dependencies

- **Spec 001** (Core Data Model): Supplier entity, Quotation entity, Item entity.
- **Spec 002** (Review Workflow): ReviewService, review screen, recommendation display, supplier pre-selection mechanism.

## Out of Scope

- Supplier reputation or history scoring across multiple applications.
- Applicant-facing score visibility.
- Admin-configurable scoring weights (all factors are equal weight).
- Score persistence or reporting.
- Warranty or shipping details as scoring factors.

## Open Questions

None -- all design decisions resolved during brainstorming.
