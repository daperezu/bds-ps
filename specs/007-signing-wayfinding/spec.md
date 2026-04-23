# Feature Specification: Signing Stage Wayfinding

**Feature Branch**: `007-signing-wayfinding`
**Created**: 2026-04-23
**Status**: Draft
**Input**: Close three navigation/discovery gaps left by spec 006. Reviewers cannot reach `/Review/SigningInbox` from any link in the app — the page is live but orphaned. Applicants land on `/ApplicantResponse/Index/{id}` after accepting reviewer decisions but the Funding Agreement signing panel lives only on `/Application/Details/{id}`, leaving applicants with no visible way to sign. The 006 quickstart prose refers to "the application's detail page" without distinguishing between the two detail-ish pages, compounding the problem. This feature wires the existing surfaces together: sub-tabs on `/Review` for reviewers; the same signing panel embedded on the applicant's response page with a contextual status banner; and a quickstart sync so prose matches the self-navigable UI. No new signing-stage behavior.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reviewer discovers the Signing Inbox without tribal knowledge (Priority: P1)

A user with the `Reviewer` role signs in, clicks the **Review** navbar link, and sees two peer sub-tabs on the landing page: **Initial Review Queue** (existing content — applications submitted for initial review) and **Signing Inbox** (existing content — applications with pending signed uploads awaiting reviewer decision). They select the **Signing Inbox** tab and see the paginated inbox exactly as it is served today at `/Review/SigningInbox`. No URL typing, no docs consultation.

**Why this priority**: Without this, the `/Review/SigningInbox` route shipped by spec 006 is effectively invisible to reviewers. Every signed upload pending review is a dead letter until a reviewer learns the URL out-of-band. This is the single most impactful wayfinding fix in the feature.

**Independent Test**: A fresh reviewer account can, starting from `/` with no prior instruction, reach the Signing Inbox in at most two clicks (Review → Signing Inbox tab). The existing `SigningInboxTests`-style coverage for the inbox contents continues to pass byte-identically.

**Acceptance Scenarios**:

