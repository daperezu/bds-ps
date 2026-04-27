# Feature Specification: Admin Role and Admin Area

**Feature Branch**: `009-admin-area`
**Created**: 2026-04-25
**Status**: Draft
**Input**: User description: "Admin role and admin area — introduce a third role (Admin) plus a dedicated /Admin MVC area for in-product user management, plus a permanent immutable sentinel default-admin user, plus an access-gated stub Reports page. The platform currently has only Applicant and Reviewer roles and no in-product way to create users, assign roles, disable accounts, or reset passwords; every account fix today requires a developer running SQL or seed code. This spec ends that."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Admin manages the full user lifecycle from the UI (Priority: P1)

Today the platform has no in-product way to create users, assign roles, disable accounts, or reset passwords — every account fix requires a developer running SQL or modifying seed code. This story introduces a dedicated `/Admin/Users` surface where an Admin performs the full lifecycle on a user without engineering involvement: create with role and initial password, edit identity/profile fields and role, reset password, and disable / re-enable. Newly-created users and users whose passwords were just reset are forced to change the password on next login. Users whose email or role was just changed have their active sessions invalidated so the new claims take effect. After this story alone, ops can run the platform end-to-end without a developer in the loop, which is the largest real-world unblock the spec produces.

**Why this priority**: This is the primary unblock. The other P1 stories (sentinel, last-admin guard) only matter because user management exists. Ship this and ops is no longer dependent on engineering for routine account work.

**Independent Test**: Log in as an Admin, open `/Admin/Users`, create a new Reviewer with an initial password, confirm the user is forced to change the password on first login, edit the user's email and confirm their existing session is invalidated, reset the user's password and confirm the force-change-on-next-login behavior, disable the user and confirm they cannot log in, re-enable and confirm they can.

**Acceptance Scenarios**:

1. **Given** an Admin on `/Admin/Users`, **When** they fill the create-user form with first name, last name, email, optional phone, role = Reviewer, and an initial password, **Then** the user is created in the Active state, with the must-change-password-on-next-login flag set, and the new user appears immediately in the list.
2. **Given** an Admin creating a user with role = Applicant, **When** they submit the form, **Then** the form additionally requires a legal id; on submit the system creates the Identity user, the role assignment, and the role-specific applicant record atomically.
3. **Given** an Admin editing an existing user, **When** they change the user's email, **Then** the user's login identifier (UserName) updates atomically and the target user's active sessions are invalidated.
4. **Given** an Admin editing an existing user, **When** they change the user's role, **Then** the previous role is removed, the new role is applied exclusively, and the target user's active sessions are invalidated so the new claims take effect on next request.
5. **Given** an Admin viewing a user, **When** they trigger Reset Password and provide a new temporary password, **Then** a confirmation dialog appears; on confirm the password is applied, the must-change-on-next-login flag is set, and the target user's active sessions are invalidated.
6. **Given** an Admin viewing an Active user, **When** they trigger Disable, **Then** a confirmation dialog appears; on confirm the user becomes Inactive, cannot log in, and any existing sessions are invalidated immediately.
7. **Given** an Admin viewing a Disabled user, **When** they trigger Enable, **Then** the user becomes Active again and can log in with their existing password (no force-change is applied unless an Admin also reset the password during or before enable).
8. **Given** an Admin demoting a user from Applicant to Reviewer, **When** the change is applied, **Then** the user's existing applicant record is preserved (history) but the user no longer holds the Applicant role; the legal-id field is no longer editable from the admin area.
9. **Given** any submission with multiple validation errors (e.g., invalid email format, weak password, missing legal id for Applicant), **When** the Admin submits, **Then** all errors are surfaced together on the form, not one at a time.

---

### User Story 2 — Sentinel default admin: seeded, hidden, immutable, recovery-only (Priority: P1)

