# Data Model: Admin Role and Admin Area

This feature adds **one new domain entity** (`ApplicationUser`, extending the existing default `IdentityUser`), **four new columns** on `dbo.AspNetUsers`, **one filtered index**, and **three new domain exceptions**. No new tables, no new aggregate roots, no schema changes outside `dbo.AspNetUsers`.

---

## ApplicationUser (new)

Replaces the default ASP.NET `IdentityUser` as the user entity throughout the codebase. Lives in `FundingPlatform.Domain.Entities`.

**Inherited (existing) properties** (set / managed by Identity, not modified by this spec):
- `Id` (string, primary key)
- `UserName`, `NormalizedUserName`
- `Email`, `NormalizedEmail`, `EmailConfirmed`
- `PasswordHash`
- `SecurityStamp`, `ConcurrencyStamp`
- `PhoneNumber`, `PhoneNumberConfirmed`
- `TwoFactorEnabled`
- `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`

**New properties** (this spec):

| Property | Type | DB column | Default | Notes |
|----------|------|-----------|---------|-------|
| `FirstName` | `string?` | `NVARCHAR(100) NULL` | `NULL` | Optional at DB level so existing rows pre-migration are valid; required in admin-area forms via view-model validation. |
| `LastName` | `string?` | `NVARCHAR(100) NULL` | `NULL` | Same rationale as `FirstName`. |
| `IsSystemSentinel` | `bool` | `BIT NOT NULL` | `0` | Set to `1` only for the sentinel admin (`admin@FundingPlatform.com`). Init-only on the C# property — there is no public setter; the only path to `true` is via `ApplicationUser.CreateSentinel(string email)` factory used by the seeder. |
| `MustChangePassword` | `bool` | `BIT NOT NULL` | `0` | Set to `1` by `UserAdministrationService.CreateUserAsync` (always, for new admin-created users) and by `UserAdministrationService.ResetUserPasswordAsync`. Cleared by `AccountController.ChangePassword(POST)` after a successful change. The middleware (`MustChangePasswordMiddleware`) reads this flag on every authenticated request and redirects to `/Account/ChangePassword` if `true`. |

**Construction**:
- `public ApplicationUser()` — parameterless constructor required by EF Core. All four new properties default to their column defaults (`null`, `null`, `false`, `false`).
- `public ApplicationUser(string email, string firstName, string lastName, string? phone)` — convenience constructor for normal users; sets `Email`, `UserName` (= email), `NormalizedEmail`, `NormalizedUserName`, `FirstName`, `LastName`, `PhoneNumber`.
- `public static ApplicationUser CreateSentinel(string email)` — static factory used **only** by the seeder. Sets `Email`, `UserName`, normalized variants, and `IsSystemSentinel = true`. `FirstName` / `LastName` left `null` so the sentinel has no display name (irrelevant — never appears in any UI). `MustChangePassword = false` (sentinel password is set by the seeder; we do not force the operator to change it on first sign-in).

**EF Core mapping**: convention-based (column names match property names). No explicit `IEntityTypeConfiguration<ApplicationUser>` needed.

**Global query filter**: applied in `AppDbContext.OnModelCreating` after `base.OnModelCreating(builder)`:

```text
builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsSystemSentinel);
```

This filter is bypassed only by:
- `SentinelAwareUserStore` (custom `UserStore` override) — for sign-in path lookups via `FindByEmailAsync`, `FindByNameAsync`, `FindByIdAsync`.
- `UserAdministrationService` — explicit `_dbContext.Users.IgnoreQueryFilters()` calls inside the service-layer guard fetches and inside the last-admin-counting query.
- Future features that legitimately need to enumerate all users including the sentinel — must explicitly call `IgnoreQueryFilters()`.

---

## dbo.AspNetUsers (modified)

The dacpac project's `Tables/dbo.AspNetUsers.sql` file is updated to add the four new columns and one filtered index. All existing columns and indexes are preserved.

**Added columns** (in column order at end of table):
```text
[FirstName]            NVARCHAR(100) NULL,
[LastName]             NVARCHAR(100) NULL,
[IsSystemSentinel]     BIT           NOT NULL CONSTRAINT [DF_AspNetUsers_IsSystemSentinel] DEFAULT (0),
[MustChangePassword]   BIT           NOT NULL CONSTRAINT [DF_AspNetUsers_MustChangePassword] DEFAULT (0)
```

**Added filtered index**:
```text
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_Sentinel]
    ON [dbo].[AspNetUsers] ([Id])
    WHERE [IsSystemSentinel] = 1;
```

The filtered index makes "find sentinel" queries (used by the seeder's idempotency check and by the `SentinelAwareUserStore`'s targeted lookups) effectively O(1). Storage cost is negligible because only one row matches the predicate.

**Schema deployment**: dacpac diff applies the new columns with their DEFAULT constraints, so existing rows (the demo seed users) get `IsSystemSentinel = 0` and `MustChangePassword = 0` automatically — no data migration script required. `FirstName` / `LastName` become `NULL` for those rows; the admin area's edit form lets an admin populate them after the fact if desired (not required for the demo users to keep working).

---

## Domain exceptions (new)

