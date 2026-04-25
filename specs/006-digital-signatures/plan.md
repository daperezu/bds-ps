# Implementation Plan: Digital Signatures for Funding Agreement

**Branch**: `006-digital-signatures` | **Date**: 2026-04-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/006-digital-signatures/spec.md`

## Summary

Extend the existing `FundingAgreement` aggregate (spec 005) with an **out-of-band signing workflow**: the applicant downloads the generated PDF, signs it externally, uploads the signed counterpart, and a reviewer visually verifies and approves or rejects it. Approval transitions the application to a new terminal `AgreementExecuted` state; rejection returns the applicant to "ready to upload" with a required reviewer comment. Regeneration of the underlying agreement is permitted until the first signed upload is accepted, then locked. No in-app signature capture, no e-signature provider, no cryptographic verification, no signing deadlines.

This plan is **purely additive** at every layer. Two new domain entities (`SignedUpload`, `SigningReviewDecision`) are children of `FundingAgreement`; the existing `VersionHistory` entity serves as the audit trail (no new audit aggregate). Two new SQL tables plus one column on `FundingAgreements`. No new NuGet packages, no new projects, no new infrastructure abstractions. Reviewer inbox surface is added on the existing `ReviewController`. The e2e tooling established in spec 001 (Playwright + NUnit) covers the four critical signing journeys mandated by SC-008.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. No new dependencies introduced by this feature.
**Storage**: SQL Server (Aspire-managed container for dev, dacpac schema management); local file system for signed-PDF bytes via the existing `IFileStorageService`. No new storage subsystems.
**Testing**: Playwright for .NET (NUnit) for E2E; `WebApplicationFactory`-based integration tests for controller/authorization/concurrency edges; NUnit for unit tests; NSubstitute for mocking. Same stack as specs 001–005.
**Target Platform**: Linux/Windows server (Aspire-orchestrated); production container image is Linux-based, matching spec 005.
**Project Type**: Web application (server-side MVC, no SPA).
**Performance Goals**: Upload-intake latency ≤ 2 s at p95 for a 20 MB upload (dominated by local disk IO); reviewer action latency ≤ 500 ms at p95; panel render on the application page ≤ 250 ms at p95 even with the signing section expanded (no regression of the existing page).
**Constraints**: All state and lifecycle rules on `FundingAgreement` / `SignedUpload` / `SigningReviewDecision` expressed as domain methods (no external state manipulation); optimistic concurrency via `RowVersion` on `SignedUpload` to resolve reviewer-vs-applicant races (FR-015); authenticated endpoints for all file delivery; non-disclosing 404 responses on authorization failure; no deadlines, no timers, no background jobs; configurable upload size with a sensible default (20 MB).
**Scale/Scope**: One `FundingAgreement` per application; 0–N `SignedUpload`s per agreement (most applications resolve in 1–3 uploads); expected peak signing-stage activity is a handful of uploads and decisions per minute across the whole platform — well within synchronous request-scoped processing.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | New entities (`SignedUpload`, `SigningReviewDecision`) in Domain; new use cases (`UploadSignedAgreement`, `WithdrawSignedUpload`, `ReplaceSignedUpload`, `ApproveSignedUpload`, `RejectSignedUpload`) in Application; `SignedUploadRepository`, new EF configurations in Infrastructure; new actions on `FundingAgreementController` and `ReviewController` + view partials in Web. Dependencies point inward; no new cross-layer leaks. |
| II. Rich Domain Model | PASS | All state transitions, invariants, and collection management for signing live on `FundingAgreement`, `SignedUpload`, `SigningReviewDecision`, and `Application`. Application-layer services are thin orchestrators (storage + DbContext + audit). The "Agreement Lockdown Flag" is a derived property (`IsLocked`) on `FundingAgreement`. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | Four user stories each have Playwright tests in a new `DigitalSignatureTests.cs`. Two new Page Objects (`SigningStagePanelPage`, `SigningReviewInboxPage`) plus extensions to `FundingAgreementPanelPage`. Authorization and concurrency edges are covered by integration tests using `WebApplicationFactory`. Each story is independently runnable. |
| IV. Schema-First Database Management | PASS | Two new `.sql` files in `src/FundingPlatform.Database/Tables/` (`dbo.SignedUploads.sql`, `dbo.SigningReviewDecisions.sql`). One ALTER-equivalent `.sql` update for `dbo.FundingAgreements.sql` to add `GeneratedVersion INT NOT NULL DEFAULT 1`. EF configuration maps to the schema; no EF migrations, no `EnsureCreated`. |
| V. Specification-Driven Development | PASS | Spec SOUND (REVIEW-SPEC.md), research.md resolves all open questions, data-model.md and contracts/README.md flow from the spec. Tasks will be generated next by `/speckit-tasks`. |
| VI. Simplicity and Progressive Complexity | PASS | Reuses `VersionHistory` for audit (no new aggregate). Reuses `IFileStorageService` (no new storage abstraction). No background queue, no notifications, no new roles, no new NuGet packages. Derived lockdown state instead of a stored flag. Deferred side-by-side reviewer UX and execution-banner cover page (documented in research.md as R-011, R-012). |

## Project Structure

### Documentation (this feature)

```text
specs/006-digital-signatures/
├── spec.md                     # Stakeholder-facing specification (SOUND)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — all open questions resolved
├── data-model.md               # Phase 1 output — entities, enums, state machine, schema
├── quickstart.md               # Phase 1 output — four manual journeys mirroring SC-008
├── contracts/
│   └── README.md               # MVC route contracts (Upload, Replace, Withdraw, Approve, Reject, DownloadSigned, SigningInbox) + authz matrix
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── review_brief.md             # Reviewer-facing guide
├── REVIEW-SPEC.md              # Formal spec soundness review
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
├── FundingPlatform.Domain/
│   ├── Entities/
│   │   ├── Application.cs                                          # MODIFY: CanRegenerateFundingAgreement, CanUserReviewSignedUpload, ExecuteAgreement, SubmitSignedUpload/Replace/Withdraw/Approve/Reject facades
│   │   ├── FundingAgreement.cs                                     # MODIFY: +GeneratedVersion, +_signedUploads, +IsLocked, +PendingUpload, +AcceptSignedUpload, +ReplacePendingUpload, +WithdrawPendingUpload, +ApprovePendingUpload, +RejectPendingUpload; Replace() now throws if IsLocked
│   │   ├── SignedUpload.cs                                         # NEW: aggregate child of FundingAgreement, holds status + RowVersion + nullable ReviewDecision
│   │   ├── SigningReviewDecision.cs                                # NEW: aggregate child of SignedUpload, holds outcome + reviewer + comment
│   │   └── SigningAuditActions.cs                                  # NEW: static action-string constants used when constructing VersionHistory entries for signing events
│   ├── Enums/
│   │   ├── ApplicationState.cs                                     # MODIFY: add AgreementExecuted = 6
│   │   ├── SignedUploadStatus.cs                                   # NEW: Pending, Superseded, Withdrawn, Rejected, Approved
│   │   └── SigningDecisionOutcome.cs                               # NEW: Approved, Rejected
│   └── Interfaces/
│       └── ISignedUploadRepository.cs                              # NEW: read projections for controller inbox + by-id lookups
├── FundingPlatform.Application/
│   ├── SignedUploads/
│   │   ├── Commands/
│   │   │   ├── UploadSignedAgreementCommand.cs                     # NEW: record(ApplicationId, UserId, GeneratedVersion, FileName, ContentType, Size, Stream)
│   │   │   ├── ReplaceSignedUploadCommand.cs                       # NEW: record(ApplicationId, UserId, GeneratedVersion, FileName, ContentType, Size, Stream)
│   │   │   ├── WithdrawSignedUploadCommand.cs                      # NEW: record(ApplicationId, UserId, SignedUploadId)
│   │   │   ├── ApproveSignedUploadCommand.cs                       # NEW: record(ApplicationId, ReviewerUserId, IsAdministrator, IsReviewerAssigned, SignedUploadId, Comment?)
│   │   │   └── RejectSignedUploadCommand.cs                        # NEW: record(ApplicationId, ReviewerUserId, IsAdministrator, IsReviewerAssigned, SignedUploadId, Comment)
│   │   └── Queries/
│   │       ├── GetSigningStagePanelQuery.cs                        # NEW: record(ApplicationId, UserId?, IsAdministrator, IsReviewerAssigned) — superset of GetFundingAgreementPanelQuery
│   │       ├── GetSignedAgreementDownloadQuery.cs                  # NEW: record(ApplicationId, SignedUploadId, UserId?, IsAdministrator, IsReviewerAssigned)
│   │       └── GetSigningInboxQuery.cs                             # NEW: record(CurrentUserId, IsAdministrator, Page, PageSize)
│   ├── Services/
│   │   └── SignedUploadService.cs                                  # NEW: orchestrates upload/replace/withdraw/approve/reject; owns IFileStorageService interaction and VersionHistory appends; 409 translation on DbUpdateConcurrencyException
│   ├── DTOs/
│   │   ├── SigningStagePanelDto.cs                                 # NEW: superset of FundingAgreementPanelDto with pending/last-decision summary + CanApplicantUpload / CanApplicantReplaceOrWithdraw / CanReviewerAct / CanRegenerate
│   │   ├── SignedUploadSummaryDto.cs                               # NEW: id, uploadedAtUtc, uploader display name, file name, size, generatedVersionAtUpload, status
│   │   ├── SigningReviewDecisionDto.cs                             # NEW: outcome, comment, decidedAtUtc, reviewer display name
│   │   └── SigningInboxRowDto.cs                                   # NEW: application id, applicant name, signed upload id, uploaded-at, versionMatchesCurrent
│   ├── Interfaces/
│   │   └── (no new interfaces — reuses IFileStorageService)
│   ├── Options/
│   │   └── SignedUploadOptions.cs                                  # NEW: MaxSizeBytes default 20 * 1024 * 1024, bound to SignedUpload config section
│   └── DependencyInjection.cs                                      # MODIFY: register SignedUploadService + SignedUploadOptions
├── FundingPlatform.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── FundingAgreementConfiguration.cs                    # MODIFY: map GeneratedVersion, SignedUploads navigation + cascade delete, backing-field map for _signedUploads
│   │   │   ├── SignedUploadConfiguration.cs                        # NEW: table mapping, RowVersion as concurrency token, enum-to-int on Status, backing-field for _reviewDecision
│   │   │   └── SigningReviewDecisionConfiguration.cs               # NEW: table mapping, enum-to-int on Outcome
│   │   ├── Repositories/
│   │   │   ├── SignedUploadRepository.cs                           # NEW: implements ISignedUploadRepository (by-id with parent, signing-inbox projections)
│   │   │   └── FundingAgreementRepository.cs                       # MODIFY: eager-load SignedUploads (+ReviewDecisions) for regeneration and panel reads
│   │   └── AppDbContext.cs                                         # MODIFY: DbSet<SignedUpload>, DbSet<SigningReviewDecision>, backing-field registration on FundingAgreement for _signedUploads and on SignedUpload for _reviewDecision
│   └── DependencyInjection.cs                                      # MODIFY: register ISignedUploadRepository → SignedUploadRepository
├── FundingPlatform.Web/
│   ├── Controllers/
│   │   ├── FundingAgreementController.cs                           # MODIFY: extend Panel action response, add Upload, ReplaceUpload, WithdrawUpload, Approve, Reject, DownloadSigned actions; add audit call to Download
│   │   └── ReviewController.cs                                     # MODIFY or NEW: add SigningInbox action
│   ├── ViewModels/
│   │   ├── SigningStagePanelViewModel.cs                           # NEW: composed from SigningStagePanelDto for the partial
│   │   ├── UploadSignedAgreementViewModel.cs                       # NEW: GeneratedVersion (hidden) + File (IFormFile)
│   │   ├── SigningReviewDecisionViewModel.cs                       # NEW: SignedUploadId + Comment (required on reject path)
│   │   └── SigningInboxRowViewModel.cs                             # NEW: for the reviewer inbox list
│   ├── Views/
│   │   ├── Applications/
│   │   │   └── _FundingAgreementPanel.cshtml                       # MODIFY: add signing section (pending-upload summary, upload/replace/withdraw/approve/reject forms, last-decision notice, version-match hint)
│   │   └── Review/
│   │       └── SigningInbox.cshtml                                 # NEW: reviewer inbox listing applications with pending signed uploads
│   └── appsettings.json                                            # MODIFY: add SignedUpload:MaxSizeBytes (default 20971520)
├── FundingPlatform.AppHost/
│   └── AppHost.cs                                                  # MODIFY: surface SignedUpload configuration to the Web project (if AppHost owns config propagation)
└── FundingPlatform.Database/
    └── Tables/
        ├── dbo.FundingAgreements.sql                               # MODIFY: add GeneratedVersion INT NOT NULL DEFAULT 1 + CK_..._Positive
        ├── dbo.SignedUploads.sql                                   # NEW: DDL per data-model.md §10.2, including filtered unique index UX_SignedUploads_OnePending_PerAgreement
        └── dbo.SigningReviewDecisions.sql                          # NEW: DDL per data-model.md §10.3

