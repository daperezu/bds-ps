# Review Brief: Admin Role and Admin Area

**Spec:** specs/009-admin-area/spec.md
**Generated:** 2026-04-25

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Introduces a third role (`Admin`) plus a dedicated `/Admin` MVC area that lets non-engineers run the platform's user lifecycle — create with role, edit profile/email/role, reset password, disable / re-enable — without developer involvement. Establishes a permanent immutable sentinel admin (`admin@FundingPlatform.com`) seeded on first deploy, hidden from all listings, and rejected on every modification attempt at both the query and service layers; this is the recovery root of trust if every other Admin gets locked out. Locks in the access-control contract for a future reporting module via an Admin-gated stub page at `/Admin/Reports`. Uses the spec-008 Tabler shell and reusable partials throughout. No new managed dependencies.

## Scope Boundaries

- **In scope:** `/Admin/Users` listing/create/edit/disable/enable/reset-password; sentinel admin seed, hide, and immutability; self-modification + last-non-sentinel-admin guards; Admin-inherits-Reviewer authorization; `/Admin/Reports` stub page; role-aware sidebar entries; Tabler-shell consumption; Playwright E2E coverage.
- **Out of scope:** Audit log of admin actions; email-based invite/reset flows; multi-role-per-user; hard delete; reports content; admin self-service of own profile from the admin area; localization (deferred to spec 011); optimistic concurrency on the user entity; performance-score editing; two-tier admin (sentinel-only privilege tier); out-of-product sentinel-recovery procedure (documented at planning time, not a product feature).
- **Why these boundaries:** The seed asked for a "Context 0" foundation. The bundle ships an MVP-grade admin area without committing the team to features that need infra not yet in stack (SMTP, audit/compliance), behaviour the data layer cannot yet support cheaply (multi-role), or capabilities that belong in adjacent specs (reports, localization).

## Critical Decisions

### Any admin can create any role, including other admins
- **Choice:** Admins are not tiered — any active Admin may create users in any role (Applicant, Reviewer, or Admin).
- **Trade-off:** Operationally simple; weakens the "sentinel = only path to admin" trust model. Sentinel's special status is *immutability and hiddenness*, not exclusivity.
- **Feedback:** If review wants the sentinel to be the only path that mints admins, this needs to flip back.

### Disable-only — no delete
- **Choice:** Users can be disabled and re-enabled but never hard-deleted; soft-delete is also out.
- **Trade-off:** Preserves audit and traceability across applications, reviews, signatures. Disabled users will accumulate over time.
- **Feedback:** Acceptable trade-off given audit-first posture; pressure-test if disabled-user churn becomes operational noise.

### No audit log of admin actions in v1
- **Choice:** Deferred to a future compliance/reporting spec.
- **Trade-off:** Faster v1; early ops actions are unobservable in-product (no who/when/what trail for create/disable/reset/role-change).
- **Feedback:** This is the loudest known risk in the spec. If the platform is going to be touched by external auditors anytime soon, this should ship in v1 instead of v2.

### Sentinel password delivery: configured-secret-or-bootstrap-log
- **Choice:** If `ADMIN_DEFAULT_PASSWORD` (env/Aspire/user-secret) is present, use it; otherwise generate cryptographically-strong random password, emit at WARN exactly once on first startup, never again.
- **Trade-off:** Zero-config dev path; secret-store-friendly prod path. Failure mode: a process crash between user-row commit and log flush leaves the sentinel password unrecoverable (flagged in REVIEW-SPEC.md as planning-phase guard-rail).
- **Feedback:** Confirm the bootstrap-log lands in a destination the operator can actually capture on first deploy.

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Profile editing scope expanded beyond "access management"
- **Decision:** Admin can edit first name, last name, phone for *all* roles; legal id for Applicants; performance score never. This requires extending the user entity with name/phone fields that don't exist on Reviewer/Admin today.
- **Why this might be controversial:** A more conservative spec would limit admin to email + role + enabled state and let users self-edit profile fields. The expansion adds data-model work to v1 that purely-access-management would not require.
- **Alternative view:** Stick to identity-level fields only; defer profile editing to a future spec or to user self-service.
- **Seeking input on:** Is the expanded surface worth the data-model expansion now, or should we narrow to identity-level only and ship sooner?

