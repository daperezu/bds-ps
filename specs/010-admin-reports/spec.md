# Feature Specification: Admin Reports Module

**Feature Branch**: `010-admin-reports`
**Created**: 2026-04-26
**Status**: Draft
**Input**: User description: "Admin Reports Module — replace the spec 009 stub /Admin/Reports page with a real reporting surface for Admins. Ships a mixed quick-glance dashboard (pipeline, financial, applicant KPI rows) with a sub-tab strip linking to four detail reports (Applications, Applicants, Funded Items, Aging Applications). To make money-denominated KPIs honest, also lands first-class per-quotation Currency (3-char code) on the Quotation entity, with a DefaultCurrency in SystemConfiguration, a dacpac post-deploy backfill, and the Funding Agreement PDF (spec 005) updated to render currency codes beside amounts. Aggregations group by currency; no FX. Single Admin tier (no sub-roles), no new audit logging, English copy only."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Per-quotation Currency lands platform-wide (Priority: P1)

Spec 010's reports must aggregate monetary amounts to be useful, but the platform today stores quotation prices as plain decimals with no currency context. Without a first-class currency attribute, every dashboard tile and every detail-report total either lies (collapsing different currencies into one number) or omits money entirely. This story lands a required `Currency` attribute on `Quotation` (a 3-character code such as `COP`, `USD`), introduces a `DefaultCurrency` configuration value used to prefill new quotations and to backfill existing rows on first deploy, updates the quotation create/edit form (existing surface from spec 001) with one new field, and updates the Funding Agreement PDF (existing surface from spec 005) to render the currency code beside every amount. After this story alone, the platform is multi-currency capable for all read paths; it does NOT introduce conversion or FX rates — those are explicitly deferred.

**Why this priority**: Every other story in this spec depends on currency being an explicit attribute. Aggregations across mixed currencies must group by currency to be honest, and that requires the data shape to exist first. Ship this before any report.

**Independent Test**: Set `DefaultCurrency = "COP"`. Open the quotation create form; confirm the Currency field is prefilled with `COP` and is editable. Create one quotation as `COP` and a second on the same item as `USD`. Generate the application's Funding Agreement PDF and confirm each amount is rendered with its currency code beside it. Run the dacpac upgrade against a database that has pre-existing quotations with no Currency value; confirm every legacy row carries the configured `DefaultCurrency` after the post-deploy script runs.

**Acceptance Scenarios**:

1. **Given** `DefaultCurrency = "COP"` and an Applicant on the quotation create form, **When** the form loads, **Then** the Currency field is prefilled with `COP` and is editable.
2. **Given** a quotation create submission with a Currency value that is not exactly 3 characters, **When** submitted, **Then** the form rejects with a single named-rule error and persists nothing; this error surfaces together with any other form validation errors.
3. **Given** an application with two items, each carrying a quotation in a different currency, **When** the Funding Agreement PDF is generated, **Then** each amount is rendered with its currency code beside it (e.g., `COP 1.234.567` on one row and `USD 12.500` on another); no implicit conversion is performed.
4. **Given** a deployment that contains pre-existing quotations with no Currency value, **When** the dacpac upgrade runs, **Then** every legacy row is backfilled with the configured `DefaultCurrency`. The backfill MUST be idempotent (re-running causes no further mutations).
5. **Given** a deployment with `DefaultCurrency` unset, **When** the system attempts to start, **Then** startup fails fast with a fatal error explaining the missing configuration; the system does NOT silently substitute a hardcoded value.
6. **Given** a Funding Agreement PDF that was generated BEFORE this feature deployed, **When** an Admin downloads it, **Then** the PDF is served as-is (without currency codes); it MUST NOT be silently regenerated.

---

### User Story 2 — Admin opens the dashboard at /Admin/Reports (Priority: P1)

The dashboard is the high-traffic landing surface for every Admin. Today `/Admin/Reports` renders a "Reports module coming soon" empty state (shipped by spec 009 as a stub). This story replaces that stub with a real dashboard composed of three KPI rows and a horizontal sub-tab strip linking to the four detail reports. The Pipeline row tiles show counts of applications currently in each non-Draft state. The Financial row tiles show period-scoped sums (Approved this period, Executed this period, Pending execution) presented as per-currency stacks. The Applicant row tiles show Active applicants, Repeat applicants, and New applicants this period. A single global date-range picker (defaults to last 30 days) scopes only the period-suffixed tiles; pipeline tiles always reflect current state. After this story alone, an Admin landing on `/Admin/Reports` can answer "where is everything in the process, how much money is moving, and how many people are we serving" without any further interaction.

