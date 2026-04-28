---
description: "Tasks for 010-admin-reports"
---

# Tasks: Admin Reports Module

**Input**: Design documents in `/specs/010-admin-reports/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. Constitution Principle III makes Playwright E2E tests non-negotiable. FR-033 mandates E2E coverage for every user story (currency rollout, dashboard, four detail reports, Tabler shell). Tests are written **before** implementation per TDD; expected red until the corresponding implementation tasks land.

**Organization**: Tasks grouped by user story (US1, US2, US3, US4, US5, US6, US7) per spec.md priority order. Each phase is a checkpoint at which build, the existing E2E suite (specs 001–009), and the spec invariants for the work landed so far must all be green.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3, US4, US5, US6, US7 — maps to user stories in spec.md
- File paths are absolute-from-repo-root

## Path Conventions

- Web application layout per plan.md §Project Structure
- `src/FundingPlatform.Domain/Entities/Quotation.cs` — extended with `Currency`
- `src/FundingPlatform.Domain/ValueObjects/CurrencyAmount.cs` — new value object
- `src/FundingPlatform.Application/Admin/Reports/` — new directory: `IAdminReportsService`, `AdminReportsService`, DTOs
- `src/FundingPlatform.Application/Interfaces/IReportQueryService.cs` — new
- `src/FundingPlatform.Application/Exceptions/CsvRowBoundExceededException.cs` — new
- `src/FundingPlatform.Infrastructure/Persistence/Reports/ReportQueryService.cs` — new
- `src/FundingPlatform.Infrastructure/Persistence/Configurations/QuotationConfiguration.cs` — modified (Currency mapping)
- `src/FundingPlatform.Database/Tables/dbo.Quotations.sql` — modified (Currency column)
- `src/FundingPlatform.Database/PostDeployment/SeedData.sql` — modified (DefaultCurrency row + backfill)
- `src/FundingPlatform.Database/PostDeployment/SeedData_TightenQuotationCurrency.sql` — new
- `src/FundingPlatform.Database/FundingPlatform.Database.sqlproj` — modified (PostDeploy registration)
- `src/FundingPlatform.Web/Configuration/AdminReportsOptions.cs` — new
- `src/FundingPlatform.Web/Program.cs` — modified (binding + startup probe)
- `src/FundingPlatform.Web/Controllers/Admin/AdminReportsController.cs` — extended (5 list + 4 export actions)
- `src/FundingPlatform.Web/ViewModels/Admin/Reports/` — new directory: per-report view-models + KpiTile
- `src/FundingPlatform.Web/Views/Admin/Reports/` — modified Index + 4 new views
- `src/FundingPlatform.Web/Views/Shared/Components/_KpiTile.cshtml` — new
- `src/FundingPlatform.Web/Views/Shared/Components/_ReportSubTabs.cshtml` — new
- `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementItemsTable.cshtml` — modified (currency-code render)
- `src/FundingPlatform.Web/Views/Quotation/Add.cshtml` and `Edit.cshtml` — modified (Currency field)
- `src/FundingPlatform.Web/ViewModels/AddQuotationViewModel.cs` — modified (Currency property)
- `src/FundingPlatform.Web/Helpers/ReportFilterEncoder.cs` — new
- `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/` — new PageObjects per surface
- `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/` — new test classes per user story
- `tests/FundingPlatform.Tests.E2E/Helpers/CsvAssertions.cs` and `FundingAgreementPdfAssertions.cs` — new helpers

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify a clean baseline build + green E2E suite before any 010 changes land. Capture the existing seed-data state. No production-code edits.

- [X] T001 Run `dotnet build --nologo` at the repo root and confirm zero errors and zero warnings on branch `010-admin-reports` before making any changes; this is the baseline against which every subsequent task is measured.
- [X] T002 [P] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm zero failures on the baseline branch; this is the green starting state that every subsequent task must preserve. Save the test summary line for comparison.
- [X] T003 [P] Document the existing seeded SystemConfiguration keys (`MinQuotationsPerItem`, `AllowedFileTypes`, `MaxFileSizeMB`, `MaxAppealsPerApplication` per `src/FundingPlatform.Database/PostDeployment/SeedData.sql`) and confirm none clash with the new `DefaultCurrency` key being introduced. Capture the existing demo applicant accounts so the dashboard's "Active applicants" KPI has predictable input during US2's E2E.
- [X] T004 [P] (Optional fixture capture) Generate a Funding Agreement PDF on a representative seeded application BEFORE this branch's changes land, save as `specs/010-admin-reports/quickstart-fixtures/funding-agreement-before-currency.pdf` for the manual side-by-side comparison documented in `quickstart.md`. Skip if dev environment cannot reach `ResponseFinalized` state on the existing seed.

**Checkpoint**: Baseline build and E2E green. No code changes yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land schema, type, configuration, and EF-mapping changes that every user story depends on. After this phase the build compiles, all existing E2E tests still pass, and the project has the new `Currency` column on `dbo.Quotations` (NOT NULL after backfill), the `Quotation.Currency` property, the cascading constructor changes through `Item.AddQuotation` / `AddSupplierQuotationCommand` / `QuotationDto`, the `CurrencyAmount` value object, the `CsvRowBoundExceededException`, the `AdminReportsOptions` configuration binding, and the startup-fail-fast probe for missing `DefaultCurrency`.

**⚠️ CRITICAL**: No US1–US7 work should begin until this phase is complete. Every report (US2–US6) reads `Quotation.Currency`, every monetary aggregation uses `CurrencyAmount`, and every CSV export depends on the row-bound guard.

### dacpac schema + seed (deployment-order-sensitive)

- [X] T005 Add the nullable `[Currency] NVARCHAR(3) NULL` column to `src/FundingPlatform.Database/Tables/dbo.Quotations.sql` per `data-model.md §"Database table change"`. The column lands as nullable so dacpac can add it to non-empty production tables before the backfill runs. NO change to the existing constraints, indexes, or other columns.
- [X] T006 Append two idempotent blocks to `src/FundingPlatform.Database/PostDeployment/SeedData.sql` per `research.md §6 Block 1 + Block 2`: (1) `IF NOT EXISTS ... INSERT INTO [dbo].[SystemConfigurations] (Key='DefaultCurrency', Value='$(DefaultCurrency)', Description='...', UpdatedAt=GETUTCDATE())`; (2) `UPDATE [dbo].[Quotations] SET [Currency] = (SELECT [Value] FROM [dbo].[SystemConfigurations] WHERE [Key] = N'DefaultCurrency') WHERE [Currency] IS NULL`. Both use `IF NOT EXISTS` / `WHERE ... IS NULL` guards so re-runs are no-ops.
- [X] T007 Create new post-deploy script `src/FundingPlatform.Database/PostDeployment/SeedData_TightenQuotationCurrency.sql` per `research.md §6 step 3` and `data-model.md §"Quotations[Currency] tightening"`. The script reads `sys.columns` to detect whether `Quotations.Currency` is currently nullable and, only then, runs `ALTER TABLE [dbo].[Quotations] ALTER COLUMN [Currency] NVARCHAR(3) NOT NULL`. Idempotent: a second invocation finds the column already NOT NULL and short-circuits.
- [X] T008 Register the new tightening script in `src/FundingPlatform.Database/FundingPlatform.Database.sqlproj` AFTER the existing `<PostDeploy Include="PostDeployment\SeedData.sql" />` line, so dacpac runs them in order: nullable column add (declarative) → SeedData.sql (default-currency row + backfill) → SeedData_TightenQuotationCurrency.sql (idempotent NOT NULL tighten).
- [X] T009 Configure the dacpac publish profile (Aspire deployment configuration) so the `$(DefaultCurrency)` sqlcmd variable is supplied with the same value as `AdminReports:DefaultCurrency`. For dev, set `DefaultCurrency=COP` in the AppHost's Aspire wiring (`src/FundingPlatform.AppHost/AppHost.cs` or its associated config). For E2E, the Aspire fixture passes the variable. For production, the deploy pipeline supplies it from the same source as the application config.

### Domain types

- [X] T010 [P] Create `src/FundingPlatform.Domain/ValueObjects/CurrencyAmount.cs` per `data-model.md §"CurrencyAmount (new value object)"`. Implementation: `public sealed record CurrencyAmount(string Currency, decimal Amount)` with a primary-constructor invariant that throws `ArgumentException` when `Currency` is null/whitespace or its length is not exactly 3, and uppercase-canonicalizes (`Currency.Trim().ToUpperInvariant()`) on construction. Living in namespace `FundingPlatform.Domain.ValueObjects`. Pure value object; no EF mapping.
- [X] T011 [P] Create `src/FundingPlatform.Application/Exceptions/CsvRowBoundExceededException.cs` per `data-model.md §"CsvRowBoundExceededException"`. Carries `int Limit`, `int ActualCount`, message `"CSV export refused: {actualCount} rows exceeds the configured limit of {limit}. Narrow your filter and try again."`. No HTTP-aware fields; the controller surfaces the response shape.
- [X] T012 Modify `src/FundingPlatform.Domain/Entities/Quotation.cs` to add the `Currency` property and cascading constructor change per `data-model.md §"Quotation"`. Add `public string Currency { get; private set; } = string.Empty;`. Change the public constructor signature to `public Quotation(int supplierId, int documentId, decimal price, DateOnly validUntil, string currency)`. Validate `currency` length-equals-3 and canonicalize-uppercase in the constructor body before assignment. Add `public void EditCurrency(string code)` mutator with the same validation. The existing `ReplaceDocument(int newDocumentId)` mutator is unchanged.
- [X] T013 Cascade the `currency` argument through `src/FundingPlatform.Domain/Entities/Item.cs::AddQuotation`. Change signature from `AddQuotation(Supplier supplier, Document document, decimal price, DateOnly validUntil)` to `AddQuotation(Supplier supplier, Document document, decimal price, DateOnly validUntil, string currency)` and pass `currency` through to the `new Quotation(...)` invocation. Existing duplicate-supplier guard unchanged.
- [X] T014 Cascade through `src/FundingPlatform.Application/Applications/Commands/AddSupplierQuotationCommand.cs`. Add `string Currency` to the command record and pass it into the `Item.AddQuotation(...)` call. Update any associated handler to forward the value. Default currency at the command-construction layer is `null` (caller MUST supply); validation handles the absence with a friendly model-state error in the form.
- [X] T015 Cascade through `src/FundingPlatform.Application/DTOs/QuotationDto.cs`. Add `string Currency { get; init; }` (or equivalent for the project's record/class style). Update any constructor / mapper that materializes the DTO to populate `Currency` from `Quotation.Currency`.
- [X] T016 Modify `src/FundingPlatform.Infrastructure/Persistence/Configurations/QuotationConfiguration.cs` to add the `Currency` property mapping. EF Core will map by convention (column name = property name) and the schema declares the column NVARCHAR(3); add an explicit `.HasColumnType("NVARCHAR(3)")` and `.HasMaxLength(3).IsRequired()` to lock the contract in code as well. Existing mappings preserved.

### Web/configuration plumbing

- [X] T017 [P] Create `src/FundingPlatform.Web/Configuration/AdminReportsOptions.cs` per `data-model.md §"AdminReportsOptions"`. `public sealed class AdminReportsOptions { public const string SectionName = "AdminReports"; public string? DefaultCurrency { get; set; } public int CsvRowLimit { get; set; } = 50_000; }`. Living in namespace `FundingPlatform.Web.Configuration` (or `FundingPlatform.Application.Options` if that namespace is the existing convention — confirm at implementation time and align with spec 005's `FundingAgreementOptions` placement).
- [X] T018 Modify `src/FundingPlatform.Web/Program.cs` to bind `AdminReportsOptions` from configuration AND add the startup-fail-fast probe per `research.md §3` and FR-007. (1) `builder.Services.Configure<AdminReportsOptions>(builder.Configuration.GetSection(AdminReportsOptions.SectionName));`. (2) BEFORE `builder.Build()`, read the value: `var defaultCurrency = builder.Configuration[$"{AdminReportsOptions.SectionName}:DefaultCurrency"];` (3) If `string.IsNullOrWhiteSpace(defaultCurrency)` OR `defaultCurrency.Length != 3`, throw `InvalidOperationException("AdminReports:DefaultCurrency is required and must be a 3-character currency code (e.g., 'COP', 'USD'). Set the configuration value before starting the host.")`. No fallback default; no log-and-continue.
- [X] T019 Add `AdminReports:DefaultCurrency` to the development configuration in `src/FundingPlatform.Web/appsettings.Development.json` (or user-secrets, whichever the project's existing convention is). Set the value to `"COP"` for dev convenience. Add `AdminReports:CsvRowLimit` only if a non-default is desired; otherwise rely on the in-code default of 50,000.

### DI wiring (registration only — implementations land per user story)

- [X] T020 Modify `src/FundingPlatform.Infrastructure/DependencyInjection.cs` to register the new service interfaces with stub implementations: `services.AddScoped<IAdminReportsService, AdminReportsService>();` and `services.AddScoped<IReportQueryService, ReportQueryService>();`. The implementations are added skeleton-first in T021/T022 below so the build passes; their methods throw `NotImplementedException` until each user story fleshes them out. This split lets US2–US6 land in parallel without DI churn.
- [X] T021 [P] Create `src/FundingPlatform.Application/Admin/Reports/IAdminReportsService.cs` interface skeleton per `contracts/README.md §"IAdminReportsService"`. All nine method signatures present (`GetDashboardAsync`, `ListApplicationsAsync`, `ListApplicantsAsync`, `ListFundedItemsAsync`, `ListAgingApplicationsAsync`, `ExportApplicationsCsvAsync`, `ExportApplicantsCsvAsync`, `ExportFundedItemsCsvAsync`, `ExportAgingApplicationsCsvAsync`). DTOs do NOT exist yet — declare them as TODO comments so the file compiles only after the per-story DTO tasks land. Alternatively: create the interface with the empty marker types referenced as `object` placeholders, then refine in each user story. The simpler path is a single foundational task that creates ALL DTO types as empty records and lets each user story populate them; we do that in T022.
- [X] T022 Create empty placeholder DTO records in `src/FundingPlatform.Application/Admin/Reports/DTOs/` so the interface from T021 compiles: `DateRange.cs` (full record per `data-model.md`), `DashboardResult.cs` (empty placeholder), `ListApplicationsRequest.cs`/`Result.cs`/`ApplicationRowDto.cs` (empty), and the same trio for Applicants, FundedItems, AgingApplications. Each per-story phase fills in its DTO bodies. Empty record = `public sealed record TName();` so the type compiles.
- [X] T023 [P] Create `src/FundingPlatform.Application/Interfaces/IReportQueryService.cs` interface skeleton per `contracts/README.md §"IReportQueryService"`. Five methods: `ApplicationsQuery`, `ApplicantsQuery`, `FundedItemsQuery`, `AgingApplicationsQuery`, `DashboardSnapshotAsync`. Method bodies are populated in user-story phases.
- [X] T024 Create stub `src/FundingPlatform.Application/Admin/Reports/Services/AdminReportsService.cs`. Implements `IAdminReportsService` with every method throwing `NotImplementedException("Implemented in user-story phase.")`. Constructor takes `IReportQueryService`, `IOptions<AdminReportsOptions>`, `ILogger<AdminReportsService>`. This is the file user-story phases will fill in.
- [X] T025 Create stub `src/FundingPlatform.Infrastructure/Persistence/Reports/ReportQueryService.cs`. Implements `IReportQueryService` with every method throwing `NotImplementedException`. Constructor takes `AppDbContext`. User-story phases populate each query.

### Build & test gate

- [X] T026 Run `dotnet build --nologo` and confirm the entire solution compiles. Zero errors, zero warnings (consistent with T001's baseline). The interface skeletons + DTO placeholders + DI wiring must compile without the user-story implementations being present.
- [X] T027 Publish the dacpac (`dotnet publish src/FundingPlatform.Database` or via Aspire if AppHost orchestrates publish). Verify the new `Currency` column exists on `dbo.Quotations` and is `NOT NULL` after the post-deploy scripts run, by querying `INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Quotations' AND COLUMN_NAME='Currency'`.
- [X] T028 Run the existing E2E suite (`dotnet test tests/FundingPlatform.Tests.E2E`). All previous-spec tests must still pass — the constructor signature change on `Quotation` and `Item.AddQuotation` cascades through call sites that the existing tests exercise; if any test fails, fix the call sites in test setup helpers before proceeding.

