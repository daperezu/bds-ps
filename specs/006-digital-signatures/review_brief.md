# Review Brief: Digital Signatures for Funding Agreement

**Spec:** [spec.md](./spec.md)
**Generated:** 2026-04-18

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

After the Funding Agreement PDF is generated (spec 005), the applicant must execute it to unlock the future Payment & Closure stage. This feature provides an **out-of-band signing workflow**: the applicant downloads the generated PDF, signs it externally using any PDF-stamping tool of their choice (Adobe Reader, Preview, etc.), and uploads the signed counterpart back to the platform. A reviewer (same role as specs 002/003) then performs a **visual verification** and either approves the upload (transitioning the application to "Agreement Executed") or rejects it with a comment (returning the applicant to re-sign). The platform does no in-app signature capture, no e-signature provider integration, and no cryptographic verification — it is a file-flow and audit system.

## Scope Boundaries

- **In scope:** download of generated PDF, upload of signed PDF (single PDF, configurable size limit), applicant-side pre-review withdraw/replace, reviewer approve/reject with comment, pre-upload agreement regeneration, post-first-upload regeneration lockdown, comprehensive audit trail, state transition to "Agreement Executed", e2e test coverage for the four critical journeys.
- **Out of scope:** in-app signature capture (typed/drawn/OTP), external e-signature provider integration (DocuSign, Adobe Sign, Firmamex, Certicámara), PKI / qualified electronic signatures, cryptographic signature verification, signing deadlines, applicant explicit-decline action, notifications on signing events, payment processing, administrative back-out tooling, execution certificates, watermarking, and side-by-side comparison UX.
- **Why these boundaries:** The client's target legal bar is satisfied by a reviewed-upload model; avoiding e-signature providers keeps the platform self-contained and avoids vendor cost; deferring notifications/payment/reporting matches the staged rollout of the overall platform.

## Critical Decisions

### Mechanism: out-of-band signing, not in-app
- **Choice:** Applicant signs externally with their tool of choice; platform handles file-flow only.
- **Trade-off:** Simpler implementation and zero vendor dependency, but reviewer effort is permanent (every signed upload requires human visual verification).
- **Feedback:** Is visual review by a human an acceptable steady-state cost, or do we expect volume growth that will force a later upgrade to an e-signature provider?

### Reviewer identity: reuse of the spec 002/003 reviewer role
- **Choice:** Any user with the existing reviewer role performs the signed-upload verification — no new role is introduced.
- **Trade-off:** Less role sprawl and zero onboarding overhead, but reviewers' responsibilities widen to cover agreement execution in addition to application assessment.
- **Feedback:** Is there any operational reason to separate "application reviewer" from "agreement reviewer" (e.g., funder-side audit, segregation of duties)?

### Rejection loop: unlimited retries, no regeneration
- **Choice:** When a signed upload is rejected, the applicant can upload a new signed PDF. No retry cap, no automatic regeneration of the underlying agreement.
- **Trade-off:** Simple and friendly to the applicant, but a malformed-signer could loop indefinitely; operational visibility is deferred to a future reporting spec.
- **Feedback:** Are we comfortable leaving pathological loops to ops discovery rather than a system-enforced cap?

### No deadlines, no decline: stage stays open indefinitely
- **Choice:** The signing stage has no timeout, no auto-decline, and no explicit applicant-decline action at this step.
- **Trade-off:** Consistent with this project's existing "no deadlines" pattern (see spec 004 open thread about operational visibility) and avoids stranding applicants who genuinely need more time; risk is operational (stuck applications), not functional.
- **Feedback:** Does the funder need a contractual response window that the platform should enforce, or is the platform's current pattern acceptable?

### Lockdown timing: immediately after first signed upload
- **Choice:** The generated agreement PDF can be regenerated (invalidating the prior one) only until the first signed upload is accepted; thereafter, regeneration is blocked.
- **Trade-off:** Protects the signed document chain and matches legal expectation that a signed instrument is not unilaterally regenerated; any post-signature correction requires out-of-band administrative back-out.
- **Feedback:** Acceptable, or should regeneration remain possible until approval (at the cost of potentially invalidating an applicant's in-flight signed upload)?

## Areas of Potential Disagreement

### No system-imposed retry cap on rejections
- **Decision:** Unlimited rejection/re-upload cycles.
- **Why this might be controversial:** A determined bad actor or a confused applicant could generate audit-log noise and reviewer workload indefinitely.
- **Alternative view:** Cap at N rejections (e.g., 5), then escalate to manual intervention or back to "appeal" stage (spec 004).
- **Seeking input on:** Whether the absence of a cap is acceptable given that reviewer capacity is not currently a concern and reporting will later expose stuck cases.

### Administrative back-out deliberately out of scope
- **Decision:** When the agreement is locked post-signed-upload but a correction is actually needed, the spec defers this to "administrative tooling" not built here.
- **Why this might be controversial:** This is a real operational hole at feature boundary. If the need arises before the admin tooling ships, ops has no supported path.
- **Alternative view:** Include a minimal "admin reset" action in this feature, gated by a new permission.
- **Seeking input on:** Whether punting is acceptable given that the scenario is rare, or whether a skeleton admin action should be added.

### Reviewer verification is purely visual
- **Decision:** No tooling assists the reviewer in spotting mismatches between the generated and signed documents; they open the signed PDF and judge.
- **Why this might be controversial:** Visual-only review is error-prone at scale and does not detect, for example, a signed PDF of an earlier generated version.
- **Alternative view:** Add automated checks (file-hash of base content, embedded version marker, side-by-side diff in reviewer UI). A side-by-side view is already listed in Open Questions for planning.
- **Seeking input on:** Whether to commit to any automated assist in this spec or leave it to planning as a UX decision.

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Final state after approval | Agreement Executed | Terminal state for this feature; handoff point for future Payment & Closure |
| Stage name | Signing stage | Pipeline stage following generation |
| Pending state | Signed Upload Pending Review | Applicant has uploaded, awaiting reviewer |
| Return state after rejection | Ready to upload signed agreement | Applicant can upload again |
| Lockdown | Agreement Lockdown Flag | Per-application flag flipped at first signed upload |
| Key entities | Signed Upload, Signing Review Decision, Signing Audit Entry, Agreement Lockdown Flag | Domain-layer concepts |

## Open Questions

- [ ] Should reviewers have a side-by-side view of the generated PDF vs. the signed upload to aid visual verification? (UX decision — planning)
- [ ] Should the signed PDF, post-execution, be served with an execution banner / cover page? (Cosmetic — likely deferred)
- [ ] What is the initial configured upload size limit? (Default 20 MB proposed — confirm at planning)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Applicant uploads wrong-version signed PDF after regeneration | Medium — applicant confusion, one rejection cycle | FR-011 detects version mismatch at intake and prompts re-download |
| Concurrent reviewer-approve vs applicant-withdraw/replace race | Medium — partial state or silent overwrite | FR-015 mandates version-stamp concurrency; losing action returns a clear conflict error |
| Indefinitely stuck signing stage (applicant never uploads) | Low–Medium functionally, potential Medium operationally | Accepted per project pattern (no deadlines); ops visibility deferred to reporting spec |
| Post-signature correction required but admin tooling not yet shipped | Low (rare) but hard-impact when it hits | Spec explicitly defers this to administrative tooling — a known gap, acknowledged in Out of Scope |
| Reviewer visually approves a mismatched signed PDF | Medium — executed agreement references wrong content | Mitigated by reviewer discipline; a future automated assist (side-by-side or hash check) is an open question |

---
*Share with reviewers before implementation.*
