# Research: Digital Signatures for Funding Agreement

**Feature:** 006-digital-signatures
**Phase:** 0 — Outline & Research
**Date:** 2026-04-18

This document resolves all Open Questions from `spec.md` plus additional questions surfaced by codebase exploration during planning. Each item has a **Decision**, **Rationale**, and **Alternatives considered**.

---

## R-001 — Application-state additions

**Question:** The spec mandates an "Agreement Executed" terminal state on approval (FR-008) and a "ready to upload" return state on rejection (FR-009). Do both need values in `ApplicationState`?

**Decision:** Add exactly **one** new value to `ApplicationState`: `AgreementExecuted = 6`. The "ready to upload" and "pending review" sub-states are **not** on `Application.State`; they are derived from the `SignedUpload` collection's status.

**Rationale:**
- During signing, `Application.State` stays `ResponseFinalized`. The macro state of the application has not changed — only an artifact (SignedUpload) is moving through its own lifecycle.
- On reviewer approval, the macro state flips to `AgreementExecuted`. Terminal for this feature; handoff to future Payment & Closure.
- Matches Constitution II (Rich Domain Model) — sub-state belongs on the owning entity (`SignedUpload.Status`), not duplicated on the parent.
- Minimal schema change: one new enum int, no CHECK constraint touches (the `Applications.State` column stores int).

**Alternatives considered:**
- Add `SigningInProgress` and `SignedUploadPendingReview` as Application states → rejected: duplicates information already on `SignedUpload`, expands state-transition surface for no gain.
- Add a separate `SigningStage` enum on Application → rejected: two parallel state machines on one entity is a smell.

---

## R-002 — Audit trail mechanism

**Question:** The spec refers to an "audit trail" extensively. Does the codebase already have one, and if so, is it appropriate for signing events?

**Decision:** Reuse the existing `VersionHistory` entity (`src/FundingPlatform.Domain/Entities/VersionHistory.cs`) with new `Action` string constants for signing events. Events are appended via `Application.AddVersionHistory(VersionHistory entry)` which is already public.

**Rationale:**
- `VersionHistory` is a generic application-scoped audit-log entity with fields `UserId`, `Action`, `Details?`, `Timestamp` — exactly the shape FR-012 demands.
- Centralizing audit events on one entity keeps reviewers' existing views (if any) consistent and avoids a second audit stream.
- No new table, no new repository — lowest-complexity additive change.
- Action values are conventions, not enums, matching the existing pattern.

**Action string constants (new, defined in the domain layer):**
- `AgreementDownloaded`
- `SignedAgreementUploaded`
- `SignedUploadReplaced`
- `SignedUploadWithdrawn`
- `SignedUploadApproved`
- `SignedUploadRejected`
- `FundingAgreementRegenerationBlocked` (regeneration success is already audited in spec 005)

**Alternatives considered:**
- Introduce a dedicated `SigningAuditEntry` aggregate → rejected: duplicates existing infrastructure; adds a table, a repository, and a query surface for no semantic gain.
- Use a generic event-sourcing library → rejected: overkill for a feature that needs 7 action types.

---

## R-003 — SignedUpload parent aggregate

**Question:** Where should `SignedUpload` records be attached — directly to `Application` or nested under `FundingAgreement`?

**Decision:** Nest under `FundingAgreement` as a 1:* child collection `_signedUploads`. Expose via `IReadOnlyList<SignedUpload> FundingAgreement.SignedUploads`.

**Rationale:**
- Semantically a signed upload is always *of* a specific agreement. The agreement is the artifact; the signed upload is a counterpart to that artifact.
- FundingAgreement is 1:1 with Application, so scoping uploads to Agreement vs. Application is identical in practice, but `FundingAgreement` is the natural home.
- Lockdown logic (FR-010) lives naturally on `FundingAgreement.IsLocked => _signedUploads.Count > 0`.
- All upload-related domain methods (accept/withdraw/replace/approve/reject) belong on `FundingAgreement`, keeping `Application` uncluttered.

