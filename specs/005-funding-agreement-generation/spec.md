# Feature Specification: Funding Agreement Document Generation

**Feature Branch**: `005-funding-agreement-generation`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "Generate the formal Funding Agreement as a deterministic PDF for applications whose review and applicant response are fully resolved with at least one accepted item. Administrators and reviewers trigger generation; applicants, administrators, and reviewers can download the resulting document; administrators and reviewers can regenerate (overwriting the prior file). This is the canonical artifact that feeds later Digital Signatures, Payment, and Closure features."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Administrator Generates the Funding Agreement (Priority: P1)

An administrator opens an application whose review has closed with at least one approved item, whose applicant has responded to every approved item, which has no active appeal, and which has at least one accepted item. The administrator clicks "Generate agreement" and the system produces a PDF containing the funder details, the applicant details, every accepted item with its supplier and price, the overall funded total, the terms and conditions, and empty signature blocks. The administrator is returned to the application page with the agreement now linked and downloadable.

**Why this priority**: This is the minimum viable slice. Without it, there is no Funding Agreement at all and every downstream feature (signatures, payment, closure) is blocked. Every other story in this spec depends on the existence of this document.

**Independent Test**: Can be fully tested by completing an application through response (spec 004) with at least one accepted item, logging in as an administrator, triggering generation, and verifying that a PDF is produced, stored, and accessible from the application page.

**Acceptance Scenarios**:

1. **Given** a fully-resolved application with two accepted items and no active appeals, **When** the administrator clicks "Generate agreement", **Then** a PDF is produced containing both accepted items (each with supplier and price), the applicant details current at that moment, and the funder details from configuration, and the application page now exposes a download link for that PDF.
2. **Given** a fully-resolved application with one accepted item and three rejected items, **When** the administrator clicks "Generate agreement", **Then** a PDF is produced containing only the single accepted item; none of the rejected items appear.
3. **Given** a fully-resolved application, **When** the administrator clicks "Generate agreement" and the generation fails for any reason after rendering has started, **Then** no partial file is retained, no database record is created, the prior state of the application is unchanged, and the administrator is shown a generic retry-or-contact-support message.

---

### User Story 2 - Applicant Downloads the Funding Agreement (Priority: P1)

Once the administrator has generated the funding agreement, the applicant who owns the application sees it listed on their application page and can download the PDF to review before the later signature stage.

**Why this priority**: The document exists to be read and eventually signed by the applicant. A generated-but-hidden agreement has no value. This story closes the loop from "document exists" to "applicant has seen it."

**Independent Test**: Can be fully tested by generating an agreement (Story 1), signing in as the owning applicant, navigating to the application page, and downloading the PDF.

**Acceptance Scenarios**:

1. **Given** the applicant's application has a generated funding agreement, **When** the applicant opens the application page and clicks the agreement download link, **Then** the PDF file is delivered to the applicant intact.
2. **Given** the applicant's application does not yet have a generated funding agreement, **When** the applicant opens the application page, **Then** no download link is shown and no placeholder document is exposed.

---

### User Story 3 - Administrator or Reviewer Regenerates the Funding Agreement (Priority: P2)

After an agreement has been generated, an administrator or reviewer realizes the content should be regenerated (e.g., the applicant updated their profile, a wording correction was deployed). While preconditions still hold, they click "Regenerate", confirm the destructive action in a dialog, and a new PDF replaces the prior one. Only the latest version is retained.

**Why this priority**: Provides a recovery path for mistakes and profile updates without introducing version history or the complexity of immutable-once-generated rules. Required for operational ergonomics.

**Independent Test**: Can be fully tested by generating an agreement (Story 1), modifying the applicant's profile or the contents of an item's accepted data, clicking "Regenerate" with confirmation, and verifying that the downloaded PDF reflects the new data while the prior file is no longer accessible anywhere.

**Acceptance Scenarios**:

1. **Given** an application with a generated funding agreement and preconditions still holding, **When** a reviewer clicks "Regenerate" and confirms the action, **Then** a new PDF replaces the previous one, the previous file is no longer accessible via any route, and the regenerating user and timestamp are recorded.
2. **Given** an application with a generated funding agreement, **When** the regeneration confirmation dialog is cancelled, **Then** the existing PDF remains unchanged and no audit entry is written.

---

### User Story 4 - Generate Action is Blocked When Preconditions Are Unmet (Priority: P2)

