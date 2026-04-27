# Review Brief: Admin Reports Module

**Spec:** specs/010-admin-reports/spec.md
**Generated:** 2026-04-26

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Replaces the spec 009 stub `/Admin/Reports` page (which renders "Reports module coming soon") with a real reporting surface for Admins. Ships a mixed quick-glance dashboard composed of three KPI rows (pipeline, financial, applicant), a horizontal sub-tab strip linking to four detail reports (Applications, Applicants, Funded Items, Aging Applications), and CSV export on every detail report. To make money-denominated KPIs honest, the spec also lands a first-class per-quotation `Currency` attribute (a 3-character code) with a `DefaultCurrency` configuration value, a dacpac post-deploy backfill, and an updated Funding Agreement PDF (spec 005) that renders the currency code beside every amount. Aggregations group by currency; no FX, no conversion, no rollup. The Aging Applications report closes a long-standing thread (brainstorm #04, "operational visibility for stuck applications").

## Scope Boundaries

- **In scope:** dashboard at `/Admin/Reports` with three KPI rows + global date-range picker; four detail reports (Applications / Applicants / Funded Items / Aging) with server-side filter + sort + pagination, deep-linkable querystring filters, and CSV export of the currently-filtered dataset; per-quotation `Currency` attribute; `DefaultCurrency` in `SystemConfiguration`; dacpac backfill of legacy quotations; spec 005 Funding Agreement PDF updated to render currency codes; consumption of spec 008's Tabler shell + reusable partials; one new generic `_KpiTile` partial.
- **Out of scope:** FX / multi-currency conversion; admin-action audit log (still deferred from spec 009); report-access audit log; sub-tiers within the Admin role (Auditor, Super-admin); Excel / PDF / API exports; charts requiring a charting library; programs/calls / geography / multi-funder / distinct disbursement entity; localization (deferred to future spec 011); saved presets / scheduled deliveries / email reports / threshold alerts; ISO 4217 enforcement of currency codes; historical snapshotting of supplier or applicant attributes.
- **Why these boundaries:** the seed described 8+ sections of comprehensive reporting; this spec scopes a slim MVP that fills the spec 009 stub with operationally-valuable content while bounding the cross-cutting currency change to the smallest surface that lets every monetary KPI be honest end-to-end.

## Critical Decisions

### Slim MVP scope (5 surfaces)
- **Choice:** Dashboard + 4 detail reports + CSV export + currency rollout. Future specs (011+) extend with more reports, advanced metrics, and audit-log content.
- **Trade-off:** Defers most of the seed's report catalog. Trades breadth-on-day-one for shippability and a clean foundation that future reports can plug into.
- **Feedback:** Is the four-report bundle (Applications, Applicants, Funded Items, Aging) the right v1 cut, or should one of them swap for Activity / Status-Transitions or Appeals reports?

### Per-quotation Currency as a first-class attribute
- **Choice:** New required `Currency` column on `Quotation` (3-character code), prefilled from `DefaultCurrency`, backfilled via dacpac. This is the one cross-cutting change in an otherwise read-only spec.
- **Trade-off:** Adds a small footprint to spec 001 (form) and spec 005 (PDF). Costs more than locale-only formatting (rejected option) but less than a per-application currency or full FX. Closes an open thread from spec 005 ("funder identity / multi-funder scenarios") narrowly.
- **Feedback:** Is "narrow rollout" (Approach A in the brainstorm) the right boundary, or should the spec also touch program-level / applicant-level currency selection?

### Group-by-currency aggregation, no FX
- **Choice:** Every monetary KPI is a per-currency stack (e.g., a tile shows two short rows, `COP 1.234.567` and `USD 12.500`). CSVs emit one row per (entity, currency) pair so per-currency totals never collapse.
- **Trade-off:** Honest mathematics; visually denser than a single-currency dashboard. Avoids the FX rabbit hole entirely.
- **Feedback:** Are reviewers comfortable with the visual-density cost on the dashboard, or do they prefer a default-currency headline with non-default details collapsed behind a hover?

### Single Admin tier (no sub-roles)
- **Choice:** Every Admin sees every report. Spec 009's single-Admin-tier contract is preserved.
- **Trade-off:** Defers the seed's "Auditor" / "Super-admin" idea. Future read-only Auditor sub-role can land without breaking spec 010.
- **Feedback:** Is the absence of a read-only Auditor role acceptable for v1?

### No new audit logging
- **Choice:** Spec 010 introduces no audit-log entity or write path. Open thread from spec 009 ("future audit log of admin actions") and the seed's "audit log of report access" both remain deferred to a dedicated future compliance/audit spec.
- **Trade-off:** Compliance-light v1; spec stays slim. Future spec consolidates both audit needs.
- **Feedback:** If external compliance pressure surfaces between 010 and 011, the audit spec needs to land before reports ship — flag if this is a near-term risk.

### CSV-only export
- **Choice:** "Download CSV" is the only export format. Excel, PDF, and API exports are deferred.
- **Trade-off:** Universally importable; no new dependencies; expect "I want Excel" as the most likely follow-up request.
- **Feedback:** Is CSV-only acceptable for v1, or does any stakeholder need Excel/PDF before this ships?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Currency rollout in a "reports" spec
- **Decision:** Spec 010 expands beyond reports content to land a cross-cutting schema change on `Quotation` and a render change on the spec 005 Funding Agreement PDF.
- **Why this might be controversial:** Reports specs typically read existing data; making 010 a hybrid "currency + reports" spec couples two changes that some reviewers may prefer split (e.g., `010A = currency`, `010B = reports`).
- **Alternative view:** Without currency on `Quotation`, every monetary KPI in the reports either lies or is omitted. The two changes are tightly coupled in user value, so coupling them in one spec keeps the MVP coherent.
- **Seeking input on:** would reviewers prefer the split, or is the bundled spec acceptable?

### Aggregation density on the dashboard
- **Decision:** Each financial tile presents per-currency stacks (e.g., two stacked rows for `COP` and `USD`).
- **Why this might be controversial:** A multi-currency platform with three or four currencies could leave each financial tile uncomfortably tall; reviewers may push for a default-currency headline with hover-to-expand.
- **Alternative view:** Honesty over compactness; mixing currencies in one number is a known anti-pattern in grant systems.
- **Seeking input on:** is the visual cost acceptable to reviewers, or is a hover-collapse preferred for v1?

### Aging Applications: default-excludes-Draft
- **Decision:** The Aging report defaults to "all non-terminal except `Draft`"; an Admin can opt Drafts in.
- **Why this might be controversial:** A stale Draft from an Applicant who never came back is a real ops signal; defaulting to exclude may bury it.
- **Alternative view:** Drafts are private to the applicant; including them by default surfaces non-actionable rows.
- **Seeking input on:** is the default-exclude the right call, or should Drafts be included by default?

### CSV "no silent truncation" upper bound
- **Decision:** When a filtered dataset exceeds the planning-pinned upper bound (e.g., 50,000 rows), the export is refused with a named-rule error rather than partially exported.
- **Why this might be controversial:** Operators expecting "the export always works" may find a refused export annoying; some reporting tools half-export with a warning instead.
- **Alternative view:** A half-exported CSV silently lying about the dataset is a worse failure mode for an audit-grade report than a clear refusal.
- **Seeking input on:** is refuse-on-overflow the right choice for a grants platform?

## Naming Decisions

| Item | Name | Context |
|---|---|---|
| New attribute on `Quotation` | `Currency` | 3-character string, ISO 4217 by convention |
| New `SystemConfiguration` setting | `DefaultCurrency` | Drives prefill + backfill |
| Dashboard route | `/Admin/Reports` | Inherited from spec 009 |
| Detail report routes | `/Admin/Reports/{Applications,Applicants,FundedItems,Aging}` | New |
| New Razor partial | `_KpiTile` | Generic tile with optional per-currency stack |
| Sub-tab strip labels | `Applications`, `Applicants`, `Funded Items`, `Aging` | Visible at top of every report-area surface |
| CSV header on per-currency split rows | `Currency` column | Explicit, not implicit |
| Aging threshold default | `14 days` | Range `1–365` |

## Open Questions

These are planning-phase pins (not spec defects), recorded so the plan resolves them before tasks are generated:

- [ ] Pin the page-size convention reused from the review queue (open thread from brainstorm #02).
- [ ] Pin the CSV upper-bound numeric value (cited as `e.g., 50,000`).
- [ ] Pin the `DefaultCurrency` configuration key shape and per-environment conventions (mirrors spec 009's `ADMIN_DEFAULT_PASSWORD` decision).
- [ ] Verify the spec 005 Funding Agreement PDF absorbs the one-token currency-code change without unintended layout shifts (manual visual or PDF-snapshot regression).
- [ ] Verify `VersionHistory` carries timestamps and actor ids sufficient for "approved-at" (US5) and "last actor" / "days in current state" (US6); document the em-dash fallback path for any missing field.
- [ ] Confirm dacpac deployment order: column add → backfill → tighten to NOT NULL (or column add as nullable then tighten in a second pass).

## Risk Areas

| Risk | Impact | Mitigation |
|---|---|---|
| Spec 005 PDF layout regression from currency-code render change | Medium — visible in every newly-generated funding agreement | PDF-snapshot regression check or manual visual comparison in US1's E2E coverage |
| `VersionHistory` lacks the actor/timestamp fields the Aging and Funded Items reports assume | Medium — degrades two columns on those reports to em-dashes | Verify during planning (FR-005, FR-006 of the report stories implicitly tolerate this) |
| Per-currency stack visually overflows tiles when ≥ 3 currencies are present | Low — pre-production has 1-2 currencies seeded | Tile design must support vertical scrolling within the tile or a "+N more" collapse; left to planning |
| `DefaultCurrency` startup-fail-fast (FR-007) breaks E2E test bring-up if the test fixture forgets to set it | Low — caught immediately in CI | Test fixture must set `DefaultCurrency` (e.g., `COP`) explicitly; document in the plan |
| dacpac backfill ordering: column-add must precede backfill must precede NOT NULL | Medium — one wrong order leaves a corrupted upgrade | Constitution principle IV mandates dacpac discipline; planning must specify the deployment-step order |
| Reports surface arrives before the audit-log spec; external compliance pressure forces a retroactive log | Low for now (pre-production) | Open thread from spec 009 stays warm; audit log spec is the natural next compliance feature |

---
*Share with reviewers before implementation.*
