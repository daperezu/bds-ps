# Tasks: Review & Approval Workflow

**Input**: Design documents from `specs/002-review-approval-workflow/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: E2E Playwright tests are REQUIRED per constitution principle III and FR-019. Each user story includes test tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Domain enums, entity modifications, and database schema changes that all user stories depend on

- [ ] T001 Add UnderReview and Resolved values to ApplicationState enum in src/FundingPlatform.Domain/Enums/ApplicationState.cs
- [ ] T002 Create ItemReviewStatus enum (Pending, Approved, Rejected, NeedsInfo) in src/FundingPlatform.Domain/Enums/ItemReviewStatus.cs
- [ ] T003 Add review fields to Item entity (ReviewStatus, ReviewComment, SelectedSupplierId, IsNotTechnicallyEquivalent) and review methods (Approve, Reject, RequestMoreInfo, FlagNotEquivalent, ClearNotEquivalentFlag, ResetReviewStatus) in src/FundingPlatform.Domain/Entities/Item.cs
- [ ] T004 Add review state transition methods to Application entity (StartReview, SendBack, Finalize) in src/FundingPlatform.Domain/Entities/Application.cs
- [ ] T005 Add review columns (ReviewStatus, ReviewComment, SelectedSupplierId, IsNotTechnicallyEquivalent) to Items table definition in src/FundingPlatform.Database/Tables/Items.sql
- [ ] T006 Add Reviewer role to seed data in src/FundingPlatform.Database/PostDeployment/SeedData.sql
- [ ] T007 Update ItemConfiguration with review field mappings and SelectedSupplier navigation in src/FundingPlatform.Infrastructure/Persistence/Configurations/ItemConfiguration.cs
- [ ] T008 [P] Add Reviewer role creation to IdentityConfiguration.SeedRolesAsync in src/FundingPlatform.Infrastructure/Identity/IdentityConfiguration.cs

**Checkpoint**: Domain model and database schema ready. All review enums, entity methods, and EF configurations in place.

---

## Phase 2: Foundational (Application Layer + Controller Shell)

**Purpose**: ReviewService, DTOs, and ReviewController that all user stories need

- [ ] T009 [P] Create ReviewQueueItemDto in src/FundingPlatform.Application/DTOs/ReviewQueueItemDto.cs
- [ ] T010 [P] Create ReviewApplicationDto and ReviewItemDto in src/FundingPlatform.Application/DTOs/ReviewApplicationDto.cs
- [ ] T011 Create ReviewService with GetReviewQueueAsync and GetApplicationForReviewAsync methods in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T012 Register ReviewService in DI container in src/FundingPlatform.Application/DependencyInjection.cs
- [ ] T013 Create ReviewController shell with Authorize(Roles="Reviewer") and inject ReviewService in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T014 [P] Create ReviewQueuePage page object for E2E tests in tests/FundingPlatform.Tests.E2E/PageObjects/ReviewQueuePage.cs
- [ ] T015 [P] Create ReviewApplicationPage page object for E2E tests in tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs

**Checkpoint**: Foundation ready — ReviewService, DTOs, controller shell, and page objects in place. User story implementation can begin.

---

## Phase 3: User Story 1 - Reviewer Views Submitted Applications Queue (Priority: P1) MVP

**Goal**: A reviewer sees a paginated list of all submitted applications with applicant name, submission date, item count, and performance score.

**Independent Test**: Submit applications as an applicant, log in as reviewer, verify they appear in the paginated queue.

### E2E Tests for User Story 1

- [ ] T016 [US1] Write Playwright E2E test: reviewer sees submitted applications in queue, drafts and resolved apps excluded, pagination works — in tests/FundingPlatform.Tests.E2E/Tests/ReviewQueueTests.cs

### Implementation for User Story 1

- [ ] T017 [US1] Implement GetReviewQueueAsync in ReviewService: paginated query filtering State==Submitted, joining Applicant for name and score, ordered by SubmittedAt — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T018 [P] [US1] Create ReviewQueueViewModel and ReviewQueueItemViewModel in src/FundingPlatform.Web/ViewModels/ReviewQueueViewModel.cs
- [ ] T019 [US1] Implement Index(int page) action in ReviewController: call GetReviewQueueAsync, map to view model, return view — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T020 [US1] Create Review/Index.cshtml view: paginated table of applications with applicant name, submission date, item count, performance score, and link to review detail — in src/FundingPlatform.Web/Views/Review/Index.cshtml
- [ ] T021 [US1] Add Review link to navbar for users with Reviewer role in src/FundingPlatform.Web/Views/Shared/_Layout.cshtml

**Checkpoint**: User Story 1 complete. Reviewer can browse the queue and see submitted applications. Run ReviewQueueTests to validate.

---

## Phase 4: User Story 2 - Reviewer Opens and Reviews an Application (Priority: P1)

**Goal**: Reviewer opens an application from the queue, sees full details (applicant info, performance score, all items with quotations and impact), and application transitions to Under Review.

**Independent Test**: Open a submitted application as reviewer, verify state changes to Under Review, verify all data is displayed.

### E2E Tests for User Story 2

- [ ] T022 [US2] Write Playwright E2E test: reviewer opens submitted application, state transitions to Under Review, all items/suppliers/quotations/impact displayed, performance score shown read-only — in tests/FundingPlatform.Tests.E2E/Tests/ReviewApplicationTests.cs

### Implementation for User Story 2

- [ ] T023 [US2] Implement GetApplicationForReviewAsync in ReviewService: load application with all items, quotations, suppliers, impact, applicant; call StartReview on domain entity — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T024 [P] [US2] Create ReviewApplicationViewModel and ReviewItemViewModel in src/FundingPlatform.Web/ViewModels/ReviewApplicationViewModel.cs
- [ ] T025 [US2] Implement Review(int id) GET action in ReviewController: call GetApplicationForReviewAsync, compute lowest-price recommendation per item, map to view model — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T026 [US2] Create Review/Review.cshtml view: applicant info with performance score, items list with category, tech specs, impact, quotations table with supplier details and price highlighting — in src/FundingPlatform.Web/Views/Review/Review.cshtml

**Checkpoint**: User Story 2 complete. Reviewer can open an application and see all details. State transitions to Under Review. Run ReviewApplicationTests to validate.

---

## Phase 5: User Story 3 - Reviewer Makes Item-Level Decisions (Priority: P1)

**Goal**: Reviewer can approve (with supplier selection), reject, or request more info on each item with optional plain text comments. Lowest-price supplier is highlighted.

**Independent Test**: Open application, set approve/reject/request-info on items, verify decisions persist and comments are saved.

### E2E Tests for User Story 3

- [ ] T027 [US3] Write Playwright E2E test: reviewer approves item with supplier and comment, rejects item with comment, requests more info without comment, lowest-price supplier highlighted, equal-price shows no highlight — in tests/FundingPlatform.Tests.E2E/Tests/ReviewItemDecisionTests.cs

### Implementation for User Story 3

- [ ] T028 [US3] Create ReviewItemCommand (ApplicationId, ItemId, Decision, Comment, SelectedSupplierId) in src/FundingPlatform.Application/Applications/Commands/ReviewItemCommand.cs
- [ ] T029 [US3] Implement ReviewItemAsync in ReviewService: load application, call domain method on item (Approve/Reject/RequestMoreInfo), record version history, save — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T030 [US3] Implement ReviewItem POST action in ReviewController: parse decision, call ReviewItemAsync, redirect back to review page with success/error message — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T031 [US3] Add item decision form to Review/Review.cshtml: decision radio buttons (Approve/Reject/Request More Info), supplier dropdown (shown when Approve selected), comment textarea, submit button per item — in src/FundingPlatform.Web/Views/Review/Review.cshtml

**Checkpoint**: User Story 3 complete. Reviewer can make item-level decisions. Run ReviewItemDecisionTests to validate.

---

## Phase 6: User Story 4 - Reviewer Flags Technical Equivalence (Priority: P1)

**Goal**: Reviewer can flag quotations as not technically equivalent, triggering automatic rejection. Can undo the flag.

**Independent Test**: Flag an item as not equivalent, verify auto-rejection. Clear flag, verify item returns to Pending.

### E2E Tests for User Story 4

- [ ] T032 [US4] Write Playwright E2E test: flag not-equivalent triggers auto-rejection, attempt to approve flagged item is prevented, clear flag returns item to Pending — in tests/FundingPlatform.Tests.E2E/Tests/TechnicalEquivalenceTests.cs

### Implementation for User Story 4

- [ ] T033 [US4] Create FlagTechnicalEquivalenceCommand (ApplicationId, ItemId, IsNotEquivalent) in src/FundingPlatform.Application/Applications/Commands/FlagTechnicalEquivalenceCommand.cs
- [ ] T034 [US4] Implement FlagTechnicalEquivalenceAsync in ReviewService: load application, call FlagNotEquivalent or ClearNotEquivalentFlag on item, record version history, save — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T035 [US4] Implement FlagEquivalence POST action in ReviewController — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T036 [US4] Add technical equivalence toggle to Review/Review.cshtml: checkbox or button per item, visual indicator when flagged, disable approve button when flagged — in src/FundingPlatform.Web/Views/Review/Review.cshtml

**Checkpoint**: User Story 4 complete. Technical equivalence flagging works with auto-rejection. Run TechnicalEquivalenceTests to validate.

---

## Phase 7: User Story 5 - Reviewer Selects Supplier for Approved Items (Priority: P1)

**Goal**: Supplier selection is mandatory when approving. System highlights lowest-price. No highlight on ties.

**Independent Test**: Approve item without selecting supplier (expect error), approve with non-recommended supplier (accept), verify tied prices show no highlight.

### E2E Tests for User Story 5

- [ ] T037 [US5] Write Playwright E2E test: approval requires supplier selection, non-recommended supplier accepted, equal prices show no recommendation — in tests/FundingPlatform.Tests.E2E/Tests/SupplierSelectionTests.cs

### Implementation for User Story 5

Note: Supplier selection logic is primarily implemented in US3 (Approve method requires SupplierId). This phase focuses on the recommendation display and enforcement edge cases.

- [ ] T038 [US5] Add lowest-price recommendation computation to ReviewService.GetApplicationForReviewAsync: for each item find min price, set null if tie — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T039 [US5] Enhance supplier dropdown in Review/Review.cshtml: highlight recommended supplier, show "(Recommended - lowest price)" label, show "(Tied price)" when no recommendation — in src/FundingPlatform.Web/Views/Review/Review.cshtml

**Checkpoint**: User Story 5 complete. Supplier selection enforcement and recommendation display working. Run SupplierSelectionTests to validate.

---

## Phase 8: User Story 6 - Reviewer Sends Application Back for More Info (Priority: P1)

**Goal**: Reviewer can send application back to Draft. All item statuses reset to Pending. Comments preserved. Applicant can edit and resubmit.

**Independent Test**: Mark item as Needs Info, send back, verify application returns to Draft, verify applicant sees comments and can edit, verify resubmission preserves previous comments.

### E2E Tests for User Story 6

- [ ] T040 [US6] Write Playwright E2E test: send back transitions to Draft, item statuses reset to Pending, applicant sees reviewer comments, applicant can edit and resubmit, previous comments preserved on resubmission — in tests/FundingPlatform.Tests.E2E/Tests/SendBackApplicationTests.cs

### Implementation for User Story 6

- [ ] T041 [US6] Create SendBackApplicationCommand (ApplicationId) in src/FundingPlatform.Application/Applications/Commands/SendBackApplicationCommand.cs
- [ ] T042 [US6] Implement SendBackAsync in ReviewService: load application, call SendBack on domain entity, record version history, save — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T043 [US6] Implement SendBack POST action in ReviewController: call SendBackAsync, redirect to queue with confirmation message — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T044 [US6] Add Send Back button to Review/Review.cshtml: visible when application is Under Review — in src/FundingPlatform.Web/Views/Review/Review.cshtml
- [ ] T045 [US6] Show reviewer comments on applicant's application detail/edit views: display previous review comments per item as read-only notes — in src/FundingPlatform.Web/Views/Application/Details.cshtml and src/FundingPlatform.Web/Views/Application/Edit.cshtml

**Checkpoint**: User Story 6 complete. Send-back flow works end-to-end including applicant seeing comments. Run SendBackApplicationTests to validate.

---

## Phase 9: User Story 7 - Reviewer Finalizes the Review (Priority: P1)

**Goal**: Reviewer finalizes the review. Warning shown if unresolved items. Confirmation implicitly rejects unresolved items. Application transitions to Resolved.

**Independent Test**: Resolve all items and finalize. Also test finalization with unresolved items (warning + confirmation).

### E2E Tests for User Story 7

- [ ] T046 [US7] Write Playwright E2E test: finalize with all items resolved transitions to Resolved, finalize with unresolved items shows warning, confirm force-finalizes with implicit rejections, cancel keeps Under Review — in tests/FundingPlatform.Tests.E2E/Tests/FinalizeReviewTests.cs

### Implementation for User Story 7

- [ ] T047 [US7] Create FinalizeReviewCommand (ApplicationId, bool Force) in src/FundingPlatform.Application/Applications/Commands/FinalizeReviewCommand.cs
- [ ] T048 [US7] Implement FinalizeReviewAsync in ReviewService: load application, check for unresolved items, if force=false and unresolved items exist return warning with list, if force=true call Finalize(true) on domain entity, record version history, save — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T049 [US7] Implement Finalize POST action in ReviewController: handle normal finalization and force finalization, redirect to queue on success, show warning partial with unresolved item list on non-force with unresolved — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T050 [US7] Add Finalize Review button and confirmation warning dialog to Review/Review.cshtml: button visible when Under Review, warning modal listing unresolved items with Confirm/Cancel options — in src/FundingPlatform.Web/Views/Review/Review.cshtml

**Checkpoint**: User Story 7 complete. Full review lifecycle works: queue → review → decide → finalize/send-back. Run FinalizeReviewTests to validate.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Version history, authorization edge cases, concurrency handling

- [ ] T051 Ensure all review actions (item decisions, send-back, finalize) record VersionHistory entries with appropriate action descriptions in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T052 Add authorization checks: verify reviewer role on all ReviewController actions, verify application exists and is in valid state before each action — in src/FundingPlatform.Web/Controllers/ReviewController.cs
- [ ] T053 Handle DbUpdateConcurrencyException in ReviewService: catch and return user-friendly error when concurrent reviewers conflict — in src/FundingPlatform.Application/Services/ReviewService.cs
- [ ] T054 Verify resolved applications do not appear in review queue and cannot be modified — in tests/FundingPlatform.Tests.E2E/Tests/ReviewQueueTests.cs (extend existing test)
- [ ] T055 Run full E2E test suite to verify no regressions against spec 001 features

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3-9)**: All depend on Foundational phase completion
  - US1 (Queue): No dependencies on other stories
  - US2 (Open/Review): No dependencies on other stories (but naturally follows US1 in flow)
  - US3 (Item Decisions): No dependencies on other stories
  - US4 (Technical Equivalence): Depends on US3 (uses same review form)
  - US5 (Supplier Selection): Depends on US3 (extends approve action)
  - US6 (Send Back): Depends on US3 (needs items with decisions to send back)
  - US7 (Finalize): Depends on US3 (needs items with decisions to finalize)
- **Polish (Phase 10)**: Depends on all user stories being complete

### Within Each User Story

- E2E test written first (should fail initially)
- Commands/DTOs before service methods
- Service methods before controller actions
- Controller actions before views
- Commit after each task or logical group

### Parallel Opportunities

- T009 and T010 (DTOs) can run in parallel
- T014 and T015 (page objects) can run in parallel
- T018 (view model) can run in parallel with T017 (service)
- T024 (view model) can run in parallel with T023 (service)
- US1 and US2 can be developed in parallel
- US4 and US5 both extend US3, so they can be developed in parallel after US3

---

## Parallel Example: Phase 1 Setup

```bash
# These can all be developed in parallel (different files):
Task T001: ApplicationState enum (Domain/Enums/)
Task T002: ItemReviewStatus enum (Domain/Enums/)
Task T005: Items.sql schema (Database/Tables/)
Task T006: SeedData.sql (Database/PostDeployment/)
Task T008: IdentityConfiguration (Infrastructure/Identity/)

# These depend on T001+T002 (use the enums):
Task T003: Item entity changes (depends on T002)
Task T004: Application entity changes (depends on T001)
Task T007: ItemConfiguration (depends on T003)
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2)

1. Complete Phase 1: Setup (domain + schema)
2. Complete Phase 2: Foundational (service + controller shell)
3. Complete Phase 3: US1 — Reviewer sees the queue
4. Complete Phase 4: US2 — Reviewer opens and views application details
5. **STOP and VALIDATE**: Queue and detail view work independently

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 + US2 → Reviewer can browse and view applications (MVP)
3. US3 → Reviewer can make decisions on items
4. US4 + US5 → Technical equivalence and supplier selection (can be parallel)
5. US6 → Send-back flow with applicant feedback loop
6. US7 → Finalization and resolution
7. Polish → Version history, error handling, regression checks

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- E2E tests written before implementation (should fail first)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