**Alternatives considered:**
- Collection on `Application` → rejected: `Application` already has many children; grouping under `FundingAgreement` keeps aggregates tight.
- Separate `SignedUploadCollection` aggregate → rejected: adds a root with nothing to own.

---

## R-004 — SignedUpload ↔ SigningReviewDecision relationship

**Question:** The spec lists `Signing Review Decision` as a distinct entity. Is it a separate aggregate or a child of `SignedUpload`?

**Decision:** Child of `SignedUpload` with a 1:0..1 relationship. Exposed as a nullable navigation property `SigningReviewDecision? ReviewDecision` on `SignedUpload`.

**Rationale:**
- A pending upload has no decision; an approved or rejected upload has exactly one, frozen at the moment of the decision.
- Separation keeps `SignedUpload` status machine clean (the Status field reflects outcome, the ReviewDecision holds the reviewer metadata and comment).
- Matches the spec's Key Entities section verbatim.
- Separate table simplifies the NOT NULL constraint on `Comment` when `Decision = Rejected` (can be enforced by a CHECK constraint on the decision row without polluting the upload row).

**Alternatives considered:**
- Collapse into `SignedUpload` (add `DecisionComment`, `DecidedByUserId`, `DecidedAtUtc` nullable fields) → rejected: violates the spec's explicit entity split and loses the CHECK constraint leverage on the comment field.

---

## R-005 — Agreement Lockdown Flag

**Question:** The spec names an "Agreement Lockdown Flag". Stored boolean or derived?

**Decision:** **Derived**. `FundingAgreement.IsLocked => _signedUploads.Count > 0`. No stored flag, no additional column, no additional invariant to maintain.

**Rationale:**
- Single source of truth: presence of a signed upload *is* the lock.
- Derived state cannot drift from reality.
- Cheap to compute: the signed-upload collection is already loaded when the regeneration path runs (needed for authorization checks anyway).

**Alternatives considered:**
- Stored `IsLocked` boolean on `FundingAgreement` → rejected: introduces a second source of truth that must be kept consistent with upload presence.

---

## R-006 — Generation version marker for regeneration detection

**Question:** FR-011 requires rejecting a signed upload tied to a superseded (regenerated) agreement. How do we detect "tied to an older version"?

**Decision:** Add an integer `GeneratedVersion` (starts at 1, +1 on each `Replace()`) to `FundingAgreement`. The applicant's upload form carries the current `GeneratedVersion` as a hidden field; on intake, if `submittedVersion != currentGeneratedVersion`, the upload is rejected with a prompt to re-download.

**Rationale:**
- Integer monotonic counter is unambiguous (no serialization or tick-precision issues with `DateTime`).
- Additive field on `FundingAgreement`; existing rows can default to 1 at schema deploy (DEFAULT 1 NOT NULL).
- Simple enforcement point: the upload-intake service checks the form token against `FundingAgreement.GeneratedVersion`.
- Stored on the accepted `SignedUpload` for audit/debug visibility (`SignedUpload.GeneratedVersionAtUpload`).

**Alternatives considered:**
- Use `FundingAgreement.GeneratedAtUtc` as the version marker → rejected: tick-precision drift across EF round-trips and serialization is a known gotcha; integer is safer.
- Use `FundingAgreement.RowVersion` → rejected: semantically wrong (RowVersion changes for any update including unrelated metadata).

---

## R-007 — Concurrency: reviewer action vs. applicant replace/withdraw

**Question:** FR-015 requires resolving concurrent reviewer-approve/reject vs. applicant-replace/withdraw against the same pending upload.

**Decision:** Apply EF Core optimistic concurrency on `SignedUpload` via a `RowVersion` (SQL Server `ROWVERSION`) column. All five mutating operations (approve/reject/replace/withdraw + the internal supersede-on-replace) load the record, check `Status == Pending`, and commit with the expected `RowVersion`. Losers get `DbUpdateConcurrencyException` → HTTP 409 with a domain-specific message.

