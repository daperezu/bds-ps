# Contracts: Admin Reports Module

**Spec:** [../spec.md](../spec.md)
**Plan:** [../plan.md](../plan.md)
**Date:** 2026-04-26

This document enumerates the controller routes, service interfaces, request/response/CSV envelopes, and error codes introduced by spec 010. It is the implementation reference for the Web and Application layers.

---

## Controller routes

All routes live on `AdminReportsController` at `[Route("Admin/Reports")]`, gated by `[Authorize(Roles="Admin")]` (inherited unchanged from spec 009).

| HTTP verb | Route | Action method | Returns | Description |
|---|---|---|---|---|
| GET | `/Admin/Reports` | `Index(DateRange? range)` | `View(DashboardViewModel)` | Dashboard. Three KPI rows + sub-tab strip + global date-range picker (defaults to last 30 days when `range` is null). |
| GET | `/Admin/Reports/Applications` | `Applications(ListApplicationsRequest req)` | `View(ApplicationsViewModel)` | Applications report. Querystring binds to `req`. |
| GET | `/Admin/Reports/Applicants` | `Applicants(ListApplicantsRequest req)` | `View(ApplicantsViewModel)` | Applicants report. |
| GET | `/Admin/Reports/FundedItems` | `FundedItems(ListFundedItemsRequest req)` | `View(FundedItemsViewModel)` | Funded Items report. |
| GET | `/Admin/Reports/Aging` | `Aging(ListAgingApplicationsRequest req)` | `View(AgingApplicationsViewModel)` | Aging Applications report. |
| GET | `/Admin/Reports/Applications/Export` | `ExportApplications(ListApplicationsRequest req)` | `FileStreamResult` (`text/csv`) | CSV export of currently-filtered Applications dataset. |
| GET | `/Admin/Reports/Applicants/Export` | `ExportApplicants(ListApplicantsRequest req)` | `FileStreamResult` | CSV export of currently-filtered Applicants dataset. |
| GET | `/Admin/Reports/FundedItems/Export` | `ExportFundedItems(ListFundedItemsRequest req)` | `FileStreamResult` | CSV export of currently-filtered Funded Items dataset. |
| GET | `/Admin/Reports/Aging/Export` | `ExportAging(ListAgingApplicationsRequest req)` | `FileStreamResult` | CSV export of currently-filtered Aging dataset. |

**Non-Admin requests on any route:** 403 Forbidden (or login redirect when unauthenticated). Inherited from spec 009 cookie configuration; this spec does not change `AccessDeniedPath`.

**Malformed querystring:** controller catches `ModelState` errors, surfaces a single named-rule error via `ViewData["ReportFilterError"]`, and renders the report with safe defaults (does NOT return HTTP 500). Confirmed by FR-022.

**CSV row-bound exceeded:** controller catches `CsvRowBoundExceededException` and returns `BadRequest(new { error = "CsvRowBoundExceeded", limit, actualCount, hint = "Narrow your filter and try again." })`. Tests assert HTTP 400 + JSON body shape.

---

## Sub-tab strip