The system refuses to generate when the application does not satisfy all of: review closed with at least one approved item; applicant has responded to every approved item; no active appeal on any item; at least one item in final "accepted" state. The UI action is hidden or disabled with an explanatory message, and any direct request that bypasses the UI is also rejected.

**Why this priority**: Generating an agreement for an unresolved application would produce an incorrect, potentially contractually misleading document. This guardrail protects the integrity of every generated agreement.

**Independent Test**: Can be fully tested by attempting to generate on applications in each of the blocked states (in-review, partially responded, active appeal, all-rejected) and verifying that the action is unavailable in the UI and that a direct POST to the generation endpoint is rejected with an explanation.

**Acceptance Scenarios**:

1. **Given** an application whose review is still open, **When** an administrator views the application page, **Then** the "Generate agreement" action is not available.
2. **Given** an application with at least one item still under active appeal, **When** an administrator views the application page, **Then** the "Generate agreement" action is not available and the page explains why.
3. **Given** a fully-resolved application in which every approved item was rejected by the applicant, **When** an administrator views the application page, **Then** the "Generate agreement" action is disabled and the page explains that there is nothing to fund.
4. **Given** an administrator with a stale page, **When** they submit a generation request for an application whose preconditions no longer hold, **Then** the server rejects the request with an explanation and leaves the application unchanged.

---

### User Story 5 - Reviewer Accesses the Funding Agreement (Priority: P2)

A reviewer who was assigned to an application retains read-only access to the generated funding agreement, in addition to the regeneration right granted in Story 3. This supports audit and continuity needs while respecting the scope of the reviewer's role.

**Why this priority**: Reviewers are the human authority behind the decisions captured in the agreement; blocking them from seeing the final document would break operational oversight. Read-only access is a small addition on top of Stories 1 and 3.

**Independent Test**: Can be fully tested by generating an agreement on an application that a specific reviewer reviewed, signing in as that reviewer, and confirming they can download the PDF from the application page.

**Acceptance Scenarios**:

1. **Given** an application with a generated agreement that a specific reviewer previously reviewed, **When** the reviewer opens the application page, **Then** the download link is visible and returns the current PDF.
2. **Given** an application with a generated agreement that a different reviewer reviewed, **When** an unrelated reviewer attempts to open the application page or download the PDF directly, **Then** the system returns a standard not-authorized response.

---

### User Story 6 - Unauthorized Access is Prevented and Non-Disclosing (Priority: P3)

Users with no explicit access to an application (other applicants, unrelated reviewers) cannot read, download, or otherwise discover the presence or absence of a funding agreement on that application.

**Why this priority**: The agreement contains financial and personal data and is near-legal in nature. Information leakage through existence-disclosing authorization responses is a known anti-pattern that must be actively prevented.

**Independent Test**: Can be fully tested by authenticating as an applicant unrelated to a target application and issuing requests to the application page and to any direct download routes, verifying that the responses are identical whether or not the target application has a generated agreement.

**Acceptance Scenarios**:

1. **Given** an authenticated applicant who does not own a given application, **When** that applicant attempts to download the funding agreement for the application, **Then** the response is a standard not-authorized response regardless of whether an agreement exists.
2. **Given** an authenticated reviewer who did not review a given application, **When** that reviewer attempts to download the funding agreement, **Then** the response is a standard not-authorized response regardless of whether an agreement exists.

---

### Edge Cases

- **Applicant profile changes between response and generation.** The generated agreement uses the applicant's name and contact information as they are at the moment of generation; a later profile change does not auto-update the agreement but can be reflected via regeneration.
- **Single accepted item.** An application with exactly one accepted item is a valid generation target and produces an agreement with a one-row items table.
- **Appeal opened after generation.** The prior PDF remains downloadable; the "Regenerate" action becomes unavailable until the appeal resolves and preconditions hold again.
- **Appeal resolution changes the accepted-item set.** The stored PDF may become content-stale; the system does not auto-regenerate; a human must explicitly regenerate to reflect the new state.
- **Zero accepted items after appeal resolution.** Regeneration becomes unavailable; the prior PDF is retained for audit; application closure is handled by a later feature.
- **Very large item list (100+ accepted items).** The agreement renders without truncation; the items table flows cleanly across pages.
- **Quotation document deleted or replaced after acceptance.** The agreement references supplier and price, not the underlying quotation file; changes to the stored quotation file do not affect the already-generated PDF.
- **Download/regeneration race.** A download in progress at the moment of regeneration returns a complete file (either prior or new content is acceptable); partial or corrupt downloads are not acceptable.
- **Currency and decimal formatting.** The document uses a single consistent formatting convention throughout, configured per deployment, defaulting to a Latin-American style (comma decimal separator, period thousands separator).
- **Concurrent generate or regenerate attempts on the same application.** The system serializes these operations so that exactly one file and one metadata record are the final persisted outcome; one party sees a retriable conflict and is asked to reload.