**Rationale:**
- Mirrors the proven pattern from spec 005 (`FundingAgreement.RowVersion` + 409 handling in `FundingAgreementService.PersistGenerationAsync`).
- EF Core handles the mechanism; domain code just declares the token.
- Clear, well-bounded error semantic (409, not a silent overwrite).

**Alternatives considered:**
- Use `SignedUpload.Status` as the concurrency field → rejected: RowVersion is the established pattern and generalises to non-status concurrency cases cleanly.
- Pessimistic lock on the row → rejected: the request rate is low (human clicks); optimistic is fine.

---

## R-008 — Upload size limit configuration

**Question:** FR-002 and FR-004 require a configurable upload size limit. Where does it live and what's the default?

**Decision:** New `SignedUploadOptions` class bound to config section `SignedUpload` (`IOptions<SignedUploadOptions>`). Single property `MaxSizeBytes` with default `20 * 1024 * 1024` (20 MB). Enforced at intake in `SignedUploadService.UploadAsync`, **before** reading the stream into memory.

**Rationale:**
- Established pattern in this codebase (see `FunderOptions`, `FundingAgreementOptions` in `src/FundingPlatform.Application/Options/FunderOptions.cs`).
- Intake enforcement (not domain enforcement) — size is a transport concern; domain should accept a validated stream.
- 20 MB matches the spec's Assumptions default and is comfortably above any realistic signed-PDF size.

**Alternatives considered:**
- Hardcode 20 MB → rejected: violates Constitution VI (configurable where reasonable).
- Use `appsettings.json` form-limits middleware → rejected: does not give a business-friendly error; server-side service-level check produces the UX we want.

---

## R-009 — PDF content validation depth

**Question:** FR-003 requires rejecting non-PDFs "including renamed non-PDF files". How deep does validation go?

**Decision:** Two-layer intake check:
1. **Content-Type** must be `application/pdf` (the HTTP upload provides this).
2. **Magic header** must be `%PDF-` in the first 5 bytes.

No antivirus integration, no deep PDF parsing, no signature-presence detection. Beyond that is the reviewer's visual responsibility.

**Rationale:**
- Magic-header check catches the "renamed `.docx` to `.pdf`" scenario the spec calls out.
- Deeper validation (parsing the PDF, checking for embedded scripts, virus scanning) is not required by any FR and would add a heavy dependency. Documented as explicitly out of scope in the spec.
- Cheap to implement (read first 5 bytes, reset stream).

**Alternatives considered:**
- Accept `application/pdf` header only → rejected: spec explicitly mentions "renamed `.pdf`" files, which content-type alone does not catch if the client lies.
- Antivirus scan via ClamAV → rejected: out of scope; can be layered later without breaking this intake.

---

## R-010 — Reviewer inbox surface for pending signed uploads

**Question:** Reviewers need to find pending signed uploads. Is there an existing reviewer inbox to extend, or do we add a new surface?

**Decision:** Extend the existing `FundingAgreementController` panel on the Application Detail page with a per-application signing section. Add a separate **Signing Review Inbox** as a new action on `ReviewController` (`/Review/SigningInbox`) listing applications whose latest `SignedUpload` is `Pending`. The reviewer clicks through to the Application Detail page to act.

**Rationale:**
- The Application Detail page is the canonical place for per-application actions — adding the signing workflow there keeps reviewer context coherent.
- A dedicated inbox lets reviewers triage across applications without navigating application-by-application.
- Keeps controllers cohesive: `FundingAgreementController` for per-application signing actions; `ReviewController` for cross-application queues.

**Alternatives considered:**
- Add a modal-based review UX without an inbox → rejected: triage across many applications is awkward.
- Put the inbox on `FundingAgreementController` → rejected: wrong scope (controller is per-application).

---

## R-011 — Side-by-side comparison UX (spec open question)

**Question:** Spec open question: should reviewers see a side-by-side view of the generated vs. signed PDF?

