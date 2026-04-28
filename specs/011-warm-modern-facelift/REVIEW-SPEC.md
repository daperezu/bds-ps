# Spec Review: Warm-Modern Facelift

**Spec:** specs/011-warm-modern-facelift/spec.md
**Date:** 2026-04-27
**Reviewer:** Claude (speckit-spex-gates-review-spec)

## Overall Assessment

**Status:** SOUND

**Summary:** The spec is well-structured, internally consistent, testable, and ready for `/speckit-plan`. Seven user stories, 75 functional requirements, 21 success criteria, with clear deferral framing for items that belong in planning. A handful of minor tightenings are noted under Recommendations (Important / Optional) — none block implementation.

## Completeness: 5/5

### Structure
- All required sections present (Overview, User Scenarios & Testing, Requirements, Success Criteria, Assumptions).
- Recommended sections included via embedded structure: edge cases (in User Scenarios & Testing), out-of-scope guardrails (FR-067…FR-071 — explicitly named "Out-of-scope guardrails"), key entities (in Requirements), assumptions.
- No placeholder text, no TBD, no `[NEEDS CLARIFICATION]` markers.

### Coverage
- All 7 user stories carry priority, "why this priority", an Independent Test description, and 5–7 acceptance scenarios in Given-When-Then form.
- 75 functional requirements grouped by US, each is a single MUST/MUST NOT/MAY statement.
- Edge cases enumerated (9 cases, including PDF carve-out drift, role demotion mid-session, and the rare "both appeal AND sent-back loop" path).
- Cross-cutting concerns covered (FR-072..FR-075): brand sign-off, performance baseline capture, asset budget, WCAG AA contrast.

**Issues:** none.

## Clarity: 4.5/5

### Language Quality
- Uses MUST consistently for normative requirements, MAY for permissions, MUST NOT for prohibitions.
- Quantitative thresholds are specific where they matter: motion durations (50/150/250/400/700 ms), asset budget (≤ 400 KB gz), illustration size (≤ 8 KB gz), confetti library (≤ 5 KB gz), 60-frame ticker cap, +10% LCP/TBT regression budget.
- Voice-guide rules are concrete (banned constructs enumerated by example: ALL CAPS, exclamation marks, "submit" CTAs, passive voice in microcopy).

### Minor Ambiguities
1. **FR-029** ("the latest events") lacks a numeric cap, while the parallel **FR-055** caps the reviewer activity feed at "max 5 visible with show-more". Suggest adding a cap to FR-029 (e.g., "max 10 visible") for symmetry and testability.
2. **FR-042** uses the phrase "When the data refreshes mid-page". Since real-time push is excluded by FR-068, this only ever means "on the next render after a state change". Tightening to that phrasing would remove the implication that mid-page refresh is a thing.
3. **FR-039** says "scroll to (or highlight) the matching entry". The OR allows two distinct UX behaviors. If both are acceptable, fine; if only one is acceptable, pin it.
4. **FR-070** says "tokens MUST be designed to be theme-able later" without a verifiable check. Suggest adding a planning-time review item: "verified by review that token names are semantic (e.g., `--color-bg-page`), not value-bound (e.g., `--color-white`)."

None of these block planning.

## Implementability: 5/5

### Plan Generation
- The seven user stories are independently testable and deliverable, satisfying Constitution Principle V.
- US5 (Brand identity, design tokens & motion system) is correctly flagged as foundational — it is a hard prerequisite for US1, US2, US3, US4, US6, US7.
- Dependencies on prior specs are correctly cited (008 partials, 010 `VersionHistory`, 010 `AgingThresholdDays`, 006 signing flow, 002/004 Appeal/SendBack aggregates).
- Scope is bounded explicitly via FR-067..FR-071 (no schema changes, no real-time push, no reviewer ops, no dark mode, no marketing/sound/social/streaks).
- The schema-unchanged constraint (FR-067, SC-018) is sound — wow moments are projections over existing data; the escape hatch via `speckit-spex-evolve` is the right protocol.
- Asset budget is tight but realistic: subset Fraunces + Inter + JetBrains Mono ≈ 150–200 KB; 9 SVGs at < 8 KB ea < 72 KB; canvas-confetti ≈ 5 KB. Total well under 400 KB.
- Constitution alignment:
  - Principle I (Clean Architecture): the `JourneyProjector` (Application layer) keeping the partial presentation-only is correct.
  - Principle II (Rich Domain Model): unaffected — no domain model changes.
  - Principle III (E2E NON-NEGOTIABLE): explicitly honored (FR-021, SC-012, SC-017, every US has acceptance scenarios that map to Playwright tests).
  - Principle IV (Schema-First / dacpac): upheld via FR-067 + SC-018 (zero edits to dacpac).
  - Principle V (SDD): structure follows spec/plan/tasks discipline.
  - Principle VI (Simplicity / YAGNI): explicit out-of-scope guardrails.

**Issues:** none.

## Testability: 4.5/5

### Verification Mechanisms
- 17 of 21 SCs are mechanically verifiable (greps, fixture-based render checks, Playwright tests, axe-playwright, PDF byte-comparison, `git diff --stat`).
- 3 SCs are checklist-based (SC-006 manual sweep checklist; SC-019 voice-guide checklist; SC-020 brand sign-off in PR description) — appropriate for the kind of work they verify.
- 1 SC is soft (SC-021 "users reach an oriented state within 5 seconds — measured via a usability check on a representative fixture"). This is the only weak SC in the set.

