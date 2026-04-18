# Research: Applicant Response & Appeal

**Date**: 2026-04-17

## No Outstanding Unknowns

All technical decisions were resolved during brainstorming (see `brainstorm/04-applicant-response-appeal.md`) and spec review. The spec's two explicit Open Questions are planning-phase decisions resolved below. No `NEEDS CLARIFICATION` markers exist in the spec or plan.

## Decision Log

### D1: ApplicantResponse Persistence — Snapshot vs. Reconstructed

**Spec Open Question**: *"Is the `ApplicantResponse` concept persisted as a durable snapshot (captured at submission time) or reconstructable from item-level state transitions?"*

**Decision**: Persist `ApplicantResponse` as a snapshot with child `ItemResponse` rows.

**Rationale**:
- FR-024 requires a complete audit trail with author attribution and timestamps. A snapshot satisfies this directly; reconstruction would need a parallel audit log anyway.
- FR-006 requires the response to be immutable after submission. A snapshot entity enforces this at the persistence layer; reconstruction couples immutability to a separate event log.
- Multiple responses across reopen cycles (an application that reopens after a granted appeal, then is re-reviewed and re-responded) need distinguishable records. Snapshot rows are naturally disambiguated by a `ResponseNumber` or timestamp column.
- Reconstruction from item-level state would require item-level state transitions to be fully historized, which they are not in spec 002.

**Alternatives considered**:
- Reconstruct from `Item.ApplicantDecision` field added to Item. Rejected: couples audit data to Item, doesn't cleanly support multiple response cycles, and requires a separate audit log for who/when.

### D2: AppealMessage — Entity vs. Value Object

**Spec Open Question**: *"Is `AppealMessage` a child entity with its own identity, or a value object in a collection on `Appeal`?"*

**Decision**: `AppealMessage` is a child entity of the `Appeal` aggregate, with its own primary key.

**Rationale**:
- EF Core supports owned-entity collections, but the configuration is no simpler than a standard one-to-many relationship and has more footguns (cascade delete semantics, non-trivial querying).
- Having stable identity per message enables future features (message edits, per-message audit events, threading) without schema rework.
- Author attribution and timestamp are per-message; an entity is the natural fit.
- Constitution's Rich Domain Model principle is respected either way: messages are only created via `Appeal.PostMessage(author, text)`.

**Alternatives considered**:
- Value object collection on `Appeal` using EF `OwnsMany`. Rejected: no material simplicity gain; less flexibility for future changes.

### D3: ApplicationState Enum — Additive vs. Redesign

**Decision**: Add two new enum values (`AppealOpen = 4`, `ResponseFinalized = 5`) without modifying existing values (`Draft = 0`, `Submitted = 1`, `UnderReview = 2`, `Resolved = 3`).

