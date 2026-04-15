# Review Brief: Core Data Model & Application Submission

**Spec:** specs/001-core-model-submission/spec.md
**Generated:** 2026-04-15

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

This spec establishes the foundational data model and application submission workflow for a Funding Request & Evaluation Platform that replaces manual Excel-based processes. Applicants (entrepreneurs) create funding requests containing line items, each with structured impact definitions and multiple supplier quotations. The system enforces configurable business rules at submission time. This is the first of several specs — review, approval, payment, and reporting are explicitly deferred.

## Scope Boundaries

- **In scope:** Core entities (Application, Item, Supplier, Quotation, Impact, Category, Document), application submission workflow (Draft → Submitted), database-configurable impact templates, system configuration, file uploads, authentication, version history, Playwright e2e tests
- **Out of scope:** Review/approval workflow, supplier evaluation/scoring, applicant response (accept/reject/appeal), PDF generation, digital signatures, payment, notifications, reporting, external API integrations
- **Why these boundaries:** The submission flow is the foundation everything else depends on. Building it first allows future specs to layer on review, approval, and payment without reworking the core model.

## Critical Decisions

### Dynamic Impact Templates
- **Choice:** Impact types and their parameters are fully database-configurable by administrators, not code-defined
- **Trade-off:** More upfront complexity in the template rendering system, but eliminates developer involvement for adding new impact types
- **Feedback:** Is the template system scope (data types: text, decimal, integer, date) sufficient, or are more complex parameter types needed?

### Submission Validation Strategy
- **Choice:** All validation errors collected and displayed at once, not one-at-a-time
- **Trade-off:** Slightly more complex validation logic, but significantly better UX — applicants can fix everything in one pass
- **Feedback:** Any validation rules missing that should block submission?

### Optimistic Concurrency for Draft Editing
- **Choice:** Row version-based conflict detection with user warning
- **Trade-off:** Simple to implement but doesn't prevent data loss in extreme cases (last-save-wins after warning)
- **Feedback:** Is this sufficient, or do we need real-time collaborative editing?

## Areas of Potential Disagreement

### Scope Size
- **Decision:** 8 user stories covering submission, draft persistence, item management, supplier quotations, dynamic impact, system config, impact template admin, and authentication
- **Why this might be controversial:** Some may argue authentication and admin features should be separate specs
- **Alternative view:** Splitting further would create specs too small to deliver standalone value
- **Seeking input on:** Is this the right granularity, or should admin features (US6, US7, US8) be a separate spec?

### Impact Template Modification Affecting Existing Drafts
- **Decision:** If an admin modifies a template, existing drafts using it get validation errors on next submission
- **Why this might be controversial:** Could frustrate applicants who had completed their impact section
- **Alternative view:** Could version templates and let existing drafts use the version they started with
- **Seeking input on:** Is the "validate against current template" approach acceptable?

### No Draft Auto-Cleanup
- **Decision:** Draft applications persist indefinitely
- **Why this might be controversial:** Could accumulate abandoned drafts over time
- **Alternative view:** Add a configurable retention period with notifications before cleanup
- **Seeking input on:** Should this be addressed now or deferred?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Funding request | Application | Top-level entity containing items |
| Line item | Item (Line) | Single product/service within an application |
| Impact definition schema | ImpactTemplate | Admin-defined template with parameter slots |
| Impact data | Impact | User-provided values for a selected template |
| Price offer | Quotation | Supplier's offer for an item, with document |
| Global settings | SystemConfiguration | Key-value database table |
| Change log | VersionHistory | Audit trail for application changes |

## Open Questions

- [ ] Should there be a maximum number of items per application?
- [ ] Should there be a maximum number of suppliers per item (beyond the minimum)?
- [ ] Is there a retention policy for draft applications that are never submitted?
- [ ] Should the performance score on Applicant be seeded manually, calculated, or deferred?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Dynamic impact template rendering complexity | High | Start with simple data types (text, decimal, integer, date); expand later if needed |
| Schema management split (dacpac + EF Core) | Med | Clear ownership: dacpac owns schema, EF Core maps to it; documented in implementation notes |
| File storage on local file system | Med | Abstracted behind interface; can swap to Azure Blob Storage without domain changes |
| Optimistic concurrency edge cases | Low | Row version + user warning covers majority of cases; real-time collab deferred |

---
*Share with reviewers before implementation.*
