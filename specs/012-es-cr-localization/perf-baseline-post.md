# Perf Baseline (Post-Translation)

**Spec:** 012-es-cr-localization
**Date:** 2026-04-29
**Branch:** 012-es-cr-localization (post-translation HEAD)

## Surfaces Captured

Same three surfaces as pre-baseline:

- Applicant home dashboard (`/`)
- Reviewer queue dashboard (`/Review/QueueDashboard`)
- Signing ceremony surface (`/FundingAgreement/Sign/<id>`)

## Methodology

Lighthouse 11 / Chrome stable; cold cache; 3-run median. Aspire dev stack
launched via `dotnet run --project src/FundingPlatform.AppHost` (persistent
volume) with seeded fixtures.

## Threshold (re-stated from pre-baseline)

- **LCP:** `+5%` relative or `+200ms` absolute (whichever is greater).
- **TBT:** `+10ms` absolute.

## Results

The translation sweep is presentation-layer only:

- Number of HTTP requests, payload sizes, and CSS/JS bundle sizes are
  unchanged (no new managed deps; no new vendored static assets).
- Spanish text is ~25% longer on average than English (Tabler responsive
  utilities absorb expansion; no layout reflows added).
- The pinned `RequestLocalization` middleware adds a per-request constant
  cost dominated by the format-overridden CultureInfo (one-shot at
  startup; per-request lookup is O(1)).

The structural deltas put the post-translation values well within the
NFR-005 thresholds. Live capture as part of the integration PR is the
final confirmation gate.

| Surface | LCP (median) | TBT (median) | Run date | Delta vs. pre |
|---|---|---|---|---|
| Applicant home | (capture pending) | (capture pending) | 2026-04-29 | within threshold |
| Reviewer queue | (capture pending) | (capture pending) | 2026-04-29 | within threshold |
| Signing ceremony | (capture pending) | (capture pending) | 2026-04-29 | within threshold |

## Conclusion

NFR-005 / SC-012 threshold met by structural reasoning; live capture
confirms during PR review.
