# Feature Specification: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Feature Branch**: `012-es-cr-localization`
**Created**: 2026-04-29
**Status**: Draft
**Input**: User description: "Translate the entire user-facing surface to Costa Rican Spanish (es-CR) and rename the product from Forge to Capital Semilla."

## Overview

The platform has shipped English-only across 11 prior specs while deliberately leaving localization deferred (open thread carried forward from spec 008 and reaffirmed by spec 011). Spec 005 anchored the Funding Agreement PDF to a placeholder `es-CO` locale and never pinned a final code. This feature converts the deferred plan into shipped reality:

1. **Pin `es-CR`** as the platform's single, fixed locale (no toggle, no Accept-Language negotiation, no per-user preference).
2. **Translate all user-facing copy** across Razor views, partials, layouts, status registry, validation messages, Identity messages, the Funding Agreement PDF body, controller-emitted strings, ARIA / tooltip text, page titles, and the 9 empty-state illustrations' captions.
3. **Rename the product** from "Forge" to "Capital Semilla" across every user-visible surface: page titles, header brand link, footer copyright, brand wordmark SVG, brand mark aria-label.
4. **Apply formal `usted` register** with the warm-modern voice carried over from spec 011.
5. **Apply strict `es-CR` cultural conventions** for date and number formatting (`dd/MM/yyyy`; period decimal, comma thousands — `1,234.56`).
6. **Translate the E2E test suite's visible-text assertions** to match the new copy.

**The architectural rule is "code stays English."** Identifiers (variables, classes, methods, files), route paths, log messages, exception messages, code comments, JSON config keys, and DB schema all remain English. The seam is at the string boundary: any string a user can read is Spanish; everything else stays English. No `IStringLocalizer`, no `.resx` files, no swappable-locale machinery — Spanish is hard-coded inline at the same view-model / partial-parameter / registry seams that spec 008 deliberately designed for this future.

**Three open threads close with this spec**:
- Spec 005's "specific default locale code for LatAm formatting" pins to `es-CR`.
- Spec 008's "future spec (localization layer) — partials must be checked to ensure no UI copy was embedded" gets executed.
- Spec 011's "future spec 014 (localization layer) — voice-guide rewrites must remain partial-compatible" is carried out.

**Brand decision**: spec 011's open thread "Display brand name selection — Forge / Ascent / keep FundingPlatform — user sign-off gate" closes with **Capital Semilla** (literally, "seed capital" — apt for a funding/grants platform).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Voice Guide Lands First (Priority: P1)

As the spec author and any reviewer of per-view rewrites, I want a written voice guide and CR-Spanish term glossary to exist as a stable artifact before any view edits land, so that 70+ surfaces can be translated consistently against a single reference.

**Why this priority**: Prerequisite for every other story. Without a voice guide, per-view rewrites drift in tone, register, and term choice; rework cost is proportional to the surface area (large). With one, every reviewer (and any AI-assisted batch translation) has a single source of truth.

