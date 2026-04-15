# Research: Core Data Model & Application Submission

**Date**: 2026-04-15
**Branch**: `001-core-model-submission`

## Research Topics

### 1. .NET Aspire Orchestration with ASP.NET MVC + SQL Server

**Decision**: Use .NET Aspire AppHost to orchestrate the web app and SQL Server container.

**Rationale**: Aspire provides built-in service discovery, health checks, OpenTelemetry, and container management. SQL Server runs as an Aspire-managed container in development, eliminating local SQL Server installation requirements.

**Key findings**:
- AppHost `Program.cs` uses `builder.AddSqlServer("sqlserver").AddDatabase("fundingdb")` to provision SQL Server
- Web project registers DbContext via `builder.AddSqlServerDbContext<AppDbContext>("fundingdb")` — connection string auto-injected
- ServiceDefaults project provides `AddServiceDefaults()` for telemetry, health checks, resilience
- AppHost references the Web project; Web references ServiceDefaults
- Database schema deployed separately via dacpac (not through Aspire)

**Alternatives considered**:
- Docker Compose: more manual configuration, less integrated telemetry
- Manual SQL Server installation: inconsistent dev environments

---

### 2. Clean Architecture with Rich Domain Model

**Decision**: 4-layer Clean Architecture (Domain, Application, Infrastructure, Web) with rich domain entities.

**Rationale**: Business rules (minimum quotations, impact validation, state transitions) belong on entities. Clean Architecture ensures domain logic is isolated from infrastructure concerns.

**Key findings**:
- Domain layer: entities with private setters, validation methods (e.g., `Application.Submit()`), no NuGet dependencies
- Application layer: services/commands, DTOs, references Domain only
- Infrastructure layer: EF Core DbContext, repository implementations, file storage — owns all EF Core packages
- Web layer: MVC controllers, views, DI wiring — references Application and Infrastructure
- EF Core entity configurations use `IEntityTypeConfiguration<T>` in Infrastructure, keeping domain entities persistence-ignorant
- Use backing fields and private setters for DDD pattern compatibility with EF Core
- Each layer exposes a `DependencyInjection.cs` with `AddApplication()` / `AddInfrastructure()` extension methods

**Alternatives considered**:
- Anemic model + service layer: scatters business rules
- Vertical slices: less separation, harder to enforce domain invariants

---

### 3. SQL Server Database Project (dacpac) with EF Core

**Decision**: Use Microsoft.Build.Sql SDK-style project for schema management. EF Core for data access only (no migrations).

**Rationale**: Schema as a first-class artifact with proper diffing, cross-platform builds, and consistent deployments between dev and production.

**Key findings**:
- Use `Microsoft.Build.Sql` SDK-style `.sqlproj` — builds with `dotnet build`, cross-platform
- Project structure: `Tables/`, `Views/`, `StoredProcedures/`, `PostDeployment/`, `Security/`
- File naming: `dbo.TableName.sql` convention
- Local dev deployment: `SqlPackage /Action:Publish` against Aspire-managed SQL Server container
- Production: generate diff script with `/Action:Script`, review, then apply
- Post-deployment scripts for seed data (idempotent MERGE/INSERT-IF-NOT-EXISTS)
- EF Core entity POCOs maintained manually to match `.sql` table definitions
- CI check: build dacpac, deploy to test DB, scaffold and diff to catch drift

**Alternatives considered**:
- EF Core migrations: less control over schema, harder to review diffs
- Database-first with scaffolding only: loses rich domain model benefits

---

### 4. Playwright E2E Testing with .NET Aspire

**Decision**: Use Microsoft.Playwright.NUnit with Aspire.Hosting.Testing for e2e tests.

**Rationale**: Playwright provides reliable cross-browser testing. Aspire.Hosting.Testing starts the full orchestrated stack in tests automatically.

**Key findings**:
- Test project uses `Microsoft.Playwright.NUnit` — test classes inherit `PageTest`
- Aspire integration via `DistributedApplicationTestingBuilder` in a shared fixture
- `[OneTimeSetUp]` starts AppHost, extracts HTTP endpoint URL for the web app
- `[OneTimeTearDown]` disposes the Aspire host
- Page Object Model (POM) recommended: each page gets a class wrapping `IPage` with locators and action methods
- Authentication: login once, save storage state with `BrowserContext.StorageStateAsync()`, reuse in subsequent tests
- Project structure: `PageObjects/`, `Fixtures/`, `Tests/`

**Alternatives considered**:
- Selenium: slower, more brittle, less modern API
- Cypress: JavaScript-only, doesn't integrate with .NET stack

---

## All NEEDS CLARIFICATION Resolved

No remaining unknowns. All technology decisions have been researched and validated.