1. **Given** a signed-in user with the `Reviewer` role, **When** they click the **Review** link in the main navigation, **Then** the landing page shows two sub-tabs labeled **Initial Review Queue** and **Signing Inbox**, with the initial-review queue selected by default (preserving today's behavior).
2. **Given** a reviewer on the `/Review` page, **When** they click the **Signing Inbox** sub-tab, **Then** the page swaps to the Signing Inbox view served today at `/Review/SigningInbox`, with identical rows, pagination, and authorization.
3. **Given** a signed-in user with only the `Admin` role (no `Reviewer` role), **When** they click **Review**, **Then** both sub-tabs are visible (Admin is permitted on the existing `/Review/SigningInbox` route; Initial Review Queue authorization is unchanged from today).
4. **Given** a signed-in user with only the `Applicant` role, **When** they navigate to `/Review` directly, **Then** access is denied as it is today; sub-tabs introduce no new authorization surface.

---

### User Story 2 - Applicant discovers and uses the signing panel on their response page (Priority: P1)

An applicant whose application has reached `ResponseFinalized` and had a Funding Agreement generated visits `/ApplicantResponse/Index/{id}` (the page they naturally land on after accepting reviewer decisions). Above the existing response-item form they see a single-line status banner: *"Your funding agreement is ready to sign below."* Below the form, the full Funding Agreement signing panel renders — identical partial, identical actions, identical authorization as the embed on `/Application/Details/{id}`. They download, upload a signed PDF, and track review status without leaving the response page. Once the reviewer approves, the next visit shows the banner flip to *"Your funding agreement has been executed."* with a download link to the signed counterpart.

**Why this priority**: The signing panel that spec 006 built is unreachable from the page applicants actually land on; today they must discover the Applications → Details path without guidance. Making the panel appear where the applicant is already looking closes the loop between "I accepted the reviewer's decisions" and "I sign and execute the agreement" without requiring the applicant to understand the app's route structure.

**Independent Test**: Starting from the applicant seeded by 006's quickstart (`applicant@demo.com` with an application at `ResponseFinalized` + generated agreement), visiting `/ApplicantResponse/Index/{id}` shows the banner and the fully functional signing panel without any navigation. The four 006 SC-008 journeys can be walked entirely from this page for the applicant half of each journey.

**Acceptance Scenarios**:

1. **Given** an application in state `ResponseFinalized` with a Funding Agreement record present, **When** the applicant opens `/ApplicantResponse/Index/{id}`, **Then** a single-line status banner *"Your funding agreement is ready to sign below."* appears above the embedded panel, and the signing panel below it shows the same Download / Upload / Replace / Withdraw actions the applicant sees today on `/Application/Details/{id}`.
2. **Given** an application in state `AgreementExecuted`, **When** the applicant opens `/ApplicantResponse/Index/{id}`, **Then** the banner changes to *"Your funding agreement has been executed."* and the embedded panel exposes the signed-counterpart download link.
3. **Given** an application in any state earlier than `ResponseFinalized`, **When** the applicant opens `/ApplicantResponse/Index/{id}`, **Then** the banner is not rendered, and the embedded panel shows whatever state the signing panel already shows for that application phase (including *"No agreement has been generated yet."* when appropriate).
4. **Given** a reviewer or admin user visits an applicant's `/ApplicantResponse/Index/{id}`, **When** the page renders, **Then** the embedded panel exposes only the actions the existing panel already exposes to that user + state combination — no role escalation, no new actions.
5. **Given** the embedded-panel data load fails (e.g., transient error), **When** the response page renders, **Then** the rest of the page (response-item form, appeal controls, banner) continues to render and the panel area degrades silently, matching the existing `/Application/Details/{id}` fetch-catch pattern.

---

### User Story 3 - Quickstart prose matches the self-navigable UI (Priority: P2)

A QA walker or stakeholder picks up `specs/006-digital-signatures/quickstart.md` and executes all four SC-008 journeys end-to-end in the browser, following only the quickstart's own navigation instructions. The prose explicitly tells them which page to open for each role (response page for applicants, `/Review` → Signing Inbox for reviewers) and matches what they actually see in the UI. No URL typing, no ambiguous phrasing like "the application's detail page."

**Why this priority**: Docs-only fix, lowest risk, complements US1+US2. Without US1/US2 the prose changes have nowhere to point; with US1/US2 the docs update is a mechanical sync rather than a design question. Keeps docs from drifting the moment the UI ships.

**Independent Test**: A reader who has never used the app can complete each of the four 006 quickstart journeys following only the quickstart's own prose — no URL typing, no out-of-band URL lookups, no questions asked of the developer.

**Acceptance Scenarios**:

1. **Given** a reader following 006's Journey 1 (happy path), **When** they reach the step that says to navigate to the application's page, **Then** the prose names the concrete entry point (`/ApplicantResponse/Index/{id}` for the applicant; `/Review` → **Signing Inbox** tab for the reviewer) and the reader reaches the right surface without guessing.
2. **Given** a reader following 006's Journey 2 (rejection loop), **When** they reach the reviewer step, **Then** the prose says "click **Review** in the main navigation, then open the **Signing Inbox** tab" and matches what the reader sees in the UI.
3. **Given** a reader following 006's Journey 4 (regeneration lockout), **When** they reach the version-mismatch sub-path, **Then** the prose no longer refers ambiguously to "the application's detail page" — it identifies the concrete page by role.
4. **Given** no functional behavior changes in 006, **When** the reader executes all four journeys, **Then** no step is gained or lost compared to 006's current journey count — only the wayfinding prose changes.

---

### Edge Cases

- Application has no Funding Agreement generated yet and state is not `ResponseFinalized`: banner hidden, embedded panel on response page shows its existing *"No agreement has been generated yet."* state.
- State is `AppealOpen` and a Funding Agreement already exists: banner hidden per the explicit banner rules (only `ResponseFinalized` and `AgreementExecuted` trigger banners); embedded panel behaves as it does on `/Application/Details/{id}` for the same state.
- Reviewer/admin visits an applicant's response page: embedded panel inherits existing role-based gating from the 006 partial; no new review actions are exposed on the response page that are not already exposed on `/Application/Details/{id}`.
- An unauthenticated user hits `/ApplicantResponse/Index/{id}`: authentication behavior is unchanged from today — the page already redirects to login.
- Reviewer rapidly switches between **Initial Review Queue** and **Signing Inbox** tabs: each tab is served by its own existing route; no shared client-side state, no race.
- Applicant has multiple applications, each at a different signing-stage state: each `/ApplicantResponse/Index/{id}` is scoped to a single application id; banner and embedded panel reflect only that application.
- Embedded panel load fails on the response page while the response-item form has unsubmitted changes: the response form is not re-rendered by the panel load, so no data loss (the panel and the response form are independent regions of the page).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `/Review` landing MUST expose two peer sub-tabs — **Initial Review Queue** (existing content) and **Signing Inbox** (existing `/Review/SigningInbox` content) — both reachable without typing a URL. Authorization for each tab's content MUST remain exactly what the underlying route currently enforces.
- **FR-002**: The Funding Agreement signing panel MUST be embedded on `/ApplicantResponse/Index/{id}` using the same partial and view-model construction as the existing embed on `/Application/Details/{id}`. Content, authorization, and available actions MUST be identical for the same user + state.
- **FR-003**: On `/ApplicantResponse/Index/{id}`, a single-line contextual status banner MUST render above the embedded panel according to the following rules, and MUST NOT render in any other situation:
  - Application state = `ResponseFinalized` **and** a Funding Agreement record exists → *"Your funding agreement is ready to sign below."*
  - Application state = `AgreementExecuted` → *"Your funding agreement has been executed."*
- **FR-004**: The embedded panel on the response page and the embed on `/Application/Details/{id}` MUST share a single source of truth — the same partial and the same view-model construction. Divergence between the two is a defect.
- **FR-005**: The existing `/Application/Details/{id}` signing-panel embed MUST continue to work byte-identically after this feature lands — no removed actions, no changed authorization, no changed rendering path.
- **FR-006**: `specs/006-digital-signatures/quickstart.md` MUST be updated so each journey's prose names concrete entry points per role: applicants use `/ApplicantResponse/Index/{id}`; reviewers use `/Review` and the **Signing Inbox** sub-tab. No journey MAY gain or lose steps; only wayfinding prose changes.
- **FR-007**: This feature MUST NOT introduce any new authorization surface. Role-based gating on the embedded panel on the response page MUST inherit from the existing 006 partial; the two new sub-tabs MUST reuse the existing route-level authorization (`Reviewer,Admin` for Signing Inbox; current rule for Initial Review Queue). No new controller actions that return signing-stage data MAY be created for this feature.

### Key Entities

No new entities, tables, or domain concepts are introduced. This feature composes existing surfaces:

- **Funding Agreement signing panel** (from spec 006) — now rendered on two host pages instead of one.
- **Signing Inbox listing** (from spec 006) — now reachable via a sub-tab on the `/Review` landing.
- **Application state** (from spec 001+004+006) — drives the banner visibility rules in FR-003.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A signed-in reviewer can reach the Signing Inbox in at most two clicks from any page: click **Review** in the main navigation → click **Signing Inbox** sub-tab. No URL typing required.
- **SC-002**: An applicant on `/ApplicantResponse/Index/{id}` with application state `ResponseFinalized` and a generated Funding Agreement sees both the status banner and the fully functional signing panel without any navigation step.
- **SC-003**: An applicant on `/ApplicantResponse/Index/{id}` with application state `AgreementExecuted` sees the "executed" banner and is able to download the signed counterpart PDF from the embedded panel without navigating elsewhere.
- **SC-004**: A first-time reader of `specs/006-digital-signatures/quickstart.md` can complete each of the four SC-008 journeys end-to-end following only the quickstart's own prose — no URL typing, no questions asked of the developer.
- **SC-005**: For any given user and application state, the embedded panel on `/ApplicantResponse/Index/{id}` and the embed on `/Application/Details/{id}` render identical content and identical actions. A difference is a defect.
- **SC-006**: After this feature ships, no automated test covering the existing `/Application/Details/{id}` signing embed or the `/Review/SigningInbox` page regresses.
- **SC-007**: The new wayfinding journeys are covered by Playwright end-to-end tests (consistent with the tooling established in spec 001 and the pattern in spec 006's SC-008) and pass in CI on every change to this feature: (a) reviewer reaches the Signing Inbox via `/Review` → **Signing Inbox** sub-tab; (b) applicant on `/ApplicantResponse/Index/{id}` sees the signing panel and the correct contextual banner for states `ResponseFinalized` and `AgreementExecuted`; (c) the existing `/Application/Details/{id}` signing embed continues to pass its existing coverage.

## Assumptions

- The Funding Agreement signing panel partial shipped by spec 006 is already structured as a self-contained include that can be rendered from multiple host pages without internal divergence. If it is not, bringing it into that shape is a permitted and expected part of this feature's implementation.
- The existing `/Review/SigningInbox` route's authorization (`Reviewer,Admin`) is correct and does not need to change. Review and updates to role scope are out of scope here.
- The `ApplicantResponseController`'s current `Index` action can be extended to also expose a signing-panel view-model slot (or to compose with an existing panel fetch mechanism) without a broader restructure of its response-item form.
- Navigation discoverability is the only gap being closed. No new review actions, no new signing workflow steps, no new storage paths.
- The four 006 SC-008 journeys remain the canonical test fixture for end-to-end coverage of the signing stage; this feature changes how a reader of the quickstart *reaches* each step, not what the steps are.

## Dependencies

- **Spec 002 (review & approval workflow)** — owner of the `/Review` landing page that gains the two sub-tabs.
- **Spec 004 (applicant response & appeal)** — owner of `/ApplicantResponse/Index/{id}` that gains the embedded panel and the contextual banner.
- **Spec 006 (digital signatures)** — owner of the Funding Agreement signing panel, its view-model, and the `/Review/SigningInbox` route whose content the new sub-tab exposes.

## Out of Scope

- Pending-count badges on either `/Review` sub-tab. Deferred; can land when signing-inbox volume makes counts valuable.
- Top-level navbar changes. The nav stays coarse-grained (Home / Applications / Review / Admin).
- New pipeline stages, new review flavors, new inbox types. This feature only wires two existing surfaces together.
- State-aware redirects (e.g., automatically bouncing applicants from the response page to the agreement page once they reach `ResponseFinalized`). This feature chose visibility over redirection; a redirect policy is a separate decision.
- Notifications to applicants or reviewers about signing-stage events (email, in-app toasts, etc.). Remain deferred per spec 006's assumptions list.
- Reviewer-assist tooling on the embedded panel — content hashing, side-by-side diff between generated and signed PDFs, version stamping. Remain out of scope per spec 006.
- Any change to the underlying signing behavior (upload intake rules, version-mismatch logic, regeneration lockdown, audit-trail shape). This feature is wayfinding only.
