# Motion Catalog (Pinned)

This is the binding catalog. New motion outside this catalog requires spec amendment (FR-013). Every entry uses `--motion-*` and `--ease-*` tokens — never literal millisecond values (FR-014, SC-003).

## Duration tokens (declared in `tokens.css`)

| Token | Value |
|-------|-------|
| `--motion-instant` | `50ms` |
| `--motion-fast` | `150ms` |
| `--motion-base` | `250ms` |
| `--motion-slow` | `400ms` |
| `--motion-celebratory` | `700ms` |

## Easing tokens

| Token | Value |
|-------|-------|
| `--ease-out` | `cubic-bezier(0.2, 0.7, 0.2, 1)` |
| `--ease-in-out` | `cubic-bezier(0.4, 0, 0.2, 1)` |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` (tiny overshoot) |
| `--ease-decelerate` | `cubic-bezier(0, 0, 0.2, 1)` |

## Catalog

| # | Pattern | Trigger | Duration | Easing | Reduced-motion behavior |
|---|---------|---------|----------|--------|-------------------------|
| 1 | Hover lift | Card / row hover | `--motion-fast` | `--ease-out` | Opacity exempt; transforms suppressed |
| 2 | Focus ring | Tab focus | `--motion-instant` | `--ease-out` | Not suppressed (a11y) |
| 3 | Button press | `:active` state | `--motion-instant` | `--ease-out` | Suppressed |
| 4 | Modal open | Modal mount | `--motion-base` | `--ease-decelerate` | Opacity exempt only |
| 5 | Drawer slide | Drawer toggle | `--motion-base` | `--ease-decelerate` | Suppressed |
| 6 | Popover | Popover open | `--motion-fast` | `--ease-out` | Opacity exempt only |
| 7 | Skeleton swap | Async load resolve | `--motion-fast` | `--ease-out` | Opacity exempt only |
| 8 | Toast | Toast mount | `--motion-base` | `--ease-spring` | Opacity exempt only |
| 9 | Page route fade | Route change | `--motion-fast` | `--ease-out` | Opacity exempt only |
| 10 | Status pill change | State transition | `--motion-base` | `--ease-spring` | Suppressed; renders final state |
| 11 | Number ticker | KPI / amount mount | `--motion-slow` | `--ease-decelerate` | Renders final value immediately |
| 12 | Journey timeline progression | Mount + stage advance | `--motion-slow` (60 ms stagger / node) | `--ease-spring` | Renders final state, no stagger |
| 13 | Signing ceremony hero | Ceremony mount when `IsFresh` | `--motion-celebratory` | `--ease-spring` | Static seal substituted |
| 14 | Empty-state entrance | Empty-state mount | `--motion-base` | `--ease-out` | Suppressed |

## `prefers-reduced-motion` Contract (FR-015)

Implemented in `tokens.css`:

```css
@media (prefers-reduced-motion: reduce) {
  :root {
    --motion-instant: 0ms;
    --motion-fast: 0ms;
    --motion-base: 0ms;
    --motion-slow: 0ms;
    --motion-celebratory: 0ms;
  }
  /* opacity-exemption */
  :root { --motion-opacity-exempt: 150ms; }
}
```

JS guards (`motion.js`):

- Number ticker: skips animation, sets target text to final value.
- Journey stagger: applies `--state-final` class on mount, skipping the per-node delay.
- Confetti: branched in `_SigningCeremony` partial — `if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;` before `confetti.create(...)`.

## Verification (SC-012)

Single Playwright test `Motion/ReducedMotionTests.cs` runs with `BrowserContextOptions { ReducedMotion = ReducedMotion.Reduce }` and asserts:

- KPI tickers render their final values without animation.
- Journey timeline nodes are in their final states without stagger.
- Ceremony confetti is NOT mounted; static seal is present.
- Empty-state entrance suppressed.
- Filter chip reflow happens without transition.
