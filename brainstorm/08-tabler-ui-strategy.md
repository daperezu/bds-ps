# Brainstorm: Tabler.io UI Strategy and First Migration Spec

**Date:** 2026-04-25
**Status:** spec-created
**Spec:** specs/008-tabler-ui-migration/

## Problem Framing

After seven shipped features (specs 001–007), the platform's UI has drifted: default ASP.NET Bootstrap scaffolding, no sidebar, no consistent page-header pattern, alerts/badges/tables/form layouts that look slightly different depending on which feature introduced them. The seed (`brainstorm/uxui-seed.md`) asked for a strategic UX/UI exploration with eight deliverables (principles, phased migration, Tabler.io integration plan, quick wins, communication UX, design-system, risks, anti-patterns) — substantially broader than the per-feature pattern this repo has used so far. The session shape was decided up front: produce a strategic brainstorm document AND drill into the first executable spec (Tabler shell + reusable partial library + full view sweep) in the same session.

## Strategic Decisions Made

### Migration risk appetite

Three options were considered, in order of conservatism:

- **A: Foundation-only sweep** — first spec swaps shell only; subsequent specs polish individual surfaces.
- **B: Shell + one vertical slice** — first spec does shell plus one fully refactored high-value page.
- **C: Theme everything in one spec** — first spec does shell, library, and a sweep across all surfaces.

**Decision: C, "theme everything in one spec."** This is the highest-risk option but is justified by the platform's pre-production status (no real applicants or disbursements) and the user's preference to maximize visible lift in a single iteration. The session locked in this scope only after confirming pre-prod status — the same call would not have been made in production.

### Component layer architecture

- **A: Extract reusable Razor partials as we go** — small library of `_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`.
- **B: Inline Tabler classes only** — no abstraction.
- **C: Full design-system layer** — component library plus storybook-style docs.

**Decision: A.** The middle ground is YAGNI-respecting: a partial is extracted only when 2+ views need it AND its surface area is small. Every future feature inherits consistency for free, and the seed's "design-system / developer handoff" deliverable falls out naturally without requiring a separate initiative.

### Messaging UX scope

- **A: Out of scope; future spec** — re-skin the existing comment surfaces, no new messaging features.
- **B: Add unified messaging panel in this sweep** — embedded thread, sender avatars, role badges, status-change events interleaved.
- **C: Full messaging subsystem** — inbox, notifications, unread counts, real-time updates.

**Decision: A.** Messaging restructure is its own feature and deserves its own brainstorm. This sweep stays visual-only.

### UI language

- **A: Stay in English** — defer localization to a future spec.
- **B: Translate UI to Spanish in this sweep** — locks us into Spanish until a real i18n layer exists.
- **C: Build full i18n layer in this sweep** — too big.

**Decision: A.** The component library is built to receive display strings from view models / partial parameters rather than embedding copy, so a future i18n layer slots in cleanly.

## UX/UI Principles (captured for future reference)

1. **Status is the spine.** Every screen for a given application must answer "where is this in the lifecycle?" before anything else. One canonical visual mapping per domain enum, used everywhere the entity appears. *Why:* Trust on a funding platform is built by never letting an applicant or reviewer feel lost about state. *Spec mechanism:* `_StatusPill` is the sole badge renderer; centralized enum-to-(color+icon) mapping (FR-005, FR-006, SC-002).
2. **Reviewers scan; applicants are guided.** Reviewer surfaces optimize for density and predictable hotkeys/actions. Applicant surfaces optimize for one clear next action and inline help. *Spec mechanism:* `_DataTable` density flag with role-sensible default.
3. **Documents are first-class objects, not links.** *Spec mechanism:* `_DocumentCard` is the sole document renderer (FR-008, SC-003).
4. **Every action has a reversibility class, and the UI tells you which.** Destructive, state-advancing, and state-locking actions get distinct treatments; state-locking actions confirm with a one-line "this cannot be undone" rationale. *Spec mechanism:* `_ActionBar` action classes (FR-009) + `_ConfirmDialog` mandate (FR-010).
5. **Evidence trails over toasts.** Durable timeline entries on entity detail pages, not just transient toasts. *Spec mechanism:* `_EventTimeline` partial.
6. **Empty states are wayfinding, not apology.** *Spec mechanism:* `_EmptyState` partial; bare alert-style "no data" messages prohibited (FR-011, SC-004).

