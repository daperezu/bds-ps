# Feature Specification: Review & Approval Workflow

**Feature Branch**: `002-review-approval-workflow`
**Created**: 2026-04-15
**Status**: Draft
**Input**: Review and approval workflow for submitted funding applications — reviewers evaluate items, select suppliers, and finalize decisions

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reviewer Views Submitted Applications Queue (Priority: P1)

A reviewer logs in and sees a paginated list of all submitted applications awaiting review. The queue shows key details (applicant name, submission date, number of items, applicant performance score) so the reviewer can pick which application to work on.

**Why this priority**: Without a queue, reviewers have no way to discover and access submitted applications. This is the entry point for the entire review workflow.

**Independent Test**: Can be tested by submitting applications as an applicant, then logging in as a reviewer and verifying they appear in the queue with correct details.

**Acceptance Scenarios**:

1. **Given** there are three submitted applications, **When** a reviewer navigates to the review queue, **Then** all three are listed with applicant name, submission date, item count, and performance score.
2. **Given** an application in Draft state, **When** a reviewer views the queue, **Then** the draft application does not appear.
3. **Given** an application that has been resolved, **When** a reviewer views the queue, **Then** it does not appear in the active queue.
4. **Given** more applications than fit on one page, **When** the reviewer navigates through pages, **Then** applications are displayed in paginated form with navigation controls.

---

### User Story 2 - Reviewer Opens and Reviews an Application (Priority: P1)

A reviewer selects an application from the queue and sees the full details: applicant info (with performance score), all items with their categories, technical specs, impact definitions, and supplier quotations. The application transitions to Under Review when opened for the first time.

**Why this priority**: The reviewer needs full visibility into the application data to make informed decisions. This is the core review screen.

**Independent Test**: Can be tested by opening a submitted application as a reviewer and verifying all application data is displayed and the state transitions to Under Review.

**Acceptance Scenarios**:

1. **Given** a submitted application, **When** a reviewer opens it, **Then** the application state changes to Under Review and all items, suppliers, quotations, and impact definitions are displayed.
2. **Given** an application already Under Review, **When** the same or another reviewer opens it, **Then** the state remains Under Review and the data is displayed.
3. **Given** an application, **When** a reviewer views the applicant section, **Then** the performance score is displayed as read-only.

---

### User Story 3 - Reviewer Makes Item-Level Decisions (Priority: P1)

For each item in the application, the reviewer can approve, reject, or request more info. Each decision can include an optional plain text comment. The system highlights the lowest-price supplier as a recommendation.

**Why this priority**: Item-level evaluation is the core business action of the review workflow. Without it, the reviewer cannot express their decisions.

**Independent Test**: Can be tested by opening an application and setting approve/reject/request-info on individual items, verifying decisions persist and comments are saved.

**Acceptance Scenarios**:

1. **Given** an item under review, **When** the reviewer approves it, selects a supplier, and adds a comment, **Then** the item status is Approved, the selected supplier is recorded, and the comment is saved.
2. **Given** an item under review, **When** the reviewer rejects it with a comment, **Then** the item status is Rejected and the comment is saved.
3. **Given** an item under review, **When** the reviewer marks it as "Request More Info" with no comment, **Then** the item status is Needs Info and no comment is stored.
4. **Given** an item with three supplier quotations, **When** the reviewer views the item, **Then** the lowest-price supplier is visually highlighted as the system recommendation.
5. **Given** an item with two suppliers at equal prices, **When** the reviewer views the item, **Then** no supplier is highlighted (tie — reviewer must decide).

---

### User Story 4 - Reviewer Flags Technical Equivalence (Priority: P1)

For each item, the reviewer assesses whether the supplier quotations are technically equivalent. If the reviewer flags them as not equivalent, the item is automatically rejected by the system.

**Why this priority**: Technical equivalence is a core business rule from the SRS — non-equivalent quotations invalidate the comparison. Auto-rejection enforces this consistently.

**Independent Test**: Can be tested by flagging an item's quotations as not equivalent and verifying the item is auto-rejected.

**Acceptance Scenarios**:

1. **Given** an item under review, **When** the reviewer flags quotations as not technically equivalent, **Then** the item is automatically set to Rejected with a system-generated reason indicating non-equivalence.
2. **Given** an item already flagged as not equivalent (auto-rejected), **When** the reviewer tries to approve it, **Then** the system prevents approval and displays a message explaining the item was rejected due to non-equivalent quotations.
3. **Given** an item under review, **When** the reviewer confirms quotations are technically equivalent, **Then** the reviewer can proceed to approve, reject, or request more info normally.
4. **Given** an item previously flagged as not equivalent, **When** the reviewer clears the flag, **Then** the auto-rejection is removed and the item returns to Pending status.