tests/
├── FundingPlatform.Tests.Unit/
│   ├── Domain/
│   │   ├── SignedUploadTests.cs                                    # NEW: status transitions, terminal immutability, construction invariants
│   │   ├── SigningReviewDecisionTests.cs                           # NEW: comment-required-on-reject invariant, ctor validation
│   │   ├── FundingAgreementLockdownTests.cs                        # NEW: IsLocked derivation, one-pending-at-a-time, AcceptSignedUpload version-mismatch throws, Replace throws when locked
│   │   └── ApplicationSigningTests.cs                              # NEW: CanRegenerateFundingAgreement composition, ExecuteAgreement transitions state, state-gate on signing facades
├── FundingPlatform.Tests.Integration/
│   ├── Persistence/
│   │   └── SignedUploadPersistenceTests.cs                         # NEW: filtered unique index enforces one pending, cascade deletes, row-version concurrency on SignedUpload
│   └── Web/
│       └── SignedUploadEndpointsTests.cs                           # NEW: authorization matrix (applicant vs admin vs reviewer vs unassigned reviewer), non-disclosure 404s, concurrency race (reviewer approve during applicant replace) → 409, oversize + non-PDF rejected at intake without record creation
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── FundingAgreementPanelPage.cs                            # MODIFY: locators for signing-stage section (upload field, withdraw/replace buttons, approve/reject buttons, pending-upload banner, last-decision banner)
    │   ├── SigningStagePanelPage.cs                                # NEW: high-level interactions (uploadSigned, withdrawPending, replacePending, approvePending, rejectPending)
    │   └── SigningReviewInboxPage.cs                               # NEW: navigate to /Review/SigningInbox, assert row counts, click through
    └── Tests/
        └── DigitalSignatureTests.cs                                # NEW: tests mapped 1:1 to SC-008 (happy path, rejection loop, pre-review replace/withdraw, regeneration lockout + version-mismatch)
