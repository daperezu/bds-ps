# Implementation Plan: Core Data Model & Application Submission

**Branch**: `001-core-model-submission` | **Date**: 2026-04-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-core-model-submission/spec.md`

## Summary

Build the foundational data model and application submission workflow for the Funding Request & Evaluation Platform. Uses Clean Architecture (Domain, Application, Infrastructure, Web) with ASP.NET MVC, .NET Aspire orchestration, SQL Server with dacpac schema management, EF Core for data access, ASP.NET Identity for authentication, and Playwright for e2e testing. The system allows applicants to create draft funding applications, add items with dynamic impact definitions and supplier quotations, and submit them with full business rule validation.

## Technical Context

**Language/Version**: C# / .NET 8+ (latest LTS)
**Primary Dependencies**: ASP.NET MVC, .NET Aspire, EF Core, ASP.NET Identity, Playwright
**Storage**: SQL Server (Aspire-managed container for dev, dacpac for schema deployment)
**Testing**: NUnit + Playwright (e2e), xUnit or NUnit (unit/integration)
**Target Platform**: Linux/Windows server (web application)
**Project Type**: Web application (ASP.NET MVC with .NET Aspire orchestration)
**Performance Goals**: Page loads < 2 seconds, submission flow < 10 minutes for 3-item application
**Constraints**: No EF migrations (dacpac only), local file system for document storage
**Scale/Scope**: < 100 concurrent users initially, ~8 core entities, ~15 pages/views

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution is template placeholder (not yet configured for this project). No gates to evaluate. **PASS** — no violations possible.

Post-Phase 1 re-check: Still template placeholder. **PASS**.

## Project Structure

### Documentation (this feature)

```text
specs/001-core-model-submission/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (MVC routes)
├── implementation-notes.md  # Brainstorm decisions
├── review_brief.md      # Reviewer guide
├── REVIEW-SPEC.md       # Spec review report
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code (repository root)

