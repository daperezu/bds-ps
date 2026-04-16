# Implementation Plan: Review & Approval Workflow

**Branch**: `002-review-approval-workflow` | **Date**: 2026-04-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-review-approval-workflow/spec.md`

## Summary

Enable internal reviewers to evaluate submitted funding applications through a structured item-level review process. Extends the existing Application and Item entities with review states, comments, supplier selection, and technical equivalence flagging. Adds a paginated review queue, a detailed review screen, and finalization workflow. Follows the existing Clean Architecture and rich domain model patterns established in spec 001.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire
**Storage**: SQL Server (Aspire-managed container for dev, dacpac schema management)
**Testing**: Playwright for .NET (NUnit runner, Page Object Model), xUnit for unit/integration
**Target Platform**: Linux/Windows server (ASP.NET Kestrel)
**Project Type**: Web application (server-side MVC)
**Performance Goals**: Review queue page loads under 2 seconds, review screen under 3 seconds
**Constraints**: Optimistic concurrency on Application entity, role-based authorization
**Scale/Scope**: Under 100 concurrent users, moderate application volume

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | Extends existing 4-layer structure. New review logic in Domain (state transitions, item decisions), new commands/queries in Application, new EF configurations in Infrastructure, new controllers/views in Web. Dependencies point inward. |
| II. Rich Domain Model | PASS | Review state transitions (`StartReview()`, `SendBack()`, `Finalize()`) and item decisions (`Approve()`, `Reject()`, `RequestMoreInfo()`, `FlagNotEquivalent()`) belong on the entities. No anemic model. |
| III. E2E Testing | PASS | FR-019 mandates Playwright e2e tests for every requirement. Page Object Model pattern will be used (ReviewQueuePage, ReviewApplicationPage). |
| IV. Schema-First Database | PASS | New columns on Items table and new ApplicationState enum values will be added via SQL files in the Database project. No EF migrations. |
| V. Specification-Driven Development | PASS | Spec approved, plan being created, tasks will follow. |
| VI. Simplicity | PASS | Approach A selected (direct review on existing entities) over more complex Resolution entity. No new entities. Supplier scoring deferred. |

**Gate Result**: PASS — No violations. Proceed to Phase 0.

### Post-Design Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | ReviewService in Application layer orchestrates domain methods. ReviewController in Web layer. No cross-layer violations. |
| II. Rich Domain Model | PASS | All review business rules (equivalence auto-rejection, supplier selection requirement, finalization validation) enforced by domain entity methods. |
| III. E2E Testing | PASS | 7 user stories → 7+ test classes with page objects. |
| IV. Schema-First Database | PASS | 5 new columns on Items, 2 new enum values on ApplicationState, new index on Application.State for queue filtering. All via .sql files. |
| V. Specification-Driven Development | PASS | Following the workflow. |
| VI. Simplicity | PASS | No new entities, no new projects, minimal new abstractions. ReviewService follows same pattern as ApplicationService. |

**Gate Result**: PASS — No violations.

## Project Structure

### Documentation (this feature)

```text
specs/002-review-approval-workflow/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── review-endpoints.md
├── checklists/
│   └── requirements.md
├── review_brief.md
├── REVIEW-SPEC.md
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
src/
├── FundingPlatform.Domain/
│   ├── Entities/
│   │   ├── Application.cs          # MODIFY: Add StartReview(), SendBack(), Finalize() methods
│   │   └── Item.cs                 # MODIFY: Add review fields and methods
│   └── Enums/
│       ├── ApplicationState.cs     # MODIFY: Add UnderReview=2, Resolved=3
│       └── ItemReviewStatus.cs     # NEW: Pending=0, Approved=1, Rejected=2, NeedsInfo=3
│
├── FundingPlatform.Application/
│   ├── Applications/
│   │   ├── Commands/
│   │   │   ├── StartReviewCommand.cs           # NEW
│   │   │   ├── ReviewItemCommand.cs            # NEW
│   │   │   ├── FlagTechnicalEquivalenceCommand.cs  # NEW
│   │   │   ├── SendBackApplicationCommand.cs   # NEW
│   │   │   └── FinalizeReviewCommand.cs        # NEW
│   │   └── Queries/
│   │       ├── GetReviewQueueQuery.cs          # NEW
│   │       └── GetApplicationForReviewQuery.cs # NEW
│   ├── DTOs/
│   │   ├── ReviewQueueItemDto.cs               # NEW
│   │   └── ReviewApplicationDto.cs             # NEW
│   └── Services/
│       └── ReviewService.cs                    # NEW
│
├── FundingPlatform.Infrastructure/
│   └── Persistence/
│       └── Configurations/
│           ├── ApplicationConfiguration.cs     # MODIFY: Add index on State
│           └── ItemConfiguration.cs            # MODIFY: Add review field mappings
│
├── FundingPlatform.Web/
│   ├── Controllers/
│   │   └── ReviewController.cs                 # NEW
│   ├── ViewModels/
│   │   ├── ReviewQueueViewModel.cs             # NEW
│   │   └── ReviewApplicationViewModel.cs       # NEW
│   └── Views/
│       └── Review/
│           ├── Index.cshtml                    # NEW: Queue page
│           └── Review.cshtml                   # NEW: Review detail page
│
├── FundingPlatform.Database/
│   ├── Tables/
│   │   └── Items.sql                           # MODIFY: Add review columns
│   └── PostDeployment/
│       └── SeedData.sql                        # MODIFY: Add Reviewer role

tests/
└── FundingPlatform.Tests.E2E/
    ├── PageObjects/
    │   ├── ReviewQueuePage.cs                  # NEW
    │   └── ReviewApplicationPage.cs            # NEW
    └── Tests/
        ├── ReviewQueueTests.cs                 # NEW
        ├── ReviewApplicationTests.cs           # NEW
        ├── ReviewItemDecisionTests.cs          # NEW
        ├── TechnicalEquivalenceTests.cs        # NEW
        ├── SupplierSelectionTests.cs           # NEW
        ├── SendBackApplicationTests.cs         # NEW
        └── FinalizeReviewTests.cs              # NEW
```

**Structure Decision**: Extends the existing Clean Architecture 4-layer structure from spec 001. No new projects needed. New code follows the same directory conventions — commands in `Applications/Commands/`, DTOs in `DTOs/`, services in `Services/`, etc.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
