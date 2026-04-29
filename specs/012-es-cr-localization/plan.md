# Implementation Plan: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Branch**: `012-es-cr-localization` | **Date**: 2026-04-29 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-es-cr-localization/spec.md`

## Summary

Pin the platform to a single fixed locale (`es-CR`), translate every user-facing surface to formal Costa Rican Spanish (warm-modern voice, formal `usted`), and rename the product from "Forge" to "Capital Semilla." The work ships as a single coordinated sweep across ~72 Razor views, the `StatusVisualMap` registry, framework messages (validation + Identity + ModelBinding), the Funding Agreement PDF, the brand-mark assets, the JS namespace, and the 186-selector Playwright E2E test suite (POM-first refactor leveraging ~62% cascading).

**Technical approach** (settled in research, see [research.md](./research.md)):
- **Architecture**: replace strings inline at the seams that specs 008 / 011 already designed; no `IStringLocalizer`, no `.resx`, no swappable-locale machinery (NFR-003).
- **Locale formatting**: clone `CultureInfo.GetCultureInfo("es-CR")` and override `NumberFormatInfo` (period decimal, comma thousands) and `DateTimeFormatInfo` (`dd/MM/yyyy`) to deliver the spec's user-visible target. The .NET `es-CR` defaults differ from the target; the spec's Assumption clause anticipated this exactly.
- **Single-locale enforcement**: `RequestLocalization` middleware pinned to one supported culture, no negotiation, no fallback. `RequestCultureProviders.Clear()`.
- **Identity**: register a custom `EsCrIdentityErrorDescriber` subclass via `AddErrorDescriber<>` (preserves existing `SentinelAwareUserStore`).
- **MVC**: configure `MvcOptions.ModelBindingMessageProvider` with Spanish accessors for type-mismatch, missing-required, and unknown-value messages.
- **Status display**: translate the 18 `DisplayLabel` values in `StatusVisualMap.cs`. No `.ToString()` bypasses exist (research Decision 5).
- **Tests**: introduce `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` consumed by 4 POMs and shared fixtures; ~62% leverage on the 186-selector rewrite.
- **Brand**: replace "Forge" at 7 user-facing sites; hard-cut `window.ForgeMotion` → `window.PlatformMotion` (only 7 callers, no external consumers).

## Technical Context