```text
src/
├── FundingPlatform.Domain/              # Domain entities, value objects, interfaces
│   ├── Entities/
│   │   ├── Application.cs
│   │   ├── Applicant.cs
│   │   ├── Item.cs
│   │   ├── Category.cs
│   │   ├── Impact.cs
│   │   ├── ImpactTemplate.cs
│   │   ├── ImpactTemplateParameter.cs
│   │   ├── Supplier.cs
│   │   ├── Quotation.cs
│   │   ├── Document.cs
│   │   ├── SystemConfiguration.cs
│   │   └── VersionHistory.cs
│   ├── Enums/
│   │   ├── ApplicationState.cs
│   │   └── ParameterDataType.cs
│   ├── Interfaces/
│   │   ├── IApplicationRepository.cs
│   │   ├── ICategoryRepository.cs
│   │   ├── IImpactTemplateRepository.cs
│   │   ├── ISupplierRepository.cs
│   │   ├── ISystemConfigurationRepository.cs
│   │   └── IFileStorageService.cs
│   └── FundingPlatform.Domain.csproj
│
├── FundingPlatform.Application/         # Use cases, DTOs, validation
│   ├── Applications/
│   │   ├── Commands/
│   │   │   ├── CreateApplicationCommand.cs
│   │   │   ├── AddItemCommand.cs
│   │   │   ├── UpdateItemCommand.cs
│   │   │   ├── RemoveItemCommand.cs
│   │   │   ├── AddSupplierQuotationCommand.cs
│   │   │   ├── ReplaceQuotationDocumentCommand.cs
│   │   │   └── SubmitApplicationCommand.cs
│   │   └── Queries/
│   │       ├── GetApplicationQuery.cs
│   │       ├── GetApplicationsForApplicantQuery.cs
│   │       └── GetApplicationDetailsQuery.cs
│   ├── Admin/
│   │   ├── Commands/
│   │   │   ├── CreateImpactTemplateCommand.cs
│   │   │   ├── UpdateImpactTemplateCommand.cs
│   │   │   └── UpdateSystemConfigurationCommand.cs
│   │   └── Queries/
│   │       ├── GetImpactTemplatesQuery.cs
│   │       └── GetSystemConfigurationQuery.cs
│   ├── DTOs/
│   ├── Interfaces/
│   │   └── IApplicationService.cs
│   ├── Services/
│   │   ├── ApplicationService.cs
│   │   └── AdminService.cs
│   ├── DependencyInjection.cs
│   └── FundingPlatform.Application.csproj
│
├── FundingPlatform.Infrastructure/      # EF Core, file storage, Identity
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── ApplicationConfiguration.cs
│   │   │   ├── ApplicantConfiguration.cs
│   │   │   ├── ItemConfiguration.cs
│   │   │   ├── CategoryConfiguration.cs
│   │   │   ├── ImpactConfiguration.cs
│   │   │   ├── ImpactTemplateConfiguration.cs
│   │   │   ├── SupplierConfiguration.cs
│   │   │   ├── QuotationConfiguration.cs
│   │   │   ├── DocumentConfiguration.cs
│   │   │   ├── SystemConfigurationConfiguration.cs
│   │   │   └── VersionHistoryConfiguration.cs
│   │   └── Repositories/
│   │       ├── ApplicationRepository.cs
│   │       ├── CategoryRepository.cs
│   │       ├── ImpactTemplateRepository.cs
│   │       ├── SupplierRepository.cs
│   │       └── SystemConfigurationRepository.cs
│   ├── FileStorage/
│   │   └── LocalFileStorageService.cs
│   ├── Identity/
│   │   └── IdentityConfiguration.cs
│   ├── DependencyInjection.cs
│   └── FundingPlatform.Infrastructure.csproj
│
├── FundingPlatform.Web/                 # ASP.NET MVC controllers, views, Program.cs
│   ├── Controllers/
│   │   ├── HomeController.cs
│   │   ├── AccountController.cs
│   │   ├── ApplicationController.cs
│   │   ├── ItemController.cs
│   │   ├── SupplierController.cs
│   │   ├── QuotationController.cs
│   │   └── AdminController.cs
│   ├── Views/
│   │   ├── Home/
│   │   ├── Account/
│   │   ├── Application/
│   │   ├── Item/
│   │   ├── Supplier/
│   │   ├── Quotation/
│   │   ├── Admin/
│   │   └── Shared/
│   ├── ViewModels/
│   ├── wwwroot/
│   ├── Program.cs
│   └── FundingPlatform.Web.csproj
│
├── FundingPlatform.AppHost/             # .NET Aspire orchestration
│   ├── Program.cs
│   └── FundingPlatform.AppHost.csproj
│
├── FundingPlatform.ServiceDefaults/     # Shared Aspire defaults (telemetry, health)
│   ├── Extensions.cs
│   └── FundingPlatform.ServiceDefaults.csproj
│
└── FundingPlatform.Database/            # SQL Server Database Project (dacpac)
    ├── Tables/
    │   ├── dbo.Applicants.sql
    │   ├── dbo.Applications.sql
    │   ├── dbo.Items.sql
    │   ├── dbo.Categories.sql
    │   ├── dbo.ImpactTemplates.sql
    │   ├── dbo.ImpactTemplateParameters.sql
    │   ├── dbo.Impacts.sql
    │   ├── dbo.ImpactParameterValues.sql
    │   ├── dbo.Suppliers.sql
    │   ├── dbo.Quotations.sql
    │   ├── dbo.Documents.sql
    │   ├── dbo.SystemConfigurations.sql
    │   └── dbo.VersionHistory.sql
    ├── PostDeployment/
    │   └── SeedData.sql
    ├── Security/
    └── FundingPlatform.Database.sqlproj

tests/
├── FundingPlatform.Tests.Unit/          # Domain and application layer unit tests
│   └── FundingPlatform.Tests.Unit.csproj
├── FundingPlatform.Tests.Integration/   # Infrastructure/database integration tests
│   └── FundingPlatform.Tests.Integration.csproj
└── FundingPlatform.Tests.E2E/           # Playwright end-to-end tests
    ├── PageObjects/
    │   ├── LoginPage.cs
    │   ├── DashboardPage.cs
    │   ├── ApplicationPage.cs
    │   ├── ItemPage.cs
    │   └── AdminPage.cs
    ├── Fixtures/
    │   ├── AspireFixture.cs
    │   └── AuthenticatedTestBase.cs
    ├── Tests/
    │   ├── AuthenticationTests.cs
    │   ├── ApplicationSubmissionTests.cs
    │   ├── ItemManagementTests.cs
    │   ├── SupplierQuotationTests.cs
    │   ├── ImpactTemplateTests.cs
    │   └── AdminConfigurationTests.cs
    └── FundingPlatform.Tests.E2E.csproj

FundingPlatform.sln
```

**Structure Decision**: Clean Architecture with 4 core layers (Domain, Application, Infrastructure, Web) plus Aspire orchestration (AppHost, ServiceDefaults), a SQL Server Database Project for schema management, and 3 test projects (unit, integration, e2e). Project references follow the dependency rule: Web → Application + Infrastructure, Infrastructure → Application → Domain. AppHost → Web.

## Complexity Tracking

No constitution violations to track.
