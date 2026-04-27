# Feature Specification: Tabler.io UX Shell, Reusable Component Layer, and Full View Sweep

**Feature Branch**: `008-tabler-ui-migration`
**Created**: 2026-04-25
**Status**: Draft
**Input**: User description: "Migrate the platform from default ASP.NET Bootstrap scaffolding to a Tabler.io-based UI system, extract a small reusable Razor partial library, and re-skin every existing view to consume it. Establish a single, consistent visual and interaction language across applicant, reviewer, and admin surfaces. No business logic, controller, view-model, persistence, or authorization changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Themed shell so every existing screen looks like one product (Priority: P1)

Today the platform uses default ASP.NET Bootstrap scaffolding: a thin top navbar, default typography, no sidebar, no consistent page-header pattern. Across the seven shipped features the visual treatment has drifted — alerts, badges, tables, and form layouts all look slightly different depending on which feature shipped them. This story replaces the layout shell so every screen across every role lives inside one coherent chrome (collapsible left sidebar with role-aware navigation, topbar with brand and user identity, breadcrumb slot, page-header slot, content area). All existing views continue to render inside the new shell without any markup changes — they inherit consistent typography, spacing, button styling, table styling, alert styling, and form styling from the new theme via CSS cascade. After this story alone, anyone navigating the system feels they are using one coherent product instead of seven loosely related pages.

**Why this priority**: This is the smallest change that delivers the largest perception-of-consistency lift. It establishes the foundation for every subsequent visual decision and is the minimum viable migration outcome. Without it, no other story has anywhere to render.

**Independent Test**: Replace the layout, deploy, log in as each of applicant / reviewer / admin, and walk every menu entry. Every existing page must render inside the new shell without layout breakage and without JavaScript console errors. No view-side markup needs to change for this test to pass.

**Acceptance Scenarios**:

1. **Given** the user is logged in as an Applicant, **When** they visit any page reachable from the sidebar, **Then** the page renders inside the new shell with sidebar, topbar, and content area, and only sidebar entries appropriate to the Applicant role are visible.
2. **Given** the user is logged in as a Reviewer, **When** they visit any page reachable from the sidebar, **Then** the Reviewer-only entries (Review queue, Signing inbox) are visible alongside the shared entries; Admin-only entries are not.
3. **Given** the user is logged in as an Admin, **When** they visit any page reachable from the sidebar, **Then** Admin-only entries are visible.
4. **Given** an unauthenticated visitor, **When** they reach the login or register page, **Then** those pages render inside an appropriate authentication shell (no sidebar) consistent with the themed system.
5. **Given** any role, **When** the user resizes the viewport from desktop (1280×800) to mobile (360×740), **Then** the sidebar collapses per the theme's default behavior and the page remains usable.
6. **Given** any page, **When** the controller sets `TempData["SuccessMessage"]` or `TempData["ErrorMessage"]`, **Then** the message renders as a dismissible alert in the shell using the new theme styling.

---

### User Story 2 — Reusable component library, with high-traffic surfaces refactored first (Priority: P2)

Each shipped feature reinvented its own status badge, page header, action bar, and document reference. With only the shell themed (P1), this drift remains in the markup. This story extracts a small library of reusable view components (`_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`) and refactors the highest-traffic surfaces to consume them: the applicant's application list and detail pages, the reviewer's review queue and review detail page, and the signing inbox. After this story, the surfaces that drive the daily workflow speak one consistent visual language for status, headers, actions, and documents — and any future feature can adopt the library to inherit consistency for free.

**Why this priority**: Reusable components are where the long-term consistency benefit lives, but they only matter once the most-used surfaces consume them. Building the library and applying it to high-traffic surfaces in the same story keeps the work concrete and verifiable; lower-traffic surfaces (P3) can follow without losing momentum.

**Independent Test**: Render the high-traffic surfaces (Application Index, Application Details, Review Index, Review Detail, Signing Inbox) and verify each uses the library partials (visible by inspecting markup or rendered structure) and that every status pill on those surfaces uses the centralized enum-to-(color+icon) mapping. Walk a reviewer through review → approve → generate → sign and confirm consistent action-bar placement and consistent document-reference appearance.

**Acceptance Scenarios**:

1. **Given** any application state in the system, **When** the application is rendered on any high-traffic surface, **Then** its status appears as a pill rendered by `_StatusPill` with the canonical color and icon for that state, identical across surfaces.
2. **Given** an application detail page, **When** it lists generated documents (funding agreement, signed agreement, quotations), **Then** each document is rendered by `_DocumentCard` with file icon, name, size, and signer/timestamp metadata where applicable.
3. **Given** an application detail page, **When** the user is presented with primary actions (e.g., Submit, Approve, Generate Agreement, Sign), **Then** those actions appear in an `_ActionBar` with consistent placement and visually distinct treatment for primary, secondary, destructive, and state-locking action classes.
4. **Given** a state-locking action (e.g., signing the funding agreement), **When** the user clicks it, **Then** `_ConfirmDialog` opens with a one-line rationale that the action cannot be undone, and the action only proceeds on explicit confirmation.
5. **Given** any high-traffic data table (review queue, application list), **When** rendered, **Then** it uses `_DataTable` with consistent header treatment, hover behavior, and an inline empty-state via `_EmptyState` when no rows exist.
6. **Given** any form on a high-traffic surface, **When** validation fails, **Then** errors render through `_FormSection`'s feedback slot and the existing `asp-validation-for` tag helpers continue to drive the message text.

---

### User Story 3 — Remaining views swept; visual-consistency invariants enforced (Priority: P3)

Lower-traffic surfaces — Account (Login, Register), Admin (Configuration, ImpactTemplates, CreateTemplate, EditTemplate), Item (Add, Edit, Impact), Quotation (Add), and ApplicantResponse (Index, Appeal) — still contain feature-specific markup after P2. This story re-skins them to consume the same library partials and removes any remaining inline styling, ad-hoc badge markup, and bare anchor-tag document links. After this story, the entire view tree (excluding the PDF document target) speaks one consistent visual language and the codebase is ready to enforce invariants via simple greps in code review.

**Why this priority**: These surfaces matter but are visited far less often per day than the P2 set, so they can land in a follow-on increment without holding back the bigger value. Bundling them as a single story preserves the "one spec, one sweep" intent.

**Independent Test**: Walk each remaining surface as the relevant role. Confirm each uses the library partials where applicable. Run a grep across `Views/` and confirm no badge markup exists outside `_StatusPill`, no document references exist outside `_DocumentCard`, and no inline `style=` attributes remain. Run `git diff` against `Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml` and confirm both are unchanged; generate a funding agreement PDF and visually compare it side-by-side to a pre-spec generated PDF.

**Acceptance Scenarios**:

1. **Given** every view under `Views/{Account, Admin, ApplicantResponse, Item, Quotation}/`, **When** rendered, **Then** each view uses `_PageHeader`, `_FormSection`, `_DataTable`, `_EmptyState`, `_ActionBar`, `_StatusPill`, and `_DocumentCard` wherever applicable.
2. **Given** the entire `Views/` tree (excluding the PDF document target), **When** searched for badge markup outside `_StatusPill`, **Then** no occurrences are found.
3. **Given** the entire `Views/` tree (excluding the PDF document target), **When** searched for inline `style=` attributes, **Then** no occurrences are found.
4. **Given** the entire `Views/` tree (excluding the PDF document target), **When** searched for document anchor tags or file-icon markup outside `_DocumentCard`, **Then** no occurrences are found.
5. **Given** the existing Funding Agreement PDF generation flow, **When** a reviewer generates a PDF after the sweep, **Then** `Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml` source files are byte-identical to their pre-spec contents (`git diff` returns empty) and the generated PDF is visually identical (side-by-side comparison) to one generated before the sweep.

---

### Edge Cases

