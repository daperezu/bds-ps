# Implementation Plan: Admin Role and Admin Area

**Branch**: `009-admin-area` | **Date**: 2026-04-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/009-admin-area/spec.md`

## Summary

Two-layer feature: a service/data-layer foundation plus a Razor admin surface.

**Foundation.** Replace the default `IdentityUser` with a custom `ApplicationUser : IdentityUser` carrying four new columns — `FirstName`, `LastName`, `IsSystemSentinel`, `MustChangePassword` — added to `dbo.AspNetUsers` via the dacpac project. Apply an EF Core global query filter on `ApplicationUser` (`!u.IsSystemSentinel`) so the sentinel disappears from every default user enumeration in the codebase, including the existing demo seeds. A custom `SentinelAwareUserStore<ApplicationUser>` overrides `FindByEmailAsync` / `FindByNameAsync` / `FindByIdAsync` with `IgnoreQueryFilters()` so the sign-in flow can locate the sentinel and so service-layer guards can fetch the sentinel to detect-and-reject. Two domain exceptions (`SentinelUserModificationException`, `LastAdministratorException`) plus a `SelfModificationException` carry the named-rule errors. A `UserAdministrationService` in the Application layer owns every write operation (create / update / disable / enable / reset-password) and applies the sentinel guard, the self-modification guard, and the last-non-sentinel-admin guard before any state change. A new `SentinelSeeder` in `IdentityConfiguration` seeds `admin@FundingPlatform.com` on first deploy: it logs the password at WARN level **before** persisting the user (so a crash between log-flush and commit cannot strand the password), and is fully idempotent on subsequent boots. Reviewer-implies-Admin authorization is solved with a single `AdminImpliesReviewerClaimsTransformation` (an `IClaimsTransformation`) that adds a `Reviewer` role claim to any principal whose persisted role is `Admin` — no existing `[Authorize(Roles="Reviewer")]` attribute in specs 002, 004, 006, 007 is modified.

**Surface.** Two new attribute-routed controllers under `Controllers/Admin/`: `AdminUsersController` at `/Admin/Users` (Index / Create / Edit / Disable / Enable / ResetPassword) and `AdminReportsController` at `/Admin/Reports` (stub Index). Both gated by `[Authorize(Roles="Admin")]`. Six view-models, six new views (Users Index, Create, Edit; Reports Index; Account ChangePassword; Account AccessDenied), all consuming the spec-008 partials (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`, `_ConfirmDialog`). The role-aware sidebar partial introduced by spec 008 is extended with two new entries (Users, Reports) gated by the `Admin` role. A small `MustChangePasswordMiddleware` redirects authenticated users with the flag set to `/Account/ChangePassword` on every request except the change-password and logout endpoints. The cookie configuration in `Program.cs` is adjusted in three places: `AccessDeniedPath` from `/Account/Login` to `/Account/AccessDenied` (so SC-007 returns observable 403 for authenticated-but-unauthorized requests), `SecurityStampValidationInterval` from default 30 minutes to 1 minute (so admin-driven session invalidation takes effect within a bounded window), and the `SignInOptions` left at their current defaults.

Total production-code footprint: 1 new domain entity (`ApplicationUser`), 3 new domain exceptions, 1 modified `AppDbContext` (generic parameter swap + global query filter), 1 modified Identity registration in `Program.cs` (incl. cookie/security-stamp tweaks + `IClaimsTransformation` registration + custom user-store wiring), 1 new `SentinelAwareUserStore`, 1 new `AdminImpliesReviewerClaimsTransformation`, 1 modified `IdentityConfiguration` (sentinel seed step), 1 new `UserAdministrationService` (Application layer), 6 new commands/queries, 2 new controllers, 6 new view-models, 6 new views, 1 new middleware, 1 modified sidebar partial, 1 modified `dbo.AspNetUsers.sql`, plus full Playwright E2E coverage (one test class per P1 user story plus inheritance and reports stub). Three existing files are touched only for the IdentityUser → ApplicationUser cascade rename: `AccountController.cs`, `FundingAgreementController.cs`, and `IdentityConfiguration.cs`. **No new NuGet packages, no new managed dependencies.**

## Technical Context

