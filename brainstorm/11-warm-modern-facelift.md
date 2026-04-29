# Brainstorm: Warm-Modern Facelift

**Date:** 2026-04-27
**Status:** spec-created
**Spec:** specs/011-warm-modern-facelift/

## Problem Framing

Spec 008 gave the platform visual consistency through Tabler.io theming and a reusable partial library — a solid foundation, but a deliberately flat one. That spec called itself "visual-only" and explicitly resisted Tabler's dashboard demos and "wow factor" templates. The user now wants the opposite-layer initiative: a "whole facelift" that elevates the platform from utilitarian foundation to professional, "wow factor" finish appropriate for a funding agency serving entrepreneurs. The session shape was decided early: this is a NEW brainstorm (#11), not a revisit of #08 — the Tabler decision in #08 stands; this is the next layer of brand identity, signature surfaces, and choreographed motion built on that foundation.

## Strategic Decisions Made

### Surface focus

Four options were considered:

- **A: Authenticated app surface only.** Polish the working surface where applicants and reviewers live. Trust is built during the journey, not the landing page.
- **B: Public marketing surface (new).** Build a real public landing for entrepreneur acquisition.
- **C: Both, in one spec.** Largest visible lift; biggest review surface.
- **D: Brand identity foundation first.** Tokens-only spec; subsequent specs apply it surface-by-surface.

**Decision: A, "authenticated app surface."** The platform has no real public surface today (Home is a one-card placeholder), and 99% of user time is spent post-login. Building a marketing site is a different beast (copywriting, SEO, brand identity workstream); the authenticated experience is where this lift compounds.

### Aesthetic direction

Four options:

- **A: Confident institutional** (Carta / Stripe Atlas / Bloomberg-lite).
- **B: Warm modern** (Mercury / Ramp / Notion).
- **C: Motion-forward / product-led** (Linear / Vercel / Arc).
- **D: Editorial / premium publication** (Apple Newsroom / FT / a16z).

**Decision: B, "warm modern."** Friendly and human but premium; lowers anxiety for first-time applicants while still feeling serious. Color personality (amber accent, warm forest green) over generic-fintech-blue. Conversational copy without being salesy.

### Audience priority

Three options:

- **A: Applicant-first, reviewers stay utilitarian.**
- **B: Equal lift across all roles.**
- **C: Applicant-only.**

**Decision: B, "equal lift across all roles."** Reviewer surfaces still preserve density discipline (FR-060: reviewer tables use `--space-2`, applicant surfaces use `--space-4`), but the warm-modern tokens, typography, illustrations, and motion vocabulary apply uniformly. Avoids visible style divergence between roles.

### Scope intensity

Four options:

- **A: Tokens + signature moments + full sweep.**
- **B: Tokens + signature moments only.**
- **C: Tokens only — design system swap.**
- **D: Signature moments only, defer tokens.**

**Decision: A, "tokens + signature moments + full sweep."** Mirrors spec 008's full-sweep philosophy. The 8 spec-008 partials propagate token changes for free, so the sweep is mostly verification + voice-guide rewrites + new illustration plumbing.

### Signature wow moments

MultiSelect — all four candidates accepted:

1. **Applicant home dashboard** — replaces today's empty placeholder home for logged-in applicants; biggest first-impression upgrade.
2. **Application journey timeline** — reusable Razor component (3 variants: Full / Mini / Micro); makes "status is the spine" felt.
3. **Signing ceremony moment** — replaces flash-message-and-redirect with a celebratory take-over view (4 variants by who signed when).
4. **Reviewer queue dashboard** — elevates the plain Review/Index table to a heads-up display while preserving density.

### Brand identity anchor

Three options:

- **A: Use existing brand assets.**
- **B: Propose a new brand identity in the spec** (greenfield).
- **C: Defer brand to a separate spec, use placeholder palette.**

**Decision: B.** Spec proposes display name candidates (*Forge*, *Ascent*, or keep `FundingPlatform`), color story (warm forest green primary + warm amber accent + warm neutrals + warm-retuned status), type pairing (Fraunces display + Inter body + JetBrains Mono mono — all self-hosted), logo direction (wordmark + abstract mark concept), and a one-page voice guide (`BRAND-VOICE.md` deliverable). Final selection of name and exact hex values is a user sign-off gate (FR-072 / SC-020).

### Motion budget

Three options:

- **A: Restrained** — transitions only, signing ceremony as the only celebration.
- **B: Choreographed** — micro-interactions everywhere.
- **C: Minimal** — instant, no transitions.

