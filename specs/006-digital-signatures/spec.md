# Feature Specification: Digital Signatures for Funding Agreement

**Feature Branch**: `006-digital-signatures`
**Created**: 2026-04-18
**Status**: Draft
**Input**: Out-of-band signing workflow for the generated Funding Agreement. The applicant downloads the generated agreement PDF, signs it externally using any PDF-stamping application of their choice (e.g., Adobe Reader), and uploads the signed counterpart back to the platform. A reviewer visually verifies the upload and either approves it (executing the agreement) or rejects it with a comment (sending the applicant back to re-sign). No in-app signature capture, no e-signature provider, no cryptographic verification.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Applicant signs and agreement is executed (Priority: P1)

After the Funding Agreement has been generated (spec 005), the applicant downloads the agreement PDF, signs it externally with their preferred tool, uploads the signed counterpart, a reviewer visually verifies it and approves it, and the application transitions to "Agreement Executed".

**Why this priority**: This is the happy path that delivers the core value of the feature — without it, funding cannot legally advance to payment and closure. Every other scenario is a variation around this core journey.

**Independent Test**: With a fully generated Funding Agreement in place, an applicant can download the PDF, upload any valid signed PDF, a reviewer can approve it, and the system records "Agreement Executed" with a complete audit trail. Deliverable as an MVP.

**Acceptance Scenarios**:

1. **Given** the Funding Agreement has been generated and made available to the applicant, **When** the applicant opens the agreement view, **Then** a download action is available that returns the generated PDF byte-identically to the original.
2. **Given** the applicant has a signed counterpart PDF of the agreement, **When** they upload it through the platform, **Then** the upload is accepted, the application state moves to "Signed Upload Pending Review", and the event is audit-logged with timestamp and actor.
3. **Given** a signed upload is pending review, **When** a user with the reviewer role opens the upload and approves it, **Then** the application transitions to "Agreement Executed" and the approval is audit-logged with timestamp, reviewer identity, and any comment supplied.
4. **Given** the application is in "Agreement Executed" state, **When** the applicant or a reviewer revisits the agreement view, **Then** both the generated PDF and the signed counterpart PDF are retrievable for the lifetime of the application.

---

### User Story 2 - Reviewer rejects a signed upload and applicant re-uploads (Priority: P1)

A reviewer opens a pending signed upload, finds it unacceptable (e.g., signature missing, wrong document, illegible), and rejects it with a required comment. The applicant sees the rejection comment and uploads a corrected signed PDF. The reviewer then approves the corrected upload. No agreement regeneration occurs; no retry limit is enforced by the system.

**Why this priority**: Rejection is the expected second-most common path (most rejections will be user error during external signing). Without it, the system only supports the optimistic path and fails the first time an applicant signs incorrectly. Together with Story 1, this delivers a fully usable signing loop.

**Independent Test**: Starting from a pending signed upload, a reviewer can reject it with a comment, the applicant can upload a different signed PDF, and the reviewer can approve the second attempt. The audit trail captures every rejection, re-upload, and final approval.

**Acceptance Scenarios**:

1. **Given** a reviewer is viewing a pending signed upload, **When** they submit a rejection without providing a comment, **Then** the rejection is blocked and the reviewer is prompted to supply a comment.
2. **Given** a reviewer rejects a signed upload with a comment, **When** the applicant next views the agreement, **Then** the rejection comment is visible and the application is back in a "ready to upload signed agreement" state.
3. **Given** the application has one or more prior rejected uploads, **When** the applicant uploads a new signed PDF, **Then** the new upload becomes the pending one without invalidating the audit history of prior uploads.
4. **Given** an applicant has been rejected multiple times with no system-imposed ceiling, **When** a reviewer eventually approves a subsequent upload, **Then** the application transitions to "Agreement Executed" and the full rejection/upload history remains intact in the audit trail.

---

### User Story 3 - Applicant withdraws or replaces a pending upload before review (Priority: P2)