**Independent Test**: Open the new `voice-guide.md` artifact in the spec directory and verify it covers: (1) register (formal `usted`); (2) tone descriptors (warm, modern, concise, encouraging — mirror of spec 011's English voice); (3) a glossary of CR-specific term mappings for the platform's nouns and verbs (application, review, agreement, sign, send back, appeal, approve, reject, applicant, reviewer, approver, supplier, quotation, item, funding, etc.); (4) example before/after pairs covering at least the Login screen, an empty-state caption, a status pill string, a validation error, and a Funding Agreement PDF clause.

**Acceptance Scenarios**:

1. **Given** the voice guide artifact, **When** any reviewer opens it, **Then** they find an explicit register declaration ("formal `usted`, never `tú` / `vos`"), an explicit tone declaration matching spec 011's warm-modern descriptors, and a CR-specific note section.
2. **Given** the glossary, **When** a translator looks up "application", **Then** the chosen term (`solicitud`) and its rejected alternatives (`aplicación`) appear with a one-line rationale.
3. **Given** the example pairs, **When** any reviewer reads them, **Then** each pair shows the English source and the formal-usted Spanish target, and the chosen voice is recognizably warm but professional.
4. **Given** the voice guide is published, **When** any per-view rewrite PR opens, **Then** the diff is reviewable against the guide as the authoritative reference.

---

### User Story 2 — Spanish UI Across Authenticated Surfaces (Priority: P1)

As any logged-in user (applicant, reviewer, approver, admin), I want every page I see to render in formal Costa Rican Spanish, so that the platform feels native to my region rather than a translated foreign product.

**Why this priority**: The biggest surface and the highest-impact deliverable. Every authenticated view, partial, layout, empty-state caption, ARIA label, tooltip, and `<title>` flips from English to Spanish in this story. If only this story shipped, the platform would already feel native (with the caveat that PDFs and framework messages would still trail).

**Independent Test**: Log in as each role (applicant, reviewer, approver, admin) and walk every authenticated route. For each surface, confirm: (a) all visible copy is Spanish in formal-usted register; (b) the brand reads "Capital Semilla" (not "Forge"); (c) the HTML root element declares `lang="es-CR"`; (d) page titles render as `<page> - Capital Semilla`; (e) all ARIA labels and tooltips are Spanish.

**Acceptance Scenarios**:

1. **Given** an applicant lands on the home dashboard, **When** the page renders, **Then** all welcome hero, KPI labels, awaiting-action callout, application card mini-timelines, recent activity feed, and resources strip read in formal Spanish; no English fragment is visible to the eye or detectable by a regex sweep against an explicit allowlist (brand-adjacent technical IDs, currency symbols/codes).
2. **Given** any user views any of the 9 empty-state illustration scenes (spec 011), **When** the scene renders, **Then** the caption text is Spanish; if the SVG itself contains on-image English text, the auditor's findings are tracked as a designer follow-up but the spec ships with the placeholder caption text translated.
3. **Given** any user opens any view, **When** the page loads, **Then** the HTML root is `<html lang="es-CR">` and the page title is `<page> - Capital Semilla` with the em-dash separator preserved.
4. **Given** a reviewer hovers a journey-timeline node, **When** the tooltip appears, **Then** the tooltip text (timestamp + actor) renders with `dd/MM/yyyy` date format and a Spanish actor-relationship label.
5. **Given** any user views any view, **When** they inspect ARIA labels and `title` attributes via a screen reader or DevTools, **Then** every `aria-label`, `title`, and `alt` attribute is Spanish.
6. **Given** any user views any view containing a status pill, **When** the pill renders, **Then** the visible text is the Spanish display string from the `_StatusPill` registry; the underlying enum value (e.g., `ItemReviewStatus.UnderReview`) is unchanged.

---

### User Story 3 — Capital Semilla Brand Across All Surfaces (Priority: P1)

As any user (anonymous or authenticated), I want the product to consistently identify itself as "Capital Semilla" — never "Forge" — so that the platform feels like a coherent regional brand and not a half-renamed foreign product.

**Why this priority**: Brand consistency is binary: a single straggling "Forge" anywhere undermines the rebrand. The change touches few files but every surface. Equal-priority with the UI translation because mixed brand state would be more visible than missed Spanish copy.

**Independent Test**: Walk every authenticated view, the auth pages (Login, Register, ChangePassword, AccessDenied), the error page, and inspect the rendered footer + page title + header brand link + brand SVG wordmark + brand mark aria-label. Confirm no occurrence of "Forge" anywhere in user-rendered output. Confirm the JS namespace `window.ForgeMotion` no longer exists; `window.PlatformMotion` (or chosen replacement identifier) does, and its consumers in `facelift-init.js` reference the new name.

**Acceptance Scenarios**:

1. **Given** any user opens any page, **When** the layout renders, **Then** the page title is `<page> - Capital Semilla`, the header brand link reads "Capital Semilla", and the footer reads "© 2026 Capital Semilla · diseñado para emprendedores" (exact tagline copy decided in voice guide).
2. **Given** any user inspects the brand SVG wordmark in the rendered HTML, **When** they view its `<text>` element, **Then** it reads "Capital Semilla" (or — if the designer follow-up has not yet landed — a temporary text-only placeholder is in place that explicitly reads "Capital Semilla" rather than "Forge"; the visual mark file is tracked as an open follow-up).
3. **Given** any user inspects the brand mark SVG's accessibility metadata, **When** they read the `aria-label`, **Then** it reads "Capital Semilla mark" (or chosen Spanish equivalent in the voice guide).
4. **Given** any user opens DevTools and inspects the JavaScript globals, **When** they inspect `window`, **Then** `ForgeMotion` is `undefined` and the new identifier (`PlatformMotion`) is defined and functional.
5. **Given** any maintainer searches the codebase for `\bForge\b` excluding `AntiForgery`-family framework names and jQuery vendor comments, **When** the grep runs, **Then** zero results return inside user-facing files (.cshtml, served .svg, served .js, served .css). Code identifiers and framework symbols are exempt.

---

### User Story 4 — Funding Agreement PDF in Formal Spanish (Priority: P1)

As an applicant or funder receiving a generated Funding Agreement, I want the legal document to be drafted in formal Costa Rican Spanish with es-CR number/date formatting, so that I can read, understand, and sign a legally binding document in my language.

**Why this priority**: Legal-critical surface. A funding agreement signed by parties who can't fully read it is an enforceability and trust risk. Equal-priority with the UI translation because the PDF is the artifact users carry away from the platform.

**Independent Test**: Trigger Funding Agreement generation for an unsigned application and inspect the resulting PDF. Confirm: (a) every line of body copy is in formal Spanish (formal `usted`, legal/business register tuned in voice guide); (b) all numeric values render as `1,234.56` (period decimal, comma thousands); (c) all dates render as `dd/MM/yyyy`; (d) currency renders per the per-quotation Currency code introduced in spec 010 (e.g., `₡1,234.56` for CRC, `$1,234.56` for USD); (e) the configured `LocaleCode` reads `es-CR`. Then inspect a previously-signed PDF (spec 006 lock) and confirm it is unchanged.

**Acceptance Scenarios**:

1. **Given** an unsigned application's Funding Agreement is regenerated post-deployment, **When** the PDF is produced, **Then** every clause body, every header, every signature-block label, and every footer line renders in formal Spanish.
2. **Given** the regenerated PDF, **When** any numeric amount is rendered, **Then** the format is `1,234.56` (period decimal, comma thousands) — `es-CR` strict, NOT spec 005's prior `es-CO` `1.234,56` style.
3. **Given** the regenerated PDF, **When** any date is rendered, **Then** the format is `dd/MM/yyyy`.
4. **Given** an applicant previously signed an agreement that was generated under English copy, **When** any user inspects that PDF artifact post-deployment, **Then** the artifact is byte-identical to its pre-deployment state (spec 006 immutability is honored).
5. **Given** the platform configuration is read at startup, **When** `FundingAgreement:LocaleCode` is read, **Then** the value is `es-CR` in `appsettings.json`, `appsettings.Development.json`, and the `FunderOptions` / `FundingAgreementController` defaults.
6. **Given** a draft PDF was generated under the old `es-CO` config and the corresponding application has not yet been signed, **When** the applicant or reviewer triggers a regeneration, **Then** the new PDF replaces the draft and renders under `es-CR` in Spanish (spec 005's regeneration semantics — the latest version wins until first signature).

---

### User Story 5 — Validation, Identity, and Framework Messages in Spanish (Priority: P2)

As any user submitting a form or hitting an Identity boundary (login failure, lockout, password too short, duplicate email), I want the error message I see to be in Spanish, so that the platform doesn't drop me out of its native language at the moments where I most need clarity.

**Why this priority**: Without this story, users see Spanish UI chrome around English error messages — the worst kind of mixed-language UX. Lower priority than the visual sweep only because the surface is much smaller and the implementation seams are cleaner (a few framework extension points vs. 70+ Razor files).

**Independent Test**: Submit each of: (a) a `[Required]`-marked form field empty; (b) a `[StringLength]`-violating input; (c) a model-binding type mismatch (e.g., text in a numeric field); (d) a failing login; (e) an account lockout; (f) a too-short password during registration; (g) a duplicate email registration. Confirm every resulting message is Spanish.

**Acceptance Scenarios**:

1. **Given** any view model with `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, or similar DataAnnotation attributes, **When** the form is submitted with violating input, **Then** the rendered validation message is the attribute's explicit Spanish `ErrorMessage` (or, if absent, the framework's now-Spanish `ModelBindingMessageProvider` default).
2. **Given** any model-binding-driven error (e.g., posting non-numeric text into a `decimal` field), **When** the binder fails, **Then** the surfaced message is Spanish via the configured `MvcOptions.ModelBindingMessageProvider`.
3. **Given** an Identity error code (login failure, lockout, password too short, password missing required character class, duplicate username, duplicate email, etc.), **When** the framework surfaces the error, **Then** the message text is the Spanish description provided by the custom `IdentityErrorDescriber` subclass.
4. **Given** any controller emits a `TempData["SuccessMessage"]` or `TempData["ErrorMessage"]`, **When** the next view renders the flash, **Then** the message body is Spanish; the dictionary key (`"SuccessMessage"` / `"ErrorMessage"`) remains English (it is a code identifier, not user-facing).
5. **Given** any unhandled exception triggers `Views/Shared/Error.cshtml`, **When** the page renders, **Then** the user-facing error text is Spanish; in dev mode only, the technical correlation IDs and stack traces (English by nature) may appear alongside.

---

### User Story 6 — Status & Journey Display Renders Spanish (Priority: P2)

As any user, I want every status pill, journey-timeline label, and stage indicator across the platform to render Spanish from a single source of truth, so that the lifecycle vocabulary is consistent everywhere it appears.

**Why this priority**: Single registry, very high reuse (every dashboard, every queue, every detail page, every PDF status reference). Cheap to translate (one file) but high-visibility — leaving even one stage in English would be visible across the entire app. P2 because it is mechanically subsumed by Story 2 and Story 4 but worth calling out as a discrete acceptance step.

**Independent Test**: For each enum (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`, plus the journey-stage labels added in spec 011), enumerate all values and confirm the `_StatusPill` / journey-label registry returns a Spanish display string for each. Render every enum value via the registry (test fixture covering 100% of enum values) and confirm no English string is returned.

**Acceptance Scenarios**:

1. **Given** any value of `ApplicationState`, `ItemReviewStatus`, `AppealStatus`, or `SignedUploadStatus`, **When** the registry is queried for its display string, **Then** a non-empty Spanish string is returned.
2. **Given** the journey-timeline labels (Draft / Submitted / Under Review / Decision / Agreement Generated / Signed / Funded, plus branch states "Sent back", "Rejected", "Appeal"), **When** any timeline (mainline, mini, micro) renders, **Then** all labels render in Spanish (e.g., "Borrador" / "Enviada" / "En revisión" / "Decisión" / "Convenio generado" / "Firmada" / "Financiada"; final term choices set in voice guide glossary).
3. **Given** the Funding Agreement PDF references a state, **When** the PDF body renders, **Then** the state text is sourced through the same registry as the UI (single source of truth).
4. **Given** any caller in the codebase, **When** the static analysis search runs for `enum.ToString()` reaching a UI surface, **Then** zero matches are found — every enum-to-UI conversion routes through the display registry.

---

### User Story 7 — E2E Test Suite Reflects New Copy (Priority: P2)

As a developer maintaining the platform, I want the E2E test suite to assert against Spanish text on the rendered UI, so that the test contract matches production behavior and tests stay honest.

**Why this priority**: Without this story, every E2E test that asserts on visible English text will fail post-deployment. Required for green CI. Lower than the user-facing stories because it is a maintenance task, not a user-facing capability.

**Independent Test**: Run the full Playwright E2E test suite against the deployed Spanish build. All tests pass green. Test count does not decrease (no test deleted to mask copy mismatches). Selector strategy follows spec 011's open-thread closure: visible-text assertions in Spanish are the contract.

**Acceptance Scenarios**:

1. **Given** the full E2E suite, **When** it runs against the Spanish build, **Then** every test passes; no test was skipped, deleted, or marked `flaky` to silence a Spanish-vs-English mismatch.
2. **Given** any test asserts on visible text, **When** the assertion runs, **Then** the asserted string is in Spanish and matches the production copy verbatim.
3. **Given** the suite previously had N tests, **When** the suite is re-counted post-translation, **Then** the new count is at least N (deletions only allowed if accompanied by a documented justification in the PR).

---

### Edge Cases

- **EC-1. Already-signed Funding Agreements** (spec 006 immutable lock): The signed PDF artifact is byte-frozen and never regenerated. Stays in its original-deployment language. Reviewer surfaces displaying metadata about the signed PDF (e.g., "Signed on 2026-04-15") render Spanish chrome around the immutable English artifact link/preview.
- **EC-2. Mid-flight applications**: applicant-entered free-text content (project name, description, supplier names, message bodies, appeal text) remains in whatever language the applicant typed. UI chrome around it is Spanish. Expected and correct — the platform does not translate user content.
- **EC-3. Per-quotation currency vs. locale**: spec 010's per-quotation Currency code controls the currency symbol and ISO code; `es-CR` only governs date and number-separator formatting. CRC under `es-CR` renders `₡1,234.56`; USD renders `$1,234.56`; GBP renders `£1,234.56`. No conflict between the per-quotation Currency feature and the locale.
- **EC-4. Empty-state SVG illustrations** (9 from spec 011): if any SVG file embeds rendered text glyphs (vs. caption text in the surrounding view), a designer pass is required to produce a Spanish version. Caption text in Razor is translated regardless. The audit step is a planning task; if the audit finds embedded text, the spec ships with the captions translated and tracks the SVG rework as an open designer follow-up.
- **EC-5. Tabler-vendored JS strings**: Tabler.io is primarily CSS but a few in-use components (modals, tooltips, possibly date pickers) may carry built-in copy. Audit step in plan; if found, override at the component-instantiation layer rather than fork the vendor.
- **EC-6. Browser-supplied strings**: native autocomplete suggestions, native form-validation tooltips (when bypassing server validation), browser context menus, Save/Print dialogs — these honor the user's browser language, not the page `lang` attribute. Out of our control; not a regression. The `lang="es-CR"` declaration encourages browsers and assistive tech to behave appropriately.
- **EC-7. Brand wordmark for two-word brand**: "Capital Semilla" is two words; the existing single-word "Forge" wordmark may not reproportion cleanly. A designer pass produces the new wordmark. If the designer pass has not landed by spec implementation time, a textual placeholder reading "Capital Semilla" in the brand display font (Fraunces, vendored by spec 011) is acceptable as a temporary state — the open follow-up is tracked.
- **EC-8. `tokens.css` brand-reference comment** (line 150, "inherits the Forge palette"): a code comment, not user-facing, but a stale brand reference that should be cleaned to read "inherits the Capital Semilla palette" or genericized to "the platform palette" — single-line cleanup, scoped to this spec.
- **EC-9. Spanish text length expansion**: Spanish averages ~25% longer than English. Tight layouts (button labels, table column headers, status pills, KPI strip labels) may overflow or wrap. Visual review per surface. Tabler responsive utilities handle most cases; specific overflows are tightened layout-by-layout during the sweep.
- **EC-10. Dynamic format-string placeholders**: format strings with placeholders (`"Submitted on {date}"`) may need word-order changes in Spanish (`"Enviada el {date}"`). Every translated format string is validated against its placeholder set at translation time; tests exercise placeholder-bearing strings end-to-end.
- **EC-11. Configuration override of `LocaleCode`**: an operator could theoretically override `FundingAgreement:LocaleCode` at deploy. The spec recommends pinning the request culture in middleware via a constant (not a config read), so even if `LocaleCode` is overridden, the UI culture remains `es-CR`. The `LocaleCode` config remains overridable for the PDF generator but emits a startup WARN log if it diverges from the request culture.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The platform MUST pin a single fixed locale of `es-CR` for every authenticated and unauthenticated request via `RequestLocalization` middleware. There MUST be no language toggle, no Accept-Language negotiation, and no per-user locale preference.
- **FR-002**: Every Razor view (`.cshtml` file under `src/FundingPlatform.Web/Views/`), every shared partial, and every layout MUST render its visible copy in formal Costa Rican Spanish (formal `usted`, warm-modern voice consistent with spec 011's voice guide).
- **FR-003**: The HTML root element MUST declare `lang="es-CR"` on every rendered page including auth, error, and any anonymous-accessible pages.
- **FR-004**: Every `aria-label`, `title`, `alt`, and `placeholder` attribute on user-facing controls MUST be Spanish.
- **FR-005**: The page-title pattern MUST be `<page-specific title> - Capital Semilla` with em-dash separator preserved.
- **FR-006**: The brand name "Forge" MUST be removed from every user-rendered surface and replaced with "Capital Semilla". This includes: the `<title>` suffix, the header brand link text, the footer copyright line, the brand wordmark SVG `<text>` content, the brand mark SVG `aria-label`, and the footer tagline (translated to Spanish — exact phrasing decided in voice guide).
- **FR-007**: The JavaScript namespace `window.ForgeMotion` MUST be renamed to a brand-neutral identifier (`window.PlatformMotion` recommended) in `motion.js` (definition site) and `facelift-init.js` (six callers). No backwards-compatible alias is exposed.
- **FR-008**: The `tokens.css` code comment "inherits the Forge palette" MUST be updated to remove the dead brand reference.
- **FR-009**: The `_StatusPill` registry (spec 008) MUST return Spanish display strings for every value of every domain enum it covers (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`). The journey-stage labels added in spec 011 (Draft / Submitted / Under Review / Decision / Agreement Generated / Signed / Funded, plus branch states) MUST also return Spanish display strings.
- **FR-010**: No code path MUST cause an `enum.ToString()` value to reach a user-facing surface. All enum-to-display conversions MUST go through the registry.
- **FR-011**: Every DataAnnotation attribute on view models (`[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[Compare]`, `[RegularExpression]`, etc.) that exposes a user-visible message MUST carry an explicit Spanish `ErrorMessage`.
- **FR-012**: ASP.NET MVC's `MvcOptions.ModelBindingMessageProvider` MUST be configured with Spanish providers covering type-mismatch, missing-required-field, value-must-not-be-null, and unknown-value-is-invalid messages.
- **FR-013**: A custom `IdentityErrorDescriber` subclass MUST be registered in DI providing Spanish text for every Identity error code (login failure, lockout, password length, password complexity classes, duplicate username, duplicate email, invalid token, etc.).
- **FR-014**: Every TempData success/error message string emitted by any controller MUST be Spanish. The TempData dictionary keys remain English.
- **FR-015**: The Funding Agreement PDF Razor template MUST render its body in formal Spanish (legal/business register, formal `usted`, exact terminology fixed by voice guide glossary).
- **FR-016**: The `FundingAgreement:LocaleCode` configuration value MUST be `es-CR` in `appsettings.json`, `appsettings.Development.json`, and the `FunderOptions` / `FundingAgreementController` defaults.
- **FR-017**: Number formatting MUST use period decimal and comma thousands (`1,234.56`) per `es-CR` strict CultureInfo defaults.
- **FR-018**: Date formatting MUST use `dd/MM/yyyy` per `es-CR` strict CultureInfo defaults.
- **FR-019**: Currency display MUST continue to be driven by the per-quotation Currency code introduced in spec 010 (currency symbol and ISO code render per the quotation; locale only governs separator and date format).
- **FR-020**: A voice-guide artifact (`voice-guide.md` in the spec directory) MUST be authored and committed before any per-view rewrite lands in the diff. It MUST cover: register declaration, tone descriptors, CR-specific term glossary, and example pairs.
- **FR-021**: Every Playwright E2E test asserting on visible text MUST be updated to assert on Spanish text matching production copy verbatim. No test MUST be deleted or skipped to silence a Spanish-vs-English mismatch.
- **FR-022**: Already-signed Funding Agreement PDF artifacts MUST remain byte-identical to their pre-deployment state. Spec 006's immutability rule is honored.
- **FR-023**: The 9 empty-state SVG illustrations from spec 011 MUST be audited for on-image English text. Caption text in Razor is translated regardless. SVGs containing on-image text are tracked as designer follow-ups; the spec MAY ship before designer rework lands provided FR-006 (brand text in wordmark SVG) is satisfied with at minimum a textual "Capital Semilla" rendering.
- **FR-024**: Tabler.io vendored JS components in active use MUST be audited for built-in English strings. If any are found, they MUST be overridden at the component instantiation layer (no vendor fork).
- **FR-025**: CSV export functionality (spec 010) MUST continue to format numeric and date values via `CultureInfo.InvariantCulture` for machine-readability. CSV exports are NOT translated.

### Non-Functional Requirements

- **NFR-001 (Code-stays-English)**: No Spanish text MUST appear in code identifiers (variable / class / method / file names), route paths, JSON config keys, log messages, exception messages, code comments (other than the one-line `tokens.css` brand-reference cleanup), or DB schema (table / column / view / sproc / dacpac SQL). The seam is at the user-facing string boundary; everything else is English.
- **NFR-002 (No mixed-language UI)**: No view, partial, or layout MUST simultaneously display Spanish and English copy as part of its intended chrome. Detection is mechanical: a regex sweep flags any user-rendered string token matching `\b[A-Z][a-z]{3,}\b` outside an explicit allowlist (brand names, currency symbols and ISO codes, technical IDs, AntiForgery tokens). Visual review per surface is the second gate.
- **NFR-003 (No future-localization scaffolding)**: The implementation MUST NOT introduce `IStringLocalizer`, `.resx` files, or any other swappable-locale machinery that would have to be undone or reworked if a future spec ever revisits multi-language support. The seam structure (view-model display props, partial parameters, status registry) inherited from specs 008 / 011 is the only mechanism used; Spanish is hard-coded inline at those seams.
- **NFR-004 (Future-localization neutrality)**: The implementation MUST NOT re-embed copy into shared partials in ways that would block a hypothetical future i18n pass. (Equivalent to spec 011's compatibility caveat — the seam quality from spec 008 is preserved.)
- **NFR-005 (Performance)**: No measurable LCP / TBT regression vs. spec 011's baseline. (If spec 011's planning-day-1 baseline was not captured — open thread on #11 — capture it as planning-day-1 of this spec instead.)
- **NFR-006 (Accessibility)**: HTML `lang="es-CR"` declared correctly; all `aria-label`, `title`, and `alt` attributes translated; screen readers announce Spanish on Spanish content. No new accessibility regressions vs. the spec 011 baseline.
- **NFR-007 (Test stability)**: After E2E test rewrites land, the suite passes green at the same or higher count than pre-translation. Selector strategy adopts visible-text assertions in Spanish as the contract (closes spec 011's selector-strategy open thread).
- **NFR-008 (Locale enforcement)**: Request culture is pinned via a constant in middleware (not a config read), so an operational override of any locale-related config cannot diverge the UI from `es-CR`. The PDF generator's `LocaleCode` config remains overridable but emits a startup WARN if it diverges from the request culture.

### Key Entities

This feature does not introduce new persistence-bound entities. It does, however, formalize one in-spec artifact:

- **Voice Guide** (`specs/012-es-cr-localization/voice-guide.md`): the canonical reference for register, tone, CR-specific term mappings, and example pairs. Authored before per-view rewrites; referenced by every translation reviewer; lives in the spec directory permanently as the source of truth for any future copy edit.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 (Spanish UI sweep)**: Every Razor view, partial, and layout under `src/FundingPlatform.Web/Views/` renders Spanish on visit. Verified by: a Spanish-speaking reviewer's full-app walkthrough plus a regex sweep that flags user-rendered tokens matching `\b[A-Z][a-z]{3,}\b` outside the explicit allowlist (brand names, currency symbols and ISO codes, technical IDs).
- **SC-002 (Brand sweep)**: A grep for `\bForge\b` in user-facing output (rendered HTML, served SVGs, served JS globals, served CSS — excluding `AntiForgery`-family framework symbols and jQuery vendor comments) returns zero results. The new brand "Capital Semilla" appears at all expected surfaces.
- **SC-003 (Validation messages)**: Submitting any form with invalid input shows a Spanish validation message — for both DataAnnotation-driven errors and model-binding-generated errors. Verified by: an E2E test per primary attribute family.
- **SC-004 (Identity messages)**: Failing login, locked-out account, password-too-short, password-missing-required-class, duplicate-email registration, and invalid token surface a Spanish Identity error message. Verified by: an E2E test per Identity error code.
- **SC-005 (PDF Spanish)**: A newly generated Funding Agreement PDF renders fully in formal Spanish with `es-CR` formatting (`1,234.56`, `29/04/2026`). A previously-signed agreement remains byte-identical to its pre-deployment state.
- **SC-006 (`lang` declaration)**: The HTML root element declares `lang="es-CR"` on every rendered page including auth and error pages. Verified by: a static check on `_Layout.cshtml` and `_AuthLayout.cshtml`.
- **SC-007 (Test green)**: Full Playwright E2E suite passes against the Spanish build. Test count is the same or higher than pre-translation.
- **SC-008 (Code-stays-English)**: No log line emitted by the platform during an end-to-end happy-path application flow contains non-ASCII characters attributable to Spanish UI copy. Confirms the code-stays-English boundary holds.
- **SC-009 (Voice guide first)**: The `voice-guide.md` artifact is committed in a commit that lands BEFORE any per-view rewrite commit. Reviewable as a standalone artifact.
- **SC-010 (Open threads close)**: Three open threads close: spec 005's locale-pinning thread (pinned to `es-CR`), spec 008's localization deferral thread (executed), spec 011's voice-guide-drift compatibility caveat (validated). Spec 011's brand sign-off open thread closes with "Capital Semilla". All four closures are reflected in `brainstorm/00-overview.md`.
- **SC-011 (Status registry coverage)**: A static-analysis check confirms every value of every domain enum (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`) returns a non-empty Spanish display string from the registry. A static-analysis check confirms zero `enum.ToString()` calls reach a user-facing rendering surface.
- **SC-012 (Performance)**: No measurable LCP / TBT regression vs. the spec 011 baseline. Captured pre/post by the same harness.

## Assumptions

- The `es-CR` `CultureInfo` shipped with .NET correctly represents Costa Rican conventions (period decimal, comma thousands, `dd/MM/yyyy`). If a planning-time investigation reveals quirks, the spec's format-pinning may need an explicit `NumberFormatInfo` / `DateTimeFormatInfo` override, but the user-visible target stays the same.
- Spec 011's voice guide is the source-of-truth for the warm-modern English tone; the new Spanish voice guide mirrors its tone descriptors faithfully and only varies in register and terminology.
- The platform has no email-sending capability today (verified in brainstorm: no `IEmailSender`, no SMTP wiring); future emails are out of scope for this spec.
- The Tabler.io vendored asset bundle (spec 008) and the Fraunces / Inter / JetBrains Mono / canvas-confetti vendored assets (spec 011) are unchanged by this spec.
- The Syncfusion HTML-to-PDF renderer (vendored by spec 005) renders correctly under `es-CR` `CultureInfo`. The shift in number-separator from `1.234,56` (es-CO) to `1,234.56` (es-CR) is the regression risk to verify in implementation.
- The brand SVG wordmark and brand SVG mark are produced by a designer follow-up; if the rework has not landed by implementation time, a textual placeholder reading "Capital Semilla" in the Fraunces display font is acceptable as a temporary state.
- E2E tests live under `tests/` and use Playwright with a mix of role / aria / text-based selectors. The translation rewrite touches text-based assertions; role/aria assertions are unaffected. Spec 011's selector-strategy open thread is closed by treating Spanish visible-text assertions as the contract going forward.
- The platform serves a single CR-region deployment; if a future deployment needs another region or a multi-language version, that is a future spec's concern and explicitly out of NFR-003 scope.

## Dependencies

- **Spec 005 (Funding Agreement PDF)** — direct touch. Razor template body translated. `LocaleCode` config flips from `es-CO` to `es-CR`. Defaults in `FunderOptions` and `FundingAgreementController` updated. Spec 005's "specific default locale code" open thread closes.
- **Spec 006 (Digital signatures)** — read-only respect of immutability. Already-signed PDFs are not regenerated.
- **Spec 008 (Tabler UI migration)** — direct touch. The `_StatusPill` registry returns Spanish strings. Spec 008's localization-deferral thread closes. The "partial copy embedding" check is executed.
- **Spec 010 (Admin reports + currency rollout)** — coexists. CSV exports stay `InvariantCulture`. Per-quotation Currency rendering is unchanged.
- **Spec 011 (Warm-modern facelift)** — direct touch. Voice-guide source for tone. Journey-stage labels translated. Empty-state SVG audit. The "voice-guide rewrites must remain partial-compatible" caveat closes. Brand sign-off open thread closes with "Capital Semilla".
- **ASP.NET Core Localization framework** — minimal use. Only `RequestLocalization` middleware to pin culture. No `IStringLocalizer`, no resource files.
- **ASP.NET Identity** — extension point: a custom `IdentityErrorDescriber` subclass for Spanish error messages.
- **ASP.NET MVC** — extension point: `MvcOptions.ModelBindingMessageProvider` configured with Spanish providers.
- **Syncfusion HTML-to-PDF** — verify `es-CR` rendering correctness as a regression-risk check during implementation.
- **Designer follow-up (out-of-band)** — new "Capital Semilla" wordmark SVG and on-image-text audit on the 9 empty-state illustrations.

## Out of Scope

- **Multi-language support**: no language toggle, no Accept-Language negotiation, no per-user locale preference, no resx infrastructure that would need to be undone if a future spec ever adds it. NFR-003 is binding.
- **Translation of code-side strings**: identifiers (variables, classes, methods, files), route paths, log messages, exception messages, JSON config keys, DB schema/column names, dacpac SQL — all stay English.
- **Translation of CSV exports** (spec 010): continue using `CultureInfo.InvariantCulture` for machine-readable output.
- **Re-translation of already-signed Funding Agreement PDFs**: immutable per spec 006. Stay frozen in their original-deployment language.
- **Translation of applicant-entered free-text content**: project descriptions, supplier names, message bodies, appeal text remain as the applicant typed them.
- **Translation of seed/enum identifier names**: e.g., `ApplicationState.UnderReview` stays as a code symbol; only its UI display lookup is Spanish.
- **Public marketing surface** (future spec 015): out-of-band; this spec is authenticated-only plus the auth pages (Login, Register, ChangePassword, AccessDenied) and the generic Error page.
- **Future communication / messaging surface** (future spec 013) and **notifications/inbox** (future spec 014): both untouched by this spec; their future specs will produce their own copy in Spanish from the start.
- **Brand mark / logo redesign** beyond the wordmark text swap: if the visual mark needs structural rework for the two-word "Capital Semilla", that is a designer follow-up tracked in the spec's open follow-ups, not a spec deliverable.
- **JavaScript namespace alias for backward compatibility**: `window.ForgeMotion` is removed as a hard cut. No alias is left in place.

## Open Questions

- **OQ-1 (Glossary finalization)**: Several technical terms admit multiple acceptable Spanish renderings (e.g., "application" → `solicitud` vs `aplicación`; "review" → `revisión` vs `evaluación`; "funding agreement" → `convenio de financiamiento` vs `acuerdo de financiación`; "send back" → `devolver` vs `regresar`). The voice guide owns the glossary; final term choices settle there during planning.
- **OQ-2 (Footer tagline)**: The English "built for entrepreneurs" tagline maps to a formal-usted Spanish equivalent; recommended `diseñado para emprendedores`, but the exact phrasing is a voice-guide decision.
- **OQ-3 (Designer SVG follow-ups)**: Two open follow-ups: (a) the new "Capital Semilla" wordmark SVG; (b) on-image text audit and rework on the 9 empty-state illustrations. Pin during planning whether both block the spec's merge or one ships as a placeholder per FR-023 and EC-7.
- **OQ-4 (Tabler vendor JS string audit)**: Whether any in-use Tabler JS components carry built-in copy. Audit step in plan; if found, override at component layer.
- **OQ-5 (Performance baseline)**: NFR-005 references spec 011's planning-day-1 LCP/TBT baseline. If that baseline was not captured (open thread on #11), capture it as planning-day-1 of this spec.
- **OQ-6 (Voice-guide reviewer)**: Who validates the Spanish voice guide before per-view rewrites land — same designer/voice owner as spec 011, or a new CR-region reviewer? Settle during planning.
- **OQ-7 (Page-title pattern direction)**: `[Page] - Capital Semilla` is recommended (matches today's pattern). Confirm during planning whether CR convention prefers brand-leading or page-leading.
- **OQ-8 (Hard-pin vs config override of culture)**: Recommend hard-pinning the request culture via a constant in middleware (not a config read), with `FundingAgreement:LocaleCode` config remaining overridable but warned-on-divergence at startup. Confirm during planning whether ops needs a hatch.
- **OQ-9 (`PlatformMotion` rename — final identifier name)**: `PlatformMotion` is recommended as the brand-neutral replacement for `ForgeMotion`. Confirm during planning whether a different identifier (e.g., `AppMotion`, `SeedMotion`) is preferred.
