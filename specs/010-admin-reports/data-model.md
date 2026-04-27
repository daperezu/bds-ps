# Data Model: Admin Reports Module

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-26

This document specifies the data-model changes for spec 010. Reports themselves are read-only projections over existing aggregates and have no persistent shape; only the `Currency` rollout introduces persisted changes.

---

## Persisted changes

### 1. `Quotation` (existing entity, extended)

**File:** `src/FundingPlatform.Domain/Entities/Quotation.cs`

**New property:**

| Property | Type | Constraint | Notes |
|---|---|---|---|
| `Currency` | `string` | exactly 3 characters; uppercase canonicalized | Stored verbatim; not validated against ISO 4217. |

**Constructor signature change:**

```csharp
// BEFORE
public Quotation(int supplierId, int documentId, decimal price, DateOnly validUntil)

// AFTER
public Quotation(int supplierId, int documentId, decimal price, DateOnly validUntil, string currency)
```

The constructor canonicalizes (`currency.Trim().ToUpperInvariant()`) and validates length-equals-3; on failure throws `ArgumentException("Currency must be a 3-character code.", nameof(currency))`.

**New mutator:**

```csharp
public void EditCurrency(string code)
```

Same validation; replaces `Currency` in place. Used by the existing quotation-edit flow when the Admin (or applicant) changes the code on an existing quotation.

**Cascading code touches:**

- `src/FundingPlatform.Application/Applications/Commands/AddSupplierQuotationCommand.cs` — add `Currency` to the command record; pass through to `Item.AddQuotation(...)` (which currently calls `new Quotation(...)`). `Item.AddQuotation` gains a `string currency` parameter.
- `src/FundingPlatform.Domain/Entities/Item.cs::AddQuotation` — signature gains `string currency`; passes through to the constructor.
- `src/FundingPlatform.Application/DTOs/QuotationDto.cs` — add `Currency` property.
- `src/FundingPlatform.Web/ViewModels/AddQuotationViewModel.cs` — add `[Required, StringLength(3, MinimumLength = 3)]` `Currency` property; default-value bound from `AdminReportsOptions.DefaultCurrency`.
- `src/FundingPlatform.Web/Views/Quotation/Add.cshtml` and `Edit.cshtml` — add input field.

**Database table change:**

`src/FundingPlatform.Database/Tables/dbo.Quotations.sql` gains:

```sql
[Currency] NVARCHAR(3) NULL  -- Tightened to NOT NULL by post-deploy (see research.md §6)
```

### 2. `SystemConfiguration` (existing entity, new seed row only)

**File:** `src/FundingPlatform.Database/PostDeployment/SeedData.sql`

A new idempotent block appends:

```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = N'DefaultCurrency')
    INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
    VALUES (N'DefaultCurrency', N'$(DefaultCurrency)', N'Default 3-character ISO 4217 currency code applied to new quotations and historical backfill', GETUTCDATE());
```

`$(DefaultCurrency)` is a sqlcmd variable supplied by the dacpac publish profile (see research.md §3 and §6 for the per-environment wiring).

The C# `SystemConfiguration` entity itself requires no code changes — it is a generic key-value store and already supports new rows by virtue of the existing `Key` uniqueness constraint.

### 3. `dbo.Quotations` backfill

**File:** `src/FundingPlatform.Database/PostDeployment/SeedData.sql` (continued)

Appends after the `DefaultCurrency` insertion block:

```sql
UPDATE [dbo].[Quotations]
    SET [Currency] = (SELECT [Value] FROM [dbo].[SystemConfigurations] WHERE [Key] = N'DefaultCurrency')
    WHERE [Currency] IS NULL;
```

Idempotent: a second run is a no-op because every row now has a non-null `Currency`.

### 4. `dbo.Quotations.[Currency]` tightening

**File (NEW):** `src/FundingPlatform.Database/PostDeployment/SeedData_TightenQuotationCurrency.sql`

