# Research: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-25

This document resolves the planning-time unknowns identified during plan generation, plus a small spec correction surfaced during exploration.

## R-001 — Tabler.io edition and version pin

**Decision:** Vendor the Tabler.io **open-source** build (`@tabler/core`), pinned to the **latest stable 1.x** release at the time tasks are generated. Vendored under `wwwroot/lib/tabler/dist/` (CSS, JS, fonts/icons).

**Rationale:**
- Open-source coverage (sidebar, cards, page headers, forms, tables, badges, modals, empty states, alerts) maps cleanly to every existing surface — Tabler Pro's incremental components (charts, KPI tiles, complex dashboards) are not needed and explicitly out of scope.
- 1.x is the current stable line; 1.0 GA shipped in 2024 with continuous patch and minor updates since.
- Vendoring (rather than CDN) satisfies the FR-003 "no external CDN dependency" constraint and keeps the repository self-contained.
- MIT license is acceptable for vendored use (recorded as an Assumption in the spec).

**Alternatives considered:**
- **Tabler Pro** — rejected: paid license, components surplus to current needs; if charts/dashboards become spec-relevant later we can revisit.
- **CDN delivery** — rejected: violates FR-003.
- **NuGet wrapper** — none mature exists for Tabler; static-asset vendoring is the established pattern in this project (matches `wwwroot/lib/bootstrap`, `wwwroot/lib/jquery`).

## R-002 — How to bring Tabler dist files into `wwwroot/lib/tabler/`

**Decision:** Use a one-time `npm install --no-save @tabler/core@latest` in a scratch directory, then copy the contents of `node_modules/@tabler/core/dist/` into `wwwroot/lib/tabler/dist/`. Commit the vendored files. Document the version and copy procedure in a short comment in `wwwroot/lib/tabler/VERSION.txt` (just the version string and the date copied) so future updates are reproducible without ceremony.

**Rationale:**
- Matches the existing `wwwroot/lib/{bootstrap,jquery,jquery-validation}` convention — those directories also contain vendored dist files committed to source.
- Avoids introducing LibMan or a new build step (would conflict with the simplicity principle).
- Avoids npm-install at app build time (the project has no Node toolchain in its current build pipeline).
- Future updates are a one-line operation in the planning phase of a future spec.

**Alternatives considered:**
- **LibMan** (Microsoft client-side library manager) — rejected: introduces a new tool to the build, and Tabler is not in LibMan's default catalog.
- **Direct release-zip download** — rejected: less reproducible than npm; npm's package registry pins the version exactly.
- **MSBuild target to fetch at build time** — rejected: introduces a build-time network dependency.

## R-003 — Razor partials vs. ASP.NET Core View Components

**Decision:** Use **Razor partials** (`.cshtml` files in `Views/Shared/Components/`) for all nine reusable components. Each partial accepts a strongly typed model record.

**Rationale:**
- Partials are sufficient for the cases we have — they need parameters and slots, not C#-side cross-cutting concerns (no DI, no async data fetch from the partial itself, no reusable behavior beyond rendering).
- View Components require a backing C# class per component, which adds nine new files for zero benefit at our needs.
- Razor partials are conventionally where presentation-only reuse lives in ASP.NET MVC.
- The `Views/Shared/Components/` path is unambiguous and discoverable.

**Alternatives considered:**
- **ASP.NET Core View Components** — rejected: adds a class-per-component for no current benefit; we can promote individual partials to View Components later if a real cross-cutting need emerges.
- **Tag Helpers** — rejected: tag helpers are best for HTML-attribute-style consumption (`<status-pill value="@app.State" />`) but require a C# class per helper and are harder for view authors to discover. Partials with explicit `@await Html.PartialAsync("_StatusPill", model)` are more transparent.

## R-004 — Sidebar default-open vs. default-collapsed on first load

**Decision:** Accept Tabler's default behavior — **expanded** on viewports ≥ 992 px (Bootstrap `lg` breakpoint), **collapsed by default** on smaller viewports. No custom collapse logic, no first-load preference cookie. Closes one Open Question from the spec.

**Rationale:**
- Tabler's default behavior matches user expectation for desktop admin templates; reviewers and admins typically work on desktop where the sidebar adds wayfinding value.
- A custom preference cookie is YAGNI for the current user base (no real users yet). If usage data later shows reviewers wanting persistent collapse, that becomes its own future spec.
- Aligns with Edge Case in spec ("Sidebar collapse on small viewports uses Tabler default behavior").

**Alternatives considered:**
- **Persistent preference cookie** — rejected: YAGNI. No evidence today this is needed.
- **Always-expanded** — rejected: would steal real estate on narrow screens and confuse mobile users.
- **Always-collapsed** — rejected: hides wayfinding from reviewers who do most of their work in the sidebar.

## R-005 — Auth shell variant for Login / Register

**Decision:** Create a separate Razor layout **`Views/Shared/_AuthLayout.cshtml`**. `Views/Account/Login.cshtml` and `Views/Account/Register.cshtml` set `Layout = "_AuthLayout";` in their `@{}` block. The auth layout uses the same Tabler asset bundles but renders Tabler's `page page-center` chrome (no sidebar, centered card).

