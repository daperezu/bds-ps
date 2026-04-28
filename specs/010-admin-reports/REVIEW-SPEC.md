# Spec Review: Admin Reports Module

**Spec:** specs/010-admin-reports/spec.md
**Date:** 2026-04-26
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** Specification is complete, internally consistent, implementable, and testable on first review. No critical issues, no ambiguity blockers. Two planning-phase pins surfaced for downstream attention.

## Completeness: 5/5

### Structure
- ✓ All required sections present (User Scenarios, Requirements, Success Criteria, Edge Cases, Assumptions, Out of Scope, Key Entities)
- ✓ All recommended sections included
- ✓ No placeholder text, no TBD markers, no [NEEDS CLARIFICATION] markers

### Coverage
- ✓ All seven user stories carry: priority, narrative, "Why this priority", Independent Test, and ≥ 4 acceptance scenarios
- ✓ 33 functional requirements grouped by concern (Currency, Routing & access, Dashboard, Detail reports, Per-report scoping, Tabler shell, Scope & non-functional)
- ✓ 9 measurable success criteria
- ✓ 11 edge cases enumerated, including legacy-data fallbacks (`VersionHistory` gaps), pre-existing PDFs, malformed querystrings, missing config, mixed-currency aggregation
- ✓ 9 assumptions captured, 11 out-of-scope items enumerated

**Issues:** None.

## Clarity: 5/5

### Language Quality
- ✓ Functional requirements use MUST / MUST NOT / MAY with intent; no `should`, no `probably`, no `etc.`
- ✓ Numeric thresholds bound (`14 days`, range `1–365`, `3 characters`)
- ✓ Specific routes named (`/Admin/Reports/{Applications|Applicants|FundedItems|Aging}`)
- ✓ Specific entity attributes named (`ReviewStatus = Approved`, non-null `SelectedSupplierId`, parent in `ResponseFinalized | AgreementExecuted`)
- ✓ "Per-currency stack" defined consistently across dashboard, reports, and CSV exports

### Ambiguities Found
**One non-issue, surfaced for transparency:**

1. `"fail fast"` (FR-007 / US1 acceptance scenario 5)
   - Issue: `fast` is normally a red-flag term.
   - Verdict: **Not ambiguous here.** The phrase is the well-known idiom for "abort startup immediately on misconfiguration" rather than a vague performance claim. Context (refuse-to-start, fatal error, do not silently substitute) leaves no interpretation room.

## Implementability: 5/5

### Plan Generation
- ✓ Can generate implementation plan: schema change scope is one column on `Quotation` plus one row in `SystemConfiguration`; UI changes are one form field, one PDF token, one new partial (`_KpiTile`), four new MVC views.
- ✓ Dependencies identified: spec 008 (Tabler shell + partials), spec 009 (Admin role gate, sidebar pattern, sub-tab pattern from spec 007), spec 005 (Funding Agreement PDF), spec 001 (quotation form).
- ✓ Realistic constraints: no FX, no charts library, no audit log, no localization — all explicit out-of-scope.
- ✓ Manageable scope: 7 user stories, 33 FRs — comparable to spec 009 (29 FRs, 6 user stories).

### Cross-Cutting Concerns
The spec is more cross-cutting than typical (touches existing spec 001 form and existing spec 005 PDF). Both touchpoints are bounded:
- Spec 001 form: one new field, prefilled, validated as 3 chars.
- Spec 005 PDF: one currency-code token rendered beside each amount; no layout reflow.

These are appropriate places for planning-phase risk-tracking but do not constitute spec ambiguity.

**Issues:** None blocking.

## Testability: 5/5

