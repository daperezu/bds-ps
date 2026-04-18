# Research: Funding Agreement Document Generation

**Feature**: 005-funding-agreement-generation
**Date**: 2026-04-17
**Status**: Phase 0 — complete

This document resolves the unknowns surfaced in the spec's Open Questions and the brainstorm's deferred planning items, plus the technology unknowns that fall out of the decision bundle approved during brainstorming (Syncfusion HTML-to-PDF, Razor template, dedicated `FundingAgreement` aggregate, synchronous generation).

---

## R-001 — Syncfusion HTML-to-PDF on Linux / Aspire

### Decision

Use **`Syncfusion.HtmlToPdfConverter.Net.Linux`** (v31.2.x, matching the broader Syncfusion bundle version the team standardizes on) alongside **`Syncfusion.Licensing`**. Call the converter from an Infrastructure-layer service that accepts an HTML string and produces a PDF byte stream.

### Rationale

- Linux-specific package exists (`Syncfusion.HtmlToPdfConverter.Net.Linux`) with active 2026 releases, including a documented Linux / Docker deployment guide. Our Aspire-orchestrated containers run on Linux, so this is the correct variant.
- The converter uses Blink (embedded Chromium-like) rendering and accepts raw HTML strings — a clean match for a Razor-rendered template.
- Syncfusion requires `Syncfusion.Licensing` since v16.2, so a license key must be registered at process startup (validates license fail-fast in Program.cs; spec FR covers this).
- Docker/Linux runtime additionally requires fontconfig and a set of font packages (e.g., `libfontconfig1`, `fonts-liberation`) in the container image; this is a small container-image change, not a new orchestration concern.

### Alternatives Considered

