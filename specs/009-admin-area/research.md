# Research: Admin Role and Admin Area

This document resolves the planning-phase decisions called out in the spec's Open Questions, the REVIEW-SPEC.md guard-rails, and any additional unknowns discovered while drafting `plan.md`.

---

## Decision 1: User-entity shape

**Decision**: Introduce `ApplicationUser : IdentityUser` carrying four new properties — `FirstName`, `LastName`, `IsSystemSentinel`, `MustChangePassword`. Cascade rename `UserManager<IdentityUser>` / `SignInManager<IdentityUser>` to `UserManager<ApplicationUser>` / `SignInManager<ApplicationUser>` across `AccountController.cs`, `FundingAgreementController.cs`, `IdentityConfiguration.cs`. Change `AppDbContext : IdentityDbContext` → `AppDbContext : IdentityDbContext<ApplicationUser>`. Update `Program.cs` to `AddIdentity<ApplicationUser, IdentityRole>`.

**Rationale**: This is the conventional ASP.NET Identity pattern for adding profile attributes — it puts the new fields directly on the user row, avoids a 1:1 join table, and lets EF Core map the columns by convention. Phone is already on `IdentityUser.PhoneNumber` so it does not need a new property.

**Alternatives considered**:
- *Separate `UserProfile` entity with 1:1 to `IdentityUser`*. Cleaner separation of concerns but adds a join on every user lookup and an extra entity that has no purpose besides holding two name fields. YAGNI — the project has no other reason to introduce a separate profile entity.
- *Claims (no schema change at all)*. Would require seeding claims on every user instead of columns, breaks JSON-style listing, and `IsSystemSentinel` as a claim is a weak invariant.

---

## Decision 2: Sentinel exclusion mechanism

**Decision**: Two-layer enforcement.
- **Query layer**: EF Core global query filter on `ApplicationUser` — `modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsSystemSentinel)`. Applied automatically to every `_dbContext.Users` and `_userManager.Users` query.
- **Service layer**: every write method on `UserAdministrationService` first fetches the target user via `_dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == request.UserId)` and rejects with `SentinelUserModificationException` if `IsSystemSentinel` is true. Only after the guard passes does the service hand off to `UserManager`.
- **Sign-in path bypass**: a custom `SentinelAwareUserStore : UserStore<ApplicationUser, IdentityRole, AppDbContext>` overrides `FindByEmailAsync`, `FindByNameAsync`, `FindByIdAsync` to invoke the underlying queries with `IgnoreQueryFilters()`. Wired via `services.AddIdentity<ApplicationUser, IdentityRole>(...).AddUserStore<SentinelAwareUserStore>()`. This lets the cookie sign-in flow locate the sentinel during login, and lets the service-layer guard fetch the sentinel during a "this is sentinel — reject" check.

**Rationale**: The spec explicitly mandates layered enforcement (FR-027). The global filter catches every default enumeration in the codebase — admin listing, future reporting, any developer-written ad-hoc query — without each call site having to remember the exclusion. The service-layer guard catches the case where a write is attempted via a constructed URL or direct service call, regardless of how the target user was located. The custom user-store covers the two narrow cases where the system **must** locate the sentinel (sign-in and guard-fetch). Keeping the sentinel-find behavior inside the user store (rather than scattering `IgnoreQueryFilters()` calls throughout services) keeps the bypass small and auditable.

**Alternatives considered**:
- *No global filter; explicit `.Where(u => !u.IsSystemSentinel)` at every call site*. Brittle — a future feature can forget the exclusion. Spec FR-027 explicitly mandates query-layer enforcement.
- *Encrypt or rename the sentinel email so it's hard to discover*. Security-by-obscurity, doesn't satisfy the "hidden from queries" requirement, and breaks the standard sign-in flow.

**Consequence**: Any future feature that legitimately needs to enumerate "all users including sentinel" must explicitly call `_dbContext.Users.IgnoreQueryFilters()`. This is a known forward-looking constraint, captured as an open thread in the brainstorm overview.

---

## Decision 3: Reviewer-implies-Admin authorization mechanism

**Decision**: Implement `AdminImpliesReviewerClaimsTransformation : IClaimsTransformation` (registered in DI as `Scoped`). On every request after authentication, if `principal.IsInRole("Admin")` and not `principal.IsInRole("Reviewer")`, append a `Reviewer` role claim to the principal's primary `ClaimsIdentity`. Idempotent (additive only; no removal). The transformation does no I/O; it operates purely on the in-memory principal.

