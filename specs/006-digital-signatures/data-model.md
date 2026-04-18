# Data Model: Digital Signatures for Funding Agreement

**Feature:** 006-digital-signatures
**Phase:** 1 — Design & Contracts
**Date:** 2026-04-18

This document defines the domain entities, value objects, enums, relationships, state machines, and persistence mapping introduced or modified by this feature. Storage-side DDL is authoritative under `src/FundingPlatform.Database/Tables/` and mirrors the mapping here.

---

## Entity overview

```text
Application (MODIFY)
  └─ FundingAgreement (MODIFY: +GeneratedVersion, +SignedUploads, +lockdown & lifecycle methods)
       └─ SignedUpload (NEW, 1:*)
            └─ SigningReviewDecision (NEW, 1:0..1)
Application._versionHistory (EXISTING)
  └─ VersionHistory entries with new Action constants for signing events
```

No new aggregate roots. `FundingAgreement` remains the aggregate root for everything under it; `SignedUpload` and `SigningReviewDecision` are tightly owned children.

---

## 1. Enum: `ApplicationState` (MODIFY)

Path: `src/FundingPlatform.Domain/Enums/ApplicationState.cs`

Add one value. Preserve all existing ordinals.

```csharp
public enum ApplicationState
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Resolved = 3,
    AppealOpen = 4,
    ResponseFinalized = 5,
    AgreementExecuted = 6      // NEW — terminal for spec 006; handoff point for future Payment & Closure
}
```

Transitions introduced:
- `ResponseFinalized → AgreementExecuted` via `Application.ExecuteAgreement(userId)` (called on the reviewer-approval path).

No transitions remove or alter existing semantics.

---

## 2. Enum: `SignedUploadStatus` (NEW)

Path: `src/FundingPlatform.Domain/Enums/SignedUploadStatus.cs`

```csharp
public enum SignedUploadStatus
{
    Pending = 0,     // Awaiting reviewer action
    Superseded = 1,  // Replaced by a newer pending upload from the same applicant
    Withdrawn = 2,   // Explicitly withdrawn by the applicant before review
    Rejected = 3,    // Reviewer rejected with a required comment
    Approved = 4     // Reviewer approved — terminal; agreement executed
}
```

State transitions:

```text
                    (upload intake)
                         │
                         ▼
                     Pending
                    ┌──┼────┬──────┐
                    ▼  ▼    ▼      ▼
             Superseded  Withdrawn  Rejected  Approved
             (terminal)  (terminal) (terminal)(terminal)
```

Transition rules:
- `Pending → Superseded`: triggered by the applicant uploading a new signed PDF; the *previous* pending record flips to Superseded and a *new* record is created with Status=Pending.
- `Pending → Withdrawn`: triggered by the applicant explicitly withdrawing.
- `Pending → Rejected`: triggered by a reviewer with a required comment.
- `Pending → Approved`: triggered by a reviewer with an optional comment; propagates `Application.ExecuteAgreement(reviewerUserId)`.
- All non-Pending statuses are terminal for the record itself.

Domain invariant: **at most one** `SignedUpload` per `FundingAgreement` has `Status = Pending` at any time. Enforced by `FundingAgreement`'s mutation methods (see §5).

---

## 3. Enum: `SigningDecisionOutcome` (NEW)

Path: `src/FundingPlatform.Domain/Enums/SigningDecisionOutcome.cs`

```csharp
public enum SigningDecisionOutcome
{
    Approved = 0,
    Rejected = 1
}
```

Maps 1:1 to the reviewer's button choice. Recorded immutably on `SigningReviewDecision`.

---

## 4. Static class: `SigningAuditActions` (NEW)

Path: `src/FundingPlatform.Domain/Entities/SigningAuditActions.cs`

Action string constants used when constructing `VersionHistory` entries for signing events. Kept next to `VersionHistory` so the list is discoverable.

