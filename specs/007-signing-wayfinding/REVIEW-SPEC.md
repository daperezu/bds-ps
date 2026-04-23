# Spec Review: Signing Stage Wayfinding

**Spec:** specs/007-signing-wayfinding/spec.md
**Date:** 2026-04-23
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** Tight, well-bounded wayfinding feature. All three gaps are clearly scoped, all requirements are testable, and the spec correctly disclaims any signing-stage behavioral changes. After iteration: SC-007 added to mandate Playwright E2E coverage per Constitution III. Spec is ready for planning.

## Completeness: 5/5 (after iteration)

### Structure

- [x] All required sections present (User Scenarios, Requirements, Success Criteria, Edge Cases, Assumptions, Dependencies, Out of Scope)
- [x] Recommended sections included (Key Entities disclaimer, explicit Out of Scope)
- [x] No placeholder text, no `[NEEDS CLARIFICATION]` markers

### Coverage

- [x] All functional requirements defined (FR-001 through FR-007)
- [x] All acceptance scenarios present per user story (US1: 4 scenarios, US2: 5 scenarios, US3: 4 scenarios)
- [x] Edge cases covered (7 cases) — includes hidden-banner conditions, role-mixing, concurrent-tab switching, fetch-failure
- [x] Success criteria specified (SC-001 through SC-006) and measurable
- [x] Independent Test prose present for each user story

**Issues:**

- ~~Missing explicit Playwright E2E mandate per Constitution III.~~ **Resolved by adding SC-007 in iteration 1.**

## Clarity: 5/5

### Language Quality

- [x] No ambiguous language — all requirements use `MUST` / `MUST NOT`
- [x] Requirements are specific (exact banner strings quoted; exact tab labels named)
- [x] No vague terms like "fast", "reasonable", "appropriate"
- [x] URL paths used as user-facing anchors are consistent with 006 quickstart conventions (not implementation leakage)

**Ambiguities Found:** None blocking. One minor phrasing note:

1. FR-005 uses "byte-identically" to describe existing-embed preservation.
   - Issue: "Byte-identical" usually describes a file comparison; the HTTP-rendered page is not actually byte-identical across requests (timestamps, anti-forgery tokens, etc.).
   - Suggestion: Not worth changing — in context it clearly means "semantically identical / no user-visible regression" and matches spec 006's language. Leave as-is.

## Implementability: 5/5

### Plan Generation

- [x] Can generate a concrete implementation plan — the scope is a tab layout on `/Review`, a view embed on `ApplicantResponse/Index`, and a status banner; all host pages and the source partial already exist.
- [x] Dependencies identified (specs 002, 004, 006)
- [x] Constraints realistic — explicitly no new controllers, no new authorization, no new tables
- [x] Scope manageable — intentionally small wayfinding feature that reuses existing surfaces

**Issues:** None.

## Testability: 5/5

### Verification

- [x] SC-001 (two-click discovery) — measurable via click count
- [x] SC-002, SC-003 (applicant sees panel + banner without navigation) — directly testable in E2E
- [x] SC-004 (quickstart walkable from prose) — verifiable by a first-time walker
- [x] SC-005 (embed parity between two host pages) — verifiable via automated comparison
- [x] SC-006 (no regression) — verifiable via existing test suite
- [x] Each FR has at least one acceptance scenario backing it

**Issues:** None.

## Constitution Alignment

- [x] I. Clean Architecture — no layer violations; feature composes existing views/partials/routes
- [x] II. Rich Domain Model — N/A; no domain changes
- [x] III. End-to-End Testing (NON-NEGOTIABLE) — SC-007 mandates Playwright E2E coverage for all three wayfinding journeys (resolved in iteration 1).
- [x] IV. Schema-First Database Management — N/A; no schema changes
- [x] V. Specification-Driven Development — this spec is being written before implementation
- [x] VI. Simplicity and Progressive Complexity — explicitly scoped to wayfinding only; out-of-scope list is comprehensive

**Violations:** None after iteration 1.

## Recommendations

### Critical (Must Fix Before Implementation)

None.

### Important (Should Fix)

- [x] ~~Add SC-007 for explicit Playwright E2E coverage.~~ Resolved in iteration 1.

### Optional (Nice to Have)

- [ ] Consider calling out that US3 (quickstart sync) does not require code changes — this is already implied by the "docs-only" framing but could be made explicit in the user story body to prevent an implementer from scaffolding code paths for it.
- [ ] Consider a brief note in Assumptions that the `/Review` landing today renders the Initial Review Queue directly; the new sub-tab structure MUST preserve that default to avoid breaking muscle memory. (Already covered in US1 Scenario 1; could be surfaced up.)

## Conclusion

After one iteration (SC-007 added), the spec is sound, scoped, and implementable. All five review dimensions pass at 5/5; constitution alignment is full; no `[NEEDS CLARIFICATION]` markers remain.

**Ready for implementation:** Yes.

**Next steps:**
1. User review of the final spec.
2. Proceed to `/speckit-plan`.
