# Illustration Helper Contract

Razor extension method `@Illustration("scene-key")` (FR-063). Centralizes empty-state SVG embedding with correct accessibility semantics.

---

## Signatures

```csharp
public static class IllustrationHelper
{
    // Decorative use (default): SVG embedded with aria-hidden="true".
    public static IHtmlContent Illustration(this IHtmlHelper html, string sceneKey);

    // Informational use: SVG embedded with role="img" and aria-label="<altText>".
    public static IHtmlContent Illustration(this IHtmlHelper html, string sceneKey, string altText);

    // Variant for size hint (CSS class). Default is "illustration--md" (160 px).
    public static IHtmlContent Illustration(
        this IHtmlHelper html,
        string sceneKey,
        string? altText,
        IllustrationSize size);
}

public enum IllustrationSize
{
    Sm,   // 120 px
    Md,   // 160 px (default)
    Lg,   // 220 px
    Xl    // 280 px
}
```

---

## Scene-Key Registry (FR-061)

The 9 scenes for v1:

| scene-key | Description | Used by |
|-----------|-------------|---------|
| `seed` | Seedling/sprout in warm tones | Applicant home empty (US1.AS2, FR-031) |
| `folders-stack` | Warm-tinted folders, slight tilt | Application/Item index empty |
| `open-envelope` | Open envelope with letter peeking out | Quotation index empty |
| `connected-nodes` | Three nodes joined by soft lines | Supplier index empty |
| `calm-horizon` | Distant landline + subtle sun | Reviewer queue empty (US4.AS5, FR-058) |
| `soft-bar-chart` | Three pastel bars + grid line | Admin report empty |
| `off-center-compass` | Compass leaning slightly | 404 |
| `gentle-disconnected-wires` | Two soft cables, gap, no spark | 500 |
| `magnifier-on-empty` | Magnifier over an empty card | Search-no-results |

Registry implementation:

```csharp
internal static class IllustrationRegistry
{
    public static readonly IReadOnlyDictionary<string, string> SceneToFile = new Dictionary<string, string>
    {
        ["seed"] = "/lib/illustrations/seed.svg",
        ["folders-stack"] = "/lib/illustrations/folders-stack.svg",
        ["open-envelope"] = "/lib/illustrations/open-envelope.svg",
        ["connected-nodes"] = "/lib/illustrations/connected-nodes.svg",
        ["calm-horizon"] = "/lib/illustrations/calm-horizon.svg",
        ["soft-bar-chart"] = "/lib/illustrations/soft-bar-chart.svg",
        ["off-center-compass"] = "/lib/illustrations/off-center-compass.svg",
        ["gentle-disconnected-wires"] = "/lib/illustrations/gentle-disconnected-wires.svg",
        ["magnifier-on-empty"] = "/lib/illustrations/magnifier-on-empty.svg",
    };
}
```

Unknown `sceneKey` ⇒ `ArgumentException` thrown at render time. This is intentional: a typo should fail fast in development; the SWEEP-CHECKLIST verifies all swept views render their illustrations correctly (SC-007).

---

## Style Discipline (FR-062, SC-007)

Each SVG MUST satisfy:

1. **Palette discipline**: only colors from `--color-primary`, `--color-accent`, `--color-bg-surface-raised`, neutral tones. No raw hex outside these (the SVG references the values directly since SVG attributes don't inherit CSS custom properties through `<img>`; we inline the SVG so `currentColor` and CSS variable references work).
2. **Color count**: ≤ 4 colors per scene.
3. **Weight**: ≤ 8 KB gzipped per SVG.
4. **Scale**: renders cleanly at 120 px and 280 px square targets.
5. **Style**: flat fills + a single soft gradient permitted; no detailed shading, no realistic textures.

The implementation pass adds an automated check (a small build-time script or test) that gzips each SVG and asserts the size budget.

---

## Inline vs `<img>`

We inline SVGs (rather than `<img src="…">`) so:

- The illustration inherits the page's CSS custom properties, enabling future theme-ability.
- One fewer network request per illustration.
- Decorative-vs-informational accessibility can be set on the root `<svg>` element directly.

The helper reads SVG content from disk (cached via `IMemoryCache` with the scene-key → content map) and emits it inline.

---

## Entrance Animation (FR-065)

When mounted in a partial:

```css
.illustration {
  opacity: 0;
  transform: translateY(8px);
  animation: illustration-enter var(--motion-base) var(--ease-out) forwards;
}
@keyframes illustration-enter {
  to { opacity: 1; transform: translateY(0); }
}
```

Suppressed under `prefers-reduced-motion: reduce` via the `tokens.css` clamp (the duration becomes `0ms` so the animation completes instantly).

---

## Accessibility

- Decorative use (`@Illustration("seed")`): adds `aria-hidden="true"` on the root `<svg>`.
- Informational use (`@Illustration("seed", "A seedling sprouting — start your funding journey.")`): adds `role="img" aria-label="…"`.
- Color-only meaning is forbidden inside the SVG (the spec's WCAG AA color-contrast requirement does not extend to decorative illustrations, but if the illustration carries information, alt text MUST express it).
