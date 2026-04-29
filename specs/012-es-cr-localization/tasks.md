# Tasks: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Input**: Design documents from `/specs/012-es-cr-localization/`
**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: This spec deliberately rewrites the existing E2E test suite (US7) to track the translation; new test tasks land inside US7 (visible-text assertion translation) and US6 (status-registry coverage test). No additional test scope is added — the constitution requires E2E tests for every story (Principle III), satisfied by the existing suite plus US6's coverage test.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. The brainstorm chose a single coordinated sweep (Approach A), so all stories merge together at the end, but each story's tasks remain individually testable per the spec's Independent Test criteria.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks).
- **[Story]**: Which user story this task belongs to (e.g., US1–US7).
- All file paths are relative to repo root `/mnt/D/repos/bds-ps`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the pre-translation baseline and verify the locale-probe finding from research Decision 1 still holds before any production code changes.

- [ ] T001 Verify the `012-es-cr-localization` feature branch is checked out and the working tree is clean. Run `git status` and `git rev-parse --abbrev-ref HEAD`; abort if either is unexpected.
- [ ] T002 [P] Capture the pre-translation Lighthouse baseline (LCP and TBT) for the applicant home dashboard, the reviewer queue dashboard, and the signing ceremony surface. Save the report under `specs/012-es-cr-localization/perf-baseline-pre.json` (or `.md` if Lighthouse JSON is too verbose) so post-translation comparison has a fixed reference (NFR-005 / SC-012 / OQ-5).
- [ ] T003 [P] Re-run the `es-CR` CultureInfo probe described in [quickstart.md](./quickstart.md). Confirm that .NET defaults still produce comma-decimal / space-thousands / `d/M/yyyy` (matching research Decision 1). If the OS / .NET / ICU version has shifted defaults, the override pattern in T004 is still required but the override values may need to be revisited.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Wire the locale runtime so every subsequent rendering path sees `es-CR` and the correct format overrides. Wire the framework-message extension points so US5's per-attribute work has hooks. **No copy translation in this phase.**

**⚠️ CRITICAL**: All tasks here MUST land before any per-story copy translation (US2, US4, US5, US6) can be reliably verified in a running app.

