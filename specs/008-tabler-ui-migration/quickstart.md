# Quickstart: Manual Verification of Tabler.io UX Sweep

**Spec:** [spec.md](spec.md)
**Plan:** [plan.md](plan.md)
**Date:** 2026-04-25

This document is the manual verification procedure for spec 008. It is the **supplementary visual-regression check** mandated by FR-018 and SC-005 — the **primary** quality gate is the existing Playwright E2E suite plus the new `RoleAwareSidebarTests` (FR-019, FR-020), which run automatically.

This procedure should be executed once after the implementation is complete and before merging the spec branch.

## Prerequisites

- Local Aspire dev stack is running: `dotnet run --project src/FundingPlatform.AppHost`
- Aspire dashboard is reachable; the Web project shows `Healthy`.
- Playwright E2E suite has just passed: `dotnet test tests/FundingPlatform.Tests.E2E` reports zero failures.
- Browser: Chromium (latest) or Firefox (latest), with DevTools console open.
- Test users exist for each role (per existing project conventions; see `tests/FundingPlatform.Tests.E2E/Fixtures/`).

## Pre-spec PDF baseline (capture before sweep)

Before this branch is merged, **on the main branch**, generate one funding-agreement PDF for an existing executed application and save it as `baseline-pre-008.pdf`. This is the visual baseline against which SC-007 / FR-015 will be verified.

```bash
git switch main
# Run the app, log in as a reviewer, generate a PDF for an application with an executed agreement, save the file.
git switch 008-tabler-ui-migration
```

If no executed application exists yet, create a minimal test application end-to-end on `main` first, then capture the PDF, then switch back.

---

## Section A — Role golden paths

Each path verifies one role's primary daily flow renders correctly inside the new shell, with no console errors and no broken layouts.

### A.1 Applicant golden path

1. **Login** as an Applicant. Verify:
   - The Login page renders inside `_AuthLayout` (centered card, no sidebar).
   - After login, the page lands inside `_Layout` (sidebar visible on the left, topbar on top).
   - Console is clean (no errors, no 404s for missing assets).
2. **Navigate** to "My Applications" via the sidebar.
   - Verify the page renders with `_PageHeader` ("My Applications" title), a `_DataTable` of applications, and `_StatusPill` badges showing the application state per row.
   - If you have no applications yet, verify `_EmptyState` renders ("No applications yet" + "Create your first application" action) instead of a flat alert.
3. **Create** a new draft application via the page-header primary action.
   - Verify the Create page renders with `_PageHeader` + `_FormSection` + `_ActionBar` (primary "Create Draft" button, secondary "Back" link).
4. **Add** at least one item to the draft via the application Details page.
   - Verify the Details page shows `_PageHeader`, `_StatusPill` (Draft), the items list as `_DataTable`, and `_ActionBar` with primary actions (Add Item, Submit) styled per the canonical class mapping.
5. **Add** at least two quotations to the item.
   - Verify the Quotation Add form renders with `_FormSection` + `_ActionBar`.
   - Once added, verify each quotation document appears as `_DocumentCard` (file icon + name + size + download link) on the item or details view, NOT as a bare `<a>` link.
6. **Submit** the application.
   - Verify the Submit action triggers `_ConfirmDialog` (one-line "this cannot be undone" rationale; Confirm and Cancel buttons).
   - After confirmation, verify the application status pill changes to "Submitted" (`bg-info`, `ti ti-send` icon, label "Submitted") and a success toast renders in the shell as a Tabler dismissible alert.

**Console check:** Throughout the path, verify the DevTools console shows no errors and no warnings about missing CSS classes.

### A.2 Reviewer golden path

1. **Logout**, then **login** as a Reviewer.
2. **Navigate** to "Review Queue" via the sidebar.
   - Verify the page renders with `_PageHeader`, `_ReviewTabs` (Initial Review Queue / Signing Inbox), `_DataTable` (compact density per the spec convention), `_StatusPill` per row, and `_EmptyState` if the queue is empty.
3. **Open** the Review page for a submitted application.
   - Verify the page renders with `_PageHeader` showing the application reference, `_StatusPill` for the application state, items list with each item's review status as `_StatusPill` (Pending / Approved / Rejected / NeedsInfo), and `_ActionBar` with action buttons styled per class.
4. **Approve** all items, then **Approve** the application as a whole.
   - Verify the Approve action on the application opens `_ConfirmDialog` (state-locking class — amber/warning color, lock icon).
5. **Generate** the funding agreement.
   - Verify the GenerateAgreement page renders with `_PageHeader`, `_ActionBar`, and `_ConfirmDialog`.
   - After generation, navigate to the FundingAgreement Details page and verify the generated PDF renders as `_DocumentCard` with the file icon (PDF), name, size, and download link.
6. **Switch tabs** to "Signing Inbox" via `_ReviewTabs`.
   - Verify the inbox renders with `_PageHeader`, `_DataTable`, `_StatusPill` (SignedUploadStatus per row).
7. **Open** an applicant's signed upload, **Approve** it.
   - Verify the Approve action opens `_ConfirmDialog` (state-locking class).
   - After approval, verify the application status pill changes to "Agreement Executed" (`bg-success`, `ti ti-file-check` icon).