**Language/Version**: C# / .NET 10.0 (matches all prior specs).
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **No new dependencies introduced by this feature.** The Tabler.io static-asset bundle vendored by spec 008 is reused as-is.
**Storage**: SQL Server (Aspire-managed for dev, dacpac schema management). **Schema change**: four new columns on `dbo.AspNetUsers` (`FirstName`, `LastName`, `IsSystemSentinel`, `MustChangePassword`) plus one filtered index on `IsSystemSentinel = 1` for fast sentinel lookup. No new tables. No new managed storage subsystems.
**Testing**: Playwright for .NET (NUnit) for E2E. New test classes: `AdminUserLifecycleTests.cs` (US1), `SentinelExclusionTests.cs` + `SentinelImmutabilityTests.cs` (US2), `LastAdminGuardTests.cs` + `SelfModificationGuardTests.cs` (US3), `AdminInheritsReviewerTests.cs` (US4), `AdminReportsStubTests.cs` (US5), `RoleAwareSidebarAdminEntriesTests.cs` (US6 — extends spec-008's role-aware sidebar test). Existing tests for specs 001–008 must remain green; the only behavioral change touching them is the IdentityUser → ApplicationUser cascade, which is type-only and observable only through reflection.
**Target Platform**: Linux/Windows server (Aspire-orchestrated). UI verified against Chromium and Firefox at 1280×800 viewport (admin area is desktop-first; mobile is acceptable-but-not-prioritized per Tabler defaults).
**Project Type**: Web application (server-side ASP.NET MVC; no SPA framework introduced).
**Performance Goals**: No new high-traffic paths. The user-listing query at `/Admin/Users` paginates server-side using the same `Skip/Take + OrderBy` pattern as the review-queue listing (spec 002). Setting `SecurityStampValidationInterval = TimeSpan.FromMinutes(1)` means each authenticated request within a minute of the last validation skips DB validation; admin-driven invalidation propagates within ≤60 seconds for any user who is currently signed in. The IClaimsTransformation runs once per request after authentication; it does no I/O and adds < 1µs per request.
**Constraints**: FR-002 forbids modifying any existing `[Authorize(Roles="Reviewer")]` attribute in specs 002, 004, 006, 007 — solved by the IClaimsTransformation, not by attribute edits. FR-027 mandates layered exclusion: query-layer (global filter) AND service-layer (write-side guard). FR-028 mandates dacpac for the new columns; EF migrations are prohibited. SC-008 mandates the sentinel password is logged exactly once on first startup at WARN level — implemented by emitting the log line before `userManager.CreateAsync(...)` and skipping the entire seed branch if the sentinel already exists.
**Scale/Scope**: Realistic v1 scale: tens of admins, low hundreds of reviewers, low thousands of applicants. Admin-area write-rate is human-driven and infrequent (handful per day at most). The list query covers all non-sentinel users; pagination default is 20 rows, consistent with spec 002.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | New domain entity (`ApplicationUser`) and three exceptions live in `FundingPlatform.Domain.Entities` / `Domain.Exceptions`. The `UserAdministrationService` is in `FundingPlatform.Application/Admin/Users/` with the existing `Admin/Commands` and `Admin/Queries` siblings. Identity-store override (`SentinelAwareUserStore`), the claims transformation, and the sentinel seeder live in `FundingPlatform.Infrastructure/Identity/` — Infrastructure depends on Application + Domain (existing direction). Web-layer controllers, view-models, views, and middleware are confined to `FundingPlatform.Web/`. Dependency direction stays inward; no Web/Infrastructure references leak into Domain or Application. |
| II. Rich Domain Model | PASS | Sentinel-immutability, self-modification, and last-non-sentinel-admin guards are enforced **inside `UserAdministrationService`** (Application layer) by reading `ApplicationUser.IsSystemSentinel` and active-admin-count from the data layer and throwing domain exceptions. The exceptions themselves live in Domain. The `ApplicationUser` entity exposes its administrative attributes (`IsSystemSentinel`, `MustChangePassword`, `FirstName`, `LastName`) as get-only properties initialized via constructor or factory; mutation goes through service methods that enforce the guards, not via raw property setters. (Note: full Rich-Domain-Model encapsulation is awkward when extending an Identity framework entity — `IdentityUser` exposes settable properties by convention. The pragmatic boundary is: invariants live in services that wrap UserManager.) |
| III. E2E Testing (NON-NEGOTIABLE) | PASS | One Playwright E2E test class per P1 user story (US1, US2, US3) plus inheritance (US4) plus reports stub (US5) plus sidebar visibility (US6). Each test class is independently runnable per the constitution. PageObject pattern: new `AdminUsersListPage`, `AdminUserCreatePage`, `AdminUserEditPage`, `AdminReportsPage`, `AccessDeniedPage`, `ChangePasswordPage`. Sentinel-immutability tests construct direct POSTs to confirm service-layer rejection (not just UI-layer hiding). The last-admin guard test orchestrates a multi-admin → single-admin → guard-fires scenario in one test method. |
| IV. Schema-First Database Management | PASS | All four columns added to `dbo.AspNetUsers.sql` in the dacpac project. New filtered index `IX_AspNetUsers_Sentinel` on `IsSystemSentinel = 1`. **No EF migrations.** The C# `ApplicationUser` entity declares the matching properties; EF Core maps them by convention (column-name-equals-property-name). |
| V. Specification-Driven Development | PASS | Spec is SOUND (see `REVIEW-SPEC.md`). Plan flows from spec; `research.md` resolves nine planning decisions; `data-model.md` documents `ApplicationUser` shape, the new columns, the Applicant-relationship preservation rule, and the `MustChangePassword` flow; `contracts/README.md` enumerates controller routes/actions, `IUserAdministrationService` signatures, and the request/response envelopes; `quickstart.md` walks the manual smoke procedure plus sentinel-bootstrap capture. Tasks generated next by `/speckit-tasks`. |
| VI. Simplicity and Progressive Complexity | PASS | YAGNI honored throughout: no audit-log persistence (deferred), no email/SMTP (deferred), no multi-role (single-role only), no hard delete (disable-only), no optimistic concurrency (last-write-wins is documented), no per-role profile entities (shared columns on `ApplicationUser`). Two interface-based abstractions are introduced (`IUserAdministrationService`, `IClaimsTransformation`) — both serve current needs, not speculative ones. The custom `SentinelAwareUserStore` is the most novel piece; it is justified in `research.md` as the minimum-friction way to satisfy FR-019 + FR-021 simultaneously (sentinel hidden by default, but the sign-in path must still find it). Complexity Tracking table is empty. |

**Gate result: PASS — proceed to Phase 0.**

**Post-design re-check (after Phase 1 — `research.md`, `data-model.md`, `contracts/README.md`, `quickstart.md` all generated):** Still PASS. The Phase 1 artifacts surface no new types beyond those already enumerated above. The `IUserAdministrationService` contract is a single interface with seven methods (one per command/query); each maps 1:1 to a controller action. No additional Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/009-admin-area/
├── spec.md                     # Stakeholder-facing specification (SOUND)
├── plan.md                     # This file (/speckit-plan command output)
├── research.md                 # Phase 0 output — nine planning decisions resolved
├── data-model.md               # Phase 1 output — ApplicationUser shape, new columns, Applicant-relationship preservation, MustChangePassword flow
├── quickstart.md               # Phase 1 output — manual smoke procedure for golden paths + sentinel-bootstrap capture
├── contracts/
│   └── README.md               # Phase 1 output — controller routes/actions, IUserAdministrationService interface, request/response envelopes, sidebar entry table, error-code map
├── checklists/
│   └── requirements.md         # Spec quality checklist (all green)
├── review_brief.md             # Reviewer-facing guide
├── REVIEW-SPEC.md              # Formal spec soundness review (iteration 1 SOUND)
├── REVIEW-PLAN.md              # Formal plan review (generated by spex:review-plan after this command)
└── tasks.md                    # Generated by /speckit-tasks (NOT created by this command)
```

### Source Code (changes to existing structure)

```text
src/
├── FundingPlatform.Domain/
│   ├── Entities/
│   │   └── ApplicationUser.cs                                # NEW: extends IdentityUser. Properties: FirstName, LastName, IsSystemSentinel (init-only), MustChangePassword. Constructor for normal users; static `CreateSentinel(string email)` factory for the seeder so external code cannot accidentally set IsSystemSentinel=true.
│   └── Exceptions/
│       ├── SentinelUserModificationException.cs              # NEW: thrown by UserAdministrationService when any write targets the sentinel.
│       ├── LastAdministratorException.cs                     # NEW: thrown when an action would leave zero active non-sentinel admins.
│       └── SelfModificationException.cs                      # NEW: thrown when an admin tries to disable / change own role / change own email from the admin area.
│
├── FundingPlatform.Application/
│   └── Admin/                                                # EXISTING directory (templates/configuration commands+queries already there)
│       └── Users/                                            # NEW directory
│           ├── IUserAdministrationService.cs                 # NEW: contract — ListUsersAsync, GetUserAsync, CreateUserAsync, UpdateUserAsync, DisableUserAsync, EnableUserAsync, ResetUserPasswordAsync. Each method takes a request DTO and returns a result DTO + error list.
│           ├── DTOs/
│           │   ├── ListUsersRequest.cs                       # NEW: filter (role, status), search (name/email), page, pageSize.
│           │   ├── ListUsersResult.cs                        # NEW: page of UserSummaryDto + total.
│           │   ├── UserSummaryDto.cs                         # NEW: Id, FullName, Email, Role, Status, CreatedAt.
│           │   ├── UserDetailDto.cs                          # NEW: full editable shape.
│           │   ├── CreateUserRequest.cs                      # NEW: FirstName, LastName, Email, Phone?, Role, InitialPassword, LegalId? (required when Role=Applicant).
│           │   ├── UpdateUserRequest.cs                      # NEW: same shape minus password, plus rowId.
│           │   └── ResetPasswordRequest.cs                   # NEW: rowId + new temp password.
│           └── Services/
│               └── UserAdministrationService.cs              # NEW: implements IUserAdministrationService. Owns every write; applies sentinel guard (read user with IgnoreQueryFilters, check IsSystemSentinel), self-modification guard (compare actor.Id == target.Id), last-admin guard (count active non-sentinel admins post-action). Uses UserManager<ApplicationUser>, RoleManager<IdentityRole>, AppDbContext.
│
├── FundingPlatform.Infrastructure/
│   ├── Identity/
│   │   ├── IdentityConfiguration.cs                          # MODIFY: add `SeedSentinelAdminAsync(IServiceProvider, IConfiguration, ILogger)` step. Resolves password via `Admin:DefaultPassword` config key; otherwise generates via `RandomNumberGenerator` (32 bytes → base64). Logs WARN line BEFORE `userManager.CreateAsync(...)`. Idempotent: skips if a user with the sentinel email AND the IsSystemSentinel flag exists. Existing demo seed (admin@demo.com etc.) untouched.
│   │   ├── SentinelAwareUserStore.cs                         # NEW: subclasses `UserStore<ApplicationUser, IdentityRole, AppDbContext>`. Overrides FindByEmailAsync, FindByNameAsync, FindByIdAsync to call the underlying queries with `IgnoreQueryFilters()`. Used so the sign-in flow and the service-layer guards can locate the sentinel; the global filter still hides the sentinel from listing queries.
│   │   └── AdminImpliesReviewerClaimsTransformation.cs       # NEW: implements `IClaimsTransformation`. If principal.IsInRole("Admin") and not principal.IsInRole("Reviewer"), adds a `Reviewer` role claim to the identity. Idempotent (additive only).
│   ├── Persistence/
│   │   └── AppDbContext.cs                                   # MODIFY: change to `IdentityDbContext<ApplicationUser>`. In `OnModelCreating`, add `builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsSystemSentinel);` after `base.OnModelCreating(builder)`.
│   └── DependencyInjection.cs                                # MODIFY: register `IUserAdministrationService` → `UserAdministrationService` (Application implementation lives in Application; this file is the wire-up). Register the IClaimsTransformation: `services.AddScoped<IClaimsTransformation, AdminImpliesReviewerClaimsTransformation>()`.
│
├── FundingPlatform.Database/
│   ├── Tables/
│   │   └── dbo.AspNetUsers.sql                               # MODIFY: add columns FirstName NVARCHAR(100) NULL, LastName NVARCHAR(100) NULL, IsSystemSentinel BIT NOT NULL DEFAULT 0, MustChangePassword BIT NOT NULL DEFAULT 0. Add filtered index `IX_AspNetUsers_Sentinel` ON ([IsSystemSentinel]) WHERE [IsSystemSentinel] = 1. Existing columns and indexes preserved.
│   └── PostDeployment/
│       └── SeedData.sql                                      # KEEP AS-IS: roles already seeded here. Sentinel admin is seeded by C# (IdentityConfiguration.SeedSentinelAdminAsync) so the password generation can use .NET's crypto APIs and the WARN-log emission can occur in the application-log channel.
│
└── FundingPlatform.Web/
    ├── Program.cs                                            # MODIFY: (1) swap `AddIdentity<IdentityUser, IdentityRole>` → `AddIdentity<ApplicationUser, IdentityRole>`. (2) Add `.AddUserStore<SentinelAwareUserStore>()` to the Identity builder. (3) `ConfigureApplicationCookie`: change AccessDeniedPath from "/Account/Login" → "/Account/AccessDenied". (4) Add `services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));`. (5) Register `IClaimsTransformation` (done in Infrastructure DI). (6) Insert `app.UseMiddleware<MustChangePasswordMiddleware>()` between `app.UseAuthorization()` and the route mapping. (7) Call `IdentityConfiguration.SeedSentinelAdminAsync(scope.ServiceProvider, configuration, logger)` in the bootstrap block, BEFORE the existing dev-only `SeedUsersAsync(...)`.
    ├── Controllers/
    │   ├── Admin/                                            # NEW directory (alongside existing AdminController.cs which stays as-is at the controller-root)
    │   │   ├── AdminUsersController.cs                       # NEW: `[Route("Admin/Users")]`, `[Authorize(Roles="Admin")]`. Actions: Index (GET, paginated list), Create (GET form, POST submit), Edit (GET form, POST submit), Disable (POST), Enable (POST), ResetPassword (GET form, POST submit). Each action consults `IUserAdministrationService` and surfaces domain-exception messages via `ModelState`.
    │   │   └── AdminReportsController.cs                     # NEW: `[Route("Admin/Reports")]`, `[Authorize(Roles="Admin")]`. Single action: Index (GET) renders the stub page.
    │   ├── AccountController.cs                              # MODIFY: cascade rename `UserManager<IdentityUser>` → `UserManager<ApplicationUser>` and `SignInManager<IdentityUser>` → `SignInManager<ApplicationUser>`. Add new actions: AccessDenied (GET, returns 403 + view), ChangePassword (GET, POST). Modify Login post-action: after successful sign-in, check `user.MustChangePassword` and redirect to `/Account/ChangePassword` if true.
    │   └── FundingAgreementController.cs                     # MODIFY: cascade rename `UserManager<IdentityUser>` → `UserManager<ApplicationUser>`. No behavioral change.
    ├── Middleware/
    │   └── MustChangePasswordMiddleware.cs                   # NEW: if request is authenticated AND user.MustChangePassword is true AND request path is not `/Account/ChangePassword` or `/Account/Logout` or static asset, short-circuit with redirect to `/Account/ChangePassword`. Lookup uses `UserManager.FindByIdAsync(...)` (which goes through SentinelAwareUserStore but the sentinel never has MustChangePassword set, so this is benign).
    ├── ViewModels/
    │   └── Admin/                                            # NEW directory
    │       ├── AdminUsersListViewModel.cs                    # NEW: page of users + filter/search/pagination state.
    │       ├── AdminUserCreateViewModel.cs                   # NEW: form fields with [Required] / [EmailAddress] / [DataType] annotations.
    │       ├── AdminUserEditViewModel.cs                     # NEW: same plus rowId.
    │       ├── AdminUserResetPasswordViewModel.cs            # NEW: rowId + temp-password fields.
    │       ├── AdminUserSummaryRowViewModel.cs               # NEW: per-row data shape for the listing.
    │       └── AccessDeniedViewModel.cs                      # NEW: optional `RequestedPath` for the 403 page.
    ├── Views/
    │   ├── Admin/
    │   │   ├── Users/                                        # NEW directory (alongside existing Admin/Configuration.cshtml etc.)
    │   │   │   ├── Index.cshtml                              # NEW: _PageHeader + filter form + _DataTable + _StatusPill (for Active/Disabled and Role) + _EmptyState. Each row has dropdown of actions (Edit, Disable/Enable, Reset Password) gated by domain rules.
    │   │   │   ├── Create.cshtml                             # NEW: _PageHeader + _FormSection + _ActionBar. Role dropdown drives conditional reveal of LegalId field.
    │   │   │   └── Edit.cshtml                               # NEW: same shape as Create plus current state. LegalId visible only when role=Applicant.
    │   │   └── Reports/
    │   │       └── Index.cshtml                              # NEW: _PageHeader + _EmptyState ("Reports module coming soon").
    │   ├── Account/
    │   │   ├── ChangePassword.cshtml                         # NEW: _PageHeader + _FormSection ("Old password", "New password", "Confirm new password") + _ActionBar.
    │   │   └── AccessDenied.cshtml                           # NEW: _PageHeader + _EmptyState (403 message). Status code set on response by controller.
    │   └── Shared/
    │       └── _RoleAwareSidebar.cshtml (or whatever it is named today after spec 008)  # MODIFY: append two entries — Users (`/Admin/Users`) and Reports (`/Admin/Reports`) — gated by `User.IsInRole("Admin")`. The existing Reviewer-/Applicant-only entry logic is untouched; the role-aware claims-transformation makes Admins also pass any Reviewer-gated entry, which is desired.
    └── ...

tests/
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   └── Admin/                                            # NEW directory
    │       ├── AdminUsersListPage.cs                         # NEW
    │       ├── AdminUserCreatePage.cs                        # NEW
    │       ├── AdminUserEditPage.cs                          # NEW
    │       ├── AdminReportsPage.cs                           # NEW
    │       ├── AccessDeniedPage.cs                           # NEW (used to assert 403 behavior)
    │       └── ChangePasswordPage.cs                         # NEW
    └── Tests/
        └── Admin/                                            # NEW directory
            ├── AdminUserLifecycleTests.cs                    # NEW (US1) — create / edit / reset / disable / enable golden path + force-change-on-next-login + session-invalidation observable.
            ├── SentinelExclusionTests.cs                     # NEW (US2 query layer) — listing + search + filters never surface sentinel.
            ├── SentinelImmutabilityTests.cs                  # NEW (US2 service layer) — constructed POSTs to Edit/Disable/ResetPassword targeting sentinel id all return error; sentinel state unchanged.
            ├── LastAdminGuardTests.cs                        # NEW (US3) — disable / demote last non-sentinel admin rejected; with second admin present, succeeds.
            ├── SelfModificationGuardTests.cs                 # NEW (US3) — admin cannot self-disable / self-demote / self-email-change from admin area.
            ├── AdminInheritsReviewerTests.cs                 # NEW (US4) — admin-only user reaches every route that Reviewer reaches in specs 002, 004, 006, 007. One test method per controller.
            ├── AdminReportsStubTests.cs                      # NEW (US5) — admin reaches /Admin/Reports; reviewer / applicant get 403; unauthenticated redirected to login.
            └── RoleAwareSidebarAdminEntriesTests.cs          # NEW (US6) — admin sees Users + Reports entries; reviewer / applicant don't.
```

**Structure Decision**: Web-application layout (same as all prior specs). The `Controllers/Admin/` subfolder is a namespace-only convention — controllers route via attribute routing rather than via the ASP.NET MVC Areas mechanism. This avoids a folder restructure of the existing `AdminController.cs` (templates/configuration), `Views/Admin/Configuration.cshtml` etc., while still putting all `/Admin/Users/*` and `/Admin/Reports/*` requests behind `[Authorize(Roles="Admin")]`. The existing `AdminController.cs` continues to serve `/Admin/Configuration`, `/Admin/ImpactTemplates`, `/Admin/CreateTemplate`, `/Admin/EditTemplate`, `/Admin/Index` via convention routing; the new attribute-routed controllers fill in `/Admin/Users` and `/Admin/Reports`. `Views/Admin/` already exists; we add `Users/` and `Reports/` subfolders for the new views.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified.**

No violations. Nothing to track.