## Requirements *(mandatory)*

### Functional Requirements

**Generation trigger and preconditions**

- **FR-001**: Administrators and reviewers MUST be able to trigger funding agreement generation from the application detail page via a "Generate agreement" action.
- **FR-002**: The "Generate agreement" action MUST be visible and enabled only when ALL of the following hold: (a) the application's review has closed with at least one item approved, (b) the applicant has recorded a final response (accept or reject) on every approved item, (c) no appeal is currently active on any item, and (d) at least one item has final status "accepted".
- **FR-003**: Applicants MUST NOT be able to trigger generation.
- **FR-004**: The system MUST reject any generation request for which the preconditions in FR-002 do not hold at the moment of request, regardless of the UI state that produced the request, and MUST NOT create or modify any agreement record in that case.
- **FR-005**: The UI MUST provide an explanatory message identifying the failed precondition when the "Generate agreement" action is disabled (in particular, when the application has zero accepted items the message MUST indicate that there is nothing to fund).

**Content**

- **FR-006**: The generated document MUST be a single PDF file per application.
- **FR-007**: The generated document MUST include, at minimum:
  - The source application's application number, used as the agreement reference.
  - The generation timestamp.
  - Funder identity fields sourced from configuration.
  - Applicant identity fields sourced from the applicant's profile at the moment of generation.
  - Application reference (application number and submission date).
  - An accepted-items table listing, for each accepted item: item description, category, the supplier of the accepted quotation, unit price, and line total.
  - Overall funded total.
  - Hardcoded terms and conditions.
  - Empty signature blocks for funder and applicant, reserved for a later signatures feature.
- **FR-008**: The document MUST NOT include rejected items, items that never progressed to approval, or items currently under active appeal.
- **FR-009**: The document MUST apply a single, consistent locale and currency formatting throughout, sourced from configuration, with a Latin-American default (comma decimal separator, period thousands separator).

**Storage**

- **FR-010**: The generated PDF MUST be persisted through the platform's existing file storage abstraction, together with metadata including filename, size, content type, storage path, generation timestamp, and the identity of the generating user.
- **FR-011**: Each application MUST have at most one current funding agreement at any time; there is no version history.

**Regeneration**

- **FR-012**: Administrators and reviewers MUST be able to regenerate the funding agreement at any time while the preconditions in FR-002 still hold, via a "Regenerate" action that requires explicit user confirmation.
- **FR-013**: On successful regeneration, the system MUST overwrite the prior PDF file, update metadata, and record the regenerating user and timestamp.
- **FR-014**: The system MUST NOT retain prior versions of the PDF after a regeneration completes successfully.
- **FR-015**: If the preconditions in FR-002 cease to hold after a prior generation (for example, a new appeal is opened), the "Regenerate" action MUST become unavailable until the preconditions hold again; the previously generated PDF remains accessible for download during this period.

**Visibility and access**

- **FR-016**: The applicant who owns the application MUST be able to view and download the current funding agreement from their application page once it has been generated.
- **FR-017**: Administrators MUST be able to view and download the current funding agreement for any application that has one.
- **FR-018**: Reviewers MUST be able to view and download the current funding agreement for applications they have reviewed.
- **FR-019**: Users without explicit access MUST receive a standard not-authorized response, and the system's responses MUST NOT disclose whether or not a funding agreement exists for an application the user cannot access.
- **FR-020**: The PDF MUST be served through an authenticated endpoint that enforces the authorization rules in FR-016 through FR-019; the file MUST NOT be reachable through an unauthenticated path, direct storage URL, or web-root exposure.

**Integrity and failure handling**

- **FR-021**: A failure during rendering or persistence MUST leave the system in its prior state: no partial file MUST remain on storage, no metadata record MUST be created or updated, and any prior agreement MUST remain intact and accessible.
- **FR-022**: Concurrent generate-or-regenerate attempts on the same application MUST be serialized so that exactly one PDF and one metadata record are the final persisted result; losing requests MUST receive a retriable error and be directed to reload.

**Observability**

