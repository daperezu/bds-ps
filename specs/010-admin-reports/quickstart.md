# Quickstart: Admin Reports Module

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-26

This document walks the manual smoke procedure for verifying spec 010 end-to-end. It is the bridge between automated E2E tests and human eyeballs — covering the dashboard, every detail report, CSV export, the per-quotation Currency rollout, and the Funding Agreement PDF visual comparison.

---

## Prerequisites

1. A clean dev environment running on this branch (`010-admin-reports`).
2. Configuration: `appsettings.Development.json` (or user-secrets) sets:
   ```json
   {
     "AdminReports": { "DefaultCurrency": "COP", "CsvRowLimit": 50000 }
   }
   ```
3. dacpac publish has run successfully (the post-deploy backfill needs `$(DefaultCurrency)`; for dev, the default is fine).
4. Aspire host running: `dotnet run --project src/FundingPlatform.AppHost` (dev mode, persistent volume).
5. The seeded demo users exist (`applicant@demo.com`, `reviewer@demo.com`, `admin@demo.com` per `IdentityConfiguration.cs`); the spec 009 sentinel admin (`admin@FundingPlatform.com`) also exists.

---

## US1 — Per-quotation Currency rollout

### 1.1 Form prefill

1. Sign in as `applicant@demo.com`.
2. Open or create an application; navigate to the Add Quotation form on any item.
3. **Verify**: the Currency field is visible, prefilled with `COP`, and editable.

### 1.2 Mixed-currency persistence

1. Add a quotation on item A with `Currency = COP` and any price (e.g., `1500000`).
2. Add a quotation on item B (or a different item on the same application) with `Currency = USD` and any price (e.g., `400`).
3. **Verify**: both quotations save without error. Re-open each item; confirm the saved currency code appears.

### 1.3 Validation

1. Open the Add Quotation form again.
2. Enter `Currency = "EU"` (2 characters).
3. Submit.
4. **Verify**: form re-renders with a single named-rule error pointing to the Currency field; nothing persists.

### 1.4 Funding Agreement PDF — currency rendering

1. Sign in as `reviewer@demo.com` (or any Admin) and progress the application to `ResponseFinalized` (use the existing review → applicant-response flows).
2. Generate the Funding Agreement PDF for the application.
3. Open the PDF.
4. **Verify**: each amount on the items table renders as `{Currency} {Amount}`, e.g. `COP 1.234.567` and `USD 12.500`. No amount is bare-decimal.
5. **Verify**: line breaks, column widths, and the signature block positions are unchanged versus the pre-currency template (compare against `quickstart-fixtures/funding-agreement-before-currency.pdf` if captured before this spec lands; otherwise visually inspect for reasonable layout).

### 1.5 dacpac backfill — legacy data

1. Stop the Aspire host.
2. (Dev convenience) drop and recreate the SQL Server container so the database is fresh, OR start from a database snapshot taken before this spec's dacpac was applied.
3. Re-publish the dacpac with `DefaultCurrency=COP` set.
4. After deploy, query: `SELECT COUNT(*) FROM dbo.Quotations WHERE [Currency] IS NULL;`
5. **Verify**: returns `0`. Every legacy row carries `'COP'`.
6. **Verify**: the column is `NOT NULL` per `INFORMATION_SCHEMA.COLUMNS` (`SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Quotations' AND COLUMN_NAME='Currency';` returns `'NO'`).

### 1.6 Startup-fail-fast

1. Remove `AdminReports:DefaultCurrency` from the configuration (delete the key from user-secrets / appsettings).
2. Attempt to start the Aspire host.
3. **Verify**: the host throws `InvalidOperationException` referencing the missing `AdminReports:DefaultCurrency` key. The host does NOT start.
4. Restore the configuration; the host starts cleanly.

---

## US2 — Dashboard at `/Admin/Reports`

### 2.1 First-load layout

1. Sign in as `admin@demo.com` (Admin role).
2. Click `Reports` in the sidebar OR navigate directly to `/Admin/Reports`.
3. **Verify**: the page renders three KPI rows (Pipeline, Financial, Applicant), the sub-tab strip lists `Applications | Applicants | Funded Items | Aging`, and the global date-range picker shows the last 30 days by default.

### 2.2 Per-currency Financial stack

1. Confirm the seed data has at least one quotation in `COP` and one in `USD` (US1 §1.2 ensures this).
2. **Verify**: each tile in the Financial row shows two stacked sub-rows: one `COP` row and one `USD` row. No tile collapses two currencies into one number.

### 2.3 Date-range picker

1. Note the current values of the Financial row tiles and the "New this period" applicant tile.
2. Change the date-range picker to the last 7 days; submit.
3. **Verify**: the Financial row tiles and the "New this period" applicant tile recompute. Pipeline tiles are unchanged.

### 2.4 Sub-tab navigation

1. Click `Applications` in the sub-tab strip.
2. **Verify**: navigation lands on `/Admin/Reports/Applications`.
3. Click `Applicants`, `Funded Items`, `Aging` in turn — confirm each lands on its dedicated route.

