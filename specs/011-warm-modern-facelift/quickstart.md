# Quickstart — Warm-Modern Facelift

A developer's how-to for working on spec 011. Read this before opening a sweep PR.

---

## 1. Run the app locally

```bash
dotnet run --project src/FundingPlatform.AppHost
```

Aspire dashboard at `http://localhost:18888` (or as printed). Web app at the URL printed by AppHost. SQL container persists between runs (skip `--EphemeralStorage=true` for dev; E2E fixtures pass it for clean runs).

---

## 2. Make a token-only change

Tokens live ONLY in `src/FundingPlatform.Web/wwwroot/css/tokens.css`. Every other CSS file consumes them via `var(--…)`.

To change the primary color across the entire app, edit one line in `tokens.css`:

```css
:root {
  --color-primary: #2E5E4E;   /* edit here, propagates everywhere */
}
```

Verify:

- `site.css` references `var(--color-primary)`, never a hex.
- All partials reference tokens via class-bound CSS, never inline.
- The Tabler bridge in `tokens.css` (`--tblr-primary: var(--color-primary)`) ensures Tabler components inherit the change.

---

## 3. Verify the token-discipline gates locally

Run these greps from repo root before pushing. Each MUST return zero matches outside the documented carve-outs.

```bash
# SC-001: raw hex outside tokens.css and PDF carve-outs
git ls-files 'src/FundingPlatform.Web/**/*.cshtml' 'src/FundingPlatform.Web/**/*.css' \
  | grep -v 'wwwroot/css/tokens.css' \
  | grep -v 'Views/FundingAgreement/Document.cshtml' \
  | grep -v 'Views/Shared/_FundingAgreementLayout.cshtml' \
  | xargs grep -nE '#[0-9a-fA-F]{3,8}\b' || echo "OK: no raw hex outside carve-outs"

# SC-002: inline style= attributes outside PDF carve-outs
git ls-files 'src/FundingPlatform.Web/Views/**/*.cshtml' \
  | grep -v 'Views/FundingAgreement/Document.cshtml' \
  | grep -v 'Views/Shared/_FundingAgreementLayout.cshtml' \
  | xargs grep -nE 'style=' || echo "OK: no inline styles"

# SC-003: hard-coded animation durations outside tokens.css
git ls-files 'src/FundingPlatform.Web/wwwroot/css/*.css' 'src/FundingPlatform.Web/wwwroot/js/*.js' \
  | grep -v 'wwwroot/css/tokens.css' \
  | xargs grep -nE '(transition|animation)[^;{]*[0-9]+ms' || echo "OK: no hard-coded durations"

# Token naming: no value-bound names
grep -nE '--color-(white|black|#[0-9a-fA-F]+)' src/FundingPlatform.Web/wwwroot/css/tokens.css \
  || echo "OK: token names are semantic"
```

A small script `scripts/verify-tokens.sh` bundles these for CI.

---

## 4. Add a new partial

1. Create `Views/Shared/Components/_NewPartial.cshtml`. Use only `var(--…)` for color/spacing/radius/shadow/type/motion.
2. Define a record `NewPartialModel` in `FundingPlatform.Application/DTOs/`.
3. Document parameters in `specs/011-warm-modern-facelift/contracts/partials.md`.
4. Add an entry to `SWEEP-CHECKLIST.md` if the partial introduces user-facing strings.

---

## 5. Add an illustration

1. Author SVG by hand to the style discipline (palette, ≤ 4 colors, ≤ 8 KB gz, scale check).
2. Add to `wwwroot/lib/illustrations/<scene-key>.svg`.
3. Register in `IllustrationHelper.IllustrationRegistry.SceneToFile`.
4. Run the gzip-budget check (`scripts/verify-illustrations.sh`) — fails if any SVG exceeds 8 KB gz.
5. Use via `@Illustration("scene-key")` in the empty-state partial.

---

## 6. Run wow-moment Playwright tests

```bash
# All E2E
dotnet test tests/FundingPlatform.Tests.E2E

# Just the wow-moment tests
dotnet test tests/FundingPlatform.Tests.E2E --filter "FullyQualifiedName~Applicant.ApplicantDashboardTests|Application.JourneyTimelineTests|FundingAgreement.SigningCeremonyTests|Review.ReviewerQueueDashboardTests"

# Reduced-motion test only (verifies SC-012)
dotnet test tests/FundingPlatform.Tests.E2E --filter "FullyQualifiedName~Motion.ReducedMotionTests"

# Contrast (axe-playwright) only (verifies SC-013)
dotnet test tests/FundingPlatform.Tests.E2E --filter "FullyQualifiedName~Accessibility.ContrastTests"
```

E2E fixtures pass `--EphemeralStorage=true` to AppHost so each run starts with a clean SQL container.

---

## 7. Capture / compare LCP and TBT (FR-073, SC-015)

Day-1 baseline (pre-implementation):

```bash
node scripts/capture-perf-baseline.mjs > specs/011-warm-modern-facelift/perf-baseline.json
```

Post-implementation comparison (during US6 sweep verification):

```bash
node scripts/compare-perf.mjs specs/011-warm-modern-facelift/perf-baseline.json
```

Fails if any of the four wow-moment surfaces regress LCP or TBT by more than +10%.

---

## 8. Verify the SWEEP-CHECKLIST

For every view in the inventory (FR-018), open `SWEEP-CHECKLIST.md` and tick the seven uniform criteria. The PR description includes the completed checklist.

The seven criteria per view (FR-017):

1. No raw hex/px outside tokens.
2. No inline `style=`.
3. Correct partial usage (`_StatusPill` / `_EmptyState` / `_ActionBar` / `_ConfirmDialog`).
4. Voice-guide compliant copy.
5. Correct typography roles (display = Fraunces; body = Inter; mono = JetBrains Mono).
6. HTML restructured where it improves UX.
7. Stable semantic locators present.

---

## 9. Brand sign-off (FR-072, SC-020)

Before merge, the PR description MUST contain a confirmation line:

> Brand sign-off: APPROVED by @user — name `Forge`, palette pinned in research.md §1.2, logo direction in research.md §1.4.

If sign-off is pending, the PR remains in `Draft` state.

---

## 10. PDF carve-out check (FR-020, SC-014)

Before submitting the PR:

```bash
git diff main -- src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml \
                 src/FundingPlatform.Web/Views/Shared/_FundingAgreementLayout.cshtml
```

MUST output empty. If any PDF carve-out file is touched, revert and surface as a deviation requiring `speckit-spex-evolve`.

PDF visual-identity check (manual): regenerate the funding agreement PDF for the reference fixture and compare against the stored reference image. Pixel-identical (or within rounding tolerance) ⇒ pass.

---

## 11. Common gotchas

- **Tabler classes still leak Tabler colors.** If a Tabler component renders with off-brand colors, the `--tblr-*` bridge in `tokens.css` is missing the relevant override. Add to the bridge inventory in `research.md §2.1` and the file.
- **A view file appears swept but a hex grep matches.** Look for hex in inline SVG inside the view. Move the SVG to `wwwroot/lib/illustrations/` and use `@Illustration("scene-key")`.
- **Reduced-motion test fails for ceremony.** Verify the `_SigningCeremony` partial's confetti branch checks `matchMedia('(prefers-reduced-motion: reduce)').matches` BEFORE calling `confetti(…)`. The static seal must mount unconditionally and the canvas not be inserted into the DOM.
- **Number ticker animates the wrong number.** The KPI tile's `data-ticker-target="123"` must match the final integer; the JS reads this attribute on mount and animates from 0 to the value.
