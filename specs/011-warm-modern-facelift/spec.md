# Feature Specification: Warm-Modern Facelift

**Feature Branch**: `011-warm-modern-facelift`
**Created**: 2026-04-27
**Status**: Draft
**Input**: User description: "Brand identity, design tokens, motion system, full UI sweep, and four signature 'wow moments' for the authenticated experience — elevating the platform from the spec-008 Tabler.io foundation to a warm-modern premium finish appropriate for a funding agency serving entrepreneurs."

## Overview

After spec 008 (Tabler.io migration) the platform has visual consistency and a reusable partial library — a solid foundation but a flat identity. This feature elevates the entire authenticated experience to a "warm-modern premium" finish appropriate for a funding agency serving entrepreneurs (Mercury / Ramp / Notion family). The lift consists of:

1. A new brand identity proposed in this spec (display name, color story, typography pairing, logo direction, voice guide).
2. A refreshed design-token layer applied through the spec-008 partial library so every existing view inherits the new look automatically.
3. A full sweep across every authenticated view so the tokens land cleanly (spacing, hierarchy, illustrated empty states, density rebalanced) — **HTML restructuring is permitted and encouraged where it serves UX/UI quality**.
4. Four net-new signature moments: Applicant home dashboard, Application journey timeline, Signing ceremony, Reviewer queue dashboard.
5. A choreographed-but-tasteful motion system that respects `prefers-reduced-motion`.
6. A 9-scene empty-state illustration set as a brand carrier.

**Carry-overs from spec 008**: PDF templates remain byte-identical; localization remains deferred; partials remain the only place visual decisions live; all assets vendored under `wwwroot/lib/` (no CDN).

**Three baked-in scope calls**: (a) Login / Register / ChangePassword / AccessDenied are included in the sweep but are not signature moments; (b) Dark mode is OUT for v1 (tokens designed to be theme-able later); (c) Display brand only — code, namespaces, project names, and configuration keys remain `FundingPlatform`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Applicant Home Dashboard (Priority: P1)

As an entrepreneur logged in to the platform, I want to land on a home page that immediately tells me where I am in my funding journey and what I need to do next, so that I feel oriented and supported instead of confronted with a list.

**Why this priority**: First-touch surface for the platform's primary audience. Today's home is an empty-icon placeholder — the dashboard is the most visible "wow" lift and the surface where applicant trust is earned daily.

**Independent Test**: Log in as an applicant with one or more applications, land on `/`, observe the dashboard with hero / KPI strip / awaiting-action callout / application card list / activity feed / resources strip; click the awaiting-action CTA and confirm correct navigation. Separately, log in as an applicant with zero applications and confirm the empty-state welcome scene.

**Acceptance Scenarios**:

1. **Given** an applicant with two active applications, **When** they log in and land on the home page, **Then** the welcome hero shows the first name, the KPI strip shows accurate counts (Active / In review / Awaiting your action / Funded), and a card per active application appears with embedded mini journey timeline + days-in-state + last activity + a contextual primary action.
2. **Given** an applicant with zero applications, **When** they land on the home page, **Then** a full-bleed welcome scene appears with the seedling illustration, a single primary CTA "Start a new application", and a three-card trust strip (How long it takes / What you'll need / How decisions are made).
3. **Given** an applicant whose Funding Agreement is ready to sign, **When** they land on the home page, **Then** the awaiting-action callout appears prominently with a single-sentence message naming the application and a primary CTA that navigates to the signing surface.
4. **Given** any applicant, **When** the dashboard mounts, **Then** the KPI counts animate from zero to their final value over the documented motion duration, and (under reduced-motion) the counts render their final values immediately with no animation.
5. **Given** an applicant viewing the dashboard, **When** they scroll the recent activity feed, **Then** every event is keyboard-focusable and clicking an event deep-links to the surface where it occurred.

---

### User Story 2 — Application Journey Timeline (Priority: P1)

As any user (applicant or reviewer) viewing an application, I want a visual representation of where the application is in its lifecycle, so that "status is the spine" is a felt experience and not just a status pill.

**Why this priority**: Universal visualization of the lifecycle that spec 008 declared as the platform's spine. Embedded in five surfaces (applicant detail, reviewer detail, signing surfaces, dashboard cards, queue rows). Highest reuse and longest-lived component.

**Independent Test**: Open an application detail page in each of the seven mainline stages and the three branch states; visually confirm the correct current stage emphasis, completed-stage check marks, pending-stage muted state, and (for branched applications) the warning / danger / info sub-track. Hover a completed node and confirm tooltip with timestamp and actor. Click a completed node and confirm scroll-to / highlight on the matching event log entry.

**Acceptance Scenarios**:

1. **Given** an application currently in "Under Review", **When** any user views the application detail page, **Then** the journey timeline renders nodes for Draft, Submitted, Under Review, Decision, Agreement Generated, Signed, Funded; Draft and Submitted display as completed (filled, checkmarked); Under Review displays as the current stage (larger node, primary fill, subtle glow halo, heading-sm label); the rest display as pending (small outlined low-opacity).
2. **Given** an application that was sent back to the applicant once and is now in "Submitted" again, **When** the timeline renders, **Then** a warning-tokened "Sent back" sub-branch is visible at the Decision node, returning to the Submitted mainline node, and the mainline progression continues correctly.
3. **Given** a rejected application with no Appeal opened, **When** the timeline renders, **Then** a danger-tokened terminal "Rejected" sub-branch is visible at the Decision node, and no further mainline nodes render as reachable.
4. **Given** a rejected application with an Appeal opened, **When** the timeline renders, **Then** an info-tokened "Appeal" sub-branch is visible; if the appeal resolved as Approved, the branch reconnects to mainline and progression continues; if upheld, the branch terminates with a danger token.
5. **Given** a completed Submitted node, **When** a user hovers / focuses on it, **Then** a tooltip appears showing the submission timestamp and the actor's name; **When** the user clicks the node, **Then** the page scrolls to (or highlights) the matching event in the page's event timeline.
6. **Given** an applicant viewing their dashboard, **When** the dashboard renders an application card, **Then** the card embeds the mini variant of the timeline (dots + connector + current-stage label only). **Given** a reviewer viewing the queue, **When** a row renders, **Then** the row embeds the micro variant (dots only, current dot enlarged, no labels, no tooltips).
7. **Given** any timeline rendering, **When** the user has reduced-motion enabled, **Then** completed nodes render in their final state immediately with no stagger and no spring.

---

### User Story 3 — Signing Ceremony Moment (Priority: P1)

As an applicant or funder finishing a Funding Agreement signature, I want the success state to feel like a peak moment of the funding journey, not a flash message.

**Why this priority**: Emotional peak of the funding journey; the surface where the platform either feels human or feels procedural. Replaces today's flash-message-and-redirect with a take-over view that earns the entrepreneur's enthusiasm.

**Independent Test**: Trigger each of the four signing variants (applicant-only, funder-only, both-signed, applicant-after-funder) and confirm the correct hero copy, the correct presence/absence of confetti, the funding summary card with animated number ticker, and the calendar-anchored "what happens next" card. Bookmark the ceremony URL after a fresh sign and re-visit it directly; confirm the non-animated summary state renders without re-firing the celebration.

**Acceptance Scenarios**:

1. **Given** an applicant who has just completed the final required signature on a Funding Agreement (both parties now signed), **When** the Sign action's success path runs, **Then** the user lands on the ceremony view: hero seal animation + Fraunces hero "Your funding is locked in." + soft confetti burst + funding summary card (with animated number ticker on the funded amount) + "What happens next" card naming the disbursement date + action footer with primary "View funding details" and secondary "Back to dashboard".
2. **Given** an applicant who signs before the funder, **When** the ceremony renders, **Then** the hero copy is "You're signed." with subhead "We're waiting on the funder. We'll email you when it's complete.", and confetti is NOT mounted.
3. **Given** a funder who signs while the applicant has not yet signed, **When** the ceremony renders, **Then** the hero copy is "Funder signature recorded." with subhead "The applicant has been notified.", and confetti is NOT mounted (dignified for the funder UX).
4. **Given** any signed user, **When** they later bookmark the ceremony URL and revisit directly, **Then** the page renders the non-animated summary state — funding summary card and "what happens next" card present, but no hero seal animation, no confetti, no number ticker animation.
5. **Given** the ceremony view mounts, **When** the user has reduced-motion enabled, **Then** confetti is not mounted, the static seal asset is substituted, the number ticker renders the final amount immediately, and focus moves to the primary action button.
6. **Given** the ceremony view mounts, **When** any user views it, **Then** an aria-live="polite" region announces the signing event for screen readers, the confetti canvas (if present) is aria-hidden, and pressing ESC navigates to the dashboard.

---

### User Story 4 — Reviewer Queue Dashboard (Priority: P1)

As a reviewer, I want my Review Index to be a heads-up display I look forward to opening, while preserving the density I need for triage.

**Why this priority**: Reviewer surface gets equal lift across roles per the brainstorm decision. Density-forward design ensures power-user efficiency is preserved while warm-modern tokens give the queue a heads-up display feel.

**Independent Test**: Log in as a reviewer with a populated queue; observe welcome strip, KPI strip with all four counts, filter chip strip, recent activity feed (max 5 with show-more), and the queue table with inline micro journey timeline per row. Click each filter chip and confirm the table reflows without page reload. Change the spec-010 `AgingThresholdDays` config value and confirm the Aging KPI updates. Separately, log in as a reviewer with zero assigned items and confirm the empty-state.

**Acceptance Scenarios**:

1. **Given** a reviewer with assigned items in mixed states, **When** they land on the queue dashboard, **Then** the layout renders: welcome strip + KPI strip (Awaiting your review / In progress / Aging > N days / Decided this month) + filter chip strip (All / Awaiting me / Aging / Sent back / Appealing) + recent activity feed (max 5 events) above the queue table.
2. **Given** the queue table renders, **When** any reviewer views it, **Then** each row shows: app number + project name, applicant name + avatar, an inline micro variant of the journey timeline (replacing a status-pill column), days in current state, last activity (relative time), and a contextual primary action button.
3. **Given** the filter chip strip, **When** the reviewer clicks "Aging", **Then** the table reflows to show only items past the configured threshold, the chip styles as selected (subtle primary background + primary text), and no full page reload occurs.
4. **Given** the spec-010 `AgingThresholdDays` configuration is changed, **When** the dashboard re-renders, **Then** the Aging KPI count and the "Aging" filter result both reflect the new threshold (single source of truth).
5. **Given** a reviewer with zero assigned items, **When** they land on the queue dashboard, **Then** an empty-state appears with the calm-horizon illustration and the message "All clear — nothing's awaiting your review.".
6. **Given** any queue row, **When** the reviewer hovers it, **Then** the row lifts to a higher shadow level; **When** the reviewer clicks anywhere on the row, **Then** they navigate to the Review details for that application.
7. **Given** the dashboard mounts, **When** the reviewer has reduced-motion enabled, **Then** KPI tickers render their final values immediately and chip-filter row reflows render without animation.

---

### User Story 5 — Brand Identity, Design Tokens & Motion System (Priority: P1, Foundational)

As a designer / engineer, I want a single design-token layer and motion system in place so the new visual identity propagates through the spec-008 partial library to every existing view automatically.

**Why this priority**: Foundational — US1, US2, US3, US4, US6, and US7 all consume these tokens. Without this story, none of the others land coherently. Sequencing-wise, this story is the prerequisite for the rest.

**Independent Test**: Create `wwwroot/css/tokens.css` with the documented categories of CSS custom properties; update `site.css` to reference only tokens; re-template the eight spec-008 partials to consume tokens only. Greppable verification: zero raw hex outside `tokens.css` and PDF carve-outs; zero inline `style=` attributes in non-PDF views; zero hard-coded animation durations outside `tokens.css`. Independently, install Fraunces, Inter, and JetBrains Mono under `wwwroot/lib/fonts/` and confirm display heading uses Fraunces, body uses Inter, monospace uses JetBrains Mono on a re-tokened page. Independently, render any motion in the catalog with reduced-motion enabled and confirm the documented degradation.

**Acceptance Scenarios**:

1. **Given** the spec is implemented, **When** a developer greps the codebase for raw hex (`/#[0-9a-fA-F]{3,8}/`) outside `wwwroot/css/tokens.css` and the PDF carve-out files, **Then** zero results are returned.
2. **Given** the spec is implemented, **When** a developer greps the codebase for inline `style=` attributes across `Views/**/*.cshtml` (excluding PDF carve-outs), **Then** zero results are returned.
3. **Given** the spec is implemented, **When** a developer greps for hard-coded animation durations (e.g., `transition: ... 200ms`) outside `tokens.css`, **Then** zero results are returned.
4. **Given** the eight spec-008 partials (`_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`), **When** they render, **Then** they reference colors, spacing, radii, shadows, type, and motion exclusively through `var(--…)` tokens.
5. **Given** any page renders, **When** a user inspects it, **Then** display-level headings use Fraunces, body copy uses Inter, monospace contexts (application numbers, IDs, code-like values) use JetBrains Mono — all served from `wwwroot/lib/fonts/`, no Google Fonts CDN.
6. **Given** any motion in the documented motion catalog (hover, focus, button press, modal open, drawer slide, popover, skeleton swap, toast, page route fade, status pill change, number ticker, journey timeline progression, signing ceremony hero, empty-state entrance), **When** the user has `prefers-reduced-motion` enabled, **Then** durations clamp to 0ms (with the opacity-transition exemption preserved at the fast token), the ticker renders the final value, the ceremony confetti is replaced with the static seal, the journey stagger is suppressed, and the empty-state entrance is suppressed.
7. **Given** the brand voice guide is published as `BRAND-VOICE.md`, **When** any user-facing string in a swept view is reviewed, **Then** it complies: no ALL CAPS shouting, no exclamation marks (except in the signing ceremony), no "submit" CTAs, no passive voice in microcopy, second person addressing the user, first person plural for the platform.

---

### User Story 6 — Full Authenticated-View Sweep (Priority: P1)

As a user navigating any page in the authenticated app, I see a coherent warm-modern experience — every view inherits the new tokens, every empty state shows an illustration, every copy string is voice-guide compliant.

**Why this priority**: Without the sweep, the new tokens land but old surfaces visually contradict them. The sweep is mechanical labor that delivers consistency at scale, not creative work, but it's the difference between "almost there" and "facelift complete".

**Independent Test**: After the sweep completes, walk every view in the documented inventory using `SWEEP-CHECKLIST.md` and verify against the seven uniform criteria below. Independently, confirm that PDF generation produces a byte-identical (or pixel-identical) output to a stored reference, proving the carve-outs were honored.

**Acceptance Scenarios**:

1. **Given** every view in the inventory (Account / Home / Application / Item / Quotation / Supplier / Review / ApplicantResponse / FundingAgreement non-PDF / Admin / Shared layout), **When** a developer applies the SWEEP-CHECKLIST.md uniformly, **Then** each view passes all seven criteria: zero raw hex/px outside tokens; zero inline style=; correct partial usage (`_StatusPill` / `_EmptyState` / `_ActionBar` / `_ConfirmDialog`); voice-guide compliant copy; correct typography roles; semantically restructured HTML where it improves UX; semantic locators present.
2. **Given** the spec is implemented, **When** a developer regenerates the Funding Agreement PDF for a reference application, **Then** the resulting PDF is visually identical to a stored reference PDF — `Document.cshtml` and `_FundingAgreementLayout.cshtml` source files are byte-identical to their pre-spec state.
3. **Given** any view requiring an empty state, **When** the empty state renders, **Then** it uses the `_EmptyState` partial with one of the nine illustrations from the set (US7), an illustration `aria-label` (or `aria-hidden` when title is sufficient), a voice-guide-compliant title and subtitle, and a single-CTA action.
4. **Given** Login / Register / ChangePassword / AccessDenied surfaces, **When** any anonymous user encounters them, **Then** they render with the re-templated `_AuthLayout`, the new tokens, and clean focused single-CTA tone (not a marketing hero) — no decorative illustrations, no off-brand styling.
5. **Given** Page Object Model classes touched by the sweep, **When** a developer reviews them, **Then** they expose semantic actions over raw locators (e.g., `dashboard.AwaitingActionCount` rather than `page.Locator(".kpi-card.awaiting .count")`), use ARIA roles + accessible names where possible, and use `data-testid` attributes only where role/name are insufficient.
6. **Given** the sweep is complete, **When** the Playwright suite runs, **Then** every previously-passing test passes again; net-new wow-moment test files for US1 / US2 / US3 / US4 pass; at least one test runs with the Playwright reduced-motion option and verifies the documented degradations; any deleted test has explicit justification in the PR description.

---

### User Story 7 — 9-Scene Empty-State Illustration Set (Priority: P1)

As a user encountering any empty state in the platform, I see an illustration that orients and warms the moment instead of an icon-only placeholder.

**Why this priority**: Illustrations are the brand's most visible carrier of warmth across the product. Required by US1 (applicant home empty), US4 (reviewer queue empty), and US6 (every other empty state), so it ships in v1.

**Independent Test**: Confirm nine SVG files live under `wwwroot/lib/illustrations/`, each conforming to the style discipline (palette only, ≤ 4 colors per scene, ≤ 8 KB gz, scales 120-280 px). Confirm the `_EmptyState` partial accepts the new `illustration` parameter and renders the correct asset for each scene-key. Confirm the entrance animation respects reduced-motion.

**Acceptance Scenarios**:

1. **Given** the spec is implemented, **When** a developer inspects `wwwroot/lib/illustrations/`, **Then** nine SVG files are present, one per scene: seed/sprout (applicant home empty); folders-stack (Application/Item index empty); open-envelope (Quotation index empty); connected-nodes (Supplier index empty); calm-horizon (Reviewer queue empty); soft-bar-chart (Admin report empty); off-center-compass (404); gentle-disconnected-wires (500); magnifier-on-empty (search-no-results).
2. **Given** any empty state surface, **When** the `_EmptyState` partial is rendered with an `illustration` parameter, **Then** the correct SVG renders inside the partial with the documented entrance motion, and (under reduced-motion) renders in its final state without animation.
3. **Given** any illustration SVG, **When** a developer audits it, **Then** it uses only colors from the brand palette (`--color-primary`, `--color-accent`, `--color-bg-surface-raised`, neutrals), uses no more than four colors per scene, weighs 8 KB gzipped or less, and renders cleanly at both 120 px and 280 px square targets.

---

### Edge Cases

- **Zero applications + new applicant**: dashboard collapses to the welcome scene with a single CTA (covered in US1.AS2).
- **Many applications (> 3)**: dashboard shows the first three application cards with a "show all" link to the full Applications index (planning decides if "show all" navigates or expands inline).
- **Application with both an appeal AND a sent-back loop in its history**: timeline renders both branch indicators; visual contract pinned during planning.
- **Reviewer with all assigned items in one filter (e.g., all aging)**: queue dashboard renders correctly; active chip is "All" by default but clicking "Aging" yields the same set.
- **Bookmark to a ceremony URL after the agreement state has changed (e.g., agreement was regenerated)**: ceremony page renders the latest non-animated summary state; if the user is no longer authorized to view the agreement, the standard authorization redirect applies.
- **Applicant role demoted while viewing the dashboard**: on next render, the layout switches to the appropriate role's landing surface; no in-flight action is corrupted.
- **Voice-guide compliance for system-generated copy (validation messages, default model labels)**: planning audits framework-generated strings and overrides where the voice guide demands.
- **Animation triggered by data refresh (e.g., a status change while the dashboard is open)**: re-render triggers the spring-fill on the newly-completed journey node; subject to reduced-motion suppression.
- **PDF carve-out drift**: if any non-PDF change accidentally edits `Document.cshtml` or `_FundingAgreementLayout.cshtml`, the PDF identity acceptance criterion fails.

## Requirements *(mandatory)*

### Functional Requirements

**Brand identity (US5)**

- **FR-001**: Spec MUST propose a display brand identity consisting of: a display name (working candidates: *Forge*, *Ascent*, or keep `FundingPlatform` — final selection deferred to user sign-off per FR-072); a color story (warm forest green primary, warm amber accent, warm off-white surfaces, warm mid-grays for text, warm near-black headings, warm-retuned status palette); a typography pairing (Fraunces display serif + Inter body sans + JetBrains Mono); a logo direction (wordmark in Fraunces + abstract mark — rising-line or open-arc concept); and a voice guide (`BRAND-VOICE.md` deliverable).
- **FR-002**: Project namespaces, source code identifiers, database identifiers, and configuration keys MUST remain `FundingPlatform`. The display brand applies only to user-visible UI surfaces, asset filenames, and copy.
- **FR-003**: All new fonts (Fraunces, Inter, JetBrains Mono) MUST be self-hosted under `wwwroot/lib/fonts/`. No Google Fonts CDN. Font files MUST be subsetted to keep the asset budget per FR-074.
- **FR-004**: A `BRAND-VOICE.md` deliverable MUST be published in the spec directory documenting tone, person, stage-aware copy patterns, and banned constructs (ALL CAPS shouting, exclamation marks except the signing ceremony, "submit" CTAs, passive voice in microcopy).
- **FR-005**: A logo direction MUST be expressed as deliverable assets: SVG wordmark, SVG abstract mark, and a favicon set. Production of the actual SVG content is a designer deliverable; the spec defines the constraints.

**Design tokens (US5)**

- **FR-006**: A `wwwroot/css/tokens.css` file MUST exist and declare all CSS custom properties for color (surface, text, brand, status, border), spacing (8 px base, T-shirt scale), radii, shadows, typography (family + scale tokens), motion (durations + eases), and z-index.
- **FR-007**: `wwwroot/css/site.css` and every Razor partial under `Views/Shared/` MUST consume color, spacing, radii, shadows, type, and motion exclusively through `var(--…)` tokens.
- **FR-008**: Tabler's CSS custom-property bridge (e.g., `--tblr-primary`) MUST be overridden where mapping is clean. The bridge inventory is pinned during planning.
- **FR-009**: Raw hex color literals MUST NOT appear outside `wwwroot/css/tokens.css` and the PDF carve-out files. Verified by a greppable lint rule that fails the build (or the SC-001 acceptance gate) on violation.
- **FR-010**: Inline `style=` attributes MUST NOT appear in any view under `Views/**/*.cshtml` outside the PDF carve-out files (extends spec 008 FR-017).
- **FR-011**: The eight spec-008 partials (`_PageHeader`, `_StatusPill`, `_DataTable`, `_FormSection`, `_EmptyState`, `_ActionBar`, `_DocumentCard`, `_EventTimeline`, `_ConfirmDialog`) MUST be re-templated to consume tokens only. The `_StatusPill` enum-to-color mapping MUST switch from raw color references to `--color-*-subtle` token references so future palette changes propagate.

**Motion system (US5)**

- **FR-012**: Motion duration tokens MUST exist: `--motion-instant` (50 ms), `--motion-fast` (150 ms), `--motion-base` (250 ms), `--motion-slow` (400 ms), `--motion-celebratory` (700 ms). Easing tokens MUST exist: `--ease-out` (default), `--ease-in-out`, `--ease-spring` (tuned cubic-bezier with tiny overshoot), `--ease-decelerate`.
- **FR-013**: Motion MUST be limited to a documented catalog. New motion outside the catalog requires spec amendment. Catalog: hover/focus, button press, modal/drawer/popover, skeleton swap, toast, page route, status pill change, number ticker, journey timeline progression, signing ceremony hero, empty-state entrance.
- **FR-014**: Hard-coded animation durations (e.g., `transition: ... 200ms` literals) MUST NOT appear outside `tokens.css`. All motion MUST use `--motion-*` tokens.
- **FR-015**: A `prefers-reduced-motion` contract MUST be implemented in `tokens.css`. Under reduced-motion: durations clamp to 0 ms (opacity transitions exempted, preserved at `--motion-fast`); the number ticker renders the final value; the ceremony confetti is replaced with a static seal asset; the journey timeline stagger is suppressed; empty-state entrance animations are suppressed.
- **FR-016**: No general-purpose animation library (GSAP, Motion One, Framer-Motion-equivalent, etc.) MAY be added. The single carve-out is a confetti library for the signing ceremony, ≤ 5 KB gzipped (e.g., `canvas-confetti`).

**Sweep mechanics (US6)**

- **FR-017**: Every view in the sweep inventory MUST satisfy the seven uniform "swept" criteria: (1) no raw hex/px outside tokens; (2) no inline `style=`; (3) status displays use `_StatusPill`, empty states use `_EmptyState` with a US7 illustration, action groups use `_ActionBar`, destructive actions use `_ConfirmDialog`; (4) voice-guide compliant copy; (5) page heading uses `--font-display` + the appropriate `--type-heading-*` token, body uses `--font-body`; (6) HTML restructured where it improves UX; (7) stable semantic locators present (ARIA roles + accessible names preferred; `data-testid` where role/name are insufficient).
- **FR-018**: The sweep inventory MUST cover: Account (Login / Register / ChangePassword / AccessDenied); Home/Index (replaced by US1 for applicants; routed for reviewers/admins; re-treated for anonymous); Application (Index / Details / Create / Edit / Delete; Detail hosts US2); Item (Index / Details / Create / Edit); Quotation (Index / Details / Create / Edit; currency rendering per spec 010 preserved); Supplier (Index / Details / Create / Edit); Review (Index replaced by US4; Details get US2 embed); ApplicantResponse (Index, action surfaces); FundingAgreement (Index / Generate / Sign success path elevated by US3 / Details — Document.cshtml and _FundingAgreementLayout.cshtml carved OUT); Admin (Index / Users / Roles / SystemConfigurations / all spec-010 reports); Shared layout (`_Layout`, `_AuthLayout`, all `Components/`).
- **FR-019**: HTML restructuring MUST be permitted (and encouraged) where it serves UX/UI quality. Preserving existing markup or selectors is NOT a constraint of the sweep.
- **FR-020**: PDF carve-out files MUST remain byte-identical to their pre-spec state: `Views/FundingAgreement/Document.cshtml`, `Views/Shared/_FundingAgreementLayout.cshtml`, and any view consumed by the Syncfusion HTML-to-PDF pipeline. Generated PDFs MUST remain visually identical to a stored reference.
- **FR-021**: Page Object Model classes touched by the sweep MUST be rewritten against the new HTML and selector strategy. POMs MUST expose semantic actions over raw locators where the surface allows.
- **FR-022**: User-facing copy in every swept view MUST be reviewed against `BRAND-VOICE.md` and rewritten where it violates the guide.
- **FR-023**: A `SWEEP-CHECKLIST.md` deliverable MUST be published in the spec directory listing every view in the inventory and the seven swept criteria as check items, used to drive the manual sweep verification.

**Applicant home dashboard (US1)**

- **FR-024**: A new view (replacing `Views/Home/Index.cshtml` for users in the Applicant role) MUST render the dashboard layout: welcome hero, KPI strip, awaiting-action callout (when count > 0), active applications card list, recent activity feed, resources strip.
- **FR-025**: The welcome hero MUST display "Welcome back, {FirstName} — here's where you are today." in Fraunces using the appropriate display token, with a brief Inter subhead in voice-guide-compliant copy.
- **FR-026**: The KPI strip MUST display four counts: Active applications, In review, Awaiting your action, Funded. Counts MUST animate from zero to the final value over `--motion-slow` (capped at 60 frames) on mount, suppressed under reduced-motion.
- **FR-027**: The awaiting-action callout MUST appear ONLY when at least one application requires applicant action (e.g., a draft to send, a sent-back response to provide, an agreement to sign). It MUST display a single-sentence message naming the action and a single primary CTA navigating to the correct surface.
- **FR-028**: The active applications card list MUST render rich cards (max 3 visible, "show all" link if more) showing: application number + project name (heading), embedded mini journey timeline (US2 mini variant), current-stage status pill, days in current state, last activity timestamp, and a contextual primary action.
- **FR-029**: The recent activity feed MUST use `_EventTimeline` (re-tokened) and list the latest events across the applicant's portfolio (max 10 visible, with a "show more" link if more exist — symmetric with FR-055's reviewer feed). Each event MUST be keyboard-focusable and clickable, deep-linking to the surface where it occurred.
- **FR-030**: The resources strip MUST render three cards: How funding works, Submission tips, Get help.
- **FR-031**: The empty state (zero applications) MUST render a full-bleed welcome scene with the seed/sprout illustration (US7), Fraunces hero "Ready to apply for funding?", a single primary CTA "Start a new application", and a three-card trust strip below: How long it takes / What you'll need / How decisions are made.
- **FR-032**: New partials MUST exist: `_ApplicantHero` (welcome + KPI strip + awaiting-action callout), `_ApplicationCard` (rich card variant), `_ResourcesStrip`.
- **FR-033**: The dashboard MUST surface the data it needs through Application-layer projections without requiring schema changes. Data dependencies: aggregate counts per applicant by status; recent activity events; days-in-state calculation reusing the spec-010 `VersionHistory` logic.

