# Brainstorm: Core Data Model & Application Submission

**Date:** 2026-04-15
**Status:** spec-created
**Spec:** specs/001-core-model-submission/

## Problem Framing

The Funding Request & Evaluation Platform needs to replace a manual Excel-based process for submitting, evaluating, and tracking non-reimbursable funding requests for entrepreneurs and incubators. The full SRS describes a large system with multiple subsystems. This brainstorm focused on decomposing the system and then designing the foundational first piece: the core data model and application submission workflow.

## Scope Decomposition

The full SRS was decomposed into independently spec-able subsystems:

1. **Core data model & application submission** (this spec)
2. Review/approval workflow
3. Supplier evaluation & scoring engine
4. Applicant response (accept/reject/appeal)
5. Document generation & digital signatures
6. Payment & closure
7. Notifications
8. Reporting & exports

## Approaches Considered

### A: Rich Domain Model (Selected)
- Entities encapsulate business rules (e.g., `Application.Submit()` validates before state transition)
- Pros: Co-located business logic, easy to test in isolation, scales with complexity
- Cons: More upfront modeling effort

### B: Anemic Model + Service Layer
- Entities are plain data containers, logic lives in services
- Pros: Simpler entities, familiar CRUD pattern
- Cons: Business rules scatter, harder to enforce invariants

### C: CQRS (Command/Query Separation)
- Separate write and read models
- Pros: Independent optimization of read/write
- Cons: Significant overhead for this stage, premature complexity

## Decision

Selected **Approach A: Rich Domain Model** with Clean Architecture. The funding domain has real business invariants (minimum quotations, impact completeness, state transitions) that belong on the entities. Combined with:

- **.NET stack**: ASP.NET MVC + .NET Aspire orchestration
- **SQL Server** with Database Project (dacpac) for schema, EF Core for data access only
- **ASP.NET Identity** for authentication
- **Local file system** for document storage
- **Playwright** for mandatory e2e testing of every feature
- **Database-configurable** impact templates and system settings

## Open Threads

- Should there be a maximum number of items per application?
- Should there be a maximum number of suppliers per item beyond the minimum?
- Retention policy for abandoned draft applications
- Performance score on Applicant: manual, calculated, or deferred?
- Constitution needs to be filled in after first implementation establishes project patterns
