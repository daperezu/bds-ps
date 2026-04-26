---
description: "Tasks for 009-admin-area"
---

# Tasks: Admin Role and Admin Area

**Input**: Design documents in `/specs/009-admin-area/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. Constitution Principle III makes Playwright E2E tests non-negotiable. FR-026 mandates E2E coverage for every user story (lifecycle, sentinel exclusion + immutability, last-admin guard, self-modification guard, Reviewer-inheritance, reports stub, role-aware sidebar). Tests are written **before** implementation per TDD; expected red until the corresponding implementation tasks land.

**Organization**: Tasks grouped by user story (US1, US2, US3, US4, US5, US6) per spec.md priority order. Each phase is a checkpoint at which build, the existing E2E suite (specs 001–008), and the spec invariants for the work landed so far must all be green.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3, US4, US5, US6 — maps to user stories in spec.md
- File paths are absolute-from-repo-root

## Path Conventions

- Web application layout per plan.md §Project Structure
- `src/FundingPlatform.Domain/` — `ApplicationUser` entity + 3 domain exceptions
- `src/FundingPlatform.Application/Admin/Users/` — `IUserAdministrationService`, DTOs, service implementation
- `src/FundingPlatform.Infrastructure/Identity/` — `SentinelAwareUserStore`, `AdminImpliesReviewerClaimsTransformation`, `IdentityConfiguration` (sentinel seed step added)
- `src/FundingPlatform.Infrastructure/Persistence/` — `AppDbContext` (generic swap + global filter)
- `src/FundingPlatform.Database/Tables/dbo.AspNetUsers.sql` — four new columns + filtered index
- `src/FundingPlatform.Web/Controllers/Admin/` — new attribute-routed controllers (`AdminUsersController`, `AdminReportsController`)
- `src/FundingPlatform.Web/Controllers/AccountController.cs` — modified (cascade rename + new actions)
- `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` — modified (cascade rename only)
- `src/FundingPlatform.Web/Middleware/MustChangePasswordMiddleware.cs` — new
- `src/FundingPlatform.Web/Program.cs` — modified (Identity swap, cookie + security-stamp config, middleware insertion, sentinel seed call)
- `src/FundingPlatform.Web/ViewModels/Admin/` — view-models for the admin-area forms
- `src/FundingPlatform.Web/Views/Admin/Users/`, `Views/Admin/Reports/`, `Views/Account/` — new and modified Razor views
- `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/` — new PageObjects per admin surface
- `tests/FundingPlatform.Tests.E2E/Tests/Admin/` — new test classes per user story

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify a clean baseline build + green E2E suite before any 009 changes land. Capture the existing demo-seed user state. No production-code edits.

- [X] T001 Run `dotnet build --nologo` at the repo root and confirm zero errors and zero warnings on branch `009-admin-area` before making any changes; this is the baseline against which every subsequent task is measured.
- [X] T002 [P] Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm zero failures on the baseline branch; this is the green starting state that every subsequent task must preserve. Save the test summary line for comparison.
- [X] T003 [P] Document the existing demo-seed users that must continue to behave as regular accounts (i.e., NOT receive the sentinel flag): `applicant@demo.com`, `reviewer@demo.com`, `admin@demo.com` (all with password `Demo123!`, per `src/FundingPlatform.Infrastructure/Identity/IdentityConfiguration.cs`). These are NOT the sentinel; they remain visible, modifiable, and disable-able through the admin area once US1 lands. The sentinel is `admin@FundingPlatform.com` only.

**Checkpoint**: Baseline build and E2E green. No code changes yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land all schema, type, and DI changes that every user story depends on. After this phase the build compiles, all existing E2E tests still pass, and the project has the new `ApplicationUser` entity, the four new `dbo.AspNetUsers` columns, the global query filter on `ApplicationUser`, the cascade rename across `AccountController` / `FundingAgreementController` / `IdentityConfiguration`, the `AccessDenied` page (now reachable when any user hits an unauthorized route), and the cookie/security-stamp config tweaks.

**⚠️ CRITICAL**: No US1–US6 work should begin until this phase is complete. Sentinel seeding (US2), user-administration service (US1), claims transformation (US4), and admin-area controllers (US5/US6) all depend on the foundation laid here.

### Domain types

- [X] T004 [P] Create `src/FundingPlatform.Domain/Entities/ApplicationUser.cs` per `data-model.md §"ApplicationUser (new)"`: extends `Microsoft.AspNetCore.Identity.IdentityUser`, adds `string? FirstName`, `string? LastName`, `bool IsSystemSentinel { get; init; }` (init-only), `bool MustChangePassword`. Provide parameterless ctor (EF), convenience ctor `(string email, string firstName, string lastName, string? phone)`, and **static factory `CreateSentinel(string email)`** as the only path that sets `IsSystemSentinel = true`. Living in namespace `FundingPlatform.Domain.Entities`.
- [X] T005 [P] Create `src/FundingPlatform.Domain/Exceptions/SentinelUserModificationException.cs` per `data-model.md §"Domain exceptions"`: a sealed exception type with `ErrorCode = "SENTINEL_IMMUTABLE"` and a constructor accepting an optional message. If a `DomainException` base class exists in the project, derive from it; otherwise derive from `Exception`.
- [X] T006 [P] Create `src/FundingPlatform.Domain/Exceptions/LastAdministratorException.cs`: same pattern as T005 with `ErrorCode = "LAST_ADMIN_PROTECTED"`.
- [X] T007 [P] Create `src/FundingPlatform.Domain/Exceptions/SelfModificationException.cs`: same pattern with `ErrorCode = "SELF_MODIFICATION_BLOCKED"` PLUS an enum-valued property `SelfModificationAction Action` indicating which of `DisableSelf`, `ChangeOwnRole`, `ChangeOwnEmail` was attempted.

### Database schema (dacpac)

- [X] T008 Modify `src/FundingPlatform.Database/Tables/dbo.AspNetUsers.sql` per `data-model.md §"dbo.AspNetUsers (modified)"`: add four new columns at the end of the column list — `[FirstName] NVARCHAR(100) NULL`, `[LastName] NVARCHAR(100) NULL`, `[IsSystemSentinel] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_IsSystemSentinel] DEFAULT (0)`, `[MustChangePassword] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_MustChangePassword] DEFAULT (0)`. Then ADD a filtered index `CREATE NONCLUSTERED INDEX [IX_AspNetUsers_Sentinel] ON [dbo].[AspNetUsers] ([Id]) WHERE [IsSystemSentinel] = 1;`. Existing columns and indexes preserved.
- [X] T009 Build the dacpac project (`dotnet build src/FundingPlatform.Database`) and confirm zero errors. The dacpac diff against the previous schema must show only the four new columns + the new filtered index.

### Persistence layer

- [X] T010 Modify `src/FundingPlatform.Infrastructure/Persistence/AppDbContext.cs` per `plan.md §Project Structure`: change class declaration from `AppDbContext : IdentityDbContext` to `AppDbContext : IdentityDbContext<ApplicationUser>`. Add `using FundingPlatform.Domain.Entities;` if not already present. In `OnModelCreating(ModelBuilder builder)`, after the existing `base.OnModelCreating(builder)` and `builder.ApplyConfigurationsFromAssembly(...)`, add a single line: `builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsSystemSentinel);` per `research.md §Decision 2`. The existing private-backing-field navigation binding for `AppEntity.FundingAgreement` is preserved.

### Identity wiring (Program.cs)

- [X] T011 Modify `src/FundingPlatform.Web/Program.cs` per `plan.md §Project Structure` and `research.md §Decisions 1, 4, 8, 10`:
  - Change `builder.Services.AddIdentity<IdentityUser, IdentityRole>(...)` to `builder.Services.AddIdentity<ApplicationUser, IdentityRole>(...)`. The options block (password rules, RequireConfirmedAccount=false) stays unchanged.
  - In the `ConfigureApplicationCookie` block, change `options.AccessDeniedPath = "/Account/Login"` to `options.AccessDeniedPath = "/Account/AccessDenied"`. `LoginPath` and `LogoutPath` unchanged.
  - Immediately after `ConfigureApplicationCookie(...)`, add `builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));` per `research.md §Decision 4`.
  - Add `using FundingPlatform.Domain.Entities;` at the top of the file if not present.
  - Do NOT yet add the IClaimsTransformation, the SentinelAwareUserStore, the MustChangePasswordMiddleware, or the sentinel seed call — those land in their respective story phases.

### Cascade rename: `IdentityUser` → `ApplicationUser`

- [X] T012 [P] Modify `src/FundingPlatform.Web/Controllers/AccountController.cs`: change `UserManager<IdentityUser>` → `UserManager<ApplicationUser>` and `SignInManager<IdentityUser>` → `SignInManager<ApplicationUser>` in field declarations, constructor parameters, and any local variables. Add `using FundingPlatform.Domain.Entities;`. **No behavioral change**. New actions (`AccessDenied`, `ChangePassword`) are added later (T013 for `AccessDenied`, US1 phase for `ChangePassword`).
- [X] T013 Modify `src/FundingPlatform.Web/Controllers/AccountController.cs` to add a new action: `[HttpGet, AllowAnonymous] public IActionResult AccessDenied() { Response.StatusCode = StatusCodes.Status403Forbidden; return View(); }`. The action returns the new `Views/Account/AccessDenied.cshtml` with HTTP status 403. Per `research.md §Decision 8`. (T012 is the rename; T013 is additive — T013 must come after T012 because they touch the same file.)
- [X] T014 [P] Create `src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml`: Razor view rendering `_PageHeader` (Title = "Access Denied", optional subtitle "You do not have permission to view this resource.") plus an `_EmptyState` partial with icon, headline "403 Forbidden", and body explaining the user's session is fine but the resource is restricted. No data, no actions besides a "Return to home" link. Layout = default `_Layout` (the user is authenticated; no auth shell needed).
- [X] T015 [P] Modify `src/FundingPlatform.Web/Controllers/FundingAgreementController.cs`: cascade rename `UserManager<IdentityUser>` → `UserManager<ApplicationUser>`. Add `using FundingPlatform.Domain.Entities;`. **No behavioral change.**
- [X] T016 Modify `src/FundingPlatform.Infrastructure/Identity/IdentityConfiguration.cs`: cascade rename `UserManager<IdentityUser>` → `UserManager<ApplicationUser>` inside `SeedUsersAsync`. Update the `var user = new IdentityUser { ... }` line to `var user = new ApplicationUser(seed.Email, seed.FirstName, seed.LastName, phone: null) { Email = seed.Email };` so the demo seed users now carry first/last names (currently they have those fields stored on the `Applicant` row, but per the spec, name fields live on `ApplicationUser` from now on). Existing demo password and role assignments preserved. **The sentinel seed step (`SeedSentinelAdminAsync`) is added in US2 phase, NOT here.**
- [X] T017 Run `dotnet build --nologo` and confirm zero errors after T004–T016. If any cascade-rename references were missed (e.g., other usages of `UserManager<IdentityUser>` or `SignInManager<IdentityUser>` not enumerated above), fix them in the same task and document each in a comment line in the task body for future audit. Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm zero failures — the cascade rename is type-only and observable only through reflection; no test should break.

### E2E PageObject base for admin area

- [X] T018 [P] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminBasePage.cs`: an abstract or virtual base class for admin-area PageObjects, anchored on `[data-testid="page-title"]` and `[data-testid="admin-area"]` (the latter to be added to the new admin views in US1/US5). Exposes locators for the page-header, the primary action button (when present), the empty-state region (when present), and the confirmation dialog (when present). Inherits from the existing `BasePage` if one exists (per spec 008's foundational PageObject layer).

**Checkpoint**: All foundational types compile. Database project builds clean. `dotnet build` is green. Existing E2E suite is green (the cascade rename does not change observable behavior). No user-facing change yet — the admin area surfaces don't exist; sentinel doesn't exist; no admin can do anything new.

---

## Phase 3: User Story 1 — Admin manages the full user lifecycle from the UI (Priority: P1) 🎯 MVP

**Goal**: Land the user-management surface at `/Admin/Users` (list / create / edit / disable / enable / reset password), the must-change-password flow (`MustChangePasswordMiddleware` + `Account/ChangePassword`), and the cascading session-invalidation behavior on email/role/disable/password-reset. After this phase, an Admin can perform the full lifecycle on any non-sentinel, non-self target user without engineering involvement.

**Independent Test**: `AdminUserLifecycleTests` passes. Manual smoke per `quickstart.md §"Golden-path smoke (US1 — full lifecycle)"`: create test user, force-change-password on first login, edit email (session invalidated), edit role (session invalidated), reset password (force-change again), disable, enable.

### Tests for User Story 1 (write first; expect red before implementation)

- [X] T019 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminUsersListPage.cs` — locators for the listing table, role/status filter dropdowns, search box, pagination controls, "Create user" button, per-row action menu (Edit, Disable, Enable, Reset Password). Helper methods: `GoTo()`, `ClickCreate()`, `Search(string text)`, `FilterByRole(string role)`, `RowFor(string email)`, etc.
- [X] T020 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminUserCreatePage.cs` — locators for the create form fields (`FirstName`, `LastName`, `Email`, `Phone`, `Role` dropdown, `InitialPassword`, `LegalId` — the last conditionally visible). Helper: `Fill(...)`, `Submit()`, `ConditionallyVisibleLegalId()`.
- [X] T021 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminUserEditPage.cs` — same field locators as create, plus the rowId in the URL path. Helper: `Fill(...)`, `Submit()`.
- [X] T022 [P] [US1] Create `tests/FundingPlatform.Tests.E2E/PageObjects/ChangePasswordPage.cs` — locators for the change-password form (Old/New/Confirm fields), submit button, success redirect target.
- [X] T023 [US1] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/AdminUserLifecycleTests.cs` per `quickstart.md §"Golden-path smoke (US1)"`. Test methods (each independent):
  - `Admin_CreateReviewer_Succeeds_NewUserMustChangePasswordOnFirstLogin` — create + first-login redirect to ChangePassword.
  - `Admin_CreateApplicant_RequiresLegalId_AndPersistsApplicantRow` — verify atomic creation + LegalId required.
  - `Admin_EditEmail_InvalidatesTargetUserSession_TargetMustReauthenticate` — open second browser context as the target user; admin edits email; assert second context's next request triggers re-auth (within ≤60s per `SecurityStampValidationInterval` from T011).
  - `Admin_EditRole_InvalidatesTargetUserSession` — same pattern; role change forces re-auth.
  - `Admin_ResetPassword_RequiresConfirmation_SetsMustChangeFlag` — _ConfirmDialog appears; after confirm, target's session invalidated and next login forces change.
  - `Admin_DisableUser_RequiresConfirmation_PreventsLogin` — _ConfirmDialog; disable; target cannot log in.
  - `Admin_EnableDisabledUser_AllowsLoginWithExistingPassword` — re-enable does NOT force password change.
  - `Admin_DemoteApplicantToReviewer_PreservesApplicantRecord` — demote; the user's `Applicant` row is intact in the DB; legal-id field becomes hidden in the edit form.
  - `Admin_CreateUserForm_AllValidationErrorsSurfacedTogether` — submit invalid form (bad email + weak password + missing legal id for Applicant); assert all three error messages render simultaneously, not one at a time.

### Implementation for User Story 1

- [X] T024 [P] [US1] Create `src/FundingPlatform.Application/Admin/Users/IUserAdministrationService.cs` per `contracts/README.md §2`: interface with seven async methods (`ListUsersAsync`, `GetUserAsync`, `CreateUserAsync`, `UpdateUserAsync`, `DisableUserAsync`, `EnableUserAsync`, `ResetUserPasswordAsync`). Each non-list method takes the request DTO + `string actorUserId` + `CancellationToken`. List takes `ListUsersRequest`. Return shapes per `contracts/README.md §3`.
- [X] T025 [P] [US1] Create `src/FundingPlatform.Application/Admin/Users/DTOs/` directory with the 7 DTO record files per `contracts/README.md §3`: `ListUsersRequest.cs`, `ListUsersResult.cs`, `UserSummaryDto.cs`, `UserDetailDto.cs`, `CreateUserRequest.cs`, `UpdateUserRequest.cs`, `ResetPasswordRequest.cs`. Also `DomainError.cs` if not already present in the project. Use positional records.
- [X] T026 [US1] Create `src/FundingPlatform.Application/Admin/Users/Services/UserAdministrationService.cs` implementing `IUserAdministrationService`. Phase-3 scope (NO sentinel guard yet — added in US2; NO last-admin / self-mod guards yet — added in US3; both are wired here as no-op placeholders that will throw in later phases). Methods:
  - `ListUsersAsync` — paginated query against `_dbContext.Users` (the global filter excludes sentinel automatically, even though no sentinel exists yet). Joins to `_dbContext.UserRoles` + `_dbContext.Roles` to compute role; computes status from `LockoutEnd`. Returns paged result.
  - `GetUserAsync` — `_dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)` (filter applied; returns null for sentinel — desired). Maps to `UserDetailDto`.
  - `CreateUserAsync` — validates input; constructs `ApplicationUser` via convenience ctor; sets `MustChangePassword = true`; calls `_userManager.CreateAsync(user, request.InitialPassword)`; on Identity errors maps to `DomainError` list with `WEAK_PASSWORD` / `EMAIL_IN_USE` / etc. codes; on success calls `_userManager.AddToRoleAsync(user, request.Role)`; if `Role == "Applicant"` finds-or-creates the `Applicant` row with `LegalId`; everything in one DB transaction (rollback on any failure). Returns `Result<UserDetailDto>`.
  - `UpdateUserAsync` — `_dbContext.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == request.UserId)` (per `research.md §Decision 2`); applies email change (with `UpdateNormalizedEmailAsync` + `SetUserNameAsync` + `UpdateSecurityStampAsync`); applies first/last/phone changes via direct property + `UserManager.UpdateAsync`; applies role change via `RemoveFromRolesAsync` + `AddToRoleAsync` + `UpdateSecurityStampAsync`; manages `Applicant` row when role transitions involve `Applicant`. Per `data-model.md §"Applicant entity"`.
  - `DisableUserAsync` — `SetLockoutEnabledAsync(true)` + `SetLockoutEndDateAsync(DateTimeOffset.MaxValue)` + `UpdateSecurityStampAsync`.
  - `EnableUserAsync` — `SetLockoutEndDateAsync(null)`.
  - `ResetUserPasswordAsync` — `RemovePasswordAsync` then `AddPasswordAsync(newPassword)`; sets `MustChangePassword = true`; calls `UpdateSecurityStampAsync`.
  All write methods accept `actorUserId` but ignore it for now (the self-mod guard in US3 uses it).
- [X] T027 [US1] Modify `src/FundingPlatform.Infrastructure/DependencyInjection.cs` to register the service: `services.AddScoped<IUserAdministrationService, UserAdministrationService>()`. The service implementation lives in the Application project; this file is the wire-up.
- [X] T028 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/Admin/AdminUserSummaryRowViewModel.cs` per `contracts/README.md §4`: properties for `Id`, `FullName`, `Email`, `Role`, `Status`, `CreatedAt`, `IsSelf`. Used by the listing rows.
- [X] T029 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/Admin/AdminUsersListViewModel.cs` per `contracts/README.md §4`: properties for the page of rows + filter/search/pagination state.
- [X] T030 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/Admin/AdminUserCreateViewModel.cs` per `contracts/README.md §4`: with `[Required]`, `[EmailAddress]`, `[StringLength]`, `[Phone]`, `[DataType(DataType.Password)]` annotations. `LegalId` server-side-enforced as required when `Role == "Applicant"` (a custom validation attribute or a controller-side check; pick the approach that fits the project's existing convention).
- [X] T031 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/Admin/AdminUserEditViewModel.cs`: derives from `AdminUserCreateViewModel` minus `InitialPassword`, plus `[Required] string UserId`.
- [X] T032 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/Admin/AdminUserResetPasswordViewModel.cs`: `UserId`, `NewTemporaryPassword`, `ConfirmPassword` with `[Compare]` attribute per `contracts/README.md §4`.
- [X] T033 [P] [US1] Create `src/FundingPlatform.Web/ViewModels/ChangePasswordViewModel.cs`: `OldPassword`, `NewPassword`, `ConfirmPassword` (with `[Compare]`).
- [X] T034 [US1] Create `src/FundingPlatform.Web/Controllers/Admin/AdminUsersController.cs` per `contracts/README.md §1`: class attributes `[Authorize(Roles = "Admin")]` and `[Route("Admin/Users")]`. Inject `IUserAdministrationService`, `UserManager<ApplicationUser>` (only for the `actorUserId` resolution), and `ILogger<AdminUsersController>`. Implement actions `Index`, `Create` (GET+POST), `Edit` (GET+POST), `Disable` (POST), `Enable` (POST), `ResetPassword` (GET+POST). Map `DomainError`s to `ModelState` errors; map the three guard exceptions to named-rule UI errors (the codes are no-op in this phase since the guards aren't wired yet — the catch blocks are added now and will fire after US2/US3).
- [X] T035 [US1] Create `src/FundingPlatform.Web/Middleware/MustChangePasswordMiddleware.cs` per `research.md §Decision 5`: checks `User.Identity.IsAuthenticated`, fetches the user via `UserManager.FindByIdAsync(User.GetUserId())`, short-circuits with redirect to `/Account/ChangePassword` if `user.MustChangePassword == true` AND the request path is not `/Account/ChangePassword` and not `/Account/Logout` and not a static asset. Static-asset detection via `HttpContext.Request.Path.StartsWithSegments("/lib")` plus a few common extensions, OR via the `IStaticFilesEndpointRouteBuilderExtensions` exclusion if simpler.
- [X] T036 [US1] Modify `src/FundingPlatform.Web/Program.cs`: insert `app.UseMiddleware<MustChangePasswordMiddleware>();` between `app.UseAuthorization()` and the route mapping (`app.MapControllerRoute(...)`). Order matters — must run AFTER `UseAuthentication`/`UseAuthorization` so `User.Identity.IsAuthenticated` is populated.
- [X] T037 [US1] Modify `src/FundingPlatform.Web/Controllers/AccountController.cs` to add `ChangePassword` (GET) and `ChangePassword` (POST) actions per `contracts/README.md §1`. POST handler: `var user = await _userManager.GetUserAsync(User)`, validate `OldPassword`, call `_userManager.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword)`, on success set `user.MustChangePassword = false`, call `_userManager.UpdateSecurityStampAsync(user)`, sign out + sign back in (so the new claims are effective), redirect to `/`. On Identity errors map to `ModelState`.
- [X] T038 [US1] Modify the existing `Login` POST action in `AccountController.cs` to check `user.MustChangePassword` AFTER successful sign-in and BEFORE the redirect. If true, redirect to `/Account/ChangePassword` instead of the normal landing page. (The middleware T035 catches this on subsequent requests; this is the first-request fast path.)
- [X] T039 [P] [US1] Create `src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml` per `contracts/README.md §1`: `_PageHeader` (Title = "Change Password", subtitle "You must change your password before continuing.") + `_FormSection` for the three password fields + `_ActionBar` with a single primary "Change Password" button. Layout = default `_Layout` (user is authenticated).
- [X] T040 [P] [US1] Create `src/FundingPlatform.Web/Views/Admin/Users/Index.cshtml` per `plan.md §Project Structure`: `_PageHeader` (Title = "Users", optional `PrimaryActions = ["Create user" → /Admin/Users/Create]`) + filter/search form (role dropdown, status dropdown, search box) + `_DataTable` rendering the rows + `_StatusPill` for Status and Role columns + per-row dropdown action menu (Edit, Disable/Enable, Reset Password) where each destructive action triggers a `_ConfirmDialog` + `_EmptyState` when the page is empty + pagination controls. Set `data-testid="admin-area"` on the outer wrapper.
- [X] T041 [P] [US1] Create `src/FundingPlatform.Web/Views/Admin/Users/Create.cshtml`: `_PageHeader` (Title = "Create User") + `_FormSection`s for each field group + JS-driven conditional reveal of `LegalId` when `Role == "Applicant"` (small inline script using a `data-role` attribute on the dropdown — single file, no new JS module needed) + `_ActionBar` with "Create" + "Cancel" actions.
- [X] T042 [P] [US1] Create `src/FundingPlatform.Web/Views/Admin/Users/Edit.cshtml`: same structure as Create, minus the `InitialPassword` field, plus a `Reset Password` action linking to `/Admin/Users/{id}/ResetPassword`.
- [X] T043 [P] [US1] Create `src/FundingPlatform.Web/Views/Admin/Users/ResetPassword.cshtml`: `_PageHeader` (Title = "Reset Password — {targetEmail}") + `_FormSection` with the new password / confirm fields + `_ActionBar` with `_ConfirmDialog`-gated Submit.
- [X] T044 [US1] Run `dotnet build --nologo` and confirm zero errors. Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm: (a) every existing test from specs 001–008 still passes; (b) `AdminUserLifecycleTests` is GREEN (since US1 implementation is now complete). The three test methods that exercise the *guards* (sentinel/last-admin/self-mod) are NOT in this story's test class — they live in US2/US3 phases.

**Checkpoint**: User-management lifecycle works end-to-end. Sentinel doesn't exist yet; admin can edit themselves (no guard yet). The full E2E suite is green except for the as-yet-unimplemented US2/US3/US4/US5/US6 test classes (which don't exist yet).

---

## Phase 4: User Story 2 — Sentinel default admin: seeded, hidden, immutable, recovery-only (Priority: P1)

**Goal**: Land the sentinel-admin seeder (with log-before-create ordering), the `SentinelAwareUserStore` find-bypass, and the service-layer sentinel-modification guard. After this phase the sentinel exists in every fresh deploy, never appears in user listings, can sign in, and rejects every modification attempt at both the query and the service layer.

**Independent Test**: `SentinelExclusionTests` and `SentinelImmutabilityTests` pass. Manual smoke per `quickstart.md §"Sentinel-immutability smoke (US2)"`.

### Tests for User Story 2 (write first; expect red before implementation)

- [ ] T045 [P] [US2] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/SentinelExclusionTests.cs` — query-layer assertions:
  - `Sentinel_NotInUsersList` — log in as a regular Admin, navigate `/Admin/Users`, search by `admin@FundingPlatform.com`; assert zero rows regardless of filter.
  - `Sentinel_NotInUsersList_WithRoleFilterAdmin` — same but with role=Admin filter; sentinel still hidden.
  - `Sentinel_NotInUsersList_WithSearchEmailFragment` — search by `FundingPlatform`; sentinel still hidden.