### Single role per user vs. multi-role
- **Decision:** Each user holds exactly one role.
- **Why this might be controversial:** ASP.NET Identity natively supports many-to-many; locking single-role at the spec level is a contract that will be expensive to relax later (every consumer of "the role" must change to "the active role" or "a role").
- **Alternative view:** Keep the data model multi-role (Identity default) but enforce single-role at the admin-UI level for v1. Cheaper to relax later.
- **Seeking input on:** Is single-role-by-contract or single-role-by-UX the right call?

### Sentinel hidden from *every* query, not just admin listings
- **Decision:** Layered exclusion at the query layer means the sentinel never appears in any user enumeration anywhere in the codebase, not just in `/Admin/Users`.
- **Why this might be controversial:** Could surprise a future feature that expects to enumerate "all users" (e.g., a future reports module counting active accounts). Such features will see N-1 instead of N.
- **Alternative view:** Hide only from the admin-area surfaces and let other queries see the sentinel.
- **Seeking input on:** Confirm the bias toward "always hidden" is the right one; if not, we need an explicit "include-sentinel" opt-in query path.

### `/Admin/Reports` stub page now vs. nothing now
- **Decision:** A real route + page + Admin gate, with empty-state copy.
- **Why this might be controversial:** It's a feature with no value today. Skipping the stub keeps the surface area smaller.
- **Alternative view:** Defer the route entirely until reports actually exist.
- **Seeking input on:** Is locking in the access contract worth the (small) UI surface?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Role | `Admin` | Third Identity role alongside `Applicant`, `Reviewer` |
| Sentinel email | `admin@FundingPlatform.com` | Hard-coded value; the only user with the system flag |
| MVC area | `/Admin` | Hosts `Users` and `Reports` sub-surfaces |
| Sub-surfaces | `/Admin/Users`, `/Admin/Reports` | Two top-level pages in the area |
| Configured-password key | `ADMIN_DEFAULT_PASSWORD` (placeholder) | Precise key path settled at planning time |
| Sentinel system attribute | "system flag" (conceptual) | Concrete column name set at planning time |
| Confirmation dialog | `_ConfirmDialog` | Existing partial from spec 008; reused for Disable / Reset Password |

## Open Questions

- [ ] **Applicant demotion in-flight applications.** When an Applicant is demoted to Reviewer/Admin, what happens to their existing applications? Most likely read-only, but pin during planning.
- [ ] **`ADMIN_DEFAULT_PASSWORD` configuration key shape and Aspire/user-secret wiring.** Settled at planning time.
- [ ] **Sentinel password rotation procedure (post first-deploy).** Documentation-only; out-of-product. Plan should write the runbook.
- [ ] **Last-non-sentinel-admin guard interaction with re-enable.** Re-enabling a previously-Active admin should not be incorrectly blocked; spec is clear (Edge Cases #9), confirm at implementation.

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| No audit log of admin actions in v1 | Med | Deferred explicitly; future compliance/reporting spec; flagged in brainstorm |
| Sentinel-password log-flush ordering can drop the password on a first-startup crash | High (recovery) | Plan must specify emit-before-commit ordering; logged in REVIEW-SPEC.md |
| Last-write-wins on user-record edits | Low | Acceptable v1 trade-off; revisit if conflicts surface |
| Sentinel exclusion at every query layer surprises future consumers | Low–Med | Document the convention; if a future feature needs "all users including sentinel", add an explicit opt-in query path |
| Single-role-by-contract is expensive to relax later | Med | Reviewer call — see Disagreement Areas |
| Profile-field expansion adds data-model work to v1 | Low–Med | Reviewer call — see Disagreement Areas |

---
*Share with reviewers before implementation.*
