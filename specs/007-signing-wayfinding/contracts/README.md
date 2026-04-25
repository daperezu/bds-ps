# Contracts: Signing Stage Wayfinding

**Feature:** 007-signing-wayfinding
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-23

This feature introduces **no new MVC routes, no new controller actions, no new API endpoints**. FR-007 explicitly forbids them. The contracts below document the view-layer surfaces and the one additive DTO/view-model field.

---

## 1. Existing routes — reused as-is

| Route | HTTP | Controller.Action | Authorization | Contract |
|---|---|---|---|---|
| `/Review` | GET | `ReviewController.Index` | `Reviewer, Admin` | **Contract change:** view now renders the `_ReviewTabs` partial above its queue table, with `ViewData["ActiveTab"] = "Initial"`. Response payload structure is unchanged (`ReviewQueueViewModel`). Paging query params (`?page=N`) unchanged. |
| `/Review/SigningInbox` | GET | `ReviewController.SigningInbox` | `Reviewer, Admin` | **Contract change:** view now renders the `_ReviewTabs` partial above its rows, with `ViewData["ActiveTab"] = "Signing"`. Response payload structure is unchanged (`IReadOnlyList<SigningInboxRowViewModel>`). Paging query params unchanged. |
| `/ApplicantResponse/Index/{id}` | GET | `ApplicantResponseController.Index` | `Applicant` (unchanged) | **Contract change:** view renders (a) a state-driven banner above the response-item form; (b) an async-fetch placeholder `<div>` below the form that loads the signing panel from `/FundingAgreement/Panel/{id}`. Response payload: `ApplicantResponseViewModel` gains a new `HasFundingAgreement` bool. |
| `/FundingAgreement/Panel/{applicationId}` | GET | `FundingAgreementController.Panel` | per 006 panel partial rules | **Unchanged** — sole server source of signing-panel content (FR-004, FR-007). |
| `/Application/Details/{id}` | GET | `ApplicationController.Details` | (existing) | **Unchanged** — continues to embed the signing panel via its existing async fetch (FR-005). |

---

## 2. Shared view partial contract

### `_ReviewTabs.cshtml`

**Location:** `src/FundingPlatform.Web/Views/Review/_ReviewTabs.cshtml`

**Purpose:** Render the two-tab nav pills for the Review landing area.

**Inputs (via ViewData):**

| Key | Type | Values | Required |
|---|---|---|---|
| `ActiveTab` | `string` | `"Initial"` or `"Signing"` | Yes — if missing, neither tab shows as active (defensive; not expected in normal flow). |

**Rendered HTML shape (stable contract — E2E tests depend on these selectors):**

```html
<ul class="nav nav-pills mb-3" role="tablist" data-testid="review-tabs">
  <li class="nav-item">
    <a class="nav-link [active]"
       data-testid="review-tab-initial"
       href="@Url.Action("Index", "Review")">Initial Review Queue</a>
  </li>
  <li class="nav-item">
    <a class="nav-link [active]"
       data-testid="review-tab-signing"
       href="@Url.Action("SigningInbox", "Review")">Signing Inbox</a>
  </li>
</ul>
```

The `active` class is applied to the matching `<a>` based on `ViewData["ActiveTab"]`. `data-testid` attributes are the stable hooks for Playwright page objects.

**Accessibility:** standard Bootstrap `nav-pills` semantics; each link is a full-page navigation, not a tab-switch, so no `aria-selected` gymnastics needed.

---

## 3. View-model contract changes

### `ApplicantResponseViewModel`

**Location:** `src/FundingPlatform.Web/ViewModels/ApplicantResponseViewModel.cs`

**Added field:**

```csharp
public bool HasFundingAgreement { get; set; }
```

**Default:** `false`. Safe — causes the banner to hide when the flag cannot be computed.

**Consumer:** `Views/ApplicantResponse/Index.cshtml` uses `HasFundingAgreement` together with `State` to decide banner rendering.

### `ApplicantResponseDto`

**Location:** `src/FundingPlatform.Application/DTOs/ApplicantResponseDto.cs`

**Added positional record parameter** (appended at the end so existing callers keep compiling in order):

