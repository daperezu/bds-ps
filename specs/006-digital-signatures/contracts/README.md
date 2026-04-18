# Contracts: Digital Signatures for Funding Agreement

**Feature:** 006-digital-signatures
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-18

This document specifies the MVC route contracts introduced or modified by this feature. All existing spec 005 routes retain their current shape; this feature extends one controller (`FundingAgreementController`) with new actions and adds one reviewer-queue action on `ReviewController`.

**Conventions (inherited from specs 002/005):**
- All routes require `[Authorize]` on the controller class.
- Responses use HTML-form posts with `[ValidateAntiForgeryToken]`, redirecting on success to the Application Detail page with `TempData` messages.
- Authorization failures return **404 Not Found** (non-disclosing) rather than 403, matching spec 005's pattern.
- Concurrency conflicts (FR-015) return **HTTP 409 Conflict** and render a dedicated view with a "retry" action.
- Non-PDF, oversize, and version-mismatch failures render the panel inline with validation errors (`ModelState`).

---

## 1. New routes on `FundingAgreementController`

Controller-level attribute (unchanged): `[Route("Applications/{applicationId:int}/FundingAgreement")]`, `[Authorize]`.

### 1.1 `POST /Applications/{applicationId:int}/FundingAgreement/Upload`

Applicant uploads a signed PDF counterpart.

**Action signature:**
```csharp
[HttpPost("Upload"), ValidateAntiForgeryToken]
[RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)] // coarse transport limit; service enforces business limit
public Task<IActionResult> Upload(int applicationId, UploadSignedAgreementViewModel model);
```

**ViewModel:**
```csharp
public class UploadSignedAgreementViewModel
{
    [Required] public int GeneratedVersion { get; set; }
    [Required] public IFormFile File { get; set; } = default!;
}
```

