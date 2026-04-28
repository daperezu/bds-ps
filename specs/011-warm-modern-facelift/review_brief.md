# Review Brief: Warm-Modern Facelift

**Spec:** specs/011-warm-modern-facelift/spec.md
**Generated:** 2026-04-27

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

After spec 008 (Tabler.io migration) gave the platform a consistent partial library, this spec elevates the entire authenticated experience to a "warm-modern premium" finish appropriate for a funding agency serving entrepreneurs (Mercury / Ramp / Notion family). It introduces (1) a new brand identity proposed inside the spec — display name, color story, type pairing, logo direction, voice guide; (2) a refreshed design-token layer that propagates through the spec-008 partial library; (3) a sweep across every authenticated view in which **HTML restructuring is permitted and encouraged where it serves UX/UI quality**; (4) four net-new signature moments — Applicant home dashboard, Application journey timeline, Signing ceremony, Reviewer queue dashboard; (5) a choreographed-but-tasteful motion system that respects `prefers-reduced-motion`; (6) a 9-scene empty-state illustration set as the brand's most visible carrier of warmth.

## Scope Boundaries

- **In scope:** brand identity (display name, palette, type, logo, voice); design tokens in `wwwroot/css/tokens.css`; motion catalog with reduced-motion contract; sweep of every authenticated view including Login/Register/ChangePassword/AccessDenied; four signature wow moments; 9 illustration SVGs; Page Object Model overhaul to semantic locators; net-new E2E coverage for the four wow moments and a reduced-motion test.
- **Out of scope:** PDF templates byte-identical (spec 008 carve-out preserved); localization (still deferred); CDN-served assets (everything vendored under `wwwroot/lib/`); public marketing surface; product rename (display brand only — code stays `FundingPlatform`); dark mode; real-time push / SignalR; reviewer ops surfaces (assignment UI, bulk actions, saved views, cross-reviewer visibility); general-purpose animation libraries; sound, social sharing, achievements, streaks; per-funded-item milestones on the journey component; **schema changes** (zero dacpac edits — escape hatch via `speckit-spex-evolve`).
- **Why these boundaries:** preserve spec-008 invariants; keep the lift focused on identity + signature surfaces; respect Constitution YAGNI; let real-time, ops, and i18n be their own future specs with their own review cycles.

## Critical Decisions

### Single mega-spec packaging (mirrors spec 008)
- **Choice:** One spec covers brand + tokens + sweep + all four wow moments. Same risk profile as spec 008.
- **Trade-off:** Largest single review surface vs. fastest visible lift and a single coherent brand decision.
- **Feedback:** Is the bundling right, or would you prefer to split into "tokens + sweep" first and "wow moments" later for a smaller review window?

### Equal lift across applicant, reviewer, and admin roles
- **Choice:** Warm-modern treatment is applied uniformly. Reviewer surfaces preserve density discipline (`--space-2` vs. `--space-4`) and inline a micro journey timeline instead of the status-pill column.
- **Trade-off:** Larger scope than applicant-only, but avoids visible style divergence between roles.
- **Feedback:** Confirm reviewer queue density discipline (FR-060) is acceptable, and that removing the status-pill column from queue rows in favor of the inline micro journey is the right call.

### HTML restructuring + E2E rewrite are explicitly in scope
- **Choice:** Per saved feedback, "UX quality > E2E selector stability." Page Objects are rewritten against new HTML; locators migrate to ARIA roles + `data-testid`; tests get a quality pass alongside.
- **Trade-off:** Real implementation effort spent on test rewrites vs. a test suite that mirrors the new quality bar (rather than dragging behind it).
- **Feedback:** Confirm appetite for the E2E rewrite cost.

### Schema-unchanged constraint with `spex-evolve` escape hatch
- **Choice:** FR-067 + SC-018 forbid dacpac edits. Wow-moment data needs are covered by projections over `VersionHistory` + existing aggregates.
- **Trade-off:** May force planning to use query-time aggregation where a denormalized column would be faster.
- **Feedback:** Comfortable with this constraint, or would you rather pre-authorize specific schema additions?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Brand display name proposed inside this spec
- **Decision:** Spec proposes "Forge" or "Ascent" as candidates for the display brand, with "keep `FundingPlatform`" as the fallback. Final selection is user sign-off (FR-072 / SC-020).
- **Why this might be controversial:** Brand naming is an org-level decision that often involves marketing/leadership. Proposing it inside an engineering spec may feel like jumping the gun.
- **Alternative view:** Defer all brand decisions to a separate brand-identity workstream and have this spec adopt placeholder tokens.
- **Seeking input on:** Is the in-spec brand proposal appropriate, or should brand land separately?

### Single mega-spec vs. split (revisits the packaging decision)
- **Decision:** Single mega-spec with seven user stories. Approved during brainstorm.
- **Why this might be controversial:** Spec 008 was also a mega-spec and shipped, but the review burden was real. A reviewer who didn't see the brainstorm may push back on the size.
- **Alternative view:** Split into 011 (brand + tokens + sweep) and 012 (signature moments).
- **Seeking input on:** Is the bundle still acceptable, or would you prefer a split now?

