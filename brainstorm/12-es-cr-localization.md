---
name: 12-es-cr-localization
description: Translate the entire user-facing surface to Costa Rican Spanish (es-CR) and rename the product from Forge to Capital Semilla. Pins the long-deferred localization layer and closes four open threads from prior specs.
type: brainstorm
status: spec-created
spec: specs/012-es-cr-localization/
---

# Brainstorm: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Date:** 2026-04-29
**Status:** spec-created
**Spec:** specs/012-es-cr-localization/

## Problem Framing

The platform shipped English-only across 11 prior specs while deliberately leaving localization deferred. Spec 008 deferred i18n while architecting components to receive copy as parameters; spec 011 reaffirmed the deferral and added a voice-guide-drift compatibility caveat. Spec 005 left the Funding Agreement PDF locale anchored to a placeholder `es-CO`. Spec 011 also left the brand sign-off as an open thread (Forge / Ascent / keep FundingPlatform).

Two adjacent decisions converged into one feature:
1. Translate the platform to Costa Rican Spanish for the regional deployment (no multi-language requirement).
2. Rename the product from "Forge" to "Capital Semilla" — the brand sign-off long deferred.

The framing called for resolving both in a single coordinated sweep: pin `es-CR`, translate every user-facing string, swap brand identifiers in user-rendered surfaces, and rewrite E2E test assertions to match.

## Approaches Considered

### A: Single coordinated sweep (Chosen)
- One spec, one branch, one merge. All Razor views (~72), framework messages (validation + Identity), the status registry, the Funding Agreement PDF body, the brand SVGs, and E2E test assertions translated together.
- Pros: No mixed-language window; voice guide stays internally consistent; closes 3 deferral threads + 1 brand thread in one stroke; matches the pattern that worked for 008 and 011.
- Cons: Large diff; if voice tone needs retuning, rework touches everything at once.

### B: Phased by audience surface
- Three sub-deliverables: applicant-facing → reviewer/admin → PDF / final cleanup.
- Pros: Voice-guide tunable on phase 1 evidence; smaller review chunks.
- Cons: Mixed-language windows between phases; status registry strings appear in all surfaces, so phase boundaries leak; coordination overhead.

### C: Two-phase — voice guide first, then sweep
- Phase 1: Voice guide + ~10 representative surfaces translated as proof. Phase 2: full sweep applies the validated voice.
- Pros: De-risks voice/dialect choice before heavy work.
- Cons: Stalls behind explicit stakeholder sign-off step; effectively still a single-merge sweep at the end.

### Architecture sub-decision: Replace in place vs. resx-scaffolded
- **Replace in place (Chosen)**: hard-code Spanish inline at the view-model / partial-parameter / status-registry seams already designed by specs 008 / 011. No `IStringLocalizer`, no `.resx`, no culture-middleware-for-copy.
- **Single-locale resource files (rejected)**: extract everything to `.resx` even though only es-CR ships. Adds the i18n seam now — bigger upfront refactor, cheaper if a second locale is ever added. Rejected on YAGNI: the user explicitly said no multi-language.
- **Hybrid: shared dictionary for repeated copy only (rejected)**: route repeated strings through a `UiCopy` constants class. Rejected because the existing `_StatusPill` registry already serves as the only repeated-string surface that needs this; one-off view copy stays inline.

### Architecture sub-decision: Code stays English; UI is Spanish
- The user added an explicit constraint mid-brainstorm: "I want the code to remain in english, no mix, if an enum i.e. was displayed as is in the ui, we need to translate it." Captured as NFR-001. The seam is at the user-facing string boundary; everything else (identifiers, routes, log messages, exception messages, JSON config keys, DB schema) stays English. Any enum currently rendered to UI via `.ToString()` is routed through the display registry (FR-010).

### Format-locale sub-decision: Strict `es-CR` vs. preserve comma-decimal
- **Strict `es-CR` (Chosen)**: use `.NET`'s `es-CR` `CultureInfo` as-is. Period decimal, comma thousands (`1,234.56`), `dd/MM/yyyy`. Authentic to CR business conventions; closes spec 005's open thread by pinning a final code.
- This SHIFTS the Funding Agreement PDF format from `1.234,56` (the old `es-CO` default) to `1,234.56`. Flagged as a regression-risk-check during planning.
- The "preserve comma-decimal" alternative (override `NumberFormatInfo` while keeping Spanish text) was rejected because it would feel imported/foreign to CR users.

### Test-impact sub-decision: Translate assertions vs. force-data-testid push
- **Translate assertions to Spanish (Chosen)**: update every Playwright visible-text assertion to assert on Spanish. Closes spec 011's selector-strategy thread by treating "visible Spanish text is the contract" as the long-term policy.
- The "force `data-testid` push" alternative was rejected as scope creep; the "hybrid" alternative was rejected as a half-measure that mixes concerns.

## Decision

**A (single coordinated sweep) with replace-in-place architecture, code-stays-English seam, strict `es-CR` formatting, formal `usted` warm-modern voice, and test-assertions-translated-to-Spanish test policy.** Brand renamed to **Capital Semilla** (closes spec 011's brand sign-off thread). Voice guide artifact lands first inside the same spec workstream (de-risking benefit of C without splitting deliverables).

The spec at `specs/012-es-cr-localization/spec.md` includes 7 prioritized user stories, 25 FRs, 8 NFRs, 12 SCs, 11 edge cases, and 9 open questions appropriate for planning. Formal review: SOUND, no critical issues.

## Open Threads

- (OQ-1) Glossary finalization — application/review/funding agreement/send back terms — voice guide owns the choice
- (OQ-2) Footer tagline exact phrasing for "built for entrepreneurs" — recommended `diseñado para emprendedores`
- (OQ-3) Designer SVG follow-ups — wordmark rework + on-image text audit on the 9 empty-state illustrations; whether either blocks merge
- (OQ-4) Tabler vendor JS string audit — whether any in-use components carry built-in copy
- (OQ-5) Performance baseline (LCP/TBT) capture — pin if spec 011's planning-day-1 baseline wasn't taken
- (OQ-6) Voice-guide reviewer — same designer/voice owner as spec 011 or new CR-region reviewer
- (OQ-7) Page-title direction — `[Page] - Capital Semilla` (matches today) vs. reversed
- (OQ-8) Hard-pin culture via constant vs. config-overridable hatch
- (OQ-9) Final identifier name for the JS namespace rename — `PlatformMotion` recommended vs. `AppMotion` / `SeedMotion`
- Future spec 013 (communication / messaging surface) — still pending its own brainstorm (carried forward from #08)
- Future spec 014 (notifications / inbox / SignalR) — still pending its own brainstorm (renumbered from #11's "spec 012" forecast since 012 went to localization)
- Future spec 015 (public marketing surface) — still pending its own brainstorm; this spec is authenticated-only plus the auth pages and the generic Error page

## Threads Closed by This Brainstorm

- **Spec 005's "specific default locale code for LatAm formatting"** → pinned to `es-CR` (FR-016).
- **Spec 008's "future spec (localization layer) — partials must be checked to ensure no UI copy was embedded during the 008 sweep"** → executed during this spec's UI sweep (User Story 2 + NFR-004).
- **Spec 011's "Future spec 014 (localization layer) — voice-guide rewrites in spec 011 must keep copy out of partials' code paths to remain compatible"** → carried out (User Story 1 + NFR-004).
- **Spec 011's "Display brand name selection — Forge / Ascent / keep FundingPlatform — user sign-off gate"** → closed with **Capital Semilla** (FR-006).
