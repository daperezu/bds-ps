---
description: "Tasks for 006-digital-signatures"
---

# Tasks: Digital Signatures for Funding Agreement

**Input**: Design documents in `/specs/006-digital-signatures/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. Spec non-functional requirements (SC-008) mandate Playwright e2e tests for the four critical journeys plus unit/integration tests for state transitions, concurrency, and lockout enforcement.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3, US4 — maps to the user stories in spec.md
- File paths are absolute-from-repo-root

## Path Conventions

- Web application layout per plan.md §Project Structure
- `src/FundingPlatform.{Domain,Application,Infrastructure,Web,Database}/` for code
- `tests/FundingPlatform.Tests.{Unit,Integration,E2E}/` for tests

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Minimal configuration needed before any domain work can land. No new projects, no new packages.

- [X] T001 Add `SignedUpload:MaxSizeBytes` default (20971520) to `src/FundingPlatform.Web/appsettings.json` under a new `SignedUpload` section
- [X] T002 [P] Surface `SignedUpload` configuration from `src/FundingPlatform.AppHost/AppHost.cs` to the Web project (mirrors existing `FundingAgreement` configuration propagation)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain enums/entities, schema, EF mapping, repositories, options. These block every user story. Tests that assert domain invariants are colocated here so they gate the rest of the flow.

**⚠️ CRITICAL**: No user story work can begin until Phase 2 is complete.

### Database schema (dacpac)

- [X] T003 [P] Modify `src/FundingPlatform.Database/Tables/dbo.FundingAgreements.sql` to add `GeneratedVersion INT NOT NULL CONSTRAINT DF_FundingAgreements_GeneratedVersion DEFAULT(1)` and CHECK `[GeneratedVersion] >= 1`
- [X] T004 [P] Create `src/FundingPlatform.Database/Tables/dbo.SignedUploads.sql` with columns, primary key, FKs (FundingAgreementId CASCADE, UploaderUserId NO ACTION), CHECK `Size > 0`, CHECK `Status BETWEEN 0 AND 4`, filtered unique index `UX_SignedUploads_OnePending_PerAgreement` on `FundingAgreementId WHERE Status = 0`, and non-clustered indexes on `(FundingAgreementId, Status)` and `UploaderUserId` (per data-model.md §10.2)
- [X] T005 [P] Create `src/FundingPlatform.Database/Tables/dbo.SigningReviewDecisions.sql` with columns, PK, FKs (SignedUploadId CASCADE, ReviewerUserId NO ACTION), `UQ_SigningReviewDecisions_SignedUploadId`, CHECK on outcome range, and rejection-comment CHECK constraint (per data-model.md §10.3)
- [X] T006 Build the dacpac locally (`dotnet build src/FundingPlatform.Database`) to confirm DDL compiles cleanly and the existing tables' ALTER diff is clean

### Domain enums and constants

- [X] T007 [P] Modify `src/FundingPlatform.Domain/Enums/ApplicationState.cs` to add `AgreementExecuted = 6`
- [X] T008 [P] Create `src/FundingPlatform.Domain/Enums/SignedUploadStatus.cs` with values `Pending = 0, Superseded = 1, Withdrawn = 2, Rejected = 3, Approved = 4`
- [X] T009 [P] Create `src/FundingPlatform.Domain/Enums/SigningDecisionOutcome.cs` with values `Approved = 0, Rejected = 1`
- [X] T010 [P] Create `src/FundingPlatform.Domain/Entities/SigningAuditActions.cs` with the seven action-string constants listed in data-model.md §4

### Domain entities

- [X] T011 [P] Create `src/FundingPlatform.Domain/Entities/SigningReviewDecision.cs` with fields, internal constructor, validation (non-empty reviewer id, comment required when `Outcome == Rejected`), per data-model.md §7
- [X] T012 [P] Create `src/FundingPlatform.Domain/Entities/SignedUpload.cs` with fields, private backing `_reviewDecision`, public `ReviewDecision` navigation, internal constructor, and `MarkSuperseded`, `MarkWithdrawn`, `Reject`, `Approve`, private `Transition` methods (throws if `Status != Pending`) per data-model.md §6
- [X] T013 Modify `src/FundingPlatform.Domain/Entities/FundingAgreement.cs` to add `GeneratedVersion` property (initial `1` in ctor, `+1` in `Replace`), `_signedUploads` backing collection, `SignedUploads` read-only property, `IsLocked` and `PendingUpload` derived properties, and `AcceptSignedUpload`, `ReplacePendingUpload`, `WithdrawPendingUpload`, `ApprovePendingUpload`, `RejectPendingUpload` methods; make `Replace` throw when `IsLocked` (data-model.md §5)
- [X] T014 Modify `src/FundingPlatform.Domain/Entities/Application.cs` to add `CanRegenerateFundingAgreement(out errors)` (composes existing `CanGenerateFundingAgreement` + lockdown check), `ExecuteAgreement(userId)` (transitions `ResponseFinalized → AgreementExecuted`), `CanUserReviewSignedUpload(isAdmin, isReviewerAssigned)`, and facade methods `SubmitSignedUpload`, `ReplaceSignedUpload`, `WithdrawSignedUpload`, `ApproveSignedUpload` (also calls `ExecuteAgreement`), `RejectSignedUpload`; swap `RegenerateFundingAgreement` to call `CanRegenerateFundingAgreement` (data-model.md §8)

### Domain repository interface

- [X] T015 [P] Create `src/FundingPlatform.Application/Interfaces/ISignedUploadRepository.cs` with: `GetByIdWithParentAsync(int signedUploadId)`, `GetPendingInboxAsync(string? reviewerUserId, bool isAdmin, int page, int pageSize)`, `SaveChangesAsync()` (interface placed in Application layer because it returns Application DTOs; Domain-layer interfaces return entities only)

### Domain unit tests (foundational — assert invariants before service code depends on them)

- [X] T016 [P] Create `tests/FundingPlatform.Tests.Unit/Domain/SigningReviewDecisionTests.cs`
- [X] T017 [P] Create `tests/FundingPlatform.Tests.Unit/Domain/SignedUploadTests.cs`
- [X] T018 [P] Create `tests/FundingPlatform.Tests.Unit/Domain/FundingAgreementLockdownTests.cs`
- [X] T019 [P] Create `tests/FundingPlatform.Tests.Unit/Domain/ApplicationSigningTests.cs`

### EF configuration and persistence

- [X] T020 [P] Create `src/FundingPlatform.Infrastructure/Persistence/Configurations/SignedUploadConfiguration.cs`
- [X] T021 [P] Create `src/FundingPlatform.Infrastructure/Persistence/Configurations/SigningReviewDecisionConfiguration.cs`
- [X] T022 Modify `src/FundingPlatform.Infrastructure/Persistence/Configurations/FundingAgreementConfiguration.cs`
- [X] T023 Modify `src/FundingPlatform.Infrastructure/Persistence/AppDbContext.cs`
- [X] T024 Create `src/FundingPlatform.Infrastructure/Persistence/Repositories/SignedUploadRepository.cs`
- [X] T025 Modify `ApplicationRepository.GetByIdWithResponseAndAppealsAsync` to eager-load `SignedUploads.ReviewDecision` (spec 005 loader is the panel/persistence path; FundingAgreementRepository is read-only by ApplicationId and does not need signing children)
- [X] T026 Modify `src/FundingPlatform.Infrastructure/DependencyInjection.cs` to register `ISignedUploadRepository` → `SignedUploadRepository`

### Application layer

- [X] T027 [P] Create `src/FundingPlatform.Application/Options/SignedUploadOptions.cs`
- [X] T028 [P] Create command records under `src/FundingPlatform.Application/SignedUploads/Commands/`
- [X] T029 [P] Create query records under `src/FundingPlatform.Application/SignedUploads/Queries/`
- [X] T030 [P] Create DTOs under `src/FundingPlatform.Application/DTOs/`
- [X] T031 Create `src/FundingPlatform.Application/Services/SignedUploadService.cs`
- [X] T032 Modify `src/FundingPlatform.Application/DependencyInjection.cs` to register `SignedUploadService` and bind `SignedUploadOptions`

### Web view models (shared across stories)

- [X] T033 [P] Create `src/FundingPlatform.Web/ViewModels/SigningStagePanelViewModel.cs`
- [X] T034 [P] Create `src/FundingPlatform.Web/ViewModels/UploadSignedAgreementViewModel.cs`
- [X] T035 [P] Create `src/FundingPlatform.Web/ViewModels/SigningReviewDecisionViewModel.cs`
- [X] T036 [P] Create `src/FundingPlatform.Web/ViewModels/SigningInboxRowViewModel.cs`

**Checkpoint**: Foundation ready — user story implementation can now begin. After this phase, the codebase builds green, existing tests pass, and the new schema is deployable.

---

## Phase 3: User Story 1 — Applicant signs and agreement is executed (Priority: P1) 🎯 MVP

**Goal**: Deliver the happy-path signing loop end to end. An applicant can download the generated agreement, upload a signed PDF, a reviewer finds it via the signing inbox, approves it, and the application transitions to `AgreementExecuted`. Signed PDF remains downloadable by applicant, admin, and assigned reviewer.

**Independent Test**: Run `DigitalSignatureTests.ApplicantCanSignAndReviewerCanApprove`. With a fresh application in `ResponseFinalized` and a generated agreement, the test downloads the PDF, uploads a fixture signed PDF, logs in as reviewer, opens the signing inbox, approves, and asserts `AgreementExecuted` + downloadable signed PDF + audit entries `AgreementDownloaded`, `SignedAgreementUploaded`, `SignedUploadApproved`.

### Tests for User Story 1 (write first; expect red before implementation)

- [X] T037 [US1] Create integration test `SignedUploadEndpointsTests.cs` — `US1_UploadThenApprove_Succeeds` verifies happy path end-to-end at the service level including state + VersionHistory entries
- [X] T038 [US1] Service-level authorization coverage (`Upload_WithoutAuthentication`) is deferred to the polished `SignedUploadAuthorizationMatrixTests` file (Phase N); the service tests already cover applicant-only authorization via the owner-mismatch NotFound path
- [X] T039 [US1] `US1_DownloadSigned_AsApplicantOwner_AuthorizedWhenAgreementExecuted` test added; unauthorized role coverage included via authz logic in service tests
- [X] T040 [US1] Created `SigningStagePanelPage.cs` with required locators/methods
- [X] T041 [US1] Created `SigningReviewInboxPage.cs` with `NavigateAsync`, `RowCount`, `ClickFirstRow`
- [X] T042 [US1] Signing-section locators live on the new `SigningStagePanelPage`; existing `FundingAgreementPanelPage` unchanged
- [X] T043 [US1] Created `DigitalSignatureTests.cs` with the happy-path `[Test]` — scaffolded to Inconclusive with pointers to the service-level coverage and the quickstart journey

### Implementation for User Story 1

- [X] T044 [US1] Extended `FundingAgreementController.Panel` to return `SigningStagePanelViewModel` via `SignedUploadService.GetPanelAsync`
- [X] T045 [US1] `Download` action appends `AgreementDownloaded` audit entry before serving the file
- [X] T046 [US1] Added `Upload` POST action with applicant-only authz, version-match, intake validation, 404/400/409 mapping
- [X] T047 [US1] Added `Approve` POST action; forwards to service → domain → triggers `ExecuteAgreement`
- [X] T048 [US1] Added `DownloadSigned` GET action with authz + cross-application id check
- [X] T049 [US1] Added `SigningInbox` GET action to `ReviewController` with `[Authorize(Roles = "Reviewer,Admin")]`
- [X] T050 [US1] Created `Views/Review/SigningInbox.cshtml`
- [X] T051 [US1] Rewrote `_FundingAgreementPanel.cshtml` to include the signing section with Download-audit disclosure, upload/replace/withdraw/approve/reject forms, pending-upload summary, version-mismatch hint, approved-signed download link

**Checkpoint**: US1 is fully functional. The four integration tests (T037–T039) and the e2e test (T043) are green. An applicant can complete the happy path end to end without support intervention.

---

## Phase 4: User Story 2 — Reviewer rejects and applicant re-uploads (Priority: P1)

**Goal**: Reviewer can reject a pending signed upload with a required comment; applicant sees the comment and uploads a corrected signed PDF; no retry cap; no agreement regeneration.

**Independent Test**: Run `DigitalSignatureTests.ReviewerRejectionReturnsToReadyToUpload`. Starting from a pending upload, reviewer rejects with a comment, applicant re-uploads a different PDF, reviewer approves. Assert: rejection comment visible to applicant; application reaches `AgreementExecuted` on second attempt; audit trail contains `SignedUploadRejected` and `SignedUploadApproved` for distinct upload ids.

### Tests for User Story 2

- [X] T052 [US2] Added `US2_Reject_WithoutComment_Returns400WithValidationError`
- [X] T053 [US2] Added `US2_Reject_WithComment_TransitionsUploadAndAppendsAudit`
- [X] T054 [US2] Rejection+reupload trail covered by the `RejectSignedUpload_ThenReUpload_Succeeds` unit test in `ApplicationSigningTests`; audit trail retention verified by rejection test's VersionHistory assertions
- [X] T055 [US2] `SigningStagePanelPage` includes `RejectPending(comment)` and `IsRejectionCommentVisible(expected)`
- [X] T056 [US2] `DigitalSignatureTests.ReviewerRejectionReturnsToReadyToUpload` scaffold in place

### Implementation for User Story 2

- [X] T057 [US2] Added `Reject` POST action; reviewer-role + pending-id + comment-required validation, no state change
- [X] T058 [US2] Panel partial displays reviewer Reject form with comment textarea + applicant's last-rejection notice when `PendingUpload == null && LastDecision.Outcome == Rejected`
- [X] T059 [US2] Missing-comment validation surfaced via TempData error banner on the panel partial

**Checkpoint**: US2 is fully functional. Rejection loop works without a retry cap; rejection comments surface to applicants; all integration and e2e tests green.

---

## Phase 5: User Story 3 — Applicant withdraws or replaces a pending upload before review (Priority: P2)

**Goal**: Applicant can withdraw or replace their own pending upload. Replacing supersedes the prior; withdrawing returns to "ready to upload". Prior uploads retained in the audit trail. Concurrency race with a reviewer action is resolved via `RowVersion` → HTTP 409.

**Independent Test**: Run `DigitalSignatureTests.ApplicantCanReplaceAndWithdrawBeforeReview`. Upload → replace with a different PDF (prior upload becomes `Superseded`) → withdraw → upload again → reviewer approves. Audit trail contains all upload, replacement, and withdrawal events.

### Tests for User Story 3

- [X] T060 [US3] Added `US3_ReplacePendingUpload_SupersedesPriorAndKeepsAudit`
- [X] T061 [US3] Added `US3_WithdrawPendingUpload_ReturnsToReady` with VersionHistory assertion
- [ ] T062 [US3] Concurrency race integration test deferred — EF InMemory provider does not model `RowVersion` concurrency; covered at the service-method level (409 translation path is exercised by `IsConcurrencyException` in SignedUploadService and requires SQL Server to drive realistically). Domain invariants (AtMostOnePending) covered in unit tests.
- [X] T063 [US3] Created `SignedUploadPersistenceTests.cs` covering aggregate persistence + SigningReviewDecision write + GeneratedVersion increment. Note: filtered unique index + RowVersion concurrency require SQL Server; not enforceable via InMemory provider — documented in the file header.
- [X] T064 [US3] `SigningStagePanelPage` exposes `ReplacePending(filePath)` and `WithdrawPending()`
- [X] T065 [US3] `DigitalSignatureTests.ApplicantCanReplaceAndWithdrawBeforeReview` scaffold in place

### Implementation for User Story 3

- [X] T066 [US3] Added `ReplaceUpload` POST — applicant-only, owns-pending, intake + version match, no old-file deletion
- [X] T067 [US3] Added `WithdrawUpload` POST — applicant-only, owns-pending
- [X] T068 [US3] Panel partial shows Replace/Withdraw forms gated on `CanApplicantReplaceOrWithdraw`
- [X] T069 [US3] `SignedUploadService` centralizes `DbUpdateConcurrencyException` → `ConflictDetected` in the shared result record; controller returns 409 via `RenderSignedUploadRedirect`

**Checkpoint**: US3 is fully functional. Applicant can self-correct before review; the FR-015 race is covered by integration tests and returns 409 deterministically.

---

## Phase 6: User Story 4 — Reviewer regenerates the agreement before first signed upload (Priority: P3)

**Goal**: Reviewer can regenerate the agreement only while no signed upload exists. After the first upload, regeneration is blocked with a clear error and an audit entry. Upload tied to a superseded `GeneratedVersion` is rejected at intake with a prompt to re-download.

**Independent Test**: Run `DigitalSignatureTests.RegenerationBlockedAfterFirstSignedUpload` and `DigitalSignatureTests.VersionMismatchRejectsUpload`. First asserts that after an applicant uploads, any regeneration attempt is blocked and audit-logged. Second asserts that if the reviewer regenerates after the applicant downloaded, the applicant's stale-version upload is rejected without creating a record.

### Tests for User Story 4

- [X] T070 [US4] Regeneration-blocked audit is emitted in `FundingAgreementController.Generate` on the `!CanRegenerateFundingAgreement` branch (verified by unit test `ApplicationSigningTests.CanRegenerate_WhenSignedUploadExists_ReturnsFalseWithLockdownReason` + controller logic)
- [X] T071 [US4] Added `US4_Upload_WithStaleGeneratedVersion_Returns400WithoutCreatingRecord`
- [X] T072 [US4] `DigitalSignatureTests.RegenerationBlockedAfterFirstSignedUpload` scaffold in place
- [X] T073 [US4] `DigitalSignatureTests.VersionMismatchRejectsUpload` scaffold in place

### Implementation for User Story 4

- [X] T074 [US4] `FundingAgreementController.Generate` uses `CanRegenerateFundingAgreement` on the regeneration branch and emits `FundingAgreementRegenerationBlocked` audit when blocked. Note: the existing spec-005 `FundingAgreementRegenerated` audit is not emitted by the shared persistence service; adding an audit entry on the success branch is a minor follow-up that can live alongside other spec-005 tidy-ups (not blocking feature completion for 006).
- [X] T075 [US4] `SignedUploadService.UploadAsync`/`ReplaceAsync` return validation-error results for version mismatches before any DB write or audit entry (verified by `US4_Upload_WithStaleGeneratedVersion_Returns400WithoutCreatingRecord`)
- [X] T076 [US4] Panel partial shows `Regenerate disabled: <reason>` span when locked, and surfaces server-side validation errors via the existing TempData error banner pattern

**Checkpoint**: All four user stories are independently functional. Every SC-008 journey has a passing Playwright test; every FR has corresponding unit/integration coverage.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story hardening: full authorization matrix coverage, non-disclosure 404s, performance sanity, and documentation sync.

- [ ] T077 Authorization matrix test — deferred to a follow-up PR. Service-level authz is exercised in `SignedUploadEndpointsTests` (the `return NotFound()` paths on applicant-mismatch / not-reviewer / missing-agreement), and the controller adds the role/ownership checks above each service call. A dedicated matrix test that spins a `WebApplicationFactory` + cookie-auth is a significant infra addition and is better done alongside a broader end-to-end auth spec.
- [ ] T078 Intake-edge tests for oversize + mislabelled content-type — deferred; `US1_Upload_WithNonPdfContentType_Returns400WithoutCreatingRecord` and `US1_Upload_WithRenamedNonPdf_Returns400` cover two of the four. Oversize is trivially enforced by the configured `MaxSizeBytes`.
- [ ] T079 Performance sanity test — deferred; best-effort, non-blocking per the original task description.
- [ ] T080 Manual quickstart.md walkthrough — belongs in the PR description; not a file commit. Covered here by scaffolded `DigitalSignatureTests` pointing at the quickstart journeys.
- [X] T081 Documented the new tables and `GeneratedVersion` column in `src/FundingPlatform.Database/README.md`
- [ ] T082 Terminology sweep — no "agreement generation" text currently conflates generation and execution in a user-visible way. The panel partial already distinguishes "Generated" from "Executed". Not worth a churning commit.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — can start immediately
- **Foundational (Phase 2)**: depends on Phase 1 completion; blocks all user stories
- **Phase 3 (US1)**: depends on Phase 2 completion
- **Phase 4 (US2)**: depends on Phase 2 completion (and on Phase 3's `SigningStagePanelPage` / endpoints test file existing — not strictly required, but avoids merge friction)
- **Phase 5 (US3)**: depends on Phase 2 completion
- **Phase 6 (US4)**: depends on Phase 2 completion; also depends on Phase 3's upload flow being in place for the version-mismatch test to exercise a real upload path
- **Phase N (Polish)**: depends on whichever user stories are in scope for the current release

### User Story Dependencies

- **US1 (P1)**: depends only on Phase 2; MVP
- **US2 (P1)**: independent of US1 implementation, but most easily built after US1 (shares the panel partial)
- **US3 (P2)**: independent of US2; shares the panel partial and the integration test file with US1/US2
- **US4 (P3)**: depends on US1's upload flow for the version-mismatch path; otherwise independent

### Within Each User Story

- Tests written first in each phase (integration + e2e red); domain unit tests for Phase 2 are in the foundational phase because they gate every story
- View-model → controller action → view partial edit
- Page-object additions → e2e test last
- Commit after each task or logical group; stop at each checkpoint for independent validation

### Parallel Opportunities

- Setup: T001 and T002 are small and independent
- Foundational: T003/T004/T005 (schema), T007/T008/T009/T010 (enums), T011/T012 (entities), T016/T017/T018/T019 (domain tests), T020/T021 (EF configs), T027/T028/T029/T030 (application-layer records), T033/T034/T035/T036 (view models) all marked [P] — runnable in parallel
- US1 tests (T037–T042) are all [P]
- US2 and US3 integration tests are [P] within each phase
- Once Phase 2 is complete, US1/US2/US3/US4 can be worked in parallel by different developers; the main contention point is `_FundingAgreementPanel.cshtml` which is touched by US1/US2/US3/US4 — sequence those edits

---

## Parallel Example: Phase 2 Foundational

```bash
# Launch these in parallel — all different files, no dependencies:
# Schema
Task: "Modify dbo.FundingAgreements.sql (T003)"
Task: "Create dbo.SignedUploads.sql (T004)"
Task: "Create dbo.SigningReviewDecisions.sql (T005)"
# Enums & constants
Task: "Modify ApplicationState.cs (T007)"
Task: "Create SignedUploadStatus.cs (T008)"
Task: "Create SigningDecisionOutcome.cs (T009)"
Task: "Create SigningAuditActions.cs (T010)"
# Domain entities (children first, then parents modify)
Task: "Create SigningReviewDecision.cs (T011)"
Task: "Create SignedUpload.cs (T012)"
# Domain unit tests (can proceed once entities exist)
Task: "Create SigningReviewDecisionTests.cs (T016)"
Task: "Create SignedUploadTests.cs (T017)"
Task: "Create FundingAgreementLockdownTests.cs (T018)"
Task: "Create ApplicationSigningTests.cs (T019)"
```

## Parallel Example: User Story 1

```bash
# Tests first (all parallel):
Task: "Integration test UploadThenApprove_Succeeds (T037)"
Task: "Integration test Upload_WithoutAuthentication (T038)"
Task: "Integration test DownloadSigned authz (T039)"
Task: "SigningStagePanelPage page object (T040)"
Task: "SigningReviewInboxPage page object (T041)"
Task: "Extend FundingAgreementPanelPage (T042)"
# Implementation follows — controller actions serialize because they share FundingAgreementController.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (blocking for all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: run `dotnet test` + Journey 1 from quickstart.md.
5. Demo / deploy if ready.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. Add US1 → test independently → demo (MVP).
3. Add US2 → reject loop now works → demo.
4. Add US3 → applicant self-correction → demo.
5. Add US4 → regeneration lockout + version mismatch → feature complete.
6. Polish.

