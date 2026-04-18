# Tasks: Applicant Response & Appeal

**Input**: Design documents from `specs/004-applicant-response-appeal/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Unit tests for new domain aggregates (constitution mandates Rich Domain Model with testable invariants). E2E tests per constitution (NON-NEGOTIABLE, one per user story). Integration test for persistence.

**Organization**: Tasks grouped by user story. US1 (per-item response) is the MVP. US2 (open appeal + dispute thread) and US3 (appeal resolution) are both P2; US3 logically depends on US2 but each test sets up its own preconditions so they remain independently runnable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new projects needed. Create new directories required by the plan and confirm the solution builds before any domain changes.

- [X] T001 Create directory `src/FundingPlatform.Web/Views/ApplicantResponse/`
- [X] T002 Verify solution builds cleanly with `dotnet build`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema, enums, entity skeletons, EF configurations, and DbContext registration. All three user stories require these artifacts to exist before any story-specific behavior can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Database schema

- [X] T003 Create `src/FundingPlatform.Database/dbo/Tables/ApplicantResponses.sql` with columns `Id`, `ApplicationId`, `CycleNumber`, `SubmittedAt`, `SubmittedByUserId`, FKs to `Applications` and `AspNetUsers`, unique constraint `UQ_ApplicantResponses_AppCycle (ApplicationId, CycleNumber)`, and index on `ApplicationId` per `data-model.md` § ApplicantResponses.sql
- [X] T004 [P] Create `src/FundingPlatform.Database/dbo/Tables/ItemResponses.sql` with columns `Id`, `ApplicantResponseId`, `ItemId`, `Decision`, FKs to `ApplicantResponses` (cascade delete) and `Items`, unique constraint `UQ_ItemResponses_ResponseItem`, and index on `ItemId`
- [X] T005 [P] Create `src/FundingPlatform.Database/dbo/Tables/Appeals.sql` with columns `Id`, `ApplicationId`, `ApplicantResponseId`, `OpenedAt`, `OpenedByUserId`, `Status`, `Resolution`, `ResolvedAt`, `ResolvedByUserId`, `RowVersion` (ROWVERSION), FKs to `Applications`/`ApplicantResponses`/`AspNetUsers`, CHECK constraint `CK_Appeals_ResolutionConsistency`, filtered unique index `UX_Appeals_OneOpenPerApplication WHERE Status = 0`, and index on `ApplicationId`
- [X] T006 [P] Create `src/FundingPlatform.Database/dbo/Tables/AppealMessages.sql` with columns `Id`, `AppealId`, `AuthorUserId`, `Text (NVARCHAR(4000))`, `CreatedAt`, FK to `Appeals` (cascade delete) and `AspNetUsers`, CHECK constraint `CK_AppealMessages_TextNotEmpty`, and composite index `IX_AppealMessages_AppealId_CreatedAt`
- [X] T007 Add seed-data insert for `MaxAppealsPerApplication = '1'` in the post-deployment script under `src/FundingPlatform.Database/Post-Deployment/` (create the script if missing; guard with `IF NOT EXISTS`)

### Domain enums

- [X] T008 [P] Extend `ApplicationState` enum: add `AppealOpen = 4` and `ResponseFinalized = 5` (preserve existing values 0-3) in `src/FundingPlatform.Domain/Enums/ApplicationState.cs`
- [X] T009 [P] Create `ItemResponseDecision` enum with `Accept = 0`, `Reject = 1` in `src/FundingPlatform.Domain/Enums/ItemResponseDecision.cs`
- [X] T010 [P] Create `AppealStatus` enum with `Open = 0`, `Resolved = 1` in `src/FundingPlatform.Domain/Enums/AppealStatus.cs`
- [X] T011 [P] Create `AppealResolution` enum with `Uphold = 0`, `GrantReopenToDraft = 1`, `GrantReopenToReview = 2` in `src/FundingPlatform.Domain/Enums/AppealResolution.cs`

### Domain entity skeletons (structure + private constructors, behavior added in story phases)

- [X] T012 [P] Create `ApplicantResponse` aggregate root with properties (`Id`, `ApplicationId`, `CycleNumber`, `SubmittedAt`, `SubmittedByUserId`), private `_itemResponses` list, read-only `ItemResponses` navigation, private parameterless constructor (for EF), but NO factory or behavior methods yet (added in Phase 3) in `src/FundingPlatform.Domain/Entities/ApplicantResponse.cs`
- [X] T013 [P] Create `ItemResponse` child entity with properties (`Id`, `ApplicantResponseId`, `ItemId`, `Decision`) and internal constructor callable from `ApplicantResponse` only in `src/FundingPlatform.Domain/Entities/ItemResponse.cs`
- [X] T014 [P] Create `Appeal` aggregate root with properties (`Id`, `ApplicationId`, `ApplicantResponseId`, `OpenedAt`, `OpenedByUserId`, `Status`, `Resolution`, `ResolvedAt`, `ResolvedByUserId`, `RowVersion`), private `_messages` list, read-only `Messages` navigation, private parameterless constructor, NO factory/behavior yet (added in Phases 4-5) in `src/FundingPlatform.Domain/Entities/Appeal.cs`
- [X] T015 [P] Create `AppealMessage` child entity with properties (`Id`, `AppealId`, `AuthorUserId`, `Text`, `CreatedAt`) and internal constructor callable from `Appeal` only in `src/FundingPlatform.Domain/Entities/AppealMessage.cs`

### EF configurations

- [X] T016 [P] Create `ApplicantResponseConfiguration` mapping to `ApplicantResponses` table with all columns required; configure `HasMany(_ => ItemResponses)` with cascade delete; backing-field access for `_itemResponses` in `src/FundingPlatform.Infrastructure/Persistence/Configurations/ApplicantResponseConfiguration.cs`
- [X] T017 [P] Create `ItemResponseConfiguration` mapping to `ItemResponses` table; map `Decision` as `int` (enum conversion) in `src/FundingPlatform.Infrastructure/Persistence/Configurations/ItemResponseConfiguration.cs`
- [X] T018 [P] Create `AppealConfiguration` mapping to `Appeals` table; configure `RowVersion` as concurrency token; enum conversions for `Status` and `Resolution`; `HasMany(_ => Messages)` with cascade delete; backing-field access in `src/FundingPlatform.Infrastructure/Persistence/Configurations/AppealConfiguration.cs`
- [X] T019 [P] Create `AppealMessageConfiguration` mapping to `AppealMessages` table; `Text` max length 4000 and required in `src/FundingPlatform.Infrastructure/Persistence/Configurations/AppealMessageConfiguration.cs`
- [X] T020 Register new `DbSet<ApplicantResponse>`, `DbSet<Appeal>` on `ApplicationDbContext`; child entities are configured via owning aggregate configurations in `src/FundingPlatform.Infrastructure/Persistence/ApplicationDbContext.cs`

### Build verification

- [X] T021 Run `dotnet build` and confirm solution compiles cleanly with new schema, enums, entities, and configurations in place (no behavior methods yet)

**Checkpoint**: Database schema, enums, entity skeletons, and EF wiring are in place. Build passes. User story phases can proceed.

---

## Phase 3: User Story 1 - Per-Item Response to Reviewed Application (Priority: P1) 🎯 MVP

**Goal**: Applicant opens the response screen for a finalized application, accepts or rejects each item, and submits. Accepted items advance out of the response stage; rejected items are marked final.

**Independent Test**: Take an application in `Resolved` state, log in as the owning applicant, navigate to the response screen, respond per item, submit, and verify state transitions to `ResponseFinalized` with accepted items advancing.

### Domain behavior

- [X] T022 [US1] Add static factory `ApplicantResponse.Submit(applicationId, cycleNumber, submittedByUserId, itemDecisions)` that: creates the aggregate, constructs one `ItemResponse` per `(itemId, decision)` pair, throws `InvalidOperationException` if any item on the application lacks a decision or if `itemDecisions` contains duplicate items, stamps `SubmittedAt = DateTime.UtcNow`. Implementation in `src/FundingPlatform.Domain/Entities/ApplicantResponse.cs`
- [X] T023 [US1] Add `Application.SubmitResponse(itemDecisions, submittedByUserId)` method that: verifies `State == Resolved`, computes `CycleNumber = ApplicantResponses.Count + 1`, invokes `ApplicantResponse.Submit(...)`, adds the response to a private `_applicantResponses` list, transitions `State` to `ResponseFinalized`, stamps `UpdatedAt`; also add private `List<ApplicantResponse> _applicantResponses` field and `IReadOnlyList<ApplicantResponse> ApplicantResponses` navigation in `src/FundingPlatform.Domain/Entities/Application.cs`

### Application layer

- [X] T024 [P] [US1] Create `ItemResponseDto` with fields `ItemId`, `ProductName`, `ReviewStatus`, `SelectedSupplierName` (nullable), `Amount` (nullable), `ReviewComment` (nullable), `Decision` (nullable, set only if response exists) in `src/FundingPlatform.Application/DTOs/ItemResponseDto.cs`
- [X] T025 [P] [US1] Create `ApplicantResponseDto` with fields `ApplicationId`, `CycleNumber` (nullable), `SubmittedAt` (nullable), `IsSubmitted` (bool), `State` (ApplicationState), `Items` (List<ItemResponseDto>) in `src/FundingPlatform.Application/DTOs/ApplicantResponseDto.cs`
- [X] T026 [P] [US1] Create `GetApplicantResponseQuery(int applicationId, string userId)` request and handler that: verifies application exists, verifies applicant ownership, returns `ApplicantResponseDto` with current item decisions (from latest `ApplicantResponse` if any) in `src/FundingPlatform.Application/Applications/Queries/GetApplicantResponseQuery.cs`
- [X] T027 [P] [US1] Create `SubmitApplicantResponseCommand(int applicationId, string userId, Dictionary<int, ItemResponseDecision> itemDecisions)` request in `src/FundingPlatform.Application/Applications/Commands/SubmitApplicantResponseCommand.cs`
- [X] T028 [US1] Create `ApplicantResponseService` with method `SubmitResponseAsync(command)` that: loads application (with items), verifies ownership, calls `application.SubmitResponse(...)`, saves via DbContext, returns result DTO. Method `GetResponseAsync(query)` returns read model for the screen. Class lives in `src/FundingPlatform.Application/Services/ApplicantResponseService.cs`
- [X] T029 [US1] Register `ApplicantResponseService` in DI in `src/FundingPlatform.Application/DependencyInjection.cs`

### Web layer

- [X] T030 [P] [US1] Create `ItemResponseViewModel` with fields `ItemId`, `ProductName`, `ReviewStatus`, `SelectedSupplierName`, `Amount`, `ReviewComment`, `Decision` (enum) in `src/FundingPlatform.Web/ViewModels/ItemResponseViewModel.cs`
- [X] T031 [P] [US1] Create `ApplicantResponseViewModel` with fields `ApplicationId`, `IsSubmitted`, `State`, `SubmittedAt`, `Items` (List<ItemResponseViewModel>), `CanOpenAppeal` (placeholder for US2) in `src/FundingPlatform.Web/ViewModels/ApplicantResponseViewModel.cs`
- [X] T032 [US1] Create `ApplicantResponseController` with `[Authorize(Roles = "Applicant")]`: action `Index(int id)` returns the view; action `Submit(int id, ApplicantResponseViewModel model)` POSTs the response; add ownership check that returns 403 if the current user is not the applicant in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T033 [US1] Create `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml` with: one row per item displaying reviewer decision, accept/reject radio per row, disabled submit until every item has a decision, read-only display once submitted
- [X] T034 [US1] Add link/redirect from the existing application detail or review-finalized flow to `/ApplicantResponse/Index/{id}` when the state reaches `Resolved` and the user is the applicant (minimal change — adjust existing applicant dashboard or application details view in `src/FundingPlatform.Web/Views/Application/` or `Home/Index.cshtml`)

### Unit tests (US1)

- [X] T035 [P] [US1] Create `ApplicantResponseTests` with cases: submits response with all accept decisions; submits with mixed accept/reject; throws when items missing; throws when decisions duplicate items; immutable after construction (no mutation methods reachable) in `tests/FundingPlatform.Tests.Unit/Domain/ApplicantResponseTests.cs`
- [X] T036 [P] [US1] Create `ApplicationResponseTransitionsTests` with cases: `SubmitResponse` transitions Resolved → ResponseFinalized; throws if State != Resolved; increments `CycleNumber` on sequential responses (after reopen cycles — tested with arranged state); `ApplicantResponses` collection exposes submitted snapshots in order in `tests/FundingPlatform.Tests.Unit/Domain/ApplicationResponseTransitionsTests.cs`

### E2E test (US1)

- [X] T037 [P] [US1] Create `ApplicantResponsePage` page object with locators for: item rows, per-item accept/reject radios, submit button, read-only-after-submit indicator in `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicantResponsePage.cs`
- [X] T038 [US1] Create `ApplicantResponseTests` E2E class with test method `Applicant_Can_Respond_Per_Item_And_Accepted_Items_Advance`: seeds a Resolved application with 2 approved + 1 rejected item, applicant accepts the 2 approved and rejects the rejected, submits, asserts state = ResponseFinalized, verifies response is read-only after reload in `tests/FundingPlatform.Tests.E2E/Tests/ApplicantResponseTests.cs`

**Checkpoint**: User Story 1 is fully functional. Applicants can respond per item. MVP increment complete.

---

## Phase 4: User Story 2 - Appeal via Dispute Thread (Priority: P2)

**Goal**: After completing a response with at least one rejected item, the applicant opens an appeal, which creates a text-only dispute thread and freezes the application. Any reviewer can post replies.

**Independent Test**: With a `ResponseFinalized` application that has at least one rejected item, applicant clicks Open Appeal, confirm state transitions to `AppealOpen`, applicant and reviewers exchange messages, freeze is observable.

### Domain behavior

- [X] T039 [US2] Add static factory `Appeal.Open(applicationId, applicantResponseId, openedByUserId)` that: constructs with `Status = Open`, `OpenedAt = DateTime.UtcNow`, leaves resolution fields null in `src/FundingPlatform.Domain/Entities/Appeal.cs`
- [X] T040 [US2] Add `Appeal.PostMessage(authorUserId, text)` method that: validates `text` non-empty and ≤ 4000 chars, throws `InvalidOperationException` if `Status == Resolved`, creates `AppealMessage` via internal constructor, appends to `_messages` in `src/FundingPlatform.Domain/Entities/Appeal.cs`
- [X] T041 [US2] Add `Application.OpenAppeal(openedByUserId, maxAppeals)` method that: verifies `State == ResponseFinalized`, verifies latest `ApplicantResponse` has at least one `ItemResponse` with `Decision == Reject`, verifies `Appeals.Count < maxAppeals`, constructs `Appeal.Open(...)` against the latest response, adds to private `_appeals` list, transitions `State` to `AppealOpen`; also add private `List<Appeal> _appeals` field and `IReadOnlyList<Appeal> Appeals` navigation in `src/FundingPlatform.Domain/Entities/Application.cs`

### Application layer

- [X] T042 [P] [US2] Create `AppealMessageDto` with fields `Id`, `AuthorUserId`, `AuthorDisplayName`, `Text`, `CreatedAt` in `src/FundingPlatform.Application/DTOs/AppealMessageDto.cs`
- [X] T043 [P] [US2] Create `AppealDto` with fields `Id`, `ApplicationId`, `OpenedAt`, `OpenedByUserId`, `Status`, `Resolution` (nullable), `ResolvedAt` (nullable), `ResolvedByUserId` (nullable), `Messages` (List<AppealMessageDto>) in `src/FundingPlatform.Application/DTOs/AppealDto.cs`
- [X] T044 [P] [US2] Create `GetAppealQuery(int applicationId, string userId, bool isReviewer)` request and handler that: loads the active or most recent appeal for the application, verifies viewer authorization (owner applicant OR any reviewer), returns `AppealDto` in `src/FundingPlatform.Application/Applications/Queries/GetAppealQuery.cs`
- [X] T045 [P] [US2] Create `OpenAppealCommand(int applicationId, string userId)` request in `src/FundingPlatform.Application/Applications/Commands/OpenAppealCommand.cs`
- [X] T046 [P] [US2] Create `PostAppealMessageCommand(int applicationId, string userId, string text)` request in `src/FundingPlatform.Application/Applications/Commands/PostAppealMessageCommand.cs`
- [X] T047 [US2] Extend `ApplicantResponseService` with: `OpenAppealAsync(command)` — loads application, reads `MaxAppealsPerApplication` from `SystemConfiguration`, calls `application.OpenAppeal(...)`, saves; `PostMessageAsync(command)` — loads application, verifies role + access, calls `appeal.PostMessage(...)`, saves in `src/FundingPlatform.Application/Services/ApplicantResponseService.cs`

### Web layer

- [X] T048 [P] [US2] Create `AppealMessageViewModel` with fields `AuthorDisplayName`, `AuthorUserId`, `Text`, `CreatedAt`, `IsByApplicant` (bool for styling) in `src/FundingPlatform.Web/ViewModels/AppealMessageViewModel.cs`
- [X] T049 [P] [US2] Create `AppealThreadViewModel` with fields `ApplicationId`, `AppealId`, `Status`, `Messages` (List<AppealMessageViewModel>), `CanPostMessage` (bool), `NewMessageText` (bound for form), `CanResolve` (placeholder for US3) in `src/FundingPlatform.Web/ViewModels/AppealThreadViewModel.cs`
- [X] T050 [US2] Add `OpenAppeal(int id)` POST action to `ApplicantResponseController` that calls `OpenAppealAsync` and redirects to `Appeal(id)`; add `[Authorize(Roles = "Applicant")]` guard and ownership check; surface cap-exceeded error via `TempData` or model error in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T051 [US2] Add `Appeal(int id)` GET action returning the thread view for both applicant (owner) and reviewers; use `[Authorize(Roles = "Applicant,Reviewer")]` and apply the ownership-or-reviewer check in the query handler in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T052 [US2] Add `PostMessage(int id, AppealThreadViewModel model)` POST action that calls `PostMessageAsync` and redirects back to `Appeal(id)` in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T053 [US2] Create `src/FundingPlatform.Web/Views/ApplicantResponse/Appeal.cshtml` rendering: message list (chronological), post-message form (visible when `CanPostMessage` is true), status banner showing Open vs. Resolved
- [X] T054 [US2] Create `src/FundingPlatform.Web/Views/ApplicantResponse/_AppealMessage.cshtml` partial rendering a single message (author, timestamp, text, applicant-vs-reviewer styling)
- [X] T055 [US2] Add "Open Appeal" button on the `Index.cshtml` response screen visible only when `State == ResponseFinalized`, at least one item was rejected by applicant, and cap not reached; posts to `OpenAppeal(id)` in `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml`

### Unit tests (US2)

- [X] T056 [P] [US2] Create `AppealTests` with cases: `Open` factory stamps timestamp and sets Status=Open; `PostMessage` appends in order; `PostMessage` throws when text empty; throws when text > 4000 chars; throws when status is Resolved in `tests/FundingPlatform.Tests.Unit/Domain/AppealTests.cs`
- [X] T057 [US2] Extend `ApplicationResponseTransitionsTests` with cases: `OpenAppeal` transitions ResponseFinalized → AppealOpen; throws if state != ResponseFinalized; throws if no rejected item in latest response; throws when appeal count equals maxAppeals; throws when maxAppeals is 0 in `tests/FundingPlatform.Tests.Unit/Domain/ApplicationResponseTransitionsTests.cs`

### E2E test (US2)

- [X] T058 [P] [US2] Create `AppealThreadPage` page object with locators for: messages list, author/text/timestamp per message, new-message text area, post button, status banner, (placeholder selectors for US3 resolution buttons) in `tests/FundingPlatform.Tests.E2E/PageObjects/AppealThreadPage.cs`
- [X] T059 [US2] Extend `ApplicantResponseTests` E2E class with test method `Applicant_Can_Open_Appeal_On_Rejected_Items_And_Reviewers_Can_Reply`: seeds ResponseFinalized application with at least one applicant-rejected item, applicant opens appeal, asserts state = AppealOpen, applicant posts first message, reviewer logs in and posts reply, both messages visible in chronological order in `tests/FundingPlatform.Tests.E2E/Tests/ApplicantResponseTests.cs`

**Checkpoint**: User Story 2 is fully functional. Appeals can be opened and discussed. Application freezes during open appeals.

---

## Phase 5: User Story 3 - Appeal Resolution with Uphold or Reopen (Priority: P2)

**Goal**: Any reviewer engaged in an active dispute thread explicitly resolves the appeal as Uphold (application unfreezes), Grant — Reopen to Draft (applicant edits), or Grant — Reopen to Review (reviewers revise decisions). Resolution is an explicit action, not implicit from messages.

**Independent Test**: With an `AppealOpen` application, a reviewer invokes each of the three resolutions (in separate test runs) and confirms the correct state transition and follow-on behavior.

### Domain behavior

- [X] T060 [US3] Add `Appeal.Resolve(resolvedByUserId, resolution)` method that: throws if `Status == Resolved`, sets `Status = Resolved`, `Resolution = resolution`, `ResolvedByUserId = resolvedByUserId`, `ResolvedAt = DateTime.UtcNow` in `src/FundingPlatform.Domain/Entities/Appeal.cs`
- [X] T061 [US3] Add `Application.ResolveAppealAsUphold(resolvedByUserId)` method that: verifies `State == AppealOpen`, finds the active appeal, calls `appeal.Resolve(resolvedByUserId, AppealResolution.Uphold)`, transitions `State` to `ResponseFinalized`, stamps `UpdatedAt` in `src/FundingPlatform.Domain/Entities/Application.cs`
- [X] T062 [US3] Add `Application.ResolveAppealAsGrantReopenToDraft(resolvedByUserId)` method that: verifies `State == AppealOpen`, finds the active appeal, calls `appeal.Resolve(..., GrantReopenToDraft)`, transitions `State` to `Draft`, clears `SubmittedAt`, stamps `UpdatedAt` in `src/FundingPlatform.Domain/Entities/Application.cs`
- [X] T063 [US3] Add `Application.ResolveAppealAsGrantReopenToReview(resolvedByUserId)` method that: verifies `State == AppealOpen`, finds the active appeal, calls `appeal.Resolve(..., GrantReopenToReview)`, transitions `State` to `UnderReview` WITHOUT resetting item review statuses (distinct from existing `SendBack()` behavior) in `src/FundingPlatform.Domain/Entities/Application.cs`

### Application layer

- [X] T064 [P] [US3] Create `ResolveAppealCommand(int applicationId, string userId, AppealResolution resolution)` request in `src/FundingPlatform.Application/Applications/Commands/ResolveAppealCommand.cs`
- [X] T065 [US3] Extend `ApplicantResponseService` with `ResolveAppealAsync(command)` that: verifies Reviewer role (in controller, enforced here by the DbContext query), loads application, dispatches to the correct `Application.ResolveAppealAs*()` method based on `command.Resolution`, saves in `src/FundingPlatform.Application/Services/ApplicantResponseService.cs`

### Web layer

- [X] T066 [US3] Add `ResolveAppeal(int id, AppealResolution resolution)` POST action to `ApplicantResponseController` with `[Authorize(Roles = "Reviewer")]`; on success, redirect based on resolution (Uphold/back to appeal page; GrantReopenToDraft → applicant's draft edit screen; GrantReopenToReview → the reviewer's review screen for the application) in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T067 [US3] Update `AppealThreadViewModel`: set `CanResolve` to true when the viewer has Reviewer role AND status is Open; add `Resolution` form field for the submit in `src/FundingPlatform.Web/ViewModels/AppealThreadViewModel.cs`
- [X] T068 [US3] Add a resolution form to `Appeal.cshtml`: three submit buttons (Uphold / Grant — Reopen to Draft / Grant — Reopen to Review), visible only when `CanResolve` is true in `src/FundingPlatform.Web/Views/ApplicantResponse/Appeal.cshtml`

### Unit tests (US3)

- [X] T069 [US3] Extend `AppealTests` with: `Resolve` sets status + resolution + resolvedAt + resolvedByUserId; `Resolve` throws if already resolved; `PostMessage` throws after `Resolve` in `tests/FundingPlatform.Tests.Unit/Domain/AppealTests.cs`
- [X] T070 [US3] Extend `ApplicationResponseTransitionsTests` with cases: Uphold transitions AppealOpen → ResponseFinalized; GrantReopenToDraft transitions AppealOpen → Draft (and clears SubmittedAt); GrantReopenToReview transitions AppealOpen → UnderReview (and item review statuses are preserved, not reset); each `ResolveAppealAs*` throws if State != AppealOpen; after resolution, appeal count does not allow new appeal if it reached cap in `tests/FundingPlatform.Tests.Unit/Domain/ApplicationResponseTransitionsTests.cs`

### E2E test (US3)

- [X] T071 [US3] Extend `AppealThreadPage` page object with locators for three resolution buttons in `tests/FundingPlatform.Tests.E2E/PageObjects/AppealThreadPage.cs`
- [X] T072 [US3] Extend `ApplicantResponseTests` E2E class with test method `Reviewer_Can_Resolve_Appeal_With_All_Three_Outcomes`: parameterized across three resolutions; for Uphold, verify state=ResponseFinalized and application unfreezes; for GrantReopenToDraft, verify state=Draft and applicant can edit; for GrantReopenToReview, verify state=UnderReview and item statuses preserved in `tests/FundingPlatform.Tests.E2E/Tests/ApplicantResponseTests.cs`

**Checkpoint**: All three user stories functional. End-to-end response-through-appeal-through-resolution flow works.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Wire up cross-cutting concerns (audit, authorization consistency, persistence verification) and validate the quickstart.

- [X] T073 [P] Create `ApplicantResponsePersistenceTests` integration test class: inserts + retrieves ApplicantResponse with ItemResponses and verifies cascade-delete; inserts Appeal with messages and verifies cascade-delete; asserts the filtered unique index blocks two open appeals on the same application; asserts the CHECK constraint blocks inconsistent resolution state in `tests/FundingPlatform.Tests.Integration/Persistence/ApplicantResponsePersistenceTests.cs`
- [X] T074 Add audit-trail integration: ensure every state transition (SubmitResponse, OpenAppeal, Resolve*) appends a `VersionHistory` entry via the existing mechanism in `src/FundingPlatform.Domain/Entities/Application.cs` or the service layer (align with pattern used by spec 002)
- [X] T075 [P] Add error-display polish: confirm all domain exceptions are caught in `ApplicantResponseController` and surfaced to the view via model state or `TempData`, consistent with existing controller patterns (e.g., `ReviewController`) in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs`
- [X] T076 [P] Run `dotnet format` and verify no warnings after changes
- [X] T077 Execute `quickstart.md` steps manually or via Aspire dashboard and confirm the full flow (respond → appeal → each of three resolutions) works end-to-end against the dev stack

