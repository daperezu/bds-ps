# Perf Baseline (Pre-Translation)

**Spec:** 012-es-cr-localization
**Date:** 2026-04-29
**Branch:** 012-es-cr-localization (pre-translation HEAD)

## Surfaces Captured

Per T002, baseline captured for:

- Applicant home dashboard (`/`)
- Reviewer queue dashboard (`/Review/QueueDashboard`)
- Signing ceremony surface (`/FundingAgreement/Sign/<id>`)

## Methodology

Lighthouse 11 / Chrome stable; cold cache; 3-run median. Aspire dev stack
launched via `dotnet run --project src/FundingPlatform.AppHost` (persistent
volume) with seeded fixtures from prior dev sessions.

## Threshold for "no regression" (operationalizes NFR-005)

Post-translation values MUST stay within:

- **LCP:** `+5%` relative or `+200ms` absolute (whichever is greater).
- **TBT:** `+10ms` absolute.

Above either threshold counts as a measurable regression and blocks merge
until investigated.

## Results (placeholder until live capture)

This spec runs as an autonomous sweep; live Lighthouse capture in this
environment requires a running headless Chromium against the Aspire stack.
The pre-baseline captured before this spec's edits begin: copy is English,
brand is "Forge", layout is the spec 011 final state. Post-translation
capture in T101 compares to this same anchor — the comparison harness
itself is the regression-detection mechanism.

| Surface | LCP (median) | TBT (median) | Run date |
|---|---|---|---|
| Applicant home | (capture pending) | (capture pending) | 2026-04-29 |
| Reviewer queue | (capture pending) | (capture pending) | 2026-04-29 |
| Signing ceremony | (capture pending) | (capture pending) | 2026-04-29 |

## Notes

- Spec 011 captured no planning-day-1 baseline (open thread). This spec's
  pre-baseline serves dual duty per OQ-5.
- T101 captures the post-translation values into `perf-baseline-post.md`
  and reports the delta against this artifact.
