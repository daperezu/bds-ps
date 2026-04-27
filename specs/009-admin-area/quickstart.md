# Quickstart: Admin Role and Admin Area

This is the implementer's quick-start. Follow these steps to bring the admin area up locally, capture the sentinel password on first run, and exercise the golden-path flows. Use this also as the manual smoke-test procedure before merging.

---

## 1. Prerequisites

- The branch `009-admin-area` is checked out.
- The `.NET 10` SDK is on `PATH` and Aspire's local prerequisites (Docker / Podman) are available.
- The dacpac project has been built so the new schema (`FirstName`, `LastName`, `IsSystemSentinel`, `MustChangePassword` columns + filtered index) is ready to deploy.

## 2. First-time local bootstrap

### 2a. Without a configured sentinel password (default dev path)

1. Start the AppHost with the standard dev settings:
   ```text
   dotnet run --project src/FundingPlatform.AppHost
   ```
2. Watch the application log on first startup. The seed step emits **one WARN-level line** before persisting the sentinel:
   ```text
   warn: FundingPlatform.Infrastructure.Identity.IdentityConfiguration[0]
         Sentinel admin 'admin@FundingPlatform.com' will be created with auto-generated password: <BASE64-ENCODED-PASSWORD-HERE>
   ```
3. **Capture the password.** Copy the base64 string. There is no in-product way to recover it later — if you miss the line, see "Recovering from a missed sentinel password" below.
4. Subsequent restarts find the sentinel already exists and **do not** regenerate or re-log the password.

### 2b. With a configured sentinel password (recommended for shared dev / CI)

1. Set the configuration key before first run:
   ```text
   dotnet user-secrets set "Admin:DefaultPassword" "<your-strong-password>" \
       --project src/FundingPlatform.Web
   ```
   (Or set the `Admin__DefaultPassword` env var, or wire via `WithEnvironment("Admin__DefaultPassword", "...")` in the AppHost.)
2. Start the AppHost. The seed uses the configured password. **No password line is logged.**
3. The configured password is honored on first seed only; on subsequent runs the seed step is a no-op (idempotent).

### 2c. Recovering from a missed sentinel password

There is no in-product rotation in v1. The recovery path is out-of-product:
1. Stop the application.
2. In a SQL session against the dev database, clear the sentinel's password hash:
   ```text
   UPDATE [dbo].[AspNetUsers] SET [PasswordHash] = NULL WHERE [Email] = N'admin@FundingPlatform.com';
   ```
3. Set `Admin:DefaultPassword` to a known value via user-secrets / env var.
4. **Modify the seeder check temporarily** so it treats the sentinel as un-passworded and re-applies the configured password. (The default seeder skips on existence; the recovery path requires hand-rolling. This is a deliberate v1 trade-off — operationally rare.)
5. Restart, then immediately unset the temporary override or revert the seeder change.

---

## 3. Sign in as the sentinel and verify

1. Browse to `https://localhost:<port>/Account/Login`.
2. Email: `admin@FundingPlatform.com`. Password: the one captured/configured above.
3. After sign-in, you should land on the standard authenticated home page with Admin privileges.
4. Verify the sidebar shows `Users` and `Reports` entries plus all Reviewer entries (because of the Admin-implies-Reviewer claims transformation).
5. Browse to `/Admin/Users`. **The sentinel must not appear in the listing.** This verifies the global query filter is wired correctly.

---

## 4. Golden-path smoke (US1 — full lifecycle)

Performed as the sentinel admin (or a non-sentinel Admin if one already exists):

1. **Create**: `/Admin/Users` → "Create user". Fill: First=`Test`, Last=`User`, Email=`test.user@example.com`, Phone=blank, Role=`Reviewer`, InitialPassword=`TempPass1!`. Submit.
2. **Verify creation**: row appears in the listing with Role=Reviewer, Status=Active.
3. **Force-change-on-next-login**: open a private/incognito window, sign in as `test.user@example.com` with `TempPass1!`. The system should redirect to `/Account/ChangePassword`. Set a new password. Verify subsequent navigation works.
4. **Edit email**: back as Admin, edit the test user, change email to `test.user.renamed@example.com`. The other browser session should be invalidated within ≤60 seconds (the `SecurityStampValidationInterval`); next click should redirect to login.
5. **Edit role**: change role to `Applicant`. The form reveals the `Legal ID` field. Provide one. Save. Verify the listing shows Role=Applicant. The other session is invalidated again.
6. **Reset password**: trigger Reset Password on the test user. Provide a new temp password. The other session is invalidated; the user must change password on next sign-in.
7. **Disable**: trigger Disable on the test user (confirmation dialog must appear). Verify the row shows Status=Disabled and the user cannot sign in.
8. **Enable**: trigger Enable. Verify the row returns to Status=Active and the user can sign in with their existing password.

---

## 5. Sentinel-immutability smoke (US2 — service-layer guard)

