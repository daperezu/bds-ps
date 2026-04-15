# Implementation Notes: Core Data Model & Application Submission

## Design Decisions

### Decision: Clean Architecture
- Layers: Domain, Application, Infrastructure, Web (ASP.NET MVC)
- Rationale: Business rule complexity warrants clear separation of concerns
- Domain layer owns validation logic (rich domain model)
- Rejected simpler layered approach: would not scale well with future specs (review, approval, payment)

### Decision: Rich Domain Model over Anemic Model
- Entities encapsulate their own business rules (e.g., `Application.Submit()` validates before state transition)
- Rejected anemic model + service layer: scatters business rules, harder to enforce invariants
- Rejected CQRS: overhead not justified at this stage

### Decision: ASP.NET MVC with .NET Aspire
- Server-side rendering via ASP.NET MVC (not SPA)
- .NET Aspire for orchestration (service discovery, health checks, telemetry)
- Keeps full stack in C#
- Rejected React/Angular/Vue: user preference for unified C# stack
- Rejected Blazor: user preference for traditional MVC

### Decision: SQL Server Database Project (dacpac) for Schema Management
- EF Core used for data access only (Code First model mapping), NOT for migrations
- Schema managed via SSDT/sqlproj with dacpac deployments
- Rationale: schema as first-class artifact with proper diffing, deploy tooling, and parity between dev and production
- Applies to both local development and production deployments

### Decision: Database-Configurable Impact Templates
- Impact types and their parameter definitions stored in database
- Administrators can create new impact types without code changes
- Each template defines named parameter slots with data type, required/optional, and validation constraints
- Rejected code-defined templates: limits flexibility, requires developer involvement for new types
- Rejected hybrid approach: unnecessary complexity

### Decision: System Configuration via Database
- Key-value settings stored in a SystemConfiguration table
- Changes take effect immediately (no restart required)
- Missing keys fall back to sensible defaults with logged warning

### Decision: Local File System for Document Storage
- Simple and appropriate for initial deployment
- Files stored outside web root for security
- Metadata tracked in database
- Interface designed so storage can be swapped to Azure Blob Storage later

### Decision: ASP.NET Identity for Authentication
- Self-managed users (not external identity provider)
- Basic role support (applicant, admin) via Identity roles
- Granular permissions deferred to review/approval spec

### Decision: Playwright for E2E Testing
- Non-negotiable requirement: every feature must have Playwright e2e tests
- Tests run against Aspire-orchestrated stack
- Cover golden path and key error scenarios for each user story

## Technology Stack Summary

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET MVC (.NET 8+) |
| Orchestration | .NET Aspire |
| Database | SQL Server |
| Schema Management | SQL Server Database Project (dacpac) |
| ORM | Entity Framework Core (Code First, no migrations) |
| Authentication | ASP.NET Identity |
| File Storage | Local file system |
| E2E Testing | Playwright for .NET |
