# Quickstart: Applicant Response & Appeal

**Audience**: Developers implementing or extending this feature.

## Prerequisites

- The repository builds and runs per root `README.md` / `CLAUDE.md`.
- `FundingPlatform.AppHost` launches successfully with `dotnet run --project src/FundingPlatform.AppHost`.
- Seeded dev data includes at least one application that has been reviewed and finalized (spec 002 state = `Resolved`).

## Running the Feature End-to-End

### 1. Prepare a Resolved application

From the Aspire dashboard, open the Web app and log in as an Applicant. Create a draft application, submit it, then log in as a Reviewer (or seeded `reviewer@example.com`) and finalize the review via `Review/Finalize` with a mix of approved and rejected items.

### 2. As the Applicant: respond per item

Navigate to `/ApplicantResponse/Index/{applicationId}`. You should see:

- One row per item with the reviewer's decision (approved + supplier + amount, or rejected + reason).
- An Accept / Reject radio (or checkbox) per row.
- A disabled "Submit Response" button until every item has a decision.

Make a decision on each item and submit. The application state transitions to `ResponseFinalized`. Items you accepted are flagged as having cleared the response stage.

### 3. (Optional) As the Applicant: open an appeal

If you rejected at least one item, an "Open Appeal" button appears on `/ApplicantResponse/Index/{applicationId}`. Click it. The application state transitions to `AppealOpen`. You are redirected to `/ApplicantResponse/Appeal/{applicationId}` — the dispute thread.

### 4. As a Reviewer: engage in the thread

Log in as any user with the Reviewer role. Open `/ApplicantResponse/Appeal/{applicationId}`. You see:

- The full message history.
- A form to post a reply.
- A resolution form with three options: **Uphold**, **Grant — Reopen to Draft**, **Grant — Reopen to Review**.

Post one or more replies, then resolve. Observe the application state transition:

- `Uphold` → `ResponseFinalized`
- `Grant — Reopen to Draft` → `Draft` (applicant can now edit the application)
- `Grant — Reopen to Review` → `UnderReview` (reviewers can revise their decisions)

### 5. Verify the appeal cap

- Open `/Admin/SystemConfiguration` and confirm `MaxAppealsPerApplication` (default `1`).
- After cycling the same application through enough appeals to hit the cap, the "Open Appeal" button is disabled and shows a message explaining the cap.

## Key Files to Know

| Concern | Path |
|---------|------|
| Domain aggregates | `src/FundingPlatform.Domain/Entities/{ApplicantResponse,ItemResponse,Appeal,AppealMessage}.cs` |
| State machine | `src/FundingPlatform.Domain/Entities/Application.cs` (new methods for response/appeal) |
| Enums | `src/FundingPlatform.Domain/Enums/{ApplicationState,ItemResponseDecision,AppealStatus,AppealResolution}.cs` |
| Use cases | `src/FundingPlatform.Application/Applications/Commands/` and `Queries/` |
| EF configuration | `src/FundingPlatform.Infrastructure/Persistence/Configurations/{ApplicantResponse,ItemResponse,Appeal,AppealMessage}Configuration.cs` |
| Database schema | `src/FundingPlatform.Database/dbo/Tables/{ApplicantResponses,ItemResponses,Appeals,AppealMessages}.sql` |
| Seed data for appeal cap | `src/FundingPlatform.Database/dbo/Post-Deployment/` (add or extend existing post-deployment script) |
| Controller | `src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs` |
| Views | `src/FundingPlatform.Web/Views/ApplicantResponse/` |

## Running the Tests

### Unit tests

```bash
dotnet test tests/FundingPlatform.Tests.Unit
```

Expected new classes: `ApplicantResponseTests`, `AppealTests`, `ApplicationResponseTransitionsTests`. These exercise domain invariants with no database.

### Integration tests

```bash
dotnet test tests/FundingPlatform.Tests.Integration
```

Expected new class: `ApplicantResponsePersistenceTests`. Exercises EF configuration + SQL schema on an Aspire-provided test database.

### End-to-end tests (NON-NEGOTIABLE)

```bash
dotnet test tests/FundingPlatform.Tests.E2E --filter FullyQualifiedName~ApplicantResponseTests
```

Expected new class: `ApplicantResponseTests`. One test method per user story:

- `Applicant_Can_Respond_Per_Item_And_See_Accepted_Items_Advance` (P1)
- `Applicant_Can_Open_Appeal_On_Rejected_Items_And_Application_Freezes` (P2)
- `Reviewer_Can_Resolve_Appeal_With_All_Three_Outcomes` (P3)

Each test uses Page Objects (`ApplicantResponsePage`, `AppealThreadPage`) and asserts both UI state and persisted application state.

## Common Development Tasks

### Adding a new response-related validation

1. Add the rule to the appropriate domain method (`ApplicantResponse.Submit`, `Application.OpenAppeal`, etc.) with a specific exception.
2. Add a unit test in `ApplicantResponseTests` or `AppealTests` that provokes the rule.
3. Surface the error in the view model so the controller can display it.
4. Add an E2E assertion if the rule has a user-visible trigger.

### Changing the appeal cap at runtime

Update the `MaxAppealsPerApplication` row in the `SystemConfigurations` table (via `/Admin/SystemConfiguration` UI). No redeploy, no migration. Value `0` disables appeals entirely.

### Debugging a stuck application

If an application shows `State == AppealOpen` but appears "silent," check:

1. Does an `Appeal` row exist with `Status = 0 (Open)` for that application?
2. Does any user with the Reviewer role have visibility on the application? (Role check in controller.)
3. Check the `AppealMessages` table for message activity.

## Out-of-Scope Reminders

This feature intentionally does not:

- Send notifications (email, push, in-app).
- Support attachments on appeal messages.
- Enforce deadlines or auto-decide on timeout.
- Provide bulk "accept all" / "reject all" shortcuts.
- Export thread or response data.

If a stakeholder requests any of the above, direct them to the spec's Out of Scope section and point toward the corresponding future feature.
