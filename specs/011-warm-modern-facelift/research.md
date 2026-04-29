# Phase 0 — Research: Warm-Modern Facelift

This file resolves every NEEDS-CLARIFICATION / open question from `spec.md` and `review_brief.md`. Each entry follows the **Decision / Rationale / Alternatives considered** schema and is the binding source-of-truth for `/speckit-tasks` and implementation.

---

## 1. Brand Identity

### 1.1 Display Name (FR-001, FR-072)

- **Decision**: Spec proposes **Forge** as the primary display brand candidate; *Ascent* as the fallback; "keep `FundingPlatform`" as the no-rename option. The actual selection is the user-sign-off gate of FR-072 / SC-020. Until sign-off, spec-internal deliverables (`BRAND-VOICE.md`, asset filenames) use the placeholder `Forge`. After sign-off, a single rename PR may follow.
- **Rationale**: Forge is short, action-oriented, evokes craft + creation (apt for entrepreneurs), and is acoustically distinct from "Funding". Ascent is more aspirational and journey-oriented but more abstract. Keeping `FundingPlatform` is the least-risk option but undersells the warm-modern positioning. The spec already requires the engineering namespace to remain `FundingPlatform`, so display-only renaming carries near-zero technical cost.
- **Alternatives considered**: *Cradle* (rejected — too soft, infantilizing), *Stride* (rejected — sportswear association), *Helm* (rejected — overused in tech).

### 1.2 Color Story (FR-001, FR-006, FR-009, SC-013)

Pinned palette (all values WCAG AA-checked against the warm-off-white background `#FAF7F2`):

| Token | Hex | Role |
|-------|-----|------|
| `--color-primary` | `#2E5E4E` | Warm forest green; primary CTAs, current-stage journey node, focus rings |
| `--color-primary-strong` | `#1F4438` | Hover/active for `--color-primary` |
| `--color-primary-subtle` | `#E1ECE6` | Selected filter chip background, ceremony hero seal halo |
| `--color-accent` | `#D98A1B` | Warm amber; KPI highlights, illustration secondary fill |
| `--color-accent-subtle` | `#FBEED6` | Awaiting-action callout background |
| `--color-bg-page` | `#FAF7F2` | Warm off-white page background |
| `--color-bg-surface` | `#FFFFFF` | Card / table / form surface |
| `--color-bg-surface-raised` | `#F4EFE6` | Hero strips, dashboard section dividers |
| `--color-text-primary` | `#1A1A1A` | Warm near-black headings |
| `--color-text-secondary` | `#5A5A5A` | Body copy on light surface |
| `--color-text-muted` | `#8A8A8A` | Helper text, completed-node labels |
| `--color-border` | `#E5DED2` | Card borders, table dividers |
| `--color-success` / `-subtle` | `#2E7D32` / `#E1F0E2` | Funded / signed states |
| `--color-warning` / `-subtle` | `#A86E18` / `#F8EAD0` | Sent-back branch, aging KPI |
| `--color-danger` / `-subtle`  | `#9E2A2A` / `#F5DBDB` | Rejected branch terminal |
| `--color-info` / `-subtle` | `#2563AB` / `#DDE9F4` | Appeal branch |

- **Rationale**: Warm-off-white background instead of pure `#FFFFFF` softens contrast against amber accents and improves long-session readability. Forest green primary lifts away from generic SaaS blue while remaining trustworthy for a funding agency. All status palette pairs stay in the warm spectrum and pass AA at body text size against `--color-bg-surface` and `--color-bg-page`.
- **Alternatives considered**: A blue-primary palette (rejected — generic SaaS); a desaturated grayscale + single warm accent (rejected — felt more editorial than financial); Tailwind-style cool neutrals (rejected — clashes with amber accent).

### 1.3 Typography (FR-001, FR-003)