**Rationale**: The spec mandates that Admin inherit Reviewer **without modifying any existing `[Authorize(Roles="Reviewer")]` attribute** in specs 002, 004, 006, 007 (FR-002). Identity's default authorization handler does an exact-string membership check against the principal's role claims — there is no built-in role-hierarchy mechanism. `IClaimsTransformation` is the lowest-friction hook ASP.NET Core provides: it runs once per request, sees the persisted role from the cookie's claims, and transforms the in-memory principal. From that point on, every `[Authorize(Roles="...")]` in the codebase sees an Admin user as also being a Reviewer.

**Alternatives considered**:
- *Add the `Reviewer` role to every Admin user in the AspNetUserRoles table (i.e., dual-role assignment)*. Contradicts FR-001 (single role per user) at the data layer. Spec is explicit: one role per user.
- *Replace every `[Authorize(Roles="Reviewer")]` with a custom policy `RequireReviewerOrAdmin`*. Violates FR-002 (must not modify existing gates). Also misses any future `Roles="Reviewer"` someone adds.
- *Custom `IAuthorizationPolicyProvider`*. Heavy; rewrites the policy resolution pipeline globally. Disproportionate for the simple case here.

**Trade-off**: The transformation runs on every authenticated request. Cost is sub-microsecond (string comparison + claim append), so total request-latency impact is invisible. Admin users now appear in `User.IsInRole("Reviewer")` checks throughout the codebase; existing code that *distinguishes* Reviewer from Admin (e.g., the role-aware sidebar's "Reviewer-only" entries) must use `IsInRole("Admin")` first to disambiguate. Spec 008's sidebar partial already follows this order; verified in plan.md.

---

## Decision 4: Session invalidation mechanism

**Decision**: Use ASP.NET Identity's built-in security-stamp mechanism. After every admin-driven action that should invalidate sessions (disable, role change, email change, password reset), the service calls `await _userManager.UpdateSecurityStampAsync(user)`. Set `services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1))` in `Program.cs` so the cookie middleware re-checks the security stamp at most one minute behind. Disable additionally sets `await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)` so the user cannot sign in again until enable.

**Rationale**: Identity's security-stamp validation is the framework-canonical way to invalidate authenticated sessions. The default validation interval is 30 minutes — too long for "immediately" per the spec. Setting the interval to zero would force a DB hit on every authenticated request (perf cost on hot paths). One minute strikes the balance: at most a 60-second window where a just-disabled or just-role-changed user can still see authenticated content. Spec wording ("immediately") is interpretive; a one-minute upper bound is consistent with "immediately" in the operational sense (no admin will be surprised by a 60-second propagation delay).

**Alternatives considered**:
- *Per-request DB security-stamp validation (`ValidationInterval = TimeSpan.Zero`)*. Truly instantaneous but adds a DB round-trip per request. Disproportionate for a low-rate admin action.
- *Custom session-invalidation table maintained alongside Identity*. Reinvents what `SecurityStamp` already does.
- *Sign all affected users out via cookie eviction*. ASP.NET cookie auth is server-stateless; there's no eviction primitive.

**Documented trade-off**: 60-second worst-case delay. Acceptable for v1; can tighten to 30s or 10s by lowering the interval if usage shows it matters.

---

## Decision 5: Force-change-password mechanism

**Decision**: Add `MustChangePassword BIT NOT NULL DEFAULT 0` column to `AspNetUsers` (dacpac). Map as `bool MustChangePassword` on `ApplicationUser`. Add a `MustChangePasswordMiddleware` registered between `app.UseAuthorization()` and the route mapping — if request is authenticated, user has `MustChangePassword = true`, and the request path is **not** `/Account/ChangePassword` or `/Account/Logout` and not a static asset, redirect to `/Account/ChangePassword`. The change-password POST handler clears the flag (`user.MustChangePassword = false; await _userManager.UpdateAsync(user);`) and rotates the security stamp. Setting the flag happens in two places: (a) `UserAdministrationService.CreateUserAsync` after creating the user; (b) `UserAdministrationService.ResetUserPasswordAsync` after applying the new password.

**Rationale**: ASP.NET Identity has no built-in "must change password" flag. A column + a small middleware is the simplest mechanism that generalizes (works for any future flow that wants to force a password change — e.g., periodic rotation). Middleware-based redirection avoids spreading the check across every controller.

**Alternatives considered**:
- *Use `LockoutEnd` plus a sentinel "1900-01-01" date as a "must reset" signal*. Hack; conflates two orthogonal states and breaks the disable mechanism.
- *Use a claim on the cookie*. Would need to be set when user signs in and read in middleware — works but duplicates state across DB and cookie. Column is canonical.

---

## Decision 6: `ADMIN_DEFAULT_PASSWORD` configuration key shape

**Decision**: Use the standard ASP.NET Core configuration key `Admin:DefaultPassword`. This binds to the env variable `Admin__DefaultPassword` (double-underscore convention), to user-secrets via `dotnet user-secrets set "Admin:DefaultPassword" "..."`, and to Aspire via `WithEnvironment("Admin__DefaultPassword", "...")`. The seeder reads via `IConfiguration["Admin:DefaultPassword"]`.

**Rationale**: Conventional shape. Avoids using a top-level env var (which contaminates the global namespace) and works identically across all configuration providers ASP.NET Core supports. The `Admin:` prefix leaves room for future keys like `Admin:DefaultEmail` (if ever overridable — unlikely, since the sentinel email is intentionally hard-coded per FR-017).

**Alternatives considered**:
- *Top-level `ADMIN_DEFAULT_PASSWORD` env var only*. Works but violates the project's prefix convention; harder to discover via `dotnet user-secrets list`.
- *Aspire-specific resource secret*. Would couple the seed step to Aspire; the seeder must work in test fixtures and in non-Aspire deployments.

---

## Decision 7: Sentinel password emission ordering (REVIEW-SPEC.md guard-rail #1)

**Decision**: The seeder emits the WARN-level log line **before** calling `UserManager.CreateAsync(user, password)`. The order is:

1. Resolve password (configured value or generate via `RandomNumberGenerator.GetBytes(24)` → base64).
2. If the password was generated (i.e., not configured), call `_logger.LogWarning("Sentinel admin '{Email}' will be created with auto-generated password: {Password}", SentinelEmail, password)`. The default `ILogger` console sink is synchronous; the message is flushed before the call returns under standard configuration.
3. Construct the `ApplicationUser` via `ApplicationUser.CreateSentinel(SentinelEmail)` (a static factory that sets `IsSystemSentinel = true`).
4. Call `await userManager.CreateAsync(user, password)`.
5. Call `await userManager.AddToRoleAsync(user, "Admin")`.

If step 2 throws or the process crashes between step 2 and step 4, the password was logged but no user exists — the operator can re-run the seed (it remains idempotent because step 4 fails on duplicate email, but the seeder catches that and treats it as already-seeded; a second run with the same generated password would not happen because the seed branch checks for existence first; instead the operator captures the password from the log and the next deploy with `Admin:DefaultPassword` configured to that exact value lands correctly).

If step 4 fails after step 2, the password is logged but unused — the operator can simply discard that line and re-run with a configured password. Worst-case waste: one log line.

**Rationale**: The review flagged that a crash between user-row commit and log-flush leaves the sentinel password unrecoverable. Inverting the order (log-first) eliminates that failure mode at the cost of (rare) wasted log lines. The same outcome could be achieved with a synchronous flush after `CreateAsync`, but log-first is simpler and the only configuration-dependent detail (sink synchrony) is removed from the critical path.

**Alternatives considered**:
- *Persist user row first, then log*. The original failure mode the review flagged. Rejected.
- *Use a transactional outbox pattern (log + persist in one DB transaction)*. Way over-engineered for a one-time bootstrap.

---

## Decision 8: 403 Access Denied response (REVIEW-SPEC.md guard-rail #2)

**Decision**: Change `ConfigureApplicationCookie.AccessDeniedPath` from `/Account/Login` to `/Account/AccessDenied`. Add `AccountController.AccessDenied()` action that returns `View()` with `Response.StatusCode = StatusCodes.Status403Forbidden`. Add `Views/Account/AccessDenied.cshtml` rendering an empty-state via the existing `_EmptyState` partial. Unauthenticated requests still hit `LoginPath = /Account/Login` (unchanged).

**Rationale**: SC-007 specifies non-Admin users receive 403 Forbidden when accessing `/Admin/...`. The current cookie config redirects authenticated-but-unauthorized users back to login — observable as an infinite-feeling redirect loop and inconsistent with the spec's success criterion. The fix is two lines of cookie config plus one controller action plus one view.

**Trade-off**: The behavior change applies to **all** Authorize attributes across the codebase (specs 002–008 included), not just `/Admin/...`. Audit before merging: any existing surface that depends on the access-denied → login redirect (e.g., expecting an applicant who navigates to a reviewer URL to land at login) would now see a 403 page instead. The plan calls for a sweep of existing E2E PageObjects to verify no test expects the old redirect; expected impact: zero (tests that hit access-denied paths are rare).

---

## Decision 9: Last-non-sentinel-admin guard counting query

**Decision**: The guard executes the following query (paraphrased) inside the service before applying any disable / role-change / role-removal action:

```text
adminRoleId = roles.Single(r => r.Name == "Admin").Id
activeNonSentinelAdmins = dbContext.UserRoles.IgnoreQueryFilters()
    .Where(ur => ur.RoleId == adminRoleId)
    .Where(ur => !ur.User.IsSystemSentinel)
    .Where(ur => ur.User.LockoutEnd == null || ur.User.LockoutEnd <= DateTimeOffset.UtcNow)
    .CountAsync()
```

The service then simulates the post-action count: for a disable, `count - 1`; for a role-change-away-from-Admin, `count - 1`; for re-enable of a disabled admin, `count + 1` (no rejection — re-enable always increases). If the post-action count would be 0, the service throws `LastAdministratorException`.

The `IgnoreQueryFilters()` is critical: without it the EF global filter would silently exclude the sentinel from the join *and* potentially other future-flagged users. Forcing the bypass makes the count explicit about what's included.

**Rationale**: A correct guard must be deterministic and observable in logs/tests. Anchoring on `LockoutEnd` (not on a separate "Active" boolean) reuses Identity's existing disable semantics. The `IsSystemSentinel == false` filter is explicit (not relying on the global filter) so the query's intent is local to the read.

**Alternatives considered**:
- *Use `UserManager.GetUsersInRoleAsync("Admin")`*. Loads every admin user fully into memory, then filters in C#. Wasteful at any scale. The direct LINQ query above is one round-trip and one COUNT.
- *Maintain a denormalized `ActiveAdminCount` field on a system-config table*. Requires careful invalidation — easy to drift. Direct count is correct by construction.

---

## Decision 10: URL routing for the admin area

**Decision**: Use **attribute routing** on dedicated controllers `AdminUsersController` and `AdminReportsController` placed under `Controllers/Admin/` (folder for organization only). Routes:
- `[Route("Admin/Users")]` on `AdminUsersController`
- `[Route("Admin/Reports")]` on `AdminReportsController`

The existing `AdminController.cs` (templates / configuration) keeps its current convention-routed `/Admin/Configuration`, `/Admin/ImpactTemplates`, `/Admin/CreateTemplate`, `/Admin/EditTemplate`, `/Admin/Index`. The `/Admin` URL namespace is administered cooperatively by three controllers; all three carry `[Authorize(Roles="Admin")]`.

**Rationale**: Adding a real ASP.NET MVC Areas folder structure would force the existing `AdminController.cs` and its views to move into `/Areas/Admin/...` to avoid route ambiguity — a non-trivial refactor outside this spec's scope. Attribute routing on dedicated controllers gets the URL namespace right with no impact on existing files. Spec 008's plan structure is preserved.

**Alternatives considered**:
- *Real MVC Areas (`Areas/Admin/Controllers/...`)*. Larger diff (move existing controller and views), no behavioral benefit.
- *Add `Users` / `Reports` actions to the existing `AdminController.cs`*. Would bloat that controller and conflate user-management with templates/configuration. Separation of concerns prefers separate controllers.

---

## Summary of resolved Open Questions

The four open questions in `spec.md` resolve as follows:

1. **Applicant demotion in-flight applications** → preserved (the existing `Applicant` domain record is left intact). UI for the demoted user is not modified by this spec; whether they retain read-only access to their applications is an operational concern surfaced in `data-model.md`. No new requirement introduced.
2. **`ADMIN_DEFAULT_PASSWORD` configuration key shape** → `Admin:DefaultPassword` (Decision 6).
3. **Sentinel password rotation procedure (post first-deploy)** → documented in `quickstart.md` as an out-of-product runbook; the operator deploys with `Admin:DefaultPassword` configured to the new value, the seeder finds the sentinel already exists, **does not rewrite** the password (idempotent skip). To force a rotation, the operator must manually clear the sentinel's `PasswordHash` in the user store before redeploy. v1 has no in-product rotation.
4. **Last-non-sentinel-admin guard interaction with re-enable** → re-enable always increases the active-admin count, so the guard never blocks an enable. Codified in Decision 9 above.
