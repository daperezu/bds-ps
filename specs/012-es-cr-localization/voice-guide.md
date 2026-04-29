# Voice Guide: Costa Rican Spanish (Capital Semilla)

**Spec:** [spec.md](./spec.md)
**Status:** Authored 2026-04-29
**Reviewers:** spec 011 voice owner (autonomous review accepted; recommendations from research recorded inline)

This is the canonical reference for register, tone, and CR-specific term mappings
for the Capital Semilla platform. Per FR-020 / SC-009, this artifact MUST land
**before** any per-view rewrite commit. Every translation reviewer references it
as the single source of truth.

---

## 1. Register

- **Address:** formal `usted`. Always.
- **Forbidden:** `tú`, `vos` (the regional "voseo" form is informal even in CR
  and inappropriate for an institutional/business platform).
- **Why:** Capital Semilla is a regulated funding platform; CR business
  convention treats the user-platform relationship as institutional.

### Patterns

| Pattern | Example |
|---|---|
| Verb conjugation in user prompts | "Ingrese su correo electrónico." (NOT "Ingresá") |
| Possessives | "Su solicitud" / "Sus archivos" (NOT "Tu" / "Tus") |
| Imperatives in CTAs | "Enviar solicitud" / "Guardar cambios" (infinitive form on buttons; treat as non-vocative label) |
| Confirmations | "¿Está seguro de que desea enviar la solicitud?" |
| Error addressing | "No se pudo guardar la solicitud. Inténtelo nuevamente." |

---

## 2. Tone

Mirror of spec 011's English voice descriptors:

- **Cálido** (warm): cordial without being colloquial. Use first-person plural
  (`hemos guardado`, `le acompañamos`) sparingly, as a reassurance signal.
- **Moderno** (modern): concise, low corporate jargon. Avoid `el mismo`,
  `dicho proceso`, `se le notifica`. Prefer active constructions.
- **Pragmático** (pragmatic): surface the user's next action; respect their time.
  Empty-state captions name the action ("Cree su primera solicitud para empezar")
  rather than describe absence ("No tiene solicitudes").
- **Encouraging:** celebrate progress in success states ("¡Solicitud enviada!"),
  but never hyperbolic. Never use exclamations on neutral chrome (forms, tables).

### Hard nos

- **No emojis** anywhere user-facing.
- **No corporate fluff** ("le invitamos a que considere…", "en aras de…").
- **No anglicisms** when a CR-natural Spanish term exists (see glossary).
- **No "click aquí"** — link text describes the destination ("Ver detalles",
  "Editar solicitud").

---

## 3. Glossary

CR-business-correct mappings. Where multiple terms are acceptable, the chosen
term and rejected alternatives are listed with a one-line rationale.

### Nouns

| English | Chosen Spanish | Rejected | Rationale |
|---|---|---|---|
| application (the artifact) | solicitud | aplicación | "solicitud" is the CR-business norm for grant/funding requests; "aplicación" is software-borrowed and feels like a calque |
| applicant | solicitante | aplicante | Aligned with "solicitud" |
| review | revisión | evaluación | Tighter; "evaluación" implies scoring rubric we don't have |
| reviewer | revisor / revisora | evaluador | Pairs with "revisión" |
| approver | aprobador / aprobadora | (none) | Direct mapping |
| supplier | proveedor / proveedora | suministrador | "proveedor" is dominant in CR commercial law |
| quotation | cotización | presupuesto | "cotización" is the formal commercial term |
| item | ítem | artículo, partida | "ítem" matches the platform's atomic-line semantics |
| funding | financiamiento | financiación | CR business norm |
| funding agreement | convenio de financiamiento | acuerdo de financiación | "convenio" is the legal-instrument term |
| signature | firma | (none) | Direct mapping |
| appeal | apelación | (none) | Direct mapping |
| response (to a decision) | respuesta | (none) | Direct mapping |
| draft | borrador | (none) | Direct mapping |
| dashboard | panel | tablero | "panel" reads cleaner in headings |
| home | inicio | (none) | Standard |
| inbox | bandeja | buzón | "bandeja de firmas" tested better than "buzón" |
| queue | cola | (none) | Direct mapping |
| notification | notificación | aviso | Direct mapping |
| activity feed | actividad reciente | (none) | Phrase form, not a calque |
| timeline | línea de tiempo | cronología | More user-friendly |
| stage | etapa | fase | Direct mapping |

