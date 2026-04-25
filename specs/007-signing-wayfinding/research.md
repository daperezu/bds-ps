# Research: Signing Stage Wayfinding

**Feature:** 007-signing-wayfinding
**Phase:** 0 — Outline & Research
**Date:** 2026-04-23

This document resolves the six planning decisions for 007. All six are view-layer choices; none introduce new dependencies, new storage, or new architectural patterns.

---

## R-001 — Tab navigation strategy on `/Review`

**Decision:** Server-rendered shared partial `_ReviewTabs.cshtml`, included at the top of both `Review/Index.cshtml` (initial queue) and `Review/SigningInbox.cshtml` (signing inbox). Each view sets `ViewData["ActiveTab"]` to "Initial" or "Signing"; the partial renders a Bootstrap `nav nav-pills` structure with two `<a>` tags pointing at the existing routes `/Review` and `/Review/SigningInbox`. Clicking a tab is a full GET to the existing route — no client-side tab JavaScript, no async swapping.

**Rationale:**

- Zero new routes, zero new controller actions (FR-007 compliance).
- Zero client-side state to reason about (each tab is a self-contained page).
- The two existing views already have their own controllers and view-models; introducing a single multi-tab view would require merging two unrelated view-models into one and complicating the authorization story.
- Bootstrap 5 `nav-pills` is already in use elsewhere in the app (e.g., the bootstrap CSS and JS are loaded in `_Layout.cshtml`); adding a nav-pills bar is a trivial CSS/markup exercise.
- Each tab click triggers a full page load (~1 extra HTTP round-trip on tab switch), which is acceptable given the low-frequency of tab-switching on a back-office review page.

**Alternatives considered:**

- **Client-side tabs (Bootstrap `nav-tabs` with `data-bs-toggle="tab"` and both tab bodies present in the initial DOM):** rejected because each tab body would need to be rendered server-side on every `/Review` or `/Review/SigningInbox` visit, and the non-active tab's data (e.g., 25 signing-inbox rows) would be loaded for nothing on every initial-queue visit. Costs a redundant DB query on every tab visit.
- **Async-fetch tabs (single `/Review` view with two tabs whose bodies are fetched via AJAX):** rejected as over-engineering — the two existing routes already serve the content; adding an AJAX layer on top adds a moving part without value.
- **Single merged controller action (`/Review?tab=signing`):** rejected because it would require merging two unrelated view-model shapes (`ReviewQueueViewModel` vs `IReadOnlyList<SigningInboxRowViewModel>`) and duplicating the paging logic.

**Open question:** The initial queue does not support pending-count badges in this feature; see spec's Out of Scope. If the decision to add badges later warrants a single consolidated controller, the shared partial approach is still the correct starting point — badges can be added to the nav-pills labels without structural change.

---

## R-002 — Panel embed strategy on `/ApplicantResponse/Index/{id}`

**Decision:** Reuse the async-fetch pattern already established in `src/FundingPlatform.Web/Views/Application/Details.cshtml:159-177`. The response-page view renders:

```html
<div id="funding-agreement-panel-container">
    <div data-async-panel-url="@Url.RouteUrl(new { controller = "FundingAgreement", action = "Panel", applicationId = Model.ApplicationId })">
        Loading funding agreement panel...
    </div>
</div>
```

…followed by the same inline `fetch(...)` IIFE that swaps the inner HTML on 200 response and silently drops on error (non-disclosing). **No new controller action** is introduced (FR-007 compliance); the existing `/FundingAgreement/Panel/{applicationId}` route is the single source of truth.

**Rationale:**

- FR-004 mandates single source of truth for the signing panel. Reusing the same endpoint + same fetch pattern guarantees byte-identical rendering between `/Application/Details/{id}` and `/ApplicantResponse/Index/{id}` for the same user + same state (SC-005).
- The Application/Details fetch pattern is already under test in existing E2E coverage; re-using it propagates that confidence.
- The IIFE is small, self-contained, and degrades silently on fetch failure — satisfying the edge case "embedded panel load fails on the response page" in spec §Edge Cases.

**Alternatives considered:**

- **Server-side `@await Html.PartialAsync(...)` against the partial path directly:** rejected because the partial requires a constructed `SigningStagePanelViewModel`, which is only built inside `FundingAgreementController.Panel`. Calling the partial directly from `ApplicantResponseController` would require duplicating view-model construction logic, violating FR-004.
- **Extracting the view-model construction into a shared helper:** rejected for this feature — it's a legitimate refactor but orthogonal to the wayfinding goal, and the async-fetch approach removes the need for it.
- **Blazor component / HTMX swap:** rejected — both would introduce new dependencies and patterns for zero incremental value over vanilla fetch.