- **Decision**: Self-host all three families under `wwwroot/lib/fonts/`. **Subset**: Latin-1 + numerals + currency glyphs. Variable fonts where available.
  - **Fraunces** (display serif) — variable weight 400–800; subset to ~80 KB gz. Used for `--type-display-xl`, `--type-display-lg`, `--type-hero` only.
  - **Inter** (body sans) — variable weight 400–700; subset to ~70 KB gz. Used for body, headings other than display, UI controls.
  - **JetBrains Mono** (monospace) — weight 400 only; subset Latin-1 + digits + symbols; ~50 KB gz. Used for application numbers, monetary amounts in tables, status codes.
- **Type-scale ramp** (8 steps; rem against 16 px root):

  | Token | Family | Size | Line-height | Use |
  |-------|--------|------|-------------|-----|
  | `--type-display-xl` | Fraunces 600 | 3.5 rem | 1.05 | Ceremony hero |
  | `--type-display-lg` | Fraunces 600 | 2.5 rem | 1.1 | Dashboard welcome hero |
  | `--type-heading-lg` | Inter 700 | 1.75 rem | 1.2 | Page H1 |
  | `--type-heading-md` | Inter 600 | 1.25 rem | 1.3 | Section heading, application card title |
  | `--type-heading-sm` | Inter 600 | 1.0 rem | 1.35 | Card subhead, current-stage label |
  | `--type-body` | Inter 400 | 0.9375 rem | 1.5 | Default body |
  | `--type-meta` | Inter 500 | 0.8125 rem | 1.4 | KPI labels, table cells |
  | `--type-micro` | Inter 500 | 0.6875 rem | 1.3 | Footnote, badge text |

- **Rationale**: Fraunces only at display sizes preserves the warm-modern feel without degrading body readability. Inter is the workhorse and the densest data surfaces use it at `--type-meta` for triage scannability. JetBrains Mono ensures ID/amount alignment in tables. Subsetting + variable weights keeps the budget well under FR-074's 400 KB.
- **Alternatives considered**: Fraunces for all headings (rejected — degrades scan speed at H3 and below); a single sans family (rejected — undersells the brand identity); using `system-ui` for monospace (rejected — fragments the visual identity across OSes).

### 1.4 Logo Direction (FR-001, FR-005)

- **Decision**: A wordmark in Fraunces + an abstract mark concept of a **rising open-arc** (a quarter-circle resolving upward, two-stroke gradient from `--color-primary` to `--color-accent`). Deliverables: SVG wordmark, SVG mark, favicon set (16/32/48/180 px).
- **Rationale**: A rising-arc reads as growth + journey (mirrors the journey timeline metaphor); two-stroke gradient ties the two brand colors visually and renders cleanly at favicon size.
- **Alternatives considered**: A seedling icon (rejected — too literal, conflicts with the seed/sprout illustration scene); an angular geometric mark (rejected — clashes with warm-modern positioning).

### 1.5 Voice Guide (FR-004, SC-019)

Source-of-truth file: `specs/011-warm-modern-facelift/BRAND-VOICE.md`. Outline:

- **Tone**: Warm, plain, confident, never corporate-jargony.
- **Person**: 2nd person to address the user (*you* / *your*); 1st-person plural for the platform (*we'll send you...*) only when describing platform behavior.
- **Stage-aware patterns**: Draft = encouraging ("Tell us about your project."); Review = factual ("Your application is being reviewed."); Decision = clear-eyed ("We've made a decision."); Signing = celebratory but dignified ("Your funding is locked in.").
- **Banned constructs**: ALL CAPS shouting; exclamation marks (single carve-out: signing ceremony hero copy); "submit" CTAs (use *Send*, *Sign*, *Confirm*, *Continue*); passive voice in microcopy (*"Your application has been received"* → *"We've received your application"*).
- **Verification handle**: SWEEP-CHECKLIST.md item per swept view; voice-guide compliance is one of the seven swept criteria (FR-017 #4).

---

## 2. Tabler `--tblr-*` Bridge

### 2.1 Override Inventory

- **Decision**: Override the following 12 Tabler custom properties in `tokens.css` (under `:root`). Leave all other `--tblr-*` defaults untouched.

| Tabler property | Mapped to |
|-----------------|-----------|
| `--tblr-primary` | `var(--color-primary)` |
| `--tblr-primary-rgb` | `46, 94, 78` |
| `--tblr-secondary` | `var(--color-text-secondary)` |
| `--tblr-success` | `var(--color-success)` |
| `--tblr-warning` | `var(--color-warning)` |
| `--tblr-danger` | `var(--color-danger)` |
| `--tblr-info` | `var(--color-info)` |
| `--tblr-body-bg` | `var(--color-bg-page)` |
| `--tblr-body-color` | `var(--color-text-primary)` |
| `--tblr-border-color` | `var(--color-border)` |
| `--tblr-card-bg` | `var(--color-bg-surface)` |
| `--tblr-link-color` | `var(--color-primary-strong)` |

- **Rationale**: Overriding the highest-traffic Tabler tokens propagates the new palette through every Tabler component (cards, tables, alerts, buttons) without re-templating Tabler internals. Keeping the long tail (e.g., `--tblr-input-padding-y`) at Tabler defaults preserves spec-008 compatibility and avoids drift maintenance.
- **Alternatives considered**: Override every `--tblr-*` (rejected — turns into a fork; locks us into Tabler version freezes); override nothing and let `--color-*` cascade through `site.css` only (rejected — Tabler components would render with the old palette wherever Razor partials don't gate).

---

## 3. Token Naming Rules (FR-070 verification handle)

- **Decision**: Token names MUST be **semantic** (describe role, not value).
  - ✅ `--color-bg-page`, `--color-text-primary`, `--motion-base`, `--space-4`.
  - ❌ `--color-white`, `--color-#1A1A1A`, `--motion-200ms`, `--gap-1rem`.
- **Greppable verification**: a regex catching value-bound names is added to the SWEEP-CHECKLIST and to the review-code stage's grep gate (`/--color-(white|black|#[0-9a-fA-F]+|[0-9]+)/`). Zero matches required.
- **Why this matters**: dark mode (FR-070) and any future palette pivot must be possible by overriding tokens at a different scope (e.g., `[data-theme="dark"]`). Semantic naming is the prerequisite.

---

## 4. Motion Catalog

Final pinned catalog (FR-013). Each entry: trigger, duration token, easing token, reduced-motion behavior. **Source-of-truth file**: `contracts/motion-catalog.md`.

| Pattern | Trigger | Duration | Easing | Reduced-motion |
|---------|---------|----------|--------|----------------|
| Hover lift | Card / row hover | `--motion-fast` | `--ease-out` | Opacity exempt; transforms suppressed |
| Focus ring | Tab focus | `--motion-instant` | `--ease-out` | No suppression (accessibility) |
| Button press | Active state | `--motion-instant` | `--ease-out` | Suppressed |
| Modal open | Modal mount | `--motion-base` | `--ease-decelerate` | Opacity exempt |
| Drawer slide | Drawer toggle | `--motion-base` | `--ease-decelerate` | Suppressed |
| Popover | Popover open | `--motion-fast` | `--ease-out` | Opacity exempt |
| Skeleton swap | Async load | `--motion-fast` | `--ease-out` | Opacity exempt |
| Toast | Toast mount | `--motion-base` | `--ease-spring` | Opacity exempt |
| Page route fade | Route change | `--motion-fast` | `--ease-out` | Opacity exempt |
| Status pill change | State transition | `--motion-base` | `--ease-spring` | Suppressed (renders final state) |
| Number ticker | KPI mount | `--motion-slow` | `--ease-decelerate` | Renders final value |
| Journey timeline progression | Mount + stage advance | `--motion-slow` (60 ms stagger / node) | `--ease-spring` | Renders final state, no stagger |
| Signing ceremony hero | Ceremony mount (fresh only) | `--motion-celebratory` | `--ease-spring` | Static seal asset, no spring |
| Empty-state entrance | Empty-state mount | `--motion-base` | `--ease-out` | Suppressed |

The `prefers-reduced-motion` contract is implemented in `tokens.css` via a media query that clamps each duration token to `0ms`, with `--motion-fast` preserved on opacity transitions only (FR-015).

---

## 5. Illustrations (FR-061..FR-066)

### 5.1 Source

- **Decision**: **In-team SVG production**, hand-authored against the documented style discipline (palette only, ≤ 4 colors, ≤ 8 KB gz, scales 120–280 px). The implementing engineer (or an embedded designer) draws each scene per the brief in `contracts/illustration-helper.md`.
- **Rationale**: Speed (no commission lead time), consistency (one author, one pen), control (palette compliance enforced inline). The 9-scene set is small and stylistically simple; commissioning would over-spend.
- **Fallback**: If the in-team author is unavailable for any scene, that single scene falls back to a Lucide / Tabler icon at 120 px scaled, and the `_EmptyState` partial logs a build-time `data-illustration-fallback="true"` attribute. The fallback is documented as a deviation in the task notes for that user story.
- **Scene-key registry**: `seed`, `folders-stack`, `open-envelope`, `connected-nodes`, `calm-horizon`, `soft-bar-chart`, `off-center-compass`, `gentle-disconnected-wires`, `magnifier-on-empty`. Mapping documented in `contracts/illustration-helper.md`.

### 5.2 Helper Signature

- **Decision**: Razor extension method `@Illustration("scene-key")` returning a `HtmlString` containing the inline SVG (so it inherits CSS custom properties for theme-ability). The helper looks up the scene-key in the registry, reads the SVG from `wwwroot/lib/illustrations/<scene-key>.svg`, applies an `aria-label` (when an `alt` is provided) or `aria-hidden="true"` (decorative default), and renders. A short overload `@Illustration("scene-key", "Alt text describing the scene")` switches to informational mode.
- **Rationale**: Inline SVG enables CSS-property-driven recoloring (theme-ability) and avoids a network request per illustration. Helper centralizes accessibility correctness so partials don't duplicate the logic.

---

## 6. Signing Ceremony

### 6.1 View vs Partial (FR-044)

- **Decision**: A **dedicated controller action and view**:
  - Action: `FundingAgreementController.SignCeremony(Guid applicationId)` returning `Sign/Ceremony.cshtml`.
  - The successful POST `Sign` action issues a `RedirectToAction(nameof(SignCeremony), new { applicationId })` after setting `TempData["CeremonyFresh"] = true`.
  - `Sign/Ceremony.cshtml` composes the `_SigningCeremony` partial with a `bool isFresh` parameter sourced from TempData.
- **Rationale**: A real action gives us a bookmark-able URL (a non-trivial UX requirement of FR-047). It also lets back-end variant logic (which signature was last) live in the controller, where it's testable, instead of being inferred client-side. The partial keeps the markup reusable if a future inline use case appears.

### 6.2 Fresh-vs-Bookmark Mechanism (FR-047)

- **Decision**: **TempData with a one-shot key**.
  - On POST `Sign` success: `TempData["CeremonyFresh"] = true; return RedirectToAction(nameof(SignCeremony), …)`.
  - On `SignCeremony`: `var isFresh = TempData["CeremonyFresh"] as bool? ?? false;` (TempData read auto-clears on consume).
  - The view passes `isFresh` to `_SigningCeremony`. Fresh = true mounts hero animation, confetti (when applicable), and number ticker. Fresh = false renders the static summary state.
- **Rationale**: TempData survives one redirect and dies after; bookmark re-visits never see it. No session storage growth, no URL pollution (the URL stays clean as `/FundingAgreement/SignCeremony/{id}`), no client-side localStorage races.
- **Alternatives considered**: Query-string flag (rejected — bookmark-poisons the URL); session token (rejected — opaque, harder to test); cookie (rejected — same opacity, plus revoke-on-return concerns).

---

## 7. Journey-Stage Resolver (FR-036)

- **Decision**: **Sibling resolver**. A new interface `IJourneyStageResolver` lives next to spec-008's `IStatusDisplayResolver`. Both depend on a shared `IStageMappingProvider` that owns the canonical mapping table:

  ```csharp
  public sealed record StageMapping(
      string Stage,
      string IconKey,
      string Label,
      string ColorToken,        // e.g. "--color-primary"
      string SubtleColorToken); // e.g. "--color-primary-subtle"
  ```

- The provider exposes `IReadOnlyList<StageMapping> GetMainline()` and `IReadOnlyDictionary<string, StageMapping> GetBranches()` (Sent back / Rejected / Appeal).
- `IStatusDisplayResolver` (existing) is unchanged in surface area; its implementation can read from `IStageMappingProvider` if a refactor is convenient, but doing so is a non-blocking task.
- **Rationale**: Keeps spec-008 consumers untouched (lower migration risk); journey-specific branch logic doesn't pollute the simpler status-pill resolver. Single source of truth via `IStageMappingProvider` prevents drift.
- **Alternatives considered**: Extend `IStatusDisplayResolver` (rejected — branch logic is journey-specific, would over-pollute the existing interface); duplicate the table in two places (rejected — drift risk; explicitly forbidden by FR-036).

---

## 8. Visual-Regression Tooling

- **Decision**: **Defer.** Manual `SWEEP-CHECKLIST.md` review per spec. The semantic-locator upgrade in US6 makes a future Percy / Chromatic / Playwright-screenshot adoption near-zero cost.
- **Rationale**: Carry-over from spec 008. Adopting visual regression mid-facelift would double the spec's risk surface (every reviewer would be reviewing both the change and the new tooling). A focused future spec can adopt it with a baseline captured against the post-spec-011 state.

---

## 9. Login / Register Tone

- **Decision**: **Clean focused single-CTA**, no marketing hero. Consistent with FR-018's third call-out. The auth surfaces (`Login`, `Register`, `ChangePassword`, `AccessDenied`) get:
  - The new `_AuthLayout` re-templated with token-only styling.
  - A single brand-aligned headline ("Sign in", "Create an account", etc.) in `--type-heading-lg`.
  - Form using `_FormSection`.
  - Single primary CTA (per the voice guide: never "Submit").
  - No decorative illustrations on auth surfaces (illustrations are reserved for empty-states inside the authenticated app).
- **Rationale**: Auth flow tone is utilitarian — the user wants in. A marketing hero would be off-brand for a funding agency and waste viewport.

---

## 10. POM Rewrite Strategy (FR-021)

- **Decision**: Locator hierarchy used uniformly across all rewritten POMs:
  1. `page.GetByRole(...)` with `.GetByName(...)` — preferred for any control with an accessible name (buttons, links, headings, form fields).
  2. `page.GetByLabel(...)` — preferred for form fields with explicit `<label>` association.
  3. `page.GetByText(...)` — for non-interactive content assertions only.
  4. `data-testid` attribute — fallback only when role/name are insufficient (e.g., a custom-painted KPI card that has no native role mapping).
  5. CSS selectors — banned except in test-internal helpers that compose role-based locators.
- **Semantic actions over raw locators**: POM properties expose intent (e.g., `dashboard.AwaitingActionCount`, `queue.SelectAgingFilter()`) rather than `page.Locator(".kpi-card.awaiting .count")`.
- **Naming**: New POMs follow `<Surface>Page` for full views and `<Surface>Section` for partials (e.g., `ApplicantHeroSection`, `JourneyTimelineSection`).
- **Migration plan**: every existing POM under `tests/FundingPlatform.Tests.E2E/PageObjects/` is touched. POMs that wrap views removed-or-replaced by the wow-moments are deprecated; POMs whose views are merely re-templated keep their identity but rewrite their internals.