Three exception types live in `FundingPlatform.Domain.Exceptions`. All inherit from a common base (`DomainException` if it exists, otherwise plain `Exception`) and carry a stable `ErrorCode` string for the controller to map.

| Exception | ErrorCode | Thrown when |
|-----------|-----------|-------------|
| `SentinelUserModificationException` | `SENTINEL_IMMUTABLE` | Any write operation in `UserAdministrationService` targets a user whose `IsSystemSentinel = true`. Caught by the controller layer; mapped to a 400-class HTTP response with the `_EmptyState` partial messaging. |
| `LastAdministratorException` | `LAST_ADMIN_PROTECTED` | A disable, role-change-away-from-Admin, or other action would leave zero active non-sentinel admins. Surfaced inline on the admin form as a named-rule validation error. |
| `SelfModificationException` | `SELF_MODIFICATION_BLOCKED` | An admin attempts to disable themselves, change their own role, or change their own email from the admin area. Carries an enum-valued `Action` property indicating which of the three was attempted, so the UI can surface a precise message. |

The three exceptions are caught at the controller boundary (each `AdminUsersController` action wraps the `IUserAdministrationService` call in a `try/catch` block targeting the three types). The catch block adds a `ModelState` error with the `ErrorCode` as the message key (looked up against a small static `AdminErrorMessages` dictionary in the Web layer for human-readable copy) and returns the appropriate view, preserving any field-level model state.

---

## Applicant entity (existing — relationship rules clarified, not modified)

The existing `Applicant` domain entity (`FundingPlatform.Domain.Entities.Applicant`) is **not modified** by this spec. The relationship rules between `ApplicationUser` and `Applicant` that this spec depends on:

- One `Applicant` row per user with role `Applicant`. The `Applicant.UserId` foreign key references `ApplicationUser.Id`.
- When `UserAdministrationService.CreateUserAsync` creates a user with role `Applicant`, it persists the matching `Applicant` row in the same `SaveChangesAsync` transaction (atomic). On collision (e.g., duplicate `LegalId`), the entire transaction rolls back and no user or applicant row is created (Edge Case in spec).
- When an existing `Applicant` is demoted to `Reviewer` or `Admin`, the `Applicant` row is **preserved** (history). The user no longer holds the `Applicant` role; the legal-id field disappears from the admin edit form.
- When a previously-Applicant user is re-promoted to `Applicant`, the existing `Applicant` row is **reused** (looked up by `UserId`); a new row is **not** created. The legal-id field reappears in the edit form.
- The `Applicant.PerformanceScore` field is **not** managed from the admin area (spec FR-008). It remains under evaluation-domain control as before.

These rules are enforced in `UserAdministrationService.UpdateUserAsync` and `UserAdministrationService.CreateUserAsync` via straightforward `await _dbContext.Applicants.FirstOrDefaultAsync(a => a.UserId == user.Id)` lookups.

---

## State transitions (User Active ↔ Disabled)

Identity's existing `LockoutEnd` mechanism encodes the Active / Disabled state. The admin area presents this as a binary status; underneath, the rules are:

- **Active**: `LockoutEnd IS NULL` or `LockoutEnd <= UtcNow` (the lockout has expired or was never set).
- **Disabled**: `LockoutEnd > UtcNow`. The admin area sets it to `DateTimeOffset.MaxValue` so the lockout never expires until an admin explicitly enables.
- **Disable transition**: `await _userManager.SetLockoutEnabledAsync(user, true)` (idempotent — Identity defaults `LockoutEnabled = true` for all users), then `await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)`, then `await _userManager.UpdateSecurityStampAsync(user)` to invalidate active sessions.
- **Enable transition**: `await _userManager.SetLockoutEndDateAsync(user, null)`. No security-stamp rotation needed (the user wasn't currently authenticated; no session to invalidate; sign-in continues to work with their existing password).

The sentinel never transitions — its lockout and other write operations are blocked by the sentinel guard in the service layer before any state change.

---

## Sidebar entries (extension of existing)

Spec 008 introduced `FundingPlatform.Web.Models.SidebarEntry` plus the role-aware sidebar partial. This spec extends the entry list (in the controller or view-component that builds the sidebar) with two new entries:

| Entry | URL | Role gate | Order |
|-------|-----|-----------|-------|
| Users | `/Admin/Users` | `Admin` | After existing Reviewer entries |
| Reports | `/Admin/Reports` | `Admin` | After Users |

Visibility check: the sidebar partial calls `User.IsInRole("Admin")`. Because of the `AdminImpliesReviewerClaimsTransformation` (research Decision 3), Admins also pass `User.IsInRole("Reviewer")` checks — desired for the existing reviewer-only sidebar entries to also be visible to Admins.

---

## Out-of-scope data shapes

- **No audit-log table**. Deferred to a future compliance/reporting spec (spec Out of Scope).
- **No `AdminAuditEntries` or similar** in this feature.
- **No new role entities**. Three roles exist already (`Applicant`, `Reviewer`, `Admin`).
- **No new aggregates**. `ApplicationUser` is a simple Identity-extension; the `Applicant` aggregate is unchanged.
