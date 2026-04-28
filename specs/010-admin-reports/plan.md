# Implementation Plan: Admin Reports Module

**Branch**: `010-admin-reports` | **Date**: 2026-04-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/010-admin-reports/spec.md`

## Summary

Two-layer feature: a small cross-cutting data change plus an admin-area read surface.

**Data layer (US1).** Add a required `Currency` column on `dbo.Quotations` (NVARCHAR(3)) via the dacpac project. Add a `DefaultCurrency` row to `dbo.SystemConfigurations` via the existing post-deployment seed script (`SeedData.sql`). The dacpac deployment runs in a strict three-step order: (1) add the column nullable, (2) the post-deployment script back-fills every existing row with `DefaultCurrency` and inserts the `DefaultCurrency` row if missing, (3) a second dacpac script in the same deployment tightens the column to `NOT NULL`. The `Quotation` entity gains a `Currency` property (3-character code) initialized via the constructor and an `EditCurrency(string code)` mutator. Application-layer command bus (`AddSupplierQuotationCommand`, `ReplaceQuotationDocumentCommand`) accepts a `Currency` argument; the existing `AddQuotationViewModel` gains a `Currency` field with a default of `Configuration["AdminReports:DefaultCurrency"]`. The Funding Agreement HTML template (`Views/FundingAgreement/Partials/_FundingAgreementItemsTable.cshtml`) is updated so each amount renders as `{Currency} {LocaleAmount}` (e.g., `COP 1.234.567`); the renderer (`SyncfusionFundingAgreementPdfRenderer`) is unchanged. A startup probe in `Program.cs` reads `AdminReports:DefaultCurrency` from configuration and aborts (`throw InvalidOperationException`) when missing — placed BEFORE any service is built, so the host fails fast and observably.

**Read surface (US2-US7).** A new `AdminReportsService` in the Application layer owns every read path. It exposes nine methods — `GetDashboardAsync(DateRange)`, `ListApplicationsAsync(...)`, `ListApplicantsAsync(...)`, `ListFundedItemsAsync(...)`, `ListAgingApplicationsAsync(...)`, plus four parallel `Export*CsvAsync` methods (one per detail report) — plus a single internal `EnforceCsvRowBoundOrThrow(...)` guard. Every list method takes a request DTO carrying filter, sort, page, and pageSize fields and returns a result DTO with rows, total count, applied filter echo, and the per-currency totals carried as `IReadOnlyList<CurrencyAmount>` instead of `decimal`. CSV export shares the underlying query but skips pagination and runs the row-count guard before streaming.

The `AdminReportsController` (currently a one-action stub at `/Admin/Reports`) is replaced by five controller methods on the same class: `Index` (the dashboard), `Applications`, `Applicants`, `FundedItems`, `Aging`, plus four sibling `Export{Name}` actions returning `FileStreamResult` (CSV). All gated by `[Authorize(Roles="Admin")]`, inherited unchanged from spec 009. Routes follow the spec's contract (`/Admin/Reports/{Applications|Applicants|FundedItems|Aging}` plus `/Admin/Reports/{Name}/Export`).

A new generic `_KpiTile` partial under `Views/Shared/Components/` takes a label, an optional non-currency numeric value, and an optional `IReadOnlyList<CurrencyAmount>` for per-currency stacks. The dashboard view stacks ten `_KpiTile` instances across three rows. Every detail-report view uses the existing spec-008 partials (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`). A new `_ReportSubTabs` partial (also under `Views/Shared/Components/`) renders the four-entry sub-tab strip and is included on every report-area page including the dashboard. The role-aware sidebar already carries the `Reports` entry from spec 009; nothing changes there.

Two read-side query patterns are introduced: (a) a `IReportQueryService` in Infrastructure that owns the EF Core projections (every report is a single LINQ-to-SQL query — no multiple round-trips), and (b) a `FilterEncoder`/`FilterDecoder` helper in the Web layer that round-trips the querystring filter shape (used by every detail report and shared with `_DataTable` to render filter-aware pagination links). Querystring keys mirror request-DTO field names so deep-linking is a pure GET.

