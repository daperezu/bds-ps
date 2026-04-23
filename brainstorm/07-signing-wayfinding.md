# Brainstorm: Signing Stage Wayfinding

**Date:** 2026-04-23
**Status:** spec-created
**Spec:** specs/007-signing-wayfinding/

## Problem Framing

Spec 006 shipped the digital-signatures backend and the signing panel but left three discovery gaps that surfaced the moment the feature was exercised end-to-end in a browser. The `/Review/SigningInbox` route is live but orphaned — no link in the navbar, no link on `/Review`, reviewers have to type the URL. Applicants land on `/ApplicantResponse/Index/{id}` after accepting reviewer decisions but the Funding Agreement signing panel lives only on `/Application/Details/{id}`, so applicants have no visible way to proceed. The 006 quickstart compounds this by referring to "the application's detail page" without distinguishing between the two detail-ish pages that exist.

These are not 006 design mistakes — they're gaps between "the backend and one host page exist" (what 006 shipped) and "every role can reach every step of the flow from the page they naturally land on" (what the feature needs to be usable). This session scoped a small, cohesive polish feature to close them.

A fourth missing piece was discovered in the same session (no auto-deploy of the dacpac from Aspire AppHost) and was fixed out-of-band before this brainstorm started; it is not part of the 007 scope.

## Approaches Considered

### A: Reviewer nav placement — top-level navbar entry
- Pros: Fastest to reach from anywhere (single click from any page).
- Cons: Grows the top-level navbar per pipeline stage; does not scale when future stages (Payment & Closure) land.

### B: Reviewer nav placement — sub-tabs on `/Review` (CHOSEN)
- Pros: Keeps the navbar coarse-grained; surfaces the two review-side pipelines (Initial Review, Signing) as peers under one entry point; scales to future stages naturally.
- Cons: One extra click vs. a dedicated top-level entry.

### C: Reviewer nav placement — both (navbar + sub-tabs)
- Pros: Maximum discoverability.
- Cons: Redundant; costs navbar real estate for no new capability.

### D: Applicant panel placement — embed on `/ApplicantResponse/Index/{id}` only (move from Application/Details)
- Pros: Single host page.
- Cons: Would remove the panel from Application/Details, which reviewers also use — breaks their flow.

### E: Applicant panel placement — contextual banner/link only, no embed
- Pros: Smallest UI change.
- Cons: Still requires a navigation hop; the real gap is "applicant cannot find the panel from the page they're on", which a link doesn't fully close vs. an embed.

### F: Applicant panel placement — embed on both host pages + contextual banner (CHOSEN)
- Pros: Applicant sees signing UI where they naturally land; reviewers still have it at Application/Details; single source of truth for the partial; banner provides the "you're at this stage" context on the response page where it's not obvious.
- Cons: Partial is now invoked from two host pages (mild duplication of include sites; no duplication of logic if the partial stays shape-clean).

### G: Applicant panel placement — state-aware redirect from response page to Application/Details
- Pros: Single host page (panel stays on Application/Details).
- Cons: Surprising UX (applicant expects to see their response). Rejected.

## Decision

- **Reviewer wayfinding: Approach B.** Two peer sub-tabs on `/Review` — **Initial Review Queue** (existing content) and **Signing Inbox** (existing `/Review/SigningInbox` content). No pending-count badge; deferred until inbox volume makes counts valuable. Authorization per tab is unchanged from the underlying routes.

- **Applicant wayfinding: Approach F.** Embed the Funding Agreement signing panel on `/ApplicantResponse/Index/{id}` in addition to its existing home on `/Application/Details/{id}`. Both embeds share a single source of truth (same partial + view-model). Above the response-page embed, a contextual status banner:
  - State `ResponseFinalized` + Funding Agreement exists → *"Your funding agreement is ready to sign below."*
  - State `AgreementExecuted` → *"Your funding agreement has been executed."*
  - All other states → banner hidden.

- **Docs sync:** Update `specs/006-digital-signatures/quickstart.md` so journey prose names concrete entry points per role (applicants use the response page; reviewers use `/Review` → Signing Inbox tab). No journey steps change.

- **No new authorization surface.** The embedded panel on the response page inherits role-based gating from the existing 006 partial. The two new sub-tabs reuse existing route-level authorization. No new controller actions return signing-stage data as part of this feature.

- **Constitution III alignment:** the initial spec missed an explicit Playwright E2E mandate for the new wayfinding journeys. Spex:review-spec flagged it; SC-007 was added in iteration 1 to require E2E coverage for the three wayfinding journeys (reviewer → Signing Inbox tab; applicant → panel + banner on response page; existing `/Application/Details/{id}` embed continues passing).

Spec review passed with SOUND status after one iteration (review dimensions all 5/5, constitution fully aligned, no open `[NEEDS CLARIFICATION]` markers).

## Open Threads

- Should pending-count badges ship with this feature, or remain a future polish item once signing-inbox volume makes them valuable?
- Should `AppealOpen` get its own banner string on the response page, or remain silent as currently specified?
- Is the two-click threshold (SC-001) the right long-term bar, or will future volume eventually justify a top-level nav entry for Signing (Approach A) alongside the sub-tabs?
- The 006 signing panel partial is assumed to be shape-clean enough to embed on a second host page without modification. If implementation discovers it is not, reshaping it is within scope for 007 but the reshape itself may expose further 006 refactors worth tracking.