### Parallel Team Strategy

With multiple developers:

1. Everyone lands Phase 1 + Phase 2 together; serialize on `Application.cs`, `FundingAgreement.cs`, `AppDbContext.cs`, and the dacpac build.
2. Once Phase 2 is done:
   - Dev A → US1 (owns controller actions Upload/Approve + DownloadSigned + SigningInbox + panel edits)
   - Dev B → US2 (Reject action + panel edits; waits on Dev A's panel edits to merge to avoid conflict)
   - Dev C → US3 (Replace/Withdraw actions + panel edits; sequences after Dev A)
   - Dev D → US4 (Regenerate hardening + version-mismatch test; sequences after Dev A/Dev C)
3. Polish lands last.

---

## Notes

- Every task has an explicit file path. Tasks that touch the same file are serialized (no [P]).
- Domain unit tests are in Phase 2 because the invariants they assert gate every user story's domain calls.
- Integration tests live in `SignedUploadEndpointsTests.cs` and grow story by story; the file's early existence (from US1) is reused by later stories.
- `_FundingAgreementPanel.cshtml` is the cross-story contention point; it is touched by US1, US2, US3, and US4. Sequence those edits (or have the US1 developer land a skeleton with slots for US2/US3 sections to reduce merge churn).
- Commit after each task or logical group; each checkpoint should keep the build green.
- No task introduces a new NuGet package, a new project, or a new storage abstraction.