- [ ] T046 [P] [US2] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/SentinelImmutabilityTests.cs` — service-layer assertions; each test constructs a direct authenticated POST (e.g., via `HttpClient`) targeting the sentinel's user id (looked up via `IgnoreQueryFilters` from the test fixture):
  - `Sentinel_DirectEditPost_Rejected` — POST `/Admin/Users/{sentinelId}/Edit` with field changes; assert response is 4xx with `SENTINEL_IMMUTABLE` named-rule error; sentinel state unchanged.
  - `Sentinel_DirectDisablePost_Rejected` — POST `/Admin/Users/{sentinelId}/Disable`; same.
  - `Sentinel_DirectResetPasswordPost_Rejected` — POST `/Admin/Users/{sentinelId}/ResetPassword`; same.
  - `Sentinel_CreateUserWithSentinelEmail_Rejected` — POST `/Admin/Users/Create` with `Email = admin@FundingPlatform.com`; assert `EMAIL_IN_USE` validation error.
  - `Sentinel_CanLogIn` — using a deterministic configured password (set via `Admin:DefaultPassword` in the fixture configuration), POST to `/Account/Login`; assert success and `User.IsInRole("Admin")` is true.
  - `Sentinel_LoginCookie_NotMustChangePassword` — sentinel does NOT trigger the `MustChangePasswordMiddleware`; navigation to `/` succeeds without redirect.

### Implementation for User Story 2

- [ ] T047 [P] [US2] Create `src/FundingPlatform.Infrastructure/Identity/SentinelAwareUserStore.cs` per `research.md §Decision 2`: subclasses `UserStore<ApplicationUser, IdentityRole, AppDbContext>`. Overrides `FindByEmailAsync(string normalizedEmail, CancellationToken ct)`, `FindByNameAsync(string normalizedUserName, CancellationToken ct)`, and `FindByIdAsync(string userId, CancellationToken ct)`. Each override is implemented as `Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.<predicate>, ct)` where `Users` is the `IQueryable<ApplicationUser>` exposed by the base store.
- [ ] T048 [US2] Modify `src/FundingPlatform.Web/Program.cs`: change the Identity registration chain from `.AddEntityFrameworkStores<AppDbContext>()` to `.AddEntityFrameworkStores<AppDbContext>().AddUserStore<SentinelAwareUserStore>()`. Add `using FundingPlatform.Infrastructure.Identity;` if not already present. Confirm Identity still resolves at startup.
- [ ] T049 [US2] Modify `src/FundingPlatform.Application/Admin/Users/Services/UserAdministrationService.cs`: add the **sentinel guard** at the top of every write method (`UpdateUserAsync`, `DisableUserAsync`, `EnableUserAsync`, `ResetUserPasswordAsync`). The guard:
  ```text
  var target = await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
  if (target is null) return Result.NotFound(...);
  if (target.IsSystemSentinel) throw new SentinelUserModificationException(...);
  ```
  Per `research.md §Decision 2` and `contracts/README.md §2 method semantics`.
- [ ] T050 [US2] Modify `src/FundingPlatform.Web/Controllers/Admin/AdminUsersController.cs`: ensure the `try/catch` block around each `IUserAdministrationService` call catches `SentinelUserModificationException` and adds a `ModelState` error with key `""` (form-level) and message resolved from a small static `AdminErrorMessages` dictionary using `SENTINEL_IMMUTABLE` as the lookup key (default copy: "This account is a system account and cannot be modified."). The dictionary is added inline in this controller or in a sibling helper file. Per `contracts/README.md §6`.
- [ ] T051 [US2] Modify `src/FundingPlatform.Infrastructure/Identity/IdentityConfiguration.cs`: add new method `SeedSentinelAdminAsync(IServiceProvider sp, IConfiguration configuration, ILogger<IdentityConfiguration> logger)` per `research.md §Decision 7` (log-before-create ordering):
  1. Resolve the `UserManager<ApplicationUser>`.
  2. Check existence: `var existing = await userManager.FindByEmailAsync("admin@FundingPlatform.com");`. If the existing user has `IsSystemSentinel = true`, return early (idempotent skip — no log, no rewrite).
  3. Resolve password: `var configured = configuration["Admin:DefaultPassword"]`. If non-null/non-empty, use it. Otherwise generate via `RandomNumberGenerator.GetBytes(24)` → `Convert.ToBase64String(...)`.
  4. **If the password was generated (not configured)**, emit a single WARN log line BEFORE step 5: `logger.LogWarning("Sentinel admin '{Email}' will be created with auto-generated password: {Password}", "admin@FundingPlatform.com", password);`.
  5. Construct the user: `var sentinel = ApplicationUser.CreateSentinel("admin@FundingPlatform.com");`.
  6. Call `await userManager.CreateAsync(sentinel, password)`. On failure, log error and rethrow (the seed has already logged the password if generated, so the operator can reuse it via `Admin:DefaultPassword` on the next deploy).
  7. Call `await userManager.AddToRoleAsync(sentinel, "Admin")`.
- [ ] T052 [US2] Modify `src/FundingPlatform.Web/Program.cs` bootstrap block (the `using (var scope = app.Services.CreateScope())` section): immediately after `SeedRolesAsync(scope.ServiceProvider)` and BEFORE the dev-only `SeedUsersAsync(scope.ServiceProvider)`, call `await IdentityConfiguration.SeedSentinelAdminAsync(scope.ServiceProvider, app.Configuration, scope.ServiceProvider.GetRequiredService<ILogger<IdentityConfiguration>>());`. The sentinel is seeded in **all** environments (Development, Staging, Production) — only the demo users are dev-only. Wrap in the same `try { ... } catch (Microsoft.Data.SqlClient.SqlException ex) { logger.LogWarning(...); }` as the existing seed steps.
- [ ] T053 [US2] Run `dotnet build --nologo`; run the application via `dotnet run --project src/FundingPlatform.AppHost` once. Capture the WARN-level log line emitted on first startup. Verify by inspecting `dbo.AspNetUsers` that the sentinel row exists with `IsSystemSentinel = 1` and the password hash matches the captured password. Stop the app, restart — verify the sentinel is NOT re-created and NO password is logged. Run `dotnet test tests/FundingPlatform.Tests.E2E --nologo` and confirm `SentinelExclusionTests` + `SentinelImmutabilityTests` are green; existing tests + `AdminUserLifecycleTests` still green.

**Checkpoint**: Sentinel exists; sentinel hidden everywhere; sentinel immutable through every in-product surface; sentinel can sign in. Self-modification and last-admin guards are NOT yet wired (US3).

---

## Phase 5: User Story 3 — Self-modification and last-admin guards (Priority: P1)

**Goal**: Wire the self-modification guard (admin cannot disable / change own role / change own email from the admin area) and the last-non-sentinel-admin guard (any action that would leave zero active non-sentinel admins is rejected). Both guards live in `UserAdministrationService`; the controller maps the exceptions to UI errors. Re-enable is explicitly NOT subject to the last-admin guard.

**Independent Test**: `SelfModificationGuardTests` and `LastAdminGuardTests` pass.

### Tests for User Story 3 (write first; expect red before implementation)

- [ ] T054 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/SelfModificationGuardTests.cs`:
  - `Admin_CannotDisableSelf_Rejected` — admin opens own row in `/Admin/Users`, attempts Disable; assert `SELF_MODIFICATION_BLOCKED` named-rule error in the UI.
  - `Admin_CannotChangeOwnRole_Rejected` — admin opens own Edit page, changes Role from `Admin` to `Reviewer`, submits; assert rejection with the `ChangeOwnRole` action in the message.
  - `Admin_CannotChangeOwnEmail_Rejected` — same pattern; change Email; rejection with `ChangeOwnEmail` action.
  - `Admin_CanChangeOwnFirstNameLastNamePhone_Allowed` — admin opens own Edit page, changes first/last/phone only, submits; success.