**Checkpoint**: Foundational layer ready. Build clean, dacpac applied, every existing test green. User stories US1–US7 can now land in parallel by file (though sequentially in phase order makes for cleaner reviews).

---

## Phase 3: User Story 1 — Per-quotation Currency lands platform-wide (Priority: P1) 🎯 MVP gate

**Story**: spec.md US1. **Goal**: every new quotation captures a currency, the Funding Agreement PDF renders the code beside every amount, and the missing-config startup probe protects the deploy invariant. Foundational schema + cascade has already shipped in Phase 2; this phase adds the user-facing surfaces and the E2E coverage.

**Independent Test**: Set `DefaultCurrency = "COP"`. Open the quotation create form; confirm the Currency field is prefilled with `COP` and is editable. Create a `COP` quotation and a `USD` quotation on the same application. Generate the Funding Agreement PDF; confirm both amounts render with their currency codes. Restart the host with `DefaultCurrency` removed; confirm the host throws on `Build()`.

### Tests for User Story 1 (TDD — write FIRST, expected red)

- [X] T029 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/QuotationCreatePage.cs` (NEW: this is a foundational PageObject reused by spec 001's existing tests; the new Currency field is a property + accessor on this page). If a `QuotationCreatePage` already exists, extend it with `IPageLocator CurrencyInput`, `Task EnterCurrency(string code)`, `Task<string> ReadCurrencyValue()`. Otherwise create the PageObject from scratch.
- [X] T030 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/Helpers/FundingAgreementPdfAssertions.cs` per `research.md §4 step 2`. Public static methods: `AssertEachAmountHasCurrencyCode(byte[] pdfBytes, IReadOnlyCollection<string> expectedCurrencies)` (reads PDF text via Syncfusion's text-extractor or a small wrapper around `iText7`; the Syncfusion package is already on the project's path), and `AssertNoBareDecimalAmounts(byte[] pdfBytes)`. Document any text-extraction whitespace tolerances inline.
- [X] T031 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/CurrencyRolloutTests.cs` with the following test methods (each covers one acceptance scenario from spec.md US1):
  - `QuotationCreateForm_PrefillsConfiguredDefaultCurrency` (FR-004; spec.md US1 AS1)
  - `QuotationCreateForm_RejectsCurrencyOfWrongLength` (FR-004; spec.md US1 AS2; expected error visible alongside any other form errors)
  - `FundingAgreementPdf_RendersCurrencyCodeBesideEveryAmount` (FR-005; spec.md US1 AS3; mixed-currency input asserted via the helper)
  - `LegacyQuotations_BackfilledWithDefaultCurrencyAfterDacpacUpgrade` (FR-003; spec.md US1 AS4; runs against a database snapshot or freshly-published dacpac that previously had no Currency column)
  - `Host_RefusesToStartWhenDefaultCurrencyMissing` (FR-007; spec.md US1 AS5; uses a dedicated AspireFixture configuration that omits `AdminReports:DefaultCurrency` and asserts host throws on `Build()`)
  - `PreExistingFundingAgreementPdf_NotRegeneratedOnDownload` (spec.md US1 AS6 / edge case; uploads or seeds a pre-currency PDF and confirms the bytes are unchanged on download)
- [X] T032 [US1] Run the new test class: `dotnet test --filter "FullyQualifiedName~CurrencyRolloutTests"`. Confirm all six test methods FAIL with reasons that map to the missing implementation (form has no Currency field, template renders bare decimals, etc.). Save the failing test summary so the post-implementation green run is verifiable.

### Implementation for User Story 1

- [X] T033 [P] [US1] Modify `src/FundingPlatform.Web/ViewModels/AddQuotationViewModel.cs` to add the `Currency` property: `[Required] [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character code.")] public string Currency { get; set; } = string.Empty;`. The default value is bound to `IOptions<AdminReportsOptions>.DefaultCurrency` in the controller before render (NOT hard-coded in the view-model).
- [X] T034 [P] [US1] Modify `src/FundingPlatform.Web/Views/Quotation/Add.cshtml` to add an input field for Currency. Use the existing `_FormSection` partial pattern from spec 008. Field shape: small text input, max-length 3, helper text "ISO 4217 (e.g., COP, USD)". Pre-population is handled by the controller setting `Model.Currency` before render. NO inline `style=` attributes.
- [X] T035 [P] [US1] Modify `src/FundingPlatform.Web/Views/Quotation/Edit.cshtml` similarly. The edit form must allow changing the Currency on an existing quotation; the validation behavior matches Add.cshtml.
- [X] T036 [US1] Modify `src/FundingPlatform.Web/Controllers/QuotationController.cs` (or whichever controller hosts the quotation Add/Edit actions) to inject `IOptions<AdminReportsOptions>` and prefill `viewModel.Currency = options.Value.DefaultCurrency` BEFORE returning the GET view. On POST, the value is bound from the form; pass it through to `AddSupplierQuotationCommand` (and the equivalent edit command if one exists). Validation errors surface together (FR-004 / constitution VI consistency).
- [X] T037 [P] [US1] Modify `src/FundingPlatform.Web/Views/FundingAgreement/Partials/_FundingAgreementItemsTable.cshtml` so each amount cell renders the currency code beside the value. The exact render expression depends on the model shape; a representative form is `@($"{item.Currency} {item.Price.ToString("N", LatamCulture)}")`. Sub-totals (if any) likewise. The existing column widths and styling are preserved; this is a token-level change inside the cell, not a structural change.
- [X] T038 [US1] Run the test class again: `dotnet test --filter "FullyQualifiedName~CurrencyRolloutTests"`. All six test methods MUST now pass. If `Host_RefusesToStartWhenDefaultCurrencyMissing` fails, the issue is in T018's startup-probe code; if `LegacyQuotations_BackfilledWithDefaultCurrencyAfterDacpacUpgrade` fails, the issue is in T006/T007's ordering or idempotency.
- [X] T039 [US1] Run the full E2E suite (`dotnet test tests/FundingPlatform.Tests.E2E`). Confirm zero failures: all spec 001–009 tests + the six new US1 tests are green. The constructor cascade in Phase 2 may have surfaced fixture call-sites that need a `Currency` argument — fix them in the test setup helpers, NOT by changing production code.
- [X] T040 [US1] Manual smoke per `quickstart.md §"US1"`. Walk every check from §1.1 through §1.6. If §1.4's PDF visual comparison (against `funding-agreement-before-currency.pdf` if captured in T004) reveals a layout regression, return to T037 and adjust the template until parity is restored.

**Checkpoint**: User Story 1 is complete and independently testable. Currency is captured on every new quotation, the PDF renders codes beside amounts, legacy data is backfilled, and the host fails fast on misconfiguration. The remaining six user stories add the report content; none of them require US1 to ship first beyond the foundational schema laid in Phase 2 (which already shipped).

---

## Phase 4: User Story 2 — Admin opens the dashboard at /Admin/Reports (Priority: P1)

**Story**: spec.md US2. **Goal**: replace the spec-009 stub `/Admin/Reports` page with the real dashboard — three KPI rows (Pipeline / Financial / Applicant) + horizontal sub-tab strip + global date-range picker. Pipeline tiles always reflect current state; Financial and "New this period" applicant tiles scope to the date-range picker.

**Independent Test**: Land on `/Admin/Reports` as an Admin on a seeded environment with at least two currencies and applications across multiple states. Confirm three KPI rows render with non-empty values. Confirm the date-range picker defaults to last 30 days. Change the picker to last 7 days; confirm only the period-scoped tiles recompute. Click each sub-tab entry; confirm each lands on its detail report.

### Tests for User Story 2

- [X] T041 [P] [US2] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/AdminReportsDashboardPage.cs` with locators for: each KPI tile (by data-testid), the global date-range picker (`from`, `to` inputs, submit button), and each sub-tab strip entry. Methods: `Goto()`, `ReadPipelineTile(state)`, `ReadFinancialTileStack(label)` (returns `IReadOnlyList<(string Currency, decimal Amount)>`), `ReadApplicantTile(label)`, `SetDateRange(from, to)`, `ClickSubTab(name)`.
- [X] T042 [P] [US2] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsDashboardTests.cs` with test methods (one per acceptance scenario):
  - `Dashboard_RendersThreeKpiRowsAndSubTabStrip` (spec.md US2 AS1)
  - `Dashboard_RendersFinancialTilesAsPerCurrencyStacks` (spec.md US2 AS2; FR-014)
  - `Dashboard_DateRangePickerScopesPeriodTilesOnly` (spec.md US2 AS3; FR-013)
  - `Dashboard_EmptyDatabase_RendersZerosWithoutError` (spec.md US2 AS4; FR-015)
  - `Dashboard_NonAdmin_Returns403` (spec.md US2 AS5; SC-006)
  - `Dashboard_SubTabsNavigateToDetailReports` (FR-010)
- [X] T043 [US2] Run the test class. Confirm all six methods FAIL.

### Implementation for User Story 2

- [X] T044 [P] [US2] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/DashboardResult.cs` per `contracts/README.md §"DashboardResult"` and `data-model.md`. Records: `DashboardResult(IReadOnlyList<PipelineCount> Pipeline, IReadOnlyList<FinancialKpi> Financial, IReadOnlyList<ApplicantKpi> Applicants)`, `PipelineCount(ApplicationState State, int Count)`, `FinancialKpi(string Label, IReadOnlyList<CurrencyAmount> Stack)`, `ApplicantKpi(string Label, int Count)`.
- [X] T045 [US2] Implement `DashboardSnapshotAsync` in `src/FundingPlatform.Infrastructure/Persistence/Reports/ReportQueryService.cs` per `data-model.md §"Aggregations"`. ONE batched EF query that produces:
  - Pipeline counts per state (excluding Draft) via `_db.Applications.Where(a => a.State != Draft).GroupBy(a => a.State).Select(g => new { State = g.Key, Count = g.Count() })`.
  - Financial Approved this period (per currency): join `Items.Where(ReviewStatus = Approved && parent.UpdatedAt within range).Quotations.Where(SupplierId == SelectedSupplierId).GroupBy(currency).Sum(price)`.
  - Financial Executed this period (per currency): same join plus `parent.State == AgreementExecuted`.
  - Pending = Approved minus Executed (post-projection in C#).
  - Active applicants: count of distinct applicants with at least one application in non-terminal state.
  - Repeat applicants: applicants with `Applications.Where(SubmittedAt != null).Count() >= 2`.
  - New this period: applicants whose `Applications.Min(SubmittedAt)` falls within the range.
  Return as a single `DashboardResult`. Use `.AsNoTracking()`.
- [X] T046 [US2] Implement `GetDashboardAsync` in `src/FundingPlatform.Application/Admin/Reports/Services/AdminReportsService.cs`. Default the `DateRange` to `(today - 30 days, today)` when null. Delegate to `IReportQueryService.DashboardSnapshotAsync(range, ct)`.
- [X] T047 [P] [US2] Create `src/FundingPlatform.Web/ViewModels/Admin/Reports/DashboardViewModel.cs` and `src/FundingPlatform.Web/ViewModels/Admin/Reports/KpiTileViewModel.cs` per `contracts/README.md` and `data-model.md`. `KpiTileViewModel(string Label, int? NumericValue, IReadOnlyList<CurrencyAmount>? Stack)`.
- [X] T048 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_KpiTile.cshtml` per `plan.md §"new partial"` and FR-029. Renders a Tabler card with the label, the optional numeric value, and the optional per-currency stack (one line per `CurrencyAmount` rendered as `{Currency} {Amount}`). Empty stack renders one line with em-dash. NO inline styles. Reusable outside reports.
- [X] T049 [P] [US2] Create `src/FundingPlatform.Web/Views/Shared/Components/_ReportSubTabs.cshtml` per `contracts/README.md §"Sub-tab strip"`. Takes `string ActiveTab` (one of `"Dashboard"`, `"Applications"`, `"Applicants"`, `"FundedItems"`, `"Aging"`). Renders `<ul class="nav nav-tabs">` with four entries; the active tab gets the `active` class.
- [X] T050 [US2] Modify `src/FundingPlatform.Web/Controllers/Admin/AdminReportsController.cs`. Replace the current single `Index()` action with a new signature: `public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)`. Default the range to last-30-days if both inputs are null. Inject `IAdminReportsService`. Build the `DashboardViewModel` from `service.GetDashboardAsync(...)`. Return `View(dashboardViewModel)`. Existing `[Authorize(Roles="Admin")]` and `[Route("Admin/Reports")]` preserved.
- [X] T051 [US2] Modify `src/FundingPlatform.Web/Views/Admin/Reports/Index.cshtml` per `plan.md §"Views/Admin/Reports/Index.cshtml"`. Replace the spec-009 stub. Render: `_PageHeader` (title "Reports", subtitle the active date range), `_ReportSubTabs` with `ActiveTab="Dashboard"`, the global date-range picker (a small inline form with `from` / `to` inputs and a submit button), then the three KPI rows. Each tile is a `_KpiTile` partial invocation. NO inline styles; NO badge markup outside `_StatusPill`.
- [X] T052 [US2] Run the test class: `dotnet test --filter "FullyQualifiedName~AdminReportsDashboardTests"`. All six methods must pass. Run the full E2E suite to confirm specs 001–009 + US1 are still green.
- [X] T053 [US2] Manual smoke per `quickstart.md §"US2"`. Walk §2.1–§2.6.

**Checkpoint**: Dashboard live. Sub-tab strip in place but the four detail-report routes still throw `NotImplementedException` until US3–US6 ship.

---

## Phase 5: User Story 3 — Applications report (Priority: P1)

**Story**: spec.md US3. **Goal**: server-paginated, server-sorted, server-filtered list of every application with applicant identity, current state, transition dates, item count, total approved per currency, and presence-of-agreement / presence-of-active-appeal flags. CSV export of currently-filtered dataset across all pages.

**Independent Test**: Filter to State=`UnderReview` + a known applicant search; confirm only matching rows return. Click "Download CSV"; confirm the file matches the on-screen filter across all pages with per-(application, currency) row expansion. Navigate to page 2; confirm pagination is server-side. Copy the URL with filters; paste in a fresh browser session; confirm the same filtered view renders.

### Tests for User Story 3

- [X] T054 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/AdminReportsApplicationsPage.cs` with: `Goto()`, `ApplyStateFilter(IEnumerable<ApplicationState> states)`, `ApplyDateRange(from, to)`, `ApplySearch(string query)`, `ApplyHasAgreementFilter(bool? value)`, `ApplyHasActiveAppealFilter(bool? value)`, `ClickSort(string sortToken)`, `GoToPage(int n)`, `ReadVisibleRows()` (returns IReadOnlyList<RowSnapshot>), `ClickDownloadCsv()` (returns the downloaded byte[]).
- [X] T055 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/Helpers/CsvAssertions.cs` per `plan.md §"new helper"`. Public static methods: `ParseCsv(byte[] body)` (returns `IReadOnlyList<IReadOnlyDictionary<string, string>>`), `AssertHeaderEquals(parsed, expectedHeader[])`, `AssertEveryRowHasNonEmptyCurrencyColumn(parsed)`, `AssertPerCurrencyExpansion(parsed, entityIdColumn, expectedCurrencyCount)`. Documents UTF-8 BOM tolerance and RFC 4180 quoting.
- [X] T056 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsApplicationsTests.cs` with test methods:
  - `Applications_DefaultsToMostRecentlyUpdatedDescending` (spec.md US3 AS1; FR-024)
  - `Applications_StateAndDateFilter_ProducesDeepLinkableUrl` (spec.md US3 AS2; FR-018)
  - `Applications_CsvExport_ContainsFilteredRowsAcrossAllPages_PerCurrencyExpanded` (spec.md US3 AS3; FR-019/FR-020)
  - `Applications_CsvExport_RefusedWhenRowBoundExceeded` (spec.md US3 AS4; FR-021; uses `AdminReports:CsvRowLimit=5` test fixture override)
  - `Applications_MalformedQuerystring_RendersWithSafeDefaults` (spec.md US3 AS5; FR-022; asserts HTTP 200, NOT 500)
  - `Applications_EmptyResult_RendersEmptyState` (spec.md US3 AS6; FR-023)
  - `Applications_DeepLink_ReproducesFilteredViewInFreshSession` (SC-004)
- [X] T057 [US3] Run the test class. Confirm all seven methods FAIL.

### Implementation for User Story 3

- [X] T058 [P] [US3] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/ListApplicationsRequest.cs` with the full property set per `contracts/README.md §"ListApplicationsRequest"`. Add a constructor / record body that maps from querystring keys (the controller's model-binder handles this via attribute binding; no custom binder needed if property names match keys).
- [X] T059 [P] [US3] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/ListApplicationsResult.cs` and `ApplicationRowDto.cs` per `contracts/README.md §"List result envelope"` and `data-model.md`.
- [X] T060 [P] [US3] Create `src/FundingPlatform.Web/Helpers/ReportFilterEncoder.cs` per `plan.md §"new helper"`. Pure-function helpers `Encode(ListApplicationsRequest req) -> string` and `Decode(string querystring) -> ListApplicationsRequest`. Property names map 1:1 to querystring keys. Used by the view's "Apply filter" button and by `_DataTable`'s pagination links so filter state is preserved across page changes. (The same encoder is reused for the other three reports — they share a base shape; consider a generic helper or one-per-request-DTO depending on the shapes' divergence.)
- [X] T061 [US3] Implement `ApplicationsQuery(req)` in `src/FundingPlatform.Infrastructure/Persistence/Reports/ReportQueryService.cs`. EF projection: `_db.Applications.AsNoTracking().Include(a => a.Applicant).Include(a => a.Items).ThenInclude(i => i.Quotations).Include(a => a.Appeals).Include(a => a.FundingAgreement).Where(...all filter predicates...).Select(a => new ApplicationRowDto { ... })`. The per-currency stack is computed in C# post-projection (group selected-quotation prices by currency on the items already loaded). Returns `IQueryable<ApplicationRowDto>` so the outer service can call `Skip/Take/OrderBy/CountAsync/AsAsyncEnumerable`.
- [X] T062 [US3] Implement `ListApplicationsAsync(req, ct)` in `AdminReportsService.cs`. Steps: get the IQueryable from `IReportQueryService`, apply sort token resolution via a small `switch` → `OrderBy/OrderByDescending`, count total via `await query.CountAsync(ct)`, materialize the page via `await query.Skip(...).Take(PageSize).ToListAsync(ct)`, return `ListApplicationsResult(rows, total, req)`.
- [X] T063 [US3] Implement `ExportApplicationsCsvAsync(req, ct)` in `AdminReportsService.cs`. Skip pagination. Run `var actualCount = await query.CountAsync(ct);` BEFORE streaming. If `actualCount > options.CsvRowLimit`, throw `CsvRowBoundExceededException(options.CsvRowLimit, actualCount)`. Otherwise, stream rows via `IAsyncEnumerable<string>` — first yield the header row per `contracts/README.md §"Applications CSV"`, then yield one CSV-formatted line per (application, currency) pair (per FR-020). Use `InvariantCulture` for decimals and ISO-8601 for dates.
- [X] T064 [US3] Add the controller actions in `src/FundingPlatform.Web/Controllers/Admin/AdminReportsController.cs`: `public async Task<IActionResult> Applications([FromQuery] ListApplicationsRequest req, CancellationToken ct)` returning `View(await BuildApplicationsViewModel(req, ct))`, and `public async Task<IActionResult> ExportApplications([FromQuery] ListApplicationsRequest req, CancellationToken ct)` returning a `FileStreamResult` whose body is the `IAsyncEnumerable<string>` joined by newlines. On `CsvRowBoundExceededException`, the action returns `BadRequest(new { error="CsvRowBoundExceeded", limit=ex.Limit, actualCount=ex.ActualCount, hint="Narrow your filter and try again." })`. Routes: `[HttpGet("Applications")]`, `[HttpGet("Applications/Export")]`.
- [X] T065 [P] [US3] Create `src/FundingPlatform.Web/ViewModels/Admin/Reports/ApplicationsViewModel.cs`. Wraps `ListApplicationsResult` and adds presentation-only fields (formatted dates, formatted per-currency strings, sort-link helpers).
- [X] T066 [P] [US3] Create `src/FundingPlatform.Web/Views/Admin/Reports/Applications.cshtml`. Includes `_PageHeader`, `_ReportSubTabs(ActiveTab="Applications")`, `_FormSection` for the filter bar (state multi-select + date range + search input + has-agreement select + has-active-appeal select), `_DataTable` for rows (with the columns from spec.md US3 / data-model.md / contracts), `_ActionBar` for the Download CSV button (renders as `<a href="@Url.Action("ExportApplications", new { ...req })" class="btn">Download CSV</a>`), and `_EmptyState` for empty result sets. NO inline styles. NO badge markup outside `_StatusPill`. Pagination links use `ReportFilterEncoder` to preserve filter state.
- [X] T067 [US3] Run the test class. All seven methods must pass. Run the full E2E suite.
- [X] T068 [US3] Manual smoke per `quickstart.md §"US3"`.

**Checkpoint**: Applications report fully functional. CSV export streams; deep-linkable filter querystring works; refuse-on-overflow returns HTTP 400.

---

## Phase 6: User Story 4 — Applicants report (Priority: P1)

**Story**: spec.md US4. **Goal**: list of applicants with lifecycle aggregates per applicant (apps per terminal state, approval rate, totals per currency, last-activity). CSV export with per-(applicant, currency) row expansion.

**Independent Test**: Search a known applicant; confirm exactly one row returns with non-zero values in `Total Apps`, `Resolved`, `Response Finalized`, `Agreement Executed`. Total approved/executed render as per-currency stacks. Apply `has-executed-agreement = yes`; confirm result narrows. CSV export emits one row per (applicant, currency) pair.

### Tests for User Story 4

- [X] T069 [P] [US4] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/AdminReportsApplicantsPage.cs` mirroring the Applications PageObject's shape (filter, sort, paginate, download).
- [X] T070 [P] [US4] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsApplicantsTests.cs` with test methods covering spec.md US4 AS1–AS5 and SC-002 / SC-003 / SC-004 for this report. Includes a per-currency expansion CSV test and a zero-application applicant em-dash test.
- [X] T071 [US4] Run the test class. Confirm all methods FAIL.

### Implementation for User Story 4

- [X] T072 [P] [US4] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/ListApplicantsRequest.cs`, `ListApplicantsResult.cs`, and `ApplicantRowDto.cs` per `contracts/README.md` and `data-model.md`.
- [X] T073 [US4] Implement `ApplicantsQuery(req)` in `ReportQueryService.cs`. EF projection from `_db.Applicants.AsNoTracking().Include(a => a.Applications).ThenInclude(app => app.Items).ThenInclude(i => i.Quotations)`. Apply filter predicates. Compute aggregates in the projection: `TotalApps = a.Applications.Count(app => app.SubmittedAt != null)`, `ResolvedCount = a.Applications.Count(app => app.State == Resolved || app.State == ResponseFinalized || app.State == AgreementExecuted)` (decompose into the three columns), `ApprovalRate = items-approved / items-total` (NaN-safe; em-dash when total is zero), `TotalApproved` and `TotalExecuted` as per-currency stacks computed post-projection in C#, `LastActivity = a.Applications.Max(app => app.UpdatedAt)`.
- [X] T074 [US4] Implement `ListApplicantsAsync` and `ExportApplicantsCsvAsync` in `AdminReportsService.cs`. Sort tokens: `"executed-desc"` (default), `"executed-asc"`, `"approved-desc"`, `"approved-asc"`, `"applicant-asc"`, `"applicant-desc"`, `"lastActivity-desc"`, `"lastActivity-asc"`. Tie-break on `total-approved-desc`. CSV bound + streaming pattern as per US3.
- [X] T075 [US4] Add controller actions `Applicants` and `ExportApplicants` to `AdminReportsController`. Same shape as the Applications equivalents.
- [X] T076 [P] [US4] Create `src/FundingPlatform.Web/ViewModels/Admin/Reports/ApplicantsViewModel.cs` and `src/FundingPlatform.Web/Views/Admin/Reports/Applicants.cshtml`. Mirrors the Applications view structure with applicant-specific columns/filters per spec.md US4. CSV link uses `Url.Action("ExportApplicants", new { ...req })`.
- [X] T077 [US4] Run the test class. All methods pass. Run the full E2E suite.
- [X] T078 [US4] Manual smoke per `quickstart.md §"US4"`.

**Checkpoint**: Applicants report fully functional.

---

## Phase 7: User Story 5 — Funded Items report (Priority: P1)

**Story**: spec.md US5. **Goal**: line-item view of every approved item across the platform — `ReviewStatus = Approved` AND non-null selected supplier AND parent application in `ResponseFinalized` or `AgreementExecuted`. Filters: category, supplier, app-state, approved-at date range, executed-only. CSV export adds supplier legal id and app submitted-at columns.

**Independent Test**: Filter category and supplier; confirm only matching items render. Toggle "Executed only"; confirm only `AgreementExecuted` rows remain. Apply approved-at date range; confirm scope. Export CSV; confirm column shape.

### Tests for User Story 5

- [X] T079 [P] [US5] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/AdminReportsFundedItemsPage.cs`.
- [X] T080 [P] [US5] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsFundedItemsTests.cs` covering spec.md US5 AS1–AS5: scope correctness (rejected-in-response items excluded), category+supplier filter, executed-only toggle, approved-at range, supplier-display-name-current behavior, CSV column completeness.
- [X] T081 [US5] Run the test class. Confirm all methods FAIL.

### Implementation for User Story 5

- [X] T082 [P] [US5] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/ListFundedItemsRequest.cs`, `ListFundedItemsResult.cs`, `FundedItemRowDto.cs` per `contracts/README.md` and `data-model.md`.
- [X] T083 [US5] Implement `FundedItemsQuery(req)` in `ReportQueryService.cs`. Scope predicate: `i.ReviewStatus == Approved && i.SelectedSupplierId != null && (i.Application.State == ResponseFinalized || i.Application.State == AgreementExecuted)`. Additionally exclude items rejected in the parent's most recent applicant response: `&& !_db.ApplicantResponses.Where(ar => ar.ApplicationId == i.ApplicationId).OrderByDescending(ar => ar.CycleNumber).First().ItemResponses.Any(ir => ir.ItemId == i.Id && ir.Decision == Reject)`. (Subquery shape may need optimization — confirm SQL plan in implementation.) `ApprovedAt` is sourced via a join to `VersionHistory.Where(vh => vh.ApplicationId == i.ApplicationId && vh.Action == "Finalize").Max(vh => vh.Timestamp)`; em-dash if missing per `research.md §5`. Selected-quotation `Price` and `Currency`: `i.Quotations.First(q => q.SupplierId == i.SelectedSupplierId)`.
- [X] T084 [US5] Implement `ListFundedItemsAsync` and `ExportFundedItemsCsvAsync` in `AdminReportsService.cs`. Default sort `approvedAt-desc` per FR-026. CSV adds `Supplier Legal Id` and `App Submitted` columns per `contracts/README.md §"Funded Items CSV"`.
- [X] T085 [US5] Add controller actions `FundedItems` and `ExportFundedItems` to `AdminReportsController`.
- [X] T086 [P] [US5] Create `FundedItemsViewModel.cs` and `Views/Admin/Reports/FundedItems.cshtml`. Filter bar: category multi-select (populate from `_db.Categories.Where(c => c.IsActive)`), supplier multi-select (populate from `_db.Suppliers`), app-state multi-select restricted to `ResponseFinalized` + `AgreementExecuted` (FR-026 — surface a named-rule error if the querystring carries other values), approved-at date range, executed-only checkbox.
- [X] T087 [US5] Run the test class. All methods pass. Run the full E2E suite.
- [X] T088 [US5] Manual smoke per `quickstart.md §"US5"`.

**Checkpoint**: Funded Items report fully functional.

---

## Phase 8: User Story 6 — Aging Applications report (Priority: P2)

**Story**: spec.md US6. **Goal**: applications stuck in non-terminal states beyond a configurable threshold (default 14 days, range 1–365). Default state filter excludes `Draft`; opt-in toggle includes Drafts. Default sort is days-in-current-state descending.

**Independent Test**: Seed three applications in `Submitted` with synthetic transition timestamps of 5 / 20 / 90 days ago. Threshold=14 → two render. Threshold=60 → one renders. Toggle Drafts on → stale Drafts appear.

### Tests for User Story 6

- [X] T089 [P] [US6] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/AdminReportsAgingPage.cs`.
- [X] T090 [P] [US6] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsAgingTests.cs` covering spec.md US6 AS1–AS6: threshold filter, multi-transition computation (most-recent-into-current-state), default-excludes-Draft, opt-in Draft toggle, threshold-out-of-range error, querystring deep-link.
- [X] T091 [US6] Run the test class. Confirm all methods FAIL.

### Implementation for User Story 6

- [X] T092 [P] [US6] Populate `src/FundingPlatform.Application/Admin/Reports/DTOs/ListAgingApplicationsRequest.cs`, `ListAgingApplicationsResult.cs`, `AgingApplicationRowDto.cs` per `contracts/README.md` and `data-model.md`.
- [X] T093 [US6] Implement `AgingApplicationsQuery(req)` in `ReportQueryService.cs`. Per `research.md §5`:
  - Define a `static readonly Dictionary<ApplicationState, string[]> ActionStringsByState` map: `Submitted → ["Submitted"]`, `Resolved → ["Finalize"]`, `Draft → ["Created", "SendBack"]`, `ResponseFinalized → ["ApplicantResponseSubmitted"]` (confirm the exact string in `ApplicantResponseService.cs`), `AppealOpen → ["AppealOpened"]` (confirm), `AgreementExecuted → ["AgreementExecuted"]` (confirm), `UnderReview → []` (no action string written).
  - For each application, find the most-recent `VersionHistory` entry whose `Action` is in `ActionStringsByState[app.State]`. That row's `Timestamp` is `LastTransitionAt`; its `UserId` is `LastActor` (resolved to a display name via `_db.Users` join — actor name, not user id).
  - For `UnderReview` (empty action set), fall back to the most-recent `"Submitted"` entry; if none, fall back to `app.UpdatedAt` and emit `LastActor` as em-dash.
  - `DaysInCurrentState = (DateTime.UtcNow - LastTransitionAt).TotalDays`, integer-floored.
  - Apply filter: `DaysInCurrentState >= req.ThresholdDays`. Apply state filter (default: all non-terminal except `Draft`). Apply applicant search.
- [X] T094 [US6] Add a small inline integration assertion in the implementation (NOT a separate test file): a `static readonly` action-string array on `ReportQueryService` must match the literal strings written by `ApplicationService`, `ApplicantResponseService`, `ReviewService`, and `SignedUploadService`. Confirm the literals at implementation time by greping each file (the ones already surveyed in research). If a literal in the source has drifted from the map, fail the build with a clear message — this catches the silent-divergence risk noted in `research.md §5`.
- [X] T095 [US6] Implement `ListAgingApplicationsAsync` and `ExportAgingApplicationsCsvAsync` in `AdminReportsService.cs`. Validate `1 <= req.ThresholdDays <= 365` BEFORE invoking the query; on failure surface a single named-rule error to the caller (FR-022). CSV column shape per `contracts/README.md §"Aging CSV"`.
- [X] T096 [US6] Add controller actions `Aging` and `ExportAging` to `AdminReportsController`.
- [X] T097 [P] [US6] Create `AgingApplicationsViewModel.cs` and `Views/Admin/Reports/Aging.cshtml`. Filter bar: state multi-select (default = `Submitted, UnderReview, Resolved, ResponseFinalized` — i.e., non-terminal except Draft; with explicit Draft toggle), threshold input (numeric, default 14, validation 1-365), applicant search.
- [X] T098 [US6] Run the test class. All methods pass. Run the full E2E suite.
- [X] T099 [US6] Manual smoke per `quickstart.md §"US6"`.

**Checkpoint**: Aging Applications report fully functional. Closes brainstorm thread #04.

---

## Phase 9: User Story 7 — Reports area consumes Tabler shell + reusable partials (Priority: P3)

**Story**: spec.md US7. **Goal**: prove that every report-area view consumes the Tabler shell and the existing partials, introduces no bespoke styling, and that the role-aware sidebar behavior survives unchanged. Mostly verification.

**Independent Test**: Walk every report view in a browser; confirm visual consistency with the rest of the platform. Run a regex sweep over `Views/Admin/Reports/**` for inline `style=` attributes and badge markup outside `_StatusPill`.

### Tests for User Story 7

- [X] T100 [P] [US7] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/AdminReportsTablerShellTests.cs` with test methods:
  - `Reports_NoInlineStyleAttributes` — performs a regex sweep over all `Views/Admin/Reports/**.cshtml` files and asserts zero matches for `\bstyle="`.
  - `Reports_NoBadgeMarkupOutsideStatusPill` — regex sweep for `<span class="badge` outside files named `_StatusPill.cshtml`.
  - `Reports_KpiTilePartial_IsGeneric` — reads `_KpiTile.cshtml` and asserts its parameter list contains only `(string Label, int? NumericValue, IReadOnlyList<CurrencyAmount>? Stack)` and contains no admin-area-specific markup.
  - `Sidebar_ReportsEntry_NotVisibleToReviewerOrApplicant` (FR-011 / spec.md US7 AS4)
  - `Sidebar_ReportsEntry_VisibleToAdmin`

### Implementation for User Story 7

- [X] T101 [US7] Run the test class. Verify the regex-sweep methods pass on the existing implementation (US2–US6 should already be compliant by virtue of using the existing partials). Failures here indicate violations introduced during US2–US6 — fix the offending view files, NOT the tests.
- [X] T102 [US7] If `Sidebar_ReportsEntry_*` tests fail, confirm the sidebar partial from spec 008/009 still renders the existing `Reports` entry gated on the Admin role; this spec does NOT add any new sidebar entries (FR-011).
- [X] T103 [US7] Manual smoke per `quickstart.md §"US7"`. Walk §7.1–§7.4.

**Checkpoint**: All seven user stories complete. Build clean, full E2E suite green, brainstorm thread #04 closed.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: tie up loose ends, run the full quickstart, and prepare for `/spex:review-plan` or PR.

- [X] T104 [P] Run `dotnet build --nologo` and confirm zero errors, zero warnings — same standard as T001's baseline.
- [X] T105 [P] Run the FULL E2E suite (`dotnet test tests/FundingPlatform.Tests.E2E --nologo`). Zero failures; the new test count = baseline + (CurrencyRolloutTests=6) + (AdminReportsDashboardTests=6) + (AdminReportsApplicationsTests=7) + (AdminReportsApplicantsTests=~5) + (AdminReportsFundedItemsTests=~5) + (AdminReportsAgingTests=~6) + (AdminReportsTablerShellTests=5) ≈ baseline + 40.
- [X] T106 [P] Walk the FULL `quickstart.md` end-to-end. Tick every checklist item under `Done criteria`. If §"Funding Agreement PDF — visual comparison fixture" was captured (T004), perform the side-by-side comparison and document the result in this task's commit message.
- [X] T107 [P] Run the regex sweeps from US7 manually as a belt-and-braces check: `grep -rE 'style="' src/FundingPlatform.Web/Views/Admin/Reports/` (expect zero), `grep -rE '<span class="badge' src/FundingPlatform.Web/Views/Admin/Reports/` (expect zero).
- [X] T108 Update `brainstorm/00-overview.md` to confirm thread #04 is in the Closed Threads list (already updated during the brainstorm session, but re-verify the entry references spec 010's Aging report and its threshold range).
- [X] T109 Optionally invoke `/spex:review-plan` to generate `REVIEW-PLAN.md` for a formal cross-artifact consistency check (coverage matrix, red-flag scan, NFR validation). Skip if reviewer time is constrained; the spec was already SOUND on `spex:review-spec`'s first pass.
- [X] T110 Run a final solution-wide build + test pass. Commit the resulting state on the `010-admin-reports` branch as the final shippable checkpoint.
- [X] T111 Open a draft PR from `010-admin-reports` → `main` summarizing the user stories, FR coverage, and outstanding open threads forwarded to future specs. Reference `specs/010-admin-reports/spec.md`, `plan.md`, and `REVIEW-SPEC.md` (and `REVIEW-PLAN.md` if generated in T109) in the PR description.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies. Run T001–T004 in parallel.
- **Phase 2 (Foundational)**: Depends on Phase 1. Ordering inside Phase 2 has internal sequence: dacpac (T005→T006→T007→T008→T009) → Domain (T010, T011 in parallel; then T012; then T013, T014, T015 sequentially; then T016 in parallel) → Web/Config (T017→T018→T019) → DI (T020→T021–T025 in parallel) → Build/Test gate (T026→T027→T028).
- **Phase 3 (US1)**: Depends on Phase 2. Tests T029–T032 run before implementation T033–T040.
- **Phase 4 (US2)** through **Phase 8 (US6)**: Each depends on Phase 2. Once Phase 2 is complete, these phases CAN proceed in parallel by separate developers — they touch independent DTO files, view files, controller-action methods, query-service methods, and test classes. (Reviewer-friendly serial order is still recommended for solo work: US2 → US3 → US4 → US5 → US6 in priority order per spec.md.)
- **Phase 9 (US7)**: Depends on Phases 4–8 (US2–US6) being complete. The regex sweeps and sidebar tests verify the work in those phases.
- **Phase 10 (Polish)**: Depends on Phase 9.

### Within Each User Story

- Tests are written FIRST (constitution III, TDD per project convention). Verify red.
- DTOs (record-shape changes) before query (EF projection) before service (orchestration) before controller (HTTP wiring) before view (Razor).
- Run the per-story test class to green BEFORE moving to the next story.
- Run the full E2E suite to green at every checkpoint.

### Parallel Opportunities

- **Phase 1**: T001, T002, T003, T004 in parallel.
- **Phase 2**: T010 ∥ T011, T017 ∥ T021–T025 (after T020).
- **Phase 3 (US1)**: T029, T030, T031 in parallel; T033, T034, T035, T037 in parallel.
- **Phase 4–8**: each phase can run in parallel with the others if developer capacity allows. Within each, the page-object + test class + DTOs are parallelizable.
- **Phase 10**: T104, T105, T106, T107 in parallel.

---

## Parallel Example: User Story 3

```bash
# Launch the page-object, helpers, and test scaffolds together (Phase 5):
Task: "Create AdminReportsApplicationsPage.cs in tests/FundingPlatform.Tests.E2E/PageObjects/Admin/Reports/"
Task: "Create CsvAssertions.cs helper in tests/FundingPlatform.Tests.E2E/Helpers/"
Task: "Create AdminReportsApplicationsTests.cs in tests/FundingPlatform.Tests.E2E/Tests/AdminReports/"

# Launch the DTO populations + helper together (after the test class is red):
Task: "Populate ListApplicationsRequest.cs in src/FundingPlatform.Application/Admin/Reports/DTOs/"
Task: "Populate ListApplicationsResult.cs and ApplicationRowDto.cs"
Task: "Create ReportFilterEncoder.cs in src/FundingPlatform.Web/Helpers/"
```

---

## Implementation Strategy

### MVP First — User Story 1 (Currency Rollout)

1. Complete Phase 1 (Setup).
2. Complete Phase 2 (Foundational) — schema, domain, value object, configuration, DI skeletons.
3. Complete Phase 3 (US1) — quotation form, PDF render, startup probe, full E2E coverage.
4. **STOP and VALIDATE**: every Funding Agreement PDF newly generated from this point forward shows currency codes; legacy PDFs unchanged; legacy quotations backfilled.
5. Deploy/demo if needed before continuing — the platform is multi-currency-capable on the read path even if the dashboard and reports are not yet live.

### Incremental Delivery

1. Setup + Foundational + US1 → Currency rollout shipped (MVP + multi-currency capability).
2. + US2 → dashboard live; sub-tab strip in place; the four detail-report routes still throw NotImplementedException (admins hit `/Admin/Reports/Applications` and see a 500 — acceptable for an interim release if needed; a planning-time alternative is to land US2-stub views that say "coming soon" until the route's implementation lands).
3. + US3 → applications report live.
4. + US4 → applicants report live.
5. + US5 → funded items report live.
6. + US6 → aging report live; brainstorm thread #04 closed.
7. + US7 → Tabler shell consumption verified.
8. Polish phase → ready to merge.

### Parallel Team Strategy

After Phase 2 completes, four developers can independently land US2–US5 (each phase has independent DTO files, controller-action methods, query-service methods, view files, and test classes). US6 has a small dependency on US2–US5's `_DataTable` consumption pattern but no production-code coupling.

US7 requires US2–US6 to be implemented before its regex sweeps mean anything; it is a verification phase that stays at the end.

---

## Notes

- **[P] tasks** = different files, no dependencies on incomplete tasks.
- **[Story] label** maps task to specific user story for traceability.
- Each user story should be independently completable and testable.
- Verify tests fail before implementing. Verify tests pass after.
- Commit after each task or logical group.
- Stop at any checkpoint to validate story independently.
- Avoid: vague tasks, same-file conflicts (would break parallelism), cross-story dependencies that break independence.
- **Cross-cutting safety**: every user story must run the FULL E2E suite to green at its checkpoint, not just its own test class. The currency cascade in Phase 2 touches enough call sites that a regression in spec 001/003/005 is a real risk.
- **Specific to this spec**: the dacpac three-step (T005 → T006 → T007 → T008) is order-sensitive at deploy time but the files can be edited in parallel during development as long as T008 (`.sqlproj` registration) lands LAST.