### Visual-regression tooling deferred (still)
- **Decision:** v1 uses manual side-by-side review on `SWEEP-CHECKLIST.md`. Same call as spec 008.
- **Why this might be controversial:** Mega-spec sweep is exactly when visual regression buys the most value.
- **Alternative view:** Adopt Playwright screenshot comparison or Percy/Chromatic in this spec; the cost is one-time and the upside compounds.
- **Seeking input on:** Is "deferred to a future spec" still the right call, or is now the right time?

### Removing the status-pill column from reviewer queue rows
- **Decision:** FR-056 replaces the status-pill column with an inline US2 micro journey timeline.
- **Why this might be controversial:** Reviewers may have built habits around scanning the pill column.
- **Alternative view:** Keep the pill column AND add the micro journey timeline (loses some density but preserves the scan pattern).
- **Seeking input on:** Confirm the column removal works for triage, or veto and ask the spec to keep both.

### Asset budget of 400 KB gz (FR-074, SC-016)
- **Decision:** Combined wire weight ≤ 400 KB gz for fonts + 9 SVGs + canvas-confetti.
- **Why this might be controversial:** Tight on three families of fonts. If subsetting under-delivers, the budget binds.
- **Alternative view:** Drop JetBrains Mono (use system mono for application IDs) to free ~50 KB.
- **Seeking input on:** Is the budget right, or would you raise it to 500 KB / drop a font?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| New design-token file | `wwwroot/css/tokens.css` | Sole home for all CSS custom properties |
| Brand voice deliverable | `BRAND-VOICE.md` (in spec dir) | One-page voice guide |
| Sweep checklist | `SWEEP-CHECKLIST.md` (in spec dir) | Drives manual review per FR-023 |
| Display brand candidates | *Forge*, *Ascent*, or keep `FundingPlatform` | User sign-off pending |
| Display fonts | Fraunces (display), Inter (body), JetBrains Mono | Self-hosted under `wwwroot/lib/fonts/` |
| New partials | `_ApplicantHero`, `_ApplicationCard`, `_ResourcesStrip`, `_ApplicationJourney`, `_SigningCeremony`, `_ReviewerHero`, `_ReviewerQueueRow` | One per signature surface |
| Razor helper | `Illustration("scene-key")` | Embeds the right SVG with correct aria semantics |
| Illustration scene-keys | `seed`, `folders-stack`, `open-envelope`, `connected-nodes`, `calm-horizon`, `soft-bar-chart`, `off-center-compass`, `gentle-disconnected-wires`, `magnifier-on-empty` | One per scene in the v1 set |
| Application-layer projector | `JourneyProjector` (or equivalent) | Projects Application → JourneyViewModel |
| Confetti library | e.g. `canvas-confetti`, ≤ 5 KB gz | Single allowed motion library |

## Open Questions

- [ ] **Brand display name selection** (Forge / Ascent / keep `FundingPlatform`) — user sign-off gate.
- [ ] **Exact hex values for the color story** — pinned during planning by designer pass.
- [ ] **Type-scale ramp specifics** — pinned during planning after density audit on densest surfaces.
- [ ] **Tabler `--tblr-*` bridge aggressiveness** — inventory pinned during planning.
- [ ] **Designer source for the 9 illustration SVGs** — in-team / commission / adapted-from-open.
- [ ] **Ceremony view-vs-partial implementation choice** — pinned during planning (FR-044).
- [ ] **Ceremony fresh-vs-bookmark mechanism** — TempData / query / one-shot session token (FR-047).
- [ ] **Journey-stage resolver placement** — extend `IStatusDisplayResolver` vs. sibling resolver (FR-036).
- [ ] **Visual-regression tooling adoption** — defer or now (recurring open question from spec 008).
- [ ] **Login/Register tone** — clean single-CTA vs. light marketing hero.

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Mega-spec scope creep during implementation | High | Explicit out-of-scope guardrails (FR-068..FR-071); SWEEP-CHECKLIST.md drives manual sweep verification |
| PDF carve-out drift (accidental edit to `Document.cshtml` or `_FundingAgreementLayout.cshtml`) | High | FR-020, SC-014, edge case enumerated; PDF byte-identity check in CI or manual verification |
| Asset budget overrun (fonts + illustrations + confetti exceed 400 KB) | Medium | SC-016 verifiable; subsetting strategy pinned during planning; option to drop JetBrains Mono if budget binds |
| Reduced-motion contract incomplete or selectively applied | Medium | SC-012 requires a dedicated Playwright test running with the reduce-motion option |
| WCAG AA contrast regression on the warm-tuned palette | Medium | SC-013 requires axe-playwright check on all four wow-moment views and the layout shell |
| Reviewer triage slowdown from removing the status-pill column | Medium | Open question for reviewer feedback; can revert by adding the column back behind the inline timeline if practice diverges from spec |
| Schema-unchanged constraint blocks an otherwise-optimal data shape | Medium | Documented escape hatch via `speckit-spex-evolve` (FR-067) |
| Performance regression beyond the +10% LCP/TBT budget | Medium | SC-015 verifiable; FR-073 requires baseline capture before implementation begins |
| E2E rewrite cost overruns budget | Medium | Per-saved-feedback acceptance that this cost is paid in exchange for "best-of-the-best UX" |
| Brand sign-off blocks merge | Low | FR-072 / SC-020 makes the gate explicit; user is the approver |

---
*Share with reviewers before implementation.*