An applicant uploads a signed PDF, then realizes they uploaded the wrong file or signed on the wrong page, before any reviewer has acted on it. They either withdraw the pending upload (returning the application to "ready to upload") or upload a superseding signed PDF (which automatically replaces the pending one). The prior upload is preserved in the audit trail.

**Why this priority**: Prevents wasted reviewer effort and avoids forcing applicants to wait for rejection on an upload they already know is wrong. Cheap UX win, but the system is still functional without it — applicants can wait for rejection.

**Independent Test**: An applicant can upload a file, then withdraw or replace it before any reviewer acts. The audit trail shows both actions; only the latest upload is visible as pending to reviewers.

**Acceptance Scenarios**:

1. **Given** the applicant has a pending signed upload that no reviewer has acted on, **When** they submit a withdraw action, **Then** the application returns to "ready to upload signed agreement" and the withdrawal is audit-logged.
2. **Given** the applicant has a pending signed upload that no reviewer has acted on, **When** they upload another signed PDF, **Then** the new upload becomes the pending one, the prior upload is retained in the audit trail (not exposed as pending), and a replacement event is audit-logged.
3. **Given** a reviewer has already acted on the upload, **When** the applicant attempts to withdraw or replace it, **Then** the action is blocked with a message indicating the upload is no longer replaceable by the applicant.

---

### User Story 4 - Reviewer regenerates the agreement before first signed upload (Priority: P3)

Before the applicant has uploaded any signed PDF, a reviewer or approver (per spec 005 role rules) regenerates the agreement PDF to correct a mistake (e.g., wrong amount, typo in terms). Any downloaded copy the applicant holds becomes stale; the applicant must re-download the fresh version before their signed upload will be accepted.

**Why this priority**: This is a rescue path for rare pre-signing corrections. It protects the integrity of the signed-document chain. Not part of the MVP because most agreements will be generated correctly the first time.

**Independent Test**: With no signed upload yet submitted, a reviewer can regenerate the agreement. A subsequent upload attempt tied to the prior generated version is rejected with a prompt to re-download; once the applicant downloads the new version and uploads a signed copy of it, the upload is accepted.

**Acceptance Scenarios**:

1. **Given** no signed upload exists, **When** a reviewer regenerates the agreement, **Then** the new PDF replaces the prior one and the regeneration is audit-logged.
2. **Given** at least one signed upload has been submitted (pending, rejected, or approved), **When** any actor attempts to regenerate the agreement, **Then** the attempt is blocked with an error message indicating that regeneration requires administrative back-out.
3. **Given** the agreement has been regenerated while the applicant was externally signing a now-stale copy, **When** the applicant uploads their signed PDF, **Then** the system detects that the underlying generated document is superseded and prompts the applicant to re-download and re-sign before accepting the upload.

---

### Edge Cases

