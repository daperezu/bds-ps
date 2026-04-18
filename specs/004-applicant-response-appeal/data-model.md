# Data Model: Applicant Response & Appeal

**Date**: 2026-04-17

## State Machine (Application)

New transitions added to the existing `ApplicationState` enum. Preserves existing values 0-3.

```
  Draft (0)
    |
    | Submit(minQuotations)
    v
  Submitted (1)
    |
    | StartReview()
    v
  UnderReview (2)
    |
    +-- SendBack() --> Draft
    |
    | Finalize(force)
    v
  Resolved (3)                              <-- [reviewer finalized decisions]
    |
    | SubmitResponse(responses)             <-- [applicant completes per-item response]
    |
    +--(if OpenAppeal called)--> AppealOpen (4)
    |                                |
    |                                +-- Resolve(Uphold)  --> ResponseFinalized (5)
    |                                |
    |                                +-- Resolve(GrantReopenToDraft)  --> Draft
    |                                |
    |                                +-- Resolve(GrantReopenToReview) --> UnderReview
    |
    +--(if no appeal opened)---> ResponseFinalized (5)
```

Notes:
- `Resolved (3)` now represents "review finalized, awaiting applicant response." It is no longer a terminal state when at least one item is approved. If every item is rejected and the applicant chooses not to appeal, the application still progresses to `ResponseFinalized` once the applicant submits their (all-rejected-confirmed) response.
- `ResponseFinalized (5)` is the terminal state from this feature's perspective. Downstream features (document generation, payment, closure) will introduce further transitions.
- The `SendBack()` method from spec 002 is preserved unchanged; the appeal-driven reopen uses a new `ReopenForReview()` method that does NOT reset item review statuses.

## New Entities

### ApplicantResponse (NEW - Aggregate Root)

A snapshot of the applicant's decisions for one response cycle on an application. Immutable after submission. An application may have multiple `ApplicantResponse` rows across reopen cycles.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Id | int | Yes | identity | Primary key |
| ApplicationId | int | Yes | — | Foreign key to `Applications` |
| CycleNumber | int | Yes | — | 1 for the first response; 2 for the response after a granted appeal + reopen; etc. |
| SubmittedAt | datetime2(7) | Yes | — | Timestamp of submission |
| SubmittedByUserId | string | Yes | — | Foreign key to `AspNetUsers` (the applicant) |

**Invariants:**
- Exactly one `ApplicantResponse` per `(ApplicationId, CycleNumber)`.
- Must have one `ItemResponse` per item on the application at submission time.

**Behavior:**
- Factory: `ApplicantResponse.Submit(applicationId, cycleNumber, submittedByUserId, itemResponses)` — constructs the aggregate and enforces one-response-per-item invariant.
- No mutation methods after construction (immutable).

### ItemResponse (NEW - Child Entity of ApplicantResponse)

The applicant's accept/reject decision for a single item within a response.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Id | int | Yes | identity | Primary key |
| ApplicantResponseId | int | Yes | — | Foreign key to `ApplicantResponses` |
| ItemId | int | Yes | — | Foreign key to `Items` |
| Decision | int | Yes | — | Enum: 0 = Accept, 1 = Reject |

**Invariants:**
- Exactly one `ItemResponse` per `(ApplicantResponseId, ItemId)`.
- `ItemId` must belong to the same application as the parent `ApplicantResponse`.

### Appeal (NEW - Aggregate Root)

A formal dispute opened against a completed applicant response.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Id | int | Yes | identity | Primary key |
| ApplicationId | int | Yes | — | Foreign key to `Applications` |
| ApplicantResponseId | int | Yes | — | Foreign key to the response this appeal disputes |
| OpenedAt | datetime2(7) | Yes | — | When the appeal was opened |
| OpenedByUserId | string | Yes | — | Applicant user who opened the appeal |
| Status | int | Yes | 0 (Open) | Enum: 0 = Open, 1 = Resolved |
| Resolution | int | Nullable | NULL | Enum: 0 = Uphold, 1 = GrantReopenToDraft, 2 = GrantReopenToReview (set when resolved) |
| ResolvedAt | datetime2(7) | Nullable | NULL | When the appeal was resolved |
| ResolvedByUserId | string | Nullable | NULL | Reviewer user who resolved the appeal |
| RowVersion | timestamp (rowversion) | Yes | auto | Optimistic concurrency token |

