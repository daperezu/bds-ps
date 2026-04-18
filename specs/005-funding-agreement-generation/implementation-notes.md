# Implementation Notes: Funding Agreement Document Generation

These notes capture the technology and architecture decisions reached during brainstorming so that the spec itself can remain technology-agnostic.

## Design Decisions

### Decision: Scope limited to the Funding Agreement

- **Choice**: Produce only one document type — the Funding Agreement.
- **Rejected**: A decision-letter + agreement bundle, a full lifecycle document set (submission receipt, decision letter, agreement, appeal resolution letter, closure letter), and the admin-configurable template approach (templates authored like Impact Templates in spec 001).
- **Rationale**: The Funding Agreement is the single document required to unblock the Digital Signatures and Payment features. Everything else is YAGNI at this stage. If more document types become necessary later, the admin-configurable template approach becomes the natural next step and can be introduced then.

### Decision: Admin-triggered generation after response is fully resolved

- **Choice**: Administrators (and, per the visibility choice below, reviewers) click "Generate agreement" from the application detail page. The action is available only when the application's review is closed with at least one approved item, the applicant has responded to every approved item, no appeal is active, and at least one item is accepted.
- **Rejected**: Auto-generation on full resolution, applicant-triggered generation, and auto-generation-on-first-accept-with-regeneration-on-appeal-resolution.
- **Rationale**: Keeps a human checkpoint between "applicant has decided" and "funding artifact is materialized." Avoids version-on-appeal complexity. Matches the platform's existing preference for explicit state transitions.

### Decision: Hardcoded template in the Web project

- **Choice**: The Funding Agreement layout, wording, and data placeholders live in the Web project as a Razor view. The rendering pipeline converts that Razor-produced HTML into a PDF via the Syncfusion HTML-to-PDF component.
- **Rejected**: Admin-editable single-active template, admin-editable versioned template, and a hybrid layout-in-code / text-blocks-in-DB model.
- **Rationale**: Legal wording changes for a single-funder deployment are rare; the added complexity of admin editing, versioning, or per-application overrides is unwarranted. If wording needs to change, it ships in code through the normal deployment path.

### Decision: Syncfusion HTML-to-PDF as the rendering library