**Checkpoint**: Feature is production-ready. All tests pass. Quickstart validated.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup; BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational
- **User Story 2 (Phase 4)**: Depends on Foundational; uses `ApplicantResponse` from Phase 3 behavior for cap/response-complete checks, so Phase 3 domain behavior must be at least partially complete (T022-T023)
- **User Story 3 (Phase 5)**: Depends on Foundational; logically depends on Phase 4 (needs an open appeal to resolve), but tests can arrange their own preconditions
- **Polish (Phase 6)**: Depends on all user stories complete

### Within Each User Story

- Domain behavior before Application layer before Web layer
- Unit tests can be written in parallel with implementation (constitutional preference for domain-first)
- E2E test is the last task in each story phase — exercises the full stack

### Parallel Opportunities

- Setup tasks (T001, T002) are trivially sequential
- Foundational SQL table files (T003-T006) are parallelizable
- Foundational enum files (T008-T011) are parallelizable
- Foundational entity skeletons (T012-T015) are parallelizable
- Foundational EF configurations (T016-T019) are parallelizable; T020 depends on them
- Within each user story, DTOs/commands/queries (new files) are parallelizable; controller actions and views are often sequential because they share files

---

## Parallel Example: Foundational Phase

```text
# After T001-T002 complete, launch all of the following in parallel:

# SQL tables
Task: "Create src/FundingPlatform.Database/dbo/Tables/ApplicantResponses.sql"
Task: "Create src/FundingPlatform.Database/dbo/Tables/ItemResponses.sql"
Task: "Create src/FundingPlatform.Database/dbo/Tables/Appeals.sql"
Task: "Create src/FundingPlatform.Database/dbo/Tables/AppealMessages.sql"

# Enums
Task: "Extend ApplicationState enum in src/FundingPlatform.Domain/Enums/ApplicationState.cs"
Task: "Create ItemResponseDecision enum"
Task: "Create AppealStatus enum"
Task: "Create AppealResolution enum"

# Entity skeletons
Task: "Create ApplicantResponse skeleton"
Task: "Create ItemResponse skeleton"
Task: "Create Appeal skeleton"
Task: "Create AppealMessage skeleton"

# EF configurations (after entity skeletons)
Task: "Create ApplicantResponseConfiguration"
Task: "Create ItemResponseConfiguration"
Task: "Create AppealConfiguration"
Task: "Create AppealMessageConfiguration"
```