**Invariants:**
- Only one `Appeal` with `Status == Open` may exist per `ApplicationId` at any time.
- `Resolution`, `ResolvedAt`, and `ResolvedByUserId` are all NULL iff `Status == Open`.
- Parent `ApplicantResponse` must have at least one `ItemResponse` with `Decision == Reject` (enforced when calling `Application.OpenAppeal()`).

**Behavior:**
- Factory: `Appeal.Open(applicationId, applicantResponseId, openedByUserId)` — constructs with `Status = Open`.
- `PostMessage(authorUserId, text)` — appends an `AppealMessage`. Throws if `Status == Resolved`.
- `Resolve(resolvedByUserId, resolution)` — sets `Status = Resolved`, populates resolution fields, stamps `ResolvedAt`. Throws if already resolved.

### AppealMessage (NEW - Child Entity of Appeal)

A single text message in the dispute thread.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Id | int | Yes | identity | Primary key |
| AppealId | int | Yes | — | Foreign key to `Appeals` |
| AuthorUserId | string | Yes | — | Foreign key to `AspNetUsers` |
| Text | nvarchar(4000) | Yes | — | Message body |
| CreatedAt | datetime2(7) | Yes | — | Timestamp (monotonic, used for chronological ordering) |

**Invariants:**
- `Text` is non-empty and ≤ 4000 characters.
- `CreatedAt` is set at creation and never updated.

**Behavior:**
- Constructed only via `Appeal.PostMessage()`. No external factory, no mutation methods.

## Modified Entities

### Application (MODIFIED)

**New methods:**

| Method | Purpose | Throws |
|--------|---------|--------|
| `SubmitResponse(itemResponses, submittedByUserId)` | Creates an `ApplicantResponse` snapshot, transitions `State: Resolved → ResponseFinalized` | If `State != Resolved`; if `itemResponses` does not cover all items; if the application is frozen (handled by state check). |
| `OpenAppeal(openedByUserId, maxAppeals)` | Opens an `Appeal` on the most recent `ApplicantResponse`, transitions `State: ResponseFinalized → AppealOpen` | If `State != ResponseFinalized`; if no `ItemResponse` has `Decision == Reject`; if cumulative appeal count has reached `maxAppeals`. |
| `ResolveAppealAsUphold(resolvedByUserId)` | Resolves the active appeal as `Uphold`, transitions `State: AppealOpen → ResponseFinalized` | If `State != AppealOpen`; if no active appeal. |
| `ResolveAppealAsGrantReopenToDraft(resolvedByUserId)` | Resolves the active appeal as `GrantReopenToDraft`, transitions `State: AppealOpen → Draft`, clears `SubmittedAt` | If `State != AppealOpen`; if no active appeal. |
| `ResolveAppealAsGrantReopenToReview(resolvedByUserId)` | Resolves the active appeal as `GrantReopenToReview`, transitions `State: AppealOpen → UnderReview` (preserves item review statuses and comments) | If `State != AppealOpen`; if no active appeal. |

**New navigation properties:**

| Property | Type | Description |
|----------|------|-------------|
| `ApplicantResponses` | `IReadOnlyList<ApplicantResponse>` | All response snapshots, ordered by `CycleNumber` |
| `Appeals` | `IReadOnlyList<Appeal>` | All appeals, ordered by `OpenedAt` |

**Cumulative appeal count:** `Appeals.Count` (across all cycles). Compared against `MaxAppealsPerApplication` from `SystemConfiguration` on every `OpenAppeal` call.

### ApplicationState (MODIFIED - Enum)

| Value | Name | Status |
|-------|------|--------|
| 0 | Draft | Existing |
| 1 | Submitted | Existing |
| 2 | UnderReview | Existing |
| 3 | Resolved | Existing (semantics unchanged: reviewer has finalized) |
| 4 | AppealOpen | **NEW** |
| 5 | ResponseFinalized | **NEW** |

