# bsd-ps Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-23

## Active Technologies
- C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire (002-review-approval-workflow)
- SQL Server (Aspire-managed container for dev, dacpac schema management) (002-review-approval-workflow)
- C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire, Syncfusion.HtmlToPdfConverter.Net.Linux (31.2.x), Syncfusion.Licensing (005-funding-agreement-generation)
- SQL Server (Aspire-managed container for dev, dacpac schema management); local file system for PDF bytes via `IFileStorageService` (005-funding-agreement-generation)
- C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. No new dependencies introduced by this feature. (006-digital-signatures)
- SQL Server (Aspire-managed container for dev, dacpac schema management); local file system for signed-PDF bytes via the existing `IFileStorageService`. No new storage subsystems. (006-digital-signatures)
- SQL Server (Aspire-managed container for dev, dacpac schema management). **No schema changes.** No new storage subsystems. (007-signing-wayfinding)

- C# / .NET 8+ (latest LTS) + ASP.NET MVC, .NET Aspire, EF Core, ASP.NET Identity, Playwright (001-core-model-submission)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET 8+ (latest LTS)

## Code Style

C# / .NET 8+ (latest LTS): Follow standard conventions

## Recent Changes
- 007-signing-wayfinding: Added C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. **No new dependencies introduced by this feature.**
- 006-digital-signatures: Added C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire. No new dependencies introduced by this feature.
- 005-funding-agreement-generation: Added C# / .NET 10.0 + ASP.NET MVC, Entity Framework Core 10.0, ASP.NET Identity, .NET Aspire, Syncfusion.HtmlToPdfConverter.Net.Linux (31.2.x), Syncfusion.Licensing


<!-- MANUAL ADDITIONS START -->

## Testing conventions

- **E2E tests use ephemeral SQL.** `AppHost.cs` skips `WithDataVolume("fundingplatform-sqldata")` when `--EphemeralStorage=true` is passed, and `AspireFixture` passes that flag — so every E2E fixture run starts with a clean SQL Server container. `dotnet run --project src/FundingPlatform.AppHost` (dev mode) keeps the persistent volume.

<!-- MANUAL ADDITIONS END -->