Total production-code footprint: 1 new value object (`CurrencyAmount`), 1 modified domain entity (`Quotation` — add `Currency` + mutator), 1 modified application service (`ApplicationService` / `QuotationService` for command-bus argument), 1 new Application service interface (`IAdminReportsService`) + 1 implementation, 1 new Infrastructure query service (`IReportQueryService` + EF implementation), 4 new request DTOs + 4 new result DTOs + 4 new row DTOs + 1 dashboard DTO, 4 new Razor views (one per detail report), 1 modified Razor view (`Views/Admin/Reports/Index.cshtml` — replaces stub), 1 modified Razor partial (`_FundingAgreementItemsTable.cshtml` — currency token), 2 new partials (`_KpiTile`, `_ReportSubTabs`), 1 modified `dbo.Quotations.sql` (+ `Currency` column), 1 modified `SeedData.sql` (+ `DefaultCurrency` row + Currency back-fill), 1 new dacpac post-deploy script (`SeedData_TightenCurrency.sql` for the NOT NULL pass — or equivalent technique documented in research.md), 1 modified `Program.cs` (startup probe), 1 modified `AddQuotationViewModel` + the existing quotation create/edit views (+ Currency field), plus full Playwright E2E coverage (one test class per user story). Three existing files touched only for the `Currency` cascade rename: `AddSupplierQuotationCommand.cs`, `QuotationDto.cs`, and `SyncfusionFundingAgreementPdfRenderer`'s template-binding contract. **No new NuGet packages, no new managed dependencies.**

## Technical Context

