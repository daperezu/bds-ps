# Quickstart: Digital Signatures for Funding Agreement

**Feature:** 006-digital-signatures
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-18

End-to-end manual walkthrough. Mirrors the four critical journeys mandated by SC-008 so QA and stakeholders can validate the feature in a browser without a test harness. Each journey is independently runnable.

---

## Prerequisites

- Local dev stack running via `.NET Aspire`:
  ```bash
  dotnet run --project src/FundingPlatform.AppHost
  ```
- Database deployed with the spec 006 dacpac build (includes `SignedUploads`, `SigningReviewDecisions`, `FundingAgreements.GeneratedVersion`).
- Seeded demo users (from `IdentityConfiguration.SeedUsersAsync` in dev mode):
  - `applicant@demo.com` / (seeded password) — role: `Applicant`
  - `reviewer@demo.com` / (seeded password) — role: `Reviewer`
  - `admin@demo.com` / (seeded password) — role: `Admin`
- An application belonging to `applicant@demo.com` that has already reached **ResponseFinalized** with at least one accepted item and a **generated Funding Agreement** (per spec 005). Easiest path: run the existing spec 005 quickstart to the "Funding Agreement generated" checkpoint, then pick up here.
- Any PDF-stamping tool installed locally (Adobe Reader, Preview on macOS, Foxit, etc.) for the "sign externally" steps. Anything that can add a visible signature mark and re-save a PDF is fine.

---

## Journey 1 — Happy path (SC-001, SC-008a)

**Persona:** applicant, then reviewer.

1. Log in as `applicant@demo.com`.
2. Navigate to `/ApplicantResponse/Index/{id}` for the application (the page you land on after accepting reviewer decisions). The Funding Agreement panel renders below the response-item form.
3. In the "Funding Agreement" panel, click **Download agreement**.
   - Browser downloads `funding-agreement.pdf` (or similar).
   - Audit-trail entry appears: `AgreementDownloaded`.
4. Open the PDF in your external tool. Add a signature stamp on the signature page. Save as `funding-agreement.signed.pdf`.
5. Back in the panel, click **Upload signed agreement** → select your signed PDF → submit.
   - Panel now shows a "Pending review" banner with filename, size, uploaded-at timestamp, uploader (you), and the `GeneratedVersion` the upload is tied to.
   - Application state in the header badge is still **ResponseFinalized**.
6. Log out. Log in as `reviewer@demo.com`.
7. In the main navigation click **Review**, then click the **Signing Inbox** sub-tab. You should see one row for the application.
8. Click through to the application (row link). The signing panel shows the pending upload and two buttons: **Approve** and **Reject**.
9. Click **Approve** (optional comment). Confirm.
   - Application state flips to **AgreementExecuted**.
   - Audit trail gains: `SignedUploadApproved`.
   - Panel shows the approved signed PDF with a download link.
10. Log out and log back in as `applicant@demo.com`. Confirm the application detail page shows **AgreementExecuted** and that the signed PDF is downloadable.

**Expected outcome:** application is `AgreementExecuted`. Generated PDF and signed PDF are both downloadable by applicant, reviewer, and admin. The audit trail shows (at least) `AgreementDownloaded`, `SignedAgreementUploaded`, `SignedUploadApproved`.

---

## Journey 2 — Rejection loop (SC-002, SC-008b)

**Persona:** applicant, reviewer, applicant.