### Verbs

| English | Chosen Spanish | Rejected | Rationale |
|---|---|---|---|
| submit | enviar | someter | "someter" reads coercive in CR |
| review | revisar | evaluar | Pairs with "revisión" |
| approve | aprobar | (none) | Direct mapping |
| reject | rechazar | denegar, declinar | "rechazar" is standard for review decisions |
| send back | devolver | regresar, retornar | "devolver" matches the legal-business sense |
| sign | firmar | (none) | Direct mapping |
| appeal | apelar | recurrir | Direct mapping |
| regenerate | regenerar | volver a generar | Tighter |
| finalize | finalizar | concluir | Direct mapping |
| lock | bloquear | (none) | Direct mapping |
| supersede | reemplazar | sustituir | More natural |
| withdraw | retirar | desistir, anular | "retirar" reads cleanly |
| resolve | resolver | (none) | Direct mapping |
| upload | subir | cargar | "subir" is dominant in CR UX |
| download | descargar | bajar | Direct mapping |
| save | guardar | (none) | Direct mapping |
| edit | editar | modificar | Direct mapping in UX context |
| delete | eliminar | borrar | "eliminar" reads slightly more formal |
| funded | financiada | aprobada y desembolsada | Distinguishes "approved" from "actually paid" |

### States / adjectives

| English | Chosen Spanish | Rationale |
|---|---|---|
| required | obligatorio | Standard |
| optional | opcional | Standard |
| valid | válido | Standard |
| invalid | inválido / no válido | Both acceptable; prefer "no válido" in error sentences |
| pending | pendiente | Standard |
| executed | ejecutado | Legal sense |
| frozen / immutable | bloqueado / inmutable | "bloqueado" in UX, "inmutable" in legal copy |
| under review | en revisión | Standard |
| sent back | devuelta | Past participle, feminine to agree with "solicitud" |
| approved | aprobada | Past participle, feminine to agree with "solicitud" |
| rejected | rechazada | Past participle, feminine to agree with "solicitud" |
| resolved | resuelta | Past participle, feminine to agree with "solicitud" / "apelación" |

### Layout / chrome

| English | Chosen Spanish |
|---|---|
| Login | Iniciar sesión |
| Logout | Cerrar sesión |
| Register | Crear cuenta |
| Forgot password? | ¿Olvidó su contraseña? |
| Change password | Cambiar contraseña |
| Welcome back | Bienvenido de vuelta |
| Cancel | Cancelar |
| Confirm | Confirmar |
| Save changes | Guardar cambios |
| Discard | Descartar |
| Close | Cerrar |
| Back | Atrás |
| Next | Siguiente |
| Previous | Anterior |
| Yes / No | Sí / No |
| Search | Buscar |
| Filter | Filtrar |
| Sort | Ordenar |
| Export | Exportar |
| Print | Imprimir |
| Loading… | Cargando… |
| All clear | Todo en orden |
| Nothing here yet | Aún no hay nada aquí |
| Try again | Inténtelo nuevamente |

### CR-specific compliance terminology

The platform's supplier compliance section asks about CR-specific obligations.
Do NOT translate the agency abbreviations.

| English | Chosen Spanish |
|---|---|
| CCSS compliant | Al día con la CCSS |
| Hacienda compliant | Al día con Hacienda |
| Registered in SICOP | Inscrito en SICOP |
| Tax ID | Cédula jurídica / Identificación fiscal |
| Legal name | Razón social |
| Personal ID | Cédula |

### Footer tagline (closes OQ-2)

- **Chosen:** "diseñado para emprendedores"
- **Rejected:** "creado para emprendedores" (slightly weaker), "para los emprendedores costarricenses" (over-specific; the platform is regional but the brand is universal)

### Brand mark (closes part of EC-7 / FR-006)