---

### User Story 5 - Reviewer Selects Supplier for Approved Items (Priority: P1)

When approving an item, the reviewer must select which supplier will fulfill it. The system highlights the lowest-price supplier but the reviewer makes the final decision.

**Why this priority**: Supplier selection is required for every approved item — it determines who gets the contract. Cannot finalize a review without it.

**Independent Test**: Can be tested by approving an item, selecting a supplier, and verifying the selection is persisted.

**Acceptance Scenarios**:

1. **Given** an item being approved, **When** the reviewer does not select a supplier, **Then** the system prevents the approval and prompts for supplier selection.
2. **Given** an item being approved, **When** the reviewer selects a supplier that is not the lowest-price recommendation, **Then** the selection is accepted without restriction.
3. **Given** an item with two suppliers where prices are equal, **When** the reviewer views the recommendation, **Then** no supplier is highlighted (tie — reviewer must decide).

---

### User Story 6 - Reviewer Sends Application Back for More Info (Priority: P1)

When at least one item is marked as "Request More Info," the reviewer can send the application back to the applicant. The application returns to Draft state, and the applicant can edit anything.

**Why this priority**: The feedback loop between reviewer and applicant is essential. Without it, applications with issues are stuck.

**Independent Test**: Can be tested by marking an item as needs-info, sending back, then verifying the applicant can edit and resubmit.

**Acceptance Scenarios**:

1. **Given** an application with one item marked Needs Info, **When** the reviewer sends it back, **Then** the application state transitions to Draft and all item statuses are reset to Pending.
2. **Given** an application sent back to Draft, **When** the applicant logs in, **Then** they can see the reviewer's comments on each item and edit any part of the application.
3. **Given** an application sent back and resubmitted, **When** a reviewer opens it again, **Then** the previous review comments are still visible for context.

---

### User Story 7 - Reviewer Finalizes the Review (Priority: P1)

After making decisions on all items, the reviewer finalizes the review. The system checks that all items are resolved (Approved or Rejected). If unresolved items remain, the system warns the reviewer and asks for confirmation. Upon finalization, the application moves to Resolved.

**Why this priority**: Finalization is the culmination of the review process — it produces the official outcome.

**Independent Test**: Can be tested by resolving all items and finalizing, verifying the application reaches Resolved state.

**Acceptance Scenarios**:

1. **Given** all items are Approved or Rejected, **When** the reviewer finalizes, **Then** the application transitions to Resolved.
2. **Given** one item is still Pending, **When** the reviewer attempts to finalize, **Then** the system displays a warning listing unresolved items and asks for confirmation.
3. **Given** the reviewer confirms finalization despite unresolved items, **Then** unresolved items are implicitly rejected with a system-generated reason and the application transitions to Resolved.
4. **Given** the reviewer cancels finalization after the warning, **Then** the application remains Under Review and no item statuses change.

---

### Edge Cases

- Reviewer opens an application that another reviewer is already reviewing — both can work on it, optimistic concurrency handles conflicts on save
- Application is sent back to Draft, but the applicant never resubmits — stays in Draft indefinitely (no auto-escalation in this spec)
- Reviewer tries to finalize an application where all items were sent back as "Needs Info" — system warns, confirmation treats them as rejected
- Applicant resubmits an application — previous review comments persist, all item statuses reset to Pending for fresh review
- Reviewer flags technical equivalence as not-equivalent, then wants to undo — they can clear the flag, which removes the auto-rejection and returns the item to Pending
- Admin changes system configuration during an active review — does not affect in-progress reviews (config is read at review start or per-action, not cached)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a paginated queue of all submitted applications to authenticated reviewers, showing applicant name, submission date, item count, and applicant performance score
- **FR-002**: System MUST transition an application from Submitted to Under Review when a reviewer first opens it
- **FR-003**: System MUST display the full application details to the reviewer: applicant info (including read-only performance score), all items, categories, technical specs, impact definitions, suppliers, and quotations
- **FR-004**: System MUST allow the reviewer to set each item's status to Approved, Rejected, or Needs Info
- **FR-005**: System MUST allow the reviewer to add an optional plain text comment when making any item-level decision
- **FR-006**: System MUST highlight the lowest-price supplier as a recommendation for each item; when prices are tied, no supplier is highlighted
- **FR-007**: System MUST require the reviewer to select a supplier when approving an item; approval without supplier selection is prevented
- **FR-008**: System MUST allow the reviewer to flag an item's quotations as not technically equivalent, which automatically rejects the item with a system-generated reason
- **FR-009**: System MUST prevent approval of an item that has been flagged as not technically equivalent
- **FR-010**: System MUST allow the reviewer to clear the not-equivalent flag, returning the item to Pending status
- **FR-011**: System MUST allow the reviewer to send an application back to the applicant, transitioning it to Draft and resetting all item statuses to Pending
- **FR-012**: System MUST preserve reviewer comments from previous review rounds when an application is resubmitted
- **FR-013**: System MUST allow the reviewer to finalize the review, transitioning the application to Resolved
- **FR-014**: System MUST warn the reviewer when attempting to finalize with unresolved items (Pending or Needs Info), listing the unresolved items and requiring confirmation
- **FR-015**: When the reviewer confirms finalization with unresolved items, the system MUST implicitly reject those items with a system-generated reason
- **FR-016**: System MUST enforce role-based access — only users with the Reviewer role can access the review queue and perform review actions
- **FR-017**: System MUST use optimistic concurrency to handle concurrent reviewer edits on the same application
- **FR-018**: System MUST record all review actions in version history (item decisions, comments, finalization, send-back)
- **FR-019**: Every functional requirement MUST have corresponding Playwright end-to-end tests that validate the user flow through the browser