**Decision:** **Defer** for this feature. Reviewer UX is two download links (Generated PDF / Signed PDF) + visible metadata (file name, size, uploaded-at, version match) + an approve/reject form.

**Rationale:**
- Side-by-side rendering of two PDFs is a non-trivial UX investment (dual PDF viewers in the browser, sync-scroll, responsive behavior). Deferring is the pragmatic call.
- The metadata display (including the `GeneratedVersion` match indicator from R-006) catches the version-mismatch case automatically.
- Revisit once we have operational data on reviewer time-per-decision.

**Alternatives considered:**
- Embed two `<object>` or `<iframe>` PDF viewers side-by-side → rejected: browser PDF rendering is inconsistent across platforms and would sprawl the feature.
- Use a JS PDF library (e.g., PDF.js) → rejected: adds a new frontend dependency; out of scope.

---

## R-012 — Execution banner / cover page on signed PDF (spec open question)

**Question:** Should the downloaded signed PDF be annotated with an execution banner post-approval?

**Decision:** **Defer**. The signed PDF is served byte-identically to what the applicant uploaded, for all downloaders.

**Rationale:**
- Post-upload mutation of the applicant's PDF changes the audit chain in confusing ways (which bytes were "the signed document" if the server adds a banner?).
- The "executed" state is already visible in the UI; stamping the file adds no legal weight and complicates audit.

**Alternatives considered:**
- Overlay a banner using a PDF library → rejected: same file-integrity concern as above; no functional benefit.

---

## R-013 — Spec 005 regeneration-role precision (spec open question)

**Question:** Spec 005 defines `CanUserGenerateFundingAgreement(isAdministrator, isReviewerAssignedToThisApplication)`. Does the inherited rule suffice for FR-010?

**Decision:** **Yes.** FR-010 inherits `CanUserGenerateFundingAgreement` verbatim and layers the lockdown check on top via a new `Application.CanRegenerateFundingAgreement(out errors)` method.

**Rationale:**
- The role rule is already precise: Admin OR Reviewer-assigned-to-this-application.
- Lockdown is a separate predicate, cleanly composable.
- New method signature makes the regeneration path explicit and testable.

**Alternatives considered:**
- Inline the lockdown check into `RegenerateFundingAgreement()` → rejected: hides the predicate; harder to test.
- Parameterize `CanGenerateFundingAgreement` with a `forRegeneration: bool` flag → rejected: boolean flags are a code smell.

---

## R-014 — Rejection retry cap (spec open question)

**Question:** Should we cap the number of rejection/re-upload cycles?

**Decision:** **No cap.** The signing stage accepts unbounded rejection cycles. Operational visibility is a future reporting-spec concern (already in overview open threads).

**Rationale:**
- Matches the spec's explicit FR-009 ("MUST NOT cap the number of rejection/re-upload cycles").
- The risk is operational (stuck applications), not functional — consistent with the project's existing no-deadlines pattern (spec 004 open thread).
- Adding a cap later is backward-compatible; adding it now would be speculative complexity.

---

## R-015 — Administrative back-out surface (spec boundary question)

**Question:** The spec declares administrative back-out (resetting a locked or executed agreement) out of scope. What happens if ops needs this before a dedicated spec ships?

**Decision:** This feature adds **no** admin back-out surface. Ops uses direct SQL against `SignedUploads` and `FundingAgreements` (documented in `quickstart.md` under "Operational recovery") until a dedicated admin-tooling feature is specified.

**Rationale:**
- Explicit scope boundary from the spec.
- Documenting the manual fallback keeps ops from being stranded without introducing surface area this spec does not own.

**Alternatives considered:**
- Ship a minimal admin-only POST endpoint to unlock an agreement → rejected: out of scope per spec; would expand this feature's attack surface and test obligations.

---

## R-016 — Serving the signed PDF for download

**Question:** The spec says the applicant and reviewers can retrieve the signed PDF for the lifetime of the application. Which controller action serves it?