### SystemConfiguration (EXTENDED - seed data only)

One new seed row in the key-value table. No schema change.

| Key | Default Value | Description |
|-----|---------------|-------------|
| `MaxAppealsPerApplication` | `"1"` | Maximum number of appeals per application across all reopen cycles. Integer parsed from string. `0` disables appeals. |

## New Enums

### ItemResponseDecision

```
Accept = 0
Reject = 1
```

### AppealStatus

```
Open = 0
Resolved = 1
```

### AppealResolution

```
Uphold = 0
GrantReopenToDraft = 1
GrantReopenToReview = 2
```

## Database Schema (new files)

### `src/FundingPlatform.Database/dbo/Tables/ApplicantResponses.sql`

```sql
CREATE TABLE [dbo].[ApplicantResponses] (
    [Id]                  INT                IDENTITY (1, 1) NOT NULL,
    [ApplicationId]       INT                NOT NULL,
    [CycleNumber]         INT                NOT NULL,
    [SubmittedAt]         DATETIME2 (7)      NOT NULL,
    [SubmittedByUserId]   NVARCHAR (450)     NOT NULL,
    CONSTRAINT [PK_ApplicantResponses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ApplicantResponses_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]),
    CONSTRAINT [FK_ApplicantResponses_Users]
        FOREIGN KEY ([SubmittedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [UQ_ApplicantResponses_AppCycle] UNIQUE ([ApplicationId], [CycleNumber])
);
GO
CREATE INDEX [IX_ApplicantResponses_ApplicationId] ON [dbo].[ApplicantResponses] ([ApplicationId]);
```

### `src/FundingPlatform.Database/dbo/Tables/ItemResponses.sql`

```sql
CREATE TABLE [dbo].[ItemResponses] (
    [Id]                      INT  IDENTITY (1, 1) NOT NULL,
    [ApplicantResponseId]     INT  NOT NULL,
    [ItemId]                  INT  NOT NULL,
    [Decision]                INT  NOT NULL,
    CONSTRAINT [PK_ItemResponses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ItemResponses_ApplicantResponses]
        FOREIGN KEY ([ApplicantResponseId]) REFERENCES [dbo].[ApplicantResponses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItemResponses_Items]
        FOREIGN KEY ([ItemId]) REFERENCES [dbo].[Items] ([Id]),
    CONSTRAINT [UQ_ItemResponses_ResponseItem] UNIQUE ([ApplicantResponseId], [ItemId])
);
GO
CREATE INDEX [IX_ItemResponses_ItemId] ON [dbo].[ItemResponses] ([ItemId]);
```

### `src/FundingPlatform.Database/dbo/Tables/Appeals.sql`

```sql
CREATE TABLE [dbo].[Appeals] (
    [Id]                      INT                IDENTITY (1, 1) NOT NULL,
    [ApplicationId]           INT                NOT NULL,
    [ApplicantResponseId]     INT                NOT NULL,
    [OpenedAt]                DATETIME2 (7)      NOT NULL,
    [OpenedByUserId]          NVARCHAR (450)     NOT NULL,
    [Status]                  INT                NOT NULL DEFAULT 0,
    [Resolution]              INT                NULL,
    [ResolvedAt]              DATETIME2 (7)      NULL,
    [ResolvedByUserId]        NVARCHAR (450)     NULL,
    [RowVersion]              ROWVERSION         NOT NULL,
    CONSTRAINT [PK_Appeals] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Appeals_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]),
    CONSTRAINT [FK_Appeals_ApplicantResponses]
        FOREIGN KEY ([ApplicantResponseId]) REFERENCES [dbo].[ApplicantResponses] ([Id]),
    CONSTRAINT [FK_Appeals_OpenedByUser]
        FOREIGN KEY ([OpenedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [FK_Appeals_ResolvedByUser]
        FOREIGN KEY ([ResolvedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [CK_Appeals_ResolutionConsistency] CHECK (
        ([Status] = 0 AND [Resolution] IS NULL AND [ResolvedAt] IS NULL AND [ResolvedByUserId] IS NULL)
        OR
        ([Status] = 1 AND [Resolution] IS NOT NULL AND [ResolvedAt] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL)
    )
);
GO
CREATE INDEX [IX_Appeals_ApplicationId] ON [dbo].[Appeals] ([ApplicationId]);
CREATE UNIQUE INDEX [UX_Appeals_OneOpenPerApplication]
    ON [dbo].[Appeals] ([ApplicationId]) WHERE [Status] = 0;
```