---

## R-003 — Contextual banner rendering and data dependency

**Decision:** Render the banner server-side in `ApplicantResponse/Index.cshtml` based on two view-model fields: `Model.State` (existing) and `Model.HasFundingAgreement` (new, additive). Banner visibility logic lives in the view (a single Razor `@if` chain) using enum comparisons. No client-side JavaScript.

The new DTO/view-model field flows:

```
ApplicantResponseService.GetResponseAsync
    ↓ (set HasFundingAgreement via the already-loaded application's FundingAgreements collection)
ApplicantResponseDto { ..., bool HasFundingAgreement }
    ↓ (pass-through in BuildViewModel)
ApplicantResponseViewModel { ..., bool HasFundingAgreement }
    ↓ (used in the Razor @if)
Banner rendering
```

The banner text mapping is identical to FR-003:

| State                 | HasFundingAgreement | Banner rendered |
|-----------------------|---------------------|-----------------|
| `ResponseFinalized`   | `true`              | *"Your funding agreement is ready to sign below."* |
| `ResponseFinalized`   | `false`             | (hidden) |
| `AgreementExecuted`   | *any*               | *"Your funding agreement has been executed."* |
| any other state       | *any*               | (hidden) |

**Rationale:**

- The banner is a static, state-driven UI affordance. Server-side rendering is simplest and removes any flicker on page load.
- Surfacing `HasFundingAgreement` on the DTO avoids a second DB query in the controller — the application aggregate is already loaded in `ApplicantResponseService.GetResponseAsync`; a `.Any()` on the agreement collection is O(1) and stays within the existing EF query graph.
- No new repository method, no new service — additive field only.

**Alternatives considered:**

- **Client-side banner driven by the panel's async response:** rejected — the banner needs to render before the panel fetch completes to avoid flicker, and the `HasFundingAgreement` signal would require either JSON deserialization of the panel's HTML response or a new endpoint.
- **Derive `HasFundingAgreement` from `State == AgreementExecuted` alone:** rejected — the `ResponseFinalized` banner must distinguish "agreement generated" from "agreement not generated" cases (hidden when not generated). State alone is insufficient.
- **Store `HasFundingAgreement` as a column on Applications:** rejected — this is derivable from the agreement collection; adding a denormalized column violates Constitution IV (schema-first), Constitution VI (simplicity), and creates a drift risk.

---

## R-004 — Test strategy and coverage

**Decision:** One new E2E test class `tests/FundingPlatform.Tests.E2E/Tests/SigningWayfindingTests.cs` with three `[Test]` methods mirroring SC-007's three sub-bullets:

1. `ReviewerReachesSigningInboxInTwoClicks` — logs in as reviewer, navigates to `/`, clicks the **Review** navbar link, asserts both tabs are visible with "Initial Review Queue" active, clicks the **Signing Inbox** tab, asserts URL is `/Review/SigningInbox` and the tab is now active.
2. `ApplicantSeesBannerAndPanelOnResponsePage` — seeds an application in `ResponseFinalized` with a generated Funding Agreement, logs in as the applicant, visits `/ApplicantResponse/Index/{id}`, asserts the "ready to sign below" banner is visible and the signing panel has loaded with the Download button. Then seeds the application to `AgreementExecuted`, refreshes, asserts the "has been executed" banner appears and the signed-PDF download link is present.
3. `ApplicationDetailsEmbedStillRenders` — visits `/Application/Details/{id}` for the same seeded application and asserts the signing panel still loads and exposes the same actions — guards against regression (FR-005, SC-006).

**Page object updates (in lieu of new page objects):**

- `ReviewQueuePage.cs`: add `ClickSigningInboxTab()`, `IsSigningInboxTabActive()`, `IsInitialQueueTabActive()`.
- `ApplicantResponsePage.cs`: add `IsReadyToSignBannerVisible()`, `IsAgreementExecutedBannerVisible()`, and a `SigningPanel` getter that returns the existing `SigningStagePanelPage` scoped to the embedded container on the response page.

**Rationale:**

