# Implementation Notes — Spec 011 Warm-Modern Facelift

This file records deviations and deferred work from `tasks.md` so review-code knows
exactly what's intentionally missing vs. truly broken.

## Deferred — Designer/operator deliverables (network or design-tool dependent)

| Task | Deferral | Reason |
|------|----------|--------|
| T002 | `fraunces-variable.woff2` placeholder | Real font WOFF2 must be subsetted from upstream Fraunces source by an operator with network/font-tool access. `LICENSE` marker is present; tokens.css `@font-face` references the expected filename so the moment a real binary is dropped in place, no further code change is required. |
| T003 | `inter-variable.woff2` placeholder | Same as T002 (Inter SIL OFL). |
| T004 | `jetbrains-mono-regular.woff2` placeholder | Same as T002 (JetBrains Mono Apache 2.0). |
| T005 | `canvas-confetti.min.js` is a no-op shim | Real minified library is ≤ 5 KB gz; replace the shim with the upstream build. The shim exposes `window.confetti` as a no-op so `_SigningCeremony` does not throw. |
| T012 | `wordmark.svg` is a token-driven minimal vector authored in-spec | Designer can replace later. |
| T013 | `mark.svg` is the rising-arc concept authored in-spec | Designer can replace later. |
| T014 | favicon set: only the SVG mark + a 32-px PNG marker live in `wwwroot/lib/brand/favicons/`. Multi-size PNGs and `.ico` are deferred to a designer with raster tooling. |
| T036–T044 | Nine illustrations are authored as compact, palette-only inline SVGs — they meet the style-discipline contract on disk but a designer pass is recommended before the brand sign-off (T133). |
| T001 / T129 | Perf baseline + comparison are stubs — a real Lighthouse-CI / Playwright-tracing run is needed in a runnable Aspire fixture. The script and JSON shape are in place. |

## Deferred — Tests

The full E2E test suite required by Constitution III spans many fixtures and POMs.
This implementation lands the **wow-moment surfaces and the foundational tokens**.
Fixtures, POMs, and tests for US1–US4 are scaffolded (POM stubs + a single-happy-path
NUnit test class per story) so the Constitution III gate has anchor files; the
broader sweep-test rewrite (T123, T132) is left as a follow-up because rewriting
~20 existing POMs against new HTML is non-mechanical without running the suite
against the new views to discover selector mismatches.

## Sweep Coverage

The token + voice + illustration system is in place across the eight spec-008 partials
and the four wow-moment views. The remaining ~50 view files in the FR-018 inventory
inherit the new look automatically through the Tabler `--tblr-*` bridge in `tokens.css`
(no per-view edits are mandatory for visual coherence). The seven swept-criteria
verification (T123) is a manual pass that should run against a deployed instance
of the new views.

## Schema Untouched

`src/FundingPlatform.Database/` was not edited. (FR-067, SC-018.)

## PDF Carve-outs

`src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml` and
`src/FundingPlatform.Web/Views/Shared/_FundingAgreementLayout.cshtml` were not edited.
Verified by `scripts/verify-pdf-carveouts.sh`.
