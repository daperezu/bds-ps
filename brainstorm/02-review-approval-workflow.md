# Brainstorm: Review & Approval Workflow

**Date:** 2026-04-15
**Status:** spec-created
**Spec:** specs/002-review-approval-workflow/

## Problem Framing

With the core data model and application submission workflow complete (spec 001), submitted applications have nowhere to go. The review/approval workflow is the next critical piece — it's the primary business value of the platform, where internal reviewers evaluate funding requests and make item-level decisions. Without it, the system captures applications but cannot process them.

This is the second feature in the decomposition of the full Funding Request & Evaluation Platform SRS.

## Approaches Considered

### A: Direct Review on Application (Selected)
- Review decisions stored as fields on existing Item entity (status, comment, supplier selection, equivalence flag)
- Application gains new state transitions (Under Review, Resolved)
- Pros: Minimal new entities, aligns with rich domain model, straightforward UI
- Cons: Review-round history limited to version history — no first-class review-round concept

### B: Separate Resolution Entity
- A Resolution entity created per review cycle, containing ItemResolution records
- Pros: Clean separation of submission and review data, full review-round history
- Cons: More entities and joins, complexity not justified until appeal/response workflows

### C: Event-Sourced Review Trail
- Every reviewer action stored as a discrete event, state derived from stream
- Pros: Complete audit trail, point-in-time reconstruction
- Cons: Significant complexity overhead, overkill for current scale, doesn't fit existing patterns

## Decision

Selected **Approach A: Direct Review on Application**. It follows existing patterns from spec 001, keeps complexity low, and version history provides adequate auditability. If review-round tracking becomes important for the Appeal spec, we can evolve toward Approach B then — YAGNI.

Key design decisions:
- **Shared queue** — any reviewer picks up any submitted application, no locking
- **Item-level decisions** with explicit "Finalize Review" action at application level
- **Request More Info** sends application back to Draft with full editing access
- **Comments** always optional (plain text only), UI encourages but doesn't enforce
- **Supplier selection** mandatory for approval, system highlights lowest price (full scoring deferred)
- **Single Resolved state** — no sub-types (approved/rejected/partial), item statuses tell the story
- **Finalization warning** for unresolved items — reviewer can confirm (implicit rejection) or go back
- **Technical equivalence** — reviewer flags it, non-equivalent triggers auto-rejection

## Open Threads

- Should pagination page size be configurable or fixed?
- Will version history be sufficient for audit needs, or will the Appeal spec need a Resolution entity?
- Does full item-status reset on send-back create unnecessary re-work for reviewers?
