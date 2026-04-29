# Review Guide: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Spec:** [spec.md](spec.md) | **Plan:** [plan.md](plan.md) | **Tasks:** [tasks.md](tasks.md)
**Generated:** 2026-04-29

---

## What This Spec Does

The platform has shipped English-only across 11 prior specs while deliberately leaving localization deferred. This spec converts that deferral into shipped reality: pin `es-CR` as the platform's single fixed locale, translate every user-facing surface to formal Costa Rican Spanish, and rename the product from "Forge" to "Capital Semilla." It ships as a single coordinated sweep across ~72 Razor views, the status registry, framework messages, the Funding Agreement PDF, the brand-mark assets, the JS namespace, and the Playwright E2E test suite.

**In scope:** Razor views/partials/layouts, the `StatusVisualMap` registry, DataAnnotation `ErrorMessage` and `[Display]` labels, ASP.NET MVC `ModelBindingMessageProvider`, ASP.NET Identity `IdentityErrorDescriber`, controller-emitted `TempData` strings, the Funding Agreement PDF body, brand wordmark and mark SVGs, HTML `lang` attribute, page titles, ARIA labels, the JS namespace `ForgeMotion`→`PlatformMotion`, the `tokens.css` brand-reference comment, and the E2E test suite's visible-text assertions.

**Out of scope:** Multi-language support (no toggle, no `IStringLocalizer`, no `.resx`); URL route slugs; code identifiers; route paths; log messages; exception messages; JSON config keys; DB schema; CSV exports (stay `InvariantCulture`); already-signed Funding Agreement PDFs (immutable per spec 006); applicant-entered free-text content; brand-mark visual redesign beyond the wordmark text swap; future spec 013 (messaging), 014 (notifications), 015 (public marketing).

## Bigger Picture

