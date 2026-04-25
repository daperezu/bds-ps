# Brainstorm Overview

Last updated: 2026-04-25

## Sessions

| # | Date | Topic | Status | Spec |
|---|------|-------|--------|------|
| 01 | 2026-04-15 | core-model-submission | spec-created | 001 |
| 02 | 2026-04-15 | review-approval-workflow | spec-created | 002 |
| 03 | 2026-04-16 | supplier-evaluation-engine | spec-created | 003 |
| 04 | 2026-04-17 | applicant-response-appeal | spec-created | 004 |
| 05 | 2026-04-17 | document-generation | spec-created | 005 |
| 06 | 2026-04-18 | digital-signatures | spec-created | 006 |
| 07 | 2026-04-23 | signing-wayfinding | spec-created | 007 |
| 08 | 2026-04-25 | tabler-ui-strategy | spec-created | 008 |

## Open Threads

- Should there be a maximum number of items per application? (from #01)
- Should there be a maximum number of suppliers per item beyond the minimum? (from #01)
- Retention policy for abandoned draft applications (from #01)
- Performance score on Applicant: manual, calculated, or deferred? (from #01)
- Constitution needs to be filled in after first implementation (from #01)
- Should pagination page size be configurable or fixed? (from #02)
- Does full item-status reset on send-back create unnecessary re-work for reviewers? (from #02)
- Persistence model for `ApplicantResponse`: durable snapshot vs. reconstructed from item-level state (from #04)
- Representation of `AppealMessage`: child entity with identity vs. value object in a collection on `Appeal` (from #04)
- Operational visibility for stuck applications with no deadlines — likely future reporting spec (from #04)
- Whether `ApplicantResponse` decisions should be visible to reviewers in read-only form before an appeal is opened (from #04)
- Terms & Conditions copy ownership and delivery path for the Funding Agreement template (from #05)
- Funder identity shape: single configuration block vs. richer `Funder` aggregate for multi-funder scenarios (from #05)
- Reviewer regeneration rights on the Funding Agreement — revalidate during planning when full role-scope is visible (from #05)
- Syncfusion HTML-to-PDF license acquisition and cost — planning/ops coordination prerequisite (from #05)
- Specific default locale code for LatAm formatting (e.g., `es-CO`, `es-MX`) — to be pinned during planning (from #05)
- Formal audit retention policy for generated Funding Agreement PDFs — deferred to a later compliance-driven spec (from #05)
- Side-by-side view of generated agreement vs. signed upload to aid reviewer visual verification (from #06)
- Execution banner or cover page on executed signed PDF (from #06)
- Final upload size limit value for signed PDFs; 20 MB default proposed (from #06)
- Verify spec 005 precision on which role (reviewer vs. approver) can trigger agreement regeneration, so FR-010 has no inherited ambiguity (from #06)
- Administrative back-out of the signing stage — deliberately out-of-scope gap at feature boundary; ops has no supported path until an admin tooling spec exists (from #06)
- Whether unlimited rejection cycles are acceptable long-term, or whether a future reporting/ops-visibility feature should pressure-test the assumption (from #06)
- Automated reviewer assist (content hash, version marker, or side-by-side diff) to catch mismatched signed uploads — currently purely visual (from #06)
- Should pending-count badges ship with the signing wayfinding, or remain a future polish item once inbox volume makes them valuable? (from #07)
- Should `AppealOpen` get its own banner string on the applicant response page, or remain silent as currently specified? (from #07)
- Is the two-click threshold (SC-001) the right long-term bar, or will future volume eventually justify a top-level nav entry for Signing alongside the sub-tabs? (from #07)
- The 006 signing panel partial is assumed to be shape-clean enough to embed on a second host page; if implementation discovers it is not, reshaping it is within 007 scope but may expose further 006 refactors worth tracking (from #07)
- Specific Tabler.io version pin — deferred to planning (latest stable at planning time) (from #08)
- Sidebar default-open vs. default-collapsed on first load — deferred to planning (from #08)
- Whether the absolute "no badges outside `_StatusPill`" rule should permit non-status badges (e.g., quantity counters) — to be revisited if the planning phase surfaces concrete cases (from #08)
- Whether to invest in visual-regression tooling (Playwright screenshot comparison or Percy) before the sweep, or leave manual side-by-side as the v1 visual gate (from #08)
- Whether `_ConfirmDialog` for every destructive action (including draft-item deletes) is the right baseline, or whether specific exceptions should be enumerated (from #08)
- Future spec 009 (communication surface — unified messaging panel) needs its own brainstorm before any implementation (from #08)
- Future spec 010 (notifications & inbox) needs its own brainstorm — likely SignalR (from #08)
- Future spec 011 (localization layer) — when it lands, partials must be checked to ensure no UI copy was embedded during the 008 sweep (from #08)
- Future spec 012 (admin/configuration surface polish) — likely needed once the 008 sweep lands (from #08)

## Closed Threads

- Will version history be sufficient for audit needs, or will the Appeal spec need a Resolution entity? (from #02) — **Closed by #04**: no `Resolution` entity needed; appeal resolution is a state transition + audit entry.
- Post-signature regeneration lockout on the Funding Agreement (from #05) — **Closed by #06**: resolved as "regeneration permitted until first signed upload; locked thereafter; administrative back-out explicitly out of scope for this feature."

## Parked Ideas

None.