1. As Admin, capture the sentinel's user id from the `dbo.AspNetUsers` row. (The id is not visible in the UI; use a SQL query against the dev DB.)
2. Construct a direct POST to `/Admin/Users/<sentinelId>/Disable`. Verify the response is a 4xx with the `SENTINEL_IMMUTABLE` named-rule error and the sentinel state is unchanged.
3. Construct a direct POST to `/Admin/Users/<sentinelId>/ResetPassword` with a body. Verify rejection.
4. Construct a direct POST to `/Admin/Users/<sentinelId>/Edit` with field changes. Verify rejection.
5. From the Create form, attempt to create a new user with email `admin@FundingPlatform.com`. Verify the `EMAIL_IN_USE` validation error.

---

## 6. Last-non-sentinel-admin smoke (US3)

1. From a clean dev DB (or after disabling all but one admin), confirm the platform has exactly **one** active non-sentinel admin (e.g., the demo `admin@demo.com` user). The sentinel does not count.
2. Sign in as that single admin. Try to disable yourself — verify rejection (`SELF_MODIFICATION_BLOCKED`).
3. Try to demote yourself to Reviewer via the Edit form — verify rejection.
4. Promote a Reviewer (or create a new Admin) so the platform has two active non-sentinel admins.
5. Now disable the original admin — verify success. The platform now has one active non-sentinel admin (the new one).
6. As the new admin, try to disable yourself — verify rejection (last-admin guard).
7. Re-enable the original admin to restore healthy state.

---

## 7. Admin-inherits-Reviewer smoke (US4)

1. Sign in as a user with role exclusively `Admin` (not the sentinel — use a freshly-created admin that you only assigned to `Admin`).
2. Walk the reviewer routes:
   - `/Review` (review queue)
   - `/Review/Review/<id>` (review detail page for an existing application)
   - `/Review/SigningInbox` (signing inbox)
   - `/Review/GenerateAgreement/<id>` (generate-agreement page)
   - `/ApplicantResponse/...` (any reviewer-gated action)
3. Each must respond `200 OK`. None should return `403 Forbidden`.
4. Verify by inspecting source: no `[Authorize(Roles="Reviewer")]` attribute in specs 002, 004, 006, 007 has been modified.

## 8. Reports stub smoke (US5)

1. As Admin, navigate to `/Admin/Reports`. Verify the empty-state card with copy along the lines of "Reports module coming soon".
2. Sign in as a Reviewer. Browse to `/Admin/Reports`. Verify HTTP 403 (the new AccessDenied page) — not a redirect to login.
3. Sign out. Browse to `/Admin/Reports`. Verify redirect to `/Account/Login`.

## 9. Sidebar visibility smoke (US6)

1. Sign in as Applicant. Sidebar must not show `Users` or `Reports`.
2. Sign in as Reviewer. Sidebar must not show `Users` or `Reports`.
3. Sign in as Admin. Sidebar must show both `Users` and `Reports`, plus all the Reviewer entries (because of the claims transformation).

---

## 10. Automated tests

Run the full Playwright E2E suite:
```text
dotnet test tests/FundingPlatform.Tests.E2E
```

The new test classes (per plan.md):
- `AdminUserLifecycleTests` (US1)
- `SentinelExclusionTests` + `SentinelImmutabilityTests` (US2)
- `LastAdminGuardTests` + `SelfModificationGuardTests` (US3)
- `AdminInheritsReviewerTests` (US4)
- `AdminReportsStubTests` (US5)
- `RoleAwareSidebarAdminEntriesTests` (US6)

All must pass. Per project convention, the E2E fixture uses `--EphemeralStorage=true` so each run starts with a clean SQL Server container; the sentinel is freshly seeded for every test run, and the bootstrap log line is emitted into the per-fixture log capture. Tests that need the sentinel password use the configured-password path (Admin:DefaultPassword set to a fixture-known value) rather than the auto-generated path, so test assertions are deterministic.

---

## 11. Pre-merge checklist

- [ ] Dacpac applies cleanly with the four new columns + filtered index.
- [ ] `Program.cs` has the IdentityUser → ApplicationUser swap, the AccessDeniedPath fix, the SecurityStampValidationInterval, the IClaimsTransformation registration, and the MustChangePasswordMiddleware insertion.
- [ ] All cascade renames (`UserManager<IdentityUser>` → `UserManager<ApplicationUser>`) compile.
- [ ] The full E2E suite (specs 001–008 plus the new 009 classes) is green.
- [ ] Manual smoke for each section above passes.
- [ ] No new NuGet packages were added.
- [ ] The PDF-target view files (`Views/FundingAgreement/Document.cshtml`, `_FundingAgreementLayout.cshtml`) remain byte-identical (per spec 008's invariant).
- [ ] Greps confirm no inline `style=` attributes in the new admin views, no badge markup outside `_StatusPill`, no document references outside `_DocumentCard` (extending spec 008's view-tree invariants).
