# Quickstart: Core Data Model & Application Submission

**Date**: 2026-04-15

## Prerequisites

- .NET 8+ SDK
- Docker Desktop (for Aspire-managed SQL Server container)
- Node.js (for Playwright browser installation)
- SqlPackage CLI (`dotnet tool install -g microsoft.sqlpackage`)

## Solution Setup

```bash
# Create solution
dotnet new sln -n FundingPlatform

# Create projects
dotnet new classlib -n FundingPlatform.Domain -o src/FundingPlatform.Domain
dotnet new classlib -n FundingPlatform.Application -o src/FundingPlatform.Application
dotnet new classlib -n FundingPlatform.Infrastructure -o src/FundingPlatform.Infrastructure
dotnet new mvc -n FundingPlatform.Web -o src/FundingPlatform.Web
dotnet new aspire-apphost -n FundingPlatform.AppHost -o src/FundingPlatform.AppHost
dotnet new aspire-servicedefaults -n FundingPlatform.ServiceDefaults -o src/FundingPlatform.ServiceDefaults

# Create SQL Server Database Project
dotnet new sqlproj -n FundingPlatform.Database -o src/FundingPlatform.Database

# Create test projects
dotnet new nunit -n FundingPlatform.Tests.Unit -o tests/FundingPlatform.Tests.Unit
dotnet new nunit -n FundingPlatform.Tests.Integration -o tests/FundingPlatform.Tests.Integration
dotnet new nunit -n FundingPlatform.Tests.E2E -o tests/FundingPlatform.Tests.E2E

# Add all projects to solution
dotnet sln add src/FundingPlatform.Domain
dotnet sln add src/FundingPlatform.Application
dotnet sln add src/FundingPlatform.Infrastructure
dotnet sln add src/FundingPlatform.Web
dotnet sln add src/FundingPlatform.AppHost
dotnet sln add src/FundingPlatform.ServiceDefaults
dotnet sln add src/FundingPlatform.Database
dotnet sln add tests/FundingPlatform.Tests.Unit
dotnet sln add tests/FundingPlatform.Tests.Integration
dotnet sln add tests/FundingPlatform.Tests.E2E
```

## Project References (Dependency Rule)

```bash
# Application → Domain
dotnet add src/FundingPlatform.Application reference src/FundingPlatform.Domain

# Infrastructure → Application (and transitively Domain)
dotnet add src/FundingPlatform.Infrastructure reference src/FundingPlatform.Application

# Web → Application + Infrastructure + ServiceDefaults
dotnet add src/FundingPlatform.Web reference src/FundingPlatform.Application
dotnet add src/FundingPlatform.Web reference src/FundingPlatform.Infrastructure
dotnet add src/FundingPlatform.Web reference src/FundingPlatform.ServiceDefaults

# AppHost → Web (Aspire orchestration)
dotnet add src/FundingPlatform.AppHost reference src/FundingPlatform.Web

# Test projects
dotnet add tests/FundingPlatform.Tests.Unit reference src/FundingPlatform.Domain
dotnet add tests/FundingPlatform.Tests.Unit reference src/FundingPlatform.Application
dotnet add tests/FundingPlatform.Tests.Integration reference src/FundingPlatform.Infrastructure
dotnet add tests/FundingPlatform.Tests.E2E reference src/FundingPlatform.AppHost
```

## Key NuGet Packages

```bash
# Infrastructure - EF Core
dotnet add src/FundingPlatform.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/FundingPlatform.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/FundingPlatform.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# Web - Aspire integration
dotnet add src/FundingPlatform.Web package Aspire.Microsoft.EntityFrameworkCore.SqlServer

# AppHost - Aspire hosting
dotnet add src/FundingPlatform.AppHost package Aspire.Hosting.SqlServer

# E2E Tests
dotnet add tests/FundingPlatform.Tests.E2E package Microsoft.Playwright.NUnit
dotnet add tests/FundingPlatform.Tests.E2E package Aspire.Hosting.Testing
```

## Running the Application

```bash
# Start the Aspire-orchestrated stack (web app + SQL Server)
dotnet run --project src/FundingPlatform.AppHost

# Deploy database schema (after SQL Server container is running)
dotnet build src/FundingPlatform.Database
sqlpackage /Action:Publish \
  /SourceFile:src/FundingPlatform.Database/bin/Debug/FundingPlatform.Database.dacpac \
  /TargetConnectionString:"Server=localhost,PORT;Database=fundingdb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true"
```

## Running Tests

```bash
# Unit tests
dotnet test tests/FundingPlatform.Tests.Unit

# Integration tests
dotnet test tests/FundingPlatform.Tests.Integration

# E2E tests (install browsers first)
pwsh tests/FundingPlatform.Tests.E2E/bin/Debug/net8.0/playwright.ps1 install
dotnet test tests/FundingPlatform.Tests.E2E
```

## AppHost Program.cs (Aspire Orchestration)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
                       .AddDatabase("fundingdb");

builder.AddProject<Projects.FundingPlatform_Web>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.Build().Run();
```

## Web Program.cs (DI Wiring)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<AppDbContext>("fundingdb");

builder.Services.AddApplication();      // from Application layer
builder.Services.AddInfrastructure();    // from Infrastructure layer

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

## Key Implementation Order

1. Solution structure + project references + NuGet packages
2. Database project: table definitions + seed data
3. Domain entities with validation logic
4. Infrastructure: DbContext + configurations + repositories
5. Application: services + commands + queries
6. Web: controllers + views + authentication
7. Aspire AppHost wiring
8. E2E tests with Playwright