- [ ] T055 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/LastAdminGuardTests.cs`:
  - `LastAdmin_CannotDisableSelf` — set up the platform with exactly one non-sentinel admin (the demo `admin@demo.com`); attempt to disable them; assert `LAST_ADMIN_PROTECTED`.
  - `LastAdmin_CannotDemoteSelf` — same setup; attempt to demote them to Reviewer; assert rejection.
  - `WithTwoAdmins_DisablingFirstSucceeds` — set up two active admins; disable one; assert success and the listing shows one Active admin remaining.
  - `WithTwoAdmins_DisablingBoth_SecondRejected` — disable one (success); attempt to disable the second; assert rejection.
  - `EnableLastAdmin_Allowed_DoesNotTriggerGuard` — start with zero active non-sentinel admins (sentinel is the only path in); use sentinel to re-enable a previously-disabled admin; assert success (re-enable always increases count, guard does not fire).

### Implementation for User Story 3

- [ ] T056 [US3] Modify `src/FundingPlatform.Application/Admin/Users/Services/UserAdministrationService.cs`: add the **self-modification guard** to `UpdateUserAsync`, `DisableUserAsync`, `ResetUserPasswordAsync`. Logic:
  - Compare `actorUserId == request.UserId`. If equal:
    - In `DisableUserAsync`: throw `SelfModificationException(SelfModificationAction.DisableSelf)`.
    - In `UpdateUserAsync`: if the request changes role (compare against current role) → throw `SelfModificationException(SelfModificationAction.ChangeOwnRole)`. If the request changes email (compare against current email) → throw `SelfModificationException(SelfModificationAction.ChangeOwnEmail)`. Changes to first/last/phone/legal-id are ALLOWED on self.
    - `ResetUserPasswordAsync` on self is also blocked? Spec says self-edit of password through the standard account surface is outside the spec. So an admin attempting to reset their OWN password from the admin area is also a self-modification. Let me block: throw `SelfModificationException(SelfModificationAction.DisableSelf)` — actually use a new action `ResetOwnPassword` if the enum has it. Add the enum member if absent.
- [ ] T057 [US3] Modify `src/FundingPlatform.Application/Admin/Users/Services/UserAdministrationService.cs`: add the **last-non-sentinel-admin guard** per `research.md §Decision 9`. Helper method:
  ```text
  private async Task<int> CountActiveNonSentinelAdminsAsync(CancellationToken ct)
  {
      var adminRoleId = await _dbContext.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).SingleAsync(ct);
      var nowUtc = DateTimeOffset.UtcNow;
      return await _dbContext.UserRoles.IgnoreQueryFilters()
          .Where(ur => ur.RoleId == adminRoleId)
          .Join(_dbContext.Users.IgnoreQueryFilters(), ur => ur.UserId, u => u.Id, (ur, u) => u)
          .CountAsync(u => !u.IsSystemSentinel && (u.LockoutEnd == null || u.LockoutEnd <= nowUtc), ct);
  }
  ```
  Apply in:
  - `DisableUserAsync` AFTER the sentinel + self-mod guards: simulate `count - 1`; if would be `0`, throw `LastAdministratorException`.
  - `UpdateUserAsync` when the change demotes an Admin to a non-Admin role: simulate `count - 1`; if `0`, throw.
  - `EnableUserAsync` does NOT call the guard (re-enable always increases the count; per `data-model.md §"State transitions"` and `research.md §Decision 9`).
- [ ] T058 [US3] Modify `src/FundingPlatform.Web/Controllers/Admin/AdminUsersController.cs`: extend the existing `try/catch` block in each action to catch `SelfModificationException` and `LastAdministratorException` in addition to `SentinelUserModificationException` (added in T050). Map each exception to a `ModelState` error using the `AdminErrorMessages` dictionary keyed on `ErrorCode`. For `SelfModificationException`, build the user-friendly message based on the `Action` enum value (e.g., "Administrators cannot disable their own account.", "Administrators cannot change their own role from the admin area.", "Administrators cannot change their own email from the admin area.").
- [ ] T059 [US3] Modify `src/FundingPlatform.Web/Views/Admin/Users/Index.cshtml` to **dim or hide** self-targeting destructive actions on the actor's own row when `IsSelf == true` (per the `AdminUserSummaryRowViewModel.IsSelf` flag computed in the controller). Specifically: hide the Disable action; hide the Reset Password action; for the Edit link, allow it but the Edit form's role/email fields will be server-rejected if changed. (The UI hiding is a defense-in-depth UX layer; the service-layer guard is the contract.)
- [ ] T060 [US3] Run `dotnet build --nologo` and `dotnet test tests/FundingPlatform.Tests.E2E --nologo`. Confirm: existing tests + `AdminUserLifecycleTests` + `SentinelExclusionTests` + `SentinelImmutabilityTests` + `SelfModificationGuardTests` + `LastAdminGuardTests` all green.

**Checkpoint**: All three P1 stories are now complete. The system has user management, sentinel protection at every layer, and self-modification + last-admin lockout protection.

---

## Phase 6: User Story 4 — Admin inherits all Reviewer powers (Priority: P2)

**Goal**: Add a single `IClaimsTransformation` that adds a `Reviewer` role claim to every Admin principal at runtime. This makes every existing `[Authorize(Roles="Reviewer")]` attribute in specs 002, 004, 006, 007 also accept Admins, **without modifying any of those attributes**.

**Independent Test**: `AdminInheritsReviewerTests` passes — an Admin-only user (the new sentinel or a freshly-created Admin) can hit every Reviewer-gated route in the codebase and gets HTTP 200.

### Tests for User Story 4 (write first; expect red before implementation)

- [ ] T061 [US4] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/AdminInheritsReviewerTests.cs`:
  - `Admin_CanAccess_ReviewQueue` — log in as Admin (sentinel or any Admin), GET `/Review`; assert HTTP 200.
  - `Admin_CanAccess_ReviewDetailPage` — given an existing application id, GET `/Review/Review/{id}`; assert HTTP 200 (or normal review-page result if the application is in a reviewable state).
  - `Admin_CanAccess_SigningInbox` — GET `/Review/SigningInbox`; assert HTTP 200.
  - `Admin_CanAccess_GenerateAgreement` — GET `/Review/GenerateAgreement/{id}` for an approved application; assert HTTP 200.
  - `Admin_CanAccess_ApplicantResponseReviewerGatedAction` — POST any `[Authorize(Roles="Reviewer")]` action in `ApplicantResponseController`; assert NOT 403.
  - `NoExistingReviewerGate_WasModified` — meta-test: at the start of the suite, read the source files of `ReviewController.cs`, `ApplicantResponseController.cs`, `FundingAgreementController.cs` and assert no `[Authorize(Roles="Reviewer")]` attribute was modified to add `,Admin`. (Implemented as a small test that reads files via `File.ReadAllText` and uses regex/string-search.)

