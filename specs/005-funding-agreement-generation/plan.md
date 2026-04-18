# Implementation Plan: Funding Agreement Document Generation

**Branch**: `005-funding-agreement-generation` | **Date**: 2026-04-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/005-funding-agreement-generation/spec.md`

## Summary

Produce the formal Funding Agreement as a PDF for any application whose applicant response is fully resolved (`ApplicationState.ResponseFinalized`), has no active appeal, and has at least one accepted item. Administrators and reviewers trigger generation from the application detail page; applicants, administrators, and reviewers can download the resulting PDF; administrators and reviewers can regenerate it, overwriting the prior file, as long as the preconditions still hold. The document is rendered by passing a Razor-rendered HTML template through Syncfusion's HTML-to-PDF component. The generated artifact is persisted through the existing `IFileStorageService` file-storage abstraction and recorded on a new `FundingAgreement` aggregate (one per application, with EF Core row-version concurrency). No new projects, no new infrastructure services, no background queue — a single synchronous request-scoped flow.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire, Syncfusion.HtmlToPdfConverter.Net.Linux (31.2.x), Syncfusion.Licensing
**Storage**: SQL Server (Aspire-managed container for dev, dacpac schema management); local file system for PDF bytes via `IFileStorageService`
**Testing**: Playwright for .NET (NUnit) for E2E; `WebApplicationFactory`-based integration tests for controller/authorization edges; NUnit for unit tests; NSubstitute for mocking
**Target Platform**: Linux/Windows server (Aspire-orchestrated); production container image is Linux-based, which is why the Linux-specific Syncfusion converter is chosen
**Project Type**: Web application (server-side MVC, no SPA)
**Performance Goals**: Generate-action latency ≤ 3 s at p95 for applications with up to 20 accepted items (SC-008); panel render on the application page ≤ 200 ms at p95 (no regression of the existing page)
**Constraints**: All state and lifecycle rules on the new aggregate expressed as domain methods (no external state manipulation); optimistic concurrency via `RowVersion` on `FundingAgreement`; authenticated endpoint for file delivery (no static web-root exposure); non-disclosing 404 responses on authorization failure and "no agreement exists"; Syncfusion license key required at startup (fail-fast)
**Scale/Scope**: One PDF per application; typical application has 1–10 accepted items; expected peak generation rate is a handful of clicks per minute across all administrators and reviewers (no bulk or automation path)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | New `FundingAgreement` aggregate in Domain; use cases (`GenerateFundingAgreement`, `DownloadFundingAgreement`) in Application; `FundingAgreementPdfRenderer` (wrapping Syncfusion) and `FundingAgreementConfiguration` (EF) in Infrastructure; `FundingAgreementController`, `FundingAgreementHtmlRenderer` (wrapping `IRazorViewEngine`), and views in Web. Dependencies point inward; the Infrastructure PDF renderer consumes a plain HTML string, knows nothing about Razor or MVC. |
| II. Rich Domain Model | PASS | Precondition evaluation lives on `Application.CanGenerateFundingAgreement()`; generation and regeneration are domain methods (`Application.GenerateFundingAgreement`, `Application.RegenerateFundingAgreement`); `FundingAgreement` invariants (content type, size, non-empty metadata) enforced in the constructor and mutation points. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | Six user stories each mapped to Playwright tests in `FundingAgreementTests.cs`. `FundingAgreementPanelPage` and `FundingAgreementDownloadFlow` page objects. Each story is independently runnable. Authorization and concurrency edges are covered by integration tests that use the in-memory web host to avoid Playwright's browser tax for non-UI assertions. |
| IV. Schema-First Database Management | PASS | One new table `FundingAgreements` as a `.sql` file in `FundingPlatform.Database/dbo/Tables/` with `ROWVERSION` and a unique index on `ApplicationId`. EF configuration maps to the schema; no EF migrations, no `EnsureCreated`. |
| V. Specification-Driven Development | PASS | Spec SOUND (REVIEW-SPEC.md), research.md resolves all opens, data-model.md and contracts/README.md flow from the spec. Tasks will be generated next. |
| VI. Simplicity and Progressive Complexity | PASS | One aggregate, one table, three MVC routes, one renderer, one template, one partial for T&Cs. No admin-editable templates, no versioning, no background queue, no new projects. The Syncfusion runtime dependency is justified by the brainstorm decision and tracked in `implementation-notes.md`. |

## Project Structure

### Documentation (this feature)

```text
specs/005-funding-agreement-generation/
├── spec.md                     # Stakeholder-facing specification (SOUND)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — all research resolved
├── data-model.md               # Phase 1 output — FundingAgreement aggregate, Application extensions
├── quickstart.md               # Phase 1 output — end-to-end manual walkthrough
├── contracts/
│   └── README.md               # MVC route contracts (panel, generate, download)
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── implementation-notes.md     # Brainstorm-era technology decisions (Syncfusion, Razor, aggregate)
├── review_brief.md             # Reviewer-facing guide
├── REVIEW-SPEC.md              # Formal spec soundness review
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
├── FundingPlatform.Domain/
│   ├── Entities/
│   │   ├── Application.cs                                   # MODIFY: add FundingAgreement navigation + domain methods (CanGenerateFundingAgreement, GenerateFundingAgreement, RegenerateFundingAgreement, CanUserAccessFundingAgreement, CanUserGenerateFundingAgreement)
│   │   └── FundingAgreement.cs                              # NEW: aggregate root
│   └── Interfaces/
│       └── IFundingAgreementRepository.cs                   # NEW: repository interface for Application-layer use cases
├── FundingPlatform.Application/
│   ├── FundingAgreements/
│   │   ├── Commands/
│   │   │   └── GenerateFundingAgreementCommand.cs           # NEW: orchestrates render + store + persist; handles FR-021 compensating delete
│   │   └── Queries/
│   │       ├── GetFundingAgreementPanelQuery.cs             # NEW: data for the application page's panel partial
│   │       └── GetFundingAgreementDownloadQuery.cs          # NEW: resolves storage path + metadata for the authenticated download action
│   ├── DTOs/
│   │   ├── FundingAgreementDto.cs                           # NEW
│   │   ├── FundingAgreementPanelDto.cs                      # NEW
│   │   └── FundingAgreementItemRowDto.cs                    # NEW
│   ├── Interfaces/
│   │   ├── IFundingAgreementPdfRenderer.cs                  # NEW: Application-layer contract implemented by Infrastructure (Syncfusion wrapper)
│   │   └── IFundingAgreementHtmlRenderer.cs                 # NEW: Application-layer contract implemented by Web (Razor wrapper)
│   └── Options/
│       └── FunderOptions.cs                                 # NEW: IOptions-bound funder identity config
├── FundingPlatform.Infrastructure/
│   ├── DocumentGeneration/
│   │   ├── SyncfusionFundingAgreementPdfRenderer.cs         # NEW: implements IFundingAgreementPdfRenderer using Syncfusion.HtmlToPdfConverter
│   │   └── SyncfusionLicenseValidator.cs                    # NEW: startup fail-fast license check
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── FundingAgreementConfiguration.cs             # NEW: EF Core configuration, row-version, unique index
│   │   ├── Repositories/
│   │   │   └── FundingAgreementRepository.cs                # NEW: IFundingAgreementRepository implementation
│   │   └── ApplicationDbContext.cs                          # MODIFY: register DbSet<FundingAgreement>
│   └── DependencyInjection.cs                               # MODIFY: register pdf renderer, repository, funder options, Syncfusion license validator
├── FundingPlatform.Web/
│   ├── Controllers/
│   │   └── FundingAgreementController.cs                    # NEW: three routes (Panel partial, Generate POST, Download GET)
│   ├── Services/
│   │   └── RazorFundingAgreementHtmlRenderer.cs             # NEW: implements IFundingAgreementHtmlRenderer via IRazorViewEngine
│   ├── ViewModels/
│   │   ├── FundingAgreementPanelViewModel.cs                # NEW: drives the panel partial on the Application Detail page
│   │   └── FundingAgreementDocumentViewModel.cs             # NEW: full model passed to the PDF template view
│   ├── Views/
│   │   ├── FundingAgreement/
│   │   │   ├── Document.cshtml                              # NEW: the PDF template root view (A4 layout)
│   │   │   ├── _FundingAgreementLayout.cshtml               # NEW: print-optimized layout (margins, page breaks, embedded fonts)
│   │   │   └── Partials/
│   │   │       ├── _FundingAgreementHeader.cshtml           # NEW: funder, applicant, agreement reference, date
│   │   │       ├── _FundingAgreementItemsTable.cshtml       # NEW: accepted-items table with currency formatting
│   │   │       ├── _FundingAgreementTermsAndConditions.cshtml # NEW: T&C partial (placeholder + TODO[LEGAL] at first)
│   │   │       └── _FundingAgreementSignatureBlocks.cshtml  # NEW: empty signature blocks (future Signatures spec)
│   │   └── Applications/
│   │       └── _FundingAgreementPanel.cshtml                # NEW: partial rendered on the Application Detail page
│   ├── Program.cs                                           # MODIFY: Syncfusion license registration + fail-fast validation at startup
│   └── appsettings.json                                     # MODIFY: add Syncfusion:LicenseKey (placeholder), FundingAgreement:LocaleCode (default es-CO), FundingAgreement:CurrencyIsoCode (default COP), FundingAgreement:Funder:*
├── FundingPlatform.AppHost/
│   └── AppHost.cs                                           # MODIFY: expose FundingAgreement configuration (license, locale, currency, funder) to the Web project
└── FundingPlatform.Database/
    └── dbo/Tables/
        └── dbo.FundingAgreements.sql                        # NEW: DDL for the new table

