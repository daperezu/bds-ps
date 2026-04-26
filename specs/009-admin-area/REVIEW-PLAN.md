# Review Guide: Admin Role and Admin Area

**Spec:** [spec.md](spec.md) | **Plan:** [plan.md](plan.md) | **Tasks:** [tasks.md](tasks.md)
**Generated:** 2026-04-25

---

## What This Spec Does

The platform has had only Applicant and Reviewer roles since spec 001, with **no in-product way for non-engineers to create users, assign roles, disable accounts, or reset passwords** — every account fix today requires a developer running SQL. This spec introduces a third role (Admin) and an `/Admin` MVC area with a user-management surface, a permanent immutable "sentinel" admin (`admin@FundingPlatform.com`) seeded on first deploy as a recovery root of trust, and a stub `/Admin/Reports` page that locks in the access-control contract for a future reporting module.

**In scope:** the Admin role; `/Admin/Users` (list, create, edit, disable/enable, reset password); the sentinel admin (seed + hide + immutability); self-modification and last-non-sentinel-admin guards; Admin-inherits-all-Reviewer-powers via a claims transformation; an access-gated stub Reports page; Tabler-shell consumption and role-aware sidebar entries; full Playwright E2E coverage per [FR-026](spec.md#functional-requirements).

**Out of scope:** an audit log of admin actions ([deferred](spec.md#out-of-scope) — the loudest known risk); email-based invite or password-reset flows (no SMTP in stack); multi-role per user; hard delete (disable-only); reports content; admin self-service of own profile from the admin area; localization (deferred to spec 011); optimistic concurrency on the user entity; performance-score editing; two-tier admin (sentinel-only privilege tier); out-of-product sentinel-recovery procedure (documented at planning time, not a product feature).

## Bigger Picture

This spec sits at a structural turning point: it's the first feature that **creates the foundation rather than building a workflow**. Specs 001–007 added user-visible workflows (apply, review, sign). Spec 008 unified the visual layer. Spec 009 finally gives ops the tools to run the platform end-to-end without engineering. Two specific known gaps from prior brainstorms close here: the "admin tooling spec" thread from [#06](../../brainstorm/06-digital-signatures.md) and the "future spec 012 (admin/configuration surface polish)" thread from [#08](../../brainstorm/08-tabler-ui-strategy.md).

The two extension hooks for the future:
- **The Reports stub** ([FR-024](spec.md#functional-requirements)) is positional. It locks `[Authorize(Roles="Admin")]` on `/Admin/Reports` so a future reporting spec slots in without re-debating authorization.
- **The audit-log deferral** ([Out of Scope](spec.md#out-of-scope), [Risks](review_brief.md#risk-areas)) is the load-bearing follow-up. When external compliance pressure surfaces, that spec lands quickly atop these foundations.

ASP.NET Identity's role authorization model uses exact-string matching against role claims, so role hierarchy (Admin "is a" Reviewer) is non-trivial to express. This spec's [Decision 3 in research.md](research.md#decision-3-reviewer-implies-admin-authorization-mechanism) — using `IClaimsTransformation` to inject a Reviewer claim onto Admin principals at request time — is the canonical low-friction approach. The alternative (replacing every `[Authorize(Roles="Reviewer")]` with a custom policy) was rejected because [FR-002](spec.md#functional-requirements) explicitly forbids modifying existing reviewer-gated attribute code in specs 002, 004, 006, 007.

---

## Spec Review Guide (30 minutes)

### Understanding the approach (8 min)

Read [spec.md User Stories 1–3](spec.md#user-scenarios--testing-mandatory) and [data-model.md ApplicationUser section](data-model.md#applicationuser-new). As you read, consider:

- Does the bundling of "user lifecycle + sentinel + guards" into one spec match how you'd ship this work? US1 alone delivers the operational unblock, but US2 + US3 are what make it safe to ship; the brainstorm picked one bundled spec over splitting. Worth your judgment.
- Is the sentinel email being **hardcoded** to `admin@FundingPlatform.com` ([FR-017](spec.md#functional-requirements)) acceptable given that the project name "Programa Semilla" / "Funding Platform" could shift if rebranded?
- The seed wording "Admin can do EVERYTHING a Reviewer can do" was interpreted as a strict superset for authorization purposes, not for *all* role-tagged behavior elsewhere. Is that interpretation right? See [FR-002](spec.md#functional-requirements) and the meta-test in [T061](tasks.md#user-story-4-tests).

### Key decisions that need your eyes (12 min)

**Single-role-per-user is a contract, not just a UI choice** ([FR-001](spec.md#functional-requirements), [research.md Decision 1](research.md#decision-1-user-entity-shape))

ASP.NET Identity natively supports many-to-many roles. The spec locks single-role at the data layer, with the implementation enforcing "remove all old roles, add new one" inside [`UserAdministrationService.UpdateUserAsync`](contracts/README.md#2-iuseradministrationservice-application-layer). The alternative (multi-role data model with single-role-only UI enforcement) is cheaper to relax later if the platform ever needs e.g. a "Reviewer-who-is-also-Auditor" role overlap.
- Question for reviewer: Is the simplicity of single-role-by-contract worth the cost of relaxing it later?

**Reviewer-implies-Admin via runtime claim injection** ([FR-002](spec.md#functional-requirements), [research.md Decision 3](research.md#decision-3-reviewer-implies-admin-authorization-mechanism))

The `AdminImpliesReviewerClaimsTransformation` runs once per authenticated request and adds a `Reviewer` role claim onto every Admin principal. This means **every existing `[Authorize(Roles="Reviewer")]` in specs 002, 004, 006, 007** gains Admin acceptance silently — no attribute edits, no policy rewrites.
- Question for reviewer: Are there any reviewer-gated routes where you'd actually want to **exclude** Admins (say, "this metric should only count Reviewer-role activity")? If yes, the claims-transformation approach makes that future feature awkward.

**Sentinel is hidden everywhere by default, not just in `/Admin/Users`** ([FR-019, FR-027](spec.md#functional-requirements), [research.md Decision 2](research.md#decision-2-sentinel-exclusion-mechanism))

The EF Core global query filter on `ApplicationUser` makes the sentinel invisible to every default user enumeration in the codebase, present and future. A future "count active accounts" reporting query would see N-1 unless it explicitly opts in via `IgnoreQueryFilters()`.
- Question for reviewer: Is "always hidden, opt-in to see" the right bias, or should the sentinel only be hidden in admin-area listings?

**Sentinel password log-before-create ordering** ([research.md Decision 7](research.md#decision-7-sentinel-password-emission-ordering-review-specmd-guard-rail-1), [REVIEW-SPEC.md guard-rail #1](REVIEW-SPEC.md))

The seeder logs the password at WARN level **before** persisting the sentinel user. This inverts the obvious order to prevent a process crash between row commit and log flush from making the sentinel unrecoverable. Trade-off: in the rare crash-after-log-before-commit case, the operator gets a logged password that wasn't used.
- Question for reviewer: Is that trade-off (rare wasted log line vs. unrecoverable sentinel) the right call? Could a synchronous flush after `CreateAsync` be safer in a different way?

**`AccessDeniedPath` change is a global behavior shift** ([research.md Decision 8](research.md#decision-8-403-access-denied-response-review-specmd-guard-rail-2), [T011](tasks.md#identity-wiring-programcs))

To make [SC-007](spec.md#measurable-outcomes) (non-Admin → 403 on `/Admin/...`) observable, the cookie config's `AccessDeniedPath` flips from `/Account/Login` to `/Account/AccessDenied`. This affects **every** authorize attribute in the codebase, not just the new admin-area routes. Existing E2E tests might implicitly depend on the redirect-to-login behavior.
- Question for reviewer: Is changing this globally correct, or should the 403 only apply to `/Admin/...` (would require attribute-level overrides)?

### Areas where I'm less certain (5 min)

- **The `MustChangePassword` middleware's static-asset detection** ([T035](tasks.md#implementation-for-user-story-1)). The plan describes detection via path prefix (`/lib`) plus extension match. This works but could be either too aggressive (skipping a legitimate non-static path that happens to match) or too lenient (forcing a change-password redirect on a `/api/...` request that returns JSON). The implementation may need a more curated allow-list.
- **`UpdateUserAsync` self-modification guard ordering vs. Identity's email change** ([T056](tasks.md#implementation-for-user-story-3)). The guard fires before any Identity call, which is correct. But Identity's `SetUserNameAsync` + `UpdateNormalizedEmailAsync` is a two-step operation; a partial failure in step 2 could leave the user in an inconsistent state. The spec says no optimistic concurrency in v1 ([FR-029](spec.md#functional-requirements)), so retry logic isn't included. Is that right?
- **The cascade rename `IdentityUser` → `ApplicationUser`** ([T012, T015, T016](tasks.md#cascade-rename-identityuser--applicationuser)). I'm confident the rename is type-only and observable through reflection only, but [T017](tasks.md#cascade-rename-identityuser--applicationuser) is the gate that confirms no specs 002–008 test relies on the old type. If a test does break, the fix is mechanical but the surprise factor is real.
- **Whether the admin demotion of an Applicant should preserve UI access** ([Edge Case in spec.md](spec.md#edge-cases), [data-model.md Applicant rules](data-model.md#applicant-entity-existing--relationship-rules-clarified-not-modified)). Spec preserves the `Applicant` row but doesn't say what the (now-demoted) user sees when they try to view their own applications. I assumed read-only access via existing applicant-controller logic; this could be wrong.

### Risks and open questions (5 min)

- If an external auditor asks for "who created this user, when, and why" — the audit-log deferral ([Out of Scope](spec.md#out-of-scope), [review_brief.md risks](review_brief.md#risk-areas)) means there is no answer beyond ASP.NET Identity's built-in lockout/sign-in event log. Is that an acceptable v1 posture given likely audit timelines?
- The 60-second `SecurityStampValidationInterval` ([research.md Decision 4](research.md#decision-4-session-invalidation-mechanism)) means a just-disabled user can still serve up to 60 seconds of authenticated requests. Is "≤60s" close enough to spec wording "immediately" ([FR-009, FR-010, FR-011, FR-012](spec.md#functional-requirements))?
- The sentinel-recovery procedure ([quickstart.md §2c](quickstart.md#2c-recovering-from-a-missed-sentinel-password)) requires direct SQL access plus a temporary code change to the seeder. If the sentinel password is ever lost in production, who is the human who has both? Worth pinning before go-live.
- The plan handles the `Admin:DefaultPassword` configuration key shape ([research.md Decision 6](research.md#decision-6-admin_default_password-configuration-key-shape)) but does not specify how Aspire's resource graph wires it for the AppHost. If the deploying operator never sets the key, the auto-generated WARN log line is the only signal — assumes the operator is watching the log on first boot.
- The `IClaimsTransformation` approach to Reviewer inheritance ([Decision 3](research.md#decision-3-reviewer-implies-admin-authorization-mechanism)) means Admin users will appear in `User.IsInRole("Reviewer")` checks throughout the codebase. Existing code that **distinguishes** Reviewer from Admin (e.g., the spec-008 sidebar's "Reviewer-only" entry list) needs to check `IsInRole("Admin")` first to disambiguate. [Spec 008's sidebar partial already follows this order](review_brief.md#critical-decisions); confirm during implementation.

---
*Full context in linked [spec](spec.md) and [plan](plan.md).*