- **QuestPDF (C# fluent layout)**: no browser dependency and MIT-friendly community license, but abandons HTML/CSS authoring and is not Razor-compatible. Rejected during brainstorming for the same reason.
- **Playwright for .NET at runtime**: reuses an existing test-stack dependency but promotes Chromium to a runtime dependency, materially growing the Web container image. Rejected during brainstorming.
- **iText 8** (commercial, AGPL): capable but heavier, and no advantage over Syncfusion here.
- **DinkToPdf / wkhtmltopdf**: wkhtmltopdf is EOL upstream. Not a safe long-term choice.

### Integration Pattern

- A single Infrastructure service `FundingAgreementPdfRenderer` that takes `string html` and returns `Stream` (or `byte[]`) representing the PDF.
- License registration lives in `Program.cs` (Web project), reading the key from configuration (`Syncfusion:LicenseKey`).
- Fail-fast: during startup, attempt a no-op license registration and throw on failure so the container does not start with a misconfigured renderer.

### Open Follow-ups (implementation-time)

- Confirm the exact Syncfusion bundle version. The plan pins the major line; the tasks phase will pin the build.
- The Dockerfile for `FundingPlatform.Web` will add a small set of font packages. Exact package list to be verified against Syncfusion's Linux/Docker guide during implementation.

---

## R-002 — Razor view to HTML string inside an ASP.NET Core Web project

### Decision

Use the **manual `IRazorViewEngine` + `ViewContext` + `ITempDataProvider`** pattern to render a Razor view to an HTML string inside the Web project. Encapsulate the pattern in a single Web-layer service (`FundingAgreementHtmlRenderer`) so the Infrastructure-layer PDF renderer stays view-engine-agnostic.

### Rationale

- The rendered HTML has to be produced before it is handed to Syncfusion. Razor is the best fit for this because the template (layout, tables, formatting) is essentially a web view with dynamic data.
- `IRazorViewEngine` is already available inside the Web project and does not require a new package dependency.
- Keeping Razor in the Web project preserves Clean Architecture: the Infrastructure-layer PDF renderer takes a plain HTML string, so it knows nothing about Razor or MVC.
- `Razor.Templating.Core` is a viable one-line alternative (NuGet) but adds a dependency for functionality we already have in-stack. YAGNI: stay with the built-in engine.

### Alternatives Considered

- **`Razor.Templating.Core`**: one NuGet; one line per render; actively maintained. Slight convenience win; rejected because the same result is achievable without the extra dependency.
- **RazorLight** / **RazorEngineCore**: more template-engine than we need and adds non-MVC ceremony.
- **Render by calling our own MVC endpoint via `HttpClient`**: works but is operationally uglier (internal HTTP roundtrip, cookie/auth propagation) and doesn't play well with server-side generation triggered by a synchronous action.

### Integration Pattern

- `FundingAgreementHtmlRenderer` in `FundingPlatform.Web/Services/` depends on `IRazorViewEngine`, `ITempDataProvider`, `IServiceProvider`, and takes a `ControllerContext` (or `ActionContext`) plus the model, returns a `Task<string>`.
- The view path is a constant (e.g., `"~/Views/FundingAgreement/Document.cshtml"`); the layout is a dedicated layout file (`_FundingAgreementLayout.cshtml`) optimized for print (A4, margins, page breaks, embedded fonts).
- The Infrastructure PDF renderer consumes the returned HTML via `htmlConverter.Convert(htmlString, baseUrl)`.

---

## R-003 — Funding Agreement aggregate and its relationship to the existing file storage

### Decision

Introduce a new aggregate root **`FundingAgreement`** in the Domain project. The aggregate owns the generated PDF's metadata; the PDF bytes themselves are persisted through the existing `IFileStorageService` abstraction (from spec 001). Zero or one `FundingAgreement` per `Application`.

### Rationale

- The constitution's Rich Domain Model principle requires aggregate invariants (precondition checks, concurrency token, generated-once-per-application rule) to live on an entity, not in a service.
- `Application` already carries the state needed to evaluate preconditions (`State == ResponseFinalized`, no active `Appeal`, at least one `ApplicantResponse.ItemResponses` with `Decision == Accept`). A new method on `Application` evaluates eligibility; `FundingAgreement` records the generated artifact.
- `IFileStorageService` already handles the mechanics of saving, replacing, and streaming files via a path. Regeneration uses the same `Save → GetPath → Delete old` dance that `Quotation` replacement already uses (spec 001 FR-016), so the pattern is proven.
- A separate aggregate keeps user-uploaded `Document` entities cleanly apart from system-generated artifacts.

### Alternatives Considered

- **Reuse the existing `Document` entity with a discriminator** (`DocumentKind = Quotation | GeneratedAgreement`): rejected during brainstorming (conflates lifecycles; blurs aggregate boundaries).
- **Store the PDF bytes as a `varbinary` column**: rejected; inconsistent with spec 001's file-storage abstraction and counter to the principle of interface-based file storage.

### Integration Pattern

- `FundingAgreement` holds: `Id`, `ApplicationId`, file metadata (`FileName`, `ContentType`, `Size`, `StoragePath`), audit (`GeneratedAtUtc`, `GeneratedByUserId`), and `RowVersion` for optimistic concurrency.
- A 1:1 (0..1 from `Application`) navigation on `Application.FundingAgreement`.
- `Application.CanGenerateFundingAgreement()` → `bool` (and an error-collecting variant for the UI).
- `Application.GenerateFundingAgreement(…)` and `Application.RegenerateFundingAgreement(…)` are domain methods that construct or replace the entity and enforce invariants. The actual rendering and file persistence happen in the Application/Infrastructure layers orchestrated by a use case.

---

## R-004 — Concrete locale / currency default for Latin-American formatting (closes OQ-003-style gap)

### Decision

Default locale code: **`es-CO`** (Spanish, Colombia). Default currency symbol/ISO: configurable, default `COP`. Both read from configuration keys `FundingAgreement:LocaleCode` and `FundingAgreement:CurrencyIsoCode`. Formatting lookups use `CultureInfo.GetCultureInfo(localeCode)` in the template.

### Rationale

- The spec commits to a Latin-American style default (comma decimal separator, period thousands separator) and configuration-driven selection. `es-CO` matches that style and matches the most likely first deployment target; it is also a supported CLDR culture in .NET 10.
- Pinning a concrete locale makes E2E tests reproducible (a test can assert that `1.234.567,89 COP` renders correctly without branching on locale).
- `CurrencyIsoCode` is independently configurable so a deployment can keep `es-CO` formatting but switch currency to `USD` or `MXN` without code changes.

### Alternatives Considered

- **`es-MX`**: equally valid; Mexico's CLDR formatting is subtly different from Colombia's. Not chosen because there is no stated Mexico-first requirement; can be picked per-deployment via configuration.
- **Invariant culture with manual format strings**: yields full control but loses the CLDR infrastructure and makes tests less representative.

### Tasks Phase Implication

- `appsettings.json` gains `FundingAgreement:LocaleCode` and `FundingAgreement:CurrencyIsoCode` entries with documented defaults.
- E2E test fixture explicitly sets locale to `es-CO` to guarantee reproducibility regardless of host culture.

---

## R-005 — Authoring and ownership of the Terms & Conditions copy (closes the T&C open question)

### Decision

The T&C copy lives in a dedicated Razor partial view, **`_FundingAgreementTermsAndConditions.cshtml`**, under `Views/FundingAgreement/Partials/`. The text is owned by the product/legal stakeholder; changes are submitted as a code change via PR and shipped on a normal deploy. Until legal delivers final wording, the partial contains a clearly-marked **placeholder** paragraph and a `TODO[LEGAL]` comment at the top of the file.

### Rationale

- The brainstorm chose "hardcoded in the codebase" deliberately to avoid an editing UI, versioning, and a DB-backed text block.
- Keeping the T&C in its own partial file gives legal/product one file to point to for reviews, without the developer having to touch the data layer every time the wording changes.
- A placeholder paragraph plus a visible TODO is the cheapest way to keep the spec shippable in dev and testing while legal turns around the canonical copy. The placeholder is explicitly visible on the rendered PDF so nobody can mistake a dev build for a production agreement.

### Alternatives Considered

- **T&C stored in `SystemConfiguration` as a key-value text**: rejected; conflicts with the "hardcoded template" brainstorm decision and adds an editable surface area that the business did not ask for.
- **T&C as a Markdown file compiled into the template at build time**: marginal ergonomics win over a Razor partial; rejected to keep the rendering path single-stack (Razor in, HTML out).

### Delivery Path for Real T&C Copy

- Product/legal delivers final wording asynchronously.
- When delivered, a developer replaces the placeholder in `_FundingAgreementTermsAndConditions.cshtml` and removes the TODO marker. No schema or code-path change required.

---

## R-006 — Funder identity representation (closes OQ-002)

### Decision

A **single configuration block** under `FundingAgreement:Funder` (sub-keys: `LegalName`, `TaxId`, `Address`, `ContactEmail`, `ContactPhone`). Read at generation time via `IOptions<FunderOptions>` (or the equivalent in-stack options pattern). No new aggregate.

### Rationale

- Brainstorming and the spec both assume a single-funder deployment; introducing a `Funder` aggregate now would violate YAGNI.
- A single options-bound configuration block is indistinguishable from a 1-row DB table at the PDF's level of use. If the product later becomes multi-funder, a proper `Funder` aggregate can be introduced and the configuration block can be deprecated; nothing in this spec's design blocks that migration.

### Alternatives Considered

- **`Funder` aggregate**: flagged as a possibility in the spec; not warranted at this stage.

---

## R-007 — Reviewer regeneration rights (flagged in OQ-004)

### Decision

**Keep reviewer regeneration rights** as specified. Implement the authorization check as a single method on `Application` (`CanUserGenerateFundingAgreement(User u)`) that accepts `Administrator` or `Reviewer` roles and, for `Reviewer`, verifies that the user was assigned to the application's review.

### Rationale

- The user's explicit choice during brainstorming overrode the original admin-only proposal. This plan respects that choice.
- Operational audit is preserved: FR-013 requires the regenerating user be recorded on every regeneration, so any concerns about role-separation are traceable after the fact.
- If real-world operational experience shows the choice was wrong, it is a one-method change to restrict to administrators without touching the aggregate shape or the DB schema.

### Alternatives Considered

- **Restrict regeneration to administrators**: would require one of the spec's FRs to change; not chosen here but left as a trivially-reversible decision.

---

## R-008 — Concrete concurrency pattern for regeneration

### Decision

Use **EF Core `RowVersion` (timestamp / SQL Server `rowversion`)** on the `FundingAgreement` row for optimistic concurrency, mirroring the pattern already used on `Application` (verified in existing `Application.cs` at line 18).

### Rationale

- Matches the pattern established in spec 002 and spec 004 plans; no new infrastructure.
- Serializes parallel "generate" or "regenerate" attempts on the same application. Losing requests see a `DbUpdateConcurrencyException` and are translated to FR-022's retriable user-facing response.

### Alternatives Considered

- **Application-level lock via distributed cache**: overkill; single-database deployment does not need it.
- **Pessimistic `SELECT … FOR UPDATE`**: not idiomatic in EF Core; avoids fighting the stack.

---

## R-009 — Generation flow end-to-end

### Decision

Synchronous in-request generation with the following order of operations, all inside a single `DbContext` transaction wherever possible:

1. Load `Application` + required navigations (items, quotations, suppliers, applicant profile, appeals, applicant responses).
2. Call `Application.CanGenerateFundingAgreement()`; reject if false.
3. Build the view model and render the Razor view to HTML (in-memory).
4. Pass HTML to Syncfusion converter; receive PDF byte stream.
5. Persist bytes via `IFileStorageService.SaveFileAsync(...)`; get the storage path.
6. Create (or replace) the `FundingAgreement` entity via `Application.GenerateFundingAgreement(...)` / `RegenerateFundingAgreement(...)`.
7. `SaveChangesAsync()`.
8. On any failure: delete any file written in step 5 (compensating action), let the transaction roll back.

### Rationale

- Mirrors the spec's FR-021 (prior-state integrity on failure) and FR-022 (serialization via optimistic concurrency).
- Keeps the transaction boundary around the database changes; the file-system write is the one non-transactional step and is covered by the compensating delete.

### Alternatives Considered

- **Background queue**: rejected during brainstorming; synchronous meets the 3s p95 target for the documented scale.
- **File-first / DB-second ordering**: rejected because orphaned files are harder to detect and clean up than a missing file that a retry produces.

---

## R-010 — Authenticated download endpoint

### Decision

A single MVC action that takes an application id, resolves the current `FundingAgreement`, authorizes the caller, and streams the PDF via `FileStreamResult` from the storage service. No public URL to the raw file path exists; the web root (`wwwroot/uploads`) does not contain generated agreements.

### Rationale

- Matches FR-020 (authenticated endpoint) and FR-019 (non-disclosing responses: same status code whether the agreement exists or the caller is unauthorized).
- Keeps one code path for authorization, consistent with how the project already serves `Quotation` documents.

### Non-disclosure implementation note

- Any authorization failure, any "no agreement exists", and any "application not visible to this user" return the **same** HTTP 404 with an identical body. Admin-visible diagnostics live in server logs, not in the response.

---

## Summary of research-resolved NEEDS CLARIFICATION items

| ID | Source | Status |
|----|--------|--------|
| Syncfusion Linux support | R-001 | Resolved |
| Razor → HTML string approach | R-002 | Resolved |
| `FundingAgreement` aggregate shape | R-003 | Resolved |
| Concrete default locale code | R-004 | Resolved (`es-CO`, `COP`) |
| T&C copy ownership and delivery | R-005 | Resolved (Razor partial + TODO placeholder) |
| OQ-002 (funder identity shape) | R-006 | Resolved for this spec (single config block) |
| OQ-004 (reviewer regen rights) | R-007 | Resolved (kept as specified) |
| Concurrency pattern | R-008 | Resolved (RowVersion) |
| End-to-end generation flow | R-009 | Resolved |
| Authenticated download mechanics | R-010 | Resolved |

No NEEDS CLARIFICATION items remain. Phase 1 (data-model, contracts, quickstart) can proceed.