```sql
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'Quotations' AND c.name = N'Currency' AND c.is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[Quotations] ALTER COLUMN [Currency] NVARCHAR(3) NOT NULL;
END;
```

Idempotent. `.sqlproj` registers this file as a `<PostDeploy>` immediately after `SeedData.sql` so deployment order is: schema add → SeedData.sql → SeedData_TightenQuotationCurrency.sql.

---

## New domain types

### `CurrencyAmount` (new value object)

**File (NEW):** `src/FundingPlatform.Domain/ValueObjects/CurrencyAmount.cs`

```csharp
public sealed record CurrencyAmount(string Currency, decimal Amount)
{
    public CurrencyAmount : this(currency, amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-character code.", nameof(currency));
        Currency = currency.Trim().ToUpperInvariant();
    }
}
```

Used as the cell type for any per-currency stack on the dashboard, the rows of the detail reports, and CSV per-(entity, currency) row expansion. Pure value object — no persistence, no EF mapping.

### `CsvRowBoundExceededException` (new application exception)

**File (NEW):** `src/FundingPlatform.Application/Exceptions/CsvRowBoundExceededException.cs`

```csharp
public sealed class CsvRowBoundExceededException : Exception
{
    public int Limit { get; }
    public int ActualCount { get; }

    public CsvRowBoundExceededException(int limit, int actualCount)
        : base($"CSV export refused: {actualCount} rows exceeds the configured limit of {limit}. Narrow your filter and try again.")
    {
        Limit = limit;
        ActualCount = actualCount;
    }
}
```

---

## Read-only entity surfaces consumed by reports

The following entities are read by `IReportQueryService` projections. None of these entities are modified by this feature.

### `Application` (read)

Used by every report. Fields touched: `Id`, `ApplicantId`, `State`, `CreatedAt`, `UpdatedAt`, `SubmittedAt`. Navigation: `Applicant`, `Items`, `VersionHistory`, `Appeals`, `FundingAgreement`. Counts of `Items` and `Appeals` (active / resolved) are computed at query time.

### `Applicant` (read)

Used by every report as a join target. Fields touched: `Id`, `UserId`, `LegalId`, `FirstName`, `LastName`, `Email`, `Phone`, `CreatedAt`. Navigation: `Applications`. Aggregates (total apps, totals per terminal state, approval rate, last-activity) computed at query time on the Applicants report.

### `Item` (read)

Used by Applications report (item count, total approved per currency) and Funded Items report (every approved item with selected supplier). Fields touched: `Id`, `ApplicationId`, `ProductName`, `CategoryId`, `ReviewStatus`, `SelectedSupplierId`. Navigation: `Category`, `SelectedSupplier`, `Quotations`. The selected-quotation price is `Quotations.First(q => q.SupplierId == SelectedSupplierId).Price`, joined with that quotation's `Currency`.

### `Quotation` (read, plus Currency rollout)

Used by Applications, Applicants, and Funded Items reports for `Price` + `Currency`. The Funded Items report scopes specifically on the *selected* quotation per item.

### `Supplier` (read)

Used by Funded Items report (display name + legal id). Fields touched: `Id`, `Name`, `LegalId`. The CURRENT name is rendered (no historical snapshot).

### `Category` (read)

Used by Funded Items report (filter + column). Fields touched: `Id`, `Name`.

### `VersionHistory` (read)

Used by Funded Items report (`ApprovedAt`) and Aging Applications report (`LastTransitionAt`, `LastActor`). Fields touched: `ApplicationId`, `UserId`, `Action`, `Timestamp`. The action-to-state map is documented in `research.md` §5.

### `FundingAgreement` (read, plus PDF render rollout)

Used by Applications report (`HasAgreement`) and Funded Items report (`HasAgreement`, `Executed`). Fields touched: `Id`, `ApplicationId`. Existence is the boolean signal; the agreement's content is not rendered into reports. The PDF-render change (US1) is in `_FundingAgreementItemsTable.cshtml` (Razor template); no entity changes.