**Why this priority**: This is the primary unblock for the seed's mandate ("a high-level dashboard for quick insights"). Every other report in this spec is reachable from the sub-tab strip on this surface. Ship this and Admins have a meaningful daily landing page; the other reports add depth on top.

**Independent Test**: Log in as Admin. Land on `/Admin/Reports` on a seeded environment with at least two currencies and applications across multiple states. Confirm three KPI rows render: Pipeline (one tile per non-Draft state showing current counts), Financial (Approved / Executed / Pending, each presented as per-currency stacks), Applicant (Active / Repeat / New). Confirm the date-range picker defaults to last 30 days. Change the picker to last 7 days; confirm only the period-scoped tiles (Financial row + "New this period" applicant tile) recompute; pipeline counts are unchanged. Click each entry on the sub-tab strip and confirm each navigates to its detail report.

**Acceptance Scenarios**:

1. **Given** an Admin landing on `/Admin/Reports`, **When** the page renders, **Then** three KPI rows are visible (Pipeline, Financial, Applicant), the sub-tab strip lists `Applications | Applicants | Funded Items | Aging`, and the global date-range picker defaults to the last 30 days.
2. **Given** the seeded data has applications denominated in two currencies (e.g., COP and USD), **When** the Financial row renders, **Then** each financial tile presents per-currency sub-rows (e.g., a `COP 1.234.567` row and a `USD 12.500` row inside the same tile); no tile collapses two currencies into one number.
3. **Given** an Admin changes the date-range picker, **When** the change is applied, **Then** the Financial row tiles and the "New this period" applicant tile recompute against the new range; Pipeline row tiles do NOT change (they reflect current state, not period).
4. **Given** the database has zero applications, **When** the dashboard loads, **Then** every Pipeline tile renders with `0`, every Financial tile renders an empty per-currency stack with a single em-dash row, every Applicant tile renders `0`, and no error is shown.
5. **Given** a non-Admin user (Reviewer, Applicant, or unauthenticated) requesting `/Admin/Reports`, **When** the route is hit, **Then** the response is 403 Forbidden (or redirects to login if unauthenticated). This inherits the spec 009 gate; this story does not change it.

---

### User Story 3 — Applications report (Priority: P1)

The Applications report is the highest-density operational view in the spec. Most Admin questions ("what's the status of application X?", "show me everything Applicant Y has submitted", "which applications were submitted last week and where are they now?") resolve here. The report renders a server-paginated table of every application in the platform, with columns for the applicant, current state, key transition dates, item count, total approved amount per currency, and presence-of-agreement / presence-of-active-appeal flags. Filters cover state (multi-select), date range, applicant search, has-agreement, and has-active-appeal. The currently filtered dataset is exportable as CSV across all pages, with one row per (application, currency) pair so per-currency totals never collapse. After this story alone, ops can answer almost every common application-state question from a single screen and share the result by URL or CSV.

**Why this priority**: Highest day-to-day operational value; the report most likely to replace ad-hoc database queries.

**Independent Test**: Filter to State = `UnderReview`. Search a known applicant's last name. Confirm only matching rows return. Click "Download CSV" and confirm the file contains exactly the same rows (across all pages) with the per-currency split preserved. Navigate to page 2 of the on-screen results to confirm pagination is server-side. Copy the URL after applying filters; paste it in a fresh browser session and confirm the same filtered view renders.

**Acceptance Scenarios**:

1. **Given** an Admin on `/Admin/Reports/Applications` with no filters, **When** the page renders, **Then** every application in the platform is listed (across multiple pages if necessary), with the columns: App id, Applicant (full name + legal id), State, Created, Submitted, Resolved, Item count, Total approved (per-currency stack), Has agreement, Has active appeal. The default sort is most-recently-updated descending.
2. **Given** an Admin applies State = `UnderReview` and a date range, **When** the filter applies, **Then** only matching applications appear; the URL querystring updates to encode the filter; pasting the URL in a fresh session reproduces the same filtered view.
3. **Given** an Admin downloads CSV, **When** the file is generated, **Then** it contains every row in the currently filtered dataset across all pages (NOT just the current page) and includes one row per (application, currency) pair when an application has totals across multiple currencies.
4. **Given** the filtered dataset exceeds the planning-pinned upper bound, **When** the Admin triggers CSV export, **Then** the export is refused with a named-rule error and a hint to narrow the filter; no partial file is delivered.
5. **Given** a malformed querystring (invalid date format, unrecognized state value), **When** the route is hit, **Then** the page surfaces a single named-rule error and renders with safe defaults; it MUST NOT return HTTP 500.
6. **Given** the empty filter result, **When** the page renders, **Then** the empty-state component appears with copy explaining no rows match the current filters; the filter bar remains interactive.