tests/
├── FundingPlatform.Tests.Unit/
│   └── Domain/
│       ├── FundingAgreementTests.cs                         # NEW: aggregate invariants (content type, size, metadata)
│       └── ApplicationFundingAgreementTests.cs              # NEW: precondition and transition methods on Application
├── FundingPlatform.Tests.Integration/
│   ├── Persistence/
│   │   └── FundingAgreementPersistenceTests.cs              # NEW: EF configuration, unique constraint, row-version concurrency
│   └── Web/
│       └── FundingAgreementEndpointsTests.cs                # NEW: authorization matrix, non-disclosure, concurrency-conflict behavior (WebApplicationFactory-based)
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── FundingAgreementPanelPage.cs                     # NEW: panel interactions on the Application Detail page
    │   └── FundingAgreementDownloadFlow.cs                  # NEW: download helper (captures the byte stream for assertions)
    └── Tests/
        └── FundingAgreementTests.cs                         # NEW: one test per user story (P1 generate, P1 applicant download, P2 regenerate, P2 blocked, P2 reviewer access, P3 unauthorized non-disclosure)
```

**Structure Decision**: No new projects. All changes fit the existing Clean Architecture layout established by spec 001 and used by specs 002 and 004. One new aggregate root (`FundingAgreement`), one new SQL table (`FundingAgreements`), one new MVC controller with a dedicated view folder, two new Application-layer interfaces (split between Infrastructure's PDF renderer and Web's Razor renderer to keep Clean Architecture boundaries honest), and the Razor template tree under `Views/FundingAgreement/`. `AppHost.cs` is updated to thread Syncfusion and funder/locale configuration into the Web project; `Program.cs` in Web fails fast if the Syncfusion license is missing or invalid.

## Constitution Re-Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | Final layout keeps Domain dependency-free, Application free of Razor/MVC/Syncfusion concerns (via the two split interfaces), Infrastructure talking only to Syncfusion, Web owning the Razor-rendering side plus controllers/views. The PDF-renderer interface lives in Application; the HTML-renderer interface lives in Application; Web provides the `Razor*` implementation, Infrastructure provides the `Syncfusion*` implementation. Inward dependency flow preserved. |
| II. Rich Domain Model | PASS | All precondition and lifecycle methods are on `Application` and `FundingAgreement`. Controllers and use cases are dumb pipes; no state transition or invariant lives in a service. |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | Playwright tests exist per user story. Integration tests cover authorization matrix and concurrency edges that are awkward in E2E. Test artifacts map 1:1 to spec's success criteria (SC-001 through SC-008). |
| IV. Schema-First Database Management | PASS | `dbo.FundingAgreements.sql` is the source of truth. EF configuration maps to it; `RowVersion` is a `ROWVERSION` column. Unique index on `ApplicationId` enforces the 0..1 relationship. No migrations, no `EnsureCreated`. |
| V. Specification-Driven Development | PASS | All FRs (001–023) and SCs (001–008) map to artifacts in this plan. The two remaining open questions from the spec (OQ-002 funder shape, OQ-004 reviewer regen rights) are resolved in research.md (R-006, R-007). |
| VI. Simplicity and Progressive Complexity | PASS | One aggregate, one table, three routes, no background queue, no admin UI for template editing, no version history. Syncfusion is a justified complexity (documented in `implementation-notes.md` and `research.md` R-001). |

## Complexity Tracking

No violations. All complexity is either justified by an existing constitution principle or explicitly tracked below.

| Decision | Justification |
|----------|---------------|
| Two split interfaces (`IFundingAgreementPdfRenderer` in Infrastructure, `IFundingAgreementHtmlRenderer` in Web) rather than a single Application-layer service that does Razor + Syncfusion | Razor is an ASP.NET Core MVC concern that legitimately belongs in the Web project (it needs `IRazorViewEngine`, `ActionContext`, `ITempDataProvider`). Syncfusion is an Infrastructure detail that belongs outside the Web project. Collapsing them into one service would force one layer to depend on the other's dependencies. |
| Synchronous in-request generation rather than a background queue | Brainstorming decision (no queue). Expected throughput (a few clicks per minute) and the 3 s p95 budget fit comfortably in synchronous request handling. |
| Overwrite-on-regeneration with no version history | Brainstorming decision. Operational escape hatch without storage/versioning cost; signature lockout lives in the future Signatures spec. Recorded as a reversible decision in `implementation-notes.md`. |
| Hardcoded T&C copy via a Razor partial (placeholder until legal delivers) | Brainstorming decision (hardcoded template); partial makes it easy for legal/product to point at one file for reviews. Placeholder + `TODO[LEGAL]` marker means nobody confuses a dev build for a production agreement. |
| New MVC controller (`FundingAgreementController`) rather than extending `ApplicationController` | Separation of responsibility. The funding-agreement surface is a distinct concern from application authoring/review/response; keeping it in its own controller simplifies routing, authorization filters, and test scope. |

## Out-of-Plan Notes

- **Post-signature regeneration lockout.** The future Digital Signatures spec will add a new predicate to `Application.CanGenerateFundingAgreement()` to block regeneration once signing has begun. Nothing in this plan hinders that addition.
- **Retention policy.** Spec OOS-010 defers a formal retention/purge policy for stored agreements. The current plan keeps files indefinitely; migration to a retention policy is a data-ops change, not a code change.
- **Multi-funder.** If the product ever supports multiple funders on one deployment, R-006's single-configuration-block funder identity can be migrated to a proper `Funder` aggregate without breaking `FundingAgreement`'s shape.
