# Feature Specification: Applicant Response & Appeal

**Feature Branch**: `004-applicant-response-appeal`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "Applicant Response & Appeal — after a funding application has been reviewed and approved, the applicant must decide what to do with each item's decision. This feature lets applicants accept or reject each item individually, and — when they disagree with a rejection — open a free-form dispute thread with reviewers. Appeals that succeed return the application to an earlier state for corrective action; appeals that fail make the decision final."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Per-Item Response to Reviewed Application (Priority: P1)

An applicant whose application has been approved sees each item with the reviewer's decision (chosen supplier, amount, acceptance status) and must explicitly accept or reject each item. Once every item has been responded to, accepted items advance to the next stage of the funding workflow; rejected items are marked final and do not progress.

**Why this priority**: This is the minimum viable slice. Without it, approved applications have no way to move forward — they sit in limbo after review. Accept/reject per item is the core transition from "reviewer's decision" to "applicant's confirmation," which the rest of the workflow (documents, signatures, payment) depends on.

**Independent Test**: Take an application in the Approved state (output of spec 002), sign in as the applicant, navigate to the response screen, accept/reject each item, and confirm that accepted items transition to the next stage while rejected items are marked rejected. No appeal flow needs to exist for this story to deliver value.

**Acceptance Scenarios**:

1. **Given** an application is in the Approved state with multiple reviewed items, **When** the applicant opens the response screen, **Then** each item is shown with its reviewer-assigned supplier, amount, and approval status, plus an accept/reject control.
2. **Given** an applicant has not yet responded to every item, **When** they attempt to submit their response, **Then** submission is blocked and the outstanding items are highlighted.
3. **Given** an applicant has responded to every item, **When** they submit the response, **Then** accepted items transition to the next workflow stage, rejected items are marked rejected, and the application records a completed response.
4. **Given** a response has been submitted, **When** the applicant returns to the response screen, **Then** their prior decisions are shown as read-only (no second edit of the same response).

---

### User Story 2 - Appeal via Dispute Thread (Priority: P2)

After completing a per-item response, the applicant disagrees with one or more rejected items and opens an appeal. This starts a free-form dispute thread — a conversation between the applicant and any reviewer — where both sides exchange text messages. While the appeal is open, the entire application freezes: no items advance to the next stage, and reviewers cannot modify the application.

**Why this priority**: Appeals are how disputes get resolved without back-channel communication. They are second priority because the platform can function with only P1: applicants accept decisions they like, and lose on the rest. P2 adds the legitimacy of a structured dispute process. Building it after P1 lets the team ship the core workflow without blocking on the dispute model.

**Independent Test**: After completing a response where at least one item was rejected, the applicant opens an appeal. Verify a dispute thread is created, the applicant can post a message, any user with reviewer role can post a reply, and the application is frozen (accepted items do not advance until the appeal resolves).

**Acceptance Scenarios**:

1. **Given** an applicant has completed a response and rejected at least one item, **When** they open an appeal, **Then** a dispute thread is created scoped to that application, the application freezes (no item progression), and the applicant can post the first message.
2. **Given** an applicant has completed a response with zero rejected items, **When** they attempt to open an appeal, **Then** the action is blocked with a clear message explaining appeals require at least one rejected item.
3. **Given** an applicant has not completed a response, **When** they attempt to open an appeal, **Then** the action is blocked with a clear message explaining the response must be completed first.
4. **Given** an open appeal exists, **When** any user with the reviewer role opens the application, **Then** they can read the full thread and post a reply.
5. **Given** an open appeal exists, **When** two reviewers post replies at roughly the same time, **Then** both replies are persisted in chronological order, attributable to their authors.
6. **Given** an open appeal exists, **When** a reviewer attempts to modify the application (e.g., change a decision outside the appeal resolution flow), **Then** the modification is blocked.

---

### User Story 3 - Appeal Resolution with Uphold or Reopen (Priority: P2)

A reviewer engaged in the dispute thread resolves the appeal by choosing one of two outcomes: **uphold** (the original decision stands — the appeal is denied, the application unfreezes, and accepted items resume advancing) or **grant** (the appeal is accepted — the application returns to an earlier workflow state for corrective action). When granting, the reviewer further picks the reopen target: back to draft (applicant will edit and resubmit, triggering a fresh review cycle) or back to review (reviewers revise the decisions without applicant editing).

**Why this priority**: Without a resolution mechanism, the thread in Story 2 can be opened but never closed — the application would freeze forever. This story is P2 alongside Story 2 because the two together form the complete appeal feature; neither is independently shippable in production.

**Independent Test**: With an open appeal (Story 2), a reviewer resolves it. Uphold: confirm application unfreezes and accepted items advance as if the appeal had never existed. Grant → reopen-to-draft: confirm application transitions back to an editable draft state for the applicant. Grant → reopen-to-review: confirm application transitions to the review state for reviewers to revise decisions.

