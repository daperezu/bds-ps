# Review Brief: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Spec:** specs/012-es-cr-localization/spec.md
**Generated:** 2026-04-29

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Pin the platform to a single fixed locale (`es-CR`), translate every user-facing surface to formal Costa Rican Spanish (warm-modern voice, formal `usted`), and rename the product from "Forge" to "Capital Semilla." The work ships as a single coordinated sweep across ~72 Razor views, the status registry, framework messages (validation + Identity), the Funding Agreement PDF, the brand-mark assets, and the Playwright E2E test suite. Closes three localization-deferral threads carried forward since spec 008 and the brand sign-off thread from spec 011.

## Scope Boundaries

- **In scope:** Razor views/partials/layouts; status enum + journey-stage display; DataAnnotation `ErrorMessage`; ASP.NET MVC `ModelBindingMessageProvider`; ASP.NET Identity `IdentityErrorDescriber`; controller-emitted TempData strings; Funding Agreement PDF body; brand wordmark and mark SVGs; HTML `lang` attribute; page titles; `aria-label` / `title` / `alt` / `placeholder`; the JS namespace `ForgeMotion` → `PlatformMotion`; the `tokens.css` brand-reference comment; E2E test visible-text assertions.
- **Out of scope:** Multi-language support (no toggle, no resx, no `IStringLocalizer`); URL route slugs; code identifiers; route paths; log messages; exception messages; JSON config keys; DB schema; CSV exports (stay `InvariantCulture`); already-signed Funding Agreement PDFs (immutable per spec 006); applicant-entered free-text content; brand-mark visual redesign beyond the wordmark text swap; future spec 013 (messaging), future spec 014 (notifications), future spec 015 (public marketing).
- **Why these boundaries:** The "code stays English / UI is Spanish" rule keeps the codebase searchable, debuggable, and compatible with a hypothetical future i18n layer without forcing any swappable-locale machinery now (NFR-003). CSV stays invariant because consumers are machines, not people. Signed PDFs stay frozen because spec 006's immutability rule is binding.

## Critical Decisions

### Replace in place, no resx
- **Choice:** Hard-code Spanish inline at the same view-model / partial-parameter / status-registry seams that specs 008 and 011 already designed. No `IStringLocalizer`, no `.resx`, no culture middleware for copy.
- **Trade-off:** Lowest implementation cost; if a future spec ever wants multi-language, it pays the extraction cost then. NFR-003 forbids preemptive scaffolding.
- **Feedback:** Comfortable with deferring the i18n layer if/when a real multi-language requirement appears?

### Strict `es-CR` formatting (period decimal, comma thousands)
- **Choice:** Use `.NET`'s `es-CR` `CultureInfo` as-is. Numbers render `1,234.56`; dates `dd/MM/yyyy`. This is a regional shift from spec 005's prior `es-CO` baseline (`1.234,56`).
- **Trade-off:** Authentic to CR business conventions but changes existing PDF format for any unsigned drafts.
- **Feedback:** Confirm the regional shift is acceptable for any in-flight unsigned agreements.

### Rename product to "Capital Semilla"
- **Choice:** Replace "Forge" everywhere user-facing. Two-word brand may need designer rework on the wordmark SVG; the spec ships with a textual placeholder if designer rework hasn't landed.
- **Trade-off:** Coherent regional brand vs. a one-shot designer dependency.
- **Feedback:** Confirm "Capital Semilla" as the final brand name (the spec assumes it is).

### Translate E2E test assertions to Spanish text
- **Choice:** Update every Playwright visible-text assertion to match production Spanish copy. Closes spec 011's selector-strategy thread by adopting "visible Spanish text is the contract."
- **Trade-off:** A larger one-time edit but tests stay readable and copy stays the contract. Forgoes a parallel `data-testid` push.
- **Feedback:** Comfortable accepting visible-text assertions as the long-term contract instead of investing in `data-testid` plumbing right now?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Single coordinated sweep vs. phased rollout
- **Decision:** One spec, one branch, one merge for ~72 views + framework messages + PDF + tests.
- **Why this might be controversial:** Large diff; if voice tone needs retuning mid-flight, rework touches everything.
- **Alternative view:** Phase by audience (applicant first → reviewer/admin → PDF) to allow voice-guide tuning on phase 1 evidence. The brainstorm explored this (Approach B / C) and chose A because spec 008 and 011 succeeded with the single-sweep pattern and phasing introduces mixed-language windows.
- **Seeking input on:** Does anyone want to revisit the phasing decision before planning starts?

