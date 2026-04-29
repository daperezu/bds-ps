# Quickstart: Costa Rican Spanish Localization & Capital Semilla Rebrand

**Spec:** [spec.md](./spec.md) · **Plan:** [plan.md](./plan.md) · **Research:** [research.md](./research.md)

This is a getting-started guide for an implementer (or AI agent) picking up spec 012. It covers what to do first, how to verify the locale, and how the voice guide gates per-view rewrites.

---

## Day-1 Setup

### Prerequisites

You should already have:

- The repo cloned at `/mnt/D/repos/bds-ps` with `.NET 10 SDK`, Aspire workloads, and Playwright browsers installed (per `CLAUDE.md`).
- The `012-es-cr-localization` feature branch checked out.

```bash
git checkout 012-es-cr-localization
git pull
dotnet restore
```

### Smoke check: build green

Before touching any copy, confirm the codebase builds and the existing E2E suite is at the pre-translation baseline (the suite will be entirely English-asserted at this point):

```bash
dotnet build
# Optional: capture LCP/TBT baseline before any rewrites land (NFR-005, OQ-5).
```

---

## The Three Day-1 Commits (in order)

The plan calls out three deliverables that MUST land before any per-view rewrite touches the diff:

### Commit 1 — Voice guide skeleton

```bash
# Create the voice guide as the very first commit on this branch.
# Required for SC-009 ("voice guide commit lands BEFORE any per-view rewrite commit").

cat > specs/012-es-cr-localization/voice-guide.md <<'EOF'
# Voice Guide: Costa Rican Spanish (Capital Semilla)

## Register
- Address: formal `usted`. Never `tú` / `vos`.
- Why: institutional/business platform; CR business convention.

## Tone
- Warm: cordial without being colloquial.
- Modern: concise, encouraging, low corporate jargon.
- Pragmatic: surface the user's next action; respect their time.
- (Mirror of spec 011's English voice descriptors.)

## Glossary

| English | Spanish (chosen) | Rejected | Rationale |
|---|---|---|---|
| application | solicitud | aplicación | "solicitud" is the CR-business norm for grant/funding requests |
| applicant | solicitante | aplicante | (rationale) |
| review | revisión | evaluación | (rationale) |
| reviewer | revisor / revisora | evaluador | (rationale) |
| approver | aprobador / aprobadora | (none) | direct mapping |
| supplier | proveedor / proveedora | (none) | direct mapping |
| quotation | cotización | (none) | CR norm |
| funding agreement | convenio de financiamiento | acuerdo de financiación | (rationale) |
| send back | devolver | regresar | (rationale) |
| appeal | apelación | (none) | direct mapping |
| approve | aprobar | (none) | direct mapping |
| reject | rechazar | denegar | (rationale) |
| sign | firmar | (none) | direct mapping |
| funded | financiada | aprobada | distinguishes "funded" state from "approved" |

(Expand as voice owner reviews. Final wording closes during voice-guide authoring.)

## Example Pairs

| Surface | English | Spanish |
|---|---|---|
| Login screen | Welcome back | Bienvenido de vuelta |
| Empty-state caption | All clear — nothing's awaiting your review. | Todo en orden — no tiene revisiones pendientes. |
| Status pill | Under Review | En revisión |
| Validation error | This field is required | Este campo es obligatorio |
| Funding Agreement clause | The parties agree as follows: | Las partes acuerdan lo siguiente: |
| TempData success | Application submitted. | Solicitud enviada. |
| Identity error | Incorrect password. | Contraseña incorrecta. |
| Page title | Dashboard - Capital Semilla | Inicio - Capital Semilla |

EOF

git add specs/012-es-cr-localization/voice-guide.md
git commit -m "012: voice guide skeleton (must land before per-view rewrites)"
```

(Voice owner reviews and refines this artifact. Subsequent commits update the glossary/examples; per-view rewrites cite the latest voice-guide commit hash in their PR descriptions.)