- One test class per feature (convention established by 006's `DigitalSignatureTests.cs`).
- Three tests exactly cover SC-007 sub-bullets (a)(b)(c) — no more, no less.
- Reuses the existing `SigningStagePanelPage` and `SigningReviewInboxPage` from 006 — zero new page objects for this feature.
- Existing unit + integration tests for signing behavior (006) are not touched; SC-006 is satisfied by the regression absence in the existing test run.

**Alternatives considered:**

- **Parameterized test over the two applicant banner states:** fine but adds test-framework complexity for negligible benefit at three tests total. Kept as two assertions within a single test.
- **Separate test for US3 (quickstart sync):** US3 is a docs change; verified manually (the acceptance scenarios themselves are about reading prose). An automated "quickstart walker" agent is out of scope.

---

## R-005 — Quickstart prose surgery target list

**Decision:** The following sentences in `specs/006-digital-signatures/quickstart.md` are the exact edit targets. No other lines change.

| Location | Before (current wording) | After (FR-006 compliant) |
|---|---|---|
| Journey 1, step 2 | "Navigate to the application's detail page." | "Navigate to `/ApplicantResponse/Index/{id}` for the application (the page you land on after accepting reviewer decisions). The Funding Agreement panel renders below the response-item form." |
| Journey 1, step 7 | "Go to **Review → Signing Inbox**. You should see one row for the application." | "In the main navigation click **Review**, then click the **Signing Inbox** sub-tab. You should see one row for the application." |
| Journey 1, step 8 | "Click through to the application detail page. The panel shows the pending upload and two buttons: **Approve** and **Reject**." | "Click through to the application (row link). The signing panel shows the pending upload and two buttons: **Approve** and **Reject**." (remove "detail page" ambiguity) |
| Journey 2, step 2 | "Log in as `reviewer@demo.com` and navigate to the application detail page." | "Log in as `reviewer@demo.com`, click **Review** in the main navigation, click the **Signing Inbox** sub-tab, and open the application from the row." |
| Journey 4, step 5 (version-mismatch sub-path) | "As applicant, download the agreement (call this V1)." | "As applicant, open `/ApplicantResponse/Index/{id}` and download the agreement from the embedded panel (call this V1)." |

No new steps are introduced; no steps are removed. The edit is ~5 sentence-level replacements over the four journeys.

**Rationale:**

- Preserves the canonical four-journey structure of SC-008 exactly.
- Names concrete entry points in line with the new self-navigable UI.
- Removes "the application's detail page" wherever the phrase is ambiguous between `/Application/Details/{id}` (reviewer-oriented) and `/ApplicantResponse/Index/{id}` (applicant-oriented).

---

## R-006 — Authorization reachability of US2 Scenario 4

**Decision:** Do not modify authorization on `ApplicantResponseController.Index` for this feature. The existing action-level `[Authorize(Roles = "Applicant")]` gate means reviewers and admins receive a 403 before the view renders, which trivially satisfies US2 Scenario 4's invariant ("no role escalation, no new actions").

**Rationale:**

- US2 Scenario 4 in the spec is framed as a negative invariant: *"the embedded panel exposes only the actions the existing panel already exposes to that user + state combination — no role escalation, no new actions."* A reviewer seeing a 403 at the controller gate trivially satisfies this (zero actions exposed ≤ any other count).
- FR-007 explicitly forbids new authorization surfaces, and weakening the existing `[Authorize(Roles = "Applicant")]` gate to allow reviewers/admins onto the response page would be a *new* authorization surface — exactly what FR-007 prohibits.
- The reviewer and admin already have a functioning signing surface via `/Application/Details/{id}` (for any application) and `/Review/SigningInbox` (for pending items). They do not need the applicant's response page.

**Alternatives considered:**

- **Relax the gate to `[Authorize]`:** rejected — violates FR-007.
- **Remove US2 Scenario 4 from the spec as unreachable:** not needed; the scenario is satisfied by the existing 403 semantics. No spec edit required.

**Open question (deferred):** Should a future feature add a "reviewer-read-only" surface on the response page (so reviewers can see applicant decisions in context)? This was an open thread from spec 004 and remains out of scope here.

---

## Summary of decisions

| # | Topic | Decision |
|---|-------|----------|
| R-001 | Tabs | Server-rendered shared partial, two routes (`/Review`, `/Review/SigningInbox`) |
| R-002 | Panel embed | Async-fetch pattern reused from `Application/Details.cshtml`; no new endpoint |
| R-003 | Banner | Server-side Razor `@if` against `State` + `HasFundingAgreement`; new bool on DTO/view-model |
| R-004 | Tests | One new E2E class, three tests (SC-007 (a)(b)(c)); page object extensions only |
| R-005 | Quickstart | Five sentence-level replacements in 006 `quickstart.md`; no journey structure change |
| R-006 | Authz | No change; existing `[Authorize(Roles = "Applicant")]` satisfies US2 Scenario 4 trivially |

All six are resolved. No open `[NEEDS CLARIFICATION]` markers in the spec. Ready for Phase 1.
