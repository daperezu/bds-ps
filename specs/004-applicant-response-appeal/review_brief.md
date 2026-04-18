# Review Brief: Applicant Response & Appeal

**Spec:** specs/004-applicant-response-appeal/spec.md
**Generated:** 2026-04-17

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

After review and approval (spec 002), an applicant must explicitly accept or reject each item the reviewers decided on. Accepted items advance toward document generation / signatures / payment (future specs); rejected items become final. If the applicant disagrees with one or more rejections, they may open an appeal — a free-form text dispute thread that freezes the entire application until any reviewer resolves it (either upholding the original decision or granting the appeal by reopening the application to an earlier editable state). Appeals are capped by a configurable maximum per application.

## Scope Boundaries

- **In scope:** Per-item accept/reject response; appeal initiation on rejected items; free-form text dispute thread; appeal resolution (uphold / grant-to-draft / grant-to-review); configurable appeal cap; full audit trail.
- **Out of scope:** Notifications, document generation, signatures, payment, closure, attachments in messages, deadlines, auto-decisions, bulk shortcuts, identity anonymization, exporting thread data.
- **Why these boundaries:** The feature closes the "applicant-must-decide" gap in the workflow without pulling in adjacent concerns (the rest of the lifecycle is future specs). Keeping the dispute thread text-only and the state machine single-linear trades some user-friendliness for clean modeling.

## Critical Decisions

### Per-item accept/reject, whole-application appeal (hybrid)
- **Choice:** Granular accept/reject per item, but one appeal covers the whole application.
- **Trade-off:** Richer applicant control vs. simpler appeal model.
- **Feedback:** Does "one appeal per response cycle" match how disputes will actually arise in practice, or should appeals be per-item?

### Respond first, appeal as escalation
- **Choice:** Applicant must complete a full per-item response before opening an appeal. Appeals are scoped to rejected items.
- **Trade-off:** Forces the applicant to commit to decisions before disputing, which simplifies state but adds a step.
- **Feedback:** Is this ordering acceptable, or should applicants be able to dispute an item before responding to the rest?

### Application freezes while appeal is open
- **Choice:** Single linear state machine — accepted items wait until the appeal resolves.
- **Trade-off:** Simpler reasoning and testing; slower perceived progress for the applicant.
- **Feedback:** Is freeze the right default, or should accepted items advance in parallel?

### Reopen target is reviewer's choice
- **Choice:** When an appeal is granted, the resolving reviewer decides whether the application goes back to draft (applicant edits) or back to review (reviewers revise).
- **Trade-off:** Flexibility per dispute type vs. adding a decision point for reviewers.
- **Feedback:** Will reviewers have enough context to pick correctly, or should one target be the default?

### Configurable appeal cap on SystemConfiguration
- **Choice:** Admin-configurable maximum appeals per application; enforced across all reopen cycles.
- **Trade-off:** Guarantees state-machine termination without forcing a hardcoded limit.
- **Feedback:** Is a single global cap sufficient, or should the cap vary by application type / priority?

## Areas of Potential Disagreement

### All-stall vs. parallel progress during appeal
- **Decision:** All items freeze when an appeal opens.
- **Why this might be controversial:** Applicants who appeal one rejected item have their accepted items stuck too, which may feel punitive.
- **Alternative view:** Accepted items advance independently while the appeal only affects disputed items.
- **Seeking input on:** Whether freeze-everything is acceptable given the audit/consistency benefits, or whether the UX cost is too high.

### No deadlines on any action
- **Decision:** Applications can sit in any state indefinitely; no auto-reject / auto-accept on timeout.
- **Why this might be controversial:** Applications could be stuck forever if an applicant never responds.
- **Alternative view:** Add deadlines with flag-for-reviewer-attention on expiry (not auto-decide).
- **Seeking input on:** Whether operational tooling / reporting (future) will be sufficient to surface stuck applications.

### Text-only dispute thread (no attachments)
- **Decision:** Dispute messages are text only; new documents require a grant-to-draft reopen.
- **Why this might be controversial:** A single missing quotation could force a full reopen-to-draft for what should be a quick exchange.
- **Alternative view:** Allow attachments so disputes can be resolved without reopening.
- **Seeking input on:** Whether the reopen-to-draft path is the right "escape hatch" or if inline attachments would be materially better.

### Any reviewer can resolve (no separation of duties)
- **Decision:** Any user with the Reviewer role can participate in and resolve an appeal, including the original reviewers.
- **Why this might be controversial:** Some processes require a fresh reviewer on appeals to avoid reviewer lock-in.
- **Alternative view:** Reserve resolution to a supervisor role, or exclude original reviewers.
- **Seeking input on:** Whether audit trail + configurable cap provides sufficient integrity, or whether role separation is needed.

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Applicant's per-item decision record | `ApplicantResponse` | Parallel to existing review concepts |
| Formal dispute | `Appeal` | Single entity per dispute instance |
| Thread message | `AppealMessage` | Text-only, owned by `Appeal` |
| Resolution outcomes | `uphold`, `grant — reopen to draft`, `grant — reopen to review` | Explicit, enumerated |
| Configuration attribute | `maximum appeals per application` | Extension of existing `SystemConfiguration` from spec 001 |

## Open Questions

- [ ] Is `ApplicantResponse` a persisted snapshot entity or reconstructed from item-level state? (Planning decision, both satisfy the spec.)
- [ ] Is `AppealMessage` a child entity with its own identity, or a value object in a collection on `Appeal`? (Planning decision.)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| "Stuck" applications with no deadlines (applicant never responds, never resolves) | Medium | Appeal cap prevents unbounded dispute loops; future reporting spec can surface stale applications. Acceptable for now. |
| Reviewer moderates their own decision in the dispute thread | Low | Audit trail captures every action with attribution; cap prevents infinite loops. Separation of duties can be added later without breaking this spec. |
| Grant-to-draft creates a fresh review cycle that resets all prior item decisions | Medium | Intentional (fresh review ensures revised decisions are consistent). Clearly documented in Assumptions. |
| Freeze during appeal creates a poor applicant experience | Medium | Accepted by design in favor of state-machine simplicity; revisit if users complain. |

---
*Share with reviewers before implementation.*
