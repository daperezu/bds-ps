---
description: "Tasks for 007-signing-wayfinding"
---

# Tasks: Signing Stage Wayfinding

**Input**: Design documents in `/specs/007-signing-wayfinding/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. Spec SC-007 mandates Playwright E2E coverage for the three wayfinding journeys; Constitution III makes E2E tests non-negotiable for every feature.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3 — maps to the user stories in spec.md
- File paths are absolute-from-repo-root

## Path Conventions

- Web application layout per plan.md §Project Structure
- `src/FundingPlatform.{Application,Web}/` for production code
- `tests/FundingPlatform.Tests.E2E/` for Playwright tests
- `specs/006-digital-signatures/` is the sibling spec whose quickstart is edited by US3

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Baseline verification. No new projects, packages, or configuration are introduced by this feature.

- [X] T001 Run `dotnet build --nologo` at the repo root and confirm zero errors and zero warnings on branch `007-signing-wayfinding` before making any changes; this is the baseline against which every subsequent task is measured

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: None. Unlike most features, 007 has no shared infrastructure that must land before user-story work can begin. The three user stories touch disjoint files and can be worked in parallel as soon as Phase 1 is green.

**⚠️ CRITICAL**: There is intentionally no foundational work. If a task feels like it belongs here, re-check whether it is actually US-scoped; it almost certainly is.

**Checkpoint**: Foundation ready (trivially). User story implementation can now begin.

---

## Phase 3: User Story 1 — Reviewer reaches Signing Inbox via tabs (Priority: P1) 🎯 MVP

**Goal**: A reviewer clicks **Review** in the navbar and sees two peer sub-tabs ("Initial Review Queue" active by default, "Signing Inbox"), can click the Signing Inbox tab to reach `/Review/SigningInbox` with the same inbox content served today, and can click back to Initial Review Queue. No URL typing. SC-001 and SC-007(a) met.

**Independent Test**: `SigningWayfindingTests.ReviewerReachesSigningInboxInTwoClicks` passes. Manual: follow quickstart.md Journey 1.

### Tests for User Story 1 (write first; expect red before implementation)

- [X] T002 [P] [US1] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/ReviewQueuePage.cs` with three methods — `ClickSigningInboxTab()`, `IsSigningInboxTabActive()`, `IsInitialQueueTabActive()` — targeting the `data-testid` hooks defined in `specs/007-signing-wayfinding/contracts/README.md §2` (`review-tab-initial`, `review-tab-signing`, and the `active` class on the matching `<a>`)
- [X] T003 [US1] Create `tests/FundingPlatform.Tests.E2E/Tests/SigningWayfindingTests.cs` with a single `[Test]` method `ReviewerReachesSigningInboxInTwoClicks` that logs in as a seeded reviewer, navigates to `/`, clicks the **Review** navbar link, asserts both tabs visible with Initial active, clicks the Signing Inbox tab, asserts URL `/Review/SigningInbox` and Signing active, then clicks Initial and asserts the round-trip; follow quickstart.md Journey 1. The test MUST fail before Phase 3 implementation lands (red)

### Implementation for User Story 1

- [X] T004 [US1] Create `src/FundingPlatform.Web/Views/Review/_ReviewTabs.cshtml` per `specs/007-signing-wayfinding/contracts/README.md §2` — a Bootstrap `nav nav-pills mb-3` container with two `<a class="nav-link">` links pointing at `@Url.Action("Index", "Review")` and `@Url.Action("SigningInbox", "Review")`, each with stable `data-testid` attributes; apply `active` class to the matching link based on `ViewData["ActiveTab"]`
- [X] T005 [P] [US1] Modify `src/FundingPlatform.Web/Views/Review/Index.cshtml` to include `@await Html.PartialAsync("_ReviewTabs")` immediately above the existing queue heading and to set `ViewData["ActiveTab"] = "Initial"` at the top of the Razor file. Do not alter any other existing markup
- [X] T006 [P] [US1] Modify `src/FundingPlatform.Web/Views/Review/SigningInbox.cshtml` to include `@await Html.PartialAsync("_ReviewTabs")` immediately above the existing "Signing Inbox" heading and to set `ViewData["ActiveTab"] = "Signing"` at the top. Do not alter pagination, rows, or authorization plumbing

**Checkpoint**: US1 is fully functional. Run `SigningWayfindingTests.ReviewerReachesSigningInboxInTwoClicks` and confirm green. Walk quickstart.md Journey 1 manually. At this point, an MVP ship of 007 is defensible.

---

## Phase 4: User Story 2 — Applicant sees banner and signing panel on response page (Priority: P1)

**Goal**: An applicant on `/ApplicantResponse/Index/{id}` sees a state-driven banner above the existing response-item form and the full Funding Agreement signing panel embedded below the form. The banner reads *"Your funding agreement is ready to sign below."* for `ResponseFinalized` + agreement present; it reads *"Your funding agreement has been executed."* for `AgreementExecuted`; otherwise hidden. The panel is identical to the `/Application/Details/{id}` embed (same partial, same endpoint, same actions). SC-002, SC-003, SC-005, SC-007(b) met.

