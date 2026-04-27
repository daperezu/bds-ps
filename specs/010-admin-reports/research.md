# Research: Admin Reports Module

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-26

This document resolves the six planning-phase pins surfaced by `REVIEW-SPEC.md`. Each section follows the format: **Decision · Rationale · Alternatives considered**.

---

## 1. Page-size convention reuse from the review queue

**Decision:** Page size = **`25`**, applied uniformly to every detail report (Applications, Applicants, Funded Items, Aging). Codified as a `const int PageSize = 25` on `AdminReportsService` (matching the existing `ReviewService.PageSize = 25` pattern). The page-size value is NOT exposed as a per-request parameter in v1 — every detail report ships with a fixed page size.

**Rationale:** The signing inbox (spec 007) and review queue (spec 002) both use `25`. The codebase already has the precedent (`src/FundingPlatform.Application/Services/ReviewService.cs:16`). Reusing the constant rather than introducing a new configurable knob keeps the dashboard's "feels like the rest of the platform" promise (FR-028) and avoids a new YAGNI surface.

**Alternatives considered:**
- *Configurable per request (`?pageSize=50`)*: Adds a request-DTO field, a guard against arbitrarily-large requests, and a UI affordance. v1 doesn't need it; can land in a future spec if user testing surfaces the demand.
- *Per-report different sizes (e.g., `25` for tables, `100` for Funded Items because it's a long line-item view)*: Inconsistent UX; complicates the `_DataTable` partial; v1 rejects.
- *Lift to a `SystemConfiguration` key (`AdminReports:PageSize`)*: Extra moving part with no current operational value. Easy to refactor into config later if needed.

**Closes spec assumption:** "The page-size convention (open thread from brainstorm #02) is settled at planning time" → **settled as 25**.

---

## 2. CSV export upper-bound numeric value

**Decision:** Upper bound = **`50,000` rows**. Configured as `AdminReports:CsvRowLimit` (integer) in `appsettings.json`, bound to `AdminReportsOptions.CsvRowLimit`. The default in code is `50000`. The bound is enforced **before** streaming begins by running a `COUNT(*)` against the same projection; if the count exceeds the limit, `AdminReportsService` throws `CsvRowBoundExceededException(limit, actualCount)` which the controller surfaces as an HTTP 400 with an error model carrying both numbers and a "narrow your filter" hint. No partial CSV is ever written.

**Rationale:** A grants platform's audit-grade reports must never silently truncate. 50,000 rows at ~1KB per row equates to ~50 MB — beyond what an Admin can reasonably analyze in a spreadsheet, and a clear signal that the user wanted a sub-period or sub-cohort. The configuration knob lets operators raise the bound for one-off audits without a code change.

**Alternatives considered:**
- *No bound (stream until done)*: Fine for small datasets; an unbounded export risks timeouts and OOM in adversarial cases. The config knob can effectively disable the bound (set to `int.MaxValue`).
- *Half-export with warning*: Worst failure mode for an audit-grade report; rejected.
- *Email export when the dataset is large*: Requires SMTP infrastructure (spec-level out-of-scope); future spec.

**Closes spec assumption:** "The CSV export's hard upper bound (e.g., 50,000 rows is a reasonable starting point) is settled at planning time" → **settled as 50,000, configurable via `AdminReports:CsvRowLimit`**.

---

## 3. `DefaultCurrency` configuration key shape and per-environment conventions

**Decision:** Configuration key path is **`AdminReports:DefaultCurrency`** (string, exactly 3 characters, case-insensitive — uppercased on read). Bound to `AdminReportsOptions.DefaultCurrency`.

**Per-environment wiring:**

| Environment | Source | Example |
|---|---|---|
| Development (Aspire) | `AppHost` user-secrets / `appsettings.Development.json` | `{ "AdminReports": { "DefaultCurrency": "USD" } }` |
| Test (E2E fixture) | `AspireFixture.cs` sets via `--AdminReports:DefaultCurrency=COP` parameter on `dotnet run --project src/FundingPlatform.AppHost` (mirrors the existing `--EphemeralStorage=true` precedent in CLAUDE.md) | passed by the fixture |
| Production | environment variable `AdminReports__DefaultCurrency` OR `appsettings.Production.json` | `AdminReports__DefaultCurrency=COP` |

**Startup probe:** `Program.cs` reads the value AFTER `builder.Configuration` is bound and BEFORE `builder.Build()`. If `string.IsNullOrWhiteSpace(value)` or `value.Length != 3`, the host throws `InvalidOperationException` with message "AdminReports:DefaultCurrency is required and must be a 3-character currency code (e.g., 'COP', 'USD'). Set the configuration value before starting the host." No fallback default. The probe message identifies the configuration key path so an operator knows exactly what to fix.

**dacpac coupling:** The post-deployment script `SeedData.sql` also receives the `DefaultCurrency` via a sqlcmd variable (`$(DefaultCurrency)`) so the back-fill of legacy rows uses the same value. The dacpac publish profile in `FundingPlatform.AppHost` provides the variable at deploy time. This guarantees the column-default and the application-default cannot diverge.

**Rationale:** Mirrors the precedent set by `Admin:DefaultPassword` in spec 009 (key path under a top-level feature node, accessed via strongly-typed options binding). The `AdminReports:` prefix groups every reports-related config (currently only `DefaultCurrency` and `CsvRowLimit`; future report features add to the same node). The startup-fail-fast aligns with FR-007's intent.

**Alternatives considered:**
- *Hardcoded fallback default (e.g., `"USD"`)*: Violates FR-007. Rejected.
- *Promote `DefaultCurrency` to a `SystemConfiguration` row only (no `appsettings` value)*: The DB already carries the row — but the application also needs the value at startup for form prefill before the DB is queried per-request. Reading from configuration at startup is faster than every request hitting the DB; the DB row exists for the dacpac backfill specifically. Both representations exist by design; `SeedData.sql` ensures they agree.
- *Sentinel value support (e.g., empty string allowed for "no default")*: Quotation form must always have a prefill; rejected.

**Closes spec assumption:** "The `DefaultCurrency` configuration key shape and per-environment conventions are settled at planning time" → **settled as `AdminReports:DefaultCurrency`, mirroring the spec 009 `Admin:DefaultPassword` shape**.

---

## 4. Spec 005 Funding Agreement PDF visual integrity verification

**Decision:** Visual integrity verified via a **manual side-by-side comparison** documented in `quickstart.md`, plus an **automated E2E text-extraction assertion** (`FundingAgreementPdfAssertions.AssertEachAmountHasCurrencyCode(byte[] pdf)`). PDF-snapshot regression infrastructure (e.g., pixel-diff against a golden PNG) is **not** introduced in v1.

**Implementation outline:**

1. **Manual procedure (one-time, during plan execution):** generate a Funding Agreement before US1 ships, save the PDF as `specs/010-admin-reports/quickstart-fixtures/funding-agreement-before-currency.pdf`. After US1 ships, generate the same agreement on the same seeded data, save as `funding-agreement-after-currency.pdf`. Manually compare side-by-side; assert that every amount has gained a 3-character prefix and the page layout (column widths, line breaks, signature block positions) is unchanged.

2. **Automated E2E assertion (per-test):** `CurrencyRolloutTests` includes a test that generates an agreement on an application carrying mixed-currency quotations (one `COP`, one `USD`), reads the PDF bytes, runs them through `iText7`-based or `Syncfusion.PdfTextExtractor`-based text extraction, and asserts: (a) every amount line in the items table contains exactly one currency code from the seed, (b) no amount line is missing a currency code, (c) the rendered amount string matches `^[A-Z]{3} [\d.,]+$`. (Note: text-extraction precision depends on the renderer's flow — the assertion may need to allow whitespace flexibility; the test author should iterate until it cleanly distinguishes "currency-coded amount" from "naked decimal".)

3. **Renderer reuse confirmed:** the existing `SyncfusionFundingAgreementPdfRenderer.RenderAsync(string html, string? baseUrl)` interface is unchanged; the change is purely in the Razor template `_FundingAgreementItemsTable.cshtml`. No license-revisit on Syncfusion.

**Rationale:** The change to the PDF template is a single Razor token addition (`@Model.Currency` prefix on the amount cell). PDF-snapshot regression infrastructure (Verify.NET, ImageMagick-diff) is high-value when many templates evolve concurrently; this spec touches one template. Manual comparison + automated text-extraction is sufficient for v1 and aligns with constitution VI (Simplicity).

**Alternatives considered:**
- *Verify.NET PDF snapshot tests*: Capability is real but introduces a new test-stack dependency and a fixture-file management discipline; deferred to a future spec when more PDF templates are in flight.
- *No automated assertion (manual only)*: Fragile; a future template edit could silently break the change. The text-extraction assertion is cheap insurance.
- *Re-render every existing PDF*: explicitly forbidden by FR-005. Rejected.

**Closes spec assumption:** "The spec 005 Funding Agreement template can absorb a one-token currency-code addition without triggering a license-revisit on Syncfusion" → **confirmed; renderer signature unchanged, template-only change**.

---

## 5. `VersionHistory` adequacy for "approved-at" / "last actor" / "days in current state"

**Decision:** `VersionHistory` is adequate for the cited columns. The implementation maps action strings to lifecycle events. Where an applicable entry is missing, columns degrade to em-dash (`—`) per the spec's edge-case clauses; computations fall back to `Application.UpdatedAt`.

**Mapping:**

| Report column | Source | Action string(s) to query |
|---|---|---|
| Funded Items: `ApprovedAt` | `VersionHistory` entries on the parent application | `"Finalize"` (the `ReviewService.FinalizeReview` call adds this entry when transitioning to `Resolved`) |
| Aging: `LastTransitionAt` | `VersionHistory` entries on the application, max timestamp matching the action set for the application's CURRENT state | per-state action map below |
| Aging: `LastActor` | `VersionHistory.UserId` of the row identified above | same lookup |
| Aging: `DaysInCurrentState` | `(DateTime.UtcNow - LastTransitionAt).TotalDays` (rounded down) | derived |

**Action-string → state map (extracted from existing services):**

| Application state | Triggering action string(s) | Source file |
|---|---|---|
| `Submitted` | `"Submitted"` | `ApplicationService.cs:80` |
| `UnderReview` | (no dedicated entry — `Application.StartReview()` does NOT call `AddVersionHistory`) | n/a — see fallback below |
| `Resolved` | `"Finalize"` | `ReviewService.cs:176` |
| `Draft` (after `SendBack`) | `"SendBack"` | `ReviewService.cs:152` |
| `ResponseFinalized` | `"ApplicantResponseSubmitted"` (or whatever string `ApplicantResponseService` writes) | `ApplicantResponseService.cs:50` (TODO confirm string at implementation time) |
| `AppealOpen` | `"AppealOpened"` (or service-defined string) | `ApplicantResponseService.cs:83` (confirm) |
| `AgreementExecuted` | `"AgreementExecuted"` (or service-defined string) | `SignedUploadService.cs:200/276/324/368/416` (confirm) |

**Fallback rules:**
- For an application currently in `UnderReview` (no triggering action string is written when `StartReview()` runs), the Aging report computes `DaysInCurrentState` from the **timestamp of the most recent state-changing action that produced a transition AWAY FROM `UnderReview`'s predecessor** — in practice, that is the most recent `"Submitted"` entry. If no `"Submitted"` entry exists (legacy data), fall back to `Application.UpdatedAt`. `LastActor` falls back to em-dash.
- For Funded Items where the parent application's `Finalize` entry is missing (legacy data), `ApprovedAt` is em-dash. The row still appears with all other columns populated.

**Implementation note for the implementor:** during US6 implementation, **(a)** confirm each action-string value at the source file and pin the literal in a `static readonly` constant on `ReportQueryService`, **(b)** add a small in-memory unit test asserting the action-string list matches what current services write — so adding a new action without updating the map fails CI.

**Rationale:** `VersionHistory.Timestamp` and `VersionHistory.UserId` exist on every entry (verified in `VersionHistory.cs`); the only gap is the missing entry on `UnderReview` entry — handled by the documented fallback. The em-dash fallback is consistent with the spec's edge-case clauses on legacy data.

**Alternatives considered:**
- *Add a `VersionHistory` entry to `Application.StartReview()`*: Would close the `UnderReview` gap cleanly, but it touches spec 002 behavior and risks regressing existing tests. Deferred — note as a future cleanup; v1's fallback is acceptable.
- *Introduce per-item version history*: Out of scope. Funded Items' `ApprovedAt` correctly resolves to the application-level `Finalize` timestamp, which is when the item was approved as part of the finalization batch.
- *Use `Application.UpdatedAt` exclusively (skip VersionHistory)*: Loses the `LastActor` column entirely. Rejected.

**Closes spec assumption:** "The existing `VersionHistory` entity carries timestamps and actor user ids sufficient to source 'approved-at' on the Funded Items report and 'last actor' + 'days in current state' on the Aging Applications report" → **confirmed sufficient; one documented fallback for `UnderReview` entries**.

---

## 6. dacpac deployment-step ordering for `Currency` column add → backfill → NOT NULL

**Decision:** Three-step deployment, all artifacts owned by the dacpac project.

1. **Schema add (declarative, dacpac):** `dbo.Quotations.sql` declares `[Currency] NVARCHAR(3) NULL`. dacpac publish adds the column as nullable to existing databases on first run; existing rows have `NULL`.

2. **Post-deploy backfill (`SeedData.sql`):** the existing post-deployment script — already bound to the dacpac via `<PostDeploy Include="PostDeployment\SeedData.sql" />` — gains two appended blocks:

   ```sql
   -- Block 1: ensure DefaultCurrency configuration row exists
   IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = N'DefaultCurrency')
       INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
       VALUES (N'DefaultCurrency', N'$(DefaultCurrency)', N'Default 3-character ISO 4217 currency code applied to new quotations and historical backfill', GETUTCDATE());

   -- Block 2: backfill any NULL Currency rows with the configured default
   UPDATE [dbo].[Quotations]
       SET [Currency] = (SELECT [Value] FROM [dbo].[SystemConfigurations] WHERE [Key] = N'DefaultCurrency')
       WHERE [Currency] IS NULL;
   ```

   `$(DefaultCurrency)` is a sqlcmd variable supplied by the publish profile / Aspire deployment. Re-running these blocks is a no-op (Block 1 is `IF NOT EXISTS`-guarded; Block 2 only updates `WHERE [Currency] IS NULL`).

3. **NOT NULL tightening (`SeedData_TightenQuotationCurrency.sql`):** a NEW post-deploy script registered alongside `SeedData.sql` in the `.sqlproj` (sequential `<PostDeploy>` items run in declaration order). It executes:

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

   Idempotent: a second run finds the column already `NOT NULL` and skips. The check uses `sys.columns` so it works regardless of whether any rows still violate the constraint (Block 2 above guarantees zero violators).

**Why the column starts nullable in the schema declaration:** dacpac's column-add behavior on a non-empty table will fail if the new column is declared `NOT NULL` without a default — and a hardcoded default-currency string in the schema would diverge from the configured value. The nullable-then-tightened pattern avoids this and keeps the configured `DefaultCurrency` as the single source of truth.

**Why a separate file rather than appending to `SeedData.sql`:** keeps schema-modifying DDL out of a file that is otherwise pure data seeding; makes the intent explicit; lets future audits see the tightening script as a one-time migration even though it is technically idempotent.

**`.sqlproj` change:** add the new post-deploy file to the `<ItemGroup>`:

```xml
<PostDeploy Include="PostDeployment\SeedData.sql" />
<PostDeploy Include="PostDeployment\SeedData_TightenQuotationCurrency.sql" />
```

(dacpac runs them in declaration order.)

**Rationale:** This is the canonical schema-first pattern for adding a `NOT NULL` column to a non-empty table without an EF migration. Each step is idempotent and survives re-publishes. The pattern matches the spec 009 sentinel-flag column add (which used a similar nullable-then-defaulted approach).

**Alternatives considered:**
- *Add the column as `NOT NULL` with a hardcoded `DEFAULT 'USD'`*: Diverges from the configured `DefaultCurrency`; would silently miscount currency on any deployment that meant to start with a different default. Rejected.
- *Use an EF migration*: Forbidden by constitution IV.
- *Keep the column nullable forever*: Violates FR-001 ("required"). Rejected.
- *Single-script approach (do everything in `SeedData.sql`)*: Mixes schema and data; harder to audit. Rejected.

**Closes spec assumption:** "Confirm dacpac deployment order: column add → backfill → tighten to NOT NULL" → **settled as a three-step declarative pattern with explicit `.sqlproj` registration**.

---

## Summary of resolutions

| # | Pin | Resolution |
|---|---|---|
| 1 | Page-size convention | `25` (matches `ReviewService.PageSize`); fixed in v1, no per-request override |
| 2 | CSV upper-bound | `50,000`, configurable via `AdminReports:CsvRowLimit`; refuse-on-overflow with `CsvRowBoundExceededException` |
| 3 | `DefaultCurrency` config | `AdminReports:DefaultCurrency` (3-char string); startup-fail-fast on missing; passed as sqlcmd variable to dacpac for backfill consistency |
| 4 | PDF visual integrity | Manual side-by-side comparison + automated E2E text-extraction assertion in `CurrencyRolloutTests`; no PDF-snapshot infrastructure |
| 5 | `VersionHistory` adequacy | Sufficient with documented action-string-to-state map; one fallback for `UnderReview` (no entry written by `StartReview()`); em-dash on legacy data gaps |
| 6 | dacpac ordering | Three-step: nullable column add → `SeedData.sql` (configuration row + backfill) → `SeedData_TightenQuotationCurrency.sql` (idempotent `NOT NULL` tightening); registered in `.sqlproj` in declaration order |

All six pins resolved. No remaining unknowns block Phase 1.