```csharp
public static class SigningAuditActions
{
    public const string AgreementDownloaded               = "AgreementDownloaded";
    public const string SignedAgreementUploaded           = "SignedAgreementUploaded";
    public const string SignedUploadReplaced              = "SignedUploadReplaced";
    public const string SignedUploadWithdrawn             = "SignedUploadWithdrawn";
    public const string SignedUploadApproved              = "SignedUploadApproved";
    public const string SignedUploadRejected              = "SignedUploadRejected";
    public const string FundingAgreementRegenerationBlocked = "FundingAgreementRegenerationBlocked";
}
```

(`FundingAgreementRegenerated` is expected to be emitted already by spec 005's regeneration path. If it isn't, planning-phase TODO: confirm or add it in that code path, not this one.)

`Details` convention per action:
- `AgreementDownloaded`: empty or `{ "generatedVersion": N }`.
- `SignedAgreementUploaded`: `{ "signedUploadId": N, "generatedVersion": N, "fileName": "…", "size": N }`.
- `SignedUploadReplaced`: `{ "supersededId": N, "newSignedUploadId": N }`.
- `SignedUploadWithdrawn`: `{ "signedUploadId": N }`.
- `SignedUploadApproved` / `SignedUploadRejected`: `{ "signedUploadId": N, "comment": "…" }`.
- `FundingAgreementRegenerationBlocked`: `{ "reason": "locked by signed upload", "pendingOrTerminalUploadId": N }`.

---

## 5. Entity: `FundingAgreement` (MODIFY)

Path: `src/FundingPlatform.Domain/Entities/FundingAgreement.cs`

### Added fields

| Field | Type | Notes |
|---|---|---|
| `GeneratedVersion` | `int` | Starts at `1`; incremented on each `Replace()` call. NOT NULL, DEFAULT 1 at schema level. |
| `_signedUploads` | `List<SignedUpload>` | Private backing collection; exposed as `IReadOnlyList<SignedUpload> SignedUploads`. |

### Added / modified methods

```csharp
// Read-only derived state
public IReadOnlyList<SignedUpload> SignedUploads { get; }
public bool IsLocked => _signedUploads.Count > 0;
public SignedUpload? PendingUpload => _signedUploads.SingleOrDefault(u => u.Status == SignedUploadStatus.Pending);

// Mutation: new
internal void Replace(string fileName, string contentType, long size, string storagePath, string regeneratingUserId)
{
    if (IsLocked)
        throw new InvalidOperationException("Funding agreement is locked: a signed upload has been submitted.");
    // existing validation + field assignment...
    GeneratedVersion++;
}

// Upload-lifecycle methods (all throw on invariant violation; caller translates to 400/409 as appropriate)
internal SignedUpload AcceptSignedUpload(
    string uploaderUserId,
    int generatedVersionAtUpload,
    string fileName,
    long size,
    string storagePath);

internal void WithdrawPendingUpload(string withdrawingUserId);

internal SignedUpload ReplacePendingUpload(
    string uploaderUserId,
    int generatedVersionAtUpload,
    string fileName,
    long size,
    string storagePath);

internal SigningReviewDecision ApprovePendingUpload(string reviewerUserId, string? comment);
internal SigningReviewDecision RejectPendingUpload(string reviewerUserId, string comment);
```

### Invariants enforced by `FundingAgreement`