- Applicant uploads a file that is not a PDF (including a non-PDF file renamed with a `.pdf` extension): the upload is rejected at intake with a clear error; no pending upload is created and no signing attempt is counted.
- Applicant uploads a PDF that exceeds the configured size limit: the upload is rejected at intake with a message including the size limit.
- Reviewer rejects the same applicant repeatedly with no system-imposed ceiling: allowed; repeated rejections are observable via the audit trail and will be surfaced later by a future reporting/ops-visibility feature.
- Applicant loses their downloaded PDF: they may re-download the same byte-identical PDF at any point until lockdown (first signed upload).
- Reviewer approves at the same moment that the applicant is replacing/withdrawing a pending upload: the system resolves the race via a version stamp on the pending-upload record; whichever action is committed against the current version wins, and the losing action returns a clear conflict error.
- Signed PDF uploaded is unrelated to the generated agreement (e.g., a grocery list saved as PDF): the reviewer detects this during visual verification and rejects with a reason; no automated content verification is expected.
- Applicant never uploads a signed PDF: the application remains in the signing stage indefinitely (no deadline, no auto-timeout, no auto-decline).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow the applicant to download the generated Funding Agreement PDF byte-identically, until the first signed upload is received.
- **FR-002**: System MUST accept a single signed PDF upload per signing round, with total file size bounded by a system-configurable upload limit.
- **FR-003**: System MUST reject uploads that are not valid PDFs (including non-PDFs with a renamed `.pdf` extension) at intake, with a clear error, and MUST NOT record a rejected intake as a signing attempt or pending upload.
- **FR-004**: System MUST reject uploads that exceed the configured size limit at intake, with a clear error including the limit value.
- **FR-005**: Applicants MUST be able to withdraw or replace their own pending signed upload at any time before a reviewer has acted on it; the system MUST preserve all prior uploads and actions in the audit trail.
- **FR-006**: Users with the reviewer role (as defined in specs 002 and 003) MUST be able to view pending signed uploads and take exactly one of two actions on each: approve or reject.
- **FR-007**: System MUST require a reviewer-supplied comment on rejection; approval comments MAY be supplied but are not required.
- **FR-008**: On reviewer approval, the system MUST transition the application to an "Agreement Executed" state, which is terminal for this feature and serves as the handoff point for the future Payment & Closure capability.
- **FR-009**: On reviewer rejection, the system MUST return the application to a "ready to upload signed agreement" state, MUST expose the rejection comment to the applicant, and MUST NOT cap the number of rejection/re-upload cycles.
- **FR-010**: System MUST permit authorized roles (reviewer/approver, per spec 005 role rules) to regenerate the generated Funding Agreement PDF **only until** the first signed upload is received; after that point, regeneration MUST be blocked with a clear error directing the actor to administrative back-out (which is out of scope for this feature).
- **FR-011**: When the applicant submits a signed upload tied to a generated-PDF version that has since been superseded by a regeneration, the system MUST reject the upload with a prompt to re-download the current generated version and re-sign before retrying.
- **FR-012**: System MUST record every state-changing event — download, upload intake (accepted), upload replacement, upload withdrawal, reviewer approval, reviewer rejection, agreement regeneration, regeneration-block, and any supplied comment — in the application's audit trail with timestamp and attributable actor identity.
- **FR-013**: System MUST retain the signed PDF and the complete signing audit trail, visible to the applicant and to reviewer-role users, for the lifetime of the application.
- **FR-014**: System MUST NOT enforce any deadline, automatic timeout, automatic decline, or explicit applicant-decline action on the signing stage; the stage remains open indefinitely until a signed upload is approved.
- **FR-015**: When a reviewer action and an applicant withdraw/replace action are submitted concurrently against the same pending upload, the system MUST resolve the conflict via a version stamp on the pending-upload record — whichever action is committed against the current version wins, and the losing action MUST return a clear conflict error without partial state change.

### Key Entities

