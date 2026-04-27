# Spec Review: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Spec:** specs/008-tabler-ui-migration/spec.md
**Date:** 2026-04-25
**Reviewer:** Claude (spex:review-spec)
**Iterations:** 2 (initial review → fixes applied → re-review)

## Overall Assessment

**Status (iteration 2):** ✅ SOUND

**Status (iteration 1):** ⚠️ NEEDS WORK — fixes applied inline (see "Iteration 2 Resolution" below)

**Summary:** After applying the fixes from iteration 1, the spec is structurally complete, well-scoped, implementable, testable, and aligned with the project constitution. Ready for planning.

## Completeness: 5/5

### Structure
- ✓ All required sections present (Purpose via Input field, User Scenarios, Requirements, Success Criteria, Edge Cases, Assumptions, Dependencies, Out of Scope, Open Questions)
- ✓ Recommended sections included (Key Entities, prioritized user stories with independent-test descriptions, acceptance scenarios)
- ✓ No placeholder text or TBD markers

### Coverage
- ✓ All functional requirements defined (FR-001 through FR-018, each scoped to a single behavior)
- ✓ Error cases identified (validation, server-emitted feedback, empty states all have explicit FRs and acceptance scenarios)
- ✓ Edge cases covered (sidebar collapse, wide tables, long titles, modal stacking, unauth shell variant, multi-line validation)
- ✓ Success criteria specified (SC-001 through SC-008, each verifiable)

## Clarity: 4/5

### Language Quality
- ✓ Requirements use MUST consistently
- ✓ Status mappings, file paths, and partial names are concrete and grep-verifiable
- ⚠️ Two minor ambiguities (below)

**Ambiguities Found:**

1. **SC-007**: "Funding agreement PDFs generated after the sweep are visually identical to those generated before the sweep, confirming the PDF rendering target was not impacted."
   - And FR-015 (paraphrased): the PDF target files MUST remain "byte-identical."
   - And US3 Independent Test: "byte-identical (or visually identical at the rendering level)."
   - **Issue**: The spec slides between three different identity claims (file byte-identity of source `.cshtml`, byte-identity of generated PDF, visual identity of generated PDF). PDF byte-identity is often unachievable because PDF generators embed timestamps in metadata; the operationally meaningful claim is *source byte-identity for the two `.cshtml` files* AND *visual identity of the generated PDF*.
   - **Suggestion**: Settle on two distinct claims: (a) `Document.cshtml` and `_FundingAgreementLayout.cshtml` source files MUST be byte-identical to their pre-spec contents (verifiable with `git diff`); (b) generated PDFs MUST be visually identical to pre-spec PDFs (verifiable with manual side-by-side or pixel comparison). Pick one wording and use it consistently in FR-015, US3 Independent Test, and SC-007.

2. **FR-009**: "`_ActionBar` MUST classify each action it renders into one of: primary, secondary, destructive, or state-locking, and MUST apply visually distinct treatment to each class."
   - **Issue**: "visually distinct treatment" is true but doesn't tell the implementer what dimensions vary. A reasonable implementer could make all four classes the same color and only vary the icon, which would technically satisfy the FR but fail the spirit.
   - **Suggestion**: Tighten to "MUST apply visually distinct color or icon treatment to each class such that two adjacent actions of different classes are unambiguously distinguishable at a glance," OR enumerate the canonical mapping in this FR (e.g., primary = blue solid, secondary = gray outline, destructive = red, state-locking = amber with lock icon).

## Implementability: 5/5

### Plan Generation
- ✓ Can generate implementation plan (specific files in/out of scope, specific partials enumerated, specific enums named)
- ✓ Dependencies identified (Tabler CSS/JS bundles, Tabler Icons bundle, vendored locally)
- ✓ Constraints realistic (Tabler-as-Bootstrap-superset enables incremental work)
- ✓ Scope manageable (touches 9 controller-folder view trees, all enumerated; pre-prod status reduces blast risk)

## Testability: 3/5

### Verification
- ✓ All success criteria measurable (grep-verifiable, manually verifiable, or visually verifiable)
- ✓ Acceptance scenarios use Given/When/Then format
- ✗ Acceptance pathway conflicts with the constitution's primary quality gate (see below)

**Issues:**

- **E2E test pathway is implicit**. FR-018 and SC-005 rely on manual smoke tests as the acceptance criterion. The constitution (Principle III, marked NON-NEGOTIABLE) requires Playwright E2E tests as the primary quality gate. The spec does not explicitly say whether existing E2E tests for specs 001–007 must continue to pass, nor whether a new E2E test is required for the role-aware sidebar visibility (the only genuinely new behavior introduced). This will surface as a constitution violation during the planning phase if not addressed in the spec.

## Constitution Alignment