---

### User Story 4 — Applicants report (Priority: P1)

The Applicants report answers "who are we funding and what's their pattern?" with a per-applicant lifecycle aggregate. Each row covers one applicant: their identity columns (full name, legal id, email), the count of their applications in each terminal state (Resolved / ResponseFinalized / AgreementExecuted), an approval rate computed as approved-items over total-items across all their applications, total approved and total executed amounts (each as a per-currency stack), and the most recent activity timestamp across their applications. Filters cover free-text search, has-executed-agreement, and last-activity date range. CSV export emits one row per (applicant, currency) pair so per-currency totals never collapse. After this story alone, beneficiary-insight reporting is a single screen away.

**Why this priority**: Beneficiary insight is the seed's "reports by applicant/user" minimum requirement and a frequent leadership ask. Ships at the same priority as Applications because the two together cover both axes (per-application and per-applicant) with one trip to the database per page load.

**Independent Test**: Search a known applicant; confirm exactly one row returns. Verify Total approved and Total executed render as per-currency stacks consistent with the dashboard's Financial tiles for the same date range. Apply has-executed-agreement = yes; confirm the result set narrows. Apply a last-activity range; confirm only matching applicants render. Export CSV and verify the per-currency expansion in the file.

**Acceptance Scenarios**:

1. **Given** an Admin on `/Admin/Reports/Applicants` with no filters, **When** the page renders, **Then** every applicant in the platform is listed with: identity columns (name + legal id + email), total apps submitted, columns for apps in each terminal state, approval rate, total approved (per-currency stack), total executed (per-currency stack), last activity. The default sort is total-executed descending, tie-broken on total-approved.
2. **Given** an applicant has applications denominated in two currencies, **When** their row renders, **Then** Total approved and Total executed each present per-currency sub-rows; no row collapses two currencies into one number.
3. **Given** an Admin applies the free-text search, **When** the filter applies, **Then** matching is performed across name, email, and legal id; partial substring matches are accepted.
4. **Given** an Admin downloads CSV, **When** the file is generated, **Then** it includes one row per (applicant, currency) pair when an applicant has totals across multiple currencies. Visible columns are emitted; no per-currency collapse.
5. **Given** an applicant with zero applications, **When** they appear in the listing, **Then** their numeric columns render `0`, their per-currency stacks render an em-dash, and their approval rate renders an em-dash (not "0%").

---

### User Story 5 — Funded Items report (Priority: P1)

The Funded Items report is the line-item view of every approved item across the platform — the best surface for "who got what" auditing and supplier-concentration insight. A row appears when an item has been approved during review (`ReviewStatus = Approved`), has a non-null selected supplier, AND its parent application is in `ResponseFinalized` or `AgreementExecuted` state (i.e., the applicant did not subsequently reject the item in their applicant response). Columns cover the application, applicant, item, category, selected supplier, the item's selected-quotation price and currency, the parent application's state, the approved-at timestamp (sourced from `VersionHistory`), and presence/execution flags for the parent's funding agreement. After this story alone, the seed's "relationships between applicant, provider, products/services, and disbursements" requirement is met for the data the platform already models.

**Why this priority**: This is the most flexible report for ad-hoc audit work — supplier concentration, category distribution, who-got-what-when. P1 to keep the v1 surface coherent.

**Independent Test**: Filter category = X and supplier = Y; confirm only matching items render. Toggle "Executed only"; confirm only rows whose parent application is in `AgreementExecuted` remain. Apply an approved-at date range; confirm only items approved within that range render. Export CSV and confirm the rows match the on-screen filter exactly, including the supplier's legal id and the parent application's submitted-at columns.

**Acceptance Scenarios**:

