# Quickstart: Funding Agreement Document Generation

**Feature**: 005-funding-agreement-generation
**Audience**: Developers validating the feature end-to-end in a local Aspire-orchestrated environment.

This walkthrough verifies the golden path and the three most important blocked paths against a running local stack. It assumes specs 001, 002, and 004 are already implemented and running on `main`.

---

## Prerequisites

- Repository checked out on branch `005-funding-agreement-generation`.
- `.NET 10.0` SDK, Docker Desktop (for the Aspire-managed SQL Server container), and the Playwright browser bundle installed.
- `appsettings.Development.json` contains:
  - `Syncfusion:LicenseKey` (a valid community or commercial Syncfusion license).
  - `FundingAgreement:LocaleCode` — defaults to `es-CO`; override to validate configurability.
  - `FundingAgreement:CurrencyIsoCode` — defaults to `COP`.
  - `FundingAgreement:Funder:*` — LegalName, TaxId, Address, ContactEmail, ContactPhone.
- Seed data: at least one applicant, one administrator, and one reviewer account exist (created by the existing identity seed scripts).

---

## 1. Launch the Aspire stack

```bash
cd src/FundingPlatform.AppHost
dotnet run
```

Open the Aspire dashboard, confirm SQL Server and the Web project are healthy, then open the Web URL.

Expected: the Web project starts without the "Syncfusion license missing" fail-fast error (NFR-008 / R-001). If it does fail, re-check `Syncfusion:LicenseKey` in configuration.

---

## 2. Create a fully-resolved application (pre-feature setup)

Drive the UI (or use the seeded-data script, if available) to:

1. Sign in as the applicant, create a draft application with two items, each with the minimum required quotations, submit it.
2. Sign in as a reviewer, review and approve both items.
3. Sign in as the applicant, respond to the review by accepting both items. Application state is now `ResponseFinalized` with two accepted items and no open appeal.

Verify in the Aspire dashboard logs or via SQL inspection that:
- `Applications.State = 5` (`ResponseFinalized`).
- The latest `ApplicantResponses.ItemResponses` row for each item has `Decision = 0` (`Accept`).
- `Appeals` for this application is empty (or any row has `Status = Resolved`).

---

## 3. Golden path — Administrator generates the agreement (User Story 1)

### Steps

1. Sign in as an administrator.
2. Navigate to the application from step 2 via the admin applications list.
3. Scroll to the **Funding Agreement** panel on the application detail page.
4. Confirm: panel shows "No agreement generated yet" and a **Generate agreement** button (enabled).
5. Click **Generate agreement**. Wait up to 3 seconds.

### Expected

- The page reloads (Post-Redirect-Get per contract route 2).
- The panel now shows a **Download** link, plus metadata: "Generated on &lt;UTC timestamp&gt; by &lt;admin display name&gt;" and a **Regenerate** button.
- Browser network log: initial POST → 302 → GET to `/Applications/{id}`.

### Validate the PDF

1. Click **Download**. PDF downloads as `FundingAgreement-{applicationNumber}.pdf`.
2. Open the file in at least two viewers (SC-003): Adobe Acrobat, Chrome PDF viewer, Firefox PDF viewer.
3. Verify the content (FR-007):
   - Agreement reference = the application number.
   - Funder fields match `FundingAgreement:Funder:*` configuration.
   - Applicant fields match the applicant's profile as of step 2.
   - Items table has **exactly two rows** (one per accepted item), each showing description, category, supplier of the accepted quotation, unit price, and line total.
   - Overall total equals the sum of the line totals, formatted with comma-decimal / period-thousands (e.g., `1.234.567,89`).
   - Terms & Conditions partial is present (either the legal-delivered copy or the placeholder + TODO banner if legal copy has not yet been incorporated).
   - Two empty signature blocks (funder and applicant) are present.

---

## 4. Golden path — Applicant downloads the agreement (User Story 2)

### Steps

1. Sign out, then sign in as the owning applicant.
2. Navigate to the application detail page.
3. Scroll to the **Funding Agreement** panel.

### Expected

- Panel shows a **Download** link with the same "Generated on / by" metadata.
- **Generate** and **Regenerate** buttons are **not** visible to the applicant (FR-003).
- Click **Download** — PDF downloads and matches what the administrator saw.

