# Tasks: Supplier Evaluation Engine

**Input**: Design documents from `specs/003-supplier-evaluation-engine/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Unit tests for SupplierScore value object (spec requires domain-testable scoring logic). E2E tests per constitution (NON-NEGOTIABLE).

**Organization**: Tasks grouped by user story. US3 (compliance model change) is foundational since US1 and US2 depend on structured compliance data.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new projects needed. Create the ValueObjects directory and verify build.

- [ ] T001 Create directory `src/FundingPlatform.Domain/ValueObjects/`
- [ ] T002 Verify solution builds cleanly with `dotnet build`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema change and domain model updates that ALL user stories depend on. The compliance model change (US3's data layer) must happen first because scoring (US1) and the supplier form (US3's UI) both depend on it.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 Modify database schema: drop `ComplianceStatus` column, add `IsCompliantCCSS BIT NOT NULL DEFAULT 0`, `IsCompliantHacienda BIT NOT NULL DEFAULT 0`, `IsCompliantSICOP BIT NOT NULL DEFAULT 0` in `src/FundingPlatform.Database/dbo/Tables/Suppliers.sql`
- [ ] T004 Modify `Supplier` entity: replace `ComplianceStatus` (string?) property and constructor parameter with three boolean properties `IsCompliantCCSS`, `IsCompliantHacienda`, `IsCompliantSICOP` in `src/FundingPlatform.Domain/Entities/Supplier.cs`
- [ ] T005 Update `SupplierConfiguration`: remove `ComplianceStatus` mapping, add mappings for `IsCompliantCCSS`, `IsCompliantHacienda`, `IsCompliantSICOP` (all `IsRequired()`) in `src/FundingPlatform.Infrastructure/Persistence/Configurations/SupplierConfiguration.cs`
- [ ] T006 Update `SupplierDto`: replace `ComplianceStatus` (string?) with `IsCompliantCCSS` (bool), `IsCompliantHacienda` (bool), `IsCompliantSICOP` (bool) in `src/FundingPlatform.Application/DTOs/SupplierDto.cs`
- [ ] T007 Update `AddSupplierQuotationCommand`: replace `ComplianceStatus` (string?) with `IsCompliantCCSS` (bool), `IsCompliantHacienda` (bool), `IsCompliantSICOP` (bool) in `src/FundingPlatform.Application/Applications/Commands/AddSupplierQuotationCommand.cs`
- [ ] T008 Update `ApplicationService.AddSupplierQuotationAsync`: pass the three compliance booleans to `Supplier` constructor instead of `ComplianceStatus` in `src/FundingPlatform.Application/Services/ApplicationService.cs`
- [ ] T009 Create `SupplierScore` value object with `ComputeForItem` static method in `src/FundingPlatform.Domain/ValueObjects/SupplierScore.cs`. Accepts list of (Quotation, Supplier) pairs, computes score 0-5 per quotation (CCSS + Hacienda + SICOP + E-Invoice + lowest price), determines IsRecommended and IsPreSelected, returns sorted by score descending then supplier ID ascending.
- [ ] T010 Verify solution builds cleanly with `dotnet build` after all foundational changes

**Checkpoint**: Domain model updated, SupplierScore ready. Build passes. Ready for user story implementation.

---

## Phase 3: User Story 3 - Applicant Creates Supplier with Compliance Details (Priority: P1)

**Goal**: Applicants see three compliance checkboxes (CCSS, Hacienda, SICOP) instead of a free-text field when adding suppliers.

**Independent Test**: Create a supplier with various compliance combinations, verify booleans are persisted and displayed correctly.

### Implementation for User Story 3

- [ ] T011 [US3] Update `AddSupplierViewModel`: replace `ComplianceStatus` (string?, MaxLength 200) with three bool properties `IsCompliantCCSS`, `IsCompliantHacienda`, `IsCompliantSICOP` with display names "CCSS Compliance", "Hacienda Compliance", "SICOP Registration" in `src/FundingPlatform.Web/ViewModels/AddSupplierViewModel.cs`
- [ ] T012 [US3] Update `SupplierController.Add` (POST): pass three compliance booleans from model to `AddSupplierQuotationCommand` instead of `ComplianceStatus` in `src/FundingPlatform.Web/Controllers/SupplierController.cs`
- [ ] T013 [US3] Update supplier form view: replace the single `ComplianceStatus` text input with three checkboxes for CCSS, Hacienda, and SICOP in `src/FundingPlatform.Web/Views/Supplier/Add.cshtml`
- [ ] T014 [US3] Update `SupplierPage` page object: replace `ComplianceStatusInput` locator with `IsCompliantCCSSCheckbox`, `IsCompliantHaciendaCheckbox`, `IsCompliantSICOPCheckbox` locators. Update `FillSupplierFormAsync` to accept compliance booleans in `tests/FundingPlatform.Tests.E2E/PageObjects/SupplierPage.cs`
- [ ] T015 [US3] Update existing E2E tests that create suppliers (e.g., `SupplierTests.cs`, `ApplicationSubmissionTests.cs`) to use the new compliance checkboxes instead of `ComplianceStatus` text input in `tests/FundingPlatform.Tests.E2E/Tests/`
- [ ] T016 [US3] Add E2E test: applicant creates supplier with CCSS and Hacienda checked but not SICOP, verify booleans persisted correctly in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`