1. **Given** an Admin on `/Admin/Reports/FundedItems` with no filters, **When** the page renders, **Then** every approved item with a selected supplier in a parent application in `ResponseFinalized` or `AgreementExecuted` is listed, with columns: App id, Applicant, Item product name, Category, Selected supplier, Quotation price, Currency, App state, Approved-at, Has agreement, Executed. The default sort is approved-at descending.
2. **Given** the data has items rejected in the applicant response, **When** the page renders, **Then** those items do NOT appear (their parent applications may still be `ResponseFinalized`, but the rejected item itself is excluded by the report's scope).
3. **Given** an Admin filters by category and supplier, **When** the filter applies, **Then** only items matching both criteria render; the URL querystring encodes the filter combination.
4. **Given** an Admin downloads CSV, **When** the file is generated, **Then** every visible column is included plus the supplier's legal id (when available) and the parent application's submitted-at timestamp.
5. **Given** an item's selected supplier was later disabled or had its display name modified, **When** the row renders, **Then** the supplier's CURRENT display name is shown; no historical snapshot is preserved.

---

### User Story 6 — Aging Applications report (Priority: P2)

The Aging Applications report closes a long-standing thread (open thread #04: "operational visibility for stuck applications with no deadlines — likely future reporting spec"). It surfaces every application currently in a non-terminal state (`Draft`, `Submitted`, `UnderReview`, `Resolved`, `ResponseFinalized`) where the time since the most recent transition into that state exceeds a configurable threshold (default 14 days, range 1–365). Columns cover the applicant, current state, days-in-current-state, the timestamp of the last transition, the actor who triggered that transition (sourced from `VersionHistory`), item count, and total approved (per-currency stack) — useful once the application reaches `Resolved` or beyond. The state filter defaults to "all non-terminal except `Draft`" because Drafts are private to the applicant; an Admin can opt Drafts in for a "stale Draft" sweep.

**Why this priority**: P2 (not P1) because the dashboard's Pipeline row already shows aggregate counts; this report adds the per-row drill-in that lets ops actually act on stuck items. Cheap to ship once the report framework from US3-US5 exists.

**Independent Test**: Seed three applications in `Submitted` state with synthetic transition timestamps of 5 / 20 / 90 days ago. Set threshold = 14. Confirm two applications render. Set threshold = 60. Confirm one application renders. Toggle the state filter to include `Draft`; confirm any seeded stale Drafts now appear. Sort defaults to days-in-current-state descending.

**Acceptance Scenarios**:

1. **Given** the threshold is set to 14 days and an Admin loads the report, **When** the page renders, **Then** every row's "Days in current state" value is ≥ 14, and rows are sorted by days-in-current-state descending.
2. **Given** an application has had multiple state transitions (e.g., `Submitted → UnderReview → Draft → Submitted` after an appeal-grant-reopen), **When** "Days in current state" is computed, **Then** the value is days since the MOST RECENT transition into the current state, NOT since application creation.
3. **Given** the state filter defaults to "all non-terminal except `Draft`", **When** the report renders without an explicit override, **Then** no application in `Draft` state appears, regardless of how long it has been stuck.
4. **Given** an Admin overrides the state filter to include `Draft`, **When** the report renders, **Then** stuck Draft applications appear and contribute to the listing.
5. **Given** an Admin changes the threshold from 14 to 60, **When** the change applies, **Then** rows with 14 ≤ days < 60 disappear; the URL querystring updates so the view is shareable.
6. **Given** the threshold is set outside the allowed range (e.g., 0 or 400), **When** submitted, **Then** the form rejects with a single named-rule error and the report does NOT partially render.

---

### User Story 7 — Reports area consumes Tabler shell + reusable partials (Priority: P3)

Every report-area view (dashboard plus four detail reports) MUST consume the Tabler shell from spec 008 and the established reusable partials. Detail-report tables use `_DataTable`. Filter bars use `_FormSection`. CSV-export buttons live in `_ActionBar`. Empty result sets use `_EmptyState`. State-coded values use `_StatusPill`. The dashboard's KPI tiles MAY introduce a new `_KpiTile` partial provided it follows spec 008's partial conventions, takes content via parameters (label, value, optional per-currency stack), and is reusable outside the reports area. No bespoke styling. No inline `style=` attributes anywhere in the report views, consistent with spec 008's view-tree invariants. The role-aware sidebar entry "Reports" remains visible to Admins only (already shipped by spec 009; this story confirms it survives unchanged).

**Why this priority**: Quality bar, not a delivery dependency. Mirrors spec 009's US6 — every prior story in this spec already assumes Tabler-shell consumption; this story makes the assumption explicit and testable.

**Independent Test**: Walk every report view in a browser and confirm visual consistency with the rest of the platform. Run a regex sweep over `Views/Admin/Reports/**` for inline `style=` attributes and badge markup outside `_StatusPill`; assert zero matches.

**Acceptance Scenarios**:

1. **Given** any report-area view, **When** rendered, **Then** it consumes the Tabler shell and the existing partials (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`) where applicable; no bespoke UI patterns are introduced.
2. **Given** a search of `Views/Admin/Reports/**`, **When** searched for inline `style=` attributes or badge markup outside `_StatusPill`, **Then** zero occurrences are found.
3. **Given** the dashboard introduces a new `_KpiTile` partial, **When** the partial is reviewed, **Then** it is generic, takes its content via parameters (label, value, optional per-currency stack), and is reusable outside the reports area.
4. **Given** a non-Admin user (Reviewer, Applicant, or unauthenticated), **When** they view the sidebar, **Then** the "Reports" entry is NOT visible.

---

### Edge Cases

- An application has zero quotations on any item (a Draft early in its life). Aggregations MUST treat its monetary contribution as zero across all currencies; the application still appears in the Applications report with its other columns populated.
- An applicant has applications in two currencies. Their row in the Applicants report renders two stacked sub-rows under Total approved / Total executed. The CSV export emits one row per (applicant, currency) pair.
- A Funded Items row's selected supplier was later disabled or had its display name changed. The report renders the supplier's CURRENT display name; no historical snapshot is preserved (out of scope for v1).
- The dashboard's date-range picker is set to a future-only window (e.g., next 30 days). Period-scoped tiles render zero values; no error is shown.
- A user attempts to download a CSV that would exceed the planning-pinned upper bound. The export is refused with a named-rule error and a hint to narrow the filter; no partial file is delivered.
- An application has had its state transitioned and reverted (e.g., an `Appeal — Grant — Reopen to Draft` round). The Aging report computes "Days in current state" from the MOST RECENT transition into the current state, NOT from the original entry.
- A pre-existing already-generated Funding Agreement PDF (created BEFORE this spec deploys) has no currency code in its rendered amounts. The platform does NOT regenerate it. The funding agreement remains historically accurate as generated; new generations from the moment of deployment forward render with currency codes.
- An Admin requests a deep-link URL with a malformed querystring (invalid date format, unrecognized state value, threshold outside range). The page MUST surface a single named-rule error and render with safe defaults; it MUST NOT return HTTP 500.
- The `DefaultCurrency` configuration value is missing on first deploy. The system MUST refuse to start (or log a fatal error and stop) rather than silently default to a hardcoded value, because every existing quotation's backfill depends on it.
- Two Admins simultaneously load the same report page with different filters. There is no shared state; each Admin sees their own filter result. (No locking, no collision risk — all report surfaces are read-only.)
- `VersionHistory` is missing entries for some legacy transitions (data is older than the introduction of version-history capture). For such applications, the Aging report's "Last actor" column renders an em-dash; the row still appears with its days-in-current-state computed from the application's UpdatedAt as a best-effort fallback. The Funded Items report's "Approved-at" column renders an em-dash if no transition entry can be located.

## Requirements *(mandatory)*

### Functional Requirements

**Currency (US1)**

- **FR-001**: System MUST add a required `Currency` attribute to the existing `Quotation` entity, storing a 3-character currency code as a string. Schema change MUST be made in the SQL Server Database Project (dacpac), per constitution principle IV. EF migrations MUST NOT be used.
- **FR-002**: System MUST add a `DefaultCurrency` setting in the existing `SystemConfiguration` entity, settable per deployment. The setting's value MUST drive the default selection on quotation forms and the backfill of existing rows.
- **FR-003**: System MUST backfill `Currency` on every pre-existing `Quotation` row with the configured `DefaultCurrency` value via a dacpac post-deployment script. The script MUST be idempotent: re-running causes no further mutations.
- **FR-004**: Quotation create and edit forms (existing surfaces from spec 001) MUST include a Currency field, prefilled with `DefaultCurrency`, editable per quotation, and validated as exactly 3 characters before persistence. Validation errors MUST surface together with any other form validation errors on the same submission (constitution VI consistency).
- **FR-005**: The Funding Agreement PDF (existing surface from spec 005) MUST render every per-item amount and any sub-total with its currency code beside it (e.g., `COP 1.234.567`). Pre-existing already-generated Funding Agreement PDFs MUST NOT be retroactively regenerated by this feature.
- **FR-006**: System MUST NOT perform any currency conversion. Aggregations across mixed currencies MUST be reported per currency and presented as per-currency stacks in every UI surface and CSV column set.
- **FR-007**: System MUST refuse to start (fail fast with a fatal error explaining the missing configuration) when `DefaultCurrency` is unset on first deploy. The system MUST NOT silently substitute a hardcoded value.

**Routing & access (US2-US7)**

- **FR-008**: System MUST replace the spec 009 stub `/Admin/Reports` page with a real dashboard view, gated by the existing `[Authorize(Roles="Admin")]` route gate inherited unchanged from spec 009.
- **FR-009**: System MUST expose four detail-report routes — `/Admin/Reports/Applications`, `/Admin/Reports/Applicants`, `/Admin/Reports/FundedItems`, `/Admin/Reports/Aging` — each gated by the Admin role at the route/handler level. Non-Admin requests MUST receive 403 Forbidden (or redirect to login if unauthenticated).
- **FR-010**: System MUST present a horizontal sub-tab strip on every report-area surface listing the four detail reports, with the active surface visually highlighted. The strip MUST be present on the dashboard and on each detail report.
- **FR-011**: System MUST present role-aware sidebar entries unchanged from spec 009: a single "Reports" entry visible only to Admins, linking to `/Admin/Reports`.

**Dashboard (US2)**

- **FR-012**: Dashboard MUST render three KPI rows: Pipeline (one tile per non-Draft application state showing the current count), Financial (Approved this period, Executed this period, Pending execution — each as a per-currency stack), and Applicant (Active applicants, Repeat applicants, New this period).
- **FR-013**: Dashboard MUST present a single global date-range picker, defaulting to last 30 days. The picker MUST scope only the period-suffixed tiles (Financial row + "New this period" applicant tile). Pipeline tiles MUST always reflect current state regardless of the picker.
- **FR-014**: Every monetary KPI MUST be presented as a per-currency stack inside its tile. No tile may aggregate two currencies into a single number; no default-currency-only rollup is permitted.
- **FR-015**: When the database contains zero applications, every Pipeline tile MUST render `0`, every Financial tile MUST render an empty per-currency stack with a single em-dash row, every Applicant tile MUST render `0`, and no error MUST be shown.
- **FR-016**: Dashboard's "Active applicants" KPI MUST count applicants with at least one application in any non-terminal state. "Repeat applicants" MUST count applicants with two or more submitted applications. "New this period" MUST count applicants whose first application's submission timestamp falls within the current date-range filter.

**Detail reports (US3-US6)**

- **FR-017**: Every detail report MUST be server-paginated, server-sorted, and server-filtered. The page-size convention MUST match the existing review-queue page-size setting (open thread from brainstorm #02 — pin during planning if not already settled). Sorting and filtering MUST never run client-side over a paged window.
- **FR-018**: Every filter combination on every detail report MUST be encoded as a querystring on the URL so any filtered view is deep-linkable and shareable. Pasting a copied URL in a fresh browser session MUST reproduce the same filtered view.
- **FR-019**: Every detail report MUST present a "Download CSV" action that exports the CURRENTLY FILTERED dataset across all pages — not just the visible page.
- **FR-020**: CSV exports MUST emit one row per (entity, currency) pair when an entity has totals across multiple currencies, so that no aggregated amount collapses currencies in the exported file.
- **FR-021**: System MUST refuse a CSV export when the result set exceeds a planning-pinned upper bound (e.g., 50,000 rows) and surface an error to the user with a hint to narrow the filter; partial exports MUST NOT be emitted.
- **FR-022**: Filter-form validation errors (e.g., invalid date range, threshold outside allowed bounds, unrecognized state value) MUST surface together (constitution VI consistency); the report MUST NOT partially render with invalid input. Malformed querystrings MUST surface a named-rule error and render with safe defaults; the system MUST NOT return HTTP 500.
- **FR-023**: Empty result sets MUST render the existing `_EmptyState` partial with copy explaining no rows match the current filters; the filter bar MUST remain interactive so the Admin can adjust filters.

**Per-report scoping (US3-US6)**

- **FR-024**: Applications report (US3) MUST list every application with the columns: App id, Applicant (name + legal id), State, Created, Submitted, Resolved, Item count, Total approved (per-currency stack), Has agreement, Has active appeal. Filters MUST cover state (multi-select), date range, applicant search (name / legal id / app id), has-agreement, has-active-appeal. Default sort MUST be most-recently-updated descending.
- **FR-025**: Applicants report (US4) MUST list every applicant with the columns: identity (name + legal id + email), total apps submitted, count of apps in each terminal state (Resolved / ResponseFinalized / AgreementExecuted), approval rate, total approved (per-currency stack), total executed (per-currency stack), last activity. Filters MUST cover free-text search (name / email / legal id), has-executed-agreement, last-activity date range. Default sort MUST be total-executed descending, tie-broken on total-approved descending.
- **FR-026**: Funded Items report (US5) MUST list every item where `ReviewStatus = Approved` AND a non-null `SelectedSupplierId` AND the parent application is in `ResponseFinalized` or `AgreementExecuted`. Columns MUST include: App id, Applicant, Item product name, Category, Selected supplier, Quotation price, Currency, App state, Approved-at, Has agreement, Executed. Filters MUST cover category (multi-select), supplier (multi-select), app-state (multi-select restricted to in-scope states), approved-at date range, executed-only (boolean). Default sort MUST be approved-at descending.
- **FR-027**: Aging Applications report (US6) MUST list applications currently in any non-terminal state (`Draft`, `Submitted`, `UnderReview`, `Resolved`, `ResponseFinalized`) where time since the most recent transition into the current state exceeds the configured threshold. Threshold default MUST be 14 days, valid range 1–365 inclusive. State filter MUST default to "all non-terminal except `Draft`"; an Admin MUST be able to opt Drafts in. Default sort MUST be days-in-current-state descending.

**Tabler shell (US7)**

- **FR-028**: Every report-area view MUST consume the Tabler shell and the reusable partials established in spec 008 (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`) where applicable. No bespoke UI patterns or inline `style=` attributes MUST be introduced by this feature.
- **FR-029**: Where no existing partial fits (specifically, a KPI tile), a new partial MAY be introduced. Any new partial MUST follow spec 008's partial conventions, take its content via parameters (label, value, optional per-currency stack), and be reusable outside the reports area.

**Scope & non-functional**

- **FR-030**: This feature MUST NOT introduce any new authorization tier within the Admin role. Every report is visible to every user holding the Admin role; the spec 009 single-Admin-tier contract is preserved.
- **FR-031**: This feature MUST NOT introduce any new audit-logging entity, table, or write path. The open thread from brainstorm #09 ("future audit log of admin actions") and the seed's "audit log of report access" both remain deferred to a dedicated future compliance/audit spec.
- **FR-032**: All admin-area copy in this feature MUST be English. Localization remains deferred to future spec 011 (open thread from spec 008).
- **FR-033**: Every user story (US1-US7) MUST be covered by Playwright end-to-end tests covering the golden path and at least one error or edge scenario, per constitution principle III.

### Key Entities

- **Quotation** (existing, extended): The supplier-quotation aggregate referenced by `Item`. This spec adds a required `Currency` attribute (3-character code) and a corresponding form field, backfill script, and PDF-render update. No other Quotation behavior is modified.
- **SystemConfiguration** (existing, extended): The system-wide configuration entity. This spec adds a `DefaultCurrency` setting whose value is consumed at startup (to validate presence), at quotation-form render (to prefill the Currency field), and at backfill time (to populate legacy rows).
- **Application** (existing, read-only): The aggregate consumed by every report. No fields are added or modified by this feature; reports query existing state, dates, items, applicant relationships, version history, and funding-agreement presence/state.
- **Applicant** (existing, read-only): Same as above — read-only consumption for the Applicants report and as a join target on every other report.
- **Item** (existing, read-only): Read-only consumption. The Funded Items report scopes on `ReviewStatus = Approved` plus a non-null `SelectedSupplierId`, joining to the parent Application's state.
- **Supplier** (existing, read-only): Read-only consumption. The Funded Items report displays the supplier's CURRENT name (no historical snapshot).
- **Category** (existing, read-only): Read-only consumption as a filter and column on the Funded Items report.
- **VersionHistory** (existing, read-only): Read-only consumption as the source of "approved-at" on Funded Items and "last actor" / "days in current state" on Aging.
- **FundingAgreement** (existing, extended via PDF render only): The PDF-render template gains currency-code rendering beside every amount. No schema change. No other behavior is modified.
- **Report (conceptual, not persisted)**: A read-only query result composed of the entities above, rendered into a paginated tabular surface or a CSV file. Has no persisted shape; defined by the URL of the request and its querystring filters.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An Admin lands on `/Admin/Reports` and within a single page load sees KPI counts for every non-Draft application state, period-scoped financial KPIs as per-currency stacks, and applicant-count KPIs — no further interaction required to read the dashboard.
- **SC-002**: Every monetary KPI on the dashboard and every monetary column on every detail report is presented per currency. No on-screen number aggregates two currencies into one. Verifiable by seeding two-currency data and asserting the rendered DOM contains separated per-currency rows under each money tile / cell.
- **SC-003**: Each of the four detail reports supports filter, sort, paginate, and CSV-export-of-filtered-rows. Verifiable by an end-to-end test that, for each report, applies a filter, sorts, paginates to page 2, then downloads CSV and asserts the file contains exactly the filtered rows (across pages) with the expected columns.
- **SC-004**: A filtered report view is deep-linkable: copying the URL after applying filters and pasting it in a fresh browser session reproduces the same filtered view. Verifiable by an end-to-end test for at least one report.
- **SC-005**: Pre-existing quotations, after the dacpac post-deployment script runs, all carry the configured `DefaultCurrency`; no row has a null `Currency` value after upgrade. Verifiable by a database assertion in a smoke test against an environment that previously lacked the column.
- **SC-006**: A non-Admin user (Reviewer, Applicant, unauthenticated) receives 403 Forbidden when attempting any `/Admin/Reports/...` route. Unauthenticated users are redirected to login. Verifiable by an end-to-end test for each route.
- **SC-007**: The Aging Applications report surfaces every application in any non-terminal state that is currently more than the configured threshold past its most recent transition, and supports threshold values from 1 to 365 days. Verifiable with seeded transition timestamps.
- **SC-008**: A search of `Views/Admin/Reports/**` yields zero inline `style=` attributes and zero badge markup outside `_StatusPill` (consistency with spec 008 invariants). Verifiable by a regex sweep run as part of CI or as a test step.
- **SC-009**: A regression test asserts that every reviewer-gated route in specs 002, 004, 006, 007 still passes the Admin role authorization (the spec 009 inheritance contract is preserved despite this feature's cross-cutting currency rollout touching the spec 005 PDF surface).

## Assumptions

- The platform remains pre-production with no real applicants or disbursements; introducing a required `Currency` column on `Quotation` and backfilling it via dacpac is acceptable.
- The existing `VersionHistory` entity carries timestamps and actor user ids sufficient to source "approved-at" on the Funded Items report and "last actor" + "days in current state" on the Aging Applications report. Pin during planning if the entity proves to lack either field; if missing, the relevant column degrades to an em-dash with no failure.
- The spec 005 Funding Agreement template can absorb a one-token currency-code addition without triggering a license-revisit on Syncfusion or any other rendering dependency.
- The page-size convention (open thread from brainstorm #02) is settled at planning time; this spec only constrains "server-paginated, same convention as the review queue".
- The CSV export's hard upper bound (e.g., 50,000 rows is a reasonable starting point) is settled at planning time; this spec only constrains "no silent truncation: if the dataset exceeds the bound, the system surfaces an error rather than half-exporting".
- The dataset is small enough that a one-shot SQL aggregation per report load is acceptable performance; no caching, no pre-computed materialized views, no OLAP cube, no data warehouse. The seed's mention of OLAP / data warehouse is explicitly out of scope.
- Currency codes are stored as free-form 3-character strings (uppercase by convention, ISO 4217 in practice). This spec does NOT introduce a controlled list / enum / validation table of allowed codes. Validating against ISO 4217 is a future spec.
- The `DefaultCurrency` configuration key shape and per-environment conventions are settled at planning time, mirroring the prior `ADMIN_DEFAULT_PASSWORD` decision in spec 009.
- The `_KpiTile` partial introduced for the dashboard is reusable across the platform; no other feature in this spec depends on its specific shape, but future specs (e.g., reviewer-side dashboards) may consume it.

## Out of Scope

- Multi-currency conversion, FX rates, cross-currency rollups, or any "display currency" feature.
- Audit log of admin actions (deferred from spec 009 brainstorm; remains deferred to a dedicated future compliance/audit spec).
- Audit log of report access (deferred per this brainstorm session; same compliance/audit spec target).
- Sub-tiers within the Admin role (e.g., Auditor, Super-admin, per-report visibility flags).
- Excel (.xlsx), PDF, or API export formats. CSV is the only export format in v1.
- Any chart that requires a dedicated charting library. The dashboard uses tile-based KPIs only; no time-series line charts in v1.
- Any report sourced from data the platform does not currently model: programs/calls, geography, multi-funder, distinct disbursement entity (executed agreements stand in for disbursements).
- Real-time / streaming dashboards.
- Localization of any copy in this feature (deferred to future spec 011).
- Saved filter presets, scheduled report deliveries, email-out of reports, alerting / threshold-driven notifications.
- Validation of currency codes against an allowed list (e.g., ISO 4217 enforcement).
- Historical snapshotting of supplier display names, applicant identities, or any other relational data referenced by reports. Reports always render the current state of the related entities.