**Independent Test**: `SigningWayfindingTests.ApplicantSeesBannerAndPanelOnResponsePage` passes. Manual: follow quickstart.md Journey 2.

### Tests for User Story 2 (write first; expect red before implementation)

- [X] T007 [P] [US2] Extend `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicantResponsePage.cs` with two banner predicates — `IsReadyToSignBannerVisible()` (targets `data-testid="signing-banner-ready"` per contracts/README.md §4) and `IsAgreementExecutedBannerVisible()` (targets `data-testid="signing-banner-executed"`) — and with a `SigningPanel` property that returns a `SigningStagePanelPage` scoped to the `#funding-agreement-panel-container` on the response page
- [X] T008 [US2] Add `[Test] ApplicantSeesBannerAndPanelOnResponsePage` to `tests/FundingPlatform.Tests.E2E/Tests/SigningWayfindingTests.cs` following quickstart.md Journey 2: seed an application at `ResponseFinalized` with a generated agreement, log in as applicant, open `/ApplicantResponse/Index/{id}`, assert ready-to-sign banner visible + signing panel loaded with Download action; then advance the application to `AgreementExecuted` (either via seeding or by driving the reviewer through the signing flow), refresh, assert executed banner visible + signed-PDF download link present. Test MUST fail before Phase 4 implementation lands. If the Aspire+Playwright fixture cannot seed the preconditions today, add a fixture helper (or fall back to the 006 `Assert.Inconclusive` convention with a pointer to quickstart.md Journey 2, matching `DigitalSignatureTests.cs` precedent — documented as a known limitation if chosen)

### Implementation for User Story 2

- [X] T009 [P] [US2] Modify `src/FundingPlatform.Application/DTOs/ApplicantResponseDto.cs` to append a positional record parameter `bool HasFundingAgreement` after `Items` per `specs/007-signing-wayfinding/data-model.md`; update XML doc comments if the record has any
- [X] T010 [US2] Modify `src/FundingPlatform.Application/Services/ApplicantResponseService.cs` `GetResponseAsync` to populate `HasFundingAgreement` by calling `.Any()` on the application's already-loaded `FundingAgreements` collection; this adds no new database round-trip. Update every internal construction of `ApplicantResponseDto` inside the service to pass the new value; confirm no other callers of the constructor exist outside the service (a quick `grep` over `/src` should return only this service)
- [X] T011 [P] [US2] Modify `src/FundingPlatform.Web/ViewModels/ApplicantResponseViewModel.cs` to add `public bool HasFundingAgreement { get; set; }` with a default of `false`
- [X] T012 [US2] Modify `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs` `BuildViewModel` to pass `HasFundingAgreement` from the DTO into the view-model (one-line pass-through)
- [X] T013 [US2] Modify `src/FundingPlatform.Web/Views/ApplicantResponse/Index.cshtml` to render (a) the state-driven banner immediately above the existing response-item form per `contracts/README.md §4` truth table with the stable `data-testid` hooks, and (b) the async-fetch signing-panel placeholder and IIFE **below** the existing form and appeal controls — copy the placeholder `<div>` and `<script>` blocks **byte-identically** from `src/FundingPlatform.Web/Views/Application/Details.cshtml:159-177` per `contracts/README.md §5` (FR-004 mandates divergence is a defect)

**Checkpoint**: US2 is fully functional. Run `SigningWayfindingTests.ApplicantSeesBannerAndPanelOnResponsePage` and confirm green. Walk quickstart.md Journey 2 manually. US1 + US2 together deliver the full user-facing wayfinding behavior; US3 is docs-only polish.

---

## Phase 5: User Story 3 — Quickstart prose sync (Priority: P2)

**Goal**: `specs/006-digital-signatures/quickstart.md` no longer refers ambiguously to "the application's detail page"; every journey's prose names concrete entry points (applicants use `/ApplicantResponse/Index/{id}`; reviewers use `/Review` → **Signing Inbox** tab). No journey gains or loses steps. SC-004 met.

**Independent Test**: Manual — a first-time QA walker completes all four 006 SC-008 journeys following only the updated quickstart prose.

### Implementation for User Story 3

- [X] T014 [US3] Apply exactly the five sentence-level prose edits to `specs/006-digital-signatures/quickstart.md` per `specs/007-signing-wayfinding/research.md §R-005` table (Journey 1 steps 2, 7, 8; Journey 2 step 2; Journey 4 version-mismatch step 5). Verify line-count deltas are small (no paragraphs added or removed); verify the four journey headings and their step numbering are unchanged; verify no other file is touched

**Checkpoint**: US3 is fully functional. Manual verification by reading the updated quickstart and noting that "the application's detail page" no longer appears as a navigation instruction.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Regression guard for the existing `/Application/Details/{id}` embed and final verification.