**Checkpoint**: Supplier form shows three compliance checkboxes. Existing supplier tests pass with updated inputs.

---

## Phase 4: User Story 1 - Reviewer Sees Scored and Ranked Supplier Quotations (Priority: P1)

**Goal**: Reviewers see a composite score (0-5) with factor breakdown for each quotation, sorted by score. Top scorer is recommended and pre-selected.

**Independent Test**: Create application with multiple items and suppliers with varying compliance/pricing, submit, log in as reviewer, verify scores, ranking, recommendation badges, and pre-selection.

### Unit Tests for User Story 1

- [ ] T017 [P] [US1] Create `SupplierScoreTests` unit test class covering: single quotation scores (gets price point), multiple quotations with varying compliance, price tie handling, all-identical scores, zero-compliance scoring, recommendation flag, pre-selection tie-breaking by lowest supplier ID in `tests/FundingPlatform.Tests.Unit/Domain/SupplierScoreTests.cs`

### Implementation for User Story 1

- [ ] T018 [US1] Update `ReviewQuotationDto`: add `Score` (int), `ScoreCCSS` (bool), `ScoreHacienda` (bool), `ScoreSICOP` (bool), `ScoreElectronicInvoice` (bool), `ScoreLowestPrice` (bool), `IsPreSelected` (bool). Keep `IsRecommended` (now score-based). Remove `RecommendedSupplierId` from `ReviewItemDto` in `src/FundingPlatform.Application/DTOs/ReviewApplicationDto.cs`
- [ ] T019 [US1] Update `ReviewService.MapToReviewDto`: replace `ComputeRecommendedSupplierId` with `SupplierScore.ComputeForItem`. Map score results to `ReviewQuotationDto` fields. Sort quotations by score descending in `src/FundingPlatform.Application/Services/ReviewService.cs`
- [ ] T020 [US1] Update `ReviewQuotationViewModel`: add `Score` (int), `ScoreCCSS` (bool), `ScoreHacienda` (bool), `ScoreSICOP` (bool), `ScoreElectronicInvoice` (bool), `ScoreLowestPrice` (bool), `IsPreSelected` (bool). Remove `RecommendedSupplierId` from `ReviewItemViewModel` in `src/FundingPlatform.Web/ViewModels/ReviewApplicationViewModel.cs`
- [ ] T021 [US1] Update `ReviewController.MapToViewModel`: map new score fields from DTOs to ViewModels. Remove `RecommendedSupplierId` mapping in `src/FundingPlatform.Web/Controllers/ReviewController.cs`
- [ ] T022 [US1] Update review screen quotation table: add Score column showing "N/5", add score breakdown indicators (checkmarks for each factor), sort quotations by score descending, update "Recommended" badge to show score-based recommendation, update supplier dropdown to pre-select highest scorer in `src/FundingPlatform.Web/Views/Review/Review.cshtml`
- [ ] T023 [US1] Update `ReviewApplicationPage` page object: add locators for score display (`.supplier-score`), score breakdown (`.score-breakdown`), pre-selected supplier detection in `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs`
- [ ] T024 [US1] Add E2E test: three suppliers with different compliance levels and prices, verify scores displayed correctly, ordered by score, top scorer recommended and pre-selected in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`
- [ ] T025 [US1] Add E2E test: two suppliers tie for highest score, verify both marked recommended, lower supplier ID pre-selected in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`
- [ ] T026 [US1] Add E2E test: single quotation on item, verify it gets price point, is recommended and pre-selected in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`

**Checkpoint**: Review screen shows scores with breakdown. Quotations ranked by score. Top scorer recommended and pre-selected. Unit tests and E2E tests pass.

---

## Phase 5: User Story 2 - Reviewer Overrides Recommended Supplier (Priority: P1)

**Goal**: Reviewer can select a non-recommended supplier and approve, with the override persisting.

**Independent Test**: Open item with pre-selected supplier, select different one, approve, re-open and verify override persists.

### Implementation for User Story 2

- [ ] T027 [US2] Verify existing approval flow respects reviewer's supplier selection when it differs from pre-selected. If `Item.SelectedSupplierId` is already set, the dropdown should show the reviewer's prior choice (not the score-based pre-selection). Adjust `ReviewService` and/or Review.cshtml if needed in `src/FundingPlatform.Application/Services/ReviewService.cs` and `src/FundingPlatform.Web/Views/Review/Review.cshtml`
- [ ] T028 [US2] Add E2E test: reviewer overrides pre-selected supplier, approves item, verify non-recommended supplier saved as selected in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`
- [ ] T029 [US2] Add E2E test: reviewer overrides selection, navigates away and back, verify override persists (manually selected supplier still shown, not reverted to recommendation) in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs`

**Checkpoint**: Reviewer can override recommendations without friction. Overrides persist across page reloads. E2E tests pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and validation across all stories.

- [ ] T030 Update existing review E2E tests (`ReviewApplicationTests.cs`, `FinalizeReviewTests.cs`) that reference the old "Recommended - lowest price" badge or `RecommendedSupplierId` to work with the new score-based recommendation in `tests/FundingPlatform.Tests.E2E/Tests/`
- [ ] T031 Run full test suite: `dotnet test` across all test projects (Unit, Integration, E2E). Fix any regressions.
- [ ] T032 Run quickstart.md validation: verify all 6 verification steps pass against running Aspire stack

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies -- can start immediately
- **Foundational (Phase 2)**: Depends on Setup -- BLOCKS all user stories
- **US3 (Phase 3)**: Depends on Foundational -- supplier form changes
- **US1 (Phase 4)**: Depends on Foundational -- scoring on review screen
- **US2 (Phase 5)**: Depends on US1 -- override behavior needs scoring UI to exist
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 3 (Compliance Form)**: Can start after Foundational (Phase 2). Independent of US1/US2.
- **User Story 1 (Scoring Display)**: Can start after Foundational (Phase 2). Independent of US3 (they modify different views).
- **User Story 2 (Override)**: Depends on US1 completion (needs scoring UI and pre-selection to exist before testing overrides).

**US3 and US1 can run in parallel** after Foundational phase completes.

### Within Each User Story

- Models/DTOs before services
- Services before views
- Views before E2E tests
- Commit after each task or logical group

### Parallel Opportunities

- T003, T004, T005 can run in parallel (different files: .sql, .cs entity, .cs config)
- T006, T007 can run in parallel (different DTO files)
- T011, T012, T013 within US3 are sequential (ViewModel → Controller → View)
- T017 (unit tests) can run in parallel with T018-T022 (write tests while implementing)
- US3 (Phase 3) and US1 (Phase 4) can run in parallel after Foundational completes

---

## Parallel Example: After Foundational Phase

```
# These two phases can run concurrently:

# Thread A: User Story 3 (Supplier Form)
Task T011: Update AddSupplierViewModel
Task T012: Update SupplierController
Task T013: Update Add.cshtml view
Task T014-T016: Update page objects and E2E tests

# Thread B: User Story 1 (Scoring Display)
Task T017: Write unit tests (can start immediately)
Task T018: Update ReviewQuotationDto
Task T019: Update ReviewService
Task T020-T022: Update ViewModels, Controller, View
Task T023-T026: Update page objects and E2E tests
```

---

## Implementation Strategy

### MVP First (User Story 3 + User Story 1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL -- blocks all stories)
3. Complete Phase 3: User Story 3 (compliance form)
4. Complete Phase 4: User Story 1 (scoring display)
5. **STOP and VALIDATE**: Scores visible on review screen, supplier form has checkboxes
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational -> Domain model ready
2. Add US3 -> Compliance checkboxes working -> Deploy/Demo
3. Add US1 -> Scoring visible on review screen -> Deploy/Demo (MVP!)
4. Add US2 -> Override behavior verified -> Deploy/Demo
5. Polish -> All tests green, no regressions

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Constitution requires E2E tests for every user story -- these are NOT optional
