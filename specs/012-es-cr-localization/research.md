# Phase 0 Research: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Spec:** [spec.md](./spec.md)
**Date:** 2026-04-29

## Summary

Five research threads ran in parallel during Phase 0. The investigation surfaced one critical finding (es-CR CultureInfo defaults conflict with the spec's user-visible target — resolution: explicit format overrides, anticipated by the spec's Assumption clause), three structural findings (DataAnnotation surface, controller-side user copy, E2E test impact), and one risk-eliminating finding (Tabler.io vendor JS carries no user-facing English copy). All NEEDS CLARIFICATION items from the planning template are resolved.

---

## Decision 1: Format-locale override for `es-CR`

### Finding

A live probe of .NET 10's `CultureInfo.GetCultureInfo("es-CR")` reveals defaults that differ from the spec's user-visible target:

| Property | .NET es-CR default | Spec target |
|---|---|---|
| `NumberDecimalSeparator` | `,` (comma) | `.` (period) |
| `NumberGroupSeparator` | ` ` (space) | `,` (comma) |
| `ShortDatePattern` | `d/M/yyyy` (single-digit) | `dd/MM/yyyy` (two-digit) |
| `CurrencySymbol` | `₡` (colón) | `₡` (✓) |
| `CurrencyDecimalSeparator` | `,` (comma) | `.` (period) |
| `CurrencyGroupSeparator` | ` ` (space) | `,` (comma) |
| `LongDatePattern` | `dddd, d 'de' MMMM 'de' yyyy` | (acceptable as-is) |
| `FirstDayOfWeek` | Monday | (acceptable) |

Probe output for reference:

```text
number N:  '1 234 567,890'        ← .NET es-CR
number C:  '₡1 234 567,89'        ← .NET es-CR
date d:    '29/4/2026'            ← .NET es-CR
```

Spec FR-017/FR-018 demand `1,234.56` and `dd/MM/yyyy`.

### Decision

Use `es-CR` as the request culture, then **override** the formatter properties at startup to deliver the spec target. Pattern:

```csharp
var esCr = CultureInfo.GetCultureInfo("es-CR").Clone() as CultureInfo;

// Number format: period decimal, comma thousands (US-style — what CR business uses)
esCr.NumberFormat.NumberDecimalSeparator = ".";
esCr.NumberFormat.NumberGroupSeparator = ",";
esCr.NumberFormat.CurrencyDecimalSeparator = ".";
esCr.NumberFormat.CurrencyGroupSeparator = ",";

// Date format: two-digit day/month
esCr.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";

// Pin the culture in RequestLocalization middleware, single-locale, no negotiation.
```

### Rationale

The spec's Assumption clause (Assumptions section) anticipated exactly this: *"If a planning-time investigation reveals quirks, the spec's format-pinning may need an explicit `NumberFormatInfo` / `DateTimeFormatInfo` override, but the user-visible target stays the same."* Confirmed by the probe; the user-visible target wins.

### Alternatives Considered

- **Accept .NET's es-CR defaults as-is** (rejected). Would render `1 234 567,89` and `29/4/2026`, contradicting the spec target and the user's domain knowledge that CR business documents use US-style separators.
- **Use a different culture name like `es-MX` or `en-CR`** (rejected). `es-CR` is the right semantic culture (Spanish + region) and gives Spanish month/day names; only the format properties need adjustment.
- **Build a custom `CultureInfo` from scratch** (rejected). More invasive and fragile than cloning + overriding two formatter sub-objects.

---

## Decision 2: DataAnnotation translation surface (94 attributes, 14 files)

### Finding

The view-model layer at `src/FundingPlatform.Web/ViewModels/` has **94 DataAnnotation attributes across 14 files** — every attribute IN scope, none in Domain or Application. No custom `ValidationAttribute` subclasses exist. Counts:

