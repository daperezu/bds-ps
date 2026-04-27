# Brainstorm: Admin Role and Admin Area

**Date:** 2026-04-25
**Status:** spec-created
**Spec:** specs/009-admin-area/

## Problem Framing

Until now the platform recognized only two roles, `Applicant` and `Reviewer`, and had no in-product user management whatsoever. Every onboarding, role change, password reset, or account fix required a developer running SQL or modifying seed code. There was also a long-known gap (flagged in brainstorms #06 and #08) that ops had no supported in-product way to recover from administrative situations — the absence of an "admin tooling spec" was an explicit out-of-scope thread on multiple prior features. Spec 008 (Tabler UI migration) made the visual surface ready for a real admin area; this brainstorm closes the gap by introducing the third role, the area itself, and an immutable sentinel default-admin user that exists as the system's recovery root of trust.

The seed was framed as a "Context 0" foundation: produce one bundled spec that establishes the role, the area, the sentinel, the query/service-layer exclusions, and a placeholder for future reporting access. Future iterations (audit log, reports content, email-based flows, multi-role) deliberately defer.

## Approaches Considered

### A: One bundled spec — role + area + sentinel + reports stub (chosen)
- Pros: All elements are tightly coupled — sentinel only matters if user management exists; user management's only purpose in v1 is access management; reports stub locks in the access contract for a future module. Single spec means single MVP, single test surface, single review.
- Cons: Larger surface than typical for one spec (29 FRs, 6 user stories). Risk of a single review missing something.

### B: Two specs — backend foundation, then UI
- Spec A: Admin RBAC + sentinel + query exclusions (data + auth foundation).
- Spec B: Admin area UI for user management.
- Pros: Smaller scopes, each independently testable.
- Cons: Spec A would have nothing user-visible to E2E-test except the sentinel and the role inheritance. The constitution principle III (E2E NON-NEGOTIABLE) gets awkward without the UI half. Decomposition cost outweighs benefit.

### C: Three specs — RBAC + sentinel, user management UI, reports placeholder
- Pros: Maximum independence.
- Cons: Reports placeholder is a few-hour change; isolating it as its own spec is overhead theater. The trio also breaks the "Context 0" framing the seed asked for.

## Decision

Chose **Approach A** (one bundled spec). Spec lives at `specs/009-admin-area/` with the following key shape decisions encoded:

- Three roles, **single role per user**.
- **Admin inherits all Reviewer powers** — every existing `[Authorize(Roles="Reviewer")]` gate accepts Admin without those gates' code being modified.
- **Any Admin can create any role**, including other Admins. Sentinel's special status is *immutability and hiddenness*, not exclusivity.
- **Admin sets initial passwords directly**; users are forced to change on next login. Same flow for password reset.
- **Disable only — no delete.**
- **Self-disable, self-demote, self-email-change blocked** from the admin area. **Last-non-sentinel-admin disable/demote blocked** at the service layer with named-rule UI errors.
- **No audit log in v1** — deferred to a future compliance/reporting spec; loudest acknowledged risk.
- **Sentinel password resolution**: configured secret (`ADMIN_DEFAULT_PASSWORD` env/Aspire equivalent) if present, otherwise generated cryptographically-strong random, emitted exactly once at WARN log on first startup, never again.
- **Admin-editable profile fields**: first name, last name, phone for all roles; legal id for Applicants only. Performance score not editable from admin area.
- **Email is editable**; UserName tracks; sessions invalidated.
- **Reports surface**: real route + Admin gate + stub empty-state page, no widgets.
- **Admin area uses spec 008's Tabler shell + reusable partials** — no bespoke UI patterns.
- **Sentinel exclusion enforced at BOTH query layer (DB-level filter) AND service layer (write-side guard)** — either alone insufficient.
- **Schema change** for the system flag uses the dacpac SQL Server Database Project (constitution IV).

Spec passed `spex:review-spec` review on first iteration with status SOUND (5/5 completeness, 5/5 clarity, 5/5 implementability, 4.5/5 testability, all 6 constitution principles aligned). Two minor planning-phase guard-rails were noted in `REVIEW-SPEC.md` (sentinel-log-emission ordering; 403-vs-login-redirect wording).

## Open Threads

- Applicant demotion in-flight applications: when an Applicant is demoted, what should the original applicant see for their existing applications? Most likely read-only access (Applicant record preserved), but pin during planning.
- `ADMIN_DEFAULT_PASSWORD` configuration key shape and Aspire/user-secret wiring: precise key path and dev-vs-prod conventions to be settled in the plan.
- Sentinel password rotation procedure (post first-deploy): no in-product rotation in v1; operational path (env-var override on redeploy, manual reset script) needs a runbook in the plan.
- Sentinel-password WARN-log emission ordering: a process crash between user-row commit and log-flush could leave the sentinel password unrecoverable; plan must specify emit-before-commit or equivalent flush guarantee.
- Whether the expanded admin-edited-profile scope (first/last/phone for all roles, legal id for Applicants) is right v1 surface or should narrow back to identity-level only; revisit if data-model expansion proves expensive.
- Whether single-role-by-contract is the right call vs single-role-by-UX with a multi-role-capable data model — the latter is cheaper to relax later if the platform ever grows beyond three roles.
- Future audit log of admin actions (deferred to a future compliance/reporting spec) — when external audit pressure surfaces, this needs to land.