### Implementation for User Story 4

- [ ] T062 [P] [US4] Create `src/FundingPlatform.Infrastructure/Identity/AdminImpliesReviewerClaimsTransformation.cs` per `research.md §Decision 3`: implements `IClaimsTransformation`. The `TransformAsync(ClaimsPrincipal principal)` method:
  - If `!principal.IsInRole("Admin")` → return principal unchanged.
  - If `principal.IsInRole("Reviewer")` → return principal unchanged (idempotent).
  - Else: `var identity = (ClaimsIdentity)principal.Identity!; identity.AddClaim(new Claim(identity.RoleClaimType, "Reviewer")); return principal;`.
  - Returns `Task.FromResult(principal)` (no I/O).
- [ ] T063 [US4] Modify `src/FundingPlatform.Infrastructure/DependencyInjection.cs` (or `src/FundingPlatform.Web/Program.cs` if DI registrations live there): register the transformation as `services.AddScoped<IClaimsTransformation, AdminImpliesReviewerClaimsTransformation>()`. Per `research.md §Decision 3`. Add necessary `using` directives.
- [ ] T064 [US4] Run `dotnet build --nologo` and `dotnet test tests/FundingPlatform.Tests.E2E --nologo`. Confirm `AdminInheritsReviewerTests` is green plus all previous tests. Verify by greps that no `[Authorize(Roles="Reviewer")]` attribute in `Controllers/{ReviewController,ApplicantResponseController,FundingAgreementController}.cs` has been modified by this story.