**Language/Version**: C# / .NET 10.0 (matches all prior specs).
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **No new dependencies introduced by this feature.** The Tabler.io static-asset bundle vendored by spec 008 is reused as-is. The Syncfusion HTML-to-PDF renderer and license validator vendored by spec 005 are reused as-is for the Funding Agreement currency-code render change.
**Storage**: SQL Server (Aspire-managed for dev, dacpac schema management). **Schema change**: one new column on `dbo.Quotations` (`Currency` NVARCHAR(3) NOT NULL after backfill). One new seed row in `dbo.SystemConfigurations` (`DefaultCurrency`). No new tables. No new managed storage subsystems. CSV exports stream directly from the DB to the HTTP response — no temp file, no in-memory materialization beyond the page-buffer.
**Testing**: Playwright for .NET (NUnit) for E2E. New test classes: `CurrencyRolloutTests.cs` (US1: form prefill, mixed-currency PDF, dacpac backfill verification, missing-config startup-fail), `AdminReportsDashboardTests.cs` (US2), `AdminReportsApplicationsTests.cs` (US3), `AdminReportsApplicantsTests.cs` (US4), `AdminReportsFundedItemsTests.cs` (US5), `AdminReportsAgingTests.cs` (US6), `AdminReportsTablerShellTests.cs` (US7). Existing tests for specs 001–009 must remain green. The Funding Agreement template change (US1) is covered by an extension to `FundingAgreementGenerationTests.cs` plus a manual visual-comparison procedure documented in `quickstart.md` (PDF-snapshot regression infrastructure is out of scope per simplicity).
**Target Platform**: Linux/Windows server (Aspire-orchestrated). UI verified against Chromium and Firefox at 1280×800 viewport (admin area is desktop-first; mobile is acceptable-but-not-prioritized per Tabler defaults established in spec 008).
**Project Type**: Web application (server-side ASP.NET MVC; no SPA framework introduced).
**Performance Goals**: All five report surfaces target sub-second page loads on a seeded dev environment with up to ~1,000 applications and ~5,000 quotations. Each list page is one EF-projected SQL query. The CSV export streams via `IAsyncEnumerable<T>` to keep memory bounded regardless of dataset size, and refuses upstream when row-count exceeds the configured upper bound (default 50,000 — settled via `AdminReports:CsvRowLimit` config). Dashboard KPI computation is one batched query (a single `UNION ALL` selecting per-state counts, per-currency financial sums, and applicant aggregates) — round-trip target ≤ 200 ms on dev volume.
**Constraints**: FR-001 / FR-003 / FR-028 mandate dacpac for every schema change; EF migrations are prohibited. FR-005 mandates the Funding Agreement PDF renders the currency code beside every amount; FR-005 also mandates that pre-existing PDFs are NOT regenerated. FR-006 forbids any FX / conversion / rollup. FR-007 mandates startup-fail-fast on missing `DefaultCurrency`. FR-017 mandates server-side pagination, sorting, and filtering. FR-018 mandates deep-linkable filter querystrings. FR-019 mandates CSV export of the currently-filtered dataset across all pages. FR-020 mandates per-(entity, currency) row expansion in CSV. FR-021 mandates refuse-on-overflow. FR-028 mandates spec-008 partial consumption; FR-029 permits new partials when no existing fits.
**Scale/Scope**: Realistic v1 scale: tens of admins, low hundreds of reviewers, low thousands of applicants, low thousands of quotations. Current dataset ranges expected to fit comfortably within the CSV upper bound; the bound exists to defend the future. Page-size default is `25` (matches `ReviewService.PageSize` and the signing inbox default).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | New value object (`CurrencyAmount`) and the `Quotation` extension live in `FundingPlatform.Domain`. The `IAdminReportsService` interface and `AdminReportsService` implementation live in `FundingPlatform.Application/Admin/Reports/` (alongside the existing `Admin/Users` from spec 009). The `IReportQueryService` interface lives in Application; its EF implementation lives in `FundingPlatform.Infrastructure/Persistence/Reports/`. The Web layer hosts the controllers, views, view-models, and partials. Dependency direction stays inward; no Web/Infrastructure references leak into Domain or Application. |
| II. Rich Domain Model | PASS | The `Currency` attribute on `Quotation` is set via the constructor and a single `EditCurrency(string code)` mutator that validates length-equals-3 and uppercase-canonicalizes. No raw setters added. Reports are read-only projections with no domain logic — domain entities stay untouched on the read path beyond the `Currency` column. The `CurrencyAmount` value object enforces the 3-char invariant in its constructor. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | One Playwright E2E test class per user story (US1-US7). Each test class is independently runnable per the constitution. PageObject pattern: new `AdminReportsDashboardPage`, `AdminReportsApplicationsPage`, `AdminReportsApplicantsPage`, `AdminReportsFundedItemsPage`, `AdminReportsAgingPage`. Currency tests reuse the existing `QuotationCreatePage` from spec 001 plus a new `FundingAgreementPdfAssertions` helper. CSV-export tests assert response headers (`Content-Type: text/csv`, `Content-Disposition: attachment`) and parse the downloaded body to assert per-(entity, currency) row expansion. The startup-fail-fast test runs against a dedicated `AspireFixture` configuration that omits `AdminReports:DefaultCurrency` and asserts the host throws on `Build()`. |
| IV. Schema-First Database Management | PASS | The new `Currency` column is added to `dbo.Quotations.sql` in the dacpac project. The `DefaultCurrency` system-configuration row is added to `PostDeployment/SeedData.sql`. The post-deploy backfill is also in `SeedData.sql` (idempotent). The `NOT NULL` tightening lands in a dedicated post-deploy script `PostDeployment/SeedData_TightenQuotationCurrency.sql` that runs after `SeedData.sql` (deployment ordering documented in `research.md`). **No EF migrations.** The C# `Quotation` entity declares the matching `Currency` property; EF Core maps it by convention. |
| V. Specification-Driven Development | PASS | Spec is SOUND on first iteration (see `REVIEW-SPEC.md`). Plan flows from spec; `research.md` resolves the six planning-phase pins from the spec review; `data-model.md` documents the `Quotation` extension, `SystemConfiguration` `DefaultCurrency` row, and the read-only entity surfaces consumed by reports; `contracts/README.md` enumerates controller routes/actions, `IAdminReportsService` signatures, and request/response/CSV-column envelopes; `quickstart.md` walks the manual smoke procedure plus PDF visual-comparison checklist. Tasks generated next by `/speckit-tasks`. |
| VI. Simplicity and Progressive Complexity | PASS | YAGNI honored throughout: no FX (deferred), no audit log (deferred), no Excel/PDF/API export (deferred), no charts library (KPI tiles only), no OLAP / data warehouse / materialized views (single-query reports). One new value object (`CurrencyAmount`) is justified — `decimal` alone cannot represent the per-currency stack required by FR-014 / FR-020. One new partial (`_KpiTile`) is justified — no existing partial supports the per-currency stack. One new partial (`_ReportSubTabs`) is justified — the spec-007 signing wayfinding sub-tab pattern is signing-specific and not directly reusable. The startup probe is the simplest possible enforcement of FR-007. The `IReportQueryService` is a focused interface, not a generic repository, and is justified by Infrastructure isolation (constitution I). Complexity Tracking table is empty. |

**Gate result: PASS — proceed to Phase 0.**