| I-01 | At most one `Pending` SignedUpload at any time. |
| I-02 | `Replace()` throws if `IsLocked` (FR-010). |
| I-03 | `AcceptSignedUpload` and `ReplacePendingUpload` throw if `generatedVersionAtUpload != GeneratedVersion` (FR-011). |
| I-04 | `WithdrawPendingUpload`, `ApprovePendingUpload`, `RejectPendingUpload` throw if there is no pending upload. |
| I-05 | `RejectPendingUpload` requires a non-empty `comment` (FR-007). `ApprovePendingUpload` allows `null` or non-empty. |
| I-06 | All state mutations update `Application.UpdatedAt` via the parent Application (caller's responsibility or via domain event; simplest: the Application-layer service touches `UpdatedAt` on save). |

---

## 6. Entity: `SignedUpload` (NEW)

Path: `src/FundingPlatform.Domain/Entities/SignedUpload.cs`

```csharp
public class SignedUpload
{
    public int Id { get; private set; }
    public int FundingAgreementId { get; private set; }
    public string UploaderUserId { get; private set; } = string.Empty;
    public int GeneratedVersionAtUpload { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/pdf";
    public long Size { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }
    public SignedUploadStatus Status { get; private set; } = SignedUploadStatus.Pending;
    public byte[] RowVersion { get; private set; } = [];

    private SigningReviewDecision? _reviewDecision;
    public SigningReviewDecision? ReviewDecision => _reviewDecision;

    private SignedUpload() { }

    internal SignedUpload(
        int fundingAgreementId,
        string uploaderUserId,
        int generatedVersionAtUpload,
        string fileName,
        long size,
        string storagePath)
    {
        Validate(uploaderUserId, fileName, size, storagePath);

        FundingAgreementId = fundingAgreementId;
        UploaderUserId = uploaderUserId;
        GeneratedVersionAtUpload = generatedVersionAtUpload;
        FileName = fileName;
        Size = size;
        StoragePath = storagePath;
        UploadedAtUtc = DateTime.UtcNow;
        Status = SignedUploadStatus.Pending;
    }

    internal void MarkSuperseded() => Transition(SignedUploadStatus.Superseded);
    internal void MarkWithdrawn() => Transition(SignedUploadStatus.Withdrawn);

    internal SigningReviewDecision Reject(string reviewerUserId, string comment)
    {
        Transition(SignedUploadStatus.Rejected);
        _reviewDecision = new SigningReviewDecision(Id, SigningDecisionOutcome.Rejected, reviewerUserId, comment);
        return _reviewDecision;
    }

    internal SigningReviewDecision Approve(string reviewerUserId, string? comment)
    {
        Transition(SignedUploadStatus.Approved);
        _reviewDecision = new SigningReviewDecision(Id, SigningDecisionOutcome.Approved, reviewerUserId, comment);
        return _reviewDecision;
    }

    private void Transition(SignedUploadStatus target)
    {
        if (Status != SignedUploadStatus.Pending)
            throw new InvalidOperationException(
                $"SignedUpload {Id} cannot transition to {target}: current status is {Status}.");
        Status = target;
    }

    private static void Validate(string uploaderUserId, string fileName, long size, string storagePath)
    {
        if (string.IsNullOrWhiteSpace(uploaderUserId))
            throw new InvalidOperationException("SignedUpload requires a non-empty uploader user id.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("SignedUpload requires a non-empty file name.");
        if (size <= 0)
            throw new InvalidOperationException("SignedUpload size must be greater than zero.");
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new InvalidOperationException("SignedUpload requires a non-empty storage path.");
    }
}
```

### Field rationale

- `ContentType` is pinned to `application/pdf` as an invariant — non-PDF intake is rejected at the service boundary before reaching the entity constructor (R-009).
- `GeneratedVersionAtUpload` is copied from `FundingAgreement.GeneratedVersion` at intake (R-006).
- `RowVersion` is the optimistic concurrency token (R-007).
- `Status` is authoritative; the `ReviewDecision` is present iff `Status ∈ {Approved, Rejected}`.

---

## 7. Entity: `SigningReviewDecision` (NEW)

Path: `src/FundingPlatform.Domain/Entities/SigningReviewDecision.cs`

```csharp
public class SigningReviewDecision
{
    public int Id { get; private set; }
    public int SignedUploadId { get; private set; }
    public SigningDecisionOutcome Outcome { get; private set; }
    public string ReviewerUserId { get; private set; } = string.Empty;
    public string? Comment { get; private set; }
    public DateTime DecidedAtUtc { get; private set; }

    private SigningReviewDecision() { }

    internal SigningReviewDecision(
        int signedUploadId,
        SigningDecisionOutcome outcome,
        string reviewerUserId,
        string? comment)
    {
        if (string.IsNullOrWhiteSpace(reviewerUserId))
            throw new InvalidOperationException("SigningReviewDecision requires a non-empty reviewer user id.");
        if (outcome == SigningDecisionOutcome.Rejected && string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("Rejection requires a non-empty comment.");

        SignedUploadId = signedUploadId;
        Outcome = outcome;
        ReviewerUserId = reviewerUserId;
        Comment = comment;
        DecidedAtUtc = DateTime.UtcNow;
    }
}
```

### Invariants

- `Comment` is required (non-null, non-whitespace) when `Outcome = Rejected`.
- `Comment` may be null or any non-empty string when `Outcome = Approved`.
- `ReviewerUserId` is non-empty.
- `DecidedAtUtc` is UTC and set at construction.

---

## 8. Entity: `Application` (MODIFY)

Path: `src/FundingPlatform.Domain/Entities/Application.cs`

### New method: `CanRegenerateFundingAgreement`

Composition of the existing `CanGenerateFundingAgreement(out errors)` plus a lockdown check against the existing FundingAgreement (if any).

```csharp
public bool CanRegenerateFundingAgreement(out IReadOnlyList<string> errors)
{
    var failures = new List<string>();

    if (!CanGenerateFundingAgreement(out var baseErrors))
        failures.AddRange(baseErrors);

    if (_fundingAgreement is null)
        failures.Add("No Funding Agreement exists to regenerate.");

    else if (_fundingAgreement.IsLocked)
        failures.Add("Agreement is locked: a signed upload has been submitted.");

    errors = failures.Distinct(StringComparer.Ordinal).ToList().AsReadOnly();
    return errors.Count == 0;
}
```

### Modified method: `RegenerateFundingAgreement`

Swap the `CanGenerateFundingAgreement` precondition call for `CanRegenerateFundingAgreement`. `FundingAgreement.Replace()` now also enforces I-02 as a defence-in-depth; callers should rely on `CanRegenerateFundingAgreement` for the pre-check.

### New method: `ExecuteAgreement`

```csharp
public void ExecuteAgreement(string reviewerUserId)
{
    if (_fundingAgreement is null)
        throw new InvalidOperationException("Cannot execute agreement: no Funding Agreement exists.");

    // Domain pre-conditions are already guaranteed by having reached this point via the approve path
    // (FundingAgreement.ApprovePendingUpload has just run). We only transition state.

    if (State != ApplicationState.ResponseFinalized)
        throw new InvalidOperationException(
            $"Cannot execute agreement: application is in '{State}' state, expected 'ResponseFinalized'.");

    State = ApplicationState.AgreementExecuted;
    UpdatedAt = DateTime.UtcNow;
}
```

### New method: `SubmitSignedUpload` (thin facade)

To keep application-layer code clean, `Application` exposes a facade that forwards to `FundingAgreement.AcceptSignedUpload`. This keeps the caller from ever touching `FundingAgreement` directly.

```csharp
public SignedUpload SubmitSignedUpload(
    string uploaderUserId,
    int generatedVersionAtUpload,
    string fileName,
    long size,
    string storagePath)
{
    if (_fundingAgreement is null)
        throw new InvalidOperationException("Cannot submit signed upload: no Funding Agreement exists.");
    if (State != ApplicationState.ResponseFinalized)
        throw new InvalidOperationException(
            $"Cannot submit signed upload: application is in '{State}' state, expected 'ResponseFinalized'.");
    return _fundingAgreement.AcceptSignedUpload(
        uploaderUserId, generatedVersionAtUpload, fileName, size, storagePath);
}
```

Analogous facades: `ReplaceSignedUpload`, `WithdrawSignedUpload`, `ApproveSignedUpload`, `RejectSignedUpload`. `ApproveSignedUpload` additionally calls `ExecuteAgreement` at the end.

### Authorization reused

- `CanUserAccessFundingAgreement(applicantUserId, isAdministrator, isReviewerAssignedToThisApplication)` — used for all download paths (generated + signed).
- Applicant-side actions (upload/withdraw/replace) require `Applicant.UserId == currentUserId` and `State == ResponseFinalized` and `!_fundingAgreement.IsLocked || currentUploadIsPending`. Enforced at the application-service layer, not in the entity.
- Reviewer-side actions (approve/reject/regenerate) require `isAdministrator || isReviewerAssignedToThisApplication`. Reuses `CanUserGenerateFundingAgreement` for regeneration; approve/reject gets its own predicate (same roles) on `Application`.

```csharp
public bool CanUserReviewSignedUpload(bool isAdministrator, bool isReviewerAssignedToThisApplication)
    => isAdministrator || isReviewerAssignedToThisApplication;
```

---

## 9. Relationship summary

| Parent | Child | Cardinality | Cascade |
|---|---|---|---|
| `Application` | `FundingAgreement` | 1 : 0..1 | `ON DELETE NO ACTION` (existing, spec 005) |
| `FundingAgreement` | `SignedUpload` | 1 : 0..* | `ON DELETE CASCADE` — deleting the agreement (not supported by UI) would also remove signed uploads |
| `SignedUpload` | `SigningReviewDecision` | 1 : 0..1 | `ON DELETE CASCADE` |
| `Application` | `VersionHistory` | 1 : 0..* | existing |

---

## 10. Persistence: SQL Server schema

All schema changes are dacpac-managed in `src/FundingPlatform.Database/Tables/`. No EF migrations. EF Core maps to the existing tables; fluent configurations under `src/FundingPlatform.Infrastructure/Persistence/Configurations/`.

### 10.1 `dbo.FundingAgreements` — MODIFY

Add column:

```sql
ALTER TABLE [dbo].[FundingAgreements]
ADD [GeneratedVersion] INT NOT NULL CONSTRAINT DF_FundingAgreements_GeneratedVersion DEFAULT(1);
```

Add a CHECK constraint (optional safeguard; can be deferred if dacpac diffing is noisy):

```sql
ALTER TABLE [dbo].[FundingAgreements]
ADD CONSTRAINT CK_FundingAgreements_GeneratedVersion_Positive CHECK ([GeneratedVersion] >= 1);
```

### 10.2 `dbo.SignedUploads` — NEW

```sql
CREATE TABLE [dbo].[SignedUploads]
(
    [Id]                        INT            IDENTITY(1,1) NOT NULL,
    [FundingAgreementId]        INT            NOT NULL,
    [UploaderUserId]            NVARCHAR(450)  NOT NULL,
    [GeneratedVersionAtUpload]  INT            NOT NULL,
    [FileName]                  NVARCHAR(260)  NOT NULL,
    [ContentType]               NVARCHAR(100)  NOT NULL CONSTRAINT DF_SignedUploads_ContentType DEFAULT('application/pdf'),
    [Size]                      BIGINT         NOT NULL,
    [StoragePath]               NVARCHAR(1024) NOT NULL,
    [UploadedAtUtc]             DATETIME2(3)   NOT NULL,
    [Status]                    INT            NOT NULL,                        -- SignedUploadStatus enum
    [RowVersion]                ROWVERSION     NOT NULL,

    CONSTRAINT [PK_SignedUploads] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SignedUploads_FundingAgreements]
        FOREIGN KEY ([FundingAgreementId]) REFERENCES [dbo].[FundingAgreements]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SignedUploads_AspNetUsers]
        FOREIGN KEY ([UploaderUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_SignedUploads_Size_Positive] CHECK ([Size] > 0),
    CONSTRAINT [CK_SignedUploads_Status_Range] CHECK ([Status] BETWEEN 0 AND 4)
);

CREATE NONCLUSTERED INDEX [IX_SignedUploads_FundingAgreementId_Status]
    ON [dbo].[SignedUploads] ([FundingAgreementId], [Status]);

CREATE NONCLUSTERED INDEX [IX_SignedUploads_UploaderUserId]
    ON [dbo].[SignedUploads] ([UploaderUserId]);

-- Filtered unique index: at most one Pending upload per FundingAgreement (I-01, defence in depth).
CREATE UNIQUE NONCLUSTERED INDEX [UX_SignedUploads_OnePending_PerAgreement]
    ON [dbo].[SignedUploads] ([FundingAgreementId])
    WHERE [Status] = 0; -- Pending
```

### 10.3 `dbo.SigningReviewDecisions` — NEW

```sql
CREATE TABLE [dbo].[SigningReviewDecisions]
(
    [Id]               INT            IDENTITY(1,1) NOT NULL,
    [SignedUploadId]   INT            NOT NULL,
    [Outcome]          INT            NOT NULL,                        -- SigningDecisionOutcome enum
    [ReviewerUserId]   NVARCHAR(450)  NOT NULL,
    [Comment]          NVARCHAR(2000) NULL,
    [DecidedAtUtc]     DATETIME2(3)   NOT NULL,

    CONSTRAINT [PK_SigningReviewDecisions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SigningReviewDecisions_SignedUploads]
        FOREIGN KEY ([SignedUploadId]) REFERENCES [dbo].[SignedUploads]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SigningReviewDecisions_AspNetUsers]
        FOREIGN KEY ([ReviewerUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_SigningReviewDecisions_SignedUploadId] UNIQUE ([SignedUploadId]),
    CONSTRAINT [CK_SigningReviewDecisions_Outcome_Range] CHECK ([Outcome] BETWEEN 0 AND 1),
    CONSTRAINT [CK_SigningReviewDecisions_RejectComment]
        CHECK ([Outcome] <> 1 OR ([Comment] IS NOT NULL AND LTRIM(RTRIM([Comment])) <> N''))
);

CREATE NONCLUSTERED INDEX [IX_SigningReviewDecisions_ReviewerUserId]
    ON [dbo].[SigningReviewDecisions] ([ReviewerUserId]);
```

### 10.4 `dbo.Applications` — no DDL change

`ApplicationState` is stored as `INT`. Adding the `AgreementExecuted = 6` value is purely a C# enum change; the column already accepts any int. If there's a CHECK constraint on the column (verify at planning), extend its range.

### 10.5 `dbo.VersionHistory` — no change

Existing table; new `Action` constants are data, not schema.

---

## 11. EF Core configuration

One new file, one modified file:

### `FundingAgreementConfiguration.cs` — MODIFY

- Map `GeneratedVersion` to the new column.
- Add `HasMany(fa => fa.SignedUploads).WithOne().HasForeignKey(u => u.FundingAgreementId)` with `Cascade` delete.
- Backing-field mapping for `_signedUploads` (mirrors the existing backing-field pattern for `_fundingAgreement` in `AppDbContext`).

### `SignedUploadConfiguration.cs` — NEW

- Map to `[dbo].[SignedUploads]`.
- `IsConcurrencyToken()` on `RowVersion`.
- Configure enum-to-int conversions for `Status`.
- `HasOne(u => u.ReviewDecision).WithOne().HasForeignKey<SigningReviewDecision>(d => d.SignedUploadId)` with `Cascade`.
- Backing-field mapping for `_reviewDecision`.

### `SigningReviewDecisionConfiguration.cs` — NEW

- Map to `[dbo].[SigningReviewDecisions]`.
- Enum-to-int for `Outcome`.
- No concurrency token (the decision is immutable once inserted).

### `AppDbContext.cs` — MODIFY

- Add `DbSet<SignedUpload> SignedUploads` and `DbSet<SigningReviewDecision> SigningReviewDecisions`.
- Register backing fields for the new private collections so EF populates them on load.

---

## 12. Invariant / FR cross-reference

| Invariant | Enforced by | Covers FR |
|---|---|---|
| I-01 (one Pending per agreement) | `FundingAgreement.AcceptSignedUpload` + filtered unique index | FR-002, FR-005 |
| I-02 (no regen when locked) | `Application.CanRegenerateFundingAgreement` + `FundingAgreement.Replace` | FR-010 |
| I-03 (version-match on intake) | `FundingAgreement.AcceptSignedUpload` & `ReplacePendingUpload` | FR-011 |
| I-04 (no op without pending) | `FundingAgreement` mutation methods | FR-005, FR-006, FR-009 |
| I-05 (rejection needs comment) | `SigningReviewDecision` ctor + CHECK constraint | FR-007 |
| Terminal-status immutability (SignedUpload.Transition) | `SignedUpload.Transition` | FR-008, FR-009 |
| RowVersion concurrency on SignedUpload | EF config + service 409 translation | FR-015 |
| Audit for every state-changing event | `Application.AddVersionHistory` in each service method | FR-012 |
| Signed PDF retained for life | Storage policy (no purge) + authz on download | FR-013 |
| No deadline | Absence of scheduled-job code | FR-014 |

---

## 13. Removed / renamed artifacts

None. This is purely additive at the domain and data-model levels.