- **Brand:** Capital Semilla
- **Wordmark `<text>`:** "Capital Semilla"
- **Mark `aria-label`:** "Marca Capital Semilla"
- **Wordmark `aria-label`:** "Capital Semilla"
- **Page-title pattern:** `<page> - Capital Semilla` (em-dash separator preserved per FR-005)

---

## 4. Example Pairs

| Surface | English (before) | Spanish (after) |
|---|---|---|
| Login screen heading | Welcome back | Bienvenido de vuelta |
| Login submit button | Sign in | Iniciar sesión |
| Empty-state caption (review queue) | All clear — nothing's awaiting your review. | Todo en orden — no tiene revisiones pendientes. |
| Empty-state caption (applicant home) | Start your first application to see it here. | Cree su primera solicitud para verla aquí. |
| Status pill | Under Review | En revisión |
| Status pill | Agreement Executed | Convenio ejecutado |
| Validation error (Required) | This field is required. | Este campo es obligatorio. |
| Validation error (StringLength) | The {0} field must be at most {1} characters. | El campo {0} debe tener máximo {1} caracteres. |
| Validation error (Range) | Price must be greater than zero. | El precio debe ser mayor a cero. |
| Funding Agreement clause heading | Terms and Conditions | Términos y condiciones |
| Funding Agreement clause body | The parties agree as follows: | Las partes acuerdan lo siguiente: |
| TempData success | Application submitted. | Solicitud enviada. |
| TempData error | Could not submit application. | No se pudo enviar la solicitud. |
| Identity error (PasswordMismatch) | Incorrect password. | Contraseña incorrecta. |
| Identity error (DuplicateEmail) | Email '{0}' is already taken. | El correo electrónico '{0}' ya está registrado. |
| Page title (applicant home) | Dashboard - Forge | Inicio - Capital Semilla |
| Page title (review queue) | Review Queue - Forge | Cola de revisión - Capital Semilla |
| Footer copyright | © 2026 Forge · built for entrepreneurs | © 2026 Capital Semilla · diseñado para emprendedores |
| Sidebar entry | My Applications | Mis solicitudes |
| Sidebar entry | Signing Inbox | Bandeja de firmas |
| Confirm action | Are you sure? | ¿Está seguro? |
| Loading state | Loading… | Cargando… |
| Logout button | Logout | Cerrar sesión |

---

## 5. Format Conventions (es-CR)

- **Numbers:** `1,234.56` (period decimal, comma thousands). NOT `1.234,56` (es-CO style).
- **Dates (short):** `dd/MM/yyyy`. Today renders as `29/04/2026`.
- **Dates (long):** `miércoles, 29 de abril de 2026` (lowercase day-of-week and month — Spanish convention).
- **Currency:** symbol from per-quotation Currency code (FR-019 / spec 010): `₡` for CRC, `$` for USD, `£` for GBP, `€` for EUR. Always with the override-driven separators above. So CRC renders `₡1,234.56`.
- **Time:** 24-hour clock by default (`14:30` not `2:30 PM`). The platform shows `dd/MM/yyyy HH:mm` for timestamped events.

---

## 6. Notes for Reviewers

- **A view's copy doesn't fit?** Update the glossary in this guide first, then propagate.
- **Term feels stiff?** Read it aloud. Spanish business writing is more formal than English equivalent; "stiff" relative to English is often "appropriate" in CR business Spanish.
- **Length expansion?** Spanish averages ~25% longer than English. Tabler responsive utilities handle most cases; tighten layout if a table column header overflows.
- **Placeholder safety:** when translating format strings, validate that every `{0}`, `{1}` placeholder still appears at least once in the translation, even if word order changes.

---

## 7. Lifecycle

- **Authored** in this commit; standalone (no per-view edits).
- **Reviewed** asynchronously; reviewer feedback amends this file before per-view rewrites land.
- **Living artifact:** any future copy edit MUST update this guide first, then propagate to code.

This artifact closes:

- **OQ-1** (glossary finalization) — section 3 above.
- **OQ-2** (footer tagline) — "diseñado para emprendedores".
- **OQ-7** (page-title direction) — `<page> - Capital Semilla`, em-dash preserved.
- **OQ-9** (JS namespace) — `PlatformMotion`.