| Attribute | Count |
|---|---|
| `[Required]` | 36 |
| `[Display(Name=…)]` | 11 |
| `[MaxLength(…)]` | 9 |
| `[StringLength(…)]` | 4 |
| `[DataType(…)]` | 5 |
| `[Range(…)]` | 2 |
| `[Compare(…)]` | 3 |
| `[EmailAddress]` | 5 |
| `[Phone]` | 2 |
| `[MinLength(…)]` | 1 |

Two existing custom `ErrorMessage` values: `"Price must be greater than zero."` (×2 sites) and `"Currency must be a 3-character code."` (×2 sites).

Affected files (all under `src/FundingPlatform.Web/ViewModels/`):
- `AddItemViewModel.cs`, `EditItemViewModel.cs`
- `AddQuotationViewModel.cs`
- `AddSupplierViewModel.cs`
- `ChangePasswordViewModel.cs`, `LoginViewModel.cs`, `RegisterViewModel.cs`
- `CreateImpactTemplateViewModel.cs`, `EditImpactTemplateViewModel.cs`
- `SystemConfigurationViewModel.cs`
- `UploadSignedAgreementViewModel.cs`
- `AdminUserCreateViewModel.cs`, `AdminUserEditViewModel.cs`, `AdminUserResetPasswordViewModel.cs`

### Decision

For every DataAnnotation attribute, set an explicit Spanish `ErrorMessage` (and translate `[Display(Name=…)]` to Spanish). Do NOT rely on framework `DataAnnotationsResources` localization — explicit messages keep tone consistent with the voice guide and avoid surprise behavior.

For framework-generated messages NOT covered by explicit `ErrorMessage` (model-binding errors like type mismatch, missing required field), configure `MvcOptions.ModelBindingMessageProvider` with Spanish providers in `Program.cs`.

### Rationale

The voice-guide warm-modern tone differs from the literal framework defaults. Explicit messages give per-attribute control. The catalog is small enough (94 attributes / 14 files) that explicit translation is bounded work, and the result is searchable (any future copy edit grep-able by Spanish string).

### Alternatives Considered

- **Rely on .NET's DataAnnotationsResources for es** (rejected). Defaults exist for many cultures, but the resulting messages don't match the warm-modern voice and aren't reviewable per-attribute.
- **Centralize messages in a static `ValidationMessages` constants class referenced by every attribute** (deferred). Could simplify maintenance but adds an indirection layer that doesn't pay back at 94 attributes; revisit only if duplication grows.

---

## Decision 3: Controller-side user copy (52 sites)

### Finding

Across 10 controllers, **52 user-facing string sites** in C# code:

| Category | Count |
|---|---|
| `TempData["..."]` success/error messages | 26 |
| `ModelState.AddModelError(...)` user-visible | 15 |
| `ViewData["..."]` user-visible | 1 |
| `return BadRequest("...")` / `Ok("...")` literal | 2 |
| Email senders | 0 (none — confirmed) |

Top files: `ApplicantResponseController` (4), `ReviewController` (5), `AdminUsersController` (11 incl. ModelState), `ItemController` (4), `QuotationController` (5), `FundingAgreementController` (5), `AdminController` (3), `SupplierController` (2), `AccountController` (3), `AdminReportsController` (1).

Duplicate string: `"A quotation file is required."` appears 3 times (Supplier, Quotation×2). Interpolated strings present: `"User '{vm.Email}' created."`, `"Appeal resolved as {resolution}."`.

### Decision

Translate every controller-side user-facing string in place. Spanish strings live as inline literals in the C# code; the dictionary keys (`"SuccessMessage"`, `"ErrorMessage"`) and the variable interpolation placeholders remain English. The duplicate `"A quotation file is required."` string is consolidated into a single `private const string` field on each affected controller (or, if a stronger pattern emerges, a `static class ValidationMessages` in the Web project) — but only if duplication grows beyond two occurrences.

### Rationale

Controller-emitted strings are not "framework" — they're application copy. Inline translation keeps the seam at the user-facing string boundary (per NFR-001) without introducing a new module. The duplicate is small (×3) and a single `const` per controller is sufficient deduplication.

### Alternatives Considered

- **Centralize all controller-side messages in a `ControllerMessages` resource module** (rejected). Premature abstraction; only one string is genuinely duplicated; the inline pattern matches the spec's "no swappable-locale machinery" rule.
- **Use `IStringLocalizer<TController>` per controller** (rejected). Violates NFR-003 (no resx machinery, no IStringLocalizer).

---

## Decision 4: E2E test impact (186 visible-text selectors, 4 POMs)

### Finding

The E2E suite at `tests/FundingPlatform.Tests.E2E/` has **186 visible-text-dependent assertions across 20 test files**, broken down:

| Selector type | Count |
|---|---|
| `Locator(...).has-text(...)` | 161 |
| `ToContainTextAsync(...)` | 22 |
| `ToHaveTextAsync(...)` | 1 |
| `ToHaveValueAsync(...)` | 1 |
| `text=` pseudo-selector | 1 |

Critically, **no `GetByRole` / `GetByLabel` / `GetByPlaceholder` / `GetByTitle` / `GetByAltText` patterns exist in the suite** — selectors are has-text() and CSS based.

48 unique English strings in selectors. Top frequencies: `"Add Supplier"` (37×), `"Submit Application"` (20×), `"Save Impact"` (17×), `"Impact"` (17×), `"Submitted"` (15×), `"Draft"` (8×), `"Finalized"` (3×).

Four POMs hold ~10 text-dependent properties cascading to ~115 downstream test usages:
- `AdminPage.cs` (4 properties)
- `ApplicationPage.cs` (3 properties)
- `ReviewApplicationPage.cs` (2 properties)
- `ReviewQueuePage.cs` (1 property)

### Decision

**POM-first translation.** Externalize the 10 POM properties to a single `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` (or similar) holding `public static class UiCopy { public const string AddSupplier = "Agregar proveedor"; … }`. Update each POM to reference `UiCopy.X` rather than English literals. This propagates ~62% of the rewrite automatically through cascading POM consumers.

For the remaining ~70-80 assertions on state strings (`"Approved"` / `"Aprobada"`, `"Rejected"` / `"Rechazada"`, etc.), update test-by-test, sourcing from the same `UiCopy` constants where reuse warrants and inline literals where one-shot.

Tests that currently use the rare `text=` pseudo-selector (1 in `SendBackApplicationTests`) are updated as one-off rewrites.

### Rationale

The "POM with constants" pattern is consistent with the constitution's POM requirement for E2E tests. Centralized constants avoid the 37× repetition of `"Add Supplier"` across the test suite and make future copy edits a single-file change. The E2E suite already proves the visible-text-as-contract approach works at scale; translating constants preserves that contract in Spanish.

### Alternatives Considered

- **Hard-cut all has-text to data-testid attributes** (rejected per spec brainstorm). Conflates two scopes; balloons the spec.
- **Per-test inline rewrites without POM constants** (rejected). Scales poorly with the 37× / 20× / 17× duplications; one English-string change would touch 37 places.
- **Inject `CultureInfo` into POMs and use a runtime lookup** (rejected). Reintroduces the i18n machinery NFR-003 forbids.

---

## Decision 5: Status registry is centralized — no .ToString() bypasses

### Finding

The `_StatusPill` registry (spec 008) lives at three files:
- `src/FundingPlatform.Web/Helpers/StatusVisualMap.cs` — registry implementation
- `src/FundingPlatform.Web/Views/Shared/Components/_StatusPill.cshtml` — partial template
- `src/FundingPlatform.Web/Models/StatusVisual.cs` — `record StatusVisual(string Color, string Icon, string DisplayLabel)`

The registry covers **18 enum values across 4 enums** (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`) with no `.ToString()` bypasses anywhere in the codebase. Static analysis confirms every enum-to-display path goes through `StatusVisualMap.For(...)`.

### Decision

Translate every `DisplayLabel` value in `StatusVisualMap.cs` to Spanish. The 18 strings:

| Enum | Value | English | Recommended Spanish (voice guide finalizes) |
|---|---|---|---|
| ApplicationState | Draft | Draft | Borrador |
| ApplicationState | Submitted | Submitted | Enviada |
| ApplicationState | UnderReview | Under Review | En revisión |
| ApplicationState | Resolved | Resolved | Resuelta |
| ApplicationState | AppealOpen | Appeal Open | Apelación abierta |
| ApplicationState | ResponseFinalized | Response Finalized | Respuesta finalizada |
| ApplicationState | AgreementExecuted | Agreement Executed | Convenio ejecutado |
| ItemReviewStatus | Pending | Pending | Pendiente |
| ItemReviewStatus | Approved | Approved | Aprobado |
| ItemReviewStatus | Rejected | Rejected | Rechazado |
| ItemReviewStatus | NeedsInfo | Needs Info | Requiere información |
| AppealStatus | Open | Open | Abierta |
| AppealStatus | Resolved | Resolved | Resuelta |
| SignedUploadStatus | Pending | Pending | Pendiente |
| SignedUploadStatus | Approved | Approved | Aprobada |
| SignedUploadStatus | Rejected | Rejected | Rechazada |
| SignedUploadStatus | Superseded | Superseded | Reemplazada |
| SignedUploadStatus | Withdrawn | Withdrawn | Retirada |

Color and icon properties remain unchanged; only `DisplayLabel` flips to Spanish. SC-011's "zero `.ToString()` reach UI" is already satisfied by the existing architecture.

### Rationale

Spec 008's seam works exactly as designed. No structural change required; only the string contents.

---

## Decision 6: Funding Agreement PDF (~250 words, 4 templates)

### Finding

The PDF rendering chain at `src/FundingPlatform.Web/Views/FundingAgreement/`:
- `Document.cshtml` (orchestrator, 9 lines)
- `_FundingAgreementLayout.cshtml` (HTML frame, 123 lines)
- 4 partial templates (~115 lines combined, ~250 words English)

Sections: Header (32 lines), Items Table (40 lines), Terms & Conditions (24 lines, ~100 words placeholder text), Signature Blocks (19 lines).

Locale-aware formatting already in place: `@Model.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", culture)` and a `Money(currency, decimal)` helper using `ToString("N2", culture)`. The `culture` parameter is threaded through; flipping the configured culture from `es-CO` to `es-CR` (with the Decision 1 overrides) makes the existing format machinery produce the spec target.

### Decision

Translate the four PDF partials to formal Spanish (legal/business register, formal `usted`). Final terminology comes from the voice guide glossary (OQ-1). Verify that the existing `Money` and date helpers continue to work under the new culture — they should, since both already accept `culture` as a parameter.

Add a one-time PDF visual-diff check during implementation: render a representative agreement under both `es-CO` (pre-deployment) and `es-CR + overrides` (post-deployment), compare layout. The number-separator shift (`1.234,56` → `1,234.56`) is the regression-risk surface.

### Rationale

Same seam as the views — translate the Razor template body inline. The existing culture-parameterized formatting helpers mean no implementation change beyond the configured culture and the Razor copy.

### Alternatives Considered

- **Re-render all existing unsigned drafts to Spanish on first deploy** (rejected). spec 005's regeneration semantics already cover this — the next regeneration replaces the draft, no batch job needed.
- **Add a Spanish-specific PDF template alongside the existing English one** (rejected). Two templates would diverge and contradict NFR-003.

---

## Decision 7: Tabler.io vendor JS carries no user-facing English copy

### Finding

`@tabler/core 1.4.0` (vendored 2026-04-25) at `src/FundingPlatform.Web/wwwroot/lib/tabler/`. The minified JS bundle was scanned: only Bootstrap-bundled developer-console warnings exist (`"Bootstrap's dropdowns require Popper..."`, etc.). No user-facing UI strings, no built-in component labels, no date-picker captions. `data-bs-*` attributes are configuration hooks, not embedded copy.

Supporting libraries audited: `canvas-confetti` (no copy), jQuery (no UI copy beyond dev warnings).

### Decision

**No vendor work needed.** OQ-4 closes here: no Tabler/Bootstrap JS strings reach users. Spec edge case EC-5 is resolved.

### Rationale

The spec's audit step found nothing actionable. Skipping a vendor fork is the simplest path that honors NFR-003.

---

## Open Question Resolution Status

| Open Question | Resolved by Phase 0? | Resolution |
|---|---|---|
| OQ-1 Glossary finalization | Partially | Voice guide skeleton (next deliverable) carries the terms list; final picks land during voice-guide authoring. |
| OQ-2 Footer tagline phrasing | No | `diseñado para emprendedores` recommended in spec; finalize during voice-guide authoring. |
| OQ-3 Designer SVG follow-ups | No | Out-of-band designer task; placeholder approach in spec stands. |
| OQ-4 Tabler vendor JS audit | **YES** | No copy found; no override required (Decision 7). |
| OQ-5 Performance baseline | Defer | Capture LCP/TBT on day 1 of implementation, before any UI rewrites. |
| OQ-6 Voice-guide reviewer | No | Deferred to ops decision. Plan recommends the same designer/voice owner as spec 011. |
| OQ-7 Page-title direction | No | Spec recommends `[Page] - Capital Semilla`; matches today's pattern. Confirmed acceptable. |
| OQ-8 Hard-pin culture vs config | **YES (recommend hard-pin)** | Decision 1 pins the culture in middleware via a built constant; `FundingAgreement:LocaleCode` config remains overridable but emits WARN if it diverges (per NFR-008). |
| OQ-9 JS namespace rename | **YES (recommend `PlatformMotion`)** | Two callers, no external consumers; hard cut is safe. |

---

## Implementation Patterns Confirmed

1. **`RequestLocalization` middleware** — single supported culture, fixed defaults, no fallback negotiation:

   ```csharp
   builder.Services.Configure<RequestLocalizationOptions>(o =>
   {
       var esCr = BuildEsCrCultureWithOverrides();  // see Decision 1
       o.SupportedCultures = new[] { esCr };
       o.SupportedUICultures = new[] { esCr };
       o.DefaultRequestCulture = new RequestCulture(esCr);
       o.RequestCultureProviders.Clear();  // disable Accept-Language negotiation
   });
   // ...
   app.UseRequestLocalization();
   ```

2. **`IdentityErrorDescriber` Spanish subclass** — register in DI:

   ```csharp
   builder.Services.AddIdentity<...>().AddErrorDescriber<EsCrIdentityErrorDescriber>();
   // EsCrIdentityErrorDescriber overrides every IdentityErrorDescriber method.
   ```

3. **`ModelBindingMessageProvider` Spanish providers** — config in `AddControllersWithViews`:

   ```csharp
   builder.Services.AddControllersWithViews(options =>
   {
       var p = options.ModelBindingMessageProvider;
       p.SetValueIsInvalidAccessor(v => $"El valor '{v}' no es válido.");
       p.SetMissingBindRequiredValueAccessor(name => $"El campo {name} es obligatorio.");
       p.SetMissingKeyOrValueAccessor(() => "Falta una clave o valor obligatorio.");
       // ... cover the full set of accessor properties
   });
   ```

4. **`StatusVisualMap` translation** — single-file edit; no signature change.

5. **POM constants for E2E tests** — new `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` consumed by all 4 POMs and shared test fixtures.

---

## Phase 0 Conclusion

All NEEDS CLARIFICATION items resolved. The plan can proceed to Phase 1 (data-model, quickstart, agent context update) with no outstanding spec-blocking unknowns. The single-locale, single-coordinated-sweep approach is fully validated against the codebase reality.
