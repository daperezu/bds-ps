# Quickstart: Supplier Evaluation Engine

## Prerequisites

- .NET 10.0 SDK
- Docker (for Aspire SQL Server container)
- Node.js (for Playwright)
- Playwright browsers installed (`pwsh bin/Debug/net10.0/playwright.ps1 install` or `npx playwright install`)

## Build & Run

```bash
# From repo root
dotnet build
cd src/FundingPlatform.AppHost
dotnet run
```

The Aspire dashboard shows all services. The web app URL will be displayed in the console output.

## Run Tests

```bash
# Unit tests (includes SupplierScore tests)
dotnet test tests/FundingPlatform.Tests.Unit

# E2E tests (requires Aspire stack running)
dotnet test tests/FundingPlatform.Tests.E2E
```

## Key Files for This Feature

| What | Where |
|------|-------|
| Scoring logic | `src/FundingPlatform.Domain/ValueObjects/SupplierScore.cs` |
| Supplier entity | `src/FundingPlatform.Domain/Entities/Supplier.cs` |
| Review service | `src/FundingPlatform.Application/Services/ReviewService.cs` |
| Review screen | `src/FundingPlatform.Web/Views/Review/Review.cshtml` |
| Supplier form | `src/FundingPlatform.Web/Views/Supplier/Add.cshtml` |
| DB schema | `src/FundingPlatform.Database/dbo/Tables/Suppliers.sql` |
| Unit tests | `tests/FundingPlatform.Tests.Unit/Domain/SupplierScoreTests.cs` |
| E2E tests | `tests/FundingPlatform.Tests.E2E/Tests/SupplierEvaluationTests.cs` |

## Verification

After implementation, verify:

1. **Create supplier**: Add supplier form shows 3 compliance checkboxes (CCSS, Hacienda, SICOP)
2. **Score display**: Review screen shows scores (e.g., "4/5") with factor breakdown per quotation
3. **Ranking**: Quotations ordered by score descending
4. **Recommendation**: Highest-scoring supplier(s) marked "Recommended"
5. **Pre-selection**: Highest scorer pre-selected in supplier dropdown
6. **Override**: Selecting a different supplier and approving works without friction
