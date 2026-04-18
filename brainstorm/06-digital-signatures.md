# Brainstorm: Digital Signatures

**Date:** 2026-04-18
**Status:** spec-created
**Spec:** specs/006-digital-signatures/

## Problem Framing

The funding-agreement PDF produced in spec 005 has no execution mechanism. Without a signed, verified counterpart, the application cannot legally advance to payment and closure. The initial scope image (`brainstorm/01-initial-scope.png`) listed "Digital signatures" as one of eight out-of-scope-for-later items; four of those eight have shipped as specs 002–005, and this brainstorm selects Digital Signatures as the next feature because it closes the agreement lifecycle and unblocks Payment & Closure, the natural follow-on.

A key pivot emerged during the session: the initial framing imagined an **in-app signature ceremony** (typed, drawn, or OTP-confirmed), but the real requirement is an **out-of-band workflow**. The applicant downloads the PDF, signs it externally with any PDF-stamping tool (Adobe Reader or equivalent), and uploads the signed counterpart for manual reviewer verification. No e-signature library, no provider integration, no cryptography — just file-flow and audit.

## Approaches Considered

### A: In-app simple electronic signature (typed / drawn)
- Pros: fully self-contained, no external tools, clean UX, fast to implement.
- Cons: does not match the actual user intent (applicants prefer their own tools); arguably weaker evidentiary footing than a signed PDF uploaded back.

### B: In-app advanced electronic signature (OTP-verified)
- Pros: stronger identity binding than A; still self-contained; reasonable legal footing for most LatAm jurisdictions.
- Cons: requires OTP infrastructure; still does not match the user's preferred workflow.

### C: External e-signature provider (DocuSign / Adobe Sign / Firmamex / Certicámara)
- Pros: highest off-the-shelf legal footing; certificates of completion; professional UX.
- Cons: vendor cost, vendor lock-in, integration complexity; overkill for the target bar.

### D: Out-of-band signing + upload + manual review (CHOSEN)
- Pros: matches the real user workflow (sign in Adobe Reader, upload back); zero vendor dependency; simplest technically; reuses existing reviewer pipeline and file-storage contracts; keeps the platform self-contained.
- Cons: permanent human visual-verification cost per signed upload; no automated detection of version mismatch or signature presence; no cryptographic evidence.

### E: Qualified / PKI certificate signature (firma electrónica cualificada)
- Pros: highest legal weight; accredited authority backing.
- Cons: significant complexity, niche applicability, not required by the target bar.

## Decision

**Approach D** — out-of-band signing with upload and manual reviewer verification. Chosen because the applicant's real workflow is "sign in Adobe Reader and upload back", and the platform's role is to route the file and keep an audit trail, not to operate a signing ceremony or integrate a provider.

Key design decisions encoded in the spec:

- **Signing parties:** applicant only signs in-system (funder pre-signed in template or out-of-band).
- **Review:** any user with the existing reviewer role from specs 002/003 — no new role introduced.
- **Rejection loop:** applicant re-uploads a corrected signed PDF; no retry cap; no agreement regeneration required.
- **Deadlines / decline:** none — the signing stage stays open indefinitely (consistent with the project's pre-existing no-deadlines pattern noted in spec 004 open thread).
- **Regeneration lockdown:** regeneration permitted until the first signed upload arrives; locked thereafter. Administrative back-out is explicitly out of scope for this feature.
- **Upload constraints:** PDF only, single file, system-configurable size limit (20 MB default). Applicant can withdraw or replace a pending upload before review begins.
- **Testing:** e2e tests (Playwright, per constitution §III) required for four critical journeys: happy path, rejection loop, pre-review replacement, regeneration lockout.

Spec review passed with SOUND status (scores: Completeness 5/5, Clarity 4.5/5, Implementability 5/5, Testability 5/5, Constitution Alignment 5/5).

## Open Threads

- Whether reviewers would benefit from a side-by-side view of the generated agreement vs. the signed upload to aid visual verification — deferred to planning as a UX decision.
- Whether the signed PDF, once executed, should be presented with an execution banner or cover page — likely deferred, purely cosmetic.
- Final upload size limit value (20 MB default proposed; to be confirmed at planning).
- Whether spec 005 is sufficiently precise on which role (reviewer vs. approver) can trigger agreement regeneration; verify during planning so FR-010 has no inherited ambiguity.
- Administrative back-out of the signing stage (resetting a locked agreement) is a deliberately out-of-scope gap at the feature boundary; if operations hits the scenario before an admin tooling spec exists, they will lack a supported path.
- Whether unlimited rejection cycles are acceptable long-term or whether a future reporting/ops-visibility feature should expose and pressure-test the assumption.
- Whether an automated assist (content hash, version marker, or side-by-side diff) should help reviewers catch mismatched signed uploads — currently purely visual.
