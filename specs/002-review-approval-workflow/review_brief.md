# Review Brief: Review & Approval Workflow

**Spec:** specs/002-review-approval-workflow/spec.md
**Generated:** 2026-04-15

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

This feature enables internal reviewers (staff) to evaluate submitted funding applications through a structured, item-level review process. Reviewers pick applications from a shared queue, assess each item's quotations for technical equivalence, approve or reject items with optional comments, select suppliers for approved items, and finalize the review to move the application to a Resolved state. It also supports a feedback loop where reviewers can send applications back to applicants for corrections.

## Scope Boundaries

- **In scope:** Review queue with pagination, application state transitions (Submitted -> Under Review -> Resolved or back to Draft), item-level decisions (Approve/Reject/Needs Info), supplier selection with lowest-price highlight, technical equivalence flagging with auto-rejection, finalization with unresolved-item warning, reviewer comments (plain text), role-based access for reviewers
- **Out of scope:** Supplier evaluation scoring engine, applicant response (accept/reject/appeal), notifications, PDF document generation, digital signatures, payment & closure, review queue search/filtering/sorting, reviewer dashboards
- **Why these boundaries:** Each deferred feature is an independent subsystem with its own complexity. This spec delivers the core review loop that all downstream features depend on. Supplier scoring is advisory-only here (lowest price) to avoid coupling with the evaluation engine spec.

## Critical Decisions

### No Application Locking
- **Choice:** Multiple reviewers can work on the same application simultaneously
- **Trade-off:** Simplicity over preventing duplicate work; optimistic concurrency handles conflicts
- **Feedback:** Is the expected reviewer count low enough that collisions are rare? Should we revisit if the team grows?

### Send-Back Resets to Draft (Not a Distinct State)
- **Choice:** Applications sent back for more info return to Draft state, not a separate "Needs Info" state
- **Trade-off:** Simpler state machine, but you can't distinguish a fresh draft from a returned one by state alone (reviewer comments provide context)
- **Feedback:** Is losing the state-level distinction acceptable, or would tracking "returned" applications separately be valuable for reporting?

### Direct Review on Existing Entities (No Resolution Entity)
- **Choice:** Review decisions are stored as fields on Item (status, comment, selected supplier, equivalence flag) rather than a separate Resolution entity
- **Trade-off:** Simpler model now, but review-round history is limited to version history. A Resolution entity would give first-class review-round tracking.
- **Feedback:** Will version history be sufficient for audit needs, or should we add a Resolution entity for the Appeal spec later?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### All Items Reset on Send-Back
- **Decision:** When an application is sent back, ALL item statuses reset to Pending, even items the reviewer already approved
- **Why this might be controversial:** Reviewers might expect approved items to retain their status, only re-reviewing flagged items
- **Alternative view:** Keep approved/rejected decisions, only reset Needs Info items to Pending
- **Seeking input on:** Does a full reset create unnecessary re-work for reviewers, or is it the right conservative approach given the applicant can edit anything?

### Implicit Rejection on Forced Finalization
- **Decision:** If a reviewer finalizes with unresolved items (after confirming the warning), those items are implicitly rejected with a system-generated reason
- **Why this might be controversial:** Implicit rejection could surprise applicants who expected those items to be reviewed
- **Alternative view:** Require all items to be explicitly resolved before finalization (no force-finalize)
- **Seeking input on:** Is the warning + confirmation sufficient safeguard, or should forced finalization be removed entirely?

### Comments Always Optional
- **Decision:** Reviewer comments are optional for all decisions, including rejections
- **Why this might be controversial:** Many review systems mandate reasons for rejection to ensure transparency and provide actionable feedback
- **Alternative view:** Make comments mandatory for rejections and needs-info, optional for approvals
- **Seeking input on:** Does the UI encouragement (but not enforcement) provide enough transparency for applicants?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Application state | Under Review | State when a reviewer first opens a submitted application |
| Application state | Resolved | Final state after reviewer finalizes (covers all-approved, all-rejected, and mixed outcomes) |
| Item status | Needs Info | When reviewer requests more information from applicant |
| Action | Finalize Review | Explicit reviewer action to complete the review and transition to Resolved |
| Action | Send Back | Reviewer action to return application to Draft for applicant corrections |
| Field | Technical Equivalence Flag | Boolean on Item indicating whether quotations are technically equivalent |

## Open Questions

- [ ] Should pagination page size be configurable or fixed (e.g., 25 per page)?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Concurrent reviewers overwriting each other's decisions | Med | Optimistic concurrency with conflict detection and reload prompt |
| Reviewer comments lost on send-back/resubmit cycle | High | FR-012 explicitly mandates comment preservation across rounds |
| Applicant edits approved items after send-back | Med | Full reset to Pending on send-back; all items re-reviewed on resubmission |
| Technical equivalence flag incorrectly set | Med | Reviewer can clear the flag to undo auto-rejection |

---
*Share with reviewers before implementation.*