---

## 5. Blocked path A — Generate is unavailable when preconditions fail (User Story 4)

### 5.1 Review not closed

1. Create a second application, submit it, but **do not** review it. State is `Submitted`.
2. Sign in as administrator, navigate to the application detail page.
3. Funding Agreement panel shows: "Generate agreement" **not available**; the panel explains that review is not yet complete.

### 5.2 Active appeal

1. Use an application whose applicant has opened an appeal (state = `AppealOpen`).
2. Sign in as administrator, navigate to the application detail page.
3. Panel shows: "Generate agreement" **not available**; panel explains that an appeal is currently open.

### 5.3 All-rejected

1. Create an application where the applicant rejected every approved item. State is `ResponseFinalized` with zero accepted items.
2. Sign in as administrator, navigate to the application detail page.
3. Panel shows: **Generate agreement is disabled**, with the explanation "Nothing to fund: all items were rejected."

### 5.4 Server-side re-check (stale UI)

1. Stage the state: leave the admin on the application page while response is still `ResponseFinalized` with one accepted item.
2. In another session, have the applicant open an appeal on the rejected item. Application state transitions to `AppealOpen`.
3. Back in the admin's stale page, click **Generate agreement**.
4. Expected: the server rejects the POST with a 400 and a banner message explaining the failed precondition. No `FundingAgreement` row is created (FR-004, EH-001).

---

## 6. Regeneration (User Story 3)

### Steps

1. With the application from step 3, sign in as a reviewer who was assigned to that application's review.
2. Navigate to the application detail page.
3. Expected: **Download** visible, **Regenerate** button visible (R-007).
4. Click **Regenerate**, confirm in the dialog.
5. After the page reloads, click **Download** and verify the new PDF's `GeneratedAtUtc` is more recent and the `GeneratedByUserId` reflects the reviewer.
6. Verify in SQL that `FundingAgreements.RowVersion` has changed, `GeneratedAtUtc` has updated, and the prior file on disk is no longer present at the prior storage path (FR-013, FR-014).

### Cancellation

1. Click **Regenerate** again.
2. **Cancel** the confirmation dialog.
3. Verify the PDF, metadata, and storage path are unchanged.

---

## 7. Authorization (User Stories 5 and 6)

### 7.1 Reviewer from a different review

1. Sign in as a reviewer who did **not** review the application from step 3.
2. Attempt to navigate to `/Applications/{id}/FundingAgreement/Download` directly.
3. Expected: 404 (FR-019 non-disclosure). No indication of whether the agreement exists.

### 7.2 Different applicant

1. Sign in as an applicant who does **not** own the application.
2. Attempt the direct download route.
3. Expected: identical 404 response body and status.

### 7.3 Anonymous

1. Sign out.
2. Attempt the download route.
3. Expected: the usual authentication redirect (standard Identity behavior); if a token session is forged, still 404 with no distinguishing metadata.

---

## 8. Concurrency (FR-022 / EH-006)

### Simulated race (manual, coarse-grained)

1. Open two browser sessions: one administrator tab and one reviewer tab (both eligible to regenerate).
2. Both load the application detail page.
3. Click **Regenerate** in tab A and **Regenerate** in tab B as close together as possible.
4. Expected: one tab succeeds; the other receives a 409 (or the in-view equivalent error banner) asking the user to reload the page. The final `FundingAgreement.RowVersion` corresponds to exactly one of the two attempts.

---

## 9. Teardown

```bash
# In FundingPlatform.AppHost's terminal
Ctrl-C
```

Aspire tears down the containers. For a fully-clean slate, remove the Aspire-created volumes:

```bash
docker volume prune
```

---

## 10. Automated E2E counterparts

Each of the manual scenarios above is mirrored by a Playwright E2E test in `tests/FundingPlatform.Tests.E2E/Tests/FundingAgreementTests.cs`, organized one test method per user story, using `FundingAgreementPanelPage` and `FundingAgreementDownloadFlow` Page Objects. Running `dotnet test --filter Category=E2E&FullyQualifiedName~FundingAgreement` executes the full suite against the Aspire-orchestrated stack.