**Decision: B, "choreographed."** Documented motion catalog with one-token-per-purpose (`--motion-instant`, `--motion-fast`, `--motion-base`, `--motion-slow`, `--motion-celebratory`); spring easing for journey timeline + signing ceremony; number tickers on dashboards; signing ceremony confetti via `canvas-confetti` (≤ 5 KB gz, the single library exception). Hard-coded `prefers-reduced-motion` contract codified at the bottom of `tokens.css`.

### Spec packaging

Three options:

- **A: Single mega-spec** (mirrors 008).
- **B: Two specs in series** (tokens + sweep first, signature moments second).
- **C: Three specs** (most decomposed).

**Decision: A, "single mega-spec."** Pre-prod status justifies aggressive scope. Same risk profile as spec 008.

## Significant Mid-Brainstorm Pivot

During §5 (Sweep mechanics) draft, the user pushed back on a passage that framed E2E expectations as "preserve existing markup so tests don't break". Their feedback (paraphrased): *"don't hold yourself to save work on adjusting later element selectors or similar in the e2e — I want the best of the best in UX/UI; I want our e2e tests all reflect the quality."*

This pivot reversed spec 008's stance (visual-only, preserve markup, preserve POMs). The §5 section was rewritten to:

- Permit (and encourage) HTML restructuring where it serves UX/UI quality.
- Budget Page Object Model rewrites and selector migration as part of the spec.
- Migrate to semantic locators (ARIA roles + accessible names; `data-testid` where insufficient).
- Elevate test quality alongside selector updates (replace flaky waits, tighten assertions to assert the new UX, not just "page loaded").
- Require dedicated E2E coverage for each of the four wow moments + a reduced-motion test.

Saved as a durable feedback memory (`feedback_ui_quality_over_e2e_stability.md`) and indexed in `MEMORY.md`. Applies to all future facelift / UX-elevation work in this project.

## UX/UI Principles (additive to spec 008)

Spec 008 established six principles (status is the spine, reviewers scan vs. applicants are guided, documents are first-class, every action has a reversibility class, evidence trails over toasts, empty states are wayfinding). This brainstorm adds three:

7. **Brand presence is felt, not announced.** Identity shows up in micro-decisions (typography rhythm, motion easing, color warmth, illustration tone) rather than in logo slabs or marketing copy. *Spec mechanism:* design-token layer + voice guide + illustration set discipline.
8. **Every wow moment must earn its motion budget.** Choreographed motion is allowed but catalog-bound; new motion outside the catalog requires spec amendment. Reduced-motion is non-negotiable. *Spec mechanism:* §4 motion catalog + FR-013 + FR-015.
9. **Density per audience.** Applicant surfaces are generous; reviewer surfaces are dense; both inherit the same brand. Codified in token usage, not as ad-hoc per-page overrides. *Spec mechanism:* FR-060 (`--space-4` for applicant, `--space-2` for reviewer).

## Phased Plan (forward-looking)

This brainstorm produces **one spec immediately** (011) and identifies the next likely specs in the strategic queue:

- **Spec 011 (THIS spec, created):** Warm-modern facelift — brand identity + design tokens + motion system + sweep + 4 wow moments + 9-scene illustration set.
- **Spec 012 (future):** Notifications & inbox — real-time stage transitions on the journey timeline (SignalR), pending-count badges on the signing inbox, notification bell. Was previously "future spec 010" before admin-reports took 010.
- **Spec 013 (future):** Communication surface — unified messaging panel for reviewer ↔ applicant interactions on Application detail. Was previously "future spec 009" before admin-area took 009; still pending its own brainstorm.
- **Spec 014 (future):** Localization layer — ASP.NET resource files, culture middleware, Spanish translations. Brand voice rewrites in spec 011 must keep copy out of partials' code paths to remain compatible.
- **Spec 015 (future):** Public marketing surface — entrepreneur acquisition site (hero stories, social proof, SEO). Distinct workstream; needs its own brainstorm.

## Risks & Anti-Patterns Captured

