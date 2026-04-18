# Contracts — Funding Agreement Generation

This feature is a server-side MVC slice inside `FundingPlatform.Web`; it exposes no public API and no external machine-to-machine contract. What it *does* expose is a small set of authenticated MVC routes consumed by the application's own server-rendered views. Those routes are documented here so the controller, view-model, and E2E test expectations line up.

Authentication: every route requires an authenticated session. Authorization: the routes enforce the role-and-ownership rules defined in `../data-model.md` section 2 and `../spec.md` FR-016 through FR-020.

All "not found / not authorized" responses are **non-disclosing** (FR-019): identical HTTP status (404) and identical response body are returned whether the application doesn't exist, the agreement doesn't exist, or the caller is not authorized to see either.

---

## Route 1 — View agreement state on the application page (read)

**Route**: `GET /Applications/{applicationId}/FundingAgreement/Panel`
**Consumed by**: the Application Detail page (partial view render).
**Purpose**: render the funding-agreement panel on the application detail page, including: whether an agreement exists, download link (if any), "Generate" or "Regenerate" action availability, and explanatory messages for disabled actions.

### Authorization

- `Applicant` who owns `applicationId`: download visible when agreement exists; Generate/Regenerate never visible.
- `Administrator`: download visible when agreement exists; Generate or Regenerate visible subject to precondition evaluation.
- `Reviewer` assigned to the application: same as Administrator.
- Anyone else: 404, empty body (see non-disclosure rule).

### Response view model (shape)

```text
FundingAgreementPanelViewModel {
  applicationId: int
  agreementExists: bool
  agreementDownloadUrl: string?      // null when agreementExists == false
  canGenerate: bool                  // driver of "Generate agreement" button
  canRegenerate: bool                // driver of "Regenerate" button
  disabledReason: string?            // user-presentable message when canGenerate == false AND agreementExists == false
  generatedAtUtc: DateTime?          // shown to the viewer when agreement exists
  generatedByDisplayName: string?    // human-readable name for the latest generator
}
```

The controller constructs this model via `Application.CanUserAccessFundingAgreement(...)`, `Application.CanGenerateFundingAgreement(out var errors)`, and the `Application.FundingAgreement` navigation.

---

## Route 2 — Generate or regenerate the agreement (write)

**Route**: `POST /Applications/{applicationId}/FundingAgreement/Generate`
**Consumed by**: the "Generate agreement" and "Regenerate" buttons on the application detail page. The UI distinguishes between the two actions purely by prior state — the server treats both identically: "produce the current agreement for this application."

### Authorization

- `Administrator`: allowed.
- `Reviewer` assigned to the application: allowed.
- `Applicant`: 403 with a non-disclosing body (applicants never see the button, but a hand-crafted POST is rejected).
- Unauthorized: 404 per non-disclosure.

### Request body

Anti-forgery token (standard MVC). No user-supplied fields; the caller identifies the application via the route parameter and the caller via the authenticated session.

### Server behavior

1. Load the application with the navigations the PDF template needs.
2. Re-check `CanUserGenerateFundingAgreement(...)` and `CanGenerateFundingAgreement(out var errors)` — FR-004, FR-011.
3. On a failed precondition: return 400 (or re-render the panel with the error banner) and leave state untouched. FR-004, EH-001.
4. On success: render HTML, convert to PDF, save to storage, create or replace the `FundingAgreement` entity, commit transaction (R-009).
5. On renderer or storage failure: roll back, delete any partially-written file, log the failure, return an error view with a generic retry message. FR-021, EH-004, EH-005.
6. On concurrency conflict (`DbUpdateConcurrencyException`): return 409 Conflict with a "reload the page" message. FR-022, EH-006.

### Responses

- `302 Found → /Applications/{applicationId}` on success (Post-Redirect-Get; the application page re-queries the panel and now shows the download link).
- `400 Bad Request` on precondition failure, re-rendering the application page with the error.
- `409 Conflict` on concurrency conflict, re-rendering with a reload prompt.
- `500 Internal Server Error` only on unexpected renderer failure; the error view masks details (FR-019 intent extends to error surfaces).

### Idempotency

The endpoint is **not idempotent**: two successful POSTs produce two distinct generation events (each with its own `GeneratedAtUtc`) and may replace the stored file. This is intentional per spec FR-013; the UI's confirmation dialog on the Regenerate action is the guardrail against accidental double-submits.

---

## Route 3 — Download the agreement (read, binary)

**Route**: `GET /Applications/{applicationId}/FundingAgreement/Download`
**Consumed by**: the download link in the application detail page.
**Purpose**: stream the current agreement PDF to the authenticated, authorized caller.

### Authorization

- `Applicant` who owns the application: allowed.
- `Administrator`: allowed.
- `Reviewer` assigned to the application: allowed.
- Anyone else, or application has no agreement: 404 per non-disclosure. FR-019.

### Response

- `200 OK` with `Content-Type: application/pdf` and `Content-Disposition: attachment; filename="FundingAgreement-{applicationNumber}.pdf"`; body is the PDF bytes streamed from `IFileStorageService.GetFileAsync(storagePath)`.
- `404 Not Found` in every unauthorized / missing case.

### Caching headers

- `Cache-Control: private, no-cache` — agreements can be regenerated, and stale client caches would surface the old version. FR-014, EC-008.

### Download/regeneration race (EC-008)

In-flight downloads complete against the bytes they opened; a regeneration that starts during a download does not corrupt the in-flight response. The compensating delete in the generation flow (R-009) happens after the new file is written and the DB transaction commits, which is after the download stream has handed its stream to the HTTP response.

---

## Contract tests (E2E)

The three routes above are exercised by Playwright end-to-end tests living in `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs`. The tests drive the routes through the normal web UI (click "Generate agreement", click the download link, etc.) rather than calling the routes directly. Direct HTTP-level contract tests are covered by integration tests in `tests/FundingPlatform.Tests.Integration/FundingAgreementEndpointsTests.cs`, which use `WebApplicationFactory` and focus on authorization edges (wrong-role POSTs, non-disclosure responses, concurrency-conflict behavior) that are awkward to exercise through the browser.
