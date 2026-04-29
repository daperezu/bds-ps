# Partial Contracts

Parameter contracts for every partial introduced or re-templated by spec 011. Partials are stored under `src/FundingPlatform.Web/Views/Shared/Components/`.

---

## Re-Templated (spec-008 origin → token-only)

### `_StatusPill.cshtml`
- **Model**: `(string Status, string ColorTokenSubtle)` (existing API; `ColorTokenSubtle` is now a `--color-*-subtle` token rather than a raw color reference) — see FR-011.
- **Output**: A pill rendered with `--color-{status}-subtle` background and `--color-{status}` text.

### `_PageHeader.cshtml`, `_DataTable.cshtml`, `_FormSection.cshtml`, `_DocumentCard.cshtml`, `_ActionBar.cshtml`, `_ConfirmDialog.cshtml`, `_KpiTile.cshtml`
- **Model**: existing parameters preserved.
- **Change**: every color / spacing / radius / shadow / type / motion property switches from raw values to `var(--…)` tokens (FR-007).

### `_EventTimeline.cshtml`
- **Model**: existing parameters + `EventTimelineScope Scope = EventTimelineScope.Application` (new optional parameter; values: `Application` (default; per-application timeline as today), `Applicant` (cross-portfolio recent activity for US1), `ReviewerQueue` (filtered by reviewer assignments for US4)).
- **Source**: existing query path for `Application`; new query paths in `IApplicantDashboardProjection.GetActivityFeed` and `IReviewerQueueProjection.GetActivityFeed` for the other two scopes.

### `_EmptyState.cshtml`
- **Model**: now requires `string IllustrationSceneKey` (FR-064) **OR** the existing icon parameter (the icon path is preserved for the auth-layout AccessDenied case where illustration is intentionally absent — see FR-018).
- **Validation**: must specify exactly one of `IllustrationSceneKey` or `IconKey`. Throws on neither / both.
- **Output**: `_EmptyState` renders `@Illustration(IllustrationSceneKey, AltLabel)` + heading + subhead + single-CTA action.

---

## Net-New Wow-Moment Partials

### `_ApplicantHero.cshtml`
- **Model**:
  ```csharp
  public sealed record ApplicantHeroModel(
      string FirstName,
      KpiSnapshot Kpis,
      AwaitingAction? AwaitingAction);
  ```
- **Renders**: welcome strip + KPI strip + (when present) awaiting-action callout.
- **Contains**: 4 instances of `_KpiTile` (configured for ticker via `data-ticker-target`).

### `_ApplicationCard.cshtml`
- **Model**: `ApplicationCardDto` (defined in `data-model.md` §2).
- **Renders**: rich card with project name + mini journey timeline + status pill + days in state + last activity + contextual primary action.
- **Composes**: `_ApplicationJourney` with `Variant=Mini`.

### `_ResourcesStrip.cshtml`
- **Model**: `IReadOnlyList<ResourcesStripCard>` (3 cards).
- **Renders**: 3-card horizontal strip; collapses to vertical at < 768 px.

### `_ApplicationJourney.cshtml`
- **Model**: `JourneyViewModel` (data-model.md §1).
- **Variants**: `Full`, `Mini`, `Micro`. Renders accordingly.
- **Behaviors (Full only)**:
  - Hover/focus → tooltip with timestamp + actor (FR-038).
  - Click completed node → `<a>` with `href="#{EventLogAnchorId}"` AND `data-action="scroll-and-highlight"` so JS scrolls to + applies `.is-highlighted` for 1.5 s (FR-039 with both behaviors required).
  - Mount → spring-fill stagger over `--motion-slow` with 60 ms per node (FR-042). Suppressed under reduced-motion.
- **Branches**: rendered as a sub-track below mainline with status-token color.

### `_SigningCeremony.cshtml`
- **Model**: `SigningCeremonyViewModel` (data-model.md §4).
- **Renders**: hero seal + Fraunces hero + variant-aware copy + confetti canvas (when applicable) + funding summary card (with ticker on amount when fresh) + "what happens next" card + action footer (primary "View funding details", secondary "Back to dashboard").
- **Behaviors**:
  - When `IsFresh == true` and variant has confetti → mount `canvas-confetti`, fire single shot, leave canvas aria-hidden.
  - When `IsFresh == false` → render static-summary state (no animations, no confetti).
  - aria-live="polite" region announces variant-appropriate signing event (FR-049).
  - ESC key navigates to `DashboardHref` (FR-049).

### `_ReviewerHero.cshtml`
- **Model**:
  ```csharp
  public sealed record ReviewerHeroModel(
      string FirstName,
      ReviewerKpiSnapshot Kpis,
      ReviewerFilter ActiveFilter);
  ```
- **Renders**: welcome strip + KPI strip + filter chip strip.
- **Behaviors**: chip click triggers a `GET` to `/Review/QueueRows?filter={filter}` and replaces the rows partial (data-model.md §3).

### `_ReviewerQueueRow.cshtml`
- **Model**: `ReviewerQueueRowDto` (data-model.md §3).
- **Renders**: `<tr>` with project + applicant + micro journey + days + last activity + primary action.
- **Behaviors**: hover lifts to `--shadow-md`; click anywhere navigates to Review details.

---

## Layout Edits

### `_Layout.cshtml`
- Imports `tokens.css` BEFORE `site.css` and BEFORE Tabler's CSS (so Tabler bridge overrides take effect).
- Imports the three font families via `@font-face` declarations from `wwwroot/lib/fonts/`.
- Imports `motion.js` and `facelift-init.js` (deferred).
- No raw hex / inline style introduced.

### `_AuthLayout.cshtml`
- Re-templated to use the new tokens.
- Strips any decorative styling that would read as a marketing hero (FR-018, research §9).
- Single brand-aligned headline, no illustrations.

---

## Razor Helper

### `IllustrationHelper`
- See `contracts/illustration-helper.md` for the full contract.

---

## Behavioral notes

- **No partial owns motion-duration literals.** All transitions / animations reference `--motion-*` and `--ease-*` tokens. (FR-014, SC-003)
- **No partial owns inline `style=` attributes.** All styling rides classes that resolve to tokens. (FR-010, SC-002)
- **No partial owns voice-guide-violating copy.** All user-facing strings either come from view models (controllable via tests) or are string literals reviewed against `BRAND-VOICE.md` during the sweep (FR-022, SC-019).