1. Start with an application in the same state as the end of step 5 of Journey 1 (pending signed upload) — or go through steps 1–5 first with a deliberately bad signed PDF (e.g., don't actually stamp a signature).
2. Log in as `reviewer@demo.com`, click **Review** in the main navigation, click the **Signing Inbox** sub-tab, and open the application from the row.
3. Click **Reject**. Try to submit with an empty comment — the form should refuse with "Rejection comment is required".
4. Enter a comment like "Signature not visible on page 3; please sign again" and submit.
   - Audit trail gains: `SignedUploadRejected` with the comment as detail.
   - The panel returns to the "Ready to upload signed agreement" state. The rejection comment is displayed prominently to the applicant.
   - Application state remains **ResponseFinalized**.
5. Log out, log in as `applicant@demo.com`. The rejection comment is visible on the panel.
6. Re-sign the PDF correctly (add signature stamp on the right page this time). Re-upload.
7. Log out, log in as `reviewer@demo.com`. Approve the new upload.

**Expected outcome:** application reaches **AgreementExecuted** after at least one rejection cycle. Audit trail contains both the rejection and the subsequent approval, referencing distinct `SignedUpload` ids. No retry limit is enforced; the same loop can be run repeatedly.

---

## Journey 3 — Pre-review replacement and withdrawal (SC-008c)

**Persona:** applicant.

1. Log in as `applicant@demo.com` with an application whose agreement has been generated and has **no** signed upload yet.
2. Upload a signed PDF (Journey 1 step 5). The panel shows "Pending review".
3. Before any reviewer acts, click **Replace pending upload**. Select a different signed PDF. Submit.
   - Panel now shows the new file as "Pending review".
   - Audit trail shows: first `SignedAgreementUploaded`, then `SignedUploadReplaced`, referencing the new and superseded upload ids.
   - The superseded upload is not visible in the main panel but is retrievable via the audit list (reviewers and admins can download it via `DownloadSigned/{id}`).
4. Still as the applicant, click **Withdraw pending upload**. Confirm.
   - Panel returns to "Ready to upload signed agreement".
   - Audit trail gains: `SignedUploadWithdrawn`.
   - No pending upload exists; no reviewer action possible until the applicant uploads again.
5. (Race check) Open two browser windows as the applicant. In one, start to upload a replacement; in the other, log in as reviewer and approve the current pending upload. Submit the replace from the first window last. Expect **HTTP 409** with a "refresh and retry" message — the underlying record's status is no longer `Pending`.

**Expected outcome:** audit trail retains all three uploads (first, replacement, withdrawn) with correct statuses. The system prevents a partial commit on the reviewer-vs-applicant race.

---

## Journey 4 — Regeneration lockout (SC-005, SC-008d)

**Persona:** reviewer, applicant, reviewer.

1. Log in as `reviewer@demo.com` with an application whose agreement has been generated but not yet signed.
2. On the panel, click **Regenerate**. Confirm. The agreement's `GeneratedVersion` increments; a fresh PDF is produced.
   - Audit trail gains the existing `FundingAgreementRegenerated` entry (from spec 005).
3. Log out. Log in as `applicant@demo.com`. Upload a signed PDF. Confirm it's pending.
4. Log out. Log in as `reviewer@demo.com`. Click **Regenerate** again.
   - Expected: the action is blocked. The panel surfaces "Agreement is locked: a signed upload has been submitted."
   - Audit trail gains: `FundingAgreementRegenerationBlocked`.
5. **Version-mismatch sub-path** — skip the pending upload above and instead do the following:
   - As applicant, open `/ApplicantResponse/Index/{id}` and download the agreement from the embedded panel (call this V1).
   - Log out. Log in as reviewer, regenerate. Current `GeneratedVersion` becomes V2.
   - Log out. Log in as applicant. Sign V1 externally. Upload.
   - Expected: upload is rejected inline with "Please re-download the latest agreement and re-sign." No `SignedUpload` record is created. No audit entry.
   - Re-download (you now get V2), sign V2, upload. Upload is accepted. Panel shows pending review.

**Expected outcome:** regeneration is permitted until the first accepted signed upload and blocked after. Applicants cannot short-circuit the chain by uploading a signature of an outdated generated version.

---

## Operational recovery (post-signature)

Spec 006 does not include an administrative "back out" surface (R-015). If a signed agreement needs to be reset (e.g., accidentally approved, legal correction):

1. Verify the need out-of-band with product/legal.
2. On the database directly:
   ```sql
   -- Identify the application
   SELECT a.Id, a.State, fa.Id AS FundingAgreementId, fa.GeneratedVersion
   FROM dbo.Applications a
   LEFT JOIN dbo.FundingAgreements fa ON fa.ApplicationId = a.Id
   WHERE a.Id = @ApplicationId;

   -- Reset to ResponseFinalized and remove signed uploads
   BEGIN TRANSACTION;
   UPDATE dbo.Applications SET State = 5 WHERE Id = @ApplicationId;           -- ResponseFinalized
   DELETE d FROM dbo.SigningReviewDecisions d
      INNER JOIN dbo.SignedUploads u ON u.Id = d.SignedUploadId
      INNER JOIN dbo.FundingAgreements fa ON fa.Id = u.FundingAgreementId
     WHERE fa.ApplicationId = @ApplicationId;
   DELETE u FROM dbo.SignedUploads u
      INNER JOIN dbo.FundingAgreements fa ON fa.Id = u.FundingAgreementId
     WHERE fa.ApplicationId = @ApplicationId;
   -- Files on disk: leave as-is (retention) OR remove via ops runbook.
   COMMIT;
   ```
3. Record a manual `VersionHistory` entry describing the reset if audit completeness matters.

A dedicated admin-tooling feature will replace this manual path in a future spec.

---

## Where each journey is covered automatically

| Journey | E2E test | Integration test | Unit test |
|---|---|---|---|
| 1 (Happy path) | `DigitalSignatureTests.ApplicantCanSignAndReviewerCanApprove` | `SignedUploadEndpointsTests.UploadThenApprove_Succeeds` | `SignedUploadTests.PendingToApproved_EmitsDecision`; `ApplicationExecuteAgreementTests.ApproveTransitionsState` |
| 2 (Rejection loop) | `DigitalSignatureTests.ReviewerRejectionReturnsToReadyToUpload` | `SignedUploadEndpointsTests.RejectWithoutComment_Returns400` | `SigningReviewDecisionTests.RejectionRequiresComment` |
| 3 (Pre-review replace/withdraw) | `DigitalSignatureTests.ApplicantCanReplaceAndWithdrawBeforeReview` | `SignedUploadEndpointsTests.ReplacePendingUpload_SupersedesPrior`; `...ReviewerApproveDuringApplicantReplace_ReturnsConflict` | `FundingAgreementTests.AtMostOnePendingAtATime`; `SignedUploadTests.SupersededIsTerminal` |
| 4 (Regeneration lockout + version mismatch) | `DigitalSignatureTests.RegenerationBlockedAfterFirstSignedUpload`; `...VersionMismatchRejectsUpload` | `SignedUploadEndpointsTests.RegenerateAfterUpload_Returns400WithAudit` | `FundingAgreementLockdownTests.LockedWhenAnySignedUploadExists`; `ApplicationCanRegenerateTests.FailsWhenLocked` |

Running the full suite: `dotnet test`. Running just e2e: `dotnet test tests/FundingPlatform.Tests.E2E`.