### Commit 2 — Locale infrastructure

Land the request-culture pinning, format overrides, ModelBinding messages, and Identity error describer. **No copy translation yet**, just the runtime plumbing.

Files touched:
- `src/FundingPlatform.Web/Program.cs` — add `RequestLocalization` middleware, configure `MvcOptions.ModelBindingMessageProvider`, register `EsCrIdentityErrorDescriber`.
- `src/FundingPlatform.Web/Localization/EsCrCultureFactory.cs` — new helper that builds the cloned + format-overridden `CultureInfo` (per research Decision 1).
- `src/FundingPlatform.Infrastructure/Identity/EsCrIdentityErrorDescriber.cs` — new subclass (per data-model.md §3).
- `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` — `<html lang="es-CR">` declaration.
- `src/FundingPlatform.Web/Views/Shared/_AuthLayout.cshtml` — same.
- `src/FundingPlatform.Web/appsettings.json` — `"FundingAgreement:LocaleCode": "es-CR"`.
- `src/FundingPlatform.Web/appsettings.Development.json` — same.
- `src/FundingPlatform.AppHost/AppHost.cs` — `localeCode` default flips.
- `src/FundingPlatform.Application/Options/FunderOptions.cs` — default flips.

### Commit 3 — Brand rename + JS namespace cut

Independent of copy translation. Touches few files but every page visually:

- `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` — title suffix, header brand link, footer.
- `src/FundingPlatform.Web/wwwroot/lib/brand/wordmark.svg` — `<text>` content + `aria-label`.
- `src/FundingPlatform.Web/wwwroot/lib/brand/mark.svg` — `aria-label`.
- `src/FundingPlatform.Web/wwwroot/js/motion.js` — `root.ForgeMotion =` → `root.PlatformMotion =`.
- `src/FundingPlatform.Web/wwwroot/js/facelift-init.js` — six callers.
- `src/FundingPlatform.Web/wwwroot/css/tokens.css:150` — comment.

After this commit, the platform reads "Capital Semilla" everywhere user-visible; copy is still English.

---

## Verifying Locale Behavior

After Commit 2, confirm the runtime culture is wired correctly:

```bash
dotnet run --project src/FundingPlatform.AppHost
# Open http://localhost:<port>/ and view source.
# Confirm <html lang="es-CR">.
# Confirm any rendered date format (e.g., a draft created date) shows dd/MM/yyyy.
# Confirm any rendered numeric (e.g., a quotation price) shows 1,234.56.
```

Or run a quick console probe (matches research Decision 1):

```bash
mkdir -p /tmp/es-cr-probe && cd /tmp/es-cr-probe
cat > Program.cs <<'EOF'
using System.Globalization;
var ci = (CultureInfo)CultureInfo.GetCultureInfo("es-CR").Clone();
ci.NumberFormat.NumberDecimalSeparator = ".";
ci.NumberFormat.NumberGroupSeparator = ",";
ci.NumberFormat.CurrencyDecimalSeparator = ".";
ci.NumberFormat.CurrencyGroupSeparator = ",";
ci.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";

var num = 1234567.89;
var dt = new DateTime(2026, 4, 29);
Console.WriteLine($"N: {num.ToString("N", ci)}");      // expect: 1,234,567.890
Console.WriteLine($"C: {num.ToString("C", ci)}");      // expect: ₡1,234,567.89
Console.WriteLine($"d: {dt.ToString("d", ci)}");       // expect: 29/04/2026
Console.WriteLine($"D: {dt.ToString("D", ci)}");       // expect: miércoles, 29 de abril de 2026
EOF
cat > probe.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF
dotnet run
```

If your output matches the comments, the override is correct.

---

## Per-View Rewrite Workflow

After Commits 1–3 land, sweep views one folder at a time. Each PR:

1. Cites the latest `voice-guide.md` commit hash in the PR description.
2. Translates copy in place (no resx, no IStringLocalizer — see NFR-003).
3. Updates any test fixture that asserts on changed strings (use `tests/FundingPlatform.Tests.E2E/Constants/UiCopy.cs` for shared strings).
4. Runs the local E2E suite for the affected area; confirms green.
5. Includes a screenshot of one before / one after surface in the PR.

Recommended sweep order (matches user-story priority):

| Order | Folder / Surface | Reason |
|---|---|---|
| 1 | `Views/Account/` | Auth pages — first impression, small surface |
| 2 | `Views/Home/` | Applicant dashboard — high traffic |
| 3 | `Views/Shared/` (partials, layout, Error.cshtml) | Touches every page |
| 4 | `Views/Item/` + `Views/Quotation/` + `Views/Supplier/` | Applicant submission flow |
| 5 | `Views/ApplicantResponse/` | Post-decision flow |
| 6 | `Views/Review/` | Reviewer surface |
| 7 | `Views/FundingAgreement/` (PDF templates) | Legal artifact — careful per voice guide |
| 8 | `Views/Admin/` | Admin surface |
| 9 | `StatusVisualMap.cs` (single-file edit) | Cascades into every status pill |
| 10 | View models — `[Display]` and `[ErrorMessage]` | 14 files, 94 attributes |
| 11 | Controllers — TempData / ModelState strings | 10 controllers, 52 sites |
| 12 | E2E test rewrite (POM constants → cascading consumers) | After all UI lands |

---

## Verification Gates

Before requesting code review on the final integration PR:

- [ ] **SC-001 (regex sweep)**: Run `grep -rn '\b[A-Z][a-z]\{3,\}' src/FundingPlatform.Web/Views/` and verify only allowlisted tokens (Capital, Semilla, USD, GBP, CRC, EUR, etc.) appear.
- [ ] **SC-002 (brand sweep)**: Run `grep -rn '\bForge\b' src/FundingPlatform.Web/ tests/` excluding `AntiForgery` and jQuery vendor files; confirm zero results.
- [ ] **SC-006 (`lang` declaration)**: Inspect `_Layout.cshtml` and `_AuthLayout.cshtml`; confirm `<html lang="es-CR">`.
- [ ] **SC-007 (E2E green)**: `dotnet test tests/FundingPlatform.Tests.E2E` — confirm green at same or higher count than pre-translation baseline.
- [ ] **SC-008 (logs ASCII)**: Filter logs from a happy-path run; confirm no Spanish characters appear (logs stay English per NFR-001).
- [ ] **SC-009 (voice-guide-first)**: `git log --oneline 012-es-cr-localization` — confirm `voice-guide.md` commit precedes every per-view rewrite.
- [ ] **SC-011 (registry coverage)**: A test enumerates every value of every domain enum and confirms `StatusVisualMap.For(...)` returns a non-empty Spanish `DisplayLabel`.
- [ ] **SC-012 (perf)**: LCP/TBT pre/post comparison shows no regression vs. spec 011 baseline.

---

## When in Doubt

- **Term choice unclear?** → Update the voice-guide glossary first, then propagate to code.
- **A view's copy doesn't fit the voice guide?** → Open a voice-guide-discussion comment in the PR; do NOT silently improvise.
- **A test assertion's Spanish string seems wrong?** → Reference the rendered production HTML, not your translation hunch. The visible text IS the contract.
- **`es-CR` formatting looks weird in a render?** → Re-run the probe in this doc; verify the override is wired in `Program.cs`.
- **Hit an `enum.ToString()` somewhere?** → Route it through `StatusVisualMap.For(...)` (or its sibling for journey labels). FR-010 forbids `.ToString()` to UI.

---

## Open Questions to Watch

OQ-1, OQ-2, OQ-3, OQ-4, OQ-5, OQ-6, OQ-7, OQ-8, OQ-9 — see [spec.md §Open Questions](./spec.md#open-questions). Most close during voice-guide authoring; OQ-3 (designer SVG follow-ups) is the only one that may extend past first deploy.