**Application journey timeline (US2)**

- **FR-034**: A new reusable Razor partial `_ApplicationJourney` MUST be introduced. It MUST accept an `Application` (or projected view model) plus a variant parameter (`Full`, `Mini`, `Micro`).
- **FR-035**: The canonical stage model MUST be: Draft → Submitted → Under Review → Decision (branches: Approved / Sent back / Rejected) → Agreement Generated → Signed → Funded. Branches: Sent back loops to Submitted; Rejected is terminal-or-Appeal; Appeal resolves to Approved (rejoins mainline) or Rejection upheld (terminal).
- **FR-036**: A single canonical (icon, label, color-token) mapping for stages MUST exist as the source of truth across `_StatusPill` (spec 008) and `_ApplicationJourney`. Implementation choice (extend `IStatusDisplayResolver` vs. introduce a sibling resolver) is deferred to planning.
- **FR-037**: The Full variant MUST render: completed nodes (small, filled, checkmarked, label in `--color-text-secondary`); current node (larger, primary fill, subtle glow halo, heading-sm label in `--color-text-primary`); pending nodes (small, outlined, low-opacity, muted label); branch nodes (sub-track below mainline, status-token color: warning for Sent back, danger for Rejected, info for Appeal).
- **FR-038**: Hover or focus on any node MUST reveal a tooltip showing the stage's timestamp and actor name (sourced from `VersionHistory`).
- **FR-039**: Clicking any completed node MUST scroll to (or highlight) the matching entry in the page's `_EventTimeline` partial.
- **FR-040**: The Mini variant MUST render dots + connector + current-stage label only (no tooltips, no per-node interaction). Used in `_ApplicationCard` (US1).
- **FR-041**: The Micro variant MUST render dots only (no labels, no tooltips), with the current dot enlarged. Used inline in `_ReviewerQueueRow` (US4).
- **FR-042**: Mount animation MUST stagger completed nodes filling in sequence with a 60 ms stagger using `--ease-spring`, total duration capped at `--motion-slow`. On the next page render after a stage advance, the newly-completed node receives the same spring-fill (real-time push is excluded by FR-068; "advance" therefore means "the user reloaded or navigated to a page that re-renders the timeline"). Suppressed under reduced-motion.
- **FR-043**: A `JourneyProjector` (or equivalent Application-layer service) MUST project an `Application` into the view model the partial consumes, keeping the partial presentation-only. Branch resolution (Send-back loop count, Appeal status) MUST be sourced from existing aggregates per specs 002, 004, and 006.