This is the 12th spec in the project, the largest user-facing copy change since [spec 011 (warm-modern facelift)](../011-warm-modern-facelift/spec.md), and the change that converts the platform from English-only to a region-locked Costa Rican Spanish deployment. Once merged, the platform reads native to CR users — but reverting to English (or adding a second locale) becomes a real reverse migration, since the architectural rule explicitly forbids `IStringLocalizer` or resx scaffolding ([NFR-003](spec.md#non-functional-requirements)).

The spec closes four open threads carried forward from prior brainstorms: the [Funding Agreement locale pin](../005-funding-agreement-generation/spec.md) (open since spec 005), the [partial-copy embedding check](../008-tabler-ui-migration/spec.md) (open since spec 008), the [voice-guide compatibility caveat](../011-warm-modern-facelift/spec.md) (open since spec 011), and the [brand sign-off](../011-warm-modern-facelift/spec.md) (open since spec 011, closed with "Capital Semilla").

The spec touches existing seams designed by predecessors: spec 008's partial library was deliberately built to receive copy as parameters, and spec 011's warm-modern voice-guide rewrites kept that constraint. Phase 0 research confirmed those seams hold — the `StatusVisualMap` registry has zero `enum.ToString()` bypasses, partials don't embed copy that would block translation. The plan's "replace in place" architecture rests on that prior work.

External context worth knowing: .NET's `es-CR` `CultureInfo` defaults are not what the user assumed — see "Areas where I'm less certain" below. Costa Rica's business documents conventionally use US-style `1,234.56` formatting (period decimal, comma thousands) due to long-standing US economic ties, but ICU/CLDR data for `es-CR` has shifted toward pan-LatAm `1.234,56` styling in recent ICU releases. The user explicitly chose the US-style convention; the plan implements it via explicit `NumberFormatInfo` overrides on a cloned `CultureInfo`.

---

## Spec Review Guide (30 minutes)

### Understanding the approach (8 min)

Read [spec.md Overview](spec.md#overview) and the [architectural-rule statement](spec.md#overview) ("code stays English; UI is Spanish") for the framing. Then [spec.md User Story 1](spec.md#user-story-1--voice-guide-lands-first-priority-p1) and [User Story 7](spec.md#user-story-7--e2e-test-suite-reflects-new-copy-priority-p2) for the bookends — voice-guide first, test-rewrite last. As you read, consider:

- Does the "code stays English / UI is Spanish" seam hold up for your team's debugging workflow? Is finding a Spanish error message in `git grep` going to be harder than finding an English one?
- Is the single-coordinated-sweep choice (Approach A in [brainstorm 12](../../brainstorm/12-es-cr-localization.md)) right for this team's review bandwidth, or would phased rollout (B/C) reduce review fatigue at the cost of mixed-language windows?
- Is "visible Spanish text is the contract" (NFR-007 in [spec.md NFRs](spec.md#non-functional-requirements)) the right long-term test policy, or should this spec also drive a `data-testid` push? The brainstorm chose A; I think the choice is reasonable but it does lock the suite to Spanish forever.

### Key decisions that need your eyes (12 min)

**Locale-format override on a cloned `CultureInfo`** ([research.md Decision 1](research.md#decision-1-format-locale-override-for-es-cr))

The .NET `es-CR` `CultureInfo` defaults to `1 234 567,89` (comma decimal, space thousands) and `d/M/yyyy` (single-digit). The plan clones the culture and overrides `NumberFormatInfo` and `DateTimeFormatInfo` to deliver the spec target `1,234.56` and `dd/MM/yyyy`. The spec's [Assumption clause](spec.md#assumptions) anticipated this finding.

- Question for reviewer: Does Costa Rican business actually consistently use US-style separators across all sectors (banking, retail, government, NGO grants)? If sector-specific conventions differ, the override approach may need a per-context switch — currently it's one-size-fits-all.
- Question for reviewer: The [`CultureInfo.ReadOnly(...)` wrapping in T004](tasks.md#phase-2-foundational-blocking-prerequisites) prevents accidental mutation, but does it interact well with downstream code that may try to `.Clone()` the culture for ad-hoc formatting? Worth confirming during implementation.

**Hard-cut JS namespace `ForgeMotion` → `PlatformMotion`** ([spec.md FR-007](spec.md#functional-requirements), [tasks.md T022–T023](tasks.md#phase-4-user-story-3--capital-semilla-brand-across-all-surfaces-priority-p1))

No backwards-compatible alias is left in place. The codebase has 7 callers (one definition site + six in `facelift-init.js`), all internal.

- Question for reviewer: Is anyone running browser bookmarklets, monitoring scripts, or extensions against the platform that might reference `window.ForgeMotion`? If yes, even a 1-week alias would prevent breakage. If no, hard cut is cleaner.

**Brand wordmark SVG: textual placeholder is acceptable** ([spec.md FR-023](spec.md#functional-requirements), [spec.md EC-7](spec.md#edge-cases))

If the designer hasn't produced a redrawn "Capital Semilla" wordmark by implementation time, the plan ships with a textual rendering of "Capital Semilla" in the Fraunces display font.

- Question for reviewer: Is product/marketing comfortable with a textual placeholder going to production, or should the merge block on designer delivery? Note: "Capital Semilla" is two words; the existing single-word "Forge" wordmark may not reproportion cleanly even with a designer pass.

**Voice-guide-first commit ordering enforced via `git log`** ([spec.md SC-009](spec.md#measurable-outcomes), [tasks.md T015–T018](tasks.md#phase-3-user-story-1--voice-guide-lands-first-priority-p1-prerequisite))

The voice-guide commit MUST land before any per-view rewrite commit. Verifiable via `git log --oneline 012-es-cr-localization -- specs/012-es-cr-localization/voice-guide.md` and a comparison against per-view commit timestamps.

- Question for reviewer: Is enforcing this via git history overkill, or is it the right discipline given the size of the sweep (~72 views + framework + tests)? My read: the SC justifies itself when the voice guide goes through review iteration mid-sweep — anchoring it as the first commit prevents silent drift.

**Identity error coverage scope** ([data-model.md §3](data-model.md#3-new-infrastructure-subclass-escridentityerrordescriber))

The data-model lists 22 `IdentityErrorDescriber` methods to override, out of ~28 in the base class. The unlisted ones are methods not currently surfaced by the codebase (e.g., role-related errors that the platform doesn't trigger).

- Question for reviewer: Should the subclass override every `IdentityErrorDescriber` method (including the unused ones) for completeness, or only the actually-surfaced subset? Trade-off: completeness costs ~6 more translations now; partial coverage risks an edge case showing English if a future spec uses an unhandled code path.

### Areas where I'm less certain (5 min)

- [spec.md FR-019 currency display](spec.md#functional-requirements): The plan asserts that per-quotation Currency rendering (from spec 010) is unchanged by this spec, but no test verifies that the `₡` colón symbol prefixes correctly under the format-overridden culture (we override `CurrencyDecimalSeparator` and `CurrencyGroupSeparator`). [T071 in tasks.md](tasks.md#phase-8-user-story-4--funding-agreement-pdf-in-formal-spanish-priority-p1) checks this in the PDF; the UI side is implicit. Worth a deliberate UI test for currency rendering across at least USD/CRC/GBP.
- [research.md Decision 4](research.md#decision-4-e2e-test-impact-186-visible-text-selectors-4-poms): The 62% leverage estimate from POM constants is a back-of-envelope projection based on selector frequency. Actual leverage may be lower if many of the 161 `has-text` selectors are in tests that don't go through the 4 high-leverage POMs. Worth budgeting more time for the long tail.
- [spec.md EC-4](spec.md#edge-cases): The 9 empty-state SVG illustrations from spec 011 may contain on-image English text. The spec acknowledges this but **no task explicitly audits the SVG files** — see [Risks and open questions](#risks-and-open-questions) below for how I'd close that gap.
- [tasks.md T072 PDF visual-diff](tasks.md#phase-8-user-story-4--funding-agreement-pdf-in-formal-spanish-priority-p1): The check requires rendering an agreement under the prior `es-CO` config to compare against `es-CR`. This requires temporarily checking out an earlier commit. The task says "use a checkout of `b9da2da` or earlier" but doesn't fully prescribe the workflow — implementer may need to figure it out.

### Risks and open questions (5 min)

- **Coverage gap: empty-state SVG audit not tasked.** [FR-023](spec.md#functional-requirements) and [EC-4](spec.md#edge-cases) require auditing the 9 SVGs for on-image English text, but [tasks.md](tasks.md) doesn't dedicate a task to this. If you accept this review, I propose adding a task in Phase 7 (US2) or as a Polish task. Should I add one?
- **NFR-005 quantified threshold missing.** The NFR says "no measurable LCP/TBT regression vs. spec 011 baseline," but doesn't define a numeric threshold. If the post-translation LCP is +50ms vs. baseline, is that "measurable"? Worth pinning a number during planning. Tasks T002 and T101 capture and compare baselines but accept no threshold.
- **The 109-task / ~110-file modification count is large.** Single coordinated sweep is the chosen approach, but does the team have the review bandwidth for one PR of this size? The brainstorm explicitly considered phased rollout and rejected it. Worth confirming the team's review capacity.
- **CR-specific compliance terminology** (CCSS, Hacienda, SICOP) appears in [`AddSupplierViewModel`](../FundingPlatform.Web/ViewModels/AddSupplierViewModel.cs) and the supplier views. Non-CR Spanish speakers may not catch register nuances on these acronyms. Voice-guide reviewer per [OQ-6](spec.md#open-questions) ideally is CR-region-fluent.
- **Brand rename has out-of-software implications.** "Capital Semilla" replacement extends to (presumably) marketing collateral, business cards, domain names, social media — none of which are this spec's concern. Has marketing/comms been notified that the production deployment will read "Capital Semilla" once this merges?
- **`CultureInfo.ReadOnly(...)` interaction with downstream callers.** If any code path tries to `.Clone()` the read-only culture and mutate, behavior may differ across .NET runtime versions. Worth a quick check during T004 implementation.

---

*Full context in linked [spec](spec.md), [plan](plan.md), [tasks](tasks.md), and [research](research.md).*
