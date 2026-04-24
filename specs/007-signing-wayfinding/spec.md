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

---

## Amendment 1 — Reviewer queue for applications awaiting Funding Agreement generation

**Amended**: 2026-04-24
**Trigger**: Post-ship field report. Once an applicant submits their response and the application transitions to `ResponseFinalized`, the application disappears from every reviewer-visible list on `/Review`. The **Initial Review Queue** filters to `Submitted` only (see `src/FundingPlatform.Application/Services/ReviewService.cs:24-27`); the **Signing Inbox** filters to applications that already have a pending signed upload (see `src/FundingPlatform.Infrastructure/Persistence/Repositories/SignedUploadRepository.cs:42`), which requires a Funding Agreement to already exist. The agreement-generation action (`FundingAgreementController.Generate`, authorized for the assigned reviewer and for admins per `Application.CanUserGenerateFundingAgreement` at `src/FundingPlatform.Domain/Entities/Application.cs:462-467`) therefore has no discoverable entry point from any reviewer-accessible page. `/Application/Details/{id}` is `[Authorize(Roles = "Applicant")]` (see `src/FundingPlatform.Web/Controllers/ApplicationController.cs:12-13`) and cannot serve as a reviewer entry either. Result: generation is a dead-letter action whose preconditions can be met but whose trigger is effectively unreachable without URL guessing, which in turn leaves every applicant stuck on the *"No agreement has been generated yet."* state covered by acceptance scenario 3 of User Story 2.

**Scope of the amendment**: Add a third `/Review` sub-tab — a peer to the two introduced in FR-001 — that lists applications awaiting Funding Agreement generation and links each row to the existing `/Applications/{applicationId}/FundingAgreement` page, where the Generate button is already exposed to the assigned reviewer and admins per spec 005. No new server-side generation entry point, no change to authorization of existing surfaces, no schema change.

**Relationship to the original Out of Scope list**: This amendment deliberately narrows the original out-of-scope clause *"New pipeline stages, new review flavors, new inbox types. This feature only wires two existing surfaces together."* — the new tab is, strictly, a third inbox type. The narrowing is intentional: without this tab, the wayfinding loop the original spec defined (US1 reviewer discoverability → US2 applicant signing → eventual execution) cannot be closed in practice. The rest of that out-of-scope clause stands.

### User Story 4 - Reviewer discovers which applications are ready for agreement generation (Priority: P1)

An assigned reviewer signs in after an applicant has submitted their response. They click **Review** in the main navigation and see a new sub-tab labeled **Generate Agreement** positioned between **Initial Review Queue** and **Signing Inbox**. Selecting it shows a paginated list of the applications the reviewer is assigned to that are in state `ResponseFinalized` with no Funding Agreement record yet, with columns *Application title · Applicant · Response finalized on · Open →*. The reviewer clicks **Open** on a row and lands on `/Applications/{applicationId}/FundingAgreement`, where the Generate button shipped by spec 005 is already visible to their role. After they generate the agreement, the same application leaves this queue on the next page load and becomes visible to the applicant via the embedded panel on `/ApplicantResponse/Index/{id}` (as already covered by User Story 2), closing the end-to-end wayfinding loop.

**Why this priority**: Without this, User Story 2's happy path cannot be reached in practice. An applicant whose response is finalized sees *"No agreement has been generated yet."* indefinitely because no reviewer has a navigable path to the generation action. This gap blocks every application from progressing past `ResponseFinalized` regardless of how well US1 and US2 are implemented.

**Independent Test**: Seed an application at `ResponseFinalized` with no Funding Agreement and a reviewer assigned to it. Starting from `/`, the assigned reviewer reaches the Generate Agreement tab in at most two clicks (Review → Generate Agreement) and sees the application listed. Clicking **Open** lands on the Funding Agreement page with the Generate button visible. A second, unassigned reviewer does not see the application in their own list. An admin user sees the application regardless of assignment.

**Acceptance Scenarios**:

1. **Given** a user with the `Reviewer` role assigned to an application in state `ResponseFinalized` with no Funding Agreement, **When** they navigate to `/Review` and click the **Generate Agreement** sub-tab, **Then** the page lists the application with columns *Application title*, *Applicant*, *Response finalized on*, and an **Open** action linking to `/Applications/{applicationId}/FundingAgreement`.
2. **Given** a user with the `Reviewer` role who is NOT assigned to a matching application, **When** they open the **Generate Agreement** tab, **Then** that application does NOT appear in their list.
3. **Given** a user with the `Admin` role, **When** they open the **Generate Agreement** tab, **Then** all applications in state `ResponseFinalized` with no Funding Agreement are listed regardless of reviewer assignment.
4. **Given** an assigned reviewer viewing the **Generate Agreement** tab, **When** they click **Open** on a row and then click **Generate agreement** on the Funding Agreement page, **Then** the application is removed from the **Generate Agreement** tab on the next page load, and — per User Story 2 — appears on the applicant's response page with the *"ready to sign"* banner.
5. **Given** a reviewer with no applications matching the tab's criteria, **When** they open **Generate Agreement**, **Then** the empty-state message *"No applications are waiting for agreement generation."* renders with the test hook `data-testid="generate-agreement-empty"`.
6. **Given** an application in state `AppealOpen` with no Funding Agreement, **When** the assigned reviewer opens **Generate Agreement**, **Then** the application does NOT appear in the list (`AppealOpen` is a distinct state; its discoverability is explicitly out of scope for this amendment).
7. **Given** an application in state `ResponseFinalized` whose applicant rejected every item (so `Application.CanGenerateFundingAgreement()` would return false on the preconditions check), **When** the assigned reviewer opens **Generate Agreement**, **Then** the application DOES appear in the list — the tab's filter is scoped by state plus missing agreement only. The Funding Agreement page surfaces the precondition blocker when the reviewer clicks **Open**.
8. **Given** a user with only the `Applicant` role, **When** they attempt to reach `/Review/GenerateAgreement` directly, **Then** access is denied, matching the existing `/Review` authorization posture.
9. **Given** two applications the same reviewer is assigned to — one finalized earlier and one finalized later — **When** the reviewer opens **Generate Agreement**, **Then** the earlier-finalized application appears above the later-finalized one (ascending order of response-finalized-on so long-waiting applicants are not starved).

