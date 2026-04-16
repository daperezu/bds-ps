# Quickstart: Review & Approval Workflow

**Date**: 2026-04-15
**Feature**: specs/002-review-approval-workflow/

## Prerequisites

- .NET 10.0 SDK installed
- Docker running (for SQL Server container via Aspire)
- Spec 001 (core-model-submission) fully implemented and merged

## Running the Application

```bash
# From repo root
cd src/FundingPlatform.AppHost
dotnet run
```

Aspire will start the SQL Server container, deploy the dacpac schema, and launch the web application. Open the Aspire dashboard URL shown in the console to find the web app endpoint.

## Testing the Review Workflow

### Setup Test Data

1. Register as an applicant (or use seeded credentials if available)
2. Create an application with at least one item
3. Add suppliers and quotations to each item (minimum 2 per system config)
4. Define impact for each item
5. Submit the application

### Review as a Reviewer

1. Log in as a user with the "Reviewer" role
2. Navigate to `/Review` to see the review queue
3. Click on an application to open the review screen
4. For each item:
   - Check technical equivalence of quotations
   - Approve (select supplier), reject, or request more info
   - Add optional comments
5. Finalize the review or send back for more info

### Creating a Reviewer User

After the Reviewer role is seeded, assign it to a user:

```sql
-- Find the role ID
SELECT Id FROM AspNetRoles WHERE Name = 'Reviewer';

-- Assign to a user
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'reviewer@example.com' AND r.Name = 'Reviewer';
```

Or register a new user and assign the role through the admin interface (if available).

## Running E2E Tests

```bash
# From repo root
cd tests/FundingPlatform.Tests.E2E
dotnet test --filter "Category=Review"
```

Tests use the Aspire fixture to spin up the full stack (AppHost → Web → SQL Server), deploy the dacpac, and run Playwright browser tests.

## Key Files

| File | Purpose |
|------|---------|
| `src/FundingPlatform.Domain/Entities/Application.cs` | State transitions (StartReview, SendBack, Finalize) |
| `src/FundingPlatform.Domain/Entities/Item.cs` | Review decisions (Approve, Reject, RequestMoreInfo, FlagNotEquivalent) |
| `src/FundingPlatform.Domain/Enums/ApplicationState.cs` | UnderReview, Resolved states |
| `src/FundingPlatform.Domain/Enums/ItemReviewStatus.cs` | Pending, Approved, Rejected, NeedsInfo |
| `src/FundingPlatform.Application/Services/ReviewService.cs` | Review orchestration |
| `src/FundingPlatform.Web/Controllers/ReviewController.cs` | Review queue and review actions |
| `src/FundingPlatform.Database/Tables/Items.sql` | Review columns schema |
| `src/FundingPlatform.Database/PostDeployment/SeedData.sql` | Reviewer role seed |