**Rationale:**
- Cleanly satisfies FR-002 ("authentication-shell variant of the same theme that omits the sidebar") without parameterizing the main `_Layout.cshtml` with an `if (User.Identity?.IsAuthenticated == true)` branch around the sidebar markup.
- Razor convention for "different chrome on a small set of pages" is a separate `_*Layout.cshtml`; future page additions just opt in by setting `Layout`.
- The `_ViewStart.cshtml` continues to default to `_Layout` for every other view.

**Alternatives considered:**
- **One layout with a conditional sidebar** — rejected: pollutes the main layout with auth-state branching for two pages; the conditional path is rarely taken in production traffic.
- **No layout for auth pages** — rejected: would lose typography/spacing inheritance and make the auth pages visually disconnected from the rest of the system.

## R-006 — Status mapping registry implementation

**Decision:** Static class **`FundingPlatform.Web.Helpers.StatusVisualMap`** with one method per enum (e.g., `For(ApplicationState s)` → `StatusVisual`). `StatusVisual` is a small `record(string Color, string Icon, string DisplayLabel)` under `FundingPlatform.Web.Models`. `_StatusPill.cshtml` accepts the enum value (boxed via `object` plus a runtime type switch, or via a generic helper extension), calls the appropriate `For(...)` overload, and renders the badge.

**Rationale:**
- Static helper is simpler than an interface + DI registration; the mapping has no runtime state and no need for swapping at runtime.
- Co-locating the four overloads in one class keeps the canonical mapping in a single grep-able file (matches FR-006: "single canonical mapping … _StatusPill MUST be the only place in the view tree where badges are produced").
- `StatusVisual` as a record makes the partial's binding obvious and enables ad-hoc debugging via `.ToString()`.
- Tabler color tokens (`bg-primary`, `bg-success`, `bg-warning`, `bg-danger`, `bg-info`, `bg-secondary`) and Tabler icon class names (`ti ti-clock`, `ti ti-check`, etc.) are stored as plain strings — the helper does not abstract the theme tokens.

**Alternatives considered:**
- **Per-enum attribute decoration** (e.g., `[StatusVisual(Color="bg-primary", Icon="ti ti-clock")]`) — rejected: scatters the mapping across the four enum files in the Domain layer, which would create a Web-to-Domain coupling (presentation tokens leaking into Domain); also harder to grep for "the one place badges are defined."
- **Razor-side switch in `_StatusPill.cshtml`** — rejected: hides the canonical mapping inside a view file rather than a code file, making it harder to reason about and harder to unit-test the mapping if we later want to.
- **Database-driven mapping** — rejected: vastly over-engineered for four enums that change with the codebase.

## R-007 — Action-class enum and canonical visual treatment

**Decision:** Introduce a small enum **`FundingPlatform.Web.Models.ActionClass`** with values `Primary`, `Secondary`, `Destructive`, `StateLocking`. `_ActionBar.cshtml` accepts a list of `(string Label, string Url, string FormPostTarget?, ActionClass Class, string? Icon)` records and renders each per FR-009's enumerated mapping:
- `Primary` → Tabler `btn btn-primary` (solid theme accent)
- `Secondary` → Tabler `btn btn-outline-secondary` (outlined neutral)
- `Destructive` → Tabler `btn btn-danger` (theme danger color)
- `StateLocking` → Tabler `btn btn-warning` paired with `ti ti-lock` icon (theme warning color + lock icon)

**Rationale:**
- Enumerated mapping in code prevents drift over time.
- Tabler's color tokens are well-suited (no need to invent new ones).
- The `Icon` parameter is optional so callers can override defaults when a specific action has a more specific icon (e.g., a "Generate Agreement" primary button might want `ti ti-file-text`).

**Alternatives considered:**
- **String-typed action class** — rejected: invites typos.
- **Per-class partial** (`_PrimaryAction.cshtml`, `_DestructiveAction.cshtml`, etc.) — rejected: explodes file count for no readability gain over a single `_ActionBar` with an enum parameter.

## R-008 — Selector-update strategy for existing E2E PageObjects (FR-019)

**Decision:** Centralize shell DOM access in a new (or expanded) `BasePage.cs` (or `LayoutPage.cs` if naming convention prefers) under `tests/FundingPlatform.Tests.E2E/PageObjects/`. The base exposes `Sidebar`, `Topbar`, `PageTitle`, and `BreadcrumbContainer` Locators. Other PageObjects inherit from this base instead of re-implementing nav/topbar selectors, so future shell DOM changes touch one file.

For the seven existing PageObjects that contain shell selectors today, update each to:
1. Inherit `BasePage` (or compose with it).
2. Replace prior navbar/menu locators with calls into the base.
3. Update content-area locators to match the new partials' rendered structure (e.g., `_DataTable` renders as `.card-table`, `_StatusPill` renders as `.badge.bg-{color}`).