**Post-design re-check (after Phase 1 — `research.md`, `data-model.md`, `contracts/README.md`, `quickstart.md` all generated):** Still PASS. The Phase 1 artifacts surface no new types beyond those enumerated above. The `IAdminReportsService` contract is a single interface with nine methods (one dashboard + four lists + four exports); each maps 1:1 to a controller action. The `IReportQueryService` interface has five projection methods (four lists + one dashboard snapshot) — each list returns an `IQueryable<T>` so the service can compose pagination/sort over the projection without re-doing the join. No additional Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/010-admin-reports/
├── spec.md                     # Stakeholder-facing specification (SOUND on first iteration)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — six planning pins resolved
├── data-model.md               # Phase 1 output — Quotation extension, DefaultCurrency row, read-only entity surfaces
├── quickstart.md               # Phase 1 output — manual smoke procedure + PDF visual-comparison checklist
├── contracts/
│   └── README.md               # Phase 1 output — controller routes/actions, IAdminReportsService interface, request/response/CSV-column envelopes, sub-tab strip table, error-code map
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── review_brief.md             # Reviewer-facing guide
├── REVIEW-SPEC.md              # Formal spec soundness review (SOUND)
├── REVIEW-PLAN.md              # Formal plan review (generated by spex:review-plan after this command, if invoked)
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
├── FundingPlatform.Domain/
│   ├── Entities/
│   │   └── Quotation.cs                                       # MODIFY: add `Currency` (string, 3-char code) property and `EditCurrency(string)` mutator. Constructor signature gains `string currency`. Existing call sites in spec 001 cascade-update.
│   └── ValueObjects/
│       └── CurrencyAmount.cs                                  # NEW: immutable record `(string Currency, decimal Amount)` with constructor invariant `Currency.Length == 3` (uppercase-canonicalized).
│
├── FundingPlatform.Application/
│   ├── Admin/                                                 # EXISTING directory (Users, Configuration commands)
│   │   └── Reports/                                           # NEW directory
│   │       ├── IAdminReportsService.cs                        # NEW: contract — GetDashboardAsync, ListApplicationsAsync, ListApplicantsAsync, ListFundedItemsAsync, ListAgingApplicationsAsync, plus four parallel ExportXxxCsvAsync methods. Each list method takes a request DTO and returns a result DTO carrying rows + total + applied filter echo.
│   │       ├── DTOs/
│   │       │   ├── DashboardResult.cs                         # NEW: Pipeline counts + Financial per-currency stacks + Applicant counts.
│   │       │   ├── DateRange.cs                               # NEW: (DateOnly From, DateOnly To) record.
│   │       │   ├── ListApplicationsRequest.cs                 # NEW: state[], dateFrom, dateTo, search?, hasAgreement?, hasActiveAppeal?, page, pageSize, sort.
│   │       │   ├── ListApplicationsResult.cs                  # NEW: page of ApplicationRowDto + total + filter echo.
│   │       │   ├── ApplicationRowDto.cs                       # NEW: AppId, ApplicantFullName, ApplicantLegalId, State, CreatedAt, SubmittedAt?, ResolvedAt?, ItemCount, TotalApproved (CurrencyAmount[]), HasAgreement, HasActiveAppeal.
│   │       │   ├── ListApplicantsRequest.cs                   # NEW: search?, hasExecutedAgreement?, lastActivityFrom?, lastActivityTo?, page, pageSize, sort.
│   │       │   ├── ListApplicantsResult.cs                    # NEW: page of ApplicantRowDto + total.
│   │       │   ├── ApplicantRowDto.cs                         # NEW: identity (FullName, LegalId, Email), TotalApps, ResolvedCount, ResponseFinalizedCount, AgreementExecutedCount, ApprovalRate?, TotalApproved (CurrencyAmount[]), TotalExecuted (CurrencyAmount[]), LastActivity?.
│   │       │   ├── ListFundedItemsRequest.cs                  # NEW: categoryIds[], supplierIds[], appStates[], approvedFrom?, approvedTo?, executedOnly?, page, pageSize, sort.
│   │       │   ├── ListFundedItemsResult.cs                   # NEW: page of FundedItemRowDto + total.
│   │       │   ├── FundedItemRowDto.cs                        # NEW: AppId, ApplicantFullName, ItemProductName, CategoryName, SupplierName, SupplierLegalId?, Price, Currency, AppState, AppSubmittedAt?, ApprovedAt?, HasAgreement, Executed.
│   │       │   ├── ListAgingApplicationsRequest.cs            # NEW: states[], thresholdDays (default 14, range 1-365), search?, page, pageSize, sort.
│   │       │   ├── ListAgingApplicationsResult.cs             # NEW: page of AgingApplicationRowDto + total.
│   │       │   └── AgingApplicationRowDto.cs                  # NEW: AppId, ApplicantFullName, ApplicantEmail, ApplicantLegalId, State, DaysInCurrentState, LastTransitionAt, LastActor?, ItemCount, TotalApproved (CurrencyAmount[]).
│   │       └── Services/
│   │           └── AdminReportsService.cs                     # NEW: implements IAdminReportsService. Owns every read; delegates projection to IReportQueryService; applies pagination, sort, and the CSV row-bound guard. Throws CsvRowBoundExceededException with the configured limit on overflow.
│   ├── Interfaces/
│   │   └── IReportQueryService.cs                             # NEW: contract for IQueryable-returning projections. Methods: ApplicationsQuery(filter), ApplicantsQuery(filter), FundedItemsQuery(filter), AgingApplicationsQuery(filter), DashboardSnapshotAsync(dateRange).
│   └── Exceptions/
│       └── CsvRowBoundExceededException.cs                    # NEW: thrown by AdminReportsService when the filtered dataset exceeds the configured CSV row bound. Carries the bound and the actual count for the error response.
│
├── FundingPlatform.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── QuotationConfiguration.cs                      # MODIFY: add `Currency` property mapping (NVARCHAR(3), NOT NULL once backfill lands).
│   │   └── Reports/
│   │       └── ReportQueryService.cs                          # NEW: implements IReportQueryService. EF Core LINQ projections with `.AsNoTracking()`. Each method returns IQueryable<RowDto> with all joins flattened so the outer service composes pagination/sort over a single SQL statement.
│   └── DependencyInjection.cs                                 # MODIFY: register IAdminReportsService → AdminReportsService and IReportQueryService → ReportQueryService (both scoped). Add an `AdminReportsOptions` configuration binding for `AdminReports:DefaultCurrency` and `AdminReports:CsvRowLimit`.
│
├── FundingPlatform.Database/
│   ├── Tables/
│   │   └── dbo.Quotations.sql                                 # MODIFY: add `[Currency] NVARCHAR(3) NULL` column. The `NOT NULL` tightening is performed via post-deployment script (see below) so existing deployments backfill before the constraint applies.
│   └── PostDeployment/
│       ├── SeedData.sql                                       # MODIFY: append (a) idempotent insert of `DefaultCurrency` row in `dbo.SystemConfigurations` (default value pulled from a sqlcmd variable `$(DefaultCurrency)` passed by the deployment, fallback `'USD'` for dev convenience), (b) idempotent backfill of `dbo.Quotations.[Currency] = (SELECT Value FROM dbo.SystemConfigurations WHERE [Key] = 'DefaultCurrency')` WHERE `[Currency] IS NULL`. Both operations are placed under existing `IF NOT EXISTS` / `WHERE` guards so re-running is a no-op.
│       └── SeedData_TightenQuotationCurrency.sql              # NEW: executes after SeedData.sql; runs `ALTER TABLE dbo.Quotations ALTER COLUMN [Currency] NVARCHAR(3) NOT NULL` only if the current schema reports the column as nullable. Idempotent: a second run is a no-op because the column is already NOT NULL.
│
└── FundingPlatform.Web/
    ├── Program.cs                                             # MODIFY: (1) Bind `AdminReportsOptions` from `AdminReports:` configuration section. (2) Add a startup probe BEFORE `builder.Build()` that throws `InvalidOperationException` with a clear message when `AdminReportsOptions.DefaultCurrency` is null/empty. (3) No other Program.cs change.
    ├── Controllers/Admin/
    │   └── AdminReportsController.cs                          # MODIFY: replace the single `Index` action with five list actions (`Index` → dashboard; `Applications`, `Applicants`, `FundedItems`, `Aging`) and four export actions (`ExportApplications`, `ExportApplicants`, `ExportFundedItems`, `ExportAging`). Each list action consults `IAdminReportsService` and renders its dedicated view; each export action streams a CSV via `FileStreamResult`. Existing `[Authorize(Roles="Admin")]` and `[Route("Admin/Reports")]` preserved.
    ├── ViewModels/
    │   └── Admin/                                             # EXISTING directory (spec 009)
    │       └── Reports/                                       # NEW sub-directory
    │           ├── DashboardViewModel.cs                      # NEW: maps DashboardResult to view-friendly shape.
    │           ├── ApplicationsViewModel.cs                   # NEW
    │           ├── ApplicantsViewModel.cs                     # NEW
    │           ├── FundedItemsViewModel.cs                    # NEW
    │           ├── AgingApplicationsViewModel.cs              # NEW
    │           └── KpiTileViewModel.cs                        # NEW: (Label, NumericValue?, IReadOnlyList<CurrencyAmount>? Stack).
    ├── Views/
    │   ├── Admin/Reports/
    │   │   ├── Index.cshtml                                   # MODIFY: replace stub. Three KPI rows of `_KpiTile` partials + `_ReportSubTabs` + global date-range picker (a small `_DateRangePicker` partial reused from spec 005's planning era — confirm during implementation; otherwise inline form).
    │   │   ├── Applications.cshtml                            # NEW: _PageHeader + _ReportSubTabs + _FormSection (filter bar) + _DataTable (rows) + _ActionBar ("Download CSV") + _EmptyState.
    │   │   ├── Applicants.cshtml                              # NEW: same shape with applicant-specific columns/filters.
    │   │   ├── FundedItems.cshtml                             # NEW: same shape with funded-item-specific columns/filters.
    │   │   └── Aging.cshtml                                   # NEW: same shape with threshold input + state-include-Drafts toggle.
    │   ├── Quotation/
    │   │   ├── Add.cshtml                                     # MODIFY: add Currency input field, prefilled from `AdminReportsOptions.DefaultCurrency`.
    │   │   └── Edit.cshtml                                    # MODIFY: same.
    │   ├── FundingAgreement/Partials/
    │   │   └── _FundingAgreementItemsTable.cshtml             # MODIFY: each amount cell renders as `@Model.Currency @Model.Price.ToString("N", LatamCulture)` (or equivalent); sub-totals (if any) likewise.
    │   └── Shared/Components/
    │       ├── _KpiTile.cshtml                                # NEW: renders (Label, NumericValue, optional per-currency stack). Generic; reusable outside reports.
    │       └── _ReportSubTabs.cshtml                          # NEW: renders the four-entry sub-tab strip with the active surface highlighted.
    └── Helpers/
        └── ReportFilterEncoder.cs                             # NEW: pure-function helpers `Encode(request) -> querystring` and `Decode(querystring) -> request` for round-tripping the request-DTO shape. Used by every detail-report view's "Apply filter" button and by the pagination links inside `_DataTable`.