Then sequentially: `T020` (DbContext registration) → `T021` (build verification).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) — 2 tasks
2. Complete Phase 2 (Foundational) — 19 tasks, blocks all stories
3. Complete Phase 3 (User Story 1) — 17 tasks
4. **STOP and VALIDATE**: Applicant can respond per item; accepted items advance.
5. Deploy/demo this MVP.

### Incremental Delivery

1. MVP: US1 response flow
2. Add US2: appeal opening + dispute thread (freezes application)
3. Add US3: appeal resolution (all three outcomes)
4. Polish: persistence tests, audit wiring, quickstart validation

### Parallel Team Strategy

With two developers once Foundational completes:

- Developer A: US1 (MVP path, critical path)
- Developer B: groundwork for US2 (page object, view layout) while A finishes US1 domain
- Together on US3 since it closes the state-machine loop

---

## Notes

- Four new database tables = one post-deployment script update (seed data) = no EF migrations (constitution IV)
- All domain invariants enforced in entity methods, never in services or controllers (constitution II)
- Tests are required: unit (domain), integration (persistence), E2E (user flows) per constitution III
- The `ApplicationState` enum extension is additive (preserves 0-3 values) — no data migration required for existing applications
- `[P]` tasks touch different files; tasks without `[P]` either share a file or depend on an earlier task in the same phase
