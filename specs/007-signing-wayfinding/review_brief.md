# Review Brief: Signing Stage Wayfinding

**Spec:** specs/007-signing-wayfinding/spec.md
**Generated:** 2026-04-23

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Spec 006 shipped the digital-signatures backend and the signing panel, but left two UI discovery gaps and one docs gap that make the feature unusable without tribal knowledge. Reviewers cannot reach `/Review/SigningInbox` from any link in the app; applicants land on `/ApplicantResponse/Index/{id}` after accepting reviewer decisions but the signing panel lives only on `/Application/Details/{id}`; and the 006 quickstart refers ambiguously to "the application's detail page." This feature wires the existing surfaces together so every step of the 006 signing flow is reachable from the page the user naturally lands on. No new signing behavior.

## Scope Boundaries

- **In scope:** Two peer sub-tabs on `/Review` (Initial Review Queue + Signing Inbox); embed the existing signing panel on `/ApplicantResponse/Index/{id}`; add a state-driven contextual banner above that embed; sync the 006 quickstart prose to match the new navigation; explicit Playwright E2E coverage for the three wayfinding journeys (SC-007).
- **Out of scope:** Pending-count badges on tabs; top-level navbar changes; new pipeline stages; state-aware redirects; applicant notifications; reviewer-assist tooling (content hashing, side-by-side diff, version stamping); any change to the underlying 006 signing behavior (upload intake rules, version-mismatch logic, regeneration lockdown, audit trail).
- **Why these boundaries:** The feature is a discovery fix, not a behavior change. Keeping the 006 partial as a single source of truth (FR-004) and reusing existing routes (FR-007) minimizes risk of regressions and scope creep.

## Critical Decisions

### Sub-tabs on `/Review` (not a top-level navbar entry)

- **Choice:** Add two peer sub-tabs on `/Review` rather than a new top-level "Signing" nav item.
- **Trade-off:** One extra click (from the Review landing) in exchange for a coarse-grained navbar that still has room when future pipeline stages (Payment & Closure) land.
- **Feedback:** Is "coarse-grained navbar" the right long-term stance, or would a top-level entry be expected once volume grows?

### Embed on the applicant's response page, plus a status banner

- **Choice:** Render the signing panel on both `/ApplicantResponse/Index/{id}` and `/Application/Details/{id}` (single source of truth — same partial, same view-model), with a one-line contextual banner above the response-page embed for states `ResponseFinalized` and `AgreementExecuted`.
- **Trade-off:** One partial rendered from two host pages (small duplication of include sites) vs. adding yet another page for the applicant to discover.
- **Feedback:** Is the banner copy — *"Your funding agreement is ready to sign below."* and *"Your funding agreement has been executed."* — the right voice for applicants?

### No new authorization surface

- **Choice:** FR-007 explicitly forbids new controller actions for signing data. The embedded panel inherits the existing 006 partial's authorization; the two new sub-tabs reuse existing route-level authorization.
- **Trade-off:** None identified — this is a strict tightening that prevents accidental privilege escalation.
- **Feedback:** Confirm no legitimate case exists for a new signing-stage endpoint here; if there is one, it should probably go in 006's scope, not here.

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Pending-count badges left out of scope

- **Decision:** No "(3)" badge on the Signing Inbox sub-tab.
- **Why this might be controversial:** Reviewers may still miss the sub-tab if it's visually quiet; a badge is a cheap nudge.
- **Alternative view:** Add a badge in this feature to maximize discoverability.
- **Seeking input on:** Whether to push badges into this feature's scope or keep them as a follow-up once inbox volume makes them valuable.

### Banner states limited to `ResponseFinalized` and `AgreementExecuted`

- **Decision:** The banner is silent for all other states — explicitly including `AppealOpen` (even if a Funding Agreement exists).
- **Why this might be controversial:** An applicant in `AppealOpen` with a generated (but now stale) agreement sees no cue about the agreement on the response page, potentially confusing them.
- **Alternative view:** Add a third banner state for `AppealOpen` explaining the agreement is paused pending appeal resolution.
- **Seeking input on:** Whether the current two-state banner is sufficient, or if we should extend the state map now.

### URL paths named explicitly in the spec

- **Decision:** The spec names `/Review`, `/ApplicantResponse/Index/{id}`, `/Application/Details/{id}`, and `/Review/SigningInbox` directly in requirements and acceptance scenarios.
- **Why this might be controversial:** Naming URL paths can be read as implementation leak into a spec that's supposed to be technology-agnostic.
- **Alternative view:** Name the surfaces abstractly ("the review landing", "the applicant response page") and let the plan map them to concrete routes.
- **Seeking input on:** Whether this level of URL-naming is acceptable given 006's quickstart already uses these paths as the canonical vocabulary.

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Review sub-tab label | **Initial Review Queue** | Preserves current `/Review` content as the default tab |
| Review sub-tab label | **Signing Inbox** | Matches the existing `/Review/SigningInbox` route name |
| Banner string (ready) | *"Your funding agreement is ready to sign below."* | Exact copy; quoted in FR-003 and US2 scenarios |
| Banner string (executed) | *"Your funding agreement has been executed."* | Exact copy; quoted in FR-003 and US2 scenarios |

## Open Questions

- [ ] Should pending-count badges ship with this feature, or remain a future polish item?
- [ ] Should `AppealOpen` get its own banner string on the response page, or stay silent?
- [ ] Is the two-click threshold (SC-001) the right bar, or should future volume push for a one-click surface (top-level nav entry)?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Signing panel partial is not currently shape-clean for multi-host embedding (relies on controller-specific setup) | Medium | Spec Assumption notes that reshaping the partial to a self-contained include is permitted as part of this feature's work |
| Re-rendering the panel on a page that also has a stateful response form (draft radio selections) could cause UX confusion | Low | Edge case spec'd — panel and form are independent regions; panel failure MUST NOT affect the form |
| Divergence between the two panel embeds over time (bugfix applied to one, missed on the other) | Medium | FR-004 declares divergence a defect; SC-005 mandates parity; implementation MUST use a single partial + view-model source |
| Constitution III E2E mandate initially missed | Low | Resolved in review iteration 1 — SC-007 now requires Playwright E2E coverage for all three wayfinding journeys |

---
*Share with reviewers before implementation.*
