# New SDK-style SQL project with Microsoft.Build.Sql

## Build

To build the project, run the following command:

```bash
dotnet build
```

🎉 Congrats! You have successfully built the project and now have a `dacpac` to deploy anywhere.

## Publish

To publish the project, the SqlPackage CLI or the SQL Database Projects extension for VS Code is required. The following command will publish the project to a local SQL Server instance:

```bash
sqlpackage /Action:Publish /SourceFile:bin/Debug/FundingPlatform.Database.dacpac /TargetServerName:localhost /TargetDatabaseName:FundingPlatform.Database
```

Learn more about authentication and other options for SqlPackage here: https://aka.ms/sqlpackage-ref

### Install SqlPackage CLI

If you would like to use the command-line utility SqlPackage.exe for deploying the `dacpac`, you can obtain it as a dotnet tool.  The tool is available for Windows, macOS, and Linux.

```bash
dotnet tool install -g microsoft.sqlpackage
```

## Spec 006 — Digital Signatures tables

Added by feature `006-digital-signatures`:

- **`dbo.FundingAgreements.GeneratedVersion`** — `INT NOT NULL DEFAULT 1` column.
  Increments on each `Replace()` (regeneration). Carried onto each `SignedUpload`
  at intake so stale-version uploads can be rejected at the application layer.
- **`dbo.SignedUploads`** — one row per uploaded signed PDF.
  `RowVersion` column (`ROWVERSION`) drives optimistic concurrency. The filtered
  unique index `UX_SignedUploads_OnePending_PerAgreement` enforces the
  "at most one pending upload per agreement" invariant at the DB level.
  Foreign key to `FundingAgreements` cascades on delete; foreign key to
  `AspNetUsers` uses `NO ACTION`.
- **`dbo.SigningReviewDecisions`** — one row per reviewer decision (approve or
  reject). `UQ_SigningReviewDecisions_SignedUploadId` enforces the 1:0..1
  relationship with `SignedUploads`. The
  `CK_SigningReviewDecisions_RejectComment` CHECK constraint requires a
  non-empty `Comment` when `Outcome = 1` (Rejected).