**Console check:** Throughout the path, no errors or warnings.

### A.3 Admin golden path

1. **Logout**, then **login** as an Admin.
2. **Navigate** to "Admin" via the sidebar.
   - Verify the Admin Index page renders with `_PageHeader` and a Tabler card grid for the available admin actions.
3. **Open** "Configuration", change a value, save.
   - Verify the page renders with `_PageHeader` + `_FormSection` + `_ActionBar`.
4. **Open** "Impact Templates", verify the list renders as `_DataTable` (or `_EmptyState` if empty).
5. **Create** a new impact template via "Create Template".
   - Verify `_PageHeader` + `_FormSection` + `_ActionBar`.
6. **Edit** the template; verify the Edit page mirrors the Create page's structure.

**Console check:** No errors or warnings.

---

## Section B — Role-aware sidebar visibility

This section duplicates what `RoleAwareSidebarTests` automates, as a manual sanity check.

### B.1 Applicant sidebar
Logged in as an Applicant, verify the sidebar contains exactly: **Home**, **My Applications**. It MUST NOT show Review Queue, Signing Inbox, or Admin.

### B.2 Reviewer sidebar
Logged in as a Reviewer, verify the sidebar contains exactly: **Home**, **Review Queue**, **Signing Inbox**. It MUST NOT show My Applications or Admin.

### B.3 Admin sidebar
Logged in as an Admin, verify the sidebar contains: **Home**, **Review Queue**, **Signing Inbox**, **Admin**.

### B.4 Unauthenticated auth shell
Logged out, navigate to `/Account/Login` and `/Account/Register`. Verify NO `<aside>` sidebar element exists in the DOM (use DevTools to confirm). The auth shell renders a centered card variant.

---

## Section C — Visual-consistency invariants (grep checks)

Run these from the repo root. All MUST return zero matches (excluding the explicit out-of-scope PDF target files). These mirror SC-006.

```bash
# Badge markup outside _StatusPill
grep -rn 'class="badge' src/FundingPlatform.Web/Views/ \
  --exclude='_StatusPill.cshtml' \
  --exclude='Document.cshtml' \
  --exclude='_FundingAgreementLayout.cshtml'

# Inline style= attributes
grep -rn 'style="' src/FundingPlatform.Web/Views/ \
  --exclude='Document.cshtml' \
  --exclude='_FundingAgreementLayout.cshtml'

# Bare file-anchor markup outside _DocumentCard (heuristic — manual review of any matches)
grep -rinE '<a [^>]*href="[^"]+\.pdf"' src/FundingPlatform.Web/Views/ \
  --exclude='_DocumentCard.cshtml' \
  --exclude='Document.cshtml' \
  --exclude='_FundingAgreementLayout.cshtml'

# Bare alert-info "no data" markup (should ONLY hit TempData rendering in _Layout)
grep -rn 'alert-info' src/FundingPlatform.Web/Views/ \
  --exclude='_Layout.cshtml'
```

If any grep returns a match outside the allowed locations, the sweep is incomplete — return to the offending view and re-skin it.

---

## Section D — PDF parity check (FR-015, SC-007)

### D.1 Source byte-identity

```bash
git diff main -- src/FundingPlatform.Web/Views/FundingAgreement/Document.cshtml
git diff main -- src/FundingPlatform.Web/Views/FundingAgreement/_FundingAgreementLayout.cshtml
```

Both commands MUST report zero changes. If either reports any change, `git restore` it from main before merging.

### D.2 Generated-PDF visual identity

1. Generate a fresh funding-agreement PDF for the same application that produced `baseline-pre-008.pdf`.
2. Save it as `current-008.pdf`.
3. Open both side-by-side. Verify they are visually identical: same typography, spacing, page breaks, signature blocks, table layout, terms text.
4. If a difference is observed, the PDF target was inadvertently affected — diagnose (likely a CSS leak from `_Layout`'s shared assets if the PDF target was changed to use `_Layout`); revert the offending change.

---

## Section E — Viewport sanity

Resize the browser window:

1. **Desktop (1280×800):** sidebar expanded by default, all surfaces render without horizontal scroll.
2. **Mobile (360×740):** sidebar collapses to a hamburger toggle (Tabler default), wide tables scroll horizontally inside `.table-responsive`, page-header titles truncate without breaking layout.

Verify on Chromium, then on Firefox.

---

## Acceptance summary (mirrors spec acceptance criteria)

After completing all sections above without finding a defect:

- ✅ Section A — All three role golden paths complete end-to-end with no console errors and no broken layouts (SC-005)
- ✅ Section B — Role-aware sidebar visibility correct for all four user states (US1 acceptance scenarios 1–4)
- ✅ Section C — Visual-consistency invariants hold across the view tree (SC-002, SC-003, SC-004, SC-006)
- ✅ Section D — PDF target untouched and PDFs visually identical (FR-015, SC-007)
- ✅ Section E — Viewport behavior matches edge-case expectations
- ✅ Existing Playwright E2E suite passes (FR-019)
- ✅ New `RoleAwareSidebarTests` passes (FR-020)

When all of the above are true, the spec is ready to merge.