### `src/FundingPlatform.Database/dbo/Tables/AppealMessages.sql`

```sql
CREATE TABLE [dbo].[AppealMessages] (
    [Id]              INT               IDENTITY (1, 1) NOT NULL,
    [AppealId]        INT               NOT NULL,
    [AuthorUserId]    NVARCHAR (450)    NOT NULL,
    [Text]            NVARCHAR (4000)   NOT NULL,
    [CreatedAt]       DATETIME2 (7)     NOT NULL,
    CONSTRAINT [PK_AppealMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AppealMessages_Appeals]
        FOREIGN KEY ([AppealId]) REFERENCES [dbo].[Appeals] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppealMessages_Users]
        FOREIGN KEY ([AuthorUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [CK_AppealMessages_TextNotEmpty] CHECK (LEN([Text]) > 0)
);
GO
CREATE INDEX [IX_AppealMessages_AppealId_CreatedAt]
    ON [dbo].[AppealMessages] ([AppealId], [CreatedAt]);
```

### Seed data (post-deployment script)

```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'MaxAppealsPerApplication')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
    VALUES ('MaxAppealsPerApplication', '1', 'Maximum appeals per application across all reopen cycles. 0 disables appeals.', SYSUTCDATETIME());
END
```

## Relationships

```
Application 1 ─── N ApplicantResponse 1 ─── N ItemResponse N ─── 1 Item
     │                     │
     │                     │
     └── 1 ─── N Appeal 1 ─── N AppealMessage
                     │
                     └── 1 ─── 1 ApplicantResponse (the response being disputed)
```

All new entities link back to `AspNetUsers` for author/actor attribution.

## DTOs (summary)

New DTOs in `FundingPlatform.Application/DTOs/`:

- `ApplicantResponseDto` — application id, cycle number, submitted at/by, list of `ItemResponseDto`
- `ItemResponseDto` — item id, item display fields (name, reviewed supplier, amount, review status), decision
- `AppealDto` — id, application id, opened at/by, status, resolution (nullable), resolved at/by (nullable), list of `AppealMessageDto`
- `AppealMessageDto` — id, author user id, author display name, text, created at

## ViewModels (summary)

New view models in `FundingPlatform.Web/ViewModels/`:

- `ApplicantResponseViewModel` — bound to response screen (accept/reject each item)
- `ItemResponseViewModel` — per-item row
- `AppealThreadViewModel` — bound to dispute thread screen (message list + post-reply form + resolution form)
- `AppealMessageViewModel` — single message

## Validation Rules (summary)

Collected from FRs and surfaced in domain methods + view-model validation:

| Rule | Enforced in | Related FR |
|------|-------------|------------|
| Every item must have an `ItemResponse` before submit | `ApplicantResponse.Submit()` | FR-002, FR-003 |
| Response immutable after submission | No mutation methods exist | FR-006 |
| Appeal only after complete response | `Application.OpenAppeal()` | FR-007 |
| Appeal requires at least one rejected item | `Application.OpenAppeal()` | FR-008 |
| Appeal cap enforced | `Application.OpenAppeal()` | FR-009, FR-023 |
| Application freezes during appeal | `Application` state machine (AppealOpen) | FR-010 |
| Messages text-only | Schema (no attachment columns) + domain (`Appeal.PostMessage` accepts only string) | FR-014 |
| Resolution requires explicit action | `Appeal.Resolve()` is the only resolution path | FR-021 |
| Only reviewer role can resolve | Controller `[Authorize(Roles = "Reviewer")]` | FR-016 |
| Only applicant owner + reviewers can view | Query handlers check ownership/role | FR-025 |
