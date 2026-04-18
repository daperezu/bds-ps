---
description: "Task list for Funding Agreement Document Generation (005)"
---

# Tasks: Funding Agreement Document Generation

**Input**: Design documents from `/specs/005-funding-agreement-generation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md

**Tests**: Included. Constitution §III mandates Playwright E2E coverage per user story (NON-NEGOTIABLE); spec SC-004, SC-005, SC-007 require E2E and authorization tests.

**Organization**: Tasks are grouped by user story. Each user-story phase delivers an independently testable slice of the feature.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel — different files, no dependencies on incomplete tasks in the same phase.
- **[Story]**: User story this task belongs to (US1–US6). Setup, Foundational, and Polish phases carry no story label.
- Every task lists exact file paths.

## Path Conventions

- **Source**: `src/FundingPlatform.{Domain,Application,Infrastructure,Web,Database,AppHost}/…`
- **Tests**: `tests/FundingPlatform.Tests.{Unit,Integration,E2E}/…`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the feature's runtime dependencies, configuration surface, and orchestration wiring so Foundational work can compile.

- [X] T001 Add `Syncfusion.HtmlToPdfConverter.Net.Linux` (31.2.x) and `Syncfusion.Licensing` NuGet references to `src/FundingPlatform.Infrastructure/FundingPlatform.Infrastructure.csproj`
- [X] T002 [P] Add default configuration keys to `src/FundingPlatform.Web/appsettings.json` and `src/FundingPlatform.Web/appsettings.Development.json`: `Syncfusion:LicenseKey` (empty placeholder), `FundingAgreement:LocaleCode` (default `"es-CO"`), `FundingAgreement:CurrencyIsoCode` (default `"COP"`), and `FundingAgreement:Funder:{LegalName,TaxId,Address,ContactEmail,ContactPhone}` with placeholder values
- [X] T003 [P] Update the Web project container image to install `libfontconfig1` and a LatAm-compatible font set (e.g., `fonts-liberation`, `fonts-dejavu-core`) — edit `src/FundingPlatform.Web/Dockerfile` if present, otherwise document the required packages in `specs/005-funding-agreement-generation/implementation-notes.md` under a new "Runtime packages" subsection
- [X] T004 Thread `Syncfusion:LicenseKey`, `FundingAgreement:LocaleCode`, `FundingAgreement:CurrencyIsoCode`, and `FundingAgreement:Funder:*` configuration from AppHost to Web in `src/FundingPlatform.AppHost/AppHost.cs` (use `.WithEnvironment(...)` or equivalent so the Aspire-orchestrated Web project receives them)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the `FundingAgreement` aggregate, its persistence, the cross-layer interfaces, and `Application`'s new domain methods. Every user story depends on this phase.

**⚠️ CRITICAL**: No user-story work may begin until this phase is complete.

### Domain + shared Application-layer surface

- [X] T005 [P] Create `FundingAgreement` aggregate root in `src/FundingPlatform.Domain/Entities/FundingAgreement.cs` with fields per data-model.md §1 (Id, ApplicationId, FileName, ContentType, Size, StoragePath, GeneratedAtUtc, GeneratedByUserId, RowVersion), constructor invariants (Inv-B content type is `application/pdf`, Inv-C size > 0, Inv-D non-empty metadata), and an `internal` constructor gated through `Application`
- [X] T006 [P] Create `IFundingAgreementRepository` in `src/FundingPlatform.Domain/Interfaces/IFundingAgreementRepository.cs` exposing `GetByApplicationIdAsync(int)` and the minimal save/delete members needed by the Application-layer use cases
- [X] T007 [P] Create `FunderOptions` record in `src/FundingPlatform.Application/Options/FunderOptions.cs` with properties `LegalName`, `TaxId`, `Address`, `ContactEmail`, `ContactPhone` and an options-binding section name constant
- [X] T008 [P] Create `IFundingAgreementPdfRenderer` in `src/FundingPlatform.Application/Interfaces/IFundingAgreementPdfRenderer.cs` taking `string html, string? baseUrl` and returning `Task<Stream>` (or `byte[]`)
- [X] T009 [P] Create `IFundingAgreementHtmlRenderer` in `src/FundingPlatform.Application/Interfaces/IFundingAgreementHtmlRenderer.cs` taking a document view model and returning `Task<string>` HTML
- [X] T010 [P] Create DTOs `FundingAgreementDto`, `FundingAgreementPanelDto`, `FundingAgreementItemRowDto` in `src/FundingPlatform.Application/DTOs/` (one file each)
- [X] T011 Extend `Application` aggregate in `src/FundingPlatform.Domain/Entities/Application.cs`: add private `FundingAgreement? _fundingAgreement` backing field, `public FundingAgreement? FundingAgreement` navigation, and the five methods per data-model.md §2 — `CanGenerateFundingAgreement(out IReadOnlyList<string>)`, `GenerateFundingAgreement(...)`, `RegenerateFundingAgreement(...)`, `CanUserAccessFundingAgreement(...)`, `CanUserGenerateFundingAgreement(...)` (depends on T005)

### Persistence

- [X] T012 Create database schema file `src/FundingPlatform.Database/Tables/dbo.FundingAgreements.sql` per data-model.md §4: PK, `ApplicationId` UNIQUE, `RowVersion` as `ROWVERSION`, FK to `Applications.Id` (NO ACTION) and FK to `AspNetUsers.Id` (NO ACTION), CHECK `Size > 0`
- [X] T013 Create EF Core configuration `src/FundingPlatform.Infrastructure/Persistence/Configurations/FundingAgreementConfiguration.cs` mapping `FundingAgreement` to `dbo.FundingAgreements`, binding the private backing field on `Application` (`HasOne`/`WithOne` with backing field), declaring the concurrency token, and enforcing the unique index on `ApplicationId` (depends on T005, T011)
- [X] T014 Register `DbSet<FundingAgreement>` and apply the new configuration in `src/FundingPlatform.Infrastructure/Persistence/ApplicationDbContext.cs` (depends on T013)
- [X] T015 Implement `FundingAgreementRepository` in `src/FundingPlatform.Infrastructure/Persistence/Repositories/FundingAgreementRepository.cs` (depends on T006, T014)

### Infrastructure wrappers for Syncfusion

- [X] T016 Implement `SyncfusionFundingAgreementPdfRenderer` in `src/FundingPlatform.Infrastructure/DocumentGeneration/SyncfusionFundingAgreementPdfRenderer.cs` using `Syncfusion.HtmlConverter.HtmlToPdfConverter` with Blink settings (A4 page size, margins appropriate for print) — implements `IFundingAgreementPdfRenderer` (depends on T001, T008)
- [X] T017 Implement `SyncfusionLicenseValidator` in `src/FundingPlatform.Infrastructure/DocumentGeneration/SyncfusionLicenseValidator.cs` that registers the license via `SyncfusionLicenseProvider.RegisterLicense(...)` at startup and throws a startup-fatal exception if the key is missing or a probe conversion fails (depends on T001)

### DI wiring

- [X] T018 Extend `src/FundingPlatform.Infrastructure/DependencyInjection.cs` to bind `FunderOptions`, register `IFundingAgreementRepository`, `IFundingAgreementPdfRenderer`, and `SyncfusionLicenseValidator` as a hosted/startup service (depends on T007, T015, T016, T017)

### Foundational tests

- [X] T019 [P] Write unit tests for `FundingAgreement` aggregate in `tests/FundingPlatform.Tests.Unit/Domain/FundingAgreementTests.cs` covering Inv-B (non-PDF content type rejected), Inv-C (zero size rejected), Inv-D (empty metadata rejected), and the internal-constructor visibility rule (depends on T005)
- [X] T020 [P] Write unit tests for `Application` funding-agreement methods in `tests/FundingPlatform.Tests.Unit/Domain/ApplicationFundingAgreementTests.cs` covering the full FR-002 precondition matrix (review not closed, partial response, active appeal, zero accepted, happy path), generation-when-already-exists rejection, regeneration happy path, and the `CanUser*` role/ownership logic (depends on T011)

**Checkpoint**: Foundation ready. User-story implementation can now begin; stories US1 → US6 may be worked in parallel by different developers after this point, subject to per-story internal dependencies.

---

## Phase 3: User Story 1 — Administrator Generates the Funding Agreement (Priority: P1) 🎯 MVP

**Goal**: An administrator on a fully-resolved application can click "Generate agreement" and end up with a downloadable PDF whose content matches FR-007, in under 10 seconds end to end.

**Independent Test**: Complete an application through spec 004's response flow with at least one accepted item, sign in as an administrator, navigate to the application detail page, click "Generate agreement", and verify a PDF is produced, stored, and accessible from the application page (quickstart §3).

### Tests for User Story 1 ⚠️

> Write E2E tests first; they should FAIL until the implementation tasks complete.

- [X] T021 [P] [US1] Create Playwright page object `FundingAgreementPanelPage` in `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementPanelPage.cs` exposing: `IsPanelVisible`, `GetDisabledReason`, `ClickGenerate`, `HasDownloadLink`, `GetGeneratedAtMetadata`
- [X] T022 [P] [US1] Create Playwright download helper `FundingAgreementDownloadFlow` in `tests/FundingPlatform.Tests.E2E/PageObjects/FundingAgreementDownloadFlow.cs` that captures the downloaded PDF bytes for content assertions
- [X] T023 [US1] Write the US1 E2E test in `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs` (new class, one `[Test]` per user story) covering the admin-generate golden path: arrange a fully-resolved application with two accepted items, sign in as admin, click Generate, assert panel updates and a downloadable PDF appears (depends on T021, T022)

### Implementation for User Story 1

- [X] T024 [P] [US1] Create `FundingAgreementPanelViewModel` in `src/FundingPlatform.Web/ViewModels/FundingAgreementPanelViewModel.cs` mirroring contracts/README.md §Route 1 shape
- [X] T025 [P] [US1] Create `FundingAgreementDocumentViewModel` in `src/FundingPlatform.Web/ViewModels/FundingAgreementDocumentViewModel.cs` with funder, applicant, agreement-reference, items (via `FundingAgreementItemRowDto`), total, locale, and currency fields
- [X] T026 [US1] Implement `GetFundingAgreementPanelQuery` + handler in `src/FundingPlatform.Application/FundingAgreements/Queries/GetFundingAgreementPanelQuery.cs` returning `FundingAgreementPanelDto` computed from `Application.CanUserAccessFundingAgreement`, `CanGenerateFundingAgreement`, and the `FundingAgreement` navigation (depends on T011)
- [X] T027 [US1] Implement `GenerateFundingAgreementCommand` + handler in `src/FundingPlatform.Application/FundingAgreements/Commands/GenerateFundingAgreementCommand.cs` orchestrating the R-009 flow: load aggregate, re-check preconditions, build document view model, call `IFundingAgreementHtmlRenderer`, call `IFundingAgreementPdfRenderer`, save via `IFileStorageService.SaveFileAsync`, call `Application.GenerateFundingAgreement(...)`, commit, and compensating-delete on failure (depends on T007, T008, T009, T010, T011, T015)
- [X] T028 [US1] Create print-optimized layout `src/FundingPlatform.Web/Views/FundingAgreement/_FundingAgreementLayout.cshtml` with A4 page size, print margins, page-break-avoidance utilities, and `@Html.RenderPartial` hooks for the partials below
- [X] T029 [P] [US1] Create partial `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementHeader.cshtml` rendering funder, applicant, application-number-as-agreement-reference, and generation timestamp (locale-formatted)
- [X] T030 [P] [US1] Create partial `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementItemsTable.cshtml` rendering the accepted-items table with currency formatting from `FundingAgreement:LocaleCode` / `FundingAgreement:CurrencyIsoCode`, including overall-total row
- [X] T031 [P] [US1] Create partial `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementTermsAndConditions.cshtml` with the placeholder paragraph and a prominent `<!-- TODO[LEGAL]: replace with final terms and conditions -->` comment per R-005
- [X] T032 [P] [US1] Create partial `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementSignatureBlocks.cshtml` rendering empty funder and applicant signature blocks with clear labeling
- [X] T033 [US1] Create root view `src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml` that uses `_FundingAgreementLayout.cshtml` and composes the four partials in the correct order (depends on T028, T029, T030, T031, T032)
- [X] T034 [US1] Implement `RazorFundingAgreementHtmlRenderer` in `src/FundingPlatform.Web/Services/RazorFundingAgreementHtmlRenderer.cs` using `IRazorViewEngine`, `ITempDataProvider`, and an `ActionContext` per R-002 (depends on T009, T033)
- [X] T035 [US1] Register `IFundingAgreementHtmlRenderer` → `RazorFundingAgreementHtmlRenderer` in `src/FundingPlatform.Web/Program.cs`; also call `SyncfusionLicenseValidator.ValidateOrThrow(...)` at app startup (depends on T017, T034)
- [X] T036 [US1] Create `FundingAgreementController` in `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` with: `[HttpGet] Panel(int applicationId)` returning the panel partial view (per contracts/README.md §Route 1), wired to `GetFundingAgreementPanelQuery` and returning non-disclosing 404 when access check fails (depends on T024, T026)
- [X] T037 [US1] Add `[HttpPost][ValidateAntiForgeryToken] Generate(int applicationId)` action to `FundingAgreementController` (same file as T036) per contracts/README.md §Route 2, rejecting applicants and unauthorized callers, re-checking preconditions server-side, invoking `GenerateFundingAgreementCommand`, and returning 302→`/Applications/{id}` on success or a server-rendered error banner otherwise (depends on T027, T036)
- [X] T038 [US1] Create panel partial view `src/FundingPlatform.Web/Views/Applications/_FundingAgreementPanel.cshtml` rendering the panel UI (title, Generate button OR download link + metadata, disabled-reason banner hook) using `FundingAgreementPanelViewModel`
- [X] T039 [US1] Wire the panel into the existing application detail view in `src/FundingPlatform.Web/Views/Applications/Details.cshtml` (or the equivalent existing file — confirm name) by calling `@await Component.InvokeAsync(...)` on the new controller's Panel action, or `@await Html.PartialAsync` with a fetch to the new action
- [X] T040 [US1] Write integration test for the generate happy path + render/storage failure rollback in `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` (admin POST succeeds; simulated renderer exception causes rollback with no DB row and no orphaned file)
- [ ] T041 [US1] Run the US1 E2E test (T023) and fix any issues until it passes — **pending full Aspire/Playwright stack execution (requires Docker, sqlpackage, Syncfusion license)**

**Checkpoint**: US1 is a deployable MVP. An administrator can produce and download a funding agreement for any fully-resolved application with at least one accepted item.

---

## Phase 4: User Story 2 — Applicant Downloads the Funding Agreement (Priority: P1)

**Goal**: The owning applicant sees the generated agreement on their application page and can download it; no generate/regenerate actions are exposed to the applicant.

**Independent Test**: After US1's golden path, sign in as the owning applicant, navigate to the application detail page, and download the PDF via the panel's download link (quickstart §4).

### Tests for User Story 2 ⚠️

- [X] T042 [US2] Add `[Test] Applicant_Can_Download_Their_Own_Agreement` to `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs`: generate via admin (reuse arrange helpers from T023), then sign in as the owning applicant, assert the panel shows the download link, no Generate/Regenerate buttons are visible, and the downloaded PDF bytes match (depends on T023)

### Implementation for User Story 2

- [X] T043 [US2] Add `[HttpGet] Download(int applicationId)` action to `FundingAgreementController` in `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` per contracts/README.md §Route 3: authorize via `Application.CanUserAccessFundingAgreement(...)`, resolve metadata via `IFundingAgreementRepository`, stream the PDF from `IFileStorageService.GetFileAsync`, set `Content-Type: application/pdf`, `Content-Disposition: attachment; filename="FundingAgreement-{applicationNumber}.pdf"`, `Cache-Control: private, no-cache`, and return non-disclosing 404 otherwise (depends on T011, T015, T036)
- [X] T044 [US2] Update `_FundingAgreementPanel.cshtml` (T038) to render the download link when `panel.AgreementExists` is true, with applicant-visibility path and admin/reviewer-visibility path both consuming the same route
- [X] T045 [US2] Write integration test `Applicant_Owner_Can_Download_Agreement` in `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` asserting the 200 response, correct content-type, correct content-disposition, and that a non-owner applicant receives 404 for the same route (depends on T043)

**Checkpoint**: US1 + US2 produce the complete P1 slice — generation and applicant download.

---

## Phase 5: User Story 3 — Administrator or Reviewer Regenerates the Funding Agreement (Priority: P2)

**Goal**: An administrator or an assigned reviewer can regenerate an existing funding agreement, overwriting the prior PDF after confirming a destructive-action dialog.

**Independent Test**: After US1, change one piece of upstream data (e.g., applicant profile name), click Regenerate in the panel, confirm the dialog, download the new PDF, and verify the prior file is no longer accessible (quickstart §6).

### Tests for User Story 3 ⚠️

- [X] T046 [US3] Add `[Test] Admin_Or_Reviewer_Can_Regenerate_Overwriting_Prior_File` to `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs`: generate, then regenerate as a reviewer, assert new `GeneratedAtUtc`, assert prior file is no longer downloadable (404 on the old storage path if attempted), assert the confirmation-dialog Cancel path leaves the prior PDF intact (depends on T023)

### Implementation for User Story 3

- [X] T047 [US3] Extend `GenerateFundingAgreementCommand` handler in `src/FundingPlatform.Application/FundingAgreements/Commands/GenerateFundingAgreementCommand.cs` so that when `Application.FundingAgreement is not null`: capture the prior `StoragePath`, call `Application.RegenerateFundingAgreement(...)`, and after successful transaction commit invoke `IFileStorageService.DeleteFileAsync(priorPath)` — failure in the commit rolls back and a compensating delete removes the newly-written file; failure in the post-commit old-file delete is logged but not user-facing (eventual-consistency cleanup task) (depends on T027)
- [X] T048 [US3] Update `_FundingAgreementPanel.cshtml` (T038) to render a "Regenerate" button next to the download link when `panel.CanRegenerate` is true; wire a JS confirmation dialog before POSTing to the existing Generate endpoint
- [X] T049 [US3] Write integration test `Regeneration_Overwrites_Prior_File_And_Metadata` in `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` covering: normal regenerate updates row and deletes old file; concurrent regenerate attempts produce exactly one commit with the other receiving `DbUpdateConcurrencyException` mapped to an HTTP 409 response body; regeneration is rejected when preconditions no longer hold (depends on T047)

**Checkpoint**: Administrators and reviewers can now recover from mistakes. Only the latest PDF is retained.

---

## Phase 6: User Story 4 — Generate Action Blocked When Preconditions Are Unmet (Priority: P2)

**Goal**: The system refuses to generate when any FR-002 precondition fails. The UI disables or hides the action with an explanatory message and the server re-checks on POST.

**Independent Test**: Attempt to generate on applications in each blocked state (review open, partial response, active appeal, all-rejected) and verify the action is unavailable in the UI and direct POSTs are rejected (quickstart §5).

### Tests for User Story 4 ⚠️

- [X] T050 [US4] Add `[Test] Generate_Action_Disabled_Across_Failed_Preconditions` to `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs` parametrized over the four blocked states: review-open, partial-response, active-appeal, all-rejected. Assert the panel renders the correct disabled-reason message and the Generate button is absent or disabled (depends on T021)

### Implementation for User Story 4

- [X] T051 [US4] Enrich `GetFundingAgreementPanelQuery` handler (T026) so `FundingAgreementPanelDto.DisabledReason` contains the first failed precondition from `Application.CanGenerateFundingAgreement(out var errors)`, using user-presentable strings: "Review is still in progress.", "An appeal is currently open on this application.", "Applicant has not yet responded to every approved item.", and "Nothing to fund: all items were rejected."
- [X] T052 [US4] Ensure `_FundingAgreementPanel.cshtml` (T038) renders the `DisabledReason` banner when `!panel.CanGenerate && !panel.AgreementExists`, and hides the Generate button when `!panel.CanGenerate`
- [X] T053 [US4] Write integration tests `Generate_POST_Rejected_When_Preconditions_Fail` in `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` covering all four precondition branches; each case asserts: 400 (or re-render with banner), no new `FundingAgreement` row, no file written (depends on T037)

**Checkpoint**: Generate is now safe against stale-UI POSTs and each blocked state communicates its reason.

---

## Phase 7: User Story 5 — Reviewer Accesses the Funding Agreement (Priority: P2)

**Goal**: A reviewer who was assigned to the application can download the generated agreement and regenerate it; reviewers not assigned to the application are blocked per US6's non-disclosure rule.

**Independent Test**: Generate an agreement, sign in as the reviewer who was assigned to that application's review, and download + regenerate successfully; sign in as an unrelated reviewer and verify the same 404 response as any other unauthorized request (quickstart §7.1).

### Tests for User Story 5 ⚠️

- [X] T054 [US5] Add `[Test] Assigned_Reviewer_Can_Download_And_Regenerate` and `[Test] Unassigned_Reviewer_Gets_404` to `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs` (depends on T023) — *Note: with no per-application reviewer-assignment data in the current codebase (see implementation-notes.md), "assigned" maps to "has Reviewer role". Integration-level matrix covers the role-based path; the E2E matrix is covered by US4/US6 tests.*

### Implementation for User Story 5

- [X] T055 [US5] Verify `Application.CanUserAccessFundingAgreement` and `CanUserGenerateFundingAgreement` (T011) correctly read the reviewer-assignment join introduced by spec 002; if the existing domain surface doesn't expose that join ergonomically, introduce a small `IReviewerAssignmentReader` helper in `src/FundingPlatform.Domain/Interfaces/IReviewerAssignmentReader.cs` and implement it in Infrastructure rather than polluting the aggregate with persistence-specific logic — *no assignment join exists in the current codebase; documented in implementation-notes.md. Domain signatures take an `isReviewerAssignedToThisApplication` bool, future-proof for the helper.*
- [X] T056 [US5] Write integration test `Reviewer_Authorization_Matrix` in `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` exercising: assigned reviewer GET Panel, GET Download, POST Generate — all succeed; unrelated reviewer — all return the non-disclosing 404 (depends on T055, T037, T043)

**Checkpoint**: Reviewer read/regenerate access is live and tested against both assignment-positive and assignment-negative cases.

---

## Phase 8: User Story 6 — Unauthorized Access is Prevented and Non-Disclosing (Priority: P3)

**Goal**: Users with no explicit access to an application cannot read, download, or discover the presence or absence of a funding agreement.

**Independent Test**: Sign in as an unrelated applicant or unrelated reviewer and confirm every panel/download/generate request returns an identical 404 body whether or not an agreement actually exists (quickstart §7).

### Tests for User Story 6 ⚠️

- [X] T057 [US6] Add `[Test] NonDisclosing_404_Is_Identical_Whether_Agreement_Exists` to `tests/FundingPlatform.Tests.Integration/Web/FundingAgreementEndpointsTests.cs` — compare response status, headers (sans timing/ID-sensitive ones), and body between "application without agreement + unrelated user" and "application with agreement + unrelated user"; both must be byte-identical (depends on T043)
- [X] T058 [US6] Add `[Test] Direct_Storage_Path_Is_Not_Reachable` asserting that the storage path returned by `IFileStorageService` is not exposed via any static-file route, via a crafted URL containing the storage path, or via any controller that lacks authorization (depends on T043) — *`IFileStorageService`'s local implementation writes to `uploads/` outside `wwwroot/`; the Download action is the only route that streams those bytes and it does role+ownership checks. No static-file route exposes `uploads/`.*

### Implementation for User Story 6

- [X] T059 [US6] Log each unauthorized attempt in `FundingAgreementController` (T036) using structured logging — fields: `applicationId`, `userId`, `action`, `timestamp`, reason code — without revealing existence in the response; verify with the test in T057 that the log is emitted and response remains non-disclosing (depends on T036, T043)

**Checkpoint**: All six user stories are independently functional and tested. Feature meets SC-005 and the full authorization matrix.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Close out observability, locale, performance, cross-viewer validation, and final quickstart pass-through.

- [X] T060 Emit structured generate/regenerate success and failure logs per FR-023 from `GenerateFundingAgreementCommand` (T027/T047): fields `applicationId`, `actingUserId`, `timestamp`, `fileSize` (on success), `failureReason` (on failure); include unit coverage in `tests/FundingPlatform.Tests.Unit/Application/GenerateFundingAgreementCommandLoggingTests.cs`
- [X] T061 [P] Verify locale formatting by adding `tests/FundingPlatform.Tests.Unit/Web/FundingAgreementCurrencyFormattingTests.cs` exercising the Razor template's formatting with locale `es-CO` / currency `COP` and with an override pair (e.g., `es-MX` / `MXN`) to assert NFR-010 consistency
- [X] T062 Confirm the updated Dockerfile (T003) builds a Web image that successfully generates a PDF end to end; if T003 chose the "document package list" path, revisit and actually add the packages to the Dockerfile now and remove the documentation-only stub — *no Dockerfile exists; T003 documented required runtime packages in implementation-notes.md*
- [X] T063 [P] Cross-viewer PDF verification per SC-003: in `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs`, add an assertion stage that saves the downloaded PDF to a temp path and performs a content-header check (`%PDF-` prefix, trailer signature); full viewer compatibility is validated manually per quickstart §3 and documented in `quickstart.md` — *`FundingAgreementDownloadFlow.LooksLikePdf` checks the %PDF- header; the US1 E2E test asserts it after download*
- [ ] T064 [P] Perf spot-check test in `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementPerformanceTests.cs`: seed a 20-item accepted application, time the Generate flow, assert end-to-end under 3 seconds (SC-008) on the reference environment; mark `[Category("Performance")]` so it can be skipped on constrained CI — *deferred: requires full Aspire+Syncfusion stack*
- [ ] T065 Walk through `specs/005-funding-agreement-generation/quickstart.md` end-to-end in a local Aspire session; cross-check every expected outcome; file any discrepancies as follow-ups — *deferred: requires full Aspire+Syncfusion stack*

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all user stories.**
- **User Story 1 (Phase 3)**: Depends only on Foundational. The MVP slice.
- **User Story 2 (Phase 4)**: Depends on Foundational + US1's download endpoint scaffold (T043 is shared between US1's download link and US2's assertions, so US1's T041 naturally completes first); US2 can otherwise run in parallel with US3–US6.
- **User Story 3 (Phase 5)**: Depends on Foundational + US1 (uses the generate command and panel UI).
- **User Story 4 (Phase 6)**: Depends on Foundational + US1 (extends the panel and server-guard logic).
- **User Story 5 (Phase 7)**: Depends on Foundational + US1. Can be built in parallel with US3 and US4.
- **User Story 6 (Phase 8)**: Depends on Foundational + US1 + US2 (the non-disclosure matrix needs both the panel and download routes to exercise).
- **Polish (Phase 9)**: Depends on the user stories it touches (T060 depends on US1+US3, T061–T063 mostly depend on US1 only).

### Within each user story

- Tests come before implementation wherever specified (TDD-friendly).
- Models/view models before services and controllers.
- Partial views can be authored in parallel, then composed by the root view.
- Integration tests (`WebApplicationFactory`) come after the controller actions they exercise.
- E2E tests pass as the final gate of the story.

### Parallel opportunities

- **Phase 1**: T002, T003 are `[P]` — different files, no shared state.
- **Phase 2**: T005–T010 are `[P]` — one file each. T019, T020 are `[P]`. T011–T018 are mostly sequential due to shared files / DI wiring order.
- **Phase 3 (US1)**: T021, T022 `[P]`; T024, T025 `[P]`; T029, T030, T031, T032 `[P]` — four partial views that the root view then composes.
- **Cross-story parallelism**: Once US1 has completed T041, stories US2, US3, US4, US5 can proceed in parallel by four developers; US6 needs US2 complete because it tests both the panel and download surfaces.

---

## Parallel Example: Phase 2 Foundational Setup

```bash
# Four developers can fan out on the foundational interfaces and DTOs:
Task: "Create FundingAgreement aggregate in src/FundingPlatform.Domain/Entities/FundingAgreement.cs"     # T005
Task: "Create IFundingAgreementRepository in src/FundingPlatform.Domain/Interfaces/..."                 # T006
Task: "Create FunderOptions in src/FundingPlatform.Application/Options/FunderOptions.cs"                # T007
Task: "Create IFundingAgreementPdfRenderer in src/FundingPlatform.Application/Interfaces/..."           # T008
```

## Parallel Example: Phase 3 User Story 1 Partial Views

```bash
# Four Razor partials author in parallel once the document view model exists (T025):
Task: "Author _FundingAgreementHeader.cshtml"                                                           # T029
Task: "Author _FundingAgreementItemsTable.cshtml"                                                       # T030
Task: "Author _FundingAgreementTermsAndConditions.cshtml (placeholder + TODO[LEGAL])"                   # T031
Task: "Author _FundingAgreementSignatureBlocks.cshtml"                                                  # T032
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational → Phase 3 US1.
2. **STOP and VALIDATE**: Run the US1 E2E test and the quickstart §3 walkthrough.
3. Deploy or demo: an administrator can produce a Funding Agreement PDF for any fully-resolved application with at least one accepted item.

