# Brainstorm: Admin Reports Module

**Date:** 2026-04-26
**Status:** spec-created
**Spec:** specs/010-admin-reports/

## Problem Framing

Spec 009 shipped a stub `/Admin/Reports` page with the Admin gate and sidebar in place but no content beyond an empty-state card ("Reports module coming soon"). The seed (`brainstorm/admin-reports-seed.md`) framed a comprehensive reporting module — dashboard, eight entity reports, traceability + audit, advanced metrics, multi-format export, OLAP / data warehouse considerations — much wider than any prior single spec in this project (5–30 FRs each). The brainstorming session had to scope down before any deep design could happen.

A second framing problem surfaced quickly: the seed talks heavily about disbursements, multi-currency aggregations, and concentration metrics, but the platform's data model has no Currency attribute. Without one, every monetary KPI either lies (collapses currencies into a single number) or omits money entirely. So the slim-MVP scope had to bundle a small but cross-cutting currency change with the reports content, or the reports could never be honest.

## Approaches Considered

### A: Slim MVP — dashboard + 4 reports + currency rollout (chosen)
- Pros: Fills the spec 009 stub with operationally-valuable content; bounds the cross-cutting currency change to the smallest surface that lets every monetary KPI be honest end-to-end; matches this project's pattern of small, shippable slices; closes brainstorm thread #04 (operational visibility for stuck applications).
- Cons: Spec is more cross-cutting than typical (touches spec 001 form and spec 005 PDF); reasonable reviewers might prefer a `010A = currency / 010B = reports` split.

### B: Reporting platform foundation only
- Spec 010 = the report-rendering infrastructure (filter framework, export, registry, navigation, RBAC) + ONE end-to-end pilot report.
- Pros: Smallest surface; pure infrastructure spec; future reports plug in by name.
- Cons: No immediate operational value beyond the pilot; the dashboard's per-currency KPI value still requires the currency change, so "infra only" doesn't actually buy a slimmer spec.

### C: Full catalog in one spec
- Dashboard + every entity report from the seed + traceability + advanced metrics + multi-format export + access tiers, all in 010.
- Pros: Single review, single MVP.
- Cons: Largest surface ever for this project (50+ FRs); high risk of missing something; long path to first ship; breaks the slim-MVP framing.

### D: Decompose into a sequence of specs
- 010 = infra + dashboard, 011 = entity reports, 012 = financials + funding agreements, 013 = traceability + audit log content.
- Pros: Maximum independence per slice.
- Cons: Defers most of the seed indefinitely; entity reports are not really independent of infra; the slim-MVP from approach A already gives a sequence (just rooted at one starting point).

## Decision

Chose **Approach A** (slim MVP). Spec lives at `specs/010-admin-reports/` with the following key shape decisions encoded:

- **5 surfaces in v1**: dashboard at `/Admin/Reports` + four detail reports (`/Admin/Reports/{Applications,Applicants,FundedItems,Aging}`).
- **Mixed quick-glance dashboard**: three KPI rows (pipeline, financial, applicant) with a single global date-range picker (defaults last 30 days) that scopes only the period-suffixed tiles. Pipeline tiles always reflect current state.
- **Sub-tab strip** (mirrors spec 007's signing-wayfinding pattern) on every report-area surface; sidebar stays a single "Reports" entry.
- **CSV-only export** in v1; Excel/PDF/API deferred. Per-(entity, currency) row expansion in CSVs so per-currency totals never collapse.
- **No silent truncation** on export — refuse with a named-rule error if the filtered dataset exceeds the planning-pinned upper bound (cited as `e.g., 50,000`).
- **Single Admin tier** preserved from spec 009; no Auditor / Super-admin / per-report flags.
- **No new audit logging** — both the deferred admin-action audit log (spec 009) and the seed's audit-of-report-access stay deferred to a dedicated future compliance/audit spec.
- **English copy only**; localization stays deferred to future spec 011.
- **Per-quotation Currency** as a first-class attribute (3-character code, free-form, no ISO 4217 enforcement), stored on `Quotation` via dacpac schema change. New `DefaultCurrency` setting in `SystemConfiguration` drives the prefill + backfill. dacpac post-deploy script backfills legacy rows idempotently. System refuses to start if `DefaultCurrency` is unset.
- **Funding Agreement PDF (spec 005) updated** to render currency codes beside every amount; pre-existing already-generated PDFs are NOT regenerated. Narrow-rollout boundary: no per-program currency, no applicant-side currency selection, no FX, no audit trail of currency changes.
- **Group-by-currency aggregation**: every monetary KPI presents per-currency stacks (e.g., a tile shows two short rows). No FX, no rollup, no default-currency-only headline.
- **Aging report** (US6, P2) closes brainstorm thread #04 (operational visibility for stuck applications). Default threshold 14 days, range 1–365. State filter defaults to "all non-terminal except `Draft`" with opt-in override.
- **Tabler shell + reusable partials** consumed throughout (consistent with spec 008 invariants); one new generic `_KpiTile` partial allowed.
- Spec passed `spex:review-spec` review on first iteration with status SOUND (5/5 across completeness, clarity, implementability, testability; all 6 constitution principles aligned). Six planning-phase pins surfaced for the plan to resolve.

## Open Threads

- Page-size convention reuse from the review queue (open thread from brainstorm #02 — pin during planning if not already settled).
- CSV export upper-bound numeric value (cited as `e.g., 50,000`) — pin during planning.
- `DefaultCurrency` configuration key shape and per-environment conventions (mirrors spec 009's `ADMIN_DEFAULT_PASSWORD` decision) — pin during planning.
- Spec 005 Funding Agreement PDF visual integrity after the one-token currency-code render change — verify with a PDF-snapshot regression or manual visual comparison during planning.
- `VersionHistory` adequacy for "approved-at" (US5) and "last actor" / "days in current state" (US6) — verify during planning; spec already specifies em-dash fallback if any field is absent.
- dacpac deployment-step ordering for the `Currency` column add → backfill → NOT NULL tightening — confirm during planning.
- Whether the per-currency-stack visual density on the dashboard is acceptable across 1–2 currencies, or if a default-currency headline + hover-collapse is preferable when the platform later supports ≥ 3 currencies — revisit if the platform onboards a third currency.
- Whether the bundled `010 = currency + reports` framing is right, or if reviewers would prefer `010A = currency / 010B = reports` — outcome of formal stakeholder review on `review_brief.md`.
- Whether v1's four-report bundle (Applications / Applicants / Funded Items / Aging) is the right cut, or if Activity / Status-Transitions or Appeals should swap in for one of the four — outcome of formal stakeholder review.
- Whether the absence of a read-only Auditor sub-role is acceptable for v1 — flagged in `review_brief.md`; future spec can add Auditor without breaking 010.
- Whether near-term external compliance pressure could force the audit-log spec (still deferred) to land before reports ship — flagged in `review_brief.md`.
- ISO 4217 enforcement of currency codes — deferred; future spec.
- Historical snapshotting of supplier display names / applicant identities on report rows — deferred; reports always render current relational state.