- ✓ Principle I (Clean Architecture): feature lives entirely in Web layer + static assets, no cross-layer leakage
- ✓ Principle II (Rich Domain Model): N/A — no domain changes
- ⚠️ Principle III (E2E Testing — NON-NEGOTIABLE): see Critical recommendation below
- ✓ Principle IV (Schema-First DB): N/A — no DB changes
- ✓ Principle V (Specification-Driven Development): spec follows the prescribed structure with priority-ordered, independently testable user stories
- ✓ Principle VI (Simplicity / YAGNI): explicit "extract only when 2+ views need a partial" rule, scope explicitly bounded, all deferrals named

**Violations:**

- Principle III (E2E Testing): the spec relies on manual smoke tests for acceptance and does not explicitly carry forward the existing E2E test suite as the primary quality gate, nor does it require a new E2E test for the role-aware sidebar visibility (which is genuinely new behavior, not just a re-skin). This is the only constitution conflict, and it is straightforward to address.

## Recommendations

### Critical (Must Fix Before Implementation)

- [ ] **Reconcile with Principle III (E2E Testing)**. Add an FR (or expand FR-018) to state explicitly: (a) existing Playwright E2E tests covering specs 001–007 MUST continue to pass after the sweep, as the primary automated quality gate; (b) one new Playwright E2E test MUST be added that asserts role-aware sidebar visibility (Applicant sees only Applicant entries; Reviewer sees Reviewer entries; Admin sees Admin entries; unauthenticated users see no sidebar) since this is the only new behavior; (c) the manual smoke test in FR-018 / SC-005 is a supplementary visual-regression check, not a substitute for the automated E2E gate.

### Important (Should Fix)

- [ ] **Settle the PDF identity claim**. Pick one wording and apply it consistently across FR-015, US3 Independent Test, and SC-007: source `.cshtml` files byte-identical (git-diff verifiable) AND generated PDFs visually identical (manual or pixel comparison verifiable). Drop the conflicting "byte-identical PDF" framing.
- [ ] **Tighten FR-009**. Replace "visually distinct treatment" with either an enumerated canonical mapping (primary/secondary/destructive/state-locking → specific color and icon convention) or a tighter qualitative bar ("color or icon treatment such that two adjacent actions of different classes are unambiguously distinguishable at a glance").

### Optional (Nice to Have)

- [ ] **Resolve the favicon Open Question now** rather than deferring. A one-line decision ("favicon refresh is out of scope for this sweep; tracked as a future quick-win") removes a triage item from the planning phase.
- [ ] **Make US2 / US3 boundary explicit** by enumerating the full set of P3 surfaces (already done in US3 prose) AND the full set of P2 surfaces in one place at the top of US2, so a future contributor doesn't have to re-derive the partition.
- [ ] **Add a brief assumption** that the Tabler.io project's licensing (MIT) is acceptable to the project; this is true today but worth documenting since it's a new vendored dependency.

## Conclusion

The spec is well-structured, scope-disciplined, and traceably anchored to the existing codebase. After applying the iteration-1 fixes inline, the constitution conflict is resolved (Principle III is now honored via FR-019/FR-020 and the updated SC-005), the PDF identity claim is consistent across FR-015, US3 acceptance scenario 5, US3 Independent Test, and SC-007, and FR-009 now enumerates the canonical color/icon treatment per action class.

**Ready for implementation:** Yes.

**Next steps:**
1. User reviews the spec file.
2. Generate `review_brief.md` for stakeholder communication.
3. Proceed to `/speckit-plan`.

## Iteration 2 Resolution

**Critical fix applied — E2E alignment with Principle III:**
- Added **FR-019**: existing Playwright E2E tests for features 001–007 MUST continue to pass; assertions on user-visible behavior MUST NOT be relaxed.
- Added **FR-020**: a new Playwright E2E test MUST be added asserting role-aware sidebar visibility (the only genuinely new behavior).
- Reframed **FR-018** as a supplementary visual-regression check, not a substitute.
- Updated **SC-005** to reflect: existing E2E tests pass + new sidebar test passes + supplementary manual smoke run.

**Important fix 1 applied — PDF identity wording:**
- Settled on two distinct, consistent claims, applied verbatim across FR-015, US3 acceptance scenario 5, US3 Independent Test, and SC-007: (a) the two source `.cshtml` files MUST be byte-identical to their pre-spec contents (`git diff` returns empty); (b) the generated PDF MUST be visually identical (side-by-side comparison). Removed the conflicting "byte-identical PDF" framing.

**Important fix 2 applied — FR-009 tightening:**
- Replaced "visually distinct treatment" with an enumerated canonical mapping: primary = theme accent solid, secondary = outlined neutral, destructive = theme danger, state-locking = theme warning + lock icon.

**Optional fixes applied:**
- Favicon question moved out of Open Questions and into Out of Scope ("explicitly out of scope; tracked as a future quick-win").
- Added MIT-license assumption for Tabler.io as a vendored dependency.

**Optional fix not applied (rationale):**
- The US2/US3 surface partition was already implicitly enumerated (US2 prose lists the high-traffic surfaces, US3 prose lists the remaining surfaces); a top-of-section restatement was deemed redundant and was skipped per YAGNI.