## Phased Plan (forward-looking)

This brainstorm produced **one spec immediately** and identifies the next four likely specs in the strategic queue:

- **Spec 008 (THIS spec, created):** Tabler.io shell + reusable partial library + full view sweep.
- **Spec 009 (future):** Communication surface — unified messaging panel for reviewer↔applicant interactions, structured thread on application detail page, status-change events interleaved. View-layer only, no new persistence.
- **Spec 010 (future):** Notifications & inbox — notification bell, unread counts, signing inbox enrichment with badges. Likely SignalR.
- **Spec 011 (future):** Localization layer — ASP.NET resource files, culture middleware, Spanish translations, language toggle.
- **Spec 012 (future):** Admin/configuration surface polish — once the sweep lands, admin pages typically need their own pass.

## Tabler.io Integration Approach

- **Edition:** Open-source Tabler (MIT). Component coverage (sidebar, cards, page headers, forms, tables, badges, modals, empty states) maps cleanly to every existing surface. Tabler Pro not required.
- **Bring-in mechanism:** Vendored under `wwwroot/lib/tabler/`, no CDN. Tabler is a Bootstrap 5 superset, so existing Bootstrap markup keeps working during the in-spec rewrite — no flag-day cutover needed within the spec.
- **Icons:** Tabler Icons bundle, with one canonical icon per lifecycle state.
- **Custom CSS:** `wwwroot/css/site.css` is the only place project-specific overrides live. No inline `style=` attributes (lint-style discipline, enforced via grep — FR-017, SC-006).

## Risks & Anti-Patterns Captured

- **Regression surface across 7 shipped features.** Mitigated by pre-prod status, manual smoke mandate (FR-018), and the constitution-required Playwright E2E suite continuing to pass (FR-019). One new sidebar-visibility test added (FR-020).
- **Premature partial abstraction.** Rule: a partial gets extracted only when 2+ views need it AND its surface is small.
- **Status pill drift.** Mitigated by `_StatusPill` accepting the *enum value*, so the mapping is centralized.
- **PDF document accidentally themed.** `Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml` are PDF-target with self-contained CSS — explicit out-of-scope, source files MUST remain byte-identical, generated PDFs MUST remain visually identical (SC-007).
- **Admin-template misuse.** Resist Tabler's dashboard demos (charts, KPI tiles); every component used must serve an existing workflow need.
- **Rewrite-while-themeing.** Do NOT also fix unrelated bugs, refactor view models, or restructure controllers during the sweep. Visual-only.

## Decision

A strategic brainstorm document captured the principles, phased plan, integration approach, and risks; the first executable feature was created as `specs/008-tabler-ui-migration/` covering Tabler shell + reusable partial library + full view sweep. The spec went through one inline review iteration (resolved E2E alignment with constitution Principle III, PDF identity wording, FR-009 action-class enumeration, plus minor improvements) and is marked SOUND. Ready for `/speckit-plan`.

## Open Threads

- Specific Tabler.io version pin — deferred to planning (latest stable at planning time).
- Sidebar default-open vs. default-collapsed on first load — deferred to planning.
- Whether the absolute "no badges outside `_StatusPill`" rule should permit non-status badges (e.g., quantity counters) — to be revisited if the planning phase surfaces concrete cases.
- Whether to invest in visual-regression tooling (Playwright screenshot comparison or Percy) before the sweep, or leave manual side-by-side as the v1 visual gate — flagged in `review_brief.md` as an area of potential disagreement.
- Whether `_ConfirmDialog` for every destructive action (including draft-item deletes) is the right baseline, or whether specific exceptions should be enumerated — flagged in `review_brief.md`.
- Future spec 009 (communication surface) needs its own brainstorm before any implementation; the current Appeal/ApplicantResponse comment surfaces are only being re-skinned in spec 008.
- Future spec 011 (localization) was deliberately deferred — when it lands, partials will need to be checked to ensure no UI copy was embedded in them during the sweep.
