# Review Brief: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Spec:** specs/008-tabler-ui-migration/spec.md
**Generated:** 2026-04-25

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

The platform's UI today uses default ASP.NET Bootstrap scaffolding, and across the seven shipped features visual treatment has drifted (alerts, badges, tables, and form layouts each look slightly different depending on which feature shipped them). This feature replaces the layout shell with a Tabler.io-based shell, extracts a small reusable Razor partial library (`_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`), and rewrites every existing view to consume it. No business logic, controller, view-model, persistence, or authorization changes. The PDF document target (`Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml`) is explicitly excluded and must remain byte-identical.

## Scope Boundaries

- **In scope:** Tabler.io shell adoption, reusable partial library, full re-skin of every view under `Views/{Account, Admin, ApplicantResponse, Application, FundingAgreement, Home, Item, Quotation, Review}/` (with the two PDF target exceptions), centralized status-enum-to-(color+icon) mapping, role-aware sidebar mirroring existing role checks, one new Playwright test for sidebar visibility, all existing E2E tests keep passing.
- **Out of scope:** Spanish/localized copy, dark mode, communication restructure beyond re-skinning, notification bell/inbox/unread counts, dashboard or chart pages, any controller/view-model/persistence/authorization change, modification of the PDF target files, project favicon refresh.
- **Why these boundaries:** The brainstorm explicitly chose a "theme everything in one spec" sweep enabled by the platform's pre-production status. Bounding hard around layer (view-only) and around the PDF target keeps a large blast-radius change controllable.

## Critical Decisions

### Theme commitment
- **Choice:** Tabler.io open-source build, MIT-licensed, vendored locally under `wwwroot/lib/tabler/`. No CDN.
- **Trade-off:** Pinning a specific theme couples our visual identity to its update cadence and conventions; gain is a complete component vocabulary and a Bootstrap-5 foundation that lets the in-spec rewrite be incremental rather than flag-day.
- **Feedback:** Any objection to the open-source edition vs. paid (Tabler Pro)? Spec assumes open-source is sufficient.

### Single-spec sweep vs. phased rollout
- **Choice:** Theme everything in one spec, decomposed internally into three priority-ordered user stories (P1 shell, P2 high-traffic surfaces with library, P3 remaining surfaces).
- **Trade-off:** Maximum visible lift in one feature, but largest single regression surface across all 7 already-shipped features. Mitigated by pre-prod status (no real users) and the mandate that all existing Playwright E2E tests keep passing.
- **Feedback:** Comfortable with the all-at-once approach given pre-prod status?

### Reusable partial library
- **Choice:** Extract a small library of Razor partials in this same spec (rather than re-skin first and extract later).
- **Trade-off:** Slightly larger spec, but every future feature inherits consistency for free; the design-system / developer-handoff outcome falls out naturally.
- **Feedback:** Are the nine partial names the right shape, or do you want to add/remove any (e.g., `_DangerZone`, `_FormGrid`)?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### "Theme everything in one spec" vs. foundation-only
- **Decision:** Sweep all views in one spec (FR-015), instead of shipping the shell first and re-skinning incrementally.
- **Why this might be controversial:** This is the highest-risk option among the three considered during brainstorming. Anyone with production experience instinctively prefers smaller increments.
- **Alternative view:** A foundation-only spec (Tabler shell + base CSS only) followed by per-feature re-skin specs would be safer and easier to bisect.
- **Seeking input on:** Does the pre-prod status genuinely justify the broader scope, or should we revisit the per-spec pattern that shipped specs 001–007?

### Centralized status mapping registry
- **Decision:** `_StatusPill` is the **only** allowed badge renderer in the entire view tree (FR-006, SC-002, SC-006); a grep across `Views/` MUST return zero badges outside it.
- **Why this might be controversial:** Hard "only allowed" rules are easy to violate accidentally and create review friction.
- **Alternative view:** Allow ad-hoc badges for non-status indicators (e.g., quantity counters), and only centralize the status-derived ones.
- **Seeking input on:** Should the rule allow badge use for non-status indicators, or stay absolute?

### `_ConfirmDialog` for every state-locking and destructive action
- **Decision:** FR-010 mandates a confirmation modal for every state-locking action (e.g., signing) and every destructive action (e.g., deleting a draft item), with a one-line irreversibility rationale.
- **Why this might be controversial:** Confirmation modals fatigue users; some destructive actions in admin contexts may not warrant them.
- **Alternative view:** Confirmation only for irreversible cross-applicant actions (signing, approving), with destructive draft actions using inline undo or no confirmation.
- **Seeking input on:** Is the "always confirm" rule the right baseline, or should we permit specific exceptions?

### Manual smoke as supplementary check, not primary gate
- **Decision:** FR-019 makes existing Playwright E2E tests the primary automated quality gate, with FR-018 manual smoke as a supplementary visual-regression check.
- **Why this might be controversial:** Some teams prefer manual smoke as the primary gate for visual changes because automated tests can pass on broken layouts (CSS regressions don't fail DOM assertions).
- **Alternative view:** Add visual-regression tooling (Playwright screenshot comparison or Percy) and make that the primary gate alongside DOM-assertion tests.
- **Seeking input on:** Is the manual side-by-side acceptable for v1, or should we invest in screenshot-based regression in this spec?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Spec branch | `008-tabler-ui-migration` | Sequential successor to 007-signing-wayfinding |
| Theme | Tabler.io (open-source) | MIT-licensed, vendored under `wwwroot/lib/tabler/` |
| Partial location | `Views/Shared/Components/` | Standard Razor shared-views convention |
| Page header partial | `_PageHeader` | Title, subtitle, breadcrumb slot, primary-actions slot |
| Status badge partial | `_StatusPill` | Centralized; sole badge renderer in `Views/` |
| Table partial | `_DataTable` | Wraps Tabler `card-table`; inline empty-state |
| Form section partial | `_FormSection` | Preserves `asp-validation-for` tag helpers |
| Empty-state partial | `_EmptyState` | Replaces all "no data" alerts |
| Action grouping partial | `_ActionBar` | Classes: primary / secondary / destructive / state-locking |
| File-reference partial | `_DocumentCard` | Sole document renderer; replaces bare anchor links |
| Audit-event partial | `_EventTimeline` | Reads from existing audit data |
| Confirmation partial | `_ConfirmDialog` | Required for state-locking and destructive actions |
| Action classes | primary, secondary, destructive, state-locking | Each with enumerated visual treatment |

## Open Questions

- [ ] Specific Tabler.io version pin — deferred to planning (latest stable at planning time)
- [ ] Sidebar default-open vs. default-collapsed on first load — deferred to planning

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Regression across 7 already-shipped features | High | Pre-prod status + all existing Playwright E2E tests must keep passing (FR-019) + manual smoke of all three role golden paths (FR-018) |
| Accidental modification of PDF target files | High | Explicit exclusion in FR-015; SC-007 requires `git diff` returns empty + visual side-by-side of generated PDF |
| Status pill drift returning post-spec | Medium | `_StatusPill` is the sole badge renderer; SC-006 verifiable via grep |
| Premature partial abstraction | Medium | Explicit "extract only when 2+ views need it" rule in Assumptions |
| Tabler theme update cadence imposes future churn | Low–Medium | Vendored locally (no auto-update); updates are deliberate spec-bounded actions |
| Confirmation-modal fatigue | Low–Medium | Per-class enumerated visual treatment so "advance" actions don't trigger confirmations |

---
*Share with reviewers before implementation.*