```

**Structure Decision**: No new projects. No new NuGet packages. All changes slot into the Clean Architecture layout established by spec 001 and used by specs 002–005. Two new entities + two new tables are the whole persistence change; everything else is behavior layered onto existing types. The only controller action added to an unrelated controller is `SigningInbox` on `ReviewController`, because the signing queue is conceptually part of the reviewer's workflow. All signing-stage state transitions are domain methods on `FundingAgreement` and `Application`; application-layer services are thin.

## Constitution Re-Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | Domain dependency-free (new entities have no EF attributes, no MVC imports); Application free of storage/MVC concerns (services orchestrate `IFileStorageService` + domain + repositories); Infrastructure holds EF configs, repos, and the concrete file-storage service reused from spec 005; Web holds controllers, view models, partial views. Inward dependency flow preserved. |
| II. Rich Domain Model | PASS | Every invariant in `data-model.md` §12 is enforced in a domain method (or by a CHECK constraint / unique index as defence-in-depth). Application-layer services cannot reach past the `FundingAgreement` root to mutate a `SignedUpload` — all mutations go through `Application.*SignedUpload` facades. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | `DigitalSignatureTests.cs` contains one `[Test]` per SC-008 journey (happy path, rejection loop, pre-review replace+withdraw, regeneration lockout + version mismatch). Page-object methods align 1:1 with the quickstart journeys. Awkward-to-E2E cases (concurrency race, oversize/non-PDF intake without audit creation) are covered in `SignedUploadEndpointsTests` against the in-memory web host. |
| IV. Schema-First Database Management | PASS | `dbo.SignedUploads.sql` and `dbo.SigningReviewDecisions.sql` are the source of truth for their tables; `dbo.FundingAgreements.sql` updates to add `GeneratedVersion`. EF configurations map into these tables; `RowVersion` on `SignedUpload` is a `ROWVERSION` column. Filtered unique index `UX_SignedUploads_OnePending_PerAgreement` enforces the one-pending invariant at the DB level. No migrations, no `EnsureCreated`. |
| V. Specification-Driven Development | PASS | All FRs (FR-001–FR-015) and SCs (SC-001–SC-008) map to artifacts in this plan (see `data-model.md` §12 and `contracts/README.md` §§1–2). All open questions from `spec.md` are resolved in `research.md` (R-011, R-012, R-013 address the three named opens; R-001–R-010 resolve exploration-surfaced questions; R-014, R-015 confirm the spec's explicit non-decisions). |
| VI. Simplicity and Progressive Complexity | PASS | Two entities, two tables, one column added, seven new controller actions. No background jobs, no new packages, no new abstractions. Reuses `VersionHistory`, `IFileStorageService`, reviewer-role, and the existing role-assignment model. Derived lockdown over a stored flag. Deferred side-by-side UX and execution-banner features documented in `research.md` and `spec.md` Out of Scope. |

## Complexity Tracking

No violations. All complexity is either justified by an existing constitution principle or explicitly tracked below.

| Decision | Justification |
|----------|---------------|
| `SigningReviewDecision` is a separate entity rather than nullable fields on `SignedUpload` | Keeps `SignedUpload.Status` machine clean; enables a single CHECK constraint `Outcome <> 1 OR Comment IS NOT NULL` on the decision row; matches the spec's explicit Key Entities split. |
| Filtered unique index `UX_SignedUploads_OnePending_PerAgreement` in addition to the domain check | Defence in depth at the persistence layer: protects against any path (including ops hotfixes) that bypasses the domain mutator; prevents silent data corruption of the one-pending invariant. |
| New `CanRegenerateFundingAgreement` method instead of inlining the lockdown check into `RegenerateFundingAgreement` | Composes cleanly with the existing `CanGenerateFundingAgreement`; makes the lockdown predicate explicitly testable; used by the panel DTO to drive UI enablement without duplicating the rule. |
| `GeneratedVersion` integer on `FundingAgreement` rather than reusing `RowVersion` or `GeneratedAtUtc` | `RowVersion` changes for unrelated updates; `GeneratedAtUtc` has tick-precision serialization hazards. Integer monotonic counter is unambiguous and trivially persists round-trip. |
| Version-mismatch rejection at upload intake rather than after intake + reviewer review | FR-011 explicitly requires it; catches an otherwise-confusing downstream failure where the reviewer has to manually spot a stale signature. |
| Audit via `VersionHistory` rather than a new `SigningAuditEntry` aggregate | Reuses existing infrastructure; centralizes audit on `Application`; avoids a parallel stream of audit records that reviewers would have to reconcile. |
| `DownloadSigned` exposes all uploads (including superseded/rejected) to authorized roles, not just the approved one | Supports operational troubleshooting ("show me what they actually uploaded on attempt #2") without adding a separate admin path. Authorization is still scoped to applicant-owner / admin / assigned-reviewer. |

## Out-of-Plan Notes

- **Administrative back-out of a locked or executed agreement.** Explicitly out of scope per spec; `quickstart.md` documents a manual-SQL fallback until a dedicated admin-tooling feature ships. Nothing in this plan blocks that future feature.
- **Notifications.** The applicant discovers state by visiting the application page; no email / SMS / push. Hookable later at the service level in `SignedUploadService` without schema changes.
- **Side-by-side reviewer comparison UX and execution-banner cover page.** Both deferred per R-011, R-012; revisitable UX decisions with no schema impact.
- **Reporting / operational visibility on stuck signing stages.** Spec 004 already flagged this for a future reporting spec; this feature's audit trail provides the raw data that future reporting will query.
- **Stored `IsLocked` flag for high-volume reads.** Current design derives `IsLocked` from the `SignedUploads` collection count. If future reporting needs to page across thousands of applications without loading child collections, a denormalized boolean can be added to `FundingAgreements` without breaking domain semantics.