**Signing ceremony moment (US3)**

- **FR-044**: A new ceremony view (or partial rendered from the Sign action's success path — implementation choice deferred to planning) MUST replace the existing flash-message-and-redirect success path of the Sign action.
- **FR-045**: The ceremony view MUST render: hero seal animation (drawn-in stylized mark, ~200 px, `--color-primary` + `--color-accent`, springs in over `--motion-celebratory`); hero headline in Fraunces using `--type-display-lg`, variant-aware copy; single-shot soft confetti burst (≤ 2 s, fires once, aria-hidden); funding summary card (animated number ticker on amount); "What happens next" calendar-anchored card; action footer with primary "View funding details" and secondary "Back to dashboard".
- **FR-046**: Variant-aware hero copy MUST be: applicant-only-signed → "You're signed." / "We're waiting on the funder. We'll email you when it's complete." (no confetti); funder-only-signed → "Funder signature recorded." / "The applicant has been notified." (no confetti); both-signed (applicant signs last) → "Your funding is locked in." / "Funds will be transferred by {date}." (full celebration); both-signed (applicant signs after funder) → "Your funding is locked in." / "Funds will be transferred by {date}." (full celebration).
- **FR-047**: The view MUST be bookmark-safe: a direct re-visit to the ceremony URL after signing renders the non-animated summary state — no hero animation, no confetti, no number ticker. The celebratory motion fires ONLY on first arrival from a fresh sign action. Implementation mechanism (TempData / query parameter / one-shot session token) deferred to planning.
- **FR-048**: Under reduced-motion: confetti MUST NOT be mounted (a static seal asset is substituted); the number ticker renders the final amount immediately; the hero seal renders in its final state; focus moves to the primary action button immediately.
- **FR-049**: An aria-live="polite" region MUST announce the signing event for screen readers on mount ("Your funding agreement is signed" or variant-appropriate copy). The confetti canvas (when mounted) MUST be aria-hidden. Pressing ESC MUST navigate to the dashboard.
- **FR-050**: The confetti library MUST be ≤ 5 KB gzipped. Selection deferred to planning.
- **FR-051**: A new partial `_SigningCeremony` MUST be introduced.

**Reviewer queue dashboard (US4)**

- **FR-052**: A new view (replacing `Views/Review/Index`) MUST render the reviewer queue dashboard layout: welcome strip, KPI strip, filter chip strip, recent activity feed, queue table.
- **FR-053**: The KPI strip MUST display four counts: Awaiting your review, In progress, Aging > N days, Decided this month. The Aging count MUST use the spec-010 `AgingThresholdDays` configuration as the single source of truth.
- **FR-054**: The filter chip strip MUST display chips: All, Awaiting me, Aging, Sent back, Appealing. The selected chip MUST style with `--color-primary-subtle` background and `--color-primary` text. Chip selection MUST filter the table without page reload, using `--motion-base` for the row reflow.
- **FR-055**: The recent activity feed MUST be a queue-scoped variant of `_EventTimeline` (re-tokened), inline above the table, max 5 visible with a "show more" link.
- **FR-056**: The queue table MUST use `_DataTable` with density-forward styling (`--space-2` row padding) and columns: app number + project name; applicant name + avatar; inline US2 micro journey timeline (replacing a status-pill column); days in current state; last activity (relative time); contextual primary action.
- **FR-057**: Row hover MUST elevate to `--shadow-md`. Row click anywhere MUST navigate to the Review details for that application.
- **FR-058**: The empty state (zero assigned items) MUST render the calm-horizon illustration (US7) with the message "All clear — nothing's awaiting your review.".
- **FR-059**: New partials MUST exist: `_ReviewerHero` (welcome + KPI strip + filter chips), `_ReviewerQueueRow` (rich row with embedded micro journey timeline). The `_EventTimeline` partial MUST accept a queue-scoped data source.
- **FR-060**: Density discipline: applicant surfaces use `--space-4` row/section padding, reviewer tables use `--space-2`. Codified in the affected partials, not as ad-hoc per-page overrides.

**Empty-state illustration set (US7)**

- **FR-061**: A set of nine SVG illustrations MUST be produced and placed under `wwwroot/lib/illustrations/`: seed/sprout (applicant home empty); folders-stack (Application/Item index empty); open-envelope (Quotation index empty); connected-nodes (Supplier index empty); calm-horizon (Reviewer queue empty); soft-bar-chart (Admin report empty); off-center-compass (404); gentle-disconnected-wires (500); magnifier-on-empty (search-no-results).
- **FR-062**: Each illustration MUST conform to the style discipline: brand palette only (`--color-primary`, `--color-accent`, `--color-bg-surface-raised`, neutrals); ≤ 4 colors per scene; ≤ 8 KB gzipped per SVG; flat fills + a single soft gradient permitted; no detailed shading; scales cleanly across 120-280 px square targets; embedded in a Razor helper for accessibility correctness.
- **FR-063**: A Razor helper `Illustration("scene-key")` MUST exist to embed the SVG with the correct `aria-label` (when informational) or `aria-hidden="true"` (when decorative).
- **FR-064**: The `_EmptyState` partial MUST be extended to require an `illustration` parameter. All existing icon-only invocations MUST be migrated during the US6 sweep.
- **FR-065**: Illustration entrance MUST use `--motion-base`, subtle entrance only. Suppressed under reduced-motion.
- **FR-066**: Production source (in-team vs. commission vs. adapted-from-open) is deferred to planning. The acceptance criterion is that the nine SVGs exist on disk at the agreed style and are wired through the partial.

**Out-of-scope guardrails**

- **FR-067**: Database schema MUST remain unchanged. Zero edits to `src/FundingPlatform.Database/`. If planning discovers an unavoidable need, the change MUST be surfaced via `speckit-spex-evolve` for explicit approval before any dacpac edit.
- **FR-068**: No real-time push update mechanism (SignalR, websockets, etc.) MAY be introduced for the journey timeline or any other surface. Pages refresh state on next render.
- **FR-069**: No reviewer ops surface (assignment UI, bulk actions on the queue, saved views, cross-reviewer visibility) MAY be introduced.
- **FR-070**: No dark mode stylesheet, toggle, or per-user preference storage MAY ship in v1. Tokens MUST be designed to be theme-able later (semantic naming, no hard-coded contrast assumptions).
- **FR-071**: No marketing-style hero, social proof strips, sound, haptic feedback, social sharing, achievements, streaks, or per-funded-item milestone overlays MAY ship in v1.

**Cross-cutting**

- **FR-072**: Brand sign-off MUST be an explicit gate before merge. The proposed display brand (name + logo + palette) MUST be approved by the user, recorded in the PR description.
- **FR-073**: Performance baseline MUST be captured (Largest Contentful Paint and Total Blocking Time) on the four wow-moment surfaces BEFORE implementation begins, so SC-015's regression budget has a real reference.
- **FR-074**: The combined incremental wire weight added by this spec — Fraunces + Inter + JetBrains Mono (subsetted) + 9 illustration SVGs + canvas-confetti — MUST NOT exceed 400 KB total over the wire (gzipped).
- **FR-075**: WCAG AA color-contrast MUST hold on every re-tokened surface. Verified by an automated check (axe-playwright or equivalent) on the four wow-moment views and the layout shell.

### Key Entities

- **Design token**: A semantically named CSS custom property (`--color-*`, `--space-*`, `--radius-*`, `--shadow-*`, `--type-*`, `--motion-*`, `--z-*`) that maps a brand decision to a value used by a partial or stylesheet. Lives in `wwwroot/css/tokens.css`.
- **Display brand**: The user-visible identity (name, logo wordmark, abstract mark, palette, voice guide) applied to UI surfaces. Distinct from code-level identity, which remains `FundingPlatform`.
- **Wow moment**: A net-new high-attention surface introduced by this spec (Applicant home dashboard, Application journey timeline, Signing ceremony, Reviewer queue dashboard) that anchors the facelift.
- **Sweep target**: A view file in the inventory that the sweep visits and brings up to standard against the seven swept criteria.
- **Illustration scene**: One of the nine empty-state SVGs in the v1 set, identified by a scene-key (e.g., `seed`, `folders-stack`, `calm-horizon`).
- **Motion catalog entry**: A documented motion pattern in the catalog (e.g., "hover/focus", "modal open", "ceremony hero"), with its trigger, duration token, easing token, and reduced-motion behavior.
- **Voice-guide rule**: A documented tone, person, or banned-construct rule from `BRAND-VOICE.md` applied to every user-facing string in the swept views.
- **PDF carve-out file**: A view file (`Document.cshtml`, `_FundingAgreementLayout.cshtml`, any view consumed by Syncfusion HTML-to-PDF) explicitly excluded from the sweep; required to remain byte-identical to its pre-spec state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A grep for raw hex color literals (`/#[0-9a-fA-F]{3,8}/`) outside `wwwroot/css/tokens.css` and the PDF carve-out files returns zero results.
- **SC-002**: A grep for inline `style=` attributes across `Views/**/*.cshtml` (excluding PDF carve-out files) returns zero results.
- **SC-003**: A grep for hard-coded animation durations (e.g., `transition` or `animation` properties with literal `\d+ms` values) outside `tokens.css` returns zero results.
- **SC-004**: All eight spec-008 partials are re-templated to consume tokens only — verified by a code-review checklist plus the SC-001 / SC-002 grep gates.
- **SC-005**: The four new partials (`_ApplicantHero`, `_ApplicationJourney` with all three variants present, `_SigningCeremony`, `_ReviewerHero` + `_ReviewerQueueRow`) exist and are used at the agreed surfaces.
- **SC-006**: Every view in the sweep inventory passes a manual review against `SWEEP-CHECKLIST.md`.
- **SC-007**: All nine illustration SVGs exist under `wwwroot/lib/illustrations/`, conform to the style discipline (palette, ≤ 4 colors, ≤ 8 KB gz, scale 120-280 px), and are wired to the `_EmptyState` partial via the `Illustration("scene-key")` helper.
- **SC-008**: The signing ceremony view renders the correct variant (four cases) per fixture; the celebratory motion fires on first arrival from a fresh sign action and does NOT fire on a bookmarked re-visit.
- **SC-009**: The applicant home dashboard renders correctly for the four reference fixtures (zero applications / one active / 2-3 active / many-with-show-all). KPI counts match seeded data exactly.
- **SC-010**: The reviewer queue dashboard renders correctly for the four reference reviewer fixtures (full queue / empty / only-aging / only-appealing). The Aging KPI value uses the spec-010 `AgingThresholdDays` configuration; verified by changing the seed value and re-asserting.
- **SC-011**: The journey timeline renders correct states across all seven mainline stages and all three branch types. Click-to-event-log linkage works on all completed nodes. All three variants (Full / Mini / Micro) render in their host surfaces.
- **SC-012**: The `prefers-reduced-motion` contract holds across the entire motion catalog. Verified by a dedicated Playwright test running with the reduce-motion option.
- **SC-013**: WCAG AA color-contrast holds on every re-tokened surface. Verified by automated axe-playwright check on the four wow-moment views and the layout shell.
- **SC-014**: PDF identity is preserved — the generated Funding Agreement PDF is visually identical to a stored reference PDF.
- **SC-015**: Page-load performance does not regress. The pre-spec LCP and TBT baseline (captured per FR-073) on the four wow-moment surfaces is matched within +10% after the spec lands.
- **SC-016**: Asset budget: the combined incremental wire weight added by this spec does not exceed 400 KB total gzipped.
- **SC-017**: Every Playwright test passing before the spec passes after; the net-new wow-moment test files for US1 / US2 / US3 / US4 pass; the reduced-motion test passes; Page Object Models touched by the sweep have been refactored to semantic locators.
- **SC-018**: Schema unchanged. `git diff --stat` against `src/FundingPlatform.Database/` is empty.
- **SC-019**: Voice-guide compliance: every user-facing string in views touched by the sweep passes the `BRAND-VOICE.md` checklist (no ALL CAPS shouting, no exclamation marks except the signing ceremony, no "submit" CTAs, no passive voice in microcopy).
- **SC-020**: Brand sign-off: the proposed display brand (name + logo + palette) is approved by the user before merge — recorded as an explicit gate in the PR description.
- **SC-021**: Designer/product review of the applicant home dashboard signs off that — for the four reference fixtures of SC-009 — the welcome hero, KPI strip, awaiting-action callout (when present), and primary "next-step" CTA are all identifiable on first paint without scrolling. Recorded as an explicit review item in the PR description.

## Assumptions

- The platform remains pre-production (no real applicants or disbursements yet). This justifies aggressive scope (mirroring spec 008's Option C call) and tolerates a meaningful amount of sweep churn.
- The `VersionHistory` mechanism shipped in spec 010 is sufficient to source per-application stage timestamps + actors for both the journey timeline tooltips and the recent-activity feed across US1 and US4.
- The aging threshold configuration (`AgingThresholdDays`) shipped in spec 010 is the single source of truth for the reviewer queue's Aging KPI and Aging filter chip.
- An applicant is in exactly one role at a time (per spec 009); demoting an applicant mid-session results in a role-appropriate landing surface on the next render.
- Self-hosted fonts under `wwwroot/lib/fonts/` are licensed-clear for production use (Fraunces SIL OFL, Inter SIL OFL, JetBrains Mono Apache 2.0).
- Designer time for SVG illustration production and logo asset finalization is available during planning/implementation. If unavailable, planning identifies a fallback (commissioned vs. adapted-open).
- Animation library `canvas-confetti` (or a comparable ≤ 5 KB-gzipped equivalent) is acceptable as the single motion-library exception. The exact dependency pin is finalized in planning.
- Visual-regression tooling (Playwright screenshot comparison or Percy / Chromatic) remains deferred to a future spec; v1 uses manual side-by-side review against `SWEEP-CHECKLIST.md`. The semantic-selector upgrade in US6 makes a future adoption cheap.
- The currency-rendering decision shipped in spec 010 is preserved as-is across the Quotation surfaces during the sweep.
- The communication / messaging surface remains a future-spec concern; reviewer-applicant comments are re-skinned but not restructured here.
- A future localization layer (the spec-008 deferred concern) remains deferred. Voice-guide rewrites in this sweep MUST NOT embed copy into partials in ways that would block a future i18n pass.