- **Signed Upload**: A single signed-PDF artifact submitted by an applicant in response to a generated Funding Agreement. Attributes: reference to the generated-agreement version it corresponds to, uploader identity, upload timestamp, status (pending review, superseded, withdrawn, rejected, approved), version stamp for concurrency control, storage reference in `IFileStorageService`.
- **Signing Review Decision**: A reviewer's approve-or-reject verdict on a specific Signed Upload. Attributes: reviewer identity, decision (approved or rejected), comment (required on reject, optional on approve), decision timestamp, reference to the Signed Upload.
- **Signing Audit Entry**: An immutable record of a signing-stage event (download, upload, replacement, withdrawal, approval, rejection, regeneration, blocked regeneration). Attributes: event type, actor identity, timestamp, reference to related Signed Upload (if applicable), comment (if applicable).
- **Agreement Lockdown Flag**: Per-application indicator that flips from unlocked to locked upon the first Signed Upload being accepted at intake. Enforces the "no regeneration after first signed upload" rule.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An applicant can complete the full happy-path signing journey — from downloading the generated agreement to seeing the application in "Agreement Executed" — without support intervention.
- **SC-002**: An applicant whose first signed upload is rejected can submit a corrected signed upload and have it approved, with the rejection comment from the first attempt visible to them during the correction.
- **SC-003**: The generated Funding Agreement PDF returns byte-identical content on every download until the first signed upload is received.
- **SC-004**: Every state-changing event (download, upload intake, replacement, withdrawal, approval, rejection, regeneration, blocked regeneration) produces exactly one timestamped and attributable audit-trail entry.
- **SC-005**: After the first signed upload is received, every attempt to regenerate the agreement is blocked with a clear error; no regeneration can occur without administrative back-out (which is out of scope for this feature).
- **SC-006**: The signed PDF and its complete signing audit trail remain retrievable by the applicant and by reviewer-role users for the entire lifetime of the application (no time-based purge within this feature).
- **SC-007**: A concurrent reviewer-approve and applicant-replace action against the same pending upload never produces a partially committed state; exactly one of the two actions succeeds, the other returns a conflict error, and the audit trail reflects the outcome unambiguously.
- **SC-008**: All critical signing journeys are covered by end-to-end tests (consistent with the Playwright tooling established in spec 001) and pass in CI on every change to this feature: (a) happy path — download, upload, approve, verify executed; (b) rejection loop — download, upload, reject with comment, re-upload, approve; (c) pre-review replacement — upload, withdraw or replace pending upload, verify audit retains both; (d) regeneration lockout — attempt regeneration after first signed upload and verify it is blocked.

## Assumptions

- The generated Funding Agreement from spec 005 exists and is retrievable through the same file-storage contract (`IFileStorageService`) that this feature will also use for signed uploads.
- The reviewer role defined in specs 002 and 003 is the same role that will perform signed-upload verification here; no new role is introduced.
- Review-stage pipeline semantics and state-transition infrastructure from spec 002 can be reused without modification to host the signing stage.
- Audit-trail infrastructure and the `Application`, `Applicant`, and `Document` entities from spec 001 are available and extensible to carry the new signing events without a structural redesign.
- Playwright-based end-to-end testing, introduced in spec 001, is the expected tool for the required e2e journeys in this feature.
- Administrative back-out of the signing stage (e.g., resetting an "Agreement Executed" state back to an earlier state) is handled outside this feature and is not part of its scope.
- Notifying the applicant that the agreement is ready for signing (or that a review decision has been issued) is deferred to the future Notifications feature; for this feature, the applicant discovers new state by viewing the application in the platform.
- The upload size limit defaults to 20 MB but is treated as a system-configurable value; planning will confirm the initial value.

## Dependencies

- **Spec 005 (Funding Agreement Generation)**: source of the PDF to be signed; shared `IFileStorageService` contract for signed-upload storage.
- **Spec 002 (Review/Approval Workflow)**: supplies the reviewer role and the pipeline-stage pattern reused here for the signing stage.
- **Spec 001 (Core Model & Submission)**: supplies `Application`, `Applicant`, `Document` entities, the audit-trail infrastructure, and the Playwright e2e test tooling.

## Out of Scope

- In-app signature capture of any kind (typed name, drawn signature, canvas, OTP-confirmed click-to-sign, etc.).
- External e-signature provider integration (DocuSign, Adobe Sign, Firmamex, Certicámara, or any other provider).
- PKI / qualified electronic signatures (*firma electrónica cualificada*) and any certificate-authority integration.
- Cryptographic verification of uploaded PDFs or automated detection of signature presence/placement on the uploaded PDF.
- Notifications to the applicant (or reviewers) about signing-stage events; all discovery is in-platform for this feature.
- Payment processing, funds disbursement, or agreement closure — handoff point only; the future Payment & Closure feature picks up from the "Agreement Executed" state.
- Administrative back-out of the signing stage (resetting a locked agreement, re-opening an executed application). Will be handled by existing or future admin tooling, not by this feature.
- Signing deadlines, expirations, or automatic decline on inactivity.
- Execution-certificate generation, watermarking, or cover pages on the signed PDF.
- Side-by-side visual comparison tooling between the generated PDF and the signed upload (likely useful, deferred to planning as a UX decision).