- Sidebar collapse on viewports narrower than the theme's breakpoint follows the theme's default behavior; no custom collapse logic is introduced.
- Tables with many columns wrap or scroll horizontally inside the theme's responsive-table wrapper; no column hiding is introduced.
- Long page-header titles truncate with ellipsis rather than push the action-bar slot off-screen.
- A `_ConfirmDialog` opened over a dropdown follows the theme's default modal stacking; no custom z-index handling is added.
- Login and Register pages render in a different shell variant (no sidebar) since the user is not yet authenticated and has no role-aware navigation to show.
- Validation messages whose existing copy is long enough to wrap render as multi-line feedback within `_FormSection` without breaking field alignment.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST render every authenticated page inside a shared shell that includes a collapsible left sidebar with role-aware navigation, a topbar with brand and current-user identity, a breadcrumb slot, and a page-header slot.
- **FR-002**: System MUST render unauthenticated pages (login, register) inside an authentication-shell variant of the same theme that omits the sidebar.
- **FR-003**: System MUST replace the previous Bootstrap CSS and JavaScript bundles with the new theme's CSS and JavaScript bundles, served locally from the application's static-assets path with no external CDN dependency.
- **FR-004**: System MUST make a single icon set available to all views and standardize the icon used to represent each lifecycle state across the platform.
- **FR-005**: System MUST provide a centralized status-rendering component, `_StatusPill`, that accepts a domain status value (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`) and renders a badge using the canonical color and icon for that value.
- **FR-006**: System MUST define and document a single canonical mapping of each domain status enum value to a (color, icon) pair, and `_StatusPill` MUST be the only place in the view tree where badges are produced.
- **FR-007**: System MUST provide a reusable component library under `Views/Shared/Components/` containing at minimum: `_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`.
- **FR-008**: `_DocumentCard` MUST be the only component used to render references to uploaded or generated files (funding agreement PDFs, signed agreement PDFs, quotation attachments) across the view tree.
- **FR-009**: `_ActionBar` MUST classify each action it renders into one of: primary, secondary, destructive, or state-locking, and MUST apply distinct color treatment per class such that two adjacent actions of different classes are unambiguously distinguishable at a glance — at minimum: primary uses the theme's solid accent color, secondary uses an outlined neutral treatment, destructive uses the theme's danger color, and state-locking uses the theme's warning color paired with a lock icon.
- **FR-010**: `_ConfirmDialog` MUST be invoked for every state-locking action (e.g., final signature acceptance) and every destructive action (e.g., deleting a draft item), and MUST display a one-line rationale describing the irreversibility before allowing the action to proceed.
- **FR-011**: `_EmptyState` MUST render whenever a data set is empty in any list or table surface; bare alert-style "no data" messages MUST NOT appear in the view tree.
- **FR-012**: `_FormSection` MUST present validation feedback through the new theme's input-validation conventions while preserving compatibility with the existing `asp-validation-for` tag helpers, so view models and controllers require no changes to drive validation messages.
- **FR-013**: System MUST present role-aware sidebar navigation that hides entries the current user is not authorized to use, mirroring the existing role checks (no new authorization logic introduced).
- **FR-014**: System MUST display server-emitted feedback (`TempData["SuccessMessage"]` and `TempData["ErrorMessage"]`) as dismissible alerts in the shell using the new theme styling.
- **FR-015**: System MUST rewrite every view file under `Views/{Account, Admin, ApplicantResponse, Application, FundingAgreement, Home, Item, Quotation, Review}/` to render inside the new shell and to consume the appropriate library components, with the explicit exceptions of `Views/FundingAgreement/Document.cshtml` and `Views/FundingAgreement/_FundingAgreementLayout.cshtml`. The two exception files MUST remain byte-identical to their pre-spec contents (verifiable with `git diff`) because they are the print-target for PDF rendering, and the PDFs they produce after the sweep MUST be visually identical to PDFs produced before the sweep (verifiable by manual side-by-side comparison of a generated funding agreement).
- **FR-016**: View rewrites MUST preserve every existing controller binding, view-model property name, form action target, route value, and form post target verbatim; no controller, view-model, validation, persistence, or authorization change is introduced by this feature.
- **FR-017**: The view tree MUST contain no inline `style=` attributes after the sweep; all styling derives from the theme tokens or from `wwwroot/css/site.css` overrides.
- **FR-018**: Acceptance of this feature MUST require a manual smoke test that exercises each role's golden path end-to-end without console errors and without layout breakage: applicant (create application → add item → add quotations → submit), reviewer (review queue → review application → approve → generate agreement → sign), admin (open admin → configure templates). The manual smoke test is a supplementary visual-regression check, not a substitute for automated tests.
- **FR-019**: All existing Playwright end-to-end tests covering features 001 through 007 MUST continue to pass after the sweep, as the primary automated quality gate. Test code may be updated only to track DOM-selector changes caused by the partial library; assertions on user-visible behavior MUST NOT be relaxed.
- **FR-020**: A new Playwright end-to-end test MUST be added that asserts role-aware sidebar visibility, since this is the only genuinely new user-facing behavior introduced by the feature: an Applicant sees only Applicant-permitted entries; a Reviewer sees Reviewer entries (Review queue, Signing inbox) plus shared entries; an Admin sees Admin entries; an unauthenticated visitor on the login or register page sees no sidebar.

### Key Entities

- **Status mapping registry**: The single source of truth for translating each domain status enum value (`ApplicationState`, `ItemReviewStatus`, `AppealStatus`, `SignedUploadStatus`) into the visual pair (color, icon) shown to users. Owned and consumed exclusively by `_StatusPill`. Has no persistence — it is a presentation-layer constant.
- **Reusable view component library**: The set of Razor partials under `Views/Shared/Components/` that encode the canonical visual treatment for page headers, status pills, data tables, form sections, empty states, action bars, document references, event timelines, and confirmation dialogs. The library is the contract through which feature views inherit consistency, and its surface area is intentionally small (extract only when 2+ views need a partial).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every authenticated page across every role renders inside the shared shell with the role-correct sidebar entries and no JavaScript console errors at desktop (1280×800) and mobile (360×740) viewports in Chromium and Firefox.
- **SC-002**: Every status indicator displayed anywhere in the application that derives from `ApplicationState`, `ItemReviewStatus`, `AppealStatus`, or `SignedUploadStatus` uses the canonical color and icon pair from the centralized mapping; no two surfaces show the same state with different visual treatment.
- **SC-003**: Every reference to an uploaded or generated file in the view tree is rendered as a structured document card with file icon, filename, and metadata; no bare anchor-tag file links remain.
- **SC-004**: Every list or table surface that can be empty displays a structured empty-state explaining the situation and offering the next action; no "no data" alert-style messages remain.
- **SC-005**: All existing Playwright E2E tests for features 001 through 007 pass after the sweep, the new role-aware sidebar visibility test passes, and a supplementary manual smoke run of all three role golden paths (applicant create-to-submit, reviewer review-to-sign, admin configure-templates) completes without console errors and without layout breakage.
- **SC-006**: A grep across the view tree returns zero badge markup outside the `_StatusPill` component, zero document-link markup outside the `_DocumentCard` component, and zero inline `style=` attributes (excluding the explicitly out-of-scope PDF target files).
- **SC-007**: `Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml` source files are byte-identical to their pre-spec contents (`git diff` returns empty), and a funding agreement PDF generated after the sweep is visually identical (side-by-side comparison) to one generated before the sweep.
- **SC-008**: Adding a new view in a future feature requires no styling decisions beyond choosing which library components to compose; visual consistency is achieved by composition rather than by per-view styling effort.

## Assumptions

- The platform is in pre-production with no real applicants or disbursements depending on it; a single-spec sweep that touches every existing view is therefore an acceptable risk profile, and any regressions can be caught in a manual smoke test before merge.
- UI copy remains in English for this feature; localization (Spanish copy, culture switching, resource files) will be addressed in a separate future feature and therefore the component library is built to receive its display strings from view models and partial parameters rather than embedding copy in the components themselves.
- Communication and messaging UX restructure (unified messaging panel, notifications, inbox enrichment) is out of scope for this feature; the existing Appeal and ApplicantResponse comment surfaces are re-skinned only and not restructured.
- The new theme is a Bootstrap 5 superset, so the existing Bootstrap-class markup in views continues to render acceptably during the in-spec rewrite — no flag-day cutover is needed within the spec.
- The funding agreement PDF target (`Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml`) has its own self-contained print CSS and is independent of the application shell; it MUST remain untouched and its existing rendering tests MUST continue to pass unchanged.
- jQuery and the unobtrusive validation pipeline are retained because the new theme is compatible with them and removing them is out of scope.
- No new domain entities, persistence schemas, or controller endpoints are introduced; the entire feature lives in the view layer plus static assets.
- The reusable component library is grown with discipline: a partial is extracted only when at least two distinct views need it and its surface area is small enough that consumers do not fight the abstraction.
- The chosen UI theme (Tabler.io, open-source build) is distributed under the MIT license, which is acceptable for vendored use in this project.

## Dependencies

- Open-source build of the chosen UI theme (CSS and JavaScript bundles) and its companion icon set, vendored into the application's static-assets path.
- Existing Bootstrap 5 markup conventions in the current view tree, for the duration of the in-spec rewrite.
- Existing role configuration in ASP.NET Identity (Applicant, Reviewer, Admin), consumed read-only by the role-aware sidebar.
- Existing controllers, view models, validation attributes, and persistence — consumed read-only and not modified.

## Out of Scope

- Spanish or any other localized UI copy, and any internationalization infrastructure (resource files, culture middleware, language toggle).
- Communication and messaging restructure beyond re-skinning the existing comment surfaces; specifically, unified messaging panels, notification bells, unread counts, and real-time updates are deferred to future features.
- Dark-mode toggle, even though the chosen theme supports it.
- New dashboard pages, charts, KPI tiles, or other surfaces that do not already exist in the view tree.
- Any change to controller logic, view-model shape, validation rules, persistence model, authorization rules, or business behavior.
- Modification of the funding agreement PDF target files (`Views/FundingAgreement/Document.cshtml` and `_FundingAgreementLayout.cshtml`).
- Project favicon refresh — explicitly out of scope; tracked as a future quick-win and not bundled with this sweep.

## Open Questions

- The specific theme version pin will be chosen during planning, based on what is the latest stable release at that time.
- Whether the sidebar should default to expanded or collapsed on first load is deferred to planning, where it can be evaluated against the most common viewport in development screenshots.
