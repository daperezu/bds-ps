# Sweep Checklist — Spec 011 Warm-Modern Facelift

**Source**: spec.md §FR-018 (sweep inventory) + FR-017 (seven swept criteria).

For every view below, tick each of the seven criteria. Voice-guide compliance is checked
against `BRAND-VOICE.md`. Token compliance is checked against `tokens.css`. PDF carve-outs
are explicitly excluded — they appear here only to record the carve-out fact.

## Seven swept criteria

1. **No raw hex/px outside tokens** — `tokens.css` is the only place hex literals live (FR-009, SC-001).
2. **No inline `style=`** — every styling rides classes (FR-010, SC-002).
3. **Correct partial usage** — status displays use `_StatusPill`; empty states use `_EmptyState` with a US7 illustration; action groups use `_ActionBar`; destructive actions use `_ConfirmDialog`.
4. **Voice-guide compliant copy** — headings, labels, CTAs, microcopy reviewed against `BRAND-VOICE.md`.
5. **Correct typography roles** — page heading uses `--font-display` + `--type-heading-*` token; body uses `--font-body`.
6. **Semantically restructured HTML where it improves UX** — restructuring is permitted (FR-019).
7. **Stable semantic locators present** — ARIA roles + accessible names preferred; `data-testid` only as fallback (FR-021).

## Inventory

| Group | View | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|-------|------|---|---|---|---|---|---|---|
| Account | Login.cshtml |   |   |   |   |   |   |   |
| Account | Register.cshtml |   |   |   |   |   |   |   |
| Account | ChangePassword.cshtml |   |   |   |   |   |   |   |
| Account | AccessDenied.cshtml |   |   |   |   |   |   |   |
| Home | Index.cshtml (replaced by ApplicantDashboard for Applicant role) |   |   |   |   |   |   |   |
| Home | ApplicantDashboard.cshtml (NEW US1) |   |   |   |   |   |   |   |
| Application | Index.cshtml (folders-stack empty) |   |   |   |   |   |   |   |
| Application | Details.cshtml (hosts US2 Full) |   |   |   |   |   |   |   |
| Application | Create.cshtml |   |   |   |   |   |   |   |
| Application | Edit.cshtml |   |   |   |   |   |   |   |
| Application | Delete.cshtml |   |   |   |   |   |   |   |
| Item | Index.cshtml (folders-stack empty) |   |   |   |   |   |   |   |
| Item | Details / Create / Edit |   |   |   |   |   |   |   |
| Quotation | Index.cshtml (open-envelope empty; spec-010 currency rendering preserved) |   |   |   |   |   |   |   |
| Quotation | Details / Create / Edit |   |   |   |   |   |   |   |
| Supplier | Index.cshtml (connected-nodes empty) |   |   |   |   |   |   |   |
| Supplier | Details / Create / Edit |   |   |   |   |   |   |   |
| Review | Index.cshtml → REPLACED by QueueDashboard.cshtml (US4) |   |   |   |   |   |   |   |
| Review | Review.cshtml (gets US2 Full embed) |   |   |   |   |   |   |   |
| Review | GenerateAgreement.cshtml |   |   |   |   |   |   |   |
| Review | SigningInbox.cshtml |   |   |   |   |   |   |   |
| ApplicantResponse | Index + action surfaces |   |   |   |   |   |   |   |
| FundingAgreement | Index.cshtml |   |   |   |   |   |   |   |
| FundingAgreement | Details.cshtml |   |   |   |   |   |   |   |
| FundingAgreement | Generate.cshtml |   |   |   |   |   |   |   |
| FundingAgreement | Sign/Ceremony.cshtml (NEW US3) |   |   |   |   |   |   |   |
| FundingAgreement | **Document.cshtml** | CARVE-OUT — NOT TOUCHED (FR-020) |
| FundingAgreement | **_FundingAgreementLayout.cshtml** | CARVE-OUT — NOT TOUCHED (FR-020) |
| Admin | Index.cshtml |   |   |   |   |   |   |   |
| Admin | Users / Roles / SystemConfigurations |   |   |   |   |   |   |   |
| Admin | Reports/* (soft-bar-chart empty) |   |   |   |   |   |   |   |
| Shared | _Layout.cshtml |   |   |   |   |   |   |   |
| Shared | _AuthLayout.cshtml |   |   |   |   |   |   |   |
| Shared | Error.cshtml (gentle-disconnected-wires for 500; off-center-compass for 404) |   |   |   |   |   |   |   |

## Carve-outs (must NOT be touched)

- `src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml`
- `src/FundingPlatform.Web/Views/Shared/_FundingAgreementLayout.cshtml`

Verified by `scripts/verify-pdf-carveouts.sh` — `git diff main` against these files MUST be empty.

## Sign-off

- [ ] Token gate passes (`scripts/verify-tokens.sh`)
- [ ] Illustration gate passes (`scripts/verify-illustrations.sh`)
- [ ] PDF carve-out gate passes (`scripts/verify-pdf-carveouts.sh`)
- [ ] Asset budget gate passes (`scripts/verify-asset-budget.sh`)
- [ ] Reduced-motion test passes (`tests/.../Motion/ReducedMotionTests.cs`)
- [ ] Contrast tests pass (`tests/.../Accessibility/ContrastTests.cs`)
- [ ] Brand sign-off recorded in PR description (FR-072, SC-020)