**Decision:** Add a new action `DownloadSigned(int applicationId, int signedUploadId)` on `FundingAgreementController` that streams the stored signed PDF via `IFileStorageService`. Authorization reuses `Application.CanUserAccessFundingAgreement` exactly.

**Rationale:**
- Parallel to the existing `Download` action for the generated PDF.
- Explicit `signedUploadId` in the route allows historical (rejected/superseded) uploads to be downloaded for audit purposes by authorized roles, not just the approved one.

**Alternatives considered:**
- Add a `type` query parameter on `Download` (`?type=signed`) → rejected: route method dispatch is clearer in MVC and matches existing conventions.
- Only expose the approved upload → rejected: reviewers may need to inspect rejected uploads when handling a support case.

---

## R-017 — Hidden-field version token serialization

**Question:** How does the client carry `GeneratedVersion` on the upload form (R-006)?

**Decision:** Hidden form field `generatedVersion` (integer). Model-bound into the `UploadSignedAgreementViewModel` / command. Server-authoritative check: if `command.GeneratedVersion != agreement.GeneratedVersion`, reject with a specific error code.

**Rationale:**
- Simplest possible transport — no token signing needed because the check is a straight equality against a server-side value.
- An attacker could lie about the value, but that would cause a silent mismatch or a false "please re-download" prompt; they cannot bypass the server-side compare.

**Alternatives considered:**
- Signed token (HMAC) → rejected: no security gain for this check; the server always has the authoritative value.
- URL path parameter → rejected: conflates form state with routing.

---

## R-018 — E2E test file layout

**Question:** Where do the required Playwright e2e tests (SC-008) live?

**Decision:** One test file `DigitalSignatureTests.cs` under `tests/FundingPlatform.Tests.E2E/Tests/`, mirroring the existing `FundingAgreementTests.cs`. Two new Page Objects under `tests/FundingPlatform.Tests.E2E/PageObjects/`: `SigningStagePanelPage.cs` and `SigningReviewInboxPage.cs`. The `FundingAgreementPanelPage` is extended with signing-stage locators.

**Rationale:**
- Matches established project layout.
- One test file per feature keeps the `[Test]` surface discoverable and maps 1:1 to the four SC-008 journeys.

---

## Consolidated outputs from Phase 0

- `ApplicationState` adds one value: `AgreementExecuted = 6`.
- `VersionHistory` is the audit store (no new audit entity).
- New domain entities: `SignedUpload` (child of `FundingAgreement`), `SigningReviewDecision` (child of `SignedUpload`).
- New enums: `SignedUploadStatus` (Pending, Superseded, Withdrawn, Rejected, Approved), `SigningDecisionOutcome` (Approved, Rejected).
- `FundingAgreement` gains: `GeneratedVersion` (int), `_signedUploads` collection, `IsLocked` derived, domain methods `AcceptSignedUpload`, `WithdrawPendingUpload`, `ReplacePendingUpload`, `ApprovePendingUpload`, `RejectPendingUpload`.
- `Application` gains: `CanRegenerateFundingAgreement(out errors)` method, `ExecuteAgreement(userId)` method transitioning to `AgreementExecuted`.
- New options class: `SignedUploadOptions.MaxSizeBytes` (default 20 MB).
- Zero new interfaces for storage (reuses `IFileStorageService`).
- New repository: `ISignedUploadRepository` (read-only projection queries for controllers; mutations go through `FundingAgreement` navigation and `SaveChangesAsync`).
- Concurrency: `SignedUpload.RowVersion` (SQL Server `ROWVERSION`).
- Schema additions: `dbo.SignedUploads`, `dbo.SigningReviewDecisions`. `dbo.FundingAgreements` adds `GeneratedVersion INT NOT NULL DEFAULT 1`.
- No new external dependencies. No new NuGet packages.

All spec Open Questions are resolved. Remaining items are planning-phase configuration values (upload size default is set to 20 MB; revisit at first deployment).