### Verification
- ✓ Every success criterion is a concrete, observable assertion (e.g., SC-005: "no row has a null `Currency` value after upgrade", SC-008: "zero inline `style=` attributes" — both directly testable).
- ✓ Every user story carries an Independent Test paragraph that names the seed data and the assertion.
- ✓ Acceptance scenarios use Given/When/Then consistently; each maps to one or more Playwright E2E tests per FR-033.
- ✓ The "system refuses to start with missing `DefaultCurrency`" scenario (US1 #5, SC implicitly via FR-007) is testable with a startup probe.
- ✓ The CSV-export upper-bound refusal (FR-021) is testable by seeding > N rows.

**Issues:** None.

## Constitution Alignment

**Constitution v1.0.0 — all 6 principles aligned:**

- ✓ **I. Clean Architecture** — spec is pure WHAT; no architectural prescriptions for layering. The implementation will follow Domain → Application → Infrastructure → Web rules per existing pattern; spec does not violate.
- ✓ **II. Rich Domain Model** — Currency is added as an attribute on the existing `Quotation` aggregate. No anemic-model violation; validation lives on the form and the entity.
- ✓ **III. E2E NON-NEGOTIABLE** — FR-033 explicitly requires Playwright coverage of the golden path and at least one error/edge scenario per US.
- ✓ **IV. Schema-First Database Management** — FR-001 and FR-003 explicitly require the dacpac SQL Server Database Project for the `Currency` column and the post-deployment backfill; FR-001 explicitly forbids EF migrations.
- ✓ **V. Specification-Driven Development** — this spec is the artifact.
- ✓ **VI. Simplicity / YAGNI** — extensive Out-of-Scope section (multi-currency conversion, audit log, sub-roles, Excel/PDF/API export, charts library, programs/calls/geography, localization, saved presets, scheduled deliveries, ISO 4217 validation, historical snapshotting). Currency rollout is bounded to the narrowest cross-cutting surface that lets reports be honest end-to-end.

**Violations:** None.

## Recommendations

### Critical (Must Fix Before Implementation)
None.

### Important (Should Fix)
None.

### Optional / Planning-Phase Pins (Forward to Plan)
- [ ] **Pin the page-size convention** (open thread from brainstorm #02): the spec defers this to planning; the plan must either reuse the existing review-queue page-size constant or document a deliberate divergence. FR-017 holds the constraint "match the existing review-queue page-size setting".
- [ ] **Pin the CSV upper-bound** (cited as `e.g., 50,000`): the plan must choose a concrete value supported by an in-process row-count probe; FR-021 holds the constraint "no silent truncation".
- [ ] **Pin the `DefaultCurrency` configuration key shape** (mirrors spec 009's `ADMIN_DEFAULT_PASSWORD` decision): the plan must specify the precise key path and per-environment conventions (Aspire / user-secrets / env var) before US1 can be implemented.
- [ ] **Verify Funding Agreement PDF visual integrity** during planning: the spec assumes the spec 005 template absorbs a one-token currency-code addition without unintended layout shifts; the plan should include a PDF-snapshot regression check (or at minimum a manual visual comparison) as part of US1's E2E coverage.
- [ ] **Verify VersionHistory column adequacy** during planning: assumptions state `VersionHistory` carries timestamps and actor user ids sufficient for "approved-at" (US5) and "last actor" / "days in current state" (US6). The plan should confirm by inspecting the entity and document the fallback path (em-dash columns) if any field is absent.
- [ ] **Verify Quotation backfill ordering**: the dacpac post-deployment script (FR-003) MUST run after the column add (FR-001); the plan should confirm the SQL Server Database Project's deployment order naturally enforces this and that the `NOT NULL` constraint is added in a second pass after backfill (or the column is added nullable and tightened later).

These are routine planning-phase pins, not spec defects. The spec is implementable as written; these items make the implementation cheaper and more predictable.

## Conclusion

The spec is sound on first iteration. It is complete, unambiguous, implementable, and testable. The cross-cutting Currency rollout is the spec's only non-trivial scope-shaping decision and it is bounded with explicit narrow rollout (US1) and a strong out-of-scope list. The Aging Applications report closes a long-standing brainstorm thread (#04), and the spec preserves both the spec 009 single-Admin-tier contract (FR-030) and the spec 008 view-tree invariants (FR-028, SC-008).

**Ready for implementation:** Yes — after planning-phase pins above are settled in `plan.md`.

**Next steps:**
1. Optional: invoke `/speckit-clarify` to surface any underspecified areas (none expected based on this review).
2. Generate `review_brief.md` for stakeholder review.
3. Proceed to `/speckit-plan` when ready; the plan should resolve the six planning-phase pins above before tasks are generated.