- [ ] T004 Create `src/FundingPlatform.Web/Localization/EsCrCultureFactory.cs` exposing `public static CultureInfo Build()` that clones `CultureInfo.GetCultureInfo("es-CR")` and overrides: `NumberFormat.NumberDecimalSeparator = "."`, `NumberFormat.NumberGroupSeparator = ","`, `NumberFormat.CurrencyDecimalSeparator = "."`, `NumberFormat.CurrencyGroupSeparator = ","`, `DateTimeFormat.ShortDatePattern = "dd/MM/yyyy"`. Mark the returned `CultureInfo` read-only via `CultureInfo.ReadOnly(...)` to prevent accidental mutation downstream.
- [ ] T005 Wire `RequestLocalization` middleware in `src/FundingPlatform.Web/Program.cs`. Use `EsCrCultureFactory.Build()` for `DefaultRequestCulture`, `SupportedCultures`, and `SupportedUICultures`. Call `o.RequestCultureProviders.Clear()` to disable Accept-Language negotiation. Call `app.UseRequestLocalization()` after `app.UseRouting()` and before `app.UseAuthentication()`.
- [ ] T006 Configure `MvcOptions.ModelBindingMessageProvider` Spanish accessors inside the existing `builder.Services.AddControllersWithViews(...)` call in `src/FundingPlatform.Web/Program.cs`. Cover all eight provider methods: `SetValueIsInvalidAccessor`, `SetMissingBindRequiredValueAccessor`, `SetMissingKeyOrValueAccessor`, `SetMissingRequestBodyRequiredValueAccessor`, `SetValueMustNotBeNullAccessor`, `SetAttemptedValueIsInvalidAccessor`, `SetUnknownValueIsInvalidAccessor`, `SetValueMustBeANumberAccessor`. Use voice-guide-aligned formal Spanish strings.
- [ ] T007 Create `src/FundingPlatform.Infrastructure/Identity/EsCrIdentityErrorDescriber.cs` extending `Microsoft.AspNetCore.Identity.IdentityErrorDescriber`. Override every method enumerated in [data-model.md §3](./data-model.md). Place the new class in namespace `FundingPlatform.Infrastructure.Identity`.
- [ ] T008 Register the describer in `src/FundingPlatform.Web/Program.cs` by appending `.AddErrorDescriber<EsCrIdentityErrorDescriber>()` to the `AddIdentity<...>().AddEntityFrameworkStores<...>().AddUserStore<SentinelAwareUserStore>()` chain (insert before `AddDefaultTokenProviders()`). Add the `using FundingPlatform.Infrastructure.Identity;` import.
- [ ] T009 Add `lang="es-CR"` to the `<html>` element in `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` and `src/FundingPlatform.Web/Views/Shared/_AuthLayout.cshtml`. **Do not** translate any other copy in this task — it lands in US2 and US3.
- [ ] T010 Flip `FundingAgreement:LocaleCode` value from `"es-CO"` to `"es-CR"` in `src/FundingPlatform.Web/appsettings.json` and `src/FundingPlatform.Web/appsettings.Development.json`.
- [ ] T011 Flip the default `LocaleCode` from `"es-CO"` to `"es-CR"` in `src/FundingPlatform.Application/Options/FunderOptions.cs`. Also update the fallback default in `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` (the `string.IsNullOrWhiteSpace(options.LocaleCode) ? "es-CO" : options.LocaleCode` line) and in `src/FundingPlatform.Web/ViewModels/FundingAgreementDocumentViewModel.cs` (the field default).
- [ ] T012 Flip the `localeCode` default from `"es-CO"` to `"es-CR"` in `src/FundingPlatform.AppHost/AppHost.cs`.
- [ ] T013 Add a startup WARN log in `Program.cs` (per NFR-008) when the read `FundingAgreement:LocaleCode` value diverges from the request culture name (`es-CR`). Log via `ILogger<Program>` at WARN level, message in English (per NFR-001).
- [ ] T014 Build smoke check: `dotnet build` returns green; `dotnet run --project src/FundingPlatform.AppHost` starts the Aspire stack; open the home page and confirm via browser DevTools that `<html lang="es-CR">` is set and that any rendered date or number on the page uses `dd/MM/yyyy` and `1,234.56` formatting (NB: copy is still English at this point — that's expected).

**Checkpoint**: Locale runtime wired; framework hooks ready. User-story phases can now begin (US1 first per spec).

---

## Phase 3: User Story 1 — Voice Guide Lands First (Priority: P1) 🎯 PREREQUISITE

**Goal**: Author the voice-guide artifact that every subsequent translation references. Per FR-020 / SC-009, the voice guide MUST commit BEFORE any per-view rewrite commit.

**Independent Test**: Open `specs/012-es-cr-localization/voice-guide.md` and verify the four required sections (register, tone, glossary, example pairs) per spec User Story 1's Independent Test description.

- [ ] T015 [US1] Author `specs/012-es-cr-localization/voice-guide.md` skeleton with the four required sections per [data-model.md §4](./data-model.md). Use the [quickstart.md](./quickstart.md) "Day-1 Setup → Commit 1" snippet as the starting body. Cover at minimum: 14 glossary terms (application, applicant, reviewer, approver, supplier, quotation, item, funding, agreement, signature, appeal, response, sign, send back), 8 example pairs (Login screen, empty-state, status pill, validation error, Funding Agreement clause, success TempData, Identity error, page title).
- [ ] T016 [US1] Submit the voice guide for review by the spec 011 voice owner (default per OQ-6) or the chosen CR-region reviewer. Capture the reviewer's name and review pass date in the artifact's frontmatter or a "Reviewers" section.
- [ ] T017 [US1] Apply reviewer feedback to the voice guide. Resolve the open questions OQ-1 (final term choices) and OQ-2 (footer tagline phrasing) inside the artifact.
- [ ] T018 [US1] Commit `voice-guide.md` as a STANDALONE commit on the feature branch — no per-view rewrite changes in the same commit. Confirm `git log --oneline 012-es-cr-localization -- specs/012-es-cr-localization/voice-guide.md` shows the commit hash that satisfies SC-009.

**Checkpoint**: Voice guide committed; per-view rewrites can now reference it.

---

## Phase 4: User Story 3 — Capital Semilla Brand Across All Surfaces (Priority: P1)

**Goal**: Replace "Forge" everywhere user-facing with "Capital Semilla" and rename the JS namespace.

**Independent Test**: Walk every authenticated view, the auth pages, the error page; inspect title, header, footer, brand SVGs; confirm zero "Forge" leakage. DevTools: `window.ForgeMotion === undefined` and `window.PlatformMotion` defined.

- [ ] T019 [US3] Update `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml`: title suffix `"Forge"` → `"Capital Semilla"`; header brand link text `"Forge"` → `"Capital Semilla"`; footer copyright + tagline → `"© 2026 Capital Semilla · diseñado para emprendedores"` (final tagline phrasing from voice guide OQ-2).
- [ ] T020 [US3] Update brand wordmark `src/FundingPlatform.Web/wwwroot/lib/brand/wordmark.svg`: `<text>` content from `"Forge"` to `"Capital Semilla"`; `aria-label` from `"Forge wordmark"` to the voice-guide-chosen Spanish equivalent. If a designer-delivered redrawn wordmark is not yet available (per FR-023 / EC-7), use a textual placeholder rendering "Capital Semilla" in the Fraunces display font.
- [ ] T021 [P] [US3] Update brand mark `src/FundingPlatform.Web/wwwroot/lib/brand/mark.svg`: `aria-label` from `"Forge mark"` to the voice-guide Spanish wording.
- [ ] T022 [P] [US3] Rename the JS namespace in `src/FundingPlatform.Web/wwwroot/js/motion.js` line 143: `root.ForgeMotion = {` → `root.PlatformMotion = {`. No backwards-compatible alias is created.
- [ ] T023 [P] [US3] Update six `window.ForgeMotion` callers in `src/FundingPlatform.Web/wwwroot/js/facelift-init.js` (lines 8, 9, 10, 11, 35, 36, 54, 57 per the brainstorm survey) to `window.PlatformMotion`. Verify with `grep -n 'ForgeMotion' src/FundingPlatform.Web/wwwroot/js/`.
- [ ] T024 [P] [US3] Update brand-reference comment in `src/FundingPlatform.Web/wwwroot/css/tokens.css` line ~150: `"inherits the Forge palette"` → `"inherits the Capital Semilla palette"` (or genericize to `"inherits the platform palette"` if voice guide prefers brand-neutral comments).
- [ ] T025 [US3] Run brand-sweep grep: `grep -rEn '\bForge\b' src/FundingPlatform.Web/ tests/ --include='*.cs' --include='*.cshtml' --include='*.js' --include='*.svg' --include='*.css' --include='*.json' | grep -vE 'Forgery|forgery'`. Confirm zero results outside `AntiForgery`-family framework symbols and jQuery vendor `// Forget` comments. Update or remove any straggler.

**Checkpoint**: Every page reads "Capital Semilla". JS globals renamed. SC-002 testable.

---

## Phase 5: User Story 6 — Status & Journey Display Renders Spanish (Priority: P2 — sequenced early)

**Goal**: Translate the 18 `DisplayLabel` values in `StatusVisualMap` so every downstream view inherits Spanish status display automatically.

**Independent Test**: Enumerate every value of every domain enum (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`); confirm each returns a non-empty Spanish `DisplayLabel`. Verify zero `enum.ToString()` callers reach UI surfaces.

- [ ] T026 [US6] Translate the 18 `DisplayLabel` values in `src/FundingPlatform.Web/Helpers/StatusVisualMap.cs` per the table in [data-model.md §2](./data-model.md). Use voice-guide-finalized terminology where ambiguous (e.g., "Resolved" for `ApplicationState.Resolved` vs `AppealStatus.Resolved` — both `"Resuelta"` per data-model.md, but verify against voice guide).
- [ ] T027 [US6] Add a registry-coverage test at `tests/FundingPlatform.Tests.Integration/Web/StatusVisualMapCoverageTests.cs`: enumerate every value of `ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus` via reflection (or hard-coded list); assert `StatusVisualMap.For(...)` returns a non-empty `DisplayLabel` for each. Use `Assert.That(label.Length > 0, "Missing Spanish label for " + enumValue)`. Also add an assertion that the label contains at least one non-ASCII Spanish character (via regex), guarding against accidental English regressions (e.g., `Assert.That(Regex.IsMatch(label, @"[áéíóúñÁÉÍÓÚÑ]") || allowedAsciiSpanishLabels.Contains(label))` — pin the allowedAsciiSpanishLabels list explicitly).
- [ ] T028 [US6] Run a static analysis check for `enum.ToString()` calls reaching UI surfaces. Use ripgrep: `rg -t cs '\.ToString\(\)' src/FundingPlatform.Web/Views/ src/FundingPlatform.Web/ViewModels/` and audit every match — confirm none of them are domain enum values reaching a Razor render. Document the audit result in the PR description (zero matches expected per research Decision 5).

**Checkpoint**: Every status pill, journey label, and detail-page status indicator now renders Spanish without changes to consumers.

---

## Phase 6: User Story 5 — Validation, Identity, and Framework Messages in Spanish (Priority: P2)

**Goal**: Translate 94 DataAnnotation messages across 14 view-model files and 52 controller-side user-facing strings across 10 controllers. The framework hooks (Identity describer + ModelBinding accessors) are already wired in Phase 2.

**Independent Test**: Submit each form-validation scenario (empty Required, length violation, type-mismatch) and Identity scenario (failed login, lockout, weak password, duplicate email) — confirm every message is Spanish.

### View Models — DataAnnotation translations (14 files, 94 attributes)

- [ ] T029 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AddItemViewModel.cs` (3 properties, 8 attributes total). Set explicit `ErrorMessage = "..."` on each `[Required]`; translate `[Display(Name=...)]` to Spanish per voice guide.
- [ ] T030 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/EditItemViewModel.cs` (mirror of AddItem).
- [ ] T031 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AddQuotationViewModel.cs` (5 properties incl. `Price` Range message and `Currency` StringLength message).
- [ ] T032 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AddSupplierViewModel.cs` (largest — 17 properties incl. CCSS / Hacienda / SICOP compliance flags; CR-specific terminology via voice guide).
- [ ] T033 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/LoginViewModel.cs` and `src/FundingPlatform.Web/ViewModels/RegisterViewModel.cs` (auth flow).
- [ ] T034 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/ChangePasswordViewModel.cs` (3 properties incl. `Compare`).
- [ ] T035 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/CreateImpactTemplateViewModel.cs` and `src/FundingPlatform.Web/ViewModels/EditImpactTemplateViewModel.cs` (incl. nested `ParameterDefinitionViewModel`).
- [ ] T036 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/SystemConfigurationViewModel.cs` and `src/FundingPlatform.Web/ViewModels/UploadSignedAgreementViewModel.cs`.
- [ ] T037 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AdminUserCreateViewModel.cs` (7 properties).
- [ ] T038 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AdminUserEditViewModel.cs` (7 properties).
- [ ] T039 [P] [US5] Translate DataAnnotations in `src/FundingPlatform.Web/ViewModels/AdminUserResetPasswordViewModel.cs` (3 properties incl. `Compare`).

### Controllers — user-facing strings (10 controllers, 52 sites)

- [ ] T040 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs` (4 TempData success messages on lines ~59, ~81, ~120, ~141 per research Decision 3). Keys remain English; values translate.
- [ ] T041 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/ReviewController.cs` (5 TempData strings on lines ~128, ~142–144, ~161, ~190).
- [ ] T042 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/AdminUsersController.cs` (3 TempData + 2 ModelState strings; note interpolated strings like `"User '{vm.Email}' created."` keep `{vm.Email}` placeholder).
- [ ] T043 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/ItemController.cs` (3 TempData + 1 ModelState string).
- [ ] T044 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/QuotationController.cs` (3 TempData + 2 ModelState strings; the duplicate `"A quotation file is required."` consolidates into a `private const string QuotationFileRequiredMessage = "Se requiere el archivo de la cotización.";` field on the controller).
- [ ] T045 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` (3 TempData + 2 TempData "FundingAgreementError" strings; `"A signed PDF file is required."` consolidates into a `private const string SignedPdfRequiredMessage` field).
- [ ] T046 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/AdminController.cs` (3 TempData strings).
- [ ] T047 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/SupplierController.cs` (1 TempData + 1 ModelState; reuse the QuotationFileRequiredMessage convention if pattern emerges; otherwise inline).
- [ ] T048 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/AccountController.cs` (1 ModelState `"Invalid login attempt."` → `"Inicio de sesión inválido."`; 2 dev-endpoint return literals — leave English IF they're only callable from a dev-only auth-debug endpoint, otherwise translate per voice guide).
- [ ] T049 [P] [US5] Translate user-facing strings in `src/FundingPlatform.Web/Controllers/AdminReportsController.cs` (1 ViewData string about aging threshold range).

**Checkpoint**: All form validation, Identity errors, and controller-emitted messages render Spanish. SC-003 / SC-004 testable.

---

## Phase 7: User Story 2 — Spanish UI Across Authenticated Surfaces (Priority: P1)

**Goal**: Translate every Razor view, partial, and layout's visible copy to formal Costa Rican Spanish per voice guide. The brand text (US3) and status registry (US6) and `lang="es-CR"` (Phase 2) are already in place — this story translates the remaining body copy, ARIA labels, tooltips, page titles, button labels, table headers, KPI labels, hero text, and empty-state captions.

**Independent Test**: Walk every authenticated view as each role; confirm 100% Spanish copy with no English token detectable except allowlist members. Spanish-speaking reviewer walkthrough validates voice register.

### Auth surfaces

- [ ] T050 [P] [US2] Translate `src/FundingPlatform.Web/Views/Account/Login.cshtml`. Per voice guide: page title, hero copy, label text, button text, error placeholder.
- [ ] T051 [P] [US2] Translate `src/FundingPlatform.Web/Views/Account/Register.cshtml`. Same surfaces as Login plus the legal-id, first-name, last-name labels.
- [ ] T052 [P] [US2] Translate `src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml`.
- [ ] T053 [P] [US2] Translate `src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml`.

### Shared chrome

- [ ] T054 [US2] Translate `src/FundingPlatform.Web/Views/Shared/Error.cshtml` (generic error page per EH-5; in dev-only correlation IDs / stack traces stay English).
- [ ] T055 [US2] Translate any user-visible copy that remains in `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` and `src/FundingPlatform.Web/Views/Shared/_AuthLayout.cshtml` after T009 / T019 (sidebar nav labels, user menu items, footer beyond brand line, breadcrumb defaults, etc.).
- [ ] T056 [P] [US2] Audit `src/FundingPlatform.Web/Views/Shared/Components/_StatusPill.cshtml` for any in-template English copy beyond the registry consumption — likely none, but verify and translate if found.

### Applicant flow surfaces

- [ ] T057 [P] [US2] Translate `src/FundingPlatform.Web/Views/Home/` views (2 files: applicant home dashboard incl. hero / KPI strip / awaiting-action callout / activity feed / resources strip; reviewer-equivalent home if present).
- [ ] T058 [P] [US2] Translate `src/FundingPlatform.Web/Views/Application/` views (4 files).
- [ ] T059 [P] [US2] Translate `src/FundingPlatform.Web/Views/Applications/` views (1 file).
- [ ] T060 [P] [US2] Translate `src/FundingPlatform.Web/Views/Item/` views (3 files: Add, Edit, Impact).
- [ ] T061 [P] [US2] Translate `src/FundingPlatform.Web/Views/Quotation/Add.cshtml`.
- [ ] T062 [P] [US2] Translate `src/FundingPlatform.Web/Views/Supplier/Add.cshtml` (incl. CCSS / Hacienda / SICOP compliance labels — CR-specific terminology per voice guide).
- [ ] T063 [P] [US2] Translate `src/FundingPlatform.Web/Views/ApplicantResponse/` views (3 files: Index, Appeal, _AppealMessage partial).

### Reviewer / approver / admin surfaces

- [ ] T064 [US2] Translate `src/FundingPlatform.Web/Views/Review/` views (7 files: Index, Review, QueueDashboard, GenerateAgreement, SigningInbox, _ReviewerQueueRows, _ReviewTabs). Sequential rather than parallel because these views share design-token-driven partials and a unified review.
- [ ] T065 [US2] Translate `src/FundingPlatform.Web/Views/Admin/` views (14 files — largest area). Sequential within this task because the views share the admin sidebar and Tabler shell.

### Verification

- [ ] T066 [US2] Run the full Aspire dev stack and walk every authenticated route as each role (applicant, reviewer, approver, admin). Capture screenshots for the integration PR. Note any voice / register inconsistencies; refer back to voice guide; resolve before continuing.
- [ ] T067 [US2] Run the SC-001 / NFR-002 regex sweep on rendered HTML or on the source `.cshtml` files: `grep -rEn '\b[A-Z][a-z]{3,}\b' src/FundingPlatform.Web/Views/` and audit every match against an allowlist (Capital, Semilla, USD, GBP, CRC, EUR, CRC, Aspire, Tabler, Fraunces, Inter, JetBrains, sentinel test-data tokens, ItemReviewStatus, ApplicationState — only as embedded data attributes, not display copy). Flag any non-allowlisted leakage and fix.

**Checkpoint**: Every authenticated screen renders fully Spanish. SC-001 / SC-006 testable.

---

## Phase 8: User Story 4 — Funding Agreement PDF in Formal Spanish (Priority: P1)

**Goal**: Translate the 4 Funding Agreement Razor partials to formal Spanish (legal/business register). Verify `es-CR` formatting renders correctly in the resulting PDF and signed agreements remain immutable.

**Independent Test**: Trigger Funding Agreement generation for an unsigned application; inspect the produced PDF. Then inspect a previously-signed PDF and confirm byte-identical pre-deployment state.

- [ ] T068 [US4] Translate `src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml` (orchestrator, 9 lines).
- [ ] T069 [US4] Translate `src/FundingPlatform.Web/Views/FundingAgreement/_FundingAgreementLayout.cshtml` (HTML frame, 123 lines): document title, reference number labels, generation timestamp label, funder identification block (legal name, tax/legal ID, address, email, phone labels), applicant identification block. Use formal Spanish business register per voice guide.
- [ ] T070 [US4] Translate the four partials in `src/FundingPlatform.Web/Views/FundingAgreement/` for: items table headers (Product, Category, Supplier, Unit Price, Line Total — translate per voice guide), totals row label, Terms & Conditions placeholder body (~100 words; mark legal text for explicit voice-guide review since legal register requires care), signature blocks (For the Funder / Applicant labels). Final terminology from voice guide (`convenio` vs `acuerdo`, `proveedor` vs `suministrador`, etc.).
- [ ] T071 [US4] Verify the existing `Money(currency, decimal)` helper and date-formatting calls (`@Model.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", culture)`) render correctly under the new culture. Render a representative agreement and inspect the generated PDF for: Spanish copy throughout; numeric format `1,234.56`; date format `dd/MM/yyyy`; currency rendering per per-quotation Currency code (e.g., `₡1,234.56` for CRC, `$1,234.56` for USD).
- [ ] T072 [US4] PDF visual-diff regression check (closes the "spec 005 PDF visual integrity" open thread carried into spec 010 OQ): render the SAME representative agreement under (a) the prior es-CO config (use a checkout of `b9da2da` or earlier) and (b) the new es-CR config; compare layout. Flag any layout breakage attributable to the number-separator shift (`1.234,56` → `1,234.56` — different glyph widths). Tighten layout in `_FundingAgreementLayout.cshtml` if needed.
- [ ] T073 [US4] Verify signed-agreement immutability: locate one previously-signed PDF artifact in dev fixtures (or generate one before merge); compute `sha256sum`; deploy the new branch; recompute `sha256sum` on the same artifact; confirm identical hashes. Spec 006 immutability honored.

**Checkpoint**: New Funding Agreements render formally in Spanish. Already-signed agreements unchanged. SC-005 testable.

---

## Phase 9: User Story 7 — E2E Test Suite Reflects New Copy (Priority: P2)

**Goal**: Translate visible-text assertions in the Playwright suite to Spanish; centralize through `UiCopy` constants for the high-leverage POM cascades. Suite passes green at same or higher count.

**Independent Test**: Run full Playwright suite against the Spanish build. All tests pass. Test count not decreased.

### Constants module

- [ ] T074 [US7] Create `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` with the ~50 unique Spanish strings consumed by POMs and shared fixtures, organized per the layout in [data-model.md §5](./data-model.md). Include both action-button strings (top-level constants) and state-label strings (nested `static class State`). Source strings from voice guide and `StatusVisualMap.cs` (the latter for state labels — duplicate intentionally per the data-model.md note).

### POM refactors (high-leverage — cascading consumers)

- [ ] T075 [P] [US7] Refactor `tests/FundingPlatform.Tests.E2E/PageObjects/AdminPage.cs` (4 text-bearing properties: `ManageTemplatesLink`, `ManageConfigurationLink`, `CreateNewTemplate`, `SaveConfiguration`) to consume `UiCopy.X`.
- [ ] T076 [P] [US7] Refactor `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicationPage.cs` (3 text-bearing properties: `CreateDraftApplication`, `AddItem`, `SubmitApplication`) to consume `UiCopy.X`.
- [ ] T077 [P] [US7] Refactor `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewApplicationPage.cs` (2 text-bearing properties: `SendBack`, `FinalizeReview`) to consume `UiCopy.X`.
- [ ] T078 [P] [US7] Refactor `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewQueuePage.cs` (1 text-bearing property: `NoApplicationsMessage`) to consume `UiCopy.X`.
- [ ] T079 [P] [US7] Audit and refactor remaining POMs that hold text-based properties: `LoginPage.cs`, `RegisterPage.cs`, `ChangePasswordPage.cs`, `ItemPage.cs`, `QuotationPage.cs`, `SupplierPage.cs`, `ApplicantResponsePage.cs`, `AppealThreadPage.cs`, `FundingAgreementPanelPage.cs`, `FundingAgreementDownloadFlow.cs`, `SigningStagePanelPage.cs`, `SigningReviewInboxPage.cs`, the `Admin/` namespaced POMs (`AdminBasePage`, `AdminReportsPage`, `AdminUserCreatePage`, `AdminUserEditPage`, `AdminUsersListPage`, `Admin/Reports/*`). Migrate any visible-text constants to `UiCopy.X`.

### Shared fixture refactor (37× cascading)

- [ ] T080 [US7] Refactor `tests/FundingPlatform.Tests.E2E/Fixtures/AuthenticatedTestBase.cs`: replace 37 occurrences of `"Add Supplier"` literal with `UiCopy.AddSupplier`. Cascades to 10+ test files automatically.

### Tests with dense visible-text assertions

- [ ] T081 [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs` (highest-affected: 21 `has-text` selectors + 2 `ToContainTextAsync`).
- [ ] T082 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/ApplicationSubmissionTests.cs` (16 selectors).
- [ ] T083 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/SendBackApplicationTests.cs` (10 selectors + 1 `ToContainTextAsync` + 1 `text=` pseudo-selector at line 70).
- [ ] T084 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/ImpactTemplateTests.cs` (9 selectors; preserve dynamic `tr:has-text('{templateName}')` data-driven selector at line 82 — locale-safe).
- [ ] T085 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/FinalizeReviewTests.cs` (9 selectors + 1 assertion).
- [ ] T086 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/ReviewItemDecisionTests.cs` (3 `ToContainTextAsync` for ItemReviewStatusBadge).
- [ ] T087 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/ApplicantResponseTests.cs` (Header / ApplicationState / DecisionDisplay assertions).
- [ ] T088 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/ReviewApplicationTests.cs` (ApplicationState / ApplicantName assertions).
- [ ] T089 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/TechnicalEquivalenceTests.cs` (2 `ToContainTextAsync`).
- [ ] T090 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/AdminConfigurationTests.cs` (System Configuration / "No system configurations found" assertions).
- [ ] T091 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/AdminImpactTemplateTests.cs` (incl. dynamic-template-name selectors which are locale-safe).
- [ ] T092 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/DraftPersistenceTests.cs`, `ItemManagementTests.cs`, `SupplierQuotationTests.cs`, `SupplierSelectionTests.cs`, `ReviewQueueTests.cs`, `RoleAwareSidebarTests.cs`, `SigningWayfindingTests.cs`, `DigitalSignatureTests.cs`, `FundingAgreementTests.cs`, `GenerateAgreementQueueTests.cs`, `AuthenticationTests.cs`.
- [ ] T093 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/AdminReports/` (8 files: dashboard, applicants, applications, funded items, aging, stub, tabler shell, currency rollout).
- [ ] T094 [P] [US7] Translate visible-text assertions in `tests/FundingPlatform.Tests.E2E/Tests/Admin/` (8 files: AdminInheritsReviewerTests, AdminUserLifecycleTests, LastAdminGuardTests, RoleAwareSidebarAdminEntriesTests, SelfModificationGuardTests, SentinelExclusionTests, SentinelImmutabilityTests, AdminReportsStubTests).
- [ ] T095 [P] [US7] Audit `tests/FundingPlatform.Tests.E2E/Helpers/CsvAssertions.cs` and `tests/FundingPlatform.Tests.E2E/Helpers/FundingAgreementPdfAssertions.cs` for embedded English text. CSV exports stay English (per spec Out of Scope); PDF assertions translate.
- [ ] T096 [P] [US7] Audit `tests/FundingPlatform.Tests.E2E/Fixtures/FundingAgreementSeeder.cs` for any text fixture that asserts on rendered PDF copy; translate.

### Integration suite

- [ ] T097 [US7] Audit `tests/FundingPlatform.Tests.Integration/` for any test that asserts on Spanish-translated content; translate as needed (most integration tests don't render UI, so changes should be minimal).

### Suite-wide verification

- [ ] T098 [US7] Run `dotnet test tests/FundingPlatform.Tests.Unit` — confirm green (no UI assertions; should be unaffected).
- [ ] T099 [US7] Run `dotnet test tests/FundingPlatform.Tests.Integration` — confirm green and at same or higher test count vs. pre-translation.
- [ ] T100 [US7] Run `dotnet test tests/FundingPlatform.Tests.E2E` — confirm green and at same or higher test count vs. pre-translation. SC-007 testable. NFR-007 testable.

**Checkpoint**: Full test suite green against Spanish build. Translation contract enforced.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, performance check, and PR-readiness.

- [ ] T101 [P] Capture post-translation Lighthouse metrics (LCP, TBT) at the same surfaces as T002. Save to `specs/012-es-cr-localization/perf-baseline-post.json`. Compare to pre-baseline. Confirm no regression per NFR-005 / SC-012; if regression detected, investigate and remediate before merge.
- [ ] T102 [P] Run final brand-sweep `grep -rEn '\bForge\b' src/ tests/` excluding AntiForgery framework family and jQuery vendor; confirm zero hits per SC-002.
- [ ] T103 [P] Run final English-token sweep on rendered HTML (or `.cshtml` source) — confirm only allowlisted tokens remain per SC-001.
- [ ] T104 [P] Capture logs from a happy-path application flow (login → create application → add item → add supplier → submit → review → finalize → generate PDF → sign). Filter for non-ASCII characters; expect zero from log messages (NFR-001 / SC-008).
- [ ] T105 [P] Verify `<html lang="es-CR">` declaration on every rendered page including auth and error per SC-006.
- [ ] T106 [P] Verify all four open-thread closures land in `brainstorm/00-overview.md`: spec 005 locale pin, spec 008 partial-check, spec 011 voice-guide-drift, spec 011 brand sign-off (all four already added in commit `c154b89`; verify present).
- [ ] T107 Compile final integration PR description: link spec, plan, voice-guide commit, summary of all files touched (use `git diff --stat main...HEAD`), screenshots of representative before/after surfaces, link to perf comparison, link to research findings. Reference SC-001 through SC-012 verification status.
- [ ] T108 Open PR against `main`. Request review from spec 011 voice owner (per OQ-6) and the project maintainer.
- [ ] T109 After review feedback resolved and PR approved, merge with squash or merge commit per project convention. Update `.specify/feature.json` to point to the next active spec (or leave at 012 until 013 starts).

---

## Dependencies

```text
Phase 1 (Setup)  ──┐
                   ├──> Phase 2 (Foundational: locale infra) ──┐
                   │                                            ├──> Phase 3 (US1: voice guide)
                   │                                            │             │
                   │                                            │             ↓
                   │                                            │    ┌────────┼─────────────────────────────┐
                   │                                            │    │        │                             │
                   │                                            │    ↓        ↓                             ↓
                   │                                            │  Phase 4   Phase 5                     Phase 6
                   │                                            │  (US3      (US6                        (US5
                   │                                            │   brand)    status)                     framework)
                   │                                            │    │        │                             │
                   │                                            │    └────┬───┴────────┬────────────────────┘
                   │                                            │         │            │
                   │                                            │         ↓            ↓
                   │                                            │      Phase 7      Phase 8
                   │                                            │      (US2 UI)     (US4 PDF)
                   │                                            │         │            │
                   │                                            │         └─────┬──────┘
                   │                                            │               │
                   │                                            │               ↓
                   │                                            │           Phase 9 (US7 tests)
                   │                                            │               │
                   │                                            │               ↓
                   │                                            │           Phase 10 (Polish + PR)
                   │                                            │
                   └──> (T002 perf baseline) ───────────────────┘
```

### Story-level dependencies

- **US1 (voice guide)** blocks US2, US4, US5, US6 (per FR-020 / SC-009).
- **US3 (brand)** is independent of US1; can run after Phase 2 alongside US6 / US5.
- **US6 (status registry)** runs before US2 so view sweep inherits translated labels for free.
- **US2 (UI sweep)** depends on US1 (voice guide), US6 (registry), US3 (brand chrome stable).
- **US4 (PDF)** depends on US1 (voice guide for legal register).
- **US5 (framework messages)** mostly independent; can run in parallel with US3 / US6 / US2 once Phase 2 lands.
- **US7 (E2E tests)** depends on US2, US3, US4, US5, US6 — runs after all production-side translations stabilize.

### Within-phase parallelism

Most translation tasks are file-scoped, so the `[P]` marker is heavy:

- T029–T039 (DataAnnotations across 14 files) are fully parallel.
- T040–T049 (controllers across 10 files) are fully parallel.
- T050–T053 (auth view set) are parallel.
- T057–T063 (applicant flow views) are parallel.
- T075–T079 (POM refactors) are parallel.
- T081–T096 (test file translations) are parallel modulo shared fixtures.
- T101–T106 (final verifications) are all parallel.

The non-parallel tasks (T064 Review/, T065 Admin/) are sequential because views in those folders share heavily-themed partials and a unified visual review.

---

## Implementation Strategy

### MVP-equivalent (smallest internally-merged increment)

Phase 1 + Phase 2 + Phase 3 (US1) is the minimum increment that gates the rest. It produces no user-visible Spanish copy yet, but the locale runtime is wired and the voice guide is committed — the entire project can resume from this state at any time.

### Day-1 commit sequence (per quickstart.md)

1. **Commit 1** — Voice guide (T015–T018). Standalone commit; satisfies SC-009.
2. **Commit 2** — Locale infrastructure (T004–T014). The Phase 2 block.
3. **Commit 3** — Brand rebrand (T019–T025). The US3 block.

### Sweep order (per quickstart.md)

Following the per-folder sweep order: Account → Home → Shared → Item/Quotation/Supplier → ApplicantResponse → Review → FundingAgreement → Admin → registry → view models → controllers → tests.

### Single-merge integration

All commits stay on `012-es-cr-localization`; the branch lands as one PR against `main` (Approach A from brainstorm). Each commit during the sweep is reviewable and revertible individually, but production rollout is the single merge.

### Reviewer cadence

- **Voice guide review** (T016): blocks Phase 4+; one round, ~1 day turnaround expected.
- **Per-PR reviewer** (each commit on the branch): aim for same-day review on file-scoped translation commits; multi-day review acceptable for large folder sweeps (Admin/, Review/).
- **Final integration review** (T108): full-feature walkthrough plus the 8 verification SCs (T101–T106).

---

## Total Counts

- **Tasks**: 109
- **Phases**: 10 (Setup → Foundational → 7 user-story phases → Polish)
- **Parallel-marked tasks**: 67
- **Files net-new**: 4 (`EsCrCultureFactory.cs`, `EsCrIdentityErrorDescriber.cs`, `voice-guide.md`, `UiCopy.cs`) — plus tests-side coverage test (`StatusVisualMapCoverageTests.cs`) and two perf-baseline JSON snapshots in the spec directory.
- **Files modified**: ~110 (~72 Razor views, 14 view models, 10 controllers, 4 PDF partials, 2 layouts, `Program.cs`, `AppHost.cs`, 2 appsettings.json, `FunderOptions.cs`, `StatusVisualMap.cs`, `motion.js`, `facelift-init.js`, `tokens.css`, 2 brand SVGs, ~25 E2E test files, ~20 POMs).

## Independent Test Criteria (from spec)

Each user story's Independent Test is testable mid-sweep:

| Story | Test (recap) |
|---|---|
| US1 Voice Guide | Open voice-guide.md and verify the four required sections + glossary + examples per spec User Story 1. |
| US2 Spanish UI | Walk every authenticated route as each role; confirm 100% Spanish copy + brand + lang attribute. |
| US3 Capital Semilla | Walk every page; confirm zero "Forge" leakage; DevTools `window.PlatformMotion` defined. |
| US4 PDF Spanish | Trigger generation; inspect PDF for Spanish copy + es-CR formatting + immutable signed agreements. |
| US5 Framework Messages | Submit invalid form, fail login, hit lockout, register duplicate email; every error in Spanish. |
| US6 Status Display | Enumerate every enum value via registry test; confirm non-empty Spanish DisplayLabel. |
| US7 E2E Tests | Run full Playwright suite; pass green at same or higher count. |