tests/
├── FundingPlatform.Tests.E2E/
│   ├── PageObjects/
│   │   └── Admin/
│   │       ├── Reports/                                       # NEW directory
│   │       │   ├── AdminReportsDashboardPage.cs               # NEW
│   │       │   ├── AdminReportsApplicationsPage.cs            # NEW
│   │       │   ├── AdminReportsApplicantsPage.cs              # NEW
│   │       │   ├── AdminReportsFundedItemsPage.cs             # NEW
│   │       │   └── AdminReportsAgingPage.cs                   # NEW
│   │       └── ... (existing pages from spec 009)
│   ├── Helpers/
│   │   ├── CsvAssertions.cs                                   # NEW: parses a downloaded CSV and asserts per-(entity, currency) row expansion + expected columns.
│   │   └── FundingAgreementPdfAssertions.cs                   # NEW: parses (or text-extracts) the generated PDF and asserts that every amount line carries an expected currency code.
│   └── Tests/
│       └── AdminReports/                                      # NEW directory
│           ├── CurrencyRolloutTests.cs                        # NEW (US1)
│           ├── AdminReportsDashboardTests.cs                  # NEW (US2)
│           ├── AdminReportsApplicationsTests.cs               # NEW (US3)
│           ├── AdminReportsApplicantsTests.cs                 # NEW (US4)
│           ├── AdminReportsFundedItemsTests.cs                # NEW (US5)
│           ├── AdminReportsAgingTests.cs                      # NEW (US6)
│           └── AdminReportsTablerShellTests.cs                # NEW (US7)
└── FundingPlatform.Tests.Integration/                          # EXISTING — extend, do not add new files unless needed
    └── ReportQueryServiceTests.cs                              # NEW (optional unit-ish test): asserts the EF projections produce a single SQL statement per call (integration-test boundary; not a substitute for E2E).
```

**Structure Decision**: This feature extends existing Clean-Architecture layout (Domain → Application → Infrastructure → Web) without introducing any new project. The new `FundingPlatform.Application/Admin/Reports/` directory is a sibling to the existing `FundingPlatform.Application/Admin/Users/` from spec 009 and follows the same shape (request DTOs, result DTOs, service interface, service implementation). The new `FundingPlatform.Infrastructure/Persistence/Reports/` directory hosts the EF projections only (no new repository — projections compose over the existing aggregates).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|

(None — Constitution Check passed with no violations on initial gate and post-design re-check.)