### Additional Functional Requirements

- **FR-008**: The `/Review` landing MUST expose a third peer sub-tab — **Generate Agreement** — positioned between **Initial Review Queue** and **Signing Inbox** in `_ReviewTabs.cshtml`. The tab is served at route `/Review/GenerateAgreement` and participates in the same route-level authorization as its siblings (`Reviewer,Admin`). No new authorization surface is introduced.
- **FR-009**: The **Generate Agreement** tab MUST list applications matching ALL of the following, paginated using the same page size as the sibling tabs, in ascending order of response-finalized-on (oldest first):
  - Application state equals `ResponseFinalized`; AND
  - The application has no Funding Agreement record associated; AND
  - Either the signed-in user is assigned as a reviewer on the application, OR the signed-in user has the `Admin` role.
- **FR-010**: Each row in the **Generate Agreement** tab MUST display the application title, the applicant's display name, the timestamp the response was finalized, and an **Open** action. The **Open** action MUST route to `/Applications/{applicationId}/FundingAgreement` — the same target used by the **Signing Inbox**'s Open link per the existing implementation — so reviewers reach a page where the Generate button is already role-gated and ready.
- **FR-011**: The **Generate Agreement** tab MUST NOT introduce any new server-side agreement-generation entry point or any new mutation endpoint. It is a discovery surface only; the creation action remains `FundingAgreementController.Generate` as shipped by spec 005.
- **FR-012**: The **Generate Agreement** tab MUST render the empty-state message *"No applications are waiting for agreement generation."* — with the test hook `data-testid="generate-agreement-empty"` — when no rows match the filter in FR-009.
- **FR-013**: Implementation MUST NOT modify the authorization of `/Application/Details/{id}`. The applicant-only gate on that route stands; reviewers and admins reach agreement generation exclusively via `/Applications/{applicationId}/FundingAgreement`.

### Additional Success Criteria

- **SC-008**: An assigned reviewer reaches an application ready for agreement generation in at most three clicks from any signed-in page: **Review** → **Generate Agreement** → **Open**. No URL typing required.
- **SC-009**: For any application in state `ResponseFinalized` with no Funding Agreement, exactly one of the following is true for each reviewer: (a) the application is listed in that reviewer's **Generate Agreement** tab (assigned-reviewer case), or (b) the application is not listed in that reviewer's tab (unassigned-reviewer case). Admins always see case (a).
- **SC-010**: The Playwright coverage for User Story 4 is added alongside the SC-007 suite and includes: (a) an assigned reviewer sees the application in the **Generate Agreement** tab with the correct columns and sort order; (b) an unassigned reviewer does not see the application; (c) an admin sees the application regardless of assignment; (d) the empty-state message and its `data-testid` render when no rows match; (e) the end-to-end chain from **Generate Agreement** → **Open** → **Generate** → the applicant's embedded panel per User Story 2 completes without navigating off the `/Review/*` and `/ApplicantResponse/Index/{id}` surfaces.
- **SC-011**: No regressions to SC-001 through SC-007. The **Initial Review Queue** and **Signing Inbox** tabs' content, authorization, URLs, and Playwright coverage are unchanged.

### Amendment Assumptions

- `Application` either already exposes a response-finalized-on timestamp or can derive one — the implementation plan must verify and name the chosen source, with *"latest `ApplicantResponse.SubmittedAt` for the application"* as the permitted fallback if no dedicated domain field exists.
- A reviewer-to-application assignment relation already exists and is queryable, since `Application.CanUserGenerateFundingAgreement(isAdministrator, isReviewerAssignedToThisApplication)` already branches on it. The implementation plan must identify the exact relation and reuse it rather than introduce a new one.
- The `/Applications/{applicationId}/FundingAgreement` page already exposes the **Generate** button to the assigned reviewer and to admins when `Application.CanGenerateFundingAgreement()` preconditions are met. This amendment does not change that.
- Pagination on the sibling tabs uses a consistent page size and partial; the new tab adopts the same convention rather than introducing its own.

### Amendment Out of Scope

- A reviewer queue for applications in state `AppealOpen`. Different workflow phase; its discoverability gap is acknowledged and will be addressed in a separate amendment or feature if raised.
- A reviewer queue for applications where the Funding Agreement has been generated but the applicant has not yet uploaded a signed PDF. The reviewer holds no action in that phase; the applicant is the blocker.
- Auto-generation of the Funding Agreement on the `ResponseFinalized` transition. Spec 005's *"Administrator Generates"* user-story framing stands; this amendment only makes the existing manual trigger discoverable.
- Any change to `/Application/Details/{id}` authorization. That route remains applicant-only; reviewers reach generation via `/Applications/{applicationId}/FundingAgreement`.
- Schema changes. This amendment keeps the *"no schema changes"* invariant carried forward from 007's plan.
- Notifications (email, in-app toast) when an application enters the new queue. Deferred in line with the original 007 out-of-scope list.
- Pending-count badges on the new tab. Deferred for the same reason stated in the original spec's Out of Scope list.
- Reordering or renaming of the two existing `/Review` sub-tabs. Their positions, labels, content, and routes remain unchanged.