### Issues
1. **SC-021** is the weakest. "Usability check" is undefined and "representative fixture" is vague. Two paths forward: (a) define the fixture concretely (e.g., "first-time applicant with one active draft application; usability rubric: 3 of 3 dashboard elements identified within 5 seconds during a moderated walkthrough"), or (b) downgrade SC-021 to "Acceptance via product/designer review of the dashboard hero, KPI strip, and awaiting-action callout". As written, it would be hard to fail.

### Strengths
- The contract-identifier naming convention (CSS custom-property names like `--color-primary`, partial filenames like `_ApplicationJourney`, font names like Fraunces/Inter/JetBrains Mono) is **appropriate** here. These are testable acceptance constraints that downstream code-review and grep gates can verify, not implementation guidance. Same precedent set by spec 008. The checklist's Notes section already explains this convention. No issue.

## Constitution Alignment

The constitution's six core principles are all upheld:

- **Clean Architecture**: spec respects the layer rule — `JourneyProjector` placed in Application layer (FR-043).
- **Rich Domain Model**: unaffected; no behavior changes to domain entities.
- **E2E NON-NEGOTIABLE**: FR-021, SC-012, SC-017, and every user story's acceptance scenarios map directly to Playwright coverage. The Page Object Model overhaul mandate is consistent with the constitution's POM requirement.
- **Schema-First (dacpac)**: explicitly preserved by FR-067 + SC-018; escape hatch via `speckit-spex-evolve`.
- **SDD**: this very spec is the SDD artifact.
- **YAGNI / Simplicity**: explicit out-of-scope guardrails (FR-068..FR-071) prevent scope creep into real-time, dark mode, reviewer ops, or marketing surfaces.

**No technology additions** beyond what is already in the stack — Tabler.io is already vendored (per spec 008); fonts are static assets (no NuGet); the single new dependency (canvas-confetti, ≤ 5 KB gz) is a static asset, not a runtime framework. Adding it does not invoke the constitution's "new technology must be justified in plan.md" gate, but planning should document its inclusion for completeness.

**Carry-over discipline from spec 008** is well preserved:
- PDF carve-outs (FR-020, SC-014) — byte-identical files, byte-identical generated PDFs.
- No CDN (FR-003, all assets vendored under `wwwroot/lib/`).
- No embedded copy in partials that would block future i18n (assumption block + FR-022 + voice-guide methodology).
- Partials remain the only place visual decisions live (continued by FR-007, FR-011).

## Recommendations

### Critical (Must Fix Before Implementation)
None. The spec is implementable as-is.

### Important (Should Fix — quick edits)
- [ ] **Add a numeric cap to FR-029** (recent activity feed on applicant home), e.g., "max 10 visible with show-more", to mirror FR-055.
- [ ] **Tighten FR-042** "data refresh mid-page" to "on the next render after a stage advance", consistent with FR-068's exclusion of real-time push.
- [ ] **Tighten or downgrade SC-021**. Either define the usability fixture concretely or convert to a designer/product-review acceptance.

### Optional (Nice to Have)
- [ ] **FR-039** "scroll to (or highlight)" — pin one or both behaviors as required.
- [ ] **FR-070** add a planning-time verification: "token names MUST be semantic, not value-bound (e.g., `--color-bg-page` not `--color-white`); verified by review during planning."
- [ ] **Document the `canvas-confetti` (or equivalent) addition in plan.md's Constitution Check** even though it falls below the runtime-framework bar — keeps the audit trail clean.

## Conclusion

The spec is sound and ready for `/speckit-plan`. It is large but coherent, with strong separation between contract identifiers (which are appropriate to name in a spec) and implementation details (which are correctly deferred). The carry-over discipline from spec 008 is preserved, and the schema-unchanged constraint with a documented escape hatch is the right pattern.

**Ready for implementation:** Yes (after `/speckit-plan`).

**Next steps:**
1. Proceed to `/speckit-plan` to produce the technical design and constitution check.
2. Plan should: (a) pin the deferred items called out in the spec (display brand name, exact hex values, type-scale ramp, ceremony view-vs-partial, journey-stage resolver placement, ceremony fresh-vs-bookmark mechanism, illustration source); (b) capture the performance baseline per FR-073 as a day-1 task; (c) document `canvas-confetti` (or equivalent) in the Constitution Check.

---

## Iteration Notes

**Iteration 1 (2026-04-27):** All three Important recommendations applied inline:
- FR-029: added "max 10 visible, with a 'show more' link" cap, symmetric with FR-055.
- FR-042: replaced "When the data refreshes mid-page" with "On the next page render after a stage advance" + an inline note tying it to FR-068's exclusion of real-time push.
- SC-021: replaced the soft "usability check on a representative fixture" with a concrete designer/product review acceptance over the four SC-009 fixtures, recorded in the PR description.

The three Optional items (FR-039 OR-clause, FR-070 verification handle, plan-time documentation of `canvas-confetti`) are deferred to `/speckit-plan` as recommended-not-required improvements.

No further iterations required. Spec is SOUND with Important fixes applied.