### 2.5 Empty-state

1. (Dev only) drop the database and re-deploy with no seed beyond roles.
2. Open `/Admin/Reports`.
3. **Verify**: every Pipeline tile shows `0`. Every Financial tile shows a single em-dash row. Every Applicant tile shows `0`. No error is shown.

### 2.6 Non-Admin gate

1. Sign out, sign in as `reviewer@demo.com`.
2. Navigate to `/Admin/Reports`.
3. **Verify**: the response is 403 (the `/Account/AccessDenied` page renders).
4. Sign out; navigate to `/Admin/Reports` unauthenticated.
5. **Verify**: redirect to `/Account/Login`.

---

## US3 — Applications report

### 3.1 List + filter + sort + paginate

1. As Admin, open `/Admin/Reports/Applications`.
2. **Verify**: every application is listed across multiple pages (assuming seeded volume); default sort is most-recently-updated descending.
3. Apply State = `UnderReview`; submit.
4. **Verify**: only `UnderReview` rows appear; the URL querystring includes `?states=UnderReview`.
5. Sort by `Submitted` ascending.
6. **Verify**: rows reorder; URL gains `&sort=submitted-asc`.
7. Navigate to page 2 of results; confirm pagination is server-side (URL gains `&page=2`).

### 3.2 Deep-linkable querystring

1. Copy the URL after applying filters in §3.1.
2. Open a fresh browser session (incognito / different profile); paste the URL.
3. Sign in as Admin if prompted.
4. **Verify**: the same filtered view renders.

### 3.3 CSV export — golden path

1. Apply State = `Resolved` and a date range that produces a non-trivial set of rows.
2. Click `Download CSV` in `_ActionBar`.
3. **Verify**: the browser downloads a `.csv` file. `Content-Type: text/csv`, `Content-Disposition: attachment`. Open the file in a spreadsheet.
4. **Verify**: the header row matches the column map in `contracts/README.md` (Applications CSV); the rows match the on-screen filtered set across all pages.
5. **Verify**: applications with mixed-currency totals appear as one row per (application, currency) — confirm by inspecting an application that has both COP and USD quotations.

### 3.4 CSV export — refused on overflow

1. Temporarily lower `AdminReports:CsvRowLimit` to a small number (e.g., `5`) in user-secrets.
2. Restart the host.
3. Open `/Admin/Reports/Applications` with no filter; click `Download CSV`.
4. **Verify**: the response is HTTP 400 with a JSON body `{ "error": "CsvRowBoundExceeded", "limit": 5, "actualCount": >5, "hint": "Narrow your filter and try again." }`. The UI surfaces a named-rule error.
5. Restore `CsvRowLimit` to `50000`; restart.

### 3.5 Malformed querystring

1. Open `/Admin/Reports/Applications?from=not-a-date`.
2. **Verify**: the page renders with the safe-default filter (no date range applied) and shows a single named-rule error in the filter bar. Response is HTTP 200, NOT 500.

### 3.6 Empty filter result

1. Apply a filter combination that yields zero rows (e.g., a date range in the future).
2. **Verify**: the `_EmptyState` partial renders with copy explaining no rows match. The filter bar remains interactive.

---

## US4 — Applicants report

### 4.1 Applicant lifecycle aggregates

1. Open `/Admin/Reports/Applicants`.
2. Locate an applicant who has applications across multiple terminal states.
3. **Verify**: the row shows non-zero values in `Total Apps`, `Resolved`, `Response Finalized`, `Agreement Executed`. Approval rate renders as a percentage (or em-dash if no items).
4. **Verify**: `Total Approved` and `Total Executed` are presented as per-currency stacks (COP / USD) — consistent with the dashboard's Financial tiles.

### 4.2 Search and filter

1. Search for a known applicant by partial last name; submit.
2. **Verify**: matching applicants appear; URL includes `?search=...`.
3. Apply `Has executed agreement = yes`.
4. **Verify**: only applicants with at least one `AgreementExecuted` application remain.

### 4.3 CSV — per-currency expansion

1. Click `Download CSV`.
2. **Verify**: an applicant with both COP and USD applications appears as two rows in the CSV (same identity columns, different currency-and-amount columns).

---

## US5 — Funded Items report

### 5.1 Scope correctness

1. Open `/Admin/Reports/FundedItems`.
2. **Verify**: every row corresponds to an item that is `Approved` AND has a non-null selected supplier AND its parent application is in `ResponseFinalized` or `AgreementExecuted`. Items rejected in the applicant response do NOT appear.
3. Default sort is approved-at descending.

### 5.2 Filter combinations

1. Filter by category = "Computing Equipment" and supplier = (any seeded supplier with computing items).
2. **Verify**: only matching items render.
3. Toggle `Executed only`.
4. **Verify**: only rows whose parent application is in `AgreementExecuted` remain.

### 5.3 CSV columns

1. Click `Download CSV`.
2. **Verify**: header row matches the Funded Items CSV column map. Each row carries the supplier's legal id (where present) and the parent application's submitted-at timestamp.