**Preconditions checked (in order):**
1. User is authenticated.
2. Application exists and is loaded with `FundingAgreement` + `SignedUploads`.
3. `Application.Applicant.UserId == currentUserId` (only the applicant uploads). If not authorized: **404**.
4. `Application.State == ResponseFinalized`. If not: **400** with validation error.
5. `FundingAgreement != null`. If no agreement yet: **404**.
6. No pending upload exists already (`FundingAgreement.PendingUpload == null`) — if one exists, this is a replacement (§1.2) not an upload. Return **400** with "use Replace".
7. Intake validation: content-type is `application/pdf`, magic header `%PDF-`, size ≤ `SignedUploadOptions.MaxSizeBytes`. On failure: render panel with validation error (ModelState) and **no** audit entry (per FR-003's "MUST NOT record a rejected intake").
8. Version match: `model.GeneratedVersion == agreement.GeneratedVersion`. On mismatch: render panel with "Please re-download the latest agreement and re-sign." No upload created.

**Happy path:**
- Save stream via `IFileStorageService.SaveFileAsync` → `storagePath`.
- Call `Application.SubmitSignedUpload(userId, agreement.GeneratedVersion, fileName, size, storagePath)`.
- Append `VersionHistory(SigningAuditActions.SignedAgreementUploaded, details)`.
- Save changes. On `DbUpdateConcurrencyException`: **409** with "Another action just modified this upload; please refresh."
- Redirect to `GET /Applications/{id}` with TempData success.

**Failure compensations:**
- If persistence fails after file save, best-effort `DeleteFileAsync(storagePath)` (matches spec 005 pattern).

**Returned status codes:** 302 (success redirect), 400 (validation), 404 (authz / not found), 409 (concurrency).

---

### 1.2 `POST /Applications/{applicationId:int}/FundingAgreement/ReplaceUpload`

Applicant supersedes a still-pending upload.

**Action signature:**
```csharp
[HttpPost("ReplaceUpload"), ValidateAntiForgeryToken]
public Task<IActionResult> ReplaceUpload(int applicationId, UploadSignedAgreementViewModel model);
```

Same ViewModel as §1.1.

**Preconditions:**
1–5. Same as §1.1.
6. A pending upload exists (`FundingAgreement.PendingUpload != null`). If not: **400** "use Upload".
7. `PendingUpload.UploaderUserId == currentUserId` (applicant can only replace their own pending upload). Otherwise **404**.
8. Intake validation (content-type, magic header, size). Failure: render with error, no mutation.
9. Version match. Failure: render with "please re-download".

**Happy path:**
- Save new file via storage → `newStoragePath`.
- `FundingAgreement.ReplacePendingUpload(userId, generatedVersion, fileName, size, newStoragePath)`:
  - Transitions the previous pending record to `Superseded`.
  - Creates a new `SignedUpload` with `Status = Pending`.
- Append `VersionHistory(SigningAuditActions.SignedUploadReplaced, details)`.
- Save.
- Do **not** delete the superseded file from storage — FR-013 mandates lifetime retention for audit.

**Concurrency:** if a reviewer just acted on the pending record (race per FR-015), the `Status != Pending` invariant fires on the old record and EF returns a concurrency conflict → **409**.

---

### 1.3 `POST /Applications/{applicationId:int}/FundingAgreement/WithdrawUpload`

Applicant withdraws their pending upload.

**Action signature:**
```csharp
[HttpPost("WithdrawUpload"), ValidateAntiForgeryToken]
public Task<IActionResult> WithdrawUpload(int applicationId, int signedUploadId);
```

**Preconditions:**
1–5. Same ownership/state checks as §1.2.
6. `FundingAgreement.PendingUpload?.Id == signedUploadId`. Mismatch or no pending: **400** "no pending upload to withdraw" or **404** if the id is not under this agreement.
7. Pending upload's uploader is the current user.

**Happy path:**
- `FundingAgreement.WithdrawPendingUpload(userId)`.
- Append `VersionHistory(SigningAuditActions.SignedUploadWithdrawn, details)`.
- Save. Concurrency → **409**.

The file itself is retained on disk (audit).

---

### 1.4 `POST /Applications/{applicationId:int}/FundingAgreement/Approve`

Reviewer approves the pending signed upload. This is the ceremony that executes the agreement.

**Action signature:**
```csharp
[HttpPost("Approve"), ValidateAntiForgeryToken]
public Task<IActionResult> Approve(int applicationId, int signedUploadId, string? comment);
```

**Preconditions:**
1. User is in role `Admin` or `Reviewer`.
2. `Application.CanUserReviewSignedUpload(isAdmin, isReviewerAssigned)` (the assignment check uses the existing review-assignment pattern from spec 002/003; if the user is a plain Reviewer not assigned to this application, treat as not authorized → **404**).
3. `FundingAgreement.PendingUpload?.Id == signedUploadId`. Mismatch: **400** "stale pending upload; refresh".

**Happy path:**
- `Application.ApproveSignedUpload(reviewerUserId, comment)`:
  - Transitions the upload to `Approved`, creates `SigningReviewDecision`.
  - Calls `Application.ExecuteAgreement(reviewerUserId)` → `State = AgreementExecuted`.
- Append `VersionHistory(SigningAuditActions.SignedUploadApproved, details)`.
- Save. Concurrency → **409**.
- Redirect to application detail; TempData success "Agreement executed."

---

### 1.5 `POST /Applications/{applicationId:int}/FundingAgreement/Reject`

Reviewer rejects the pending signed upload with a required comment.

**Action signature:**
```csharp
[HttpPost("Reject"), ValidateAntiForgeryToken]
public Task<IActionResult> Reject(int applicationId, int signedUploadId, string comment);
```

**Preconditions:**
1–2. Same as §1.4.
3. `comment` is non-empty. Empty → **400** "Rejection comment is required." (FR-007). No state change.
4. Pending-upload id matches.

**Happy path:**
- `Application.RejectSignedUpload(reviewerUserId, comment)`:
  - Transitions the upload to `Rejected`, creates `SigningReviewDecision`.
- Append `VersionHistory(SigningAuditActions.SignedUploadRejected, details)`.
- Save. Concurrency → **409**.
- Redirect; TempData info "Upload rejected; the applicant can submit a new one."

Application state **does not change** — `ResponseFinalized` persists, applicant is back in "ready to upload" by virtue of `PendingUpload == null`.

---

### 1.6 `GET /Applications/{applicationId:int}/FundingAgreement/DownloadSigned/{signedUploadId:int}`

Stream a stored signed PDF to an authorized user.

**Action signature:**
```csharp
[HttpGet("DownloadSigned/{signedUploadId:int}")]
public Task<IActionResult> DownloadSigned(int applicationId, int signedUploadId);
```

**Preconditions:**
1. `Application.CanUserAccessFundingAgreement(applicantUserId, isAdmin, isReviewerAssigned)` → true. Otherwise **404**.
2. The `signedUploadId` belongs to this application's `FundingAgreement`. Cross-application id → **404**.

**Happy path:**
- Open stream via `IFileStorageService.GetFileAsync(storagePath)`.
- Return `File(stream, "application/pdf", fileName)`.

Audit: spec 006 does **not** require an audit entry for downloading the signed PDF (the audit model in FR-012 focuses on state-changing events; download of the signed PDF is not state-changing). Kept consistent with spec 005's treatment of generated-PDF downloads.

---

### 1.7 `GET /Applications/{applicationId:int}/FundingAgreement/Download` — MODIFY

Existing spec 005 action. Extend with:
- Append `VersionHistory(SigningAuditActions.AgreementDownloaded, details)` on every successful download. (FR-012 requires download to be audit-logged in this feature.)

Everything else about the action remains unchanged.

---

### 1.8 `GET /Applications/{applicationId:int}/FundingAgreement/Panel` — MODIFY

Existing spec 005 panel returns `FundingAgreementPanelDto`. Extend the DTO shape (via a new `SigningStagePanelDto` composed from the existing panel DTO) to include:
- `PendingUpload` summary (id, uploadedAtUtc, uploader display name, size, generatedVersionAtUpload) — present iff a pending upload exists.
- `LastDecision` summary (outcome, comment, decided-at, reviewer display name) — present iff a terminal upload exists.
- `CanApplicantUpload` — derived per role, state, and lockdown.
- `CanApplicantReplaceOrWithdraw` — derived similarly.
- `CanReviewerAct` — derived.
- `CanRegenerate` — recomputed via `Application.CanRegenerateFundingAgreement` instead of `CanGenerateFundingAgreement`.
- `SignedDownloadUrl` — pointing at §1.6 for the approved upload, if one exists.

The panel is still rendered as a partial at `~/Views/Applications/_FundingAgreementPanel.cshtml`; the markup grows to include the signing section. No route path change.

---

## 2. New routes on `ReviewController`

### 2.1 `GET /Review/SigningInbox`

Lists applications whose latest SignedUpload is `Pending`, for the current user (if Reviewer) or all (if Admin).

**Action signature:**
```csharp
[HttpGet("SigningInbox")]
[Authorize(Roles = "Admin,Reviewer")]
public Task<IActionResult> SigningInbox(int page = 1, int pageSize = 25);
```

**Returns:** View `Views/Review/SigningInbox.cshtml` with a paged list of `SigningInboxRowViewModel`:

```csharp
public class SigningInboxRowViewModel
{
    public int ApplicationId { get; set; }
    public string ApplicantDisplayName { get; set; } = "";
    public int SignedUploadId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public int GeneratedVersionAtUpload { get; set; }
    public bool VersionMatchesCurrent { get; set; }  // UI hint if the applicant missed a regeneration
}
```

Rows link to the Application Detail page, not to an inline-review modal. Reviewer acts from there.

---

## 3. Authorization matrix

| Action | Applicant (owner) | Admin | Reviewer (assigned) | Reviewer (unassigned) |
|---|---|---|---|---|
| `Panel` (GET) | ✅ view | ✅ view | ✅ view | 404 |
| `Generate` (POST, spec 005) | 404 | ✅ | ✅ | 404 |
| `Regenerate` (POST, spec 005 route) | 404 | ✅ iff not locked | ✅ iff not locked | 404 |
| `Download` (GET, generated) | ✅ | ✅ | ✅ | 404 |
| `Upload` (POST) | ✅ iff no pending | 404 | 404 | 404 |
| `ReplaceUpload` (POST) | ✅ iff owns pending | 404 | 404 | 404 |
| `WithdrawUpload` (POST) | ✅ iff owns pending | 404 | 404 | 404 |
| `Approve` (POST) | 404 | ✅ iff pending exists | ✅ iff pending exists | 404 |
| `Reject` (POST) | 404 | ✅ iff pending exists | ✅ iff pending exists | 404 |
| `DownloadSigned` (GET) | ✅ | ✅ | ✅ | 404 |
| `SigningInbox` (GET) | 404 | ✅ all pending | ✅ own-assigned pending | ✅ own-assigned pending |

"Reviewer (assigned)" requires the user to be listed as the application's assigned reviewer per spec 002/003's existing assignment model. Admins bypass the assignment check.

---

## 4. Error response contracts

| Condition | HTTP | UX |
|---|---|---|
| Unauthenticated | 302 to login | Framework default |
| Unauthorized / not found | 404 | Generic "Not found" (non-disclosing) |
| Bad upload (non-PDF, oversized, wrong magic header) | 200 with validation errors in panel | Inline error; no audit record |
| Version mismatch on upload/replace | 200 with panel validation | "Please re-download the latest agreement and re-sign." |
| Missing comment on reject | 200 with panel validation | "Rejection comment is required." |
| Concurrency race | 409 | Dedicated error view offering "refresh and retry" |
| Infrastructure error (storage, db) | 500 | Generic error page; file cleanup attempted |

---

## 5. Interactions with spec 005 contracts

Spec 005's existing contract surface is preserved. Only additive changes:
- `GET Panel` response shape extends via `SigningStagePanelDto` (superset).
- `POST Regenerate` preconditions tighten by using `CanRegenerateFundingAgreement` (now fails if locked).
- `GET Download` emits an audit entry in addition to serving the file.

No breaking changes to spec 005's existing URLs or HTTP semantics.