Assertions on user-visible behavior (counts, presence of specific labels, navigation outcomes, post-action state) MUST remain identical — only the DOM-locator strings change. Where a prior assertion was DOM-structural (e.g., "third `<td>` of the row"), it gets re-anchored to a stable selector (e.g., `[data-testid='application-state-pill']` after we add a `data-testid` on `_StatusPill`).

**Rationale:**
- Centralizing selectors is the only way to keep the spec's "FR-019 must not relax assertions" promise honest as the DOM changes.
- Adding `data-testid` attributes to the new partials is cheap, makes E2E selectors stable across future visual revisions, and is invisible to end users.
- The Page Object Model pattern is already mandated by the constitution (Principle III); this just deepens its discipline.

**Alternatives considered:**
- **Re-update each existing PageObject ad-hoc** — rejected: invites duplication and selector drift between tests.
- **Snapshot-based selectors** — rejected: snapshot tests are brittle for visual sweeps and would create false positives.
- **Skip selector updates and adapt later** — rejected: violates FR-019 and Constitution Principle III.

## R-009 — Vendored asset surface and what to delete from `wwwroot/lib/`

**Decision:** Add `wwwroot/lib/tabler/` with the Tabler dist files. **Keep** `wwwroot/lib/bootstrap/`, `wwwroot/lib/jquery/`, `wwwroot/lib/jquery-validation/`, `wwwroot/lib/jquery-validation-unobtrusive/` for this spec — Tabler is built atop Bootstrap 5 and reuses Bootstrap's JS for some interactions, and the validation pipeline depends on jQuery. **Do not delete** any existing vendored asset in this spec.

A future cleanup spec can prune unreferenced files once the sweep settles and we can confirm via build/E2E that nothing references them.

**Rationale:**
- Consistent with the spec's "no flag-day cutover within the spec" principle (Tabler is a Bootstrap superset, so existing Bootstrap markup keeps rendering during the rewrite).
- Deleting jQuery would require re-validating the entire form-validation pipeline, which is out of scope (FR-016 forbids validation changes).
- Keeping the existing assets does not change runtime behavior — pages will use whichever stylesheet is linked by `_Layout.cshtml`, which after the rewrite links Tabler bundles only.

**Alternatives considered:**
- **Delete Bootstrap immediately** — rejected: invites breakage in any view that the sweep happens to leave with a Bootstrap-only class that doesn't have a Tabler equivalent.
- **Move assets to a CDN** — rejected: violates FR-003.

## R-010 — Spec correction surfaced during exploration: enum names

**Decision:** Update spec references from `ItemReviewState` → `ItemReviewStatus` and from `SigningStatus` → `SignedUploadStatus`. These are the correct enum names in `FundingPlatform.Domain.Enums`. The `StatusVisualMap` and `_StatusPill` accept the corrected names.

**Rationale:**
- Discovered by grepping `src/` during plan exploration: there is no `ItemReviewState` enum (the actual is `ItemReviewStatus`), and there is no `SigningStatus` enum (the actual is `SignedUploadStatus`, used on signed-upload entities).
- Spec inline-edited at planning time because this is a factual reference correction, not a scope change. The mapping intent (one canonical color+icon per status) is unaffected.
- The other two enums (`ApplicationState`, `AppealStatus`) are spelled correctly in the spec.

**Alternatives considered:**
- **Leave the spec wrong and clarify only in plan** — rejected: spec drift. The spec is the source of truth and must reference real names.
- **Introduce wrapper "alias" enums** — rejected: pure ceremony, hides the discrepancy.

## R-011 — Layout-cascade strategy for the sweep

**Decision:** All sweep work proceeds in one branch (`008-tabler-ui-migration`) but lands as a sequence of commits aligned with the spec's user-story priorities:
- **P1 commit batch** — vendor Tabler, rewrite `_Layout.cshtml`, add `_AuthLayout.cshtml`, add `BasePage.cs` for tests, ensure all existing E2E tests still pass against the new shell with no view-side changes. The system is fully usable after this batch — every existing view renders inside the new shell via CSS cascade only.
- **P2 commit batch** — add the nine partials and `StatusVisualMap`, rewrite the high-traffic surfaces (`Application/Index`, `Application/Details`, `Review/Index`, `Review/Review`, `Review/SigningInbox`), add the role-aware sidebar test class.
- **P3 commit batch** — sweep the remaining surfaces (`Account`, `Admin`, `ApplicantResponse`, `Item`, `Quotation`, `Home`, the smaller `Review` and `FundingAgreement` views), enforce the grep-clean invariants.

**Rationale:**
- Aligns with the spec's priority-ordered user stories (P1 / P2 / P3) and the constitution's commit discipline ("commit after each task or logical group; each user story should be independently completable and testable at its checkpoint").
- Each batch is a clean checkpoint that can be reviewed and tested independently — matches the Independent Test descriptions in the spec.
- Allows pausing or shipping at any of the three checkpoints if scope or priority changes.

**Alternatives considered:**
- **One giant commit** — rejected: violates commit discipline; impossible to review.
- **Separate branches per story** — rejected: the user explicitly chose the single-spec-sweep model; merging three branches back together adds churn for no review benefit at this scope.