- **Mega-spec scope creep.** Mitigated by explicit out-of-scope guardrails (FR-068..FR-071) and a manual `SWEEP-CHECKLIST.md` per FR-023.
- **PDF carve-out drift.** Same risk as spec 008; FR-020 + SC-014 + edge-case enumeration; PDF byte-identity verification.
- **Asset-budget overrun.** Three font families + 9 SVGs + confetti must fit ≤ 400 KB gz; SC-016 verifiable; option to drop JetBrains Mono if budget binds.
- **Reduced-motion contract missed.** SC-012 requires a dedicated Playwright reduce-motion test.
- **WCAG AA contrast regression on the warm-tuned palette.** SC-013 requires axe-playwright verification.
- **Reviewer triage slowdown** from removing the status-pill column on queue rows (FR-056). Open question for reviewer feedback.
- **Schema-unchanged constraint forces awkward query-time aggregation.** Documented escape hatch via `speckit-spex-evolve` (FR-067).
- **Brand bikeshedding** stalls planning. FR-072 / SC-020 makes the brand-sign-off gate explicit and user-owned.
- **E2E rewrite cost overrun.** Accepted via the saved feedback memory; quality > test-stability for facelift work.
- **Premature partial abstraction.** Same rule as spec 008: a new partial only when 2+ surfaces need it AND its surface is small. Four new partials introduced, all multi-host.
- **Animation library creep.** Hard prohibition (FR-016); only `canvas-confetti` (≤ 5 KB gz) carve-out for the signing ceremony.
- **Voice-guide drift after merge.** SC-019 mandates per-string voice-guide review of swept views; future-localization-compatibility caveat in Assumptions.

## Decision

A strategic brainstorm document captured the principles, phased plan, and risks; the first executable feature was created as `specs/011-warm-modern-facelift/` covering brand identity, design tokens, motion system, full sweep, four signature wow moments, and a 9-scene empty-state illustration set. Spec went through one inline review iteration (resolved three Important findings: FR-029 cap, FR-042 wording, SC-021 measurability) and is marked SOUND. Ready for `/speckit-plan`.

Mid-brainstorm pivot (E2E quality > stability) was saved as a durable feedback memory and applies to future UX/UI work.

## Open Threads

- Display brand name selection — *Forge* / *Ascent* / keep `FundingPlatform` — user sign-off gate (FR-072) before merge.
- Exact hex values for the warm forest green primary + warm amber accent + warm neutrals + warm-retuned status palette — pinned during planning by designer pass.
- 8 px spacing scale ratios and full type-scale ramp — pinned during planning after density audit on densest surfaces.
- Tabler `--tblr-*` CSS-variable bridge aggressiveness — inventory pinned during planning.
- `canvas-confetti` (or equivalent ≤ 5 KB gz) exact dependency pin — pinned during planning.
- Visual-regression tooling adoption (Playwright screenshot comparison or Percy / Chromatic) — recurring open question from spec 008; defer or now is reviewer feedback.
- Selector strategy precedence (role/aria vs. `data-testid`) — pin during planning so all POM rewrites are uniform.
- Designer source for the 9 illustration SVGs (in-team / commission / adapted-from-open) — affects timeline; decide before US7 work begins.
- Empty-state surface audit — verify the 9-scene set covers all current empty-state usages or whether a 10th is needed; planning runs the grep.
- Unified event source service vs. query-time stitching for the activity feeds across US1 + US4 (and journey tooltips) — decide during planning.
- Canonical journey-stage mapping owner — extend `IStatusDisplayResolver` from spec 008 vs. introduce a sibling `IJourneyStageResolver` (FR-036) — planning decision.
- Multi-branch journey rendering (Send-back loop AND active Appeal in one application) — visual contract pinned during planning.
- Reviewer queue activity-feed positioning at ≥ 1440 px (above table vs. right rail) — defaults to "above"; revisit if reviewer feedback contradicts.
- Confirm removing the status-pill column from reviewer queue rows in favor of inline micro journey timeline (FR-056) loses no information for reviewers — open for reviewer feedback.
- Signing ceremony view-vs-partial choice (FR-044) — pin during planning.
- Signing ceremony fresh-vs-bookmark mechanism (TempData / query / one-shot session token, FR-047) — pin during planning.
- Login/Register tone (clean single-CTA vs. light marketing hero) — defaults to "clean"; revisit if planning surfaces a brand argument.
- Schema-unchanged constraint (SC-018) escape-hatch protocol via `speckit-spex-evolve` — protocol established; specific trigger not anticipated.
- Performance baseline (LCP / TBT) capture timing — must run as planning day-1 task before any code lands (FR-073).
- Future spec 012 (notifications & inbox / SignalR) needs its own brainstorm before any implementation; spec 011 deliberately excludes real-time push.
- Future spec 013 (communication surface — unified messaging panel) still pending its own brainstorm; spec 011 re-skins reviewer-applicant comments but does not restructure them.
- Future spec 014 (localization layer) — voice-guide rewrites in spec 011 must keep copy out of partials' code paths to remain compatible (Assumptions clause).
- Future spec 015 (public marketing surface) — distinct workstream; this brainstorm explicitly chose authenticated-only for spec 011.