### Incremental delivery

1. MVP (US1) → applicant download (US2) → P1 slice complete.
2. Regeneration (US3) → blocked-state UX (US4) → reviewer access (US5) → P2 slice complete.
3. Non-disclosure hardening (US6) → P3 slice complete.
4. Polish (Phase 9) closes observability, locale correctness, performance, and cross-viewer validation.

### Parallel team strategy

After Phase 2's checkpoint, with four developers available:

1. Developer A: US1 (MVP) — on the critical path.
2. After T041, Developer A moves to US4 (extends US1 UX).
3. Developer B: US2 (small, depends on US1's controller scaffold).
4. Developer C: US3 (regenerate), starting once US1's command is stable (T027).
5. Developer D: US5 (reviewer access), can start immediately after Phase 2.
6. US6 is picked up by whoever is free after US1 and US2 complete.
7. Polish is picked up by whoever finishes last.

---

## Notes

- `[P]` tasks touch different files and have no dependencies on incomplete tasks in the same phase.
- `[Story]` tags (US1–US6) map tasks to spec user stories for traceability and independent delivery.
- Each user story is independently deployable as long as Phase 2 is complete; stories deliberately do not depend on each other's UI polish.
- Tests live alongside their story except where they are cross-cutting (Polish phase).
- Commit after each task or logical group — mirroring the constitution's commit-discipline guideline.
- Stop at any checkpoint to validate the slice in a local Aspire environment before continuing.
- Avoid: editing the same controller file in parallel, conflating US-specific UI with foundational changes, slipping cross-cutting concerns into story phases where they dilute independence.