A platform that depends on Admins to manage Admins has a footgun: every Admin getting disabled, demoted, or locked out leaves nobody to recover. This story introduces a permanent sentinel admin (`admin@FundingPlatform.com`) seeded on first deploy, with a randomly-generated password (or a configured override) emitted exactly once at WARN log level on first startup. The sentinel is hidden from every user listing, search result, and edit surface, and every modification attempt — UI, constructed URL, or service call — is rejected at both the query layer and the service layer. The sentinel can still log in via the normal login form, providing a guaranteed recovery path if every other Admin is disabled. After this story alone, the platform has a "root of trust" account that cannot be tampered with through any in-product action.

**Why this priority**: Without the sentinel, any combination of disabled/demoted Admins becomes an engineering ticket to fix. With it, ops always has a recovery path. The protection must live at both the data layer (so listings cannot leak it) and the service layer (so direct actions cannot mutate it) — either alone is insufficient.

**Independent Test**: Deploy the system fresh with no `ADMIN_DEFAULT_PASSWORD` configured. Verify the sentinel exists in the DB with the system flag set and that the generated password appears exactly once in the application log at WARN level. Log in as a regular Admin, open `/Admin/Users`, search by `admin@FundingPlatform.com`, and confirm zero results regardless of filter. Construct a URL targeting the sentinel's user id (`/Admin/Users/{id}/Edit`) and confirm the request is rejected. Attempt to create a new user with email `admin@FundingPlatform.com` and confirm rejection. Log in as the sentinel using the credentials captured from the bootstrap log and confirm the login succeeds.

**Acceptance Scenarios**:

1. **Given** a fresh deploy with no `ADMIN_DEFAULT_PASSWORD` configured, **When** the system seeds, **Then** the sentinel admin exists in the user store with the system flag set, and the generated password appears exactly once in the application log at WARN level. On subsequent restarts no password is regenerated and no password is logged.
2. **Given** a fresh deploy with `ADMIN_DEFAULT_PASSWORD` configured, **When** the system seeds, **Then** the sentinel admin is created with that password and no password is written to the log.
3. **Given** an Admin browsing `/Admin/Users`, **When** they search by name or email containing `admin@FundingPlatform.com`, **Then** the sentinel does not appear in the results, regardless of any filter combination.
4. **Given** an Admin who guesses or constructs the sentinel's user id, **When** they request `/Admin/Users/{sentinelId}/Edit`, `/Admin/Users/{sentinelId}/Disable`, `/Admin/Users/{sentinelId}/ResetPassword`, or any other modification action, **Then** the request is rejected at the service layer with a clear error response and no partial mutation occurs.
5. **Given** an Admin attempting to create a new user, **When** they submit the form with email `admin@FundingPlatform.com`, **Then** the submission is rejected with an "email already in use" validation error.
6. **Given** the sentinel's credentials, **When** an operator submits them on the standard login form, **Then** authentication succeeds and the sentinel reaches the standard authenticated landing page with Admin privileges.

---

### User Story 3 — Self-modification and last-admin guards prevent accidental lockout (Priority: P1)

An Admin can disable, demote, or change other users — but if they can do those things to themselves, or to the last remaining Admin, the platform can be locked out by a single mistake. This story enforces three guards in the application/service layer with clear, named-rule errors surfaced to the UI: (1) an Admin cannot disable themselves, (2) an Admin cannot change their own role from the admin area, (3) the system rejects any action that would leave zero non-sentinel Admins in the Active state. The sentinel does not count toward "remaining admins" because operators do not see or use it for routine work. Self-edit of name, phone, or password through the standard account surface remains unaffected — those flows are outside this spec's scope. After this story alone, the only path to a fully-locked-out platform is the out-of-product sentinel-recovery procedure.

**Why this priority**: Pure protection. Cheap to implement, high cost if missing. Without it, a single Admin clicking "Disable" on the wrong row in `/Admin/Users` could leave the platform with no operational Admins.

**Independent Test**: Log in as an Admin. Attempt to disable yourself from `/Admin/Users` — verify a named-rule error rejects the action. Attempt to change your own role to Reviewer or Applicant from the admin area — verify rejection. With exactly one active non-sentinel Admin remaining, attempt to disable them or demote them — verify rejection. Add a second active Admin, then disable the original — verify success.