**Rationale**:
- The existing enum is persisted as integers in `Applications.State`. Rearranging or renaming existing values would require a data migration.
- The new states are truly new (they did not exist in spec 002's model). They belong in the enum, not on derived/transient properties.
- Explicit state is more debuggable than deriving "frozen" from "does an open appeal exist."

**Alternatives considered**:
- Add only `AppealOpen`; keep `Resolved` as the terminal state. Rejected: "Resolved" currently means "reviewer is done" per spec 002 and stays that way if no approved items. Need a distinct "applicant has responded and accepted items are advancing" state.
- Derive all new states from presence of related entities. Rejected: couples state transitions to queries; harder to test; violates explicit state modeling.

### D4: Appeal Cap Configuration

**Decision**: Store the cap as a new `SystemConfiguration` key named `MaxAppealsPerApplication`. Default value `"1"` (one appeal per application lifetime).

**Rationale**:
- `SystemConfiguration` in the existing codebase (spec 001) is a key-value store, not a fixed-columns table. New config values are added without schema changes — they are seed data.
- Default of `1` honors YAGNI: the simplest useful cap. Administrators can raise it as needed.
- A cap of `0` is allowed (disables appeals). Read path returns `0` naturally as integer parse result of the string value.

**Alternatives considered**:
- Add a dedicated column to a fixed-schema config table. Rejected: no such table exists; `SystemConfiguration`'s existing pattern is a better fit.

### D5: Freeze Enforcement During Open Appeal

**Decision**: When `Application.State == AppealOpen`, domain methods on `Application` and its aggregates that would advance state or modify decisions throw `InvalidOperationException`. The freeze is enforced at the domain layer, not at the UI layer.

**Rationale**:
- Constitution Principle II (Rich Domain Model) mandates state transitions be gated by domain methods.
- UI-layer enforcement is bypassable (direct controller calls, concurrent requests). Domain enforcement is the only correctness guarantee.
- Reviewer-side methods (change decision, finalize) also need to respect freeze — enforced via the same `ThrowIfFrozen()` guard.

### D6: Concurrent AppealMessage Writes

**Decision**: Appeal messages are appended via `Appeal.PostMessage()` with no explicit concurrency control on the `AppealMessages` table. `Appeal` itself carries a `RowVersion` for resolution concurrency only.

**Rationale**:
- Two reviewers posting simultaneously is an append-only scenario; both messages should persist in the order they are inserted. No conflict.
- `Appeal.Resolve()` mutates the parent aggregate's state; that's where optimistic concurrency matters.
- EF Core's default behavior for child `Add` operations satisfies the append-only semantics without additional configuration.

**Alternatives considered**:
- Row versioning on `AppealMessages`. Rejected: append-only; no mutation path to conflict.
- Full ordering via explicit `Sequence` column. Rejected: `CreatedAt` timestamp (datetime2 with sufficient precision) plus the primary key (monotonically increasing identity) is sufficient for chronological ordering.

### D7: Reopen-to-Review and Existing Item State

**Decision**: On `grant — reopen to review`, the existing `Application.SendBack()` method (from spec 002) is repurposed/extended to also handle the appeal-driven path. Specifically: the application transitions to `UnderReview`, item review statuses are NOT reset (reviewers may adjust only what they need to), and a fresh `ApplicantResponse` will be created on next submission.

**Rationale**:
- Reusing `SendBack()` is clean, but its current behavior (resetting all item review statuses) is too aggressive for the appeal path — disputes often concern specific items.
- A new domain method `Application.ReopenForReview()` is added that transitions to `UnderReview` without resetting item statuses. `SendBack()` remains as the "full reset" path from spec 002.
- Open thread from spec 002 ("Does full item-status reset on send-back create unnecessary re-work for reviewers?") — this spec does not change that existing behavior, it introduces a parallel path with different semantics.

### D8: Reopen-to-Draft and Existing Draft Flow

**Decision**: On `grant — reopen to draft`, the application transitions to `Draft`, and on subsequent resubmission the existing `Submit` → `UnderReview` flow from spec 001 is reused. The previous `ApplicantResponse` remains in the database for audit; a new one will be created on the next response cycle.

**Rationale**:
- Reuses the existing draft/submit flow without modification.
- Audit integrity: historical responses are never deleted.
- `Appeal` records that caused reopens are marked `Resolved` and linked to the prior `ApplicantResponse`; new response cycles create new `ApplicantResponse` rows.

### D9: Role and Authorization

**Decision**: The `ApplicantResponseController` requires the `Applicant` role and additionally verifies that the authenticated applicant owns the application. Appeal-resolution and message-posting-as-reviewer actions require the `Reviewer` role.

**Rationale**:
- Constitution Quality Gate: "Authorization checks MUST verify resource ownership."
- Spec FR-025: only the owning applicant and Reviewer users can view response details and dispute thread. Enforced in the query handlers and controller.

### D10: Optimistic Concurrency

**Decision**: `Appeal` carries a `RowVersion` column. `Application` already has one (spec 001). `ApplicantResponse`, `ItemResponse`, and `AppealMessage` do not need concurrency tokens because they are append-only or snapshot entities.

**Rationale**:
- Appeal resolution is the scenario where concurrent reviewers could collide (two reviewers hit "Uphold" and "Grant" at nearly the same moment). Row version catches this and rejects the late writer.
- Response submission is a single-author action (applicant); no concurrency concerns.
- Message appending is append-only; no conflict path.

## References

- Constitution: `.specify/memory/constitution.md` (v1.0.0, 2026-04-15)
- Spec: `specs/004-applicant-response-appeal/spec.md`
- Prior specs: `specs/001-core-model-submission/`, `specs/002-review-approval-workflow/`, `specs/003-supplier-evaluation-engine/`
- Existing `Application` entity: `src/FundingPlatform.Domain/Entities/Application.cs`
- Existing `SystemConfiguration` entity: `src/FundingPlatform.Domain/Entities/SystemConfiguration.cs`