**Language/Version**: C# / .NET 10.0 (matches all prior specs).
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire, Syncfusion HTML-to-PDF (existing — vendored by spec 005), Tabler.io static-asset bundle (existing — vendored by spec 008), Fraunces / Inter / JetBrains Mono / canvas-confetti static assets (existing — vendored by spec 011). **Zero new managed dependencies.** **Zero new vendored static assets** (other than the new "Capital Semilla" wordmark SVG, which is a designer artifact, not a library).
**Storage**: SQL Server (Aspire-managed for dev, dacpac schema management). **No schema changes.** Local file system for PDFs (existing). **No new storage subsystems.**
**Testing**: Playwright for .NET (NUnit) for E2E (per constitution III); xUnit / NUnit for Unit and Integration. The E2E suite has 186 visible-text-dependent selectors across 20 test files plus 4 POMs — all in scope for translation.
**Target Platform**: Linux server (Aspire-orchestrated container stack); SQL Server container for dev, persistent SQL instance for prod. UI rendered in any modern browser; `<html lang="es-CR">` declared per NFR-006.
**Project Type**: Multi-project ASP.NET MVC web application orchestrated by .NET Aspire (Web + Application + Infrastructure + Domain + Database + AppHost + ServiceDefaults). Tests in three sibling projects (E2E + Integration + Unit).
**Performance Goals**: No measurable LCP / TBT regression vs. the spec 011 planning-day-1 baseline (NFR-005). If that baseline was not captured (open thread on spec #11), capture as planning day 1 of this spec.
**Constraints**: Single-locale `es-CR` pinned; code-stays-English seam (NFR-001); no swappable-locale machinery (NFR-003); no breaking change to the per-quotation Currency feature from spec 010; no regeneration of already-signed Funding Agreement PDFs (FR-022 / spec 006 immutability).
**Scale/Scope**: 72 Razor views (across 14 areas), 14 view-model files / 94 DataAnnotation attributes, 10 controllers / 52 user-facing string sites, 4 PDF Razor partials (~250 words copy), 18 status enum values, 28 Identity error codes (~22 actually surfaced), 186 E2E visible-text selectors (4 POMs). 7 brand-rename sites. ~50-100 unique Spanish strings in shared constants files. Single-merge sweep.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-evaluated post-design.*

| Principle | Pre-research evaluation | Post-design re-evaluation | Status |
|---|---|---|---|
| **I. Clean Architecture** | Touches Web (views, controllers, view models, registry), Infrastructure (Identity), and one cross-cutting middleware. No new layers; no inverted dependencies. | Confirmed: `EsCrIdentityErrorDescriber` lives in Infrastructure; `EsCrCultureFactory` lives in Web; `StatusVisualMap` stays in Web. No domain dependencies inverted. | ✅ PASS |
| **II. Rich Domain Model** | No domain entity changes; status enums remain code identifiers; their UI display stays in the presentation registry per spec 008. | Confirmed: zero domain edits. | ✅ PASS |
| **III. End-to-End Testing (NON-NEGOTIABLE)** | Spec Story 7 + FR-021 + SC-007 obligate Playwright coverage. Test count must not decrease; POM pattern preserved. | Confirmed: POM-first rewrite via `UiCopy` constants; new state-coverage tests for `StatusVisualMap` (data-model.md §2). E2E count goes UP (registry coverage + Identity error tests). | ✅ PASS |
| **IV. Schema-First Database Management** | No schema changes (declared in spec; reaffirmed in Out of Scope). | Confirmed: no `.sql` files modified, no EF migrations introduced, no `EnsureCreated` calls, no seed-data changes. data-model.md §"Schema-First Compliance" enumerates the zero-changes list. | ✅ PASS |
| **V. Specification-Driven Development** | This IS the SDD workflow: spec.md → plan.md → tasks.md → code. | Confirmed: every per-view rewrite PR will cite the voice-guide commit and the spec story it satisfies. | ✅ PASS |
| **VI. Simplicity & Progressive Complexity** | NFR-003 forbids future-localization scaffolding; sensible defaults pinned in middleware via constant; no abstractions beyond current need. | Confirmed: no new abstraction layers. The single new infrastructure file (`EsCrIdentityErrorDescriber`) is required by Identity's extension API; the single new Web helper (`EsCrCultureFactory`) builds the cloned + overridden `CultureInfo` once at startup. The `UiCopy` constants are tests-side only. | ✅ PASS |

**Tech-stack alignment**: ASP.NET MVC, EF Core, ASP.NET Identity, Playwright — all in-stack per the constitution Technology Standards table. Aspire-orchestrated stack unchanged. No new frameworks or NuGet packages introduced.

**Result**: All six principles pass on both passes. **No complexity violations.** The Complexity Tracking table is empty.

## Project Structure

### Documentation (this feature)

```text
specs/012-es-cr-localization/
├── plan.md                    # This file (/speckit-plan output)
├── spec.md                    # Spec from /speckit-specify
├── research.md                # Phase 0 output (this command)
├── data-model.md              # Phase 1 output (this command)
├── quickstart.md              # Phase 1 output (this command)
├── voice-guide.md             # First implementation commit (per FR-020 / SC-009)
├── REVIEW-SPEC.md             # From spex-gates-review-spec — SOUND
├── review_brief.md            # From speckit-spex-brainstorm
├── checklists/
│   └── requirements.md        # All items pass
└── tasks.md                   # NOT created by /speckit-plan; by /speckit-tasks
```

No `contracts/` directory — this feature exposes no external interfaces. The platform's HTTP routes are unchanged (URL slugs out of scope per FR / Out of Scope), no public APIs touched, no new MCP / RPC / GraphQL surfaces.

### Source Code (repository root)

```text
src/
├── FundingPlatform.AppHost/                        # Aspire orchestration
│   └── AppHost.cs                                  # MOD: localeCode default "es-CO" → "es-CR"
├── FundingPlatform.Application/
│   └── Options/
│       └── FunderOptions.cs                        # MOD: LocaleCode default flips
├── FundingPlatform.Database/                       # NO CHANGES (Schema-First)
├── FundingPlatform.Domain/                         # NO CHANGES (Rich Domain Model)
├── FundingPlatform.Infrastructure/
│   └── Identity/
│       └── EsCrIdentityErrorDescriber.cs           # NEW: Spanish IdentityErrorDescriber subclass
├── FundingPlatform.ServiceDefaults/                # NO CHANGES
└── FundingPlatform.Web/
    ├── Program.cs                                  # MOD: AddRequestLocalization, ModelBindingMessageProvider, AddErrorDescriber
    ├── Localization/
    │   └── EsCrCultureFactory.cs                   # NEW: clones + format-overrides es-CR CultureInfo
    ├── Helpers/
    │   └── StatusVisualMap.cs                      # MOD: 18 DisplayLabel values translate
    ├── Models/                                     # NO CHANGES (StatusVisual record shape preserved)
    ├── ViewModels/                                 # MOD: 14 files, 94 DataAnnotations get Spanish ErrorMessage / Display
    ├── Controllers/                                # MOD: 10 files, 52 user-facing strings translate
    ├── Views/
    │   ├── _ViewImports.cshtml                     # NO CHANGES
    │   ├── _ViewStart.cshtml                       # NO CHANGES
    │   ├── Shared/
    │   │   ├── _Layout.cshtml                      # MOD: <html lang>, brand link, footer copy + tagline
    │   │   ├── _AuthLayout.cshtml                  # MOD: <html lang>, copy
    │   │   ├── Error.cshtml                        # MOD: copy
    │   │   ├── Components/
    │   │   │   └── _StatusPill.cshtml              # NO CHANGES (consumes registry)
    │   │   └── _ValidationScriptsPartial.cshtml    # NO CHANGES (script-only)
    │   ├── Account/                                # MOD: 4 files (Login, Register, ChangePassword, AccessDenied)
    │   ├── Home/                                   # MOD: 2 files (applicant dashboard)
    │   ├── Application/                            # MOD: 4 files
    │   ├── Applications/                           # MOD: 1 file
    │   ├── Item/                                   # MOD: 3 files
    │   ├── Quotation/                              # MOD: 1 file
    │   ├── Supplier/                               # MOD: 1 file
    │   ├── ApplicantResponse/                      # MOD: 3 files
    │   ├── Review/                                 # MOD: 7 files
    │   ├── FundingAgreement/                       # MOD: 8 files (incl. 4 PDF partials — formal Spanish)
    │   └── Admin/                                  # MOD: 14 files
    ├── appsettings.json                            # MOD: LocaleCode "es-CO" → "es-CR"
    ├── appsettings.Development.json                # MOD: same
    └── wwwroot/
        ├── css/
        │   └── tokens.css                          # MOD: line 150 brand-comment cleanup
        ├── js/
        │   ├── motion.js                           # MOD: window.ForgeMotion → window.PlatformMotion
        │   └── facelift-init.js                    # MOD: 6 caller sites
        └── lib/
            └── brand/
                ├── wordmark.svg                    # MOD: <text>Forge</text> → <text>Capital Semilla</text>
                └── mark.svg                        # MOD: aria-label

tests/
├── FundingPlatform.Tests.E2E/
│   ├── Constants/
│   │   └── UiCopy.cs                               # NEW: ~50 unique Spanish strings (POM-consumed)
│   ├── PageObjects/
│   │   ├── AdminPage.cs                            # MOD: 4 properties → UiCopy.X
│   │   ├── ApplicationPage.cs                      # MOD: 3 properties
│   │   ├── ReviewApplicationPage.cs                # MOD: 2 properties
│   │   ├── ReviewQueuePage.cs                      # MOD: 1 property
│   │   └── (other POMs)                            # MOD: text-bearing properties → UiCopy.X
│   ├── Tests/                                      # MOD: ~70-80 visible-text assertions translated
│   ├── Fixtures/
│   │   └── AuthenticatedTestBase.cs                # MOD: 37× "Add Supplier" → UiCopy.AddSupplier
│   └── Helpers/                                    # MOD: assertion helpers if they hold copy
├── FundingPlatform.Tests.Integration/              # MOD: any integration test asserting on Spanish text
└── FundingPlatform.Tests.Unit/                     # NO CHANGES anticipated (unit tests don't render UI)
```

**Structure Decision**: The existing multi-project ASP.NET MVC + Aspire structure is preserved as-is. No new projects, no new test projects, no new namespaces. The two new files (`EsCrIdentityErrorDescriber.cs`, `EsCrCultureFactory.cs`, `UiCopy.cs`) slot into the existing layer conventions (Infrastructure / Web-helpers / Tests-constants).

## Phase 0: Outline & Research — Complete

See [research.md](./research.md) for the seven decisions:

1. **Format-locale override for `es-CR`** — clone CultureInfo, override `NumberFormatInfo` and `DateTimeFormatInfo` to deliver the spec's user-visible target.
2. **DataAnnotation translation surface** — 94 attributes / 14 files / all in `Web/ViewModels/`; explicit `ErrorMessage` per attribute.
3. **Controller-side user copy** — 52 sites across 10 controllers; inline Spanish translation; one `const` for the only triple-duplicate.
4. **E2E test impact** — 186 selectors across 20 test files; POM-first via `UiCopy` constants; ~62% leverage.
5. **Status registry** — `StatusVisualMap` is centralized; translate 18 `DisplayLabel` values; zero `.ToString()` bypasses to fix.
6. **Funding Agreement PDF** — 4 partials, ~250 words; existing culture-aware formatting helpers carry the load.
7. **Tabler vendor JS** — no user-facing copy; no override required.

All NEEDS CLARIFICATION items resolved. OQ-4 closes (no Tabler vendor work). OQ-8 closes (hard-pin culture in middleware via constant). OQ-9 recommendation locked (`PlatformMotion`). OQ-1, OQ-2, OQ-3, OQ-5, OQ-6, OQ-7 deferred to voice-guide authoring or implementation, with explicit recommendations recorded in research.

## Phase 1: Design & Contracts — Complete

See [data-model.md](./data-model.md) and [quickstart.md](./quickstart.md).

- **data-model.md**: zero domain entities; configuration-value updates enumerated; `StatusVisualMap` translation table; `EsCrIdentityErrorDescriber` method-by-method coverage; voice-guide artifact spec; `UiCopy` constants module.
- **quickstart.md**: day-1 setup; the three day-1 commits (voice guide, locale infrastructure, brand rename); locale verification probe; per-view sweep order; verification gates.
- **contracts/**: not applicable — no external interfaces.

The `update-agent-context.sh` script ran at the end of Phase 1 to refresh `CLAUDE.md` with the new spec's tech-stack annotations.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified.**

No violations. Table empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | — | — |
