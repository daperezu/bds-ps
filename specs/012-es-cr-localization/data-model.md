# Phase 1 Data Model: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Spec:** [spec.md](./spec.md) · **Plan:** [plan.md](./plan.md)
**Date:** 2026-04-29

## Scope of Changes

This spec is a **presentation-layer + configuration-layer** feature. It introduces **zero** domain entities, **zero** database schema changes, and **zero** persistence-bound state. The "data" surface is limited to:

1. **Configuration values** (existing keys; values change).
2. **In-process registries** (existing structures; string values change to Spanish).
3. **One new infrastructure-layer subclass** (`IdentityErrorDescriber` override).
4. **One new in-spec deliverable artifact** (the voice guide).
5. **One new test-side constants module** (POM-consumed).

There are **no `Domain/Entities/`, `Application/DTOs/`, or `Database/Tables/` modifications.** The Schema-First DB principle (constitution IV) is satisfied trivially — no `.sql` files touched.

---

## 1. Configuration Values

### `FundingAgreement:LocaleCode`

| Property | Before | After |
|---|---|---|
| Type | `string` | `string` (unchanged) |
| Default in code | `"es-CO"` (in `FunderOptions.LocaleCode`, `FundingAgreementController` fallback) | `"es-CR"` |
| Value in `appsettings.json` | `"es-CO"` | `"es-CR"` |
| Value in `appsettings.Development.json` | `"es-CO"` | `"es-CR"` |
| Value in `AppHost.cs` default | `"es-CO"` | `"es-CR"` |

**Validation rule** (FR-016): At startup, the value MUST equal `"es-CR"` for the spec's user-visible target to hold. If diverged, emit a startup `WARN` log (NFR-008) and proceed (config remains overridable for testing flexibility, but production deployments are expected to match).

### Hard-pinned request culture (new, in-process)

Introduced in `RequestLocalization` middleware setup. Not a config value — a built constant assembled from the cloned `es-CR` `CultureInfo` plus the format overrides from research Decision 1. No deployment override possible.

| Property | Type | Value |
|---|---|---|
| Culture name | `string` | `"es-CR"` |
| `NumberDecimalSeparator` | `string` | `"."` |
| `NumberGroupSeparator` | `string` | `","` |
| `CurrencyDecimalSeparator` | `string` | `"."` |
| `CurrencyGroupSeparator` | `string` | `","` |
| `ShortDatePattern` | `string` | `"dd/MM/yyyy"` |
| (other properties) | inherited | `es-CR` defaults preserved (currency symbol `₡`, day/month names, etc.) |

---

## 2. In-Process Registry: `StatusVisualMap`

Existing record at `src/FundingPlatform.Web/Helpers/StatusVisualMap.cs`. **Schema unchanged.** Only the `DisplayLabel` field of each registered `StatusVisual` flips from English to Spanish.

```csharp
// Existing record — no shape change
public sealed record StatusVisual(string Color, string Icon, string DisplayLabel);
```

| Enum | Value | DisplayLabel before | DisplayLabel after (recommended; voice guide finalizes) |
|---|---|---|---|
| `ApplicationState` | `Draft` | `"Draft"` | `"Borrador"` |
| `ApplicationState` | `Submitted` | `"Submitted"` | `"Enviada"` |
| `ApplicationState` | `UnderReview` | `"Under Review"` | `"En revisión"` |
| `ApplicationState` | `Resolved` | `"Resolved"` | `"Resuelta"` |
| `ApplicationState` | `AppealOpen` | `"Appeal Open"` | `"Apelación abierta"` |
| `ApplicationState` | `ResponseFinalized` | `"Response Finalized"` | `"Respuesta finalizada"` |
| `ApplicationState` | `AgreementExecuted` | `"Agreement Executed"` | `"Convenio ejecutado"` |
| `ItemReviewStatus` | `Pending` | `"Pending"` | `"Pendiente"` |
| `ItemReviewStatus` | `Approved` | `"Approved"` | `"Aprobado"` |
| `ItemReviewStatus` | `Rejected` | `"Rejected"` | `"Rechazado"` |
| `ItemReviewStatus` | `NeedsInfo` | `"Needs Info"` | `"Requiere información"` |
| `AppealStatus` | `Open` | `"Open"` | `"Abierta"` |
| `AppealStatus` | `Resolved` | `"Resolved"` | `"Resuelta"` |
| `SignedUploadStatus` | `Pending` | `"Pending"` | `"Pendiente"` |
| `SignedUploadStatus` | `Approved` | `"Approved"` | `"Aprobada"` |
| `SignedUploadStatus` | `Rejected` | `"Rejected"` | `"Rechazada"` |
| `SignedUploadStatus` | `Superseded` | `"Superseded"` | `"Reemplazada"` |
| `SignedUploadStatus` | `Withdrawn` | `"Withdrawn"` | `"Retirada"` |

**Validation rule** (FR-009 / SC-011): every enum value MUST return a non-empty Spanish `DisplayLabel`. Static analysis: any code path reaching a UI surface via `enum.ToString()` MUST be zero (already true per research Decision 5).

---

## 3. New Infrastructure Subclass: `EsCrIdentityErrorDescriber`

**File:** `src/FundingPlatform.Infrastructure/Identity/EsCrIdentityErrorDescriber.cs` (new)

Extends `Microsoft.AspNetCore.Identity.IdentityErrorDescriber`. Overrides every method that returns an `IdentityError` so the `Description` field is Spanish.

### Identity error code coverage (per FR-013 / SC-004)

The base class exposes ~28 virtual methods. The subclass MUST override every method that the codebase actually surfaces to users. The minimum set, derived from current Identity usage:

| Method | English (base) | Spanish (target) |
|---|---|---|
| `DefaultError()` | `"An unknown failure has occurred."` | `"Ocurrió un error inesperado."` |
| `ConcurrencyFailure()` | `"Optimistic concurrency failure, object has been modified."` | `"El registro fue modificado por otro usuario."` |
| `PasswordMismatch()` | `"Incorrect password."` | `"Contraseña incorrecta."` |
| `InvalidToken()` | `"Invalid token."` | `"Token inválido."` |
| `LoginAlreadyAssociated()` | `"A user with this login already exists."` | `"Ya existe un usuario con este inicio de sesión."` |
| `InvalidUserName(name)` | `"User name '{0}' is invalid..."` | `"El nombre de usuario '{0}' no es válido..."` |
| `InvalidEmail(email)` | `"Email '{0}' is invalid."` | `"El correo electrónico '{0}' no es válido."` |
| `DuplicateUserName(name)` | `"User name '{0}' is already taken."` | `"El nombre de usuario '{0}' ya está en uso."` |
| `DuplicateEmail(email)` | `"Email '{0}' is already taken."` | `"El correo electrónico '{0}' ya está registrado."` |
| `InvalidRoleName(name)` | `"Role name '{0}' is invalid."` | `"El nombre de rol '{0}' no es válido."` |
| `DuplicateRoleName(name)` | `"Role name '{0}' is already taken."` | `"El nombre de rol '{0}' ya está en uso."` |
| `UserAlreadyHasPassword()` | `"User already has a password set."` | `"El usuario ya tiene una contraseña establecida."` |
| `UserLockoutNotEnabled()` | `"Lockout is not enabled for this user."` | `"El bloqueo no está habilitado para este usuario."` |
| `UserAlreadyInRole(role)` | `"User already in role '{0}'."` | `"El usuario ya tiene el rol '{0}'."` |
| `UserNotInRole(role)` | `"User is not in role '{0}'."` | `"El usuario no tiene el rol '{0}'."` |
| `PasswordTooShort(length)` | `"Passwords must be at least {0} characters."` | `"La contraseña debe tener al menos {0} caracteres."` |
| `PasswordRequiresNonAlphanumeric()` | `"Passwords must have at least one non alphanumeric character."` | `"La contraseña debe incluir al menos un carácter no alfanumérico."` |
| `PasswordRequiresDigit()` | `"Passwords must have at least one digit ('0'-'9')."` | `"La contraseña debe incluir al menos un dígito ('0'-'9')."` |
| `PasswordRequiresLower()` | `"Passwords must have at least one lowercase ('a'-'z')."` | `"La contraseña debe incluir al menos una letra minúscula ('a'-'z')."` |
| `PasswordRequiresUpper()` | `"Passwords must have at least one uppercase ('A'-'Z')."` | `"La contraseña debe incluir al menos una letra mayúscula ('A'-'Z')."` |
| `PasswordRequiresUniqueChars(uniqueChars)` | `"Passwords must use at least {0} different characters."` | `"La contraseña debe contener al menos {0} caracteres distintos."` |
| `RecoveryCodeRedemptionFailed()` | `"Recovery code redemption failed."` | `"No se pudo canjear el código de recuperación."` |

(Final wording set in voice guide; the table above is the recommended baseline.)

**DI registration** (in `Program.cs`):

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(...)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddUserStore<SentinelAwareUserStore>()
    .AddErrorDescriber<EsCrIdentityErrorDescriber>()      // ← new line
    .AddDefaultTokenProviders();
```

The existing `SentinelAwareUserStore` and `AddDefaultTokenProviders` are unchanged.

---

## 4. In-Spec Deliverable Artifact: Voice Guide

**File:** `specs/012-es-cr-localization/voice-guide.md` (new)

Not runtime data. The voice guide is a spec-directory deliverable referenced by reviewers, never read by code.

### Required sections (per FR-020)

1. **Register declaration** — formal `usted`, never `tú` / `vos`.
2. **Tone descriptors** — warm, modern, concise, encouraging (mirror of spec 011).
3. **Glossary** — CR-specific term mappings. Minimum coverage:
   - Nouns: application, applicant, reviewer, approver, supplier, quotation, item, funding, agreement, signature, appeal, response, sender (system), notification.
   - Verbs: submit, review, approve, reject, send back, sign, appeal, regenerate, finalize, lock, supersede, replace, withdraw, resolve.
   - Adjectives/states: required, optional, valid, invalid, draft, pending, executed, frozen.
4. **Example pairs** — at least one for each surface family: Login screen, empty-state caption, status pill, validation error, Funding Agreement clause, success TempData message, Identity error, page title.

### Lifecycle

- Authored in commit N (where N < any per-view rewrite commit; satisfies SC-009 by construction).
- Reviewed by the voice owner from spec 011 (default; OQ-6 may swap to a CR-region reviewer).
- Treated as living: any future copy edit MUST update the voice guide first, then propagate.

---

## 5. New Test-Side Constants: `UiCopy`

**File:** `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` (new)

A `static class UiCopy` holding ~48 unique Spanish strings consumed by:

- 4 POMs (`AdminPage.cs`, `ApplicationPage.cs`, `ReviewApplicationPage.cs`, `ReviewQueuePage.cs`)
- Several test fixtures (notably `AuthenticatedTestBase` for the 37× repeated `"Add Supplier"` → `"Agregar proveedor"`)

```csharp
namespace FundingPlatform.Tests.E2E.Constants;

public static class UiCopy
{
    // Action buttons (consumed by POMs and test fixtures)
    public const string AddSupplier = "Agregar proveedor";
    public const string SubmitApplication = "Enviar solicitud";
    public const string SaveImpact = "Guardar impacto";
    public const string Impact = "Impacto";
    public const string SendBack = "Devolver";
    public const string FinalizeReview = "Finalizar revisión";
    // ... (≈48 unique strings; voice guide finalizes wording)

    // State labels (consumed by ToContainTextAsync state assertions)
    public static class State
    {
        public const string Draft = "Borrador";
        public const string Submitted = "Enviada";
        public const string Approved = "Aprobada";
        public const string Rejected = "Rechazada";
        // ... (mirror of StatusVisualMap labels — single source of truth at runtime is StatusVisualMap; tests duplicate the strings as a contract)
    }
}
```

**Validation rule**: every visible-text assertion in the E2E suite MUST source from `UiCopy` or be a one-off inline literal documented in the test method. Hard-coded English strings post-implementation are a test-code regression.

**Synchronization with `StatusVisualMap`**: the `UiCopy.State.*` constants intentionally duplicate the registry strings. If the registry's wording changes during voice-guide review, both sites update. (This is a deliberate two-location-with-tests-pinning pattern, not a single-source-of-truth violation — the test assertion *is* a contract about user-visible text.)

---

## Schema-First Compliance

Per constitution principle IV ("Schema-First Database Management"):

- ✅ No EF migrations.
- ✅ No `.sql` file changes (none in this spec).
- ✅ No `EnsureCreated` calls.
- ✅ No new tables, columns, indexes, views, or stored procedures.
- ✅ No seed-data changes (`PostDeployment/` untouched).

Schema-First is trivially satisfied — the feature is presentation + configuration only.

---

## State Transitions

None. This feature does not introduce any state machine, lifecycle, or workflow. The runtime state surfaces it touches (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`) keep their existing transitions; only their display labels translate.

---

## Summary

| Layer | Change |
|---|---|
| Domain | None |
| Application | None |
| Infrastructure | +1 file: `EsCrIdentityErrorDescriber.cs` |
| Web | View copy translated; controller-side strings translated; `StatusVisualMap.DisplayLabel` translated; new `RequestLocalization` middleware config; new `ModelBindingMessageProvider` config; new `EsCrCultureFactory.cs` (or equivalent helper) for the format overrides; brand SVG and `_Layout.cshtml` updated; `motion.js`/`facelift-init.js` namespace renamed |
| Database | None |
| Tests | +1 file: `UiCopy.cs`; existing POMs updated to consume it; visible-text assertions translated |
| Configuration | `appsettings*.json` + `AppHost.cs` + `FunderOptions.cs` defaults flip to `"es-CR"` |
| Spec deliverables | +1 file: `voice-guide.md` |

Total new files: 3 production, 1 test, 1 spec. Total new entities: 0.
