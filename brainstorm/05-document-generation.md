# Brainstorm: Document Generation (Funding Agreement)

**Date:** 2026-04-17
**Status:** spec-created
**Spec:** specs/005-funding-agreement-generation/

## Problem Framing

With core submission (spec 001), review/approval (spec 002), supplier evaluation (spec 003), and applicant response/appeal (spec 004) in place, applications now reach a terminal "response resolved" state with a known set of accepted items per applicant. But there is no artifact that captures *what was actually funded*. Downstream lifecycle stages — digital signatures, payment authorization, and application closure — all need a canonical document they can read, sign, process, or archive. Without it, accepted applications sit in the same kind of limbo that spec 004 was designed to eliminate, one stage later.

This is the fifth feature in the decomposition of the full Funding Request & Evaluation Platform SRS, and the first of the "downstream lifecycle" features identified in the initial-scope brainstorm (`brainstorm/01-initial-scope.png`). It produces the Funding Agreement PDF that later specs will build on.

## Approaches Considered

Brainstorming worked through a sequence of design forks. For each fork, the selected option is marked; alternatives were explicitly rejected.

### 1. Document scope

- **A) Funding agreement only (selected)** — a single document type, the formal agreement on accepted items.
- **B) Decision letter + funding agreement** — two documents covering approval communication and the contract.
- **C) Full lifecycle set** — submission receipt, decision letter, funding agreement, appeal resolution letter, closure/handover letter.
- **D) Admin-configurable templates** — document templates (name, trigger event, body) editable by admins; ships seeded with the agreement and decision letter.

Selected A: the funding agreement is the only document actually required to unblock Digital Signatures, Payment, and Closure. Any other document type can be added later, and if two or more types are eventually needed, option D becomes the right shape rather than a second bespoke feature.

### 2. Generation trigger

- **A) Auto on response fully resolved** — no human click.
- **B) Admin action after response resolved (selected)** — a human with authority clicks "Generate agreement".
- **C) Applicant action after response resolved** — applicant pulls their own agreement.
- **D) Auto on first accept, regenerated as appeals resolve** — earliest applicant feedback with multiple versions over time.

Selected B: a manual checkpoint before producing a near-legal artifact, with no version-proliferation complexity. Option D was attractive for UX but introduced multi-version storage and auditing concerns that are premature.

### 3. Template authoring model

- **A) Hardcoded in the codebase (selected)** — Razor view authored by developers; wording changes require a deploy.
- **B) Admin-editable, single active template** — admins edit HTML with placeholders in-app.
- **C) Admin-editable, versioned** — every saved template version archived; each agreement records the version it used.
- **D) Hybrid: hardcoded layout, admin-editable text blocks** — structure in code, named blocks (preamble, T&Cs, closing) in DB.

Selected A: matches YAGNI for a single-funder platform where legal wording is rare and change-controllable. Raised again later as an open question: T&C copy ownership.

### 4. Regeneration policy

- **A) No — generate once, immutable**.
- **B) Yes, with confirmation, overwrite (selected)** — destructive action on an existing file; only latest retained.
- **C) Yes, with full version history**.

Selected B, with a user-driven extension: regeneration rights granted to **both administrators and reviewers** (initial proposal was admin-only). Prior versions are not retained. A future Digital Signatures feature will introduce the lockout rule that prevents regeneration after signing begins.

### 5. Visibility / access

- **A) Applicant + admin only**.
- **B) Applicant + admin + reviewer read-only**.
- **C) Admin only at first, applicant after explicit release**.
- **User-provided answer (selected)** — applicant + admin + reviewer view/download; admin + reviewer regenerate anytime.

Selected the user's extension of option B: reviewers get read/download on reviewed applications *and* regeneration rights. Breaks cleanly from the original admin-only regeneration proposal in Q4.

### 6. All-rejected edge case

- **A) Block generation — no agreement possible (selected)**.
- **B) Allow generation of a no-fund acknowledgment document**.
- **C) Admin chooses at generation time (two variants)**.

Selected A: "Generate agreement" is disabled when zero items are accepted. Closure of all-rejected applications is deferred to the future Payment & Closure feature.

### 7. Rendering stack and storage model (architectural approach)

Three bundles were considered:

- **A) QuestPDF + dedicated `FundingAgreement` aggregate** (recommended during brainstorm): .NET-native fluent layout library, no browser runtime dependency, new aggregate root, synchronous in-request.
- **B) Razor → HTML → Playwright PDF + new aggregate**: reuses the Playwright test-stack dependency at runtime; promotes Chromium to a runtime dependency; same new aggregate.
- **C) QuestPDF + reuse the existing `Document` entity with a `DocumentKind` discriminator**: fewer entities, but conflates upload-lifecycle docs and generated-lifecycle docs in one aggregate.

**Selected: A, with Syncfusion HTML-to-PDF substituted for QuestPDF.** Rationale:
- Dedicated `FundingAgreement` aggregate preserves the Rich Domain Model principle; generated artifacts have a different lifecycle than user-uploaded documents and should not share an aggregate.
- Syncfusion (as a user-driven substitution) keeps Razor/HTML authoring ergonomics without promoting Chromium to a runtime dependency.
- Synchronous generation is acceptable given expected volume and the 3 s p95 target.

## Decision

Ship as **Funding Agreement (single document type), admin/reviewer-triggered, hardcoded Razor template rendered via Syncfusion HTML-to-PDF, persisted through the existing file storage abstraction in a new `FundingAgreement` aggregate; regeneration with confirmation overwrites the prior file; download accessible to applicant + admin + reviewer through an authenticated endpoint with non-disclosing authorization.**

Key behaviors:

- Admin/reviewer triggers generation only when preconditions hold: review closed, applicant response complete, no active appeal, at least one item accepted.
- PDF content: applicant details at generation time, accepted items table with supplier and price, total, funder identity from config, hardcoded T&Cs, empty signature blocks.
- Locale/currency formatting: single configured LatAm default (comma decimal, period thousands).
- Storage: new aggregate with optimistic concurrency; one current agreement per application; no version history.
- Synchronous generation; structured log on success/failure; startup fail-fast on missing Syncfusion license.

Key entities introduced: `FundingAgreement` aggregate. No changes to `Application`, `Item`, `Supplier`, or `Quotation`; they are read only.

## Open Threads

- **T&C copy ownership and delivery path.** The spec requires hardcoded T&Cs but does not commit to who authors the text or when it's delivered. Recommended: establish during planning; plan.md should name the author/owner and a concrete delivery checkpoint.
- **Funder identity shape (OQ-002 in spec).** Single configuration block assumed; a richer `Funder` aggregate may be warranted if multi-funder scenarios surface.
- **Reviewer regeneration rights (OQ-004 in spec).** Granted per user direction during brainstorming; should be re-validated in planning when the full role-scope map is visible — if role-separation concerns arise, restrict to admin-only with minimal change.
- **Syncfusion license acquisition and cost.** Planning/ops coordination needed before implementation can begin.
- **Specific default locale code.** Spec commits to a Latin-American style; planning should pin `es-CO`, `es-MX`, or similar for E2E-test reproducibility.
- **Audit retention for "prior PDF retained after appeal opened" (EC-005).** Spec defers formal retention policy; a compliance-driven spec later will either formalize this or change the overwrite rule.
- **Post-signature regeneration lockout.** Implicitly belongs to the future Digital Signatures spec; mentioned here so it's not missed during that spec's design.