- **FR-023**: Each successful and failed generation attempt MUST emit a structured log entry including the application identifier, the acting user identifier, a timestamp, and — for successes — the generated file size and — for failures — the failure reason.

### Key Entities *(include if feature involves data)*

- **Funding Agreement**: The system-generated PDF artifact associated with a specific application. Holds a reference to the owning application, metadata about the stored file (filename, size, content type, storage path), the identity and timestamp of the most recent generating user, and a concurrency token used to serialize parallel generation attempts. An application has zero or one current funding agreement; prior versions are not retained.
- **Application** (existing, from earlier features): The source of truth for applicant identity, the accepted-item set, appeal state, and application-level metadata consumed by the generation process.
- **Item** (existing): Each approved item whose final status is "accepted" contributes one row to the generated agreement's items table.
- **Quotation and Supplier** (existing): The accepted quotation on each accepted item supplies the supplier and price displayed in the agreement.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An authorized administrator or reviewer can, starting from a fully-resolved application, click "Generate agreement" and download the resulting PDF within 10 seconds of wall-clock time, including navigation.
- **SC-002**: 100% of accepted items from the source application appear exactly once in the generated agreement's items table; 0% of rejected items appear.
- **SC-003**: The generated PDF opens and renders all content sections described in FR-007 cleanly in at least three common PDF viewers (Adobe Acrobat Reader, Chrome's built-in PDF viewer, Firefox's built-in PDF viewer), without layout corruption or missing content.
- **SC-004**: For every combination of precondition state described in FR-002, the "Generate agreement" action is correctly enabled or disabled; verified by end-to-end tests covering each precondition branch.
- **SC-005**: An applicant can retrieve their own funding agreement but cannot retrieve another applicant's agreement via any request path or URL; verified by authorization tests that exercise both positive and negative cases.
- **SC-006**: After a successful regeneration, a download request returns the new content and the prior content is not retrievable through any endpoint or stored location in the system.
- **SC-007**: Each user story in this specification has passing end-to-end tests covering both the golden path and at least one error or blocked scenario.
- **SC-008**: For a typical application of up to 20 accepted items, end-to-end generation latency from action click to file persisted completes within 3 seconds at the 95th percentile on the reference deployment.

## Assumptions

- A single funder organization operates the platform for the purpose of this feature; multi-funder scenarios are deferred.
- The Applicant, Reviewer, and Administrator roles established in earlier features already exist and are properly assigned on all applications relevant to this feature.
- The existing file storage abstraction introduced in the core submission feature is suitable for the size, lifecycle, and concurrency pattern of the generated agreement PDF.
- The chosen PDF rendering runtime can be configured and hosted within the platform's existing deployment and orchestration story, with its required credentials/licenses supplied at deployment time.
- The funder identity fields (legal name, address, contact) and the locale/currency formatting are supplied as deployment configuration and remain stable for the lifetime of a given deployment.
- No signed, amended, or otherwise externally-committed agreement exists for any application at the time this feature is shipped; regeneration semantics (overwrite, no history) therefore cannot invalidate an existing signature. The lockout rule that will bind regeneration to signing state will be introduced with the later Digital Signatures feature.

## Dependencies

- **Core Model & Submission** (existing): provides the `Application`, `Item`, `Supplier`, `Quotation`, and document storage infrastructure on which this feature reads and writes.
- **Review & Approval Workflow** (existing): provides the per-item approval state that gates which items can appear on the agreement.
- **Applicant Response & Appeal** (existing): provides the per-item final accept/reject status and the appeal lifecycle that together determine the precondition checks in FR-002.
- **Role-based authorization and user identity** (existing): provides the Administrator, Reviewer, and Applicant roles and ownership relationships used by the authorization rules in this feature.

## Out of Scope

- Any document type other than the Funding Agreement (submission receipts, decision letters, appeal resolution letters, closure letters, and any other system-generated document).
- Digital signing of the agreement, signer identity capture, and signature verification.
- Notifications of any form on generation or regeneration (email, in-app, push).
- Payment authorization orders, application closure state transitions, or handling of the zero-accepted-items terminal path.
- Administrator-editable document templates, a template version history, or per-application template overrides.
- Multi-language content within the agreement; a single configured locale is produced.
- Structured amendments, variations, or addenda to a generated agreement.
- Bulk export or reporting of agreements across applications.
- Accessibility conformance beyond clean rendering in common viewers (tagged-PDF, WCAG, PDF-UA).
- A formal retention and purge policy for stored agreement files.