**Acceptance Scenarios**:

1. **Given** an open appeal, **When** any reviewer resolves it as "uphold," **Then** the appeal is marked resolved, the application unfreezes, and accepted items resume advancing to the next workflow stage.
2. **Given** an open appeal, **When** any reviewer resolves it as "grant — reopen to draft," **Then** the application transitions to a draft state where the applicant can edit items, suppliers, and quotations, and resubmission triggers a fresh review cycle.
3. **Given** an open appeal, **When** any reviewer resolves it as "grant — reopen to review," **Then** the application transitions to a review state where reviewers can revise their decisions, after which the applicant re-enters the response flow.
4. **Given** an open appeal, **When** a reviewer posts a reply without explicitly resolving, **Then** the appeal remains open (resolution requires an explicit action, not just a message).
5. **Given** an application has previously been reopened and is now back at the applicant-response stage, **When** the applicant appeals again and the cumulative appeal count has not reached the configured maximum, **Then** a new appeal is allowed.
6. **Given** an application has reached the configured maximum appeal count, **When** the applicant attempts to open another appeal, **Then** the action is blocked with a clear message explaining the cap has been reached.

---

### Edge Cases

- What happens when an applicant's account is disabled while an appeal is open? → The thread remains visible to reviewers and can still be resolved by any reviewer; disabled applicants cannot post new messages.
- How does the system handle an application whose approved items include amounts that are later changed by a reviewer during a reopen-to-review? → The applicant re-enters the response flow with the revised decisions and responds anew.
- What happens if every item on an application was accepted by reviewers (nothing rejected)? → No appeal is possible (there is nothing to dispute); the applicant can only accept or reject per item on the response screen.
- What happens if the applicant rejects every item? → The response is still complete; no items advance. The applicant may open an appeal on the whole rejection set.
- What happens if the configured appeal cap is set to zero? → No appeals are allowed for any application; the appeal flow is effectively disabled.
- What happens to an open appeal if the application is somehow administratively closed (outside this feature's scope)? → Out of scope; closure flows are future specs.

## Requirements *(mandatory)*

### Functional Requirements

**Per-item response**

- **FR-001**: System MUST present each item of an approved application to the applicant with the reviewer-assigned supplier, amount, and approval status.
- **FR-002**: Applicant MUST explicitly accept or reject each item before the response is considered complete.
- **FR-003**: System MUST block response submission until every item has an accept/reject decision and highlight the outstanding items.
- **FR-004**: On complete response submission, system MUST transition accepted items to the next workflow stage and mark rejected items as final (no further progression).
- **FR-005**: System MUST persist the applicant's response decisions in an auditable form, attributable to the applicant and timestamped.
- **FR-006**: System MUST prevent the applicant from editing a response once submitted (the response is immutable after submission; the only path to revise decisions is through the appeal + reopen flow).

**Appeal initiation**

- **FR-007**: Applicant MUST be able to open an appeal only after submitting a complete per-item response.
- **FR-008**: System MUST block appeal initiation when the applicant has not rejected at least one item.
- **FR-009**: System MUST block appeal initiation when the application's cumulative appeal count has reached the configured maximum (see FR-018).
- **FR-010**: When an appeal is opened, system MUST freeze the application — no accepted items advance to the next workflow stage, and reviewers MUST NOT be able to modify the application outside the appeal resolution flow.

**Dispute thread**

- **FR-011**: System MUST create a dispute thread when an appeal is opened, scoped to the specific application and appeal instance.
- **FR-012**: Applicant and any user with the Reviewer role MUST be able to post text-only messages to the thread.
- **FR-013**: System MUST persist every message with author attribution and timestamp, in chronological order.
- **FR-014**: System MUST NOT allow attachments on dispute thread messages.
- **FR-015**: System MUST handle concurrent messages from multiple reviewers by persisting all messages in the order they are received.

**Appeal resolution**

- **FR-016**: Any user with the Reviewer role MUST be able to resolve an open appeal.
- **FR-017**: Resolver MUST choose one of three explicit outcomes: **uphold**, **grant — reopen to draft**, or **grant — reopen to review**.
- **FR-018**: On **uphold**, system MUST mark the appeal resolved, unfreeze the application, and resume advancement of accepted items.
- **FR-019**: On **grant — reopen to draft**, system MUST transition the application to an editable draft state where the applicant can modify items, suppliers, and quotations, and subsequent resubmission MUST trigger a fresh review cycle.
- **FR-020**: On **grant — reopen to review**, system MUST transition the application to a review state where reviewers revise decisions; upon completion, the applicant MUST re-enter the response flow.
- **FR-021**: System MUST NOT auto-resolve an appeal based on messages alone; resolution requires an explicit resolution action by a reviewer.

**Appeal cap**

- **FR-022**: System MUST support a **maximum appeal count per application**, stored on the existing SystemConfiguration entity (from spec 001) and configurable by administrators.
- **FR-023**: System MUST track the cumulative appeal count per application across all reopen cycles and enforce the cap on every new appeal initiation.

**Audit & access**

- **FR-024**: System MUST record a complete audit trail including every response, every message, every resolution action, and every state transition triggered by this feature, with author attribution and timestamps.
- **FR-025**: Only the owning applicant and users with the Reviewer role MUST be able to view an application's response details and dispute thread; no other roles have access.

### Key Entities *(include if feature involves data)*

- **ApplicantResponse**: Represents the applicant's per-item decisions on an approved application. Captures the set of item-level accept/reject decisions, submission timestamp, and author. Immutable after submission. Multiple responses may exist across the lifetime of an application if it is reopened.
- **Appeal**: Represents a formal dispute opened against a completed response. Tracks open/resolved status, opening timestamp, and the resolution outcome (uphold / grant-to-draft / grant-to-review) when resolved. Associated with exactly one application and one parent response.
- **AppealMessage**: Represents a single message in an appeal's dispute thread. Attributes: author, timestamp, text content. Belongs to one Appeal.
- **SystemConfiguration (extended)**: The existing system configuration entity from spec 001 gains a new attribute: **maximum appeals per application**.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An applicant can view reviewer decisions on every item of an approved application and submit a complete accept/reject response in a single session.
- **SC-002**: After an applicant submits their response, accepted items become visible in the next workflow stage within the same session, and rejected items are permanently marked final.
- **SC-003**: 100% of responses submitted are auditable: given any application, any authorized viewer can see who responded, when, and what was accepted vs. rejected.
- **SC-004**: An applicant with at least one rejected item can open exactly one appeal per response cycle and engage in a multi-message dispute thread with reviewers.
- **SC-005**: Every opened appeal reaches exactly one of three terminal outcomes — uphold, grant-to-draft, or grant-to-review — within the thread's lifetime; no appeal remains open after resolution.
- **SC-006**: When the configured cap is N, no application ever accumulates more than N appeals; attempts beyond N are rejected with a message that explicitly names the cap.
- **SC-007**: During an open appeal, zero items advance to the next workflow stage until the appeal resolves; the freeze is observable on the application view for both applicant and reviewers.
- **SC-008**: Full audit trail: given any application that has gone through the response + appeal flow, reviewers can reconstruct the complete sequence of responses, messages, resolutions, and state transitions in chronological order with author attribution.

## Assumptions

- The "next workflow stage" after response is covered by future specs (document generation, signatures, payment). This feature only needs to mark accepted items as having advanced out of the response stage; downstream stages do not need to exist for the response feature to be deliverable.
- Users authenticate via the existing ASP.NET Identity setup (spec 001). Applicants and reviewers are distinct roles already established in spec 002.
- Reviewer identities are visible to the applicant in the dispute thread (consistent with visibility patterns in spec 002). If anonymization is later required, it is a future change.
- No notifications (email, in-app toast) are sent when responses are submitted, appeals are opened, messages are posted, or appeals are resolved. Users must refresh the relevant screens to see updates. Notifications are a future spec.
- There are no time limits (deadlines, auto-close, auto-decide) on any action in this feature. Applications may sit in the response stage or in an open-appeal state indefinitely.
- When an appeal reopens an application to review, the reviewer who was party to the dispute thread is allowed to participate in the revised review. There is no separation-of-duties rule excluding them.
- When an appeal reopens an application to draft and the applicant resubmits, the resulting fresh review cycle uses the existing review workflow (spec 002) without modification.
- Appeal cap of 0 disables appeals entirely; cap of 1 allows one appeal over the application's lifetime; N allows N across all reopen cycles.

## Out of Scope

- Notifications of any kind (email, push, in-app real-time).
- Document generation, digital signatures, payment, and application closure (future specs).
- Attachments, images, or non-text content in dispute thread messages.
- Deadlines, auto-expiry, auto-accept, or auto-reject on any action.
- Bulk "accept all" or "reject all" shortcuts on the response screen (each item is decided individually).
- Anonymization or redaction of reviewer or applicant identities in the dispute thread.
- Inviting non-reviewer roles (e.g., supervisors, external auditors) into the dispute thread.
- Exporting response history or appeal threads to external formats (future reporting spec).

## Dependencies

- Spec 001 (core model, SystemConfiguration, ASP.NET Identity, ApplicationUser).
- Spec 002 (review workflow, Reviewer role, approval state).
- Spec 003 (supplier scoring — indirect; influences the decisions applicants may dispute).

## Open Questions

- Does the dispute thread warrant a dedicated entity (`Appeal` with a `Messages` collection) or can messages live as value objects on the `Appeal`? Deferred to planning; either is consistent with this spec.
- Is the `ApplicantResponse` concept persisted as a durable snapshot (captured at submission time) or reconstructable from item-level state transitions? Deferred to planning; both satisfy FR-005 and the audit requirements.