**Checkpoint**: Admin transparently inherits Reviewer authorization; no existing gate code was modified.

---

## Phase 7: User Story 5 — `/Admin/Reports` stub page (Priority: P2)

**Goal**: Land the access-gated stub page at `/Admin/Reports` so the access-control contract for the future reporting module is locked in.

**Independent Test**: `AdminReportsStubTests` passes.

### Tests for User Story 5 (write first; expect red before implementation)

- [ ] T065 [P] [US5] Create `tests/FundingPlatform.Tests.E2E/PageObjects/Admin/AdminReportsPage.cs` — locator for the page-header title and the empty-state region.
- [ ] T066 [P] [US5] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/AdminReportsStubTests.cs`:
  - `Admin_GetReports_RendersEmptyStatePage` — log in as Admin, GET `/Admin/Reports`; assert HTTP 200 + the empty-state region is visible with copy along the lines of "Reports module coming soon".
  - `Reviewer_GetReports_403` — log in as Reviewer, GET `/Admin/Reports`; assert HTTP 403 (the new AccessDenied page); NOT a redirect to login.
  - `Applicant_GetReports_403` — log in as Applicant, GET `/Admin/Reports`; assert HTTP 403.
  - `Unauthenticated_GetReports_RedirectsToLogin` — without a session, GET `/Admin/Reports`; assert redirect to `/Account/Login`.

### Implementation for User Story 5

- [ ] T067 [P] [US5] Create `src/FundingPlatform.Web/Controllers/Admin/AdminReportsController.cs`: class attributes `[Authorize(Roles = "Admin")]` and `[Route("Admin/Reports")]`. Single action `[HttpGet] public IActionResult Index() => View()`. No injected dependencies.
- [ ] T068 [P] [US5] Create `src/FundingPlatform.Web/Views/Admin/Reports/Index.cshtml` per `plan.md §Project Structure`: `_PageHeader` (Title = "Reports") + a single `_EmptyState` partial with icon `ti ti-chart-line` (or appropriate icon), headline "Reports coming soon", body "The reports module is not yet available. Check back in a future release.". No data, no actions, no widgets. Set `data-testid="admin-area"` on the outer wrapper.
- [ ] T069 [US5] Run `dotnet build --nologo` and `dotnet test tests/FundingPlatform.Tests.E2E --nologo`. Confirm `AdminReportsStubTests` green plus all previous tests.

**Checkpoint**: `/Admin/Reports` access-control contract is locked in. The page is reachable, gated, and visibly placeholder.

---

## Phase 8: User Story 6 — Admin area uses Tabler shell + sidebar entries (Priority: P3)

**Goal**: Add the `Users` and `Reports` sidebar entries (Admin-gated) to the role-aware sidebar partial introduced by spec 008. Verify the admin-area views obey spec 008's invariants (no inline `style=`, no badge markup outside `_StatusPill`, no document references outside `_DocumentCard`). Add the role-aware-sidebar admin-entries E2E test.

**Independent Test**: `RoleAwareSidebarAdminEntriesTests` passes plus the grep invariants from spec 008 hold for the new admin-area views.

### Tests for User Story 6 (write first; expect red before implementation)

- [ ] T070 [US6] Create `tests/FundingPlatform.Tests.E2E/Tests/Admin/RoleAwareSidebarAdminEntriesTests.cs`:
  - `Admin_SidebarShows_UsersAndReports` — log in as Admin, visit `/`; assert the sidebar contains entries with text "Users" linking to `/Admin/Users` and "Reports" linking to `/Admin/Reports`.
  - `Reviewer_SidebarDoesNotShow_AdminEntries` — log in as Reviewer; assert sidebar does NOT contain "Users" or "Reports" entries.
  - `Applicant_SidebarDoesNotShow_AdminEntries` — log in as Applicant; same assertion.
  - `Admin_SidebarAlsoShows_ReviewerEntries` — log in as Admin; assert sidebar still contains the Reviewer entries (Review queue, Signing inbox) — verifies the role-claim transformation effect on sidebar visibility too.

### Implementation for User Story 6

- [ ] T071 [US6] Modify the role-aware sidebar partial introduced by spec 008 (the file under `Views/Shared/` named per spec 008's plan — likely `_Layout.cshtml`'s sidebar section, or a dedicated partial / view-component). Append two new `SidebarEntry` items to the canonical entry list per `data-model.md §"Sidebar entries"`:
  - `new SidebarEntry("Users", "/Admin/Users", "ti ti-users", new[] { "Admin" })`
  - `new SidebarEntry("Reports", "/Admin/Reports", "ti ti-chart-line", new[] { "Admin" })`
  Place after the existing Reviewer entries (so Admins see Home → My Applications [N/A for them] → Review queue → Signing inbox → Users → Reports). Use stable `data-testid` values consistent with spec 008's pattern (e.g., `data-testid="sidebar-entry-users"`, `data-testid="sidebar-entry-reports"`).
- [ ] T072 [US6] Verify the admin-area views (`Views/Admin/Users/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `ResetPassword.cshtml`, `Views/Admin/Reports/Index.cshtml`, `Views/Account/AccessDenied.cshtml`, `Views/Account/ChangePassword.cshtml`) obey spec 008's invariants. Run greps:
  - `rg --pcre2 'style="[^"]+"' src/FundingPlatform.Web/Views/Admin/ src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml || echo "OK: no inline style attributes"`
  - `rg 'class="badge' src/FundingPlatform.Web/Views/Admin/ src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml | rg -v _StatusPill || echo "OK: no badge markup outside _StatusPill"`
  - `rg --pcre2 '<a[^>]*href="[^"]*\\.(pdf|docx?|xlsx?)"' src/FundingPlatform.Web/Views/Admin/ src/FundingPlatform.Web/Views/Account/AccessDenied.cshtml src/FundingPlatform.Web/Views/Account/ChangePassword.cshtml || echo "OK: no document anchor tags outside _DocumentCard"`
  All three must report OK / no matches. (The third invariant is mostly trivial since the new admin views don't reference documents.)
- [ ] T073 [US6] Run `dotnet build --nologo` and `dotnet test tests/FundingPlatform.Tests.E2E --nologo`. Confirm `RoleAwareSidebarAdminEntriesTests` green plus every other test added in this spec plus every existing test from specs 001–008.

**Checkpoint**: All six user stories complete. Admin area is visually and structurally consistent with the rest of the platform.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, manual smoke per `quickstart.md`, and any cross-cutting cleanups.

- [ ] T074 [P] Run the full Playwright suite one more time end-to-end with `--EphemeralStorage=true` (forces a fresh DB per fixture run, including the sentinel re-seed) and confirm zero failures across specs 001–009. Capture the final test summary and append it to the PR description.
- [ ] T075 [P] Manual smoke per `quickstart.md`: walk every smoke section (sections 2, 3, 4, 5, 6, 7, 8, 9). Capture screenshots of the admin-area surfaces (Users list, Create form, Edit form, Reports stub) for the PR description.
- [ ] T076 [P] Confirm the dacpac diff against the previous schema shows ONLY the four new columns + the new filtered index — no accidental schema drift introduced by EF metadata generation. (`dotnet build src/FundingPlatform.Database` against the deployed dev DB; if a schema-compare tool is available, capture the diff.)
- [ ] T077 Remove any temporary workaround comments, TODO markers, or scaffolding code added during implementation. Double-check that no `[Authorize(Roles="Reviewer")]` attribute in `Controllers/{ReviewController.cs, ApplicantResponseController.cs, FundingAgreementController.cs}` was modified by this branch (per FR-002 / SC-006). `git diff main..HEAD -- src/FundingPlatform.Web/Controllers/Review*Controller.cs src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs src/FundingPlatform.Web/Controllers/FundingAgreementController.cs` should show **only** `UserManager<IdentityUser>` → `UserManager<ApplicationUser>` cascade-rename hunks, nothing else.
- [ ] T078 Run the formal `spex:review-plan` skill against `specs/009-admin-area/` — produces `REVIEW-PLAN.md` (coverage matrix, red-flag scan, NFR validation). Address any blocking findings; defer or document advisory findings.

**Checkpoint**: Branch is ready for PR. All E2E tests green. Manual smoke green. dacpac clean. No collateral changes to existing reviewer-gated controllers.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies. Establishes the green baseline.
- **Phase 2 (Foundational)**: Depends on Phase 1. **BLOCKS all user stories.** No US1–US6 work begins until Phase 2 is green.
- **Phase 3 (US1 — lifecycle)**: Depends on Phase 2.
- **Phase 4 (US2 — sentinel)**: Depends on Phase 2 + Phase 3 (US2's tests use the lifecycle UI to attempt sentinel modification through normal admin paths).
- **Phase 5 (US3 — guards)**: Depends on Phase 2 + Phase 3 + Phase 4 (the guards live in the same `UserAdministrationService` whose write methods were extended in US2 with the sentinel guard; US3 layers self-mod and last-admin guards on top).
- **Phase 6 (US4 — Reviewer inheritance)**: Depends on Phase 2 only — entirely independent of US1/US2/US3 in code. Could run in parallel with US1/US2/US3 if staffed.
- **Phase 7 (US5 — Reports stub)**: Depends on Phase 2 only — entirely independent. Could run in parallel.
- **Phase 8 (US6 — sidebar + Tabler)**: Depends on Phase 3 + Phase 5 + Phase 7 (the sidebar links resolve to the views built by those stories; the invariant grep covers all admin-area views).
- **Phase 9 (Polish)**: Depends on all stories landed.

### Within Each User Story

- Tests are written first and FAIL before implementation (TDD).
- DTOs / records / view-models before services that consume them.
- Services before controllers that consume them.
- Controllers before views that they render.
- Implementation green before moving to the next story.

### Parallel Opportunities

- All [P] tasks within Phase 2 (T004–T007 [domain], T014, T015 [P]) can run in parallel — each touches a different file.
- All [P] tasks within Phase 3 (US1 view-models T028–T033 + view files T039–T043 + PageObjects T019–T022) can run in parallel.
- US4 (Phase 6) and US5 (Phase 7) can run in parallel with US1 / US2 / US3 if multiple developers are available — they touch disjoint files.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Domain types — all [P] (different files):
Task T004: src/FundingPlatform.Domain/Entities/ApplicationUser.cs
Task T005: src/FundingPlatform.Domain/Exceptions/SentinelUserModificationException.cs
Task T006: src/FundingPlatform.Domain/Exceptions/LastAdministratorException.cs
Task T007: src/FundingPlatform.Domain/Exceptions/SelfModificationException.cs

# Followed sequentially by the dacpac + AppDbContext + Program.cs + cascade rename
# (T008 → T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 → T017 → T018)

# AccessDenied page, Identity rename, FundingAgreement rename can run in parallel:
Task T014 [P]: Views/Account/AccessDenied.cshtml
Task T015 [P]: Controllers/FundingAgreementController.cs (rename only)
```

---

## Parallel Example: Phase 3 (US1)

```bash
# All view-model / view / PageObject files are independent — run [P] tasks in parallel:
Task T019 [P]: PageObjects/Admin/AdminUsersListPage.cs
Task T020 [P]: PageObjects/Admin/AdminUserCreatePage.cs
Task T021 [P]: PageObjects/Admin/AdminUserEditPage.cs
Task T022 [P]: PageObjects/ChangePasswordPage.cs

Task T024 [P]: Application/Admin/Users/IUserAdministrationService.cs
Task T025 [P]: Application/Admin/Users/DTOs/*.cs

Task T028 [P]: ViewModels/Admin/AdminUserSummaryRowViewModel.cs
Task T029 [P]: ViewModels/Admin/AdminUsersListViewModel.cs
Task T030 [P]: ViewModels/Admin/AdminUserCreateViewModel.cs
Task T031 [P]: ViewModels/Admin/AdminUserEditViewModel.cs
Task T032 [P]: ViewModels/Admin/AdminUserResetPasswordViewModel.cs
Task T033 [P]: ViewModels/ChangePasswordViewModel.cs

# Sequential within the implementation strand (each depends on the prior):
T026 → T027 → T034 → T035 → T036 → T037 → T038

Task T039 [P]: Views/Account/ChangePassword.cshtml
Task T040 [P]: Views/Admin/Users/Index.cshtml
Task T041 [P]: Views/Admin/Users/Create.cshtml
Task T042 [P]: Views/Admin/Users/Edit.cshtml
Task T043 [P]: Views/Admin/Users/ResetPassword.cshtml

# Final integration check:
Task T044: Build + run E2E suite — confirm green
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T018)
3. Complete Phase 3: User Story 1 (T019–T044)
4. **STOP and VALIDATE**: User Story 1 runs end-to-end without sentinel, without the guards. The MVP delivers ops-can-now-manage-users on its own.
5. Decision point: ship the MVP or continue to US2/US3 (the recommended path — the P1 stories were designed to ship together).

### Incremental Delivery

1. Phase 1 + Phase 2 → foundation green.
2. + US1 (T019–T044) → MVP demoable. Ship-or-continue decision.
3. + US2 (T045–T053) → sentinel guarantee delivered.
4. + US3 (T054–T060) → admin-area is bullet-proof against accidental lockout.
5. + US4 (T061–T064) → Reviewer-inheritance contract locked in.
6. + US5 (T065–T069) → reports access-gated.
7. + US6 (T070–T073) → sidebar + visual consistency.
8. + Phase 9 polish (T074–T078) → branch ready for PR.

### Parallel Team Strategy

With multiple developers, after Phase 2 completes:
- Developer A: US1 (lifecycle).
- Developer B: US4 (claims transformation) — entirely independent.
- Developer C: US5 (reports stub) — entirely independent.
- After US1 completes: Developer A picks up US2 (sentinel) → US3 (guards). US6 (sidebar + invariants) waits for US1 + US5 since it touches both surfaces.

---

## Notes

- [P] tasks = different files, no dependencies. Tasks within the same file are sequential.
- Tests are written before implementation. The test for a story must FAIL before that story's implementation lands; the implementation's task expects the test to GREEN once complete.
- Commit after each task or logical group of tasks (per project convention; see recent commit log on the `008-tabler-ui-migration` branch for style).
- The `IdentityUser → ApplicationUser` cascade rename is foundational and intentionally bundled into Phase 2 — splitting it across stories would leave the codebase in a half-renamed state and break the build.
- The sentinel password is logged exactly once on first deploy when no `Admin:DefaultPassword` is configured. Capture the line during the first `dotnet run` after this branch lands; the password cannot be recovered later without an out-of-product procedure (see `quickstart.md §"Recovering from a missed sentinel password"`).
- Do NOT modify any `[Authorize(Roles="Reviewer")]` attribute in `Controllers/{ReviewController,ApplicantResponseController,FundingAgreementController}.cs`. The Reviewer-inheritance is delivered by `AdminImpliesReviewerClaimsTransformation` (T062). T077 polish task verifies this by `git diff`.
- Do NOT modify the PDF-target view files (`Views/FundingAgreement/Document.cshtml`, `_FundingAgreementLayout.cshtml`); spec 008's invariants apply.
