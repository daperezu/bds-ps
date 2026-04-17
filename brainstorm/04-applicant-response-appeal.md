# Brainstorm: Applicant Response & Appeal

**Date:** 2026-04-17
**Status:** spec-created
**Spec:** specs/004-applicant-response-appeal/

## Problem Framing

With the core model (spec 001), review/approval workflow (spec 002), and supplier evaluation engine (spec 003) done, reviewers can now evaluate applications and make per-item decisions with recommended suppliers. But there is no mechanism for the applicant to respond to those decisions, and no process for dispute resolution when the applicant disagrees. Approved applications sit in limbo — they cannot advance to document generation, signatures, or payment because no one has confirmed what the applicant actually wants to move forward on.

This is the fourth feature in the decomposition of the full Funding Request & Evaluation Platform SRS. It closes the human-decision loop by letting applicants accept/reject per item and appeal unfavorable decisions.

## Approaches Considered

Brainstorming worked through a sequence of design forks. For each fork, the selected option is marked; alternatives were explicitly rejected.

### 1. Response granularity

- **A) Whole-application only** — one decision covers everything.
- **B) Per-item only** — accept some items, reject others, appeal specific ones.
- **C) Per-item accept/reject + whole-application appeal (selected)** — granular response, single appeal scope.
- **D) Deferred**.

Selected C: gives applicants meaningful control without splintering appeals into per-item disputes that are harder to model and resolve.

### 2. Appeal mechanics

- **A) Re-review by same committee**.
- **B) Escalation to a higher authority**.
- **C) Structured re-submission to a specific editable state**.
- **D) Free-form dispute thread (selected)** — conversational exchange between applicant and reviewer until explicit resolution.

Selected D: simplest to build without inventing new roles/permissions; flexible enough to accommodate varied dispute content.

### 3. Interaction between response and appeal

- **A) Mutually exclusive**.
- **B) Appeal first, then respond**.
- **C) Independent tracks (parallel accept + appeal)**.
- **D) Respond first, appeal as escalation (selected)** — must complete per-item response; only rejected items can trigger an appeal.

Selected D: forces commitment to per-item decisions before disputing, which keeps the state machine linear.

### 4. Appeal outcomes

- **A) Binary uphold/reverse**.
- **B) Three outcomes including partial revision**.
- **C) Open-ended with free-text resolution**.
- **D) Reopen to an earlier state (selected)** — reuses existing workflow states (draft or review) for corrective action.

Selected D: leans on spec 002's existing review flow rather than inventing new resolution semantics. Answers spec 002's open thread about whether a `Resolution` entity is needed — it is not; state transition + audit is sufficient.

### 5. Parallel progress during appeal

- **A) All stall (selected)** — application freezes; nothing advances.
- **B) Accepted items advance independently**.
- **C) Provisional advancement**.
- **D) Applicant chooses**.

Selected A: single linear state machine; easier to model, test, and audit. UX cost is acceptable given the audit benefits.

### 6. Authority in the dispute thread

- **A) Original reviewers only**.
- **B) Dedicated appeal role**.
- **C) Mixed (reviewers engage, supervisor resolves)**.
- **D) Any reviewer (selected)** — any user with the Reviewer role can participate and resolve.

Selected D: reuses the existing Reviewer role from spec 002; no new permissions.

### 7. Reopen target

- **A) Always back to review**.
- **B) Always back to draft**.
- **C) Reviewer chooses (selected)** — uphold, or grant with draft/review target picked by resolver.
- **D) Item-scoped editability**.

Selected C: different disputes call for different remedies; reviewer has the context to pick.

### 8. Appeal rounds

- **A) Single appeal only**.
- **B) Per-cycle appeal**.
- **C) Unlimited**.
- **D) Capped at N, configurable (selected)** — admin-settable on existing SystemConfiguration.

Selected D: guarantees state-machine termination while remaining flexible. Leverages the SystemConfiguration pattern from spec 001.

### 9. Deadlines

- **A) No deadlines (selected)**.
- **B) Response deadline with auto-reject**.
- **C) Response deadline with auto-accept**.
- **D) Configurable deadlines, reviewer action on expiry**.

Selected A: YAGNI. Auto-decisions on money matters are risky; absence of deadlines is acceptable given the appeal cap provides a terminating condition.

### 10. Attachments in dispute thread

- **A) Text only (selected)**.
- **B) Both sides**.
- **C) Applicant only**.
- **D) Deferred**.

Selected A: keeps scope tight; attachments can be added later without schema breakage. If new documents are needed, grant-to-draft reopens the application to the existing upload flow.

## Decision

Ship the feature as **Per-item response + whole-application appeal with free-form dispute thread**:

- Applicant accepts/rejects each item individually; response is immutable once submitted.
- Applicant can open one appeal after a complete response, scoped to rejected items.
- Appeal opens a text-only thread; any Reviewer can participate. Application freezes.
- Resolution is explicit: **uphold** (decision stands, application resumes) or **grant** (reviewer picks reopen-to-draft or reopen-to-review).
- Cumulative appeal count capped by a new configurable attribute on `SystemConfiguration`.
- No deadlines, no notifications, no attachments — all deferred to future specs.

Key entities introduced: `ApplicantResponse`, `Appeal`, `AppealMessage`. `SystemConfiguration` gains a new attribute: maximum appeals per application.

## Open Threads

- Persistence model for `ApplicantResponse`: durable snapshot vs. reconstructed from item-level state. (Deferred to planning.)
- Representation of `AppealMessage`: child entity with identity vs. value object in a collection on `Appeal`. (Deferred to planning.)
- Operational visibility: how do admins surface applications stuck in the response stage with no applicant activity, given there are no deadlines? (Likely addressed in a future reporting spec.)
- Whether `ApplicantResponse` decisions should be visible to reviewers in read-only form even before an appeal is opened (assumed yes by audit requirements, but UX surface not specified).
