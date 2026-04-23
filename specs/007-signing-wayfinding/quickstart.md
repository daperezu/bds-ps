# Quickstart: Signing Stage Wayfinding

**Feature:** 007-signing-wayfinding
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-23

Manual walkthrough of the three wayfinding journeys mandated by SC-007. Each journey is independently runnable and corresponds to one `[Test]` in `tests/FundingPlatform.Tests.E2E/Tests/SigningWayfindingTests.cs`.

> **Relationship to 006 quickstart:** `specs/006-digital-signatures/quickstart.md` walks the four signing *behavior* journeys (SC-008). This feature's quickstart walks the three wayfinding *discovery* journeys (SC-007). After 007 ships, the 006 quickstart prose is updated (per R-005) so all of its navigation steps match what this feature builds.

---

## Prerequisites

- Local dev stack running via `.NET Aspire`:
  ```bash
  dotnet run --project src/FundingPlatform.AppHost
  ```
- Database auto-deployed on first run (the AppHost now publishes the dacpac automatically — see AppHost.cs; if you're running an old branch without that wiring, publish manually via `sqlpackage`).
- Seeded demo users:
  - `applicant@demo.com` — role: `Applicant`
  - `reviewer@demo.com` — role: `Reviewer`
  - `admin@demo.com` — role: `Admin`
- An application belonging to `applicant@demo.com` at **`ResponseFinalized`** with a generated Funding Agreement. Easiest path: complete 006 quickstart up to the "Funding Agreement generated" checkpoint.

---

## Journey 1 — Reviewer reaches the Signing Inbox via tabs (SC-001, SC-007a)

**Persona:** reviewer.

1. Log in as `reviewer@demo.com`.
2. On the home page, click **Review** in the main navigation.
3. **Expected:** the Review landing page shows two pill-style tabs at the top — **Initial Review Queue** (active) and **Signing Inbox** — above the existing review queue table.
4. Click the **Signing Inbox** tab.
5. **Expected:** the page URL changes to `/Review/SigningInbox`; the tabs remain visible with **Signing Inbox** now active; the paginated signing inbox rows render below (content identical to today's bare-URL visit).
6. Click **Initial Review Queue** tab.
7. **Expected:** URL returns to `/Review`; **Initial Review Queue** is active again; the review queue table is visible.

**Outcome:** total-clicks-from-home to reach the Signing Inbox is **2** (Review → Signing Inbox), meeting SC-001.

**Automation:** `SigningWayfindingTests.ReviewerReachesSigningInboxInTwoClicks`.

---

## Journey 2 — Applicant sees banner and signing panel on the response page (SC-002, SC-003, SC-007b)

**Persona:** applicant.

1. Seed an application as the applicant at state `ResponseFinalized` with a generated Funding Agreement.
2. Log in as `applicant@demo.com`.
3. Navigate to `/ApplicantResponse/Index/{id}` (either by following a link from the application list or by visiting the URL directly — both are valid entry points; direct URL is used here for test determinism).
4. **Expected:**
   - The state badge still reads **ResponseFinalized** at the top of the page.
   - A single-line banner is visible **above the response-item form**: *"Your funding agreement is ready to sign below."*
   - The existing response-item form renders unchanged between the banner and the signing panel.
   - **Below** the form, the Funding Agreement signing panel has loaded asynchronously and shows the applicant actions — **Download agreement** button and an upload form.
5. Click **Download agreement**; download the PDF, stamp a signature externally (any PDF tool), save as `signed.pdf`.
6. Upload `signed.pdf` via the upload form on the embedded panel.
7. **Expected:** the panel reloads to show a "Pending reviewer decision" badge; the banner remains visible (state is still `ResponseFinalized` until a reviewer approves).
8. Out-of-band: as reviewer, log in, approve the pending upload (via Journey 1 to reach the inbox).
9. As applicant, refresh `/ApplicantResponse/Index/{id}`.
10. **Expected:**
    - State badge now reads **AgreementExecuted**.
    - The banner text flips to *"Your funding agreement has been executed."* with success styling.
    - The embedded panel exposes a **Download signed agreement** link for the approved counterpart.

**Outcome:** the applicant never navigates away from `/ApplicantResponse/Index/{id}` to complete the signing flow. SC-002 and SC-003 met.

**Automation:** `SigningWayfindingTests.ApplicantSeesBannerAndPanelOnResponsePage` (two assertion blocks — one per state).

---

## Journey 3 — `/Application/Details/{id}` embed continues to render for reviewer (SC-006, SC-007c)

**Persona:** reviewer.

1. Seed the same application used in Journey 2 (any state in which the agreement exists).
2. Log in as `reviewer@demo.com`.
3. Navigate to `/Application/Details/{id}`.
4. **Expected:**
   - The page renders its existing application overview and items table.
   - The "Loading funding agreement panel..." placeholder at the bottom swaps to the fully-rendered signing panel after the async fetch completes (same as before 007).
   - All existing reviewer actions on the panel (Approve / Reject / regeneration controls per 006) remain visible and functional.

**Outcome:** no regression in the Application/Details embed after 007 lands. FR-005 and SC-006 met.

**Automation:** `SigningWayfindingTests.ApplicationDetailsEmbedStillRenders`.

---

## Journey 4 (manual) — Quickstart walk verification (SC-004, US3)

1. Open `specs/006-digital-signatures/quickstart.md` in your editor.
2. Walk Journey 1, Journey 2, and the Journey 4 version-mismatch sub-path end-to-end in the browser, following only the quickstart's own prose.
3. **Expected:** every navigation step in the prose names a concrete entry point (`/ApplicantResponse/Index/{id}`, `/Review` → **Signing Inbox** tab) — no "the application's detail page" ambiguity remains.
4. **Expected:** the four journeys still have the same step count as before 007 (no steps gained, no steps lost). Only wayfinding prose changes.

**Outcome:** a first-time QA walker completes all four 006 journeys following only the updated quickstart prose. SC-004 met.

**Automation:** manual. Not covered by Playwright; verified during 007 PR review.

---

## Operational notes

- **Nothing else changes.** Signing-stage behavior (upload intake, version-mismatch logic, regeneration lockdown, audit trail) is 100% inherited from 006.
- **Pending-count badges are deliberately absent** (spec §Out of Scope). If they ship later, the `_ReviewTabs.cshtml` partial is the only structural edit point.
- **Banner visibility logic is server-side** — the banner appears on page load, no flicker, no JS dependency.
- **The embedded panel still fails silently** on transient errors (same non-disclosing IIFE as Application/Details). The response-item form and banner remain readable if the panel fetch fails.

---

## Where each journey is covered automatically

| Journey | E2E test | Source |
|---|---|---|
| 1 (Reviewer tabs) | `SigningWayfindingTests.ReviewerReachesSigningInboxInTwoClicks` | SC-007(a) |
| 2 (Applicant banner + panel) | `SigningWayfindingTests.ApplicantSeesBannerAndPanelOnResponsePage` | SC-007(b) |
| 3 (Application/Details no regression) | `SigningWayfindingTests.ApplicationDetailsEmbedStillRenders` | SC-007(c), SC-006 |
| 4 (Quickstart walk) | *(manual only)* | SC-004, US3 |

Running the full suite: `dotnet test`. Running just e2e: `dotnet test tests/FundingPlatform.Tests.E2E`.