### Hard-cut JS namespace `ForgeMotion` → `PlatformMotion` (no alias)
- **Decision:** No backwards-compatible alias is left in place.
- **Why this might be controversial:** A single missed caller is a runtime break.
- **Alternative view:** A 1-line alias `window.ForgeMotion = window.PlatformMotion;` would cost nothing and be removed in a follow-up spec.
- **Seeking input on:** Comfortable with the hard cut, given the audit will be exhaustive (only 7 callers exist today)?

### Brand-mark SVG can ship as a textual placeholder
- **Decision:** If the designer follow-up hasn't landed by implementation, a textual rendering of "Capital Semilla" in the Fraunces display font is acceptable as a temporary state.
- **Why this might be controversial:** Visual brand integrity matters; a textual placeholder is plainly not a designed mark.
- **Alternative view:** Block the spec on designer delivery; better to delay than ship a placeholder.
- **Seeking input on:** Acceptable to ship the spec with a textual placeholder if the designer is delayed?

### CSV exports stay English / `InvariantCulture`
- **Decision:** Spec 010's CSV export functionality is explicitly excluded from translation; numeric/date values continue as `InvariantCulture` for machine-readability.
- **Why this might be controversial:** Some consumers may expect CSVs to be human-readable in Spanish too.
- **Alternative view:** Translate CSV column headers (just the headers, not the values) so a CR analyst opening a CSV in Excel sees Spanish labels.
- **Seeking input on:** If CSV column headers should be translated as a follow-up, when?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Product brand (display) | Capital Semilla | Replaces "Forge" everywhere user-facing |
| Product brand (code) | FundingPlatform | Unchanged — code namespaces, project names, config keys stay |
| JS namespace | `PlatformMotion` | Replaces `ForgeMotion`; brand-neutral identifier |
| Locale | `es-CR` | Pinned in `RequestLocalization` middleware via constant |
| Funding Agreement locale config | `FundingAgreement:LocaleCode = "es-CR"` | Replaces prior `es-CO` placeholder |
| Voice guide artifact | `specs/012-es-cr-localization/voice-guide.md` | Spec-directory artifact, authored before per-view rewrites |

## Open Questions

- [ ] (OQ-1) Glossary finalization — final term choices for application/review/funding agreement/send back/etc.
- [ ] (OQ-2) Footer tagline — exact Spanish phrasing for "built for entrepreneurs"
- [ ] (OQ-3) Designer SVG follow-ups — whether wordmark rework and on-image audit block merge
- [ ] (OQ-4) Tabler vendor JS string audit — whether any built-in copy needs override
- [ ] (OQ-5) Performance baseline — capture LCP/TBT pre-translation if not already done
- [ ] (OQ-6) Voice-guide reviewer — same person as spec 011 or new CR-region reviewer
- [ ] (OQ-7) Page-title direction — `[Page] - Capital Semilla` (matches today) or reversed
- [ ] (OQ-8) Hard-pin vs. config override of culture — recommend hard-pin via constant
- [ ] (OQ-9) `PlatformMotion` final identifier — confirm vs. `AppMotion` / `SeedMotion`

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Missed English string slips into production | Med | SC-001 regex sweep + Spanish-speaking reviewer walkthrough |
| Spanish text overflow (~25% longer) breaks tight layouts | Med | EC-9 visual review per surface; Tabler responsive utilities |
| `es-CR` `CultureInfo` quirks vs. assumed convention | Med | Assumption flagged; planning verifies and adds explicit `NumberFormatInfo` override only if needed |
| Number-separator shift breaks existing PDF layout | Low–Med | One-time visual-diff check against a representative pre-deployment PDF |
| Designer SVG rework delayed past implementation | Low | Textual placeholder is acceptable per FR-023 / EC-7 |
| Tabler vendor JS strings leak English | Low | EC-5 audit step; override at component instantiation if found |
| `ForgeMotion` consumer outside the 7 known callers | Low | Hard cut, but exhaustive grep across the codebase confirms only 7 callers |

---

*Share with reviewers before implementation.*