### 5.4 Supplier rename behavior

1. (Optional) rename a supplier in `/Admin/...` (or directly in the DB for dev convenience).
2. Reload the Funded Items report.
3. **Verify**: the row shows the supplier's CURRENT name. No historical snapshot.

---

## US6 — Aging Applications report

### 6.1 Threshold filter

1. Open `/Admin/Reports/Aging`.
2. **Verify**: every row's "Days in current state" value is ≥ 14 (the default threshold). Default sort is days-in-current-state descending.
3. Change threshold to `60`.
4. **Verify**: rows with `14 ≤ days < 60` disappear. URL gains `?threshold=60`.

### 6.2 Out-of-range threshold

1. Manually edit the URL to `?threshold=400`.
2. **Verify**: the page surfaces a single named-rule error and does NOT partially render. Response is HTTP 200.

### 6.3 Draft inclusion

1. Confirm the default state filter excludes `Draft` (no `Draft` rows visible).
2. Toggle the state filter to include `Draft`; submit.
3. **Verify**: any seeded stale `Draft` applications now appear.

### 6.4 Last-actor and days-in-state computation

1. Identify an application that has had multiple transitions (e.g., went through `SendBack` once).
2. **Verify**: `Days in current state` equals days since the most-recent entry into the current state (NOT since application creation).
3. **Verify**: `Last Actor` matches the user from the most-recent `VersionHistory` entry for the current state's triggering action.

### 6.5 `UnderReview` fallback

1. Identify an application currently in `UnderReview`.
2. **Verify**: `Days in current state` falls back to a sensible value (computed from the most recent `Submitted` `VersionHistory` entry, or `Application.UpdatedAt` if no entries exist). `Last Actor` may render as em-dash if no actor entry resolves.

---

## US7 — Tabler shell + reusable partials consumption

### 7.1 Visual consistency

1. Walk every report-area surface (`/Admin/Reports`, `/Admin/Reports/Applications`, `Applicants`, `FundedItems`, `Aging`).
2. **Verify**: every page uses the Tabler shell — same sidebar, same top bar, same fonts, same spacing as `/Admin/Users` (spec 009).
3. **Verify**: detail-report tables use the existing `_DataTable` partial. Filter bars use `_FormSection`. Empty states use `_EmptyState`. The `Download CSV` button lives in `_ActionBar`. State badges use `_StatusPill`.

### 7.2 No bespoke styles

1. Run a regex sweep:
   ```bash
   grep -rE 'style="' src/FundingPlatform.Web/Views/Admin/Reports/
   ```
2. **Verify**: zero matches.
3. Run:
   ```bash
   grep -rE '<span class="badge[^"]*"' src/FundingPlatform.Web/Views/Admin/Reports/
   ```
4. **Verify**: zero matches outside `_StatusPill` (consistency with spec 008's view-tree invariants).

### 7.3 New `_KpiTile` partial — generic

1. Open `Views/Shared/Components/_KpiTile.cshtml`.
2. **Verify**: it takes content via parameters (label, value, optional per-currency stack). It contains no admin-area-specific markup. It could be consumed by a future reviewer-side dashboard without modification.

### 7.4 Sidebar visibility

1. Sign in as `applicant@demo.com`; view the sidebar.
2. **Verify**: `Reports` entry is NOT visible.
3. Repeat for `reviewer@demo.com`; confirm the same.
4. Sign in as `admin@demo.com`; confirm `Reports` IS visible.

---

## Funding Agreement PDF — visual comparison fixture

If you have access to a Funding Agreement PDF generated BEFORE this spec deployed (e.g., from a previous build of `main`), capture it as `specs/010-admin-reports/quickstart-fixtures/funding-agreement-before-currency.pdf`. Then generate the same agreement on the same data after this spec lands and capture as `funding-agreement-after-currency.pdf`. Open both side-by-side in a PDF viewer and confirm:

- Every amount on the items table has gained a 3-character currency prefix.
- Column widths, line breaks, page breaks, and the signature block positions are unchanged.
- No new visual artifacts (overlapping text, truncated columns, layout shifts).

If the visual comparison reveals a regression, halt US1 implementation and revisit `_FundingAgreementItemsTable.cshtml` until parity is restored. The `FundingAgreementPdfAssertions.AssertEachAmountHasCurrencyCode(byte[] pdf)` helper provides the automated complement to this manual check.

---

## Done criteria

This quickstart is complete when:

- [ ] All US1 §1.1–§1.6 checks pass.
- [ ] All US2 §2.1–§2.6 checks pass.
- [ ] All US3 §3.1–§3.6 checks pass.
- [ ] All US4 §4.1–§4.3 checks pass.
- [ ] All US5 §5.1–§5.4 checks pass.
- [ ] All US6 §6.1–§6.5 checks pass.
- [ ] All US7 §7.1–§7.4 checks pass.
- [ ] The visual-comparison fixture pair (if captured) shows no regression.

A green E2E suite is a stronger gate; this quickstart documents the manual checks an Admin or operator can run after a deploy without test-runner access.
