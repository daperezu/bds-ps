# Research: Review & Approval Workflow

**Date**: 2026-04-15
**Feature**: specs/002-review-approval-workflow/

## Technical Context Resolution

No NEEDS CLARIFICATION items — the technical context is fully resolved from the existing codebase (spec 001 implementation).

## Research Topics

### 1. Extending ApplicationState Enum (Domain + Database)

**Decision**: Add `UnderReview = 2` and `Resolved = 3` to the existing `ApplicationState` enum.

**Rationale**: The enum is stored as an integer in SQL Server. Adding new values with higher ordinals is backward-compatible — existing Draft (0) and Submitted (1) rows are unaffected. The Database project `.sql` file doesn't define the enum (EF Core maps it), so the change is purely in C# code. However, the `IX_Applications_State` index already exists, which will benefit queue filtering for Submitted state.

**Alternatives considered**:
- Separate `ReviewState` enum: Rejected because the application has a single linear state machine, not parallel states.
- String-based state: Rejected because the project uses integer enums consistently.

### 2. Adding Review Fields to Item Entity

**Decision**: Add five new fields to the Item entity: `ReviewStatus` (enum), `ReviewComment` (string?), `SelectedSupplierId` (int?), `IsNotTechnicallyEquivalent` (bool), `ReviewComment` is plain text.

**Rationale**: Approach A (direct review on entities) was selected during brainstorming. Adding fields to Item rather than creating a separate ReviewDecision entity keeps the model simple and aligns with the rich domain model principle — the Item knows its own review state.

**Alternatives considered**:
- Separate `ItemReview` entity per item: More normalized but adds unnecessary joins. Review data is 1:1 with Item and logically belongs on it.
- JSON column for review data: Non-relational, harder to query and index.

### 3. Application State Transitions

**Decision**: The Application entity gains three new domain methods:
- `StartReview()`: Transitions Submitted → Under Review. Idempotent (no-op if already Under Review).
- `SendBack()`: Transitions Under Review → Draft. Resets all item review statuses to Pending.
- `Finalize(bool force)`: Transitions Under Review → Resolved. If `force` is true, implicitly rejects unresolved items.

**Rationale**: Rich domain model — state transitions are gated by the entity, not by services. The `Submit()` method from spec 001 already follows this pattern. `SendBack()` resetting item statuses matches the spec requirement that resubmitted applications get a fresh review.

**Alternatives considered**:
- State machine library (Stateless): Overkill for 4 states and 4 transitions.
- Service-layer state management: Violates Constitution Principle II (Rich Domain Model).

### 4. Pagination for Review Queue

**Decision**: Use EF Core's `Skip()`/`Take()` with a configurable page size (default 25). Query filters on `State == Submitted` to populate the queue.

**Rationale**: Simple offset-based pagination is sufficient for the expected volume. The existing `IX_Applications_State` index supports efficient filtering. No need for cursor-based pagination at current scale.

**Alternatives considered**:
- Cursor-based pagination: Better for large datasets but unnecessary complexity for under 100 concurrent users.
- Client-side pagination (load all): Poor UX if application count grows.

### 5. Lowest-Price Supplier Recommendation

**Decision**: Compute in the application layer when building the review DTO. For each item, find the quotation with the minimum price. If multiple quotations share the minimum price, highlight none (tie).

**Rationale**: This is a simple computation — no need for a domain method. The recommendation is purely advisory and has no business rule implications. Computing it in the DTO mapping avoids adding presentation concerns to the domain.

**Alternatives considered**:
- Domain method `Item.GetRecommendedSupplier()`: Possible, but recommendation is presentation logic, not a business invariant.
- Stored procedure / database view: Unnecessary complexity for a simple min() operation.

### 6. Comment Preservation Across Review Rounds

**Decision**: When `SendBack()` is called, item review comments are NOT cleared — only the `ReviewStatus` is reset to Pending. This preserves reviewer feedback for the applicant to see and for the next reviewer to reference.

**Rationale**: FR-012 mandates comment preservation. The simplest approach is to just not clear them during `SendBack()`. When a new review round begins, the reviewer can update or replace comments on each item.

**Alternatives considered**:
- Comment history (list of comments per item): More complex, deferred to Appeal spec if needed.
- Clearing comments on send-back: Violates FR-012.

### 7. Reviewer Role and Authorization

**Decision**: Add a "Reviewer" role to the ASP.NET Identity seed data. The ReviewController uses `[Authorize(Roles = "Reviewer")]`. A user can have multiple roles (e.g., both Admin and Reviewer).

**Rationale**: The existing pattern uses role-based authorization with `[Authorize(Roles = "...")]` on controllers. The seed data already creates "Applicant" and "Admin" roles — adding "Reviewer" follows the same pattern.

**Alternatives considered**:
- Policy-based authorization: More flexible but unnecessary for simple role checks.
- Reuse Admin role for reviewers: Different concerns — admins manage config, reviewers evaluate applications.

### 8. Optimistic Concurrency for Concurrent Reviewers

**Decision**: Reuse the existing `RowVersion` on Application. When two reviewers edit the same application, the second save fails with `DbUpdateConcurrencyException`, which the service catches and surfaces as a user-friendly error.

**Rationale**: The mechanism already exists from spec 001. No additional concurrency handling needed for Item-level changes because Items are loaded as part of the Application aggregate and saved together.

**Alternatives considered**:
- RowVersion per Item: Would allow more granular conflict detection but adds complexity. Since reviewers work on the whole application at once, application-level concurrency is sufficient.
- Pessimistic locking: Spec explicitly chose no locking (decision B during brainstorming).