**Acceptance Scenarios**:

1. **Given** an Admin on their own row in `/Admin/Users`, **When** they trigger Disable on themselves, **Then** the action is rejected at the service layer with a named-rule error such as "Administrators cannot disable their own account."
2. **Given** an Admin editing their own user record, **When** they attempt to change their own role to Reviewer or Applicant, **Then** the action is rejected with a named-rule error such as "Administrators cannot change their own role from the admin area."
3. **Given** an Admin editing their own user record, **When** they attempt to change their own email from the admin area, **Then** the action is rejected with a named-rule error such as "Administrators cannot change their own email from the admin area."
4. **Given** the platform has exactly one active non-sentinel Admin, **When** any actor attempts to disable that Admin or demote them to Reviewer/Applicant, **Then** the action is rejected with a named-rule error such as "Cannot disable the last remaining administrator. Promote another user to Admin first."
5. **Given** the platform has two or more active non-sentinel Admins, **When** an Admin disables one of the others, **Then** the action succeeds and the count of active Admins decreases by one.
6. **Given** a previously-disabled Admin, **When** another Admin re-enables them, **Then** the last-admin guard does not incorrectly block the enable; the action succeeds even when re-enabling produces the only active non-sentinel Admin in the system.

---

### User Story 4 — Admin inherits all Reviewer powers (Priority: P2)

Every existing reviewer-gated route across the platform — review queue, review detail, signing inbox, applicant-response surfaces (specs 002, 004, 006, 007) — must continue to work for users in the Admin role, without any of those routes' code being changed. The Admin role authorization passes wherever the Reviewer role authorization passes. After this story, an Admin is a strict superset of Reviewer for read and write authorization purposes; no surface exists that a Reviewer can use but an Admin cannot.

**Why this priority**: Required by the seed and by the operational reality that Admins must be able to step in and unblock review work. P2 (not P1) because it does not deliver new user-facing capability — it preserves an inheritance contract that the existing codebase already informally assumes.

**Independent Test**: Log in as a user who holds only the Admin role. Walk every route gated by `[Authorize(Roles="Reviewer")]` in features 002, 004, 006, and 007 — review queue, review detail, applicant response, appeal, generate-agreement, signing inbox, signing review — and confirm each loads without 403, and that write actions on those routes succeed under the Admin's authorization.

**Acceptance Scenarios**:

1. **Given** a user holding only the Admin role, **When** they navigate to any route currently gated by `[Authorize(Roles="Reviewer")]` in specs 002, 004, 006, or 007, **Then** the route loads with HTTP 200 (no 403) and write actions on those routes succeed under their authorization.
2. **Given** the existing codebase, **When** the Admin-inherits-Reviewer behavior is implemented, **Then** no gate's code in specs 002, 004, 006, or 007 is modified to enumerate the Admin role explicitly (the inheritance is achieved through configuration, not by editing every existing `[Authorize]` attribute).

---

### User Story 5 — Reports stub page exists and is gated by Admin role (Priority: P2)

The seed acknowledges that Admins will have access to reports in a future iteration but reports themselves are not designed yet. To lock in the access-control contract now, a stub page exists at `/Admin/Reports`, gated by `[Authorize(Roles="Admin")]`, rendering a single empty-state card stating "Reports module coming soon" (or equivalent copy). It carries no data and no widgets. After this story, future reporting specs slot into a route that already enforces the right authorization, and operators see a deliberate placeholder rather than a mystery 404.

**Why this priority**: Mostly-symbolic but cheap. P2 because it is genuinely useful as a placeholder and keeps the admin sidebar shape stable across the rollout of the future reports feature.

**Independent Test**: Log in as an Admin and navigate to `/Admin/Reports` — verify the page renders the empty-state card. Log in as a Reviewer or Applicant (or hit the URL unauthenticated) and verify the request returns 403 / redirects to login.

**Acceptance Scenarios**:

1. **Given** a user holding the Admin role, **When** they navigate to `/Admin/Reports`, **Then** the page renders an empty-state card with copy along the lines of "Reports module coming soon" and no other widgets.
2. **Given** a user not holding the Admin role (Reviewer, Applicant, or unauthenticated), **When** they request `/Admin/Reports`, **Then** the response is 403 Forbidden (or redirects to the login page for unauthenticated requests).
3. **Given** a user holding the Admin role, **When** they view the sidebar, **Then** a "Reports" entry is visible and links to `/Admin/Reports`.

---

### User Story 6 — Admin area consumes the Tabler shell and partials (Priority: P3)

The admin area is the first new feature surface to land after the spec 008 Tabler migration. To preserve consistency, the area uses the established Tabler shell, role-aware sidebar, and reusable partials (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`, `_ConfirmDialog`). No bespoke UI patterns are introduced. Sidebar entries (`Users`, `Reports`) appear only for users in the Admin role, mirroring the role-aware-sidebar pattern from spec 008. After this story, the admin area looks and feels like the rest of the product without any feature-specific styling work.

**Why this priority**: P3 because it is a quality bar, not a delivery dependency. The previous stories all assume Tabler-shell consumption as the default; this story makes the assumption explicit and testable.

**Independent Test**: Log in as Admin. Confirm the admin sidebar entries (Users, Reports) appear only for Admin users. Walk every admin-area surface (Users list, Users edit, Reports stub) and confirm each uses the Tabler shell, the existing partials where applicable, and no bespoke styling. Log in as Reviewer and Applicant — confirm the Admin entries do not appear in their sidebars.

**Acceptance Scenarios**:

1. **Given** every admin-area view, **When** rendered, **Then** each view uses the Tabler shell and consumes the existing partials (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`, `_ConfirmDialog`) where applicable; no bespoke UI patterns are introduced.
2. **Given** a destructive admin action (Disable, Reset Password), **When** the Admin triggers it, **Then** `_ConfirmDialog` opens with a one-line rationale and the action only proceeds on explicit confirmation.
3. **Given** a non-Admin user (Reviewer, Applicant, or unauthenticated), **When** they view the sidebar, **Then** the "Users" and "Reports" entries do not appear.
4. **Given** a search of the admin-area view tree, **When** searched for inline `style=` attributes or badge markup outside `_StatusPill`, **Then** zero occurrences are found (consistent with spec 008's view-tree invariants).

---

### Edge Cases

- An Applicant is demoted to Reviewer while they have active applications. Their existing applicant record is preserved (history) and the user no longer holds the Applicant role. Visibility/operability of in-flight applications for that user is an operational concern flagged as an Open Question for the planning phase.
- A user previously in the Applicant role is re-assigned the Applicant role. The pre-existing applicant record is reused; the legal-id field becomes editable from the admin area again.
- A user is re-enabled after being disabled. The user can log in with their existing password (no force-change is applied unless an Admin also reset the password during or before re-enable).
- An Admin creates a user with role = Applicant, but the legal id collides with an existing applicant record. Validation surfaces the collision; neither the Identity user nor the applicant record is persisted (atomic rollback).
- The sentinel password is forgotten and no `ADMIN_DEFAULT_PASSWORD` was ever captured. There is no in-product recovery; the operator must rotate the sentinel via an out-of-product procedure (env-var bootstrap on next deploy, or manual user-store intervention). Documented in the plan, not implemented as a UI feature.
- An Admin edits another Admin's email, role, or password. The target Admin's sessions are invalidated. The acting Admin's own session is unaffected unless they edited themselves — which is blocked by the self-modification guard.
- Two Admins simultaneously edit the same target user. Last write wins (no optimistic concurrency in v1). Documented as an acceptable v1 trade-off; revisit if observed in practice.
- A user-creation request arrives with an invalid role string (e.g., a crafted POST). The submission is rejected as invalid input; no partial creation occurs.
- A previously-disabled Admin is re-enabled and is the only active non-sentinel Admin afterward. The last-admin guard does not block this operation — re-enabling never reduces the count of active Admins, only increases it.
- The sentinel is not Admin's only seeded Admin in development environments. The existing `admin@demo.com` demo user (seeded by the existing `IdentityConfiguration.cs`) continues to exist as a regular Admin — visible in the listing, modifiable, disable-able. Only `admin@FundingPlatform.com` carries the sentinel flag.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST recognize exactly three roles — `Applicant`, `Reviewer`, `Admin` — and MUST enforce that each user holds exactly one of these roles at any time. Changing a user's role replaces the previous role atomically.
- **FR-002**: System MUST treat the `Admin` role as a strict superset of the `Reviewer` role for authorization purposes: any route, action, or resource currently gated for `Reviewer` MUST also be accessible to users in the `Admin` role, without modifying the existing gate code in specs 002, 004, 006, or 007.
- **FR-003**: System MUST expose an admin-only area at `/Admin` with two sub-surfaces — `/Admin/Users` and `/Admin/Reports` — both of which require the `Admin` role at the route/handler level. Non-Admin requests MUST receive 403 Forbidden (or redirect to login if unauthenticated).
- **FR-004**: System MUST present role-aware sidebar entries for `Users` and `Reports` only to users in the `Admin` role, consistent with the role-aware-sidebar pattern established in spec 008.
- **FR-005**: System MUST list non-sentinel users on `/Admin/Users` with columns: full name (first + last), email, role, status (Active / Disabled), and created date. The listing MUST support filter by role, filter by status (Active / Disabled / All), free-text search by name or email, and server-side pagination consistent with existing review-queue patterns.
- **FR-006**: System MUST allow an Admin to create a new user from `/Admin/Users` with: first name, last name, email, optional phone, role, and an initial password. When the chosen role is `Applicant`, the form MUST additionally require a legal id and the system MUST persist the role-specific applicant record atomically with the Identity user.
- **FR-007**: System MUST set a "must change password on next login" flag on every newly-created user, and MUST enforce that flag at the next sign-in attempt.
- **FR-008**: System MUST allow an Admin to edit a user's email, first name, last name, phone, role, and (when the user is or becomes an Applicant) legal id. The performance-score field MUST NOT be editable from the admin area.
- **FR-009**: System MUST update a user's login identifier (UserName) atomically when their email is changed, and MUST invalidate the target user's active sessions immediately on email change.
- **FR-010**: System MUST invalidate the target user's active sessions immediately on role change so that the new role's claims are reissued at next request.
- **FR-011**: System MUST allow an Admin to reset a user's password by entering a new temporary password. After reset, the system MUST set the "must change password on next login" flag for the target user and MUST invalidate the target user's active sessions immediately. The action MUST require explicit confirmation via `_ConfirmDialog` before applying.
- **FR-012**: System MUST allow an Admin to disable an Active user. A disabled user MUST NOT be able to log in, and existing sessions for the target user MUST be invalidated immediately. The action MUST require explicit confirmation via `_ConfirmDialog` before applying.
- **FR-013**: System MUST allow an Admin to re-enable a Disabled user. Re-enable MUST NOT change the user's password and MUST NOT set the "must change password on next login" flag.
- **FR-014**: System MUST surface all user-input validation errors together on each admin form (e.g., invalid email, weak password, missing legal id, email collision); errors MUST NOT be presented one at a time.
- **FR-015**: System MUST reject any action by an Admin against their own user record from the admin area when the action is: disable, change own role, or change own email. Each rejection MUST be enforced in the application/service layer and MUST surface a named-rule error to the UI.
- **FR-016**: System MUST reject any action that would leave zero non-sentinel Admins in the Active state, including but not limited to: disabling the last active non-sentinel Admin, demoting the last active non-sentinel Admin to a non-Admin role. The sentinel MUST NOT count toward the "remaining admins" total for this guard. Each rejection MUST be enforced in the application/service layer and MUST surface a named-rule error to the UI.
- **FR-017**: System MUST seed exactly one sentinel admin user with email `admin@FundingPlatform.com` on first deploy. The sentinel MUST be marked with a system-level flag that distinguishes it from all other users.
- **FR-018**: System MUST resolve the sentinel's password on first seed in this order: if a configured secret (e.g., an `ADMIN_DEFAULT_PASSWORD` environment variable or its Aspire/user-secret equivalent) is present, use it; otherwise generate a cryptographically strong random password. When the system generates a random password, it MUST emit the password exactly once at WARN log level on first startup, and MUST NOT log the password on any subsequent startup.
- **FR-019**: System MUST exclude the sentinel from every user listing, search result, and edit-target query at the data-access layer, regardless of any filter or search input. The sentinel MUST NOT appear in `/Admin/Users` or anywhere else where users are enumerated.
- **FR-020**: System MUST reject every modification attempt against the sentinel at the application/service layer. This applies to disable, enable, role change, email change, profile change, password reset, and delete. Modification rejection MUST occur regardless of how the request is constructed (form, URL, direct service call) and MUST produce no partial mutation.
- **FR-021**: System MUST allow the sentinel to log in via the standard login form using its credentials. After login the sentinel MUST receive `Admin` role authorization.
- **FR-022**: System MUST treat sentinel re-seeding as idempotent: if a user with the sentinel email and the system flag already exists, the seed step MUST be a no-op and MUST NOT rewrite the sentinel's password.
- **FR-023**: System MUST reject any attempt to create a new non-sentinel user with email `admin@FundingPlatform.com` (the sentinel always exists with that email and the user-store's email uniqueness covers this case).
- **FR-024**: System MUST render `/Admin/Reports` as a stub page consisting of a clear page title and a single empty-state card with copy along the lines of "Reports module coming soon" (or equivalent). The stub MUST contain no data and no widgets; its purpose is to lock in the access-control contract.
- **FR-025**: Every admin-area view MUST consume the Tabler shell and the reusable partials established in spec 008 (`_PageHeader`, `_DataTable`, `_FormSection`, `_StatusPill`, `_EmptyState`, `_ActionBar`, `_ConfirmDialog`) where applicable. No bespoke UI patterns or inline `style=` attributes MUST be introduced by this feature.
- **FR-026**: Every user story (create, edit, disable/enable, reset password, sentinel exclusion at the listing level, sentinel modification rejection at the service layer, last-admin guard, self-modification guard, role-gated `/Admin/...` access) MUST be covered by Playwright end-to-end tests covering the golden path and key error scenarios.
- **FR-027**: Sentinel exclusion MUST be enforced at BOTH the data-access/query layer (so that no listing or search can leak the sentinel even if a service-layer guard is bypassed) AND the application/service layer (so that no direct write can mutate the sentinel even if a query layer is bypassed). Either layer alone is insufficient.
- **FR-028**: Schema changes for the sentinel system flag MUST be made in the SQL Server Database Project (dacpac), per constitution principle IV. EF migrations MUST NOT be used.
- **FR-029**: System MUST NOT introduce optimistic-concurrency control on the user entity in v1. Concurrent edits resolve as last-write-wins. This is a documented v1 trade-off; revisit if usage patterns surface conflicts.

### Key Entities

- **User** (existing, extended): The authenticatable entity managed by the user store. This spec adds three administrative-lifecycle attributes that are not user-facing today: a system-flag attribute that marks the sentinel admin, a status attribute that distinguishes Active from Disabled users (existing user-store mechanism, surfaced as a managed attribute by this spec), and the must-change-password-on-next-login attribute (existing user-store mechanism, surfaced as a managed attribute by this spec). User profile attributes managed from the admin area: email (also the login identifier), first name, last name, optional phone. Performance score is NOT managed from the admin area.
- **Applicant** (existing): The role-specific record tied to users who hold the `Applicant` role. Carries the applicant-only legal id and any existing applicant fields. This spec persists / preserves the applicant record when an Admin creates an Applicant user, and preserves the existing record when an Applicant is later demoted (history).
- **Sentinel admin** (new conceptual entity, not a separate persistence type): A specific user record (`admin@FundingPlatform.com`) marked with the system flag. Distinguished from all other users by its immutability and its exclusion from listings. Has no separate storage shape — it is an instance of User with the system flag set.
- **Role**: One of `Applicant`, `Reviewer`, `Admin`. Each user holds exactly one role at any time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user with only the `Admin` role can, from the UI alone (no database access, no developer involvement), perform the full lifecycle on another user: create with role and initial password → edit profile/email/role → reset password → disable → enable. Each step completes without error and visibly updates the listing.
- **SC-002**: The sentinel admin (`admin@FundingPlatform.com`) does not appear in any admin-area listing, search result, or edit page, regardless of filter input, search string, or constructed URL.
- **SC-003**: Every direct attempt to modify the sentinel admin (constructed URL targeting the sentinel's user id, form submission targeting the sentinel, or direct service call) is rejected at the service layer with a clear error response and produces no partial mutation. This is verifiable by an automated test that constructs each modification request shape and asserts rejection plus zero state change.
- **SC-004**: Disabling, demoting, or otherwise mutating the last active non-sentinel Admin into a state with zero active non-sentinel Admins is rejected with a specific named-rule error message. The sentinel remains the sole recovery path beyond that point.
- **SC-005**: A user whose email or role was just changed by an Admin is forced to re-authenticate on their next request (existing session does not pick up the new claims silently). A user whose password was just reset by an Admin is forced to change their password on next login.
- **SC-006**: Admin role authorization passes every existing `[Authorize(Roles="Reviewer")]` gate in the codebase (specs 002, 004, 006, 007) without those gates' code being modified. Verifiable by logging in as a user with only the `Admin` role and walking every reviewer-gated route, asserting HTTP 200 on each.
- **SC-007**: A non-Admin user (Reviewer, Applicant) receives 403 Forbidden when attempting to access any `/Admin/...` route. An unauthenticated user is redirected to the login page.
- **SC-008**: On first deploy with no `ADMIN_DEFAULT_PASSWORD` configured, the sentinel exists in the user store with the system flag set, and the generated password appears exactly once in the application log at WARN level. On subsequent restarts no password is regenerated and no password is logged.
- **SC-009**: Adding a future reporting feature requires no changes to authorization configuration on `/Admin/Reports` — the route already gates on the `Admin` role and is reachable from the role-aware sidebar.

## Assumptions

- The platform is in pre-production with no real applicants or disbursements depending on it; introducing a new schema attribute on the user entity and a sentinel seed step on first deploy is acceptable.
- The existing demo seed users (`applicant@demo.com`, `reviewer@demo.com`, `admin@demo.com` per `IdentityConfiguration.cs`) continue to exist as regular users; only `admin@FundingPlatform.com` carries the sentinel flag.
- The application logging stack honors WARN-level emission and routes WARN messages to a destination that is at least observable to the deploying operator on first startup. The exact destination is left to the plan/operations.
- The `ADMIN_DEFAULT_PASSWORD` configuration key shape and dev-vs-prod conventions are settled at planning time; this spec only constrains the resolution order (configured secret first; otherwise generate-and-log).
- "Active sessions" of a target user can be invalidated by the user store / Identity infrastructure on demand (e.g., by bumping a security stamp or equivalent). Specific mechanism is a planning concern; this spec only constrains the observable behavior.
- The audit log of admin actions is intentionally out of scope for v1 and deferred to a future compliance/reporting spec. The brainstorm document captures this as a known risk.
- Email-based invite or password-reset flows are intentionally out of scope for v1 (no SMTP infrastructure in stack).
- Out-of-product sentinel password recovery (e.g., re-deploying with a configured `ADMIN_DEFAULT_PASSWORD` to override, or manual user-store intervention) is documented at planning time but is not implemented as a product feature.
- Localization of admin-area copy is intentionally deferred to the future spec 011 (localization layer); v1 ships English copy only.