### Key Entities

- **Application**: Gains two new states (Under Review, Resolved) in addition to existing Draft and Submitted. No new entity — state and transitions are added to the existing Application entity
- **Item**: Gains review fields — review status (Pending, Approved, Rejected, Needs Info), selected supplier reference, reviewer comment (plain text), technical equivalence flag. No new entity — fields are added to the existing Item entity

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reviewer can complete the full review cycle (open application, evaluate all items, select suppliers, finalize) for a typical 3-item application in under 15 minutes
- **SC-002**: 100% of review business rules are enforced — no item can be approved without supplier selection, no non-equivalent item can be approved, finalization warns on unresolved items
- **SC-003**: Applications correctly transition through all states: Submitted -> Under Review -> Resolved (or back to Draft via send-back)
- **SC-004**: Reviewer comments persist across review rounds — when an applicant resubmits, previous comments are visible
- **SC-005**: The lowest-price supplier recommendation is correctly computed and displayed for every item
- **SC-006**: Every user story has passing Playwright e2e tests covering the golden path and key error scenarios
- **SC-007**: Concurrent reviewer access is handled gracefully via optimistic concurrency — no silent data loss

## Error Handling

- If a reviewer tries to approve an item flagged as not technically equivalent, the system displays an inline error and prevents the action
- If a reviewer tries to finalize with unresolved items, a confirmation dialog warns them with the list of unresolved items
- If two reviewers edit the same application concurrently, the second save triggers a concurrency conflict message prompting them to reload
- If the reviewer attempts to open a non-existent or non-submitted application via URL, the system returns a 404 page
- If a reviewer attempts to access the review queue without the Reviewer role, the system returns a 403 forbidden response

## Dependencies

- `001-core-model-submission` — all core entities (Application, Item, Supplier, Quotation, Applicant), file storage, authentication, version history
- ASP.NET Identity roles for Reviewer authorization

## Out of Scope

- Supplier evaluation scoring and weighted criteria (deferred to Supplier Evaluation Engine spec)
- Applicant response to resolution — accept, reject, appeal (deferred to Applicant Response spec)
- Notification emails/alerts to applicants (deferred to Notifications spec)
- PDF generation of resolution documents (deferred to Document Generation spec)
- Auto-assignment or workload balancing of applications to reviewers
- Application search, filtering, or sorting in the review queue (beyond pagination)
- Reviewer dashboard or analytics

## Assumptions

- The Reviewer role already exists via ASP.NET Identity roles from `001` (basic role setup); this spec uses it for authorization
- Performance score is a pre-existing field on Applicant — this spec displays it read-only, does not define calculation
- Supplier evaluation scoring and weighted criteria are deferred to the Supplier Evaluation Engine spec — this spec only highlights lowest price
- Applicant response workflow (accept/reject/appeal the resolution) is deferred to a separate spec — this spec ends at Resolved
- Notification to applicants when the application is sent back or resolved is deferred to the Notifications spec
- Document generation (PDF resolution letter) is deferred to the Document Generation spec
- The review queue does not support search, filtering, or sorting beyond pagination in this spec
- Reviewer comments are plain text only — no rich text or markdown support