A new `_ReportSubTabs` partial under `Views/Shared/Components/_ReportSubTabs.cshtml` accepts a single argument `(string ActiveTab)` taking one of `"Applications"`, `"Applicants"`, `"FundedItems"`, `"Aging"`, or `"Dashboard"` (the dashboard's `Index.cshtml` passes `"Dashboard"` so no sub-tab is highlighted). The partial renders a `<ul class="nav nav-tabs">` Tabler component with four entries:

| Active tab | Label rendered | Href |
|---|---|---|
| n/a | "Applications" | `/Admin/Reports/Applications` |
| n/a | "Applicants" | `/Admin/Reports/Applicants` |
| n/a | "Funded Items" | `/Admin/Reports/FundedItems` |
| n/a | "Aging" | `/Admin/Reports/Aging` |

The dashboard view includes the partial above its KPI rows so an Admin can jump to any detail report from the same surface.

---

## Application service interface

### `IAdminReportsService`

**File (NEW):** `src/FundingPlatform.Application/Admin/Reports/IAdminReportsService.cs`

```csharp
public interface IAdminReportsService
{
    Task<DashboardResult> GetDashboardAsync(DateRange range, CancellationToken ct = default);

    Task<ListApplicationsResult>      ListApplicationsAsync     (ListApplicationsRequest      req, CancellationToken ct = default);
    Task<ListApplicantsResult>        ListApplicantsAsync       (ListApplicantsRequest        req, CancellationToken ct = default);
    Task<ListFundedItemsResult>       ListFundedItemsAsync      (ListFundedItemsRequest       req, CancellationToken ct = default);
    Task<ListAgingApplicationsResult> ListAgingApplicationsAsync(ListAgingApplicationsRequest req, CancellationToken ct = default);

    // Each export streams the currently-filtered dataset as CSV. Throws CsvRowBoundExceededException
    // when the row count exceeds AdminReportsOptions.CsvRowLimit. Streams via IAsyncEnumerable<string>
    // (one string per CSV line) so memory is bounded regardless of dataset size.
    IAsyncEnumerable<string> ExportApplicationsCsvAsync     (ListApplicationsRequest      req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportApplicantsCsvAsync       (ListApplicantsRequest        req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportFundedItemsCsvAsync      (ListFundedItemsRequest       req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportAgingApplicationsCsvAsync(ListAgingApplicationsRequest req, CancellationToken ct = default);
}
```

The `IAsyncEnumerable<string>` shape lets the controller wire each line to `Response.WriteAsync` directly without ever materializing the full CSV in memory.

### `IReportQueryService`

**File (NEW):** `src/FundingPlatform.Application/Interfaces/IReportQueryService.cs`

```csharp
public interface IReportQueryService
{
    IQueryable<ApplicationRowDto>      ApplicationsQuery     (ListApplicationsRequest      req);
    IQueryable<ApplicantRowDto>        ApplicantsQuery       (ListApplicantsRequest        req);
    IQueryable<FundedItemRowDto>       FundedItemsQuery      (ListFundedItemsRequest       req);
    IQueryable<AgingApplicationRowDto> AgingApplicationsQuery(ListAgingApplicationsRequest req);

    Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default);
}
```

Each `*Query(...)` returns an `IQueryable<RowDto>` so `AdminReportsService` can apply `Skip(...)/Take(...)/OrderBy(...)/CountAsync(...)/AsAsyncEnumerable()` without re-doing the joins. The implementation in Infrastructure (`ReportQueryService`) builds the projection with `.AsNoTracking()` and flattens every join into the outer DTO so the resulting SQL is one statement per call.

---

## Request DTOs (querystring-bound)

All request DTOs are defined in `src/FundingPlatform.Application/Admin/Reports/DTOs/`. Each property's name maps 1:1 to its querystring key.

### `DateRange`

```csharp
public sealed record DateRange(DateOnly From, DateOnly To);
```

Bound from `?from=YYYY-MM-DD&to=YYYY-MM-DD`. When absent, controller defaults to `(Today-30d, Today)`.

### `ListApplicationsRequest`

| Property | Querystring key | Type | Default |
|---|---|---|---|
| `States` | `states` | `IReadOnlyList<ApplicationState>` | empty (= all) |
| `From` | `from` | `DateOnly?` | null |
| `To` | `to` | `DateOnly?` | null |
| `Search` | `search` | `string?` | null |
| `HasAgreement` | `hasAgreement` | `bool?` | null |
| `HasActiveAppeal` | `hasActiveAppeal` | `bool?` | null |
| `Page` | `page` | `int` | 1 |
| `PageSize` | `pageSize` | `int` | 25 (fixed; ignored if client passes a different value — see research.md §1) |
| `Sort` | `sort` | `string` | `"updated-desc"` |

Sort tokens for Applications: `"updated-desc"` (default), `"updated-asc"`, `"submitted-desc"`, `"submitted-asc"`, `"resolved-desc"`, `"resolved-asc"`, `"applicant-asc"`, `"applicant-desc"`.

### `ListApplicantsRequest`

| Property | Querystring key | Default |
|---|---|---|
| `Search` | `search` | null |
| `HasExecutedAgreement` | `hasExecutedAgreement` | null |
| `LastActivityFrom` | `lastFrom` | null |
| `LastActivityTo` | `lastTo` | null |
| `Page` | `page` | 1 |
| `PageSize` | (fixed 25) | 25 |
| `Sort` | `sort` | `"executed-desc"` |

Sort tokens: `"executed-desc"` (default), `"executed-asc"`, `"approved-desc"`, `"approved-asc"`, `"applicant-asc"`, `"applicant-desc"`, `"lastActivity-desc"`, `"lastActivity-asc"`.

### `ListFundedItemsRequest`

| Property | Querystring key | Default |
|---|---|---|
| `CategoryIds` | `categoryIds` | empty (= all) |
| `SupplierIds` | `supplierIds` | empty (= all) |
| `AppStates` | `appStates` | empty (= ResponseFinalized + AgreementExecuted) |
| `ApprovedFrom` | `approvedFrom` | null |
| `ApprovedTo` | `approvedTo` | null |
| `ExecutedOnly` | `executedOnly` | false |
| `Page` | `page` | 1 |
| `PageSize` | (fixed 25) | 25 |
| `Sort` | `sort` | `"approvedAt-desc"` |

`AppStates` parameter values restricted to `ResponseFinalized` and `AgreementExecuted` per FR-026; controller surfaces a named-rule error if the querystring contains other values. Sort tokens: `"approvedAt-desc"` (default), `"approvedAt-asc"`, `"price-desc"`, `"price-asc"`, `"applicant-asc"`, `"applicant-desc"`, `"supplier-asc"`, `"supplier-desc"`.

### `ListAgingApplicationsRequest`

| Property | Querystring key | Default |
|---|---|---|
| `States` | `states` | non-terminal except Draft (= `Submitted, UnderReview, Resolved, ResponseFinalized`) |
| `ThresholdDays` | `threshold` | 14 |
| `Search` | `search` | null |
| `Page` | `page` | 1 |
| `PageSize` | (fixed 25) | 25 |
| `Sort` | `sort` | `"days-desc"` |

`ThresholdDays` validated server-side as `1 <= threshold <= 365`. Out-of-range values trigger FR-022's named-rule error path. Sort tokens: `"days-desc"` (default), `"days-asc"`, `"applicant-asc"`, `"applicant-desc"`, `"state-asc"`.

---

## Result DTOs

### `DashboardResult`

```csharp
public sealed record DashboardResult(
    IReadOnlyList<PipelineCount>                Pipeline,
    IReadOnlyList<FinancialKpi>                 Financial,
    IReadOnlyList<ApplicantKpi>                 Applicants);

public sealed record PipelineCount(ApplicationState State, int Count);

public sealed record FinancialKpi(
    string Label,                               // "Approved this period" | "Executed this period" | "Pending execution"
    IReadOnlyList<CurrencyAmount> Stack);       // empty list renders as a single em-dash row

public sealed record ApplicantKpi(string Label, int Count);
```

### List result envelope (per report)

```csharp
public sealed record ListApplicationsResult(
    IReadOnlyList<ApplicationRowDto> Rows,
    int TotalCount,
    ListApplicationsRequest          AppliedFilter);

public sealed record ListApplicantsResult(
    IReadOnlyList<ApplicantRowDto> Rows,
    int TotalCount,
    ListApplicantsRequest          AppliedFilter);

public sealed record ListFundedItemsResult(
    IReadOnlyList<FundedItemRowDto> Rows,
    int TotalCount,
    ListFundedItemsRequest          AppliedFilter);

public sealed record ListAgingApplicationsResult(
    IReadOnlyList<AgingApplicationRowDto> Rows,
    int TotalCount,
    ListAgingApplicationsRequest          AppliedFilter);
```

`AppliedFilter` is echoed back so views can reconstruct the filter bar without re-parsing the querystring.

### Row DTOs

#### `ApplicationRowDto`

| Field | Type |
|---|---|
| `AppId` | `int` |
| `ApplicantFullName` | `string` |
| `ApplicantLegalId` | `string` |
| `State` | `ApplicationState` |
| `CreatedAt` | `DateTime` |
| `SubmittedAt` | `DateTime?` |
| `ResolvedAt` | `DateTime?` |
| `ItemCount` | `int` |
| `TotalApproved` | `IReadOnlyList<CurrencyAmount>` (per-currency stack) |
| `HasAgreement` | `bool` |
| `HasActiveAppeal` | `bool` |

#### `ApplicantRowDto`

| Field | Type |
|---|---|
| `FullName` | `string` |
| `LegalId` | `string` |
| `Email` | `string` |
| `TotalApps` | `int` |
| `ResolvedCount` | `int` |
| `ResponseFinalizedCount` | `int` |
| `AgreementExecutedCount` | `int` |
| `ApprovalRate` | `decimal?` (em-dash if null) |
| `TotalApproved` | `IReadOnlyList<CurrencyAmount>` |
| `TotalExecuted` | `IReadOnlyList<CurrencyAmount>` |
| `LastActivity` | `DateTime?` |

#### `FundedItemRowDto`

| Field | Type |
|---|---|
| `AppId` | `int` |
| `ApplicantFullName` | `string` |
| `ItemProductName` | `string` |
| `CategoryName` | `string` |
| `SupplierName` | `string` |
| `SupplierLegalId` | `string?` |
| `Price` | `decimal` |
| `Currency` | `string` |
| `AppState` | `ApplicationState` |
| `AppSubmittedAt` | `DateTime?` |
| `ApprovedAt` | `DateTime?` (em-dash if missing) |
| `HasAgreement` | `bool` |
| `Executed` | `bool` |

#### `AgingApplicationRowDto`

| Field | Type |
|---|---|
| `AppId` | `int` |
| `ApplicantFullName` | `string` |
| `ApplicantEmail` | `string` |
| `ApplicantLegalId` | `string` |
| `State` | `ApplicationState` |
| `DaysInCurrentState` | `int` |
| `LastTransitionAt` | `DateTime?` |
| `LastActor` | `string?` (em-dash if missing) |
| `ItemCount` | `int` |
| `TotalApproved` | `IReadOnlyList<CurrencyAmount>` |

---

## CSV column maps

Every CSV exporter emits a single header row followed by the body. UTF-8 with BOM (so Excel auto-detects encoding); RFC 4180 quoting (double-quote-escaped). Date columns format as `yyyy-MM-ddTHH:mm:ssZ` (round-trip ISO 8601). Decimal columns format with `InvariantCulture` (period as decimal separator) so spreadsheets across locales behave predictably. Per-(entity, currency) row expansion: when a row's `TotalApproved` (or analogous) has multiple currencies, the CSV emits one row per currency with the non-monetary columns repeated.

### Applications CSV

```text
"App Id","Applicant Name","Applicant Legal Id","State","Created","Submitted","Resolved","Item Count","Approved Amount","Currency","Has Agreement","Has Active Appeal"
```

### Applicants CSV

```text
"Full Name","Legal Id","Email","Total Apps","Resolved Count","Response Finalized Count","Agreement Executed Count","Approval Rate","Approved Amount","Executed Amount","Currency","Last Activity"
```

(Note: per-currency expansion produces one row per currency with `Approved Amount` and `Executed Amount` in that currency. Currency-agnostic columns repeat.)

### Funded Items CSV

```text
"App Id","Applicant Name","Item Product Name","Category","Supplier","Supplier Legal Id","Price","Currency","App State","App Submitted","Approved At","Has Agreement","Executed"
```

### Aging CSV

```text
"App Id","Applicant Name","Email","Legal Id","State","Days In Current State","Last Transition","Last Actor","Item Count","Approved Amount","Currency"
```

---

## Sidebar entry (preserved from spec 009)

The role-aware sidebar entry "Reports" → `/Admin/Reports` is unchanged. This spec adds NO new sidebar entries. The four detail reports are reachable only via the sub-tab strip on `/Admin/Reports`. (FR-011.)

---

## Error-code map

| Condition | HTTP | Body shape | Source |
|---|---|---|---|
| Non-Admin requests `/Admin/Reports/...` | 403 | (cookie redirect to `/Account/AccessDenied` per spec 009) | spec 009 inheritance |
| Unauthenticated requests `/Admin/Reports/...` | 302 → `/Account/Login` | n/a | spec 009 inheritance |
| Malformed querystring (e.g., bad date format, threshold out of range) | 200 (renders with safe defaults + named-rule error in `ViewData["ReportFilterError"]`) | n/a | FR-022 |
| CSV export refused (row-bound exceeded) | 400 | `{ error: "CsvRowBoundExceeded", limit: int, actualCount: int, hint: string }` | FR-021 |
| Quotation form submitted with invalid Currency (length ≠ 3) | 200 (re-render with model-state error) | n/a | FR-004 |
| Startup probe fails (missing `DefaultCurrency`) | host throws on `Build()` | n/a | FR-007 |

---

## Configuration contract

```json
{
  "AdminReports": {
    "DefaultCurrency": "COP",
    "CsvRowLimit": 50000
  }
}
```

- `DefaultCurrency`: required, exactly 3 characters, uppercased on read. Startup-fail-fast on missing.
- `CsvRowLimit`: optional, default `50000`. Configurable for one-off audits.

---

## Summary of new contracts

- 1 controller class extended (`AdminReportsController`) — 9 actions total (5 list + 4 export).
- 2 application service interfaces (`IAdminReportsService`, `IReportQueryService`).
- 4 request DTOs, 4 result envelopes, 4 row DTOs, 1 dashboard result DTO, 1 date-range record.
- 2 new partials (`_KpiTile`, `_ReportSubTabs`).
- 1 new options class (`AdminReportsOptions`).
- 1 new application exception (`CsvRowBoundExceededException`).
- 1 new value object (`CurrencyAmount`).
- 4 CSV column maps (one per detail report).

No new authentication / authorization gates beyond what spec 009 already provides.