### `Appeal` (read)

Used by Applications report (`HasActiveAppeal`). Fields touched: `Id`, `ApplicationId`, `Status`. The boolean signal is `Application.Appeals.Any(a => a.Status == AppealStatus.Open)`.

---

## Aggregations (computed in projections)

### Dashboard tile values

| Tile | Computation |
|---|---|
| Pipeline: count per state | `Applications.GroupBy(a => a.State).Where(state != Draft).Count()` |
| Financial: Approved this period (per currency) | `Items.Where(i => i.ReviewStatus == Approved && i.Application.Resolved-or-later in [from..to]).GroupBy(currency-of-selected-quotation).Sum(price)` |
| Financial: Executed this period (per currency) | Same as Approved but with `Application.State == AgreementExecuted` |
| Financial: Pending execution (per currency) | Approved minus Executed (computed in the projection or post-projection in C#) |
| Applicant: Active applicants | `Applicants.Where(any application in non-terminal state).Count()` |
| Applicant: Repeat applicants | `Applicants.Where(2+ submitted applications).Count()` |
| Applicant: New this period | `Applicants.Where(MinSubmittedAt within [from..to]).Count()` |

### Detail-report aggregations

- Applications report `TotalApproved` (per row, per currency): sum of selected-quotation prices over approved items, grouped by quotation currency.
- Applicants report `TotalApproved` and `TotalExecuted` (per row, per currency): sum across all the applicant's applications, grouped by currency.
- Applicants report `ApprovalRate`: `approved-items / total-items` across all the applicant's applications. Em-dash if total-items is zero.
- Funded Items report `ApprovedAt`: timestamp of the parent application's `Finalize` `VersionHistory` entry; em-dash if missing.
- Aging Applications report `DaysInCurrentState`: `(UtcNow - LastTransitionAt).TotalDays`, integer-floored. Fallback to `Application.UpdatedAt` for `UnderReview` (see research.md §5).

---

## Configuration model

### `AdminReportsOptions` (new options class)

**File (NEW):** `src/FundingPlatform.Web/Configuration/AdminReportsOptions.cs` (or `src/FundingPlatform.Application/Options/` if a parallel class exists for Users — confirm at implementation time)

```csharp
public sealed class AdminReportsOptions
{
    public const string SectionName = "AdminReports";

    public string? DefaultCurrency { get; set; }   // 3-char string; required at startup
    public int CsvRowLimit { get; set; } = 50_000; // configurable; default 50,000
}
```

Bound via `builder.Services.Configure<AdminReportsOptions>(builder.Configuration.GetSection(AdminReportsOptions.SectionName))`. The startup probe in `Program.cs` reads the bound value and aborts with `InvalidOperationException` when `DefaultCurrency` is null/empty or its length is not 3.

---

## Summary of persisted changes

| Change | File | Type | Migration risk |
|---|---|---|---|
| Add `Currency` column to `Quotations` | `dbo.Quotations.sql` | Schema | Low — nullable add, then idempotent backfill, then idempotent tighten |
| Add `DefaultCurrency` row to `SystemConfigurations` | `SeedData.sql` | Seed data | Low — idempotent insert |
| Backfill `Currency` on existing `Quotations` | `SeedData.sql` | Data migration | Low — idempotent update |
| Tighten `Currency` to NOT NULL | `SeedData_TightenQuotationCurrency.sql` | Schema | Low — idempotent ALTER, no row failures because backfill ran first |
| Add `CurrencyAmount` value object | C# domain | Code | None — pure value object |
| Add `CsvRowBoundExceededException` | C# application | Code | None |
| Add `AdminReportsOptions` | C# configuration | Code | None |

No new tables. No new EF migrations. No new managed storage subsystems.