```csharp
public record ApplicantResponseDto(
    int ApplicationId,
    int? CycleNumber,
    DateTime? SubmittedAt,
    bool IsSubmitted,
    ApplicationState State,
    List<ItemResponseDto> Items,
    bool HasFundingAgreement);
```

**Populated by:** `ApplicantResponseService.GetResponseAsync`, via `.Any()` on the already-loaded application's agreement collection.

---

## 4. Banner rendering contract on `/ApplicantResponse/Index/{id}`

**Location:** `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml` (above the existing response-item form and below the existing state-badge card).

**Visibility truth table:**

| `Model.State` | `Model.HasFundingAgreement` | Rendered element | Rendered copy |
|---|---|---|---|
| `ResponseFinalized` | `true` | `<div class="alert alert-info" data-testid="signing-banner-ready">…</div>` | *Your funding agreement is ready to sign below.* |
| `ResponseFinalized` | `false` | *(hidden — no element emitted)* | — |
| `AgreementExecuted` | *any* | `<div class="alert alert-success" data-testid="signing-banner-executed">…</div>` | *Your funding agreement has been executed.* |
| *any other state* | *any* | *(hidden)* | — |

**Stable test hooks:** `data-testid="signing-banner-ready"` and `data-testid="signing-banner-executed"` — consumed by `ApplicantResponsePage.IsReadyToSignBannerVisible()` / `IsAgreementExecutedBannerVisible()`.

---

## 5. Embedded-panel placeholder contract on `/ApplicantResponse/Index/{id}`

**Location:** `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml` (below the response-item form and appeal controls).

**Rendered HTML shape (same as `Views/Application/Details.cshtml:159-177`):**

```html
<div class="mt-3" id="funding-agreement-panel-container">
    <div data-async-panel-url="@Url.RouteUrl(new { controller = "FundingAgreement", action = "Panel", applicationId = Model.ApplicationId })">
        Loading funding agreement panel...
    </div>
</div>

<script>
(function () {
    var container = document.querySelector('#funding-agreement-panel-container > div[data-async-panel-url]');
    if (!container) return;
    var url = container.getAttribute('data-async-panel-url');
    fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'text/html' } })
        .then(function (r) { return r.ok ? r.text() : ''; })
        .then(function (html) {
            var target = document.getElementById('funding-agreement-panel-container');
            if (target) target.innerHTML = html;
        })
        .catch(function () { /* non-disclosing: silently drop on error */ });
})();
</script>
```

**Contract:** byte-identical to `Views/Application/Details.cshtml:159-177` — the implementer MUST copy the two blocks verbatim. Divergence between the two host pages' embed mechanics is a defect (FR-004, SC-005).

---

## 6. Quickstart prose contract

**Location:** `specs/006-digital-signatures/quickstart.md`

**Edit list:** exactly the five sentence-level replacements enumerated in `research.md §R-005`. No other lines change. No journey gains or loses steps (FR-006 invariant).

---

## 7. Authorization matrix (unchanged)

| Surface | Role required | Notes |
|---|---|---|
| `/Review` with either tab | `Reviewer` or `Admin` | Class-level `[Authorize(Roles = "Reviewer,Admin")]` on `ReviewController` covers both tabs uniformly. No new role check introduced. |
| `/ApplicantResponse/Index/{id}` | `Applicant` | Action-level `[Authorize(Roles = "Applicant")]` unchanged; reviewers/admins receive 403 (R-006). |
| `/FundingAgreement/Panel/{applicationId}` | per 006 | Unchanged; serves as the sole signing-panel source for both host pages. |

FR-007 holds: no new authorization surfaces are introduced.

---

## 8. Summary

| Contract | Change | Kind |
|---|---|---|
| MVC routes | — | No change |
| Controller actions | — | No change |
| Request/response payloads | `ApplicantResponseViewModel` gains one bool; `ApplicantResponseDto` gains one bool | Additive, backward-compatible |
| View partials | `_ReviewTabs.cshtml` introduced | New, shared between two existing views |
| Banner HTML | New `alert` element under defined `data-testid` hooks | New, state-driven |
| Panel embed HTML | Placeholder `div` + IIFE copied from Application/Details | Reused, byte-identical |
| Authorization | — | No change |
| Quickstart prose | Five sentence-level edits | Docs-only |