- [X] T015 [P] Add `[Test] ApplicationDetailsEmbedStillRenders` to `tests/FundingPlatform.Tests.E2E/Tests/SigningWayfindingTests.cs` per SC-007(c) and quickstart.md Journey 3: log in as reviewer, visit `/Application/Details/{id}` for a seeded application, assert the existing signing panel loads and exposes reviewer actions (Approve / Reject / regenerate controls). This test guards against regressions in the Application/Details embed introduced while editing shared view-model plumbing (FR-005, SC-006)
- [X] T016 Run `dotnet test` full suite at the repo root and confirm zero failures and zero regressions across unit, integration, and E2E tiers per SC-006. If any existing test regresses, the regression — not the new feature — is the defect; fix and re-run

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — can start immediately
- **Foundational (Phase 2)**: intentionally empty; no blocker for user stories
- **Phase 3 (US1)**: depends only on Phase 1
- **Phase 4 (US2)**: depends only on Phase 1; independent of US1 (disjoint files)
- **Phase 5 (US3)**: depends only on Phase 1; benefits from US1 and US2 being merged first so the prose references real UI, but the edit itself has no code dependency and can be written ahead of implementation if desired
- **Phase N (Polish)**: depends on US1 and US2 implementation being complete; the regression test (T015) verifies US2's DTO/view-model plumbing did not break the sibling Application/Details embed

### User Story Dependencies

- **US1 (P1)**: no cross-story dependencies. MVP candidate on its own.
- **US2 (P1)**: no cross-story dependencies. MVP candidate on its own.
- **US3 (P2)**: no code dependency; the prose names entry points that exist today after US1 + US2 land. A docs PR that ships ahead of US1/US2 would reference non-existent UI — not recommended.

### Within Each User Story

- Tests are written first (T002→T003 for US1; T007→T008 for US2) and expected to fail before implementation
- View-model/DTO layer before view (US2: T009 → T010 → T011 → T012 → T013)
- Partial before host views (US1: T004 → T005/T006)

### Parallel Opportunities

- T002 (page-object) and T003 (test) can be worked in parallel — different files
- T007 (page-object) and T008 (test) can be worked in parallel
- T009/T010 (DTO + service) and T011 (view-model) are different files — can run in parallel
- T005 and T006 touch different files (`Review/Index.cshtml` vs `Review/SigningInbox.cshtml`) — can run in parallel after T004
- US1, US2, and US3 are fully independent after Phase 1; three developers can work them concurrently. Merging order only affects visual consistency of the live demo (US3 should merge after US1 + US2)

---

## Parallel Example: Phase 3 (US1)

```bash
# Launch test scaffolding and partial creation in parallel:
Task: "Extend ReviewQueuePage.cs with tab methods (T002)"
Task: "Create _ReviewTabs.cshtml partial (T004)"
# Then the two view edits in parallel once T004 merges:
Task: "Modify Review/Index.cshtml to include _ReviewTabs (T005)"
Task: "Modify Review/SigningInbox.cshtml to include _ReviewTabs (T006)"
# Test file creation (T003) waits on T002 for the page-object methods to exist.
```

## Parallel Example: Phase 4 (US2)

```bash
# Launch DTO, view-model, and test-plumbing in parallel:
Task: "Add HasFundingAgreement to ApplicantResponseDto (T009)"
Task: "Add HasFundingAgreement to ApplicantResponseViewModel (T011)"
Task: "Extend ApplicantResponsePage.cs with banner methods (T007)"
# Then serial: service → controller → view, since each depends on the previous layer.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1.
2. Skip Phase 2 (intentionally empty).
3. Complete Phase 3 (US1).
4. **STOP and VALIDATE**: run `SigningWayfindingTests.ReviewerReachesSigningInboxInTwoClicks`; walk quickstart.md Journey 1.
5. Demo/ship if desired — Signing Inbox is now discoverable, which was your original blocker.

### Incremental Delivery

1. Phase 1 → baseline green.
2. US1 → Signing Inbox discoverable → demo.
3. US2 → applicant can complete signing flow from their response page → demo.
4. US3 → docs match reality → ship.
5. Polish → regression test + full suite → merge with confidence.

### Parallel Team Strategy

With two developers:

1. Both land Phase 1 together (trivial).
2. Dev A → US1 (touches `ReviewController` views, page object, new test).
3. Dev B → US2 (touches DTO, service, view-model, controller, response view, page object, new test).
4. Either dev → US3 (docs only, conflict-free with US1/US2 code edits).
5. Either dev → Polish + full-suite verification.

No cross-story file contention exists because US1 and US2 touch disjoint files.

---

## Notes

- Every task has an explicit file path.
- No task introduces a new NuGet package, a new project, or a new domain entity. Verifying this before committing each task is a cheap sanity check.
- Commit after each task or logical group; each checkpoint should keep the build green.
- If T008's preconditions cannot be seeded with today's fixture, the task allows the 006 `Assert.Inconclusive` fallback with a pointer to the manual journey (precedent set by `DigitalSignatureTests.cs`) — but real runnable tests are strongly preferred since this feature's journeys are simpler to seed than 006's.
- FR-004 and SC-005 invariants: the byte-identical copy of the async-fetch placeholder + IIFE from `Views/Application/Details.cshtml:159-177` is a non-negotiable part of T013. Divergence between the two embed sites is a defect.
- The `HasFundingAgreement` flag MUST be derived at query time, not stored on the application entity. Persisting it violates Constitution IV (schema-first) and Constitution VI (simplicity).