- **Choice**: Use Syncfusion's HTML-to-PDF component at runtime. Authors a Razor view, renders it to an HTML string, passes that HTML through Syncfusion's converter, and persists the resulting PDF bytes via the existing file storage abstraction.
- **Rejected**:
  - **QuestPDF** (C# fluent, code-based layout): no browser dependency and fast, but abandons HTML/CSS authoring ergonomics.
  - **Playwright for .NET at runtime** (headless Chromium): reuses the test-stack dependency but promotes Chromium to a runtime dependency, materially growing the Web container image and slowing per-request generation (~1–3 s).
- **Rationale**: Preserves Razor-authoring ergonomics while avoiding a runtime browser dependency. Syncfusion bundles its own rendering engine and is a supported commercial product with a community-license path.
- **Trade-offs / Watch items**:
  - Commercial licensing — the license key must be configured at deployment and fail fast at startup if missing/invalid.
  - Syncfusion's HTML-to-PDF has known constraints around advanced CSS (certain flexbox/grid features, custom fonts); the template should be kept conservative and verified across target viewers (SC-003).
  - A dependency-health check at startup should validate that the license initializes successfully.

### Decision: Dedicated `FundingAgreement` aggregate, reusing the existing file storage abstraction

- **Choice**: Introduce a new `FundingAgreement` aggregate root with an owning reference to `Application` (0..1 per application), file metadata fields, generating-user/timestamp fields, and a concurrency token (row-version). Reuse the file storage interface introduced in spec 001 to persist and read the PDF bytes.
- **Rejected**: Reusing the existing `Document` entity with a `DocumentKind` discriminator (Quotation vs. GeneratedAgreement). That approach collapses two very different lifecycles (user-uploaded-and-replaceable vs. system-generated-and-workflow-gated) into one aggregate and would blur domain boundaries in ways that conflict with the Rich Domain Model principle (Constitution §II).
- **Rationale**: A dedicated aggregate lets the agreement encapsulate its own invariants — generated-once-per-application, overwrite-on-regeneration, FR-002 precondition checks as an entity-level validation method — without tangling with the quotation/upload lifecycle.

### Decision: Regenerate with confirmation; only latest retained; admin and reviewer both authorized

- **Choice**: Administrators and reviewers can regenerate the agreement at any time while preconditions still hold. The action requires explicit confirmation in the UI. A successful regeneration overwrites the prior file and metadata; prior versions are not retained.
- **Rejected**:
  - Immutable-once-generated (no regeneration).
  - Versioned history (every regeneration archived).
- **Rationale**: Provides an operational escape hatch without the storage and UI cost of versioning. When the Digital Signatures feature ships, it will introduce the additional rule that regeneration is locked once signing has begun; that rule belongs to the signatures feature, not this one.
- **Open concern (OQ-004)**: Whether reviewers should have regeneration rights (as opposed to view-only) was promoted from an earlier admin-only proposal to admin-plus-reviewer per user direction. This should be re-validated during planning when the full role-scope map for the feature is visible.

### Decision: Synchronous generation, in-request

- **Choice**: Generation happens synchronously inside the administrator's request. No background queue is introduced for this feature.
- **Rationale**: The expected workload (admin-clicks-once-per-application) plus the targeted latency (NFR: <3 s at p95 for up to 20 items) do not justify a queue. A future reporting/bulk feature may revisit this.

### Decision: Authenticated download endpoint, not direct storage URL

- **Choice**: Serve the PDF bytes through an authenticated controller endpoint that enforces authorization before streaming the file. Do not expose the storage path in the web root or in any URL that bypasses authorization.
- **Rationale**: Matches the Constitution's quality gate on authorization and resource ownership and closes the information-disclosure vector called out in FR-019.

### Decision: Configured Latin-American locale for formatting

- **Choice**: Locale and currency format are read from configuration and applied consistently throughout the document. Default is a Latin-American convention (comma decimal separator, period thousands separator). Specific locale code and currency symbol are deployment-configured.
- **Rejected**: Per-application locale, auto-detect from applicant profile.
- **Rationale**: The platform targets Latin-American deployments; a single configured locale per deployment is simpler and avoids inconsistency within a given document.

## Questions Answered During Brainstorming

- **Why not admin-editable templates?** — Single-funder deployment; legal wording is stable; admin UI, persistence, and versioning add cost without meeting a current need.
- **Why not reuse the existing `Document` entity?** — Two different lifecycles (uploaded vs. generated) don't belong in one aggregate; reusing would blur domain boundaries.
- **Why synchronous?** — Single-click, single-application throughput; latency budget fits in-request execution.
- **Why Syncfusion instead of QuestPDF or Playwright?** — Razor/HTML ergonomics without a Chromium runtime dependency; accepted trade-off of commercial licensing for a single-line-item component.
- **What about the zero-accepted-items path?** — Blocked at generation; terminal closure is deferred to Payment & Closure.
- **What about appeal-after-generation?** — Prior PDF retained and downloadable; regeneration becomes unavailable until preconditions hold again.

## Open Items for Planning

- **OQ-002 — Funder identity shape**: a single configuration block suffices for this feature. If multi-funder scenarios surface, this should be revisited and may become its own `Funder` aggregate.
- **OQ-004 — Reviewer regeneration rights**: recorded as granted per user direction; re-validate during the planning phase when the whole role-scope table for this feature is visible.
- **Agreement reference format**: the application's existing application number IS the reference. No additional formatting scheme is planned. If the finance team later requires a separate contract numbering scheme, it can be added without structural change.
- **Syncfusion license acquisition and configuration path**: to be coordinated during planning with the ops/procurement track.

## Runtime packages

The Web container image (or the Linux host where the Web project runs) must have the
following OS packages installed so that Syncfusion's Blink-based HTML-to-PDF renderer
can resolve fonts and render text without blank output:

- `libfontconfig1`
- `fonts-liberation`
- `fonts-dejavu-core`

No `Dockerfile` exists for `FundingPlatform.Web` today; Aspire orchestrates the project
via the .NET host. When containerization is introduced (the eventual production path),
the Dockerfile must `apt-get install -y --no-install-recommends libfontconfig1
fonts-liberation fonts-dejavu-core` in the build/runtime image layer. Until then,
Linux developer workstations should install these packages locally before running the
Aspire stack against the funding-agreement generation flow.

## Reviewer assignment model (deferred)

Spec 002 did not introduce a per-application reviewer-assignment table; the existing
`ReviewController` grants access to every user in the `Reviewer` role for every
application. For consistency with that existing surface, the Funding Agreement feature
treats "user has `Reviewer` role" as equivalent to "user is assigned to this
application" for both access and generation authorization.

When a proper assignment table is added (e.g., `ReviewerAssignments`), introduce a
`Domain/Interfaces/IReviewerAssignmentReader` helper and wire it through
`FundingAgreementController` (replacing the `User.IsInRole("Reviewer")` check in
`BuildPanelQueryAsync` + `Generate` + `Download`) without changing any domain method
signatures.
