# Quickstart: Core Data Model & Application Submission

**Date**: 2026-04-15

## Prerequisites

- .NET 10+ SDK
- Docker Desktop — **must be installed AND running** before starting Aspire
- Node.js (for Playwright browser installation)
- SqlPackage CLI: `dotnet tool install -g microsoft.sqlpackage`

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

# Create SQL Server Database Project (install template first if needed)
dotnet new install Microsoft.Build.Sql.Templates
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

### Step 1: Start the Aspire stack

Ensure Docker Desktop is running, then:

```bash
dotnet run --project src/FundingPlatform.AppHost
```

This starts the Aspire orchestrator, which:
- Pulls and runs a SQL Server container (auto-generated password, ephemeral port)
- Starts the web application with injected connection strings

The console output will show the **Aspire Dashboard URL** (typically `https://localhost:17255`). Open it to see all running resources.

### Step 2: Find the SQL Server connection details

Aspire generates a random SA password and maps SQL Server to a random host port. To find them:

**Port** — run `docker ps` and find the container mapped to port `1433`:
```bash
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep 1433
# Example output: sqlserver-xxx  0.0.0.0:63212->1433/tcp
# The host port is 63212
```

**Password** — Aspire stores it in .NET user secrets:
```bash
dotnet user-secrets list --project src/FundingPlatform.AppHost
# Look for: Parameters:sqlserver-password = <the_password>
```

Alternatively, the Aspire Dashboard shows the full connection string on the `sqlserver` resource details page.

### Step 3: Build and deploy the database schema

The dacpac contains ALL tables — both custom domain tables and ASP.NET Identity tables. No EF migrations are used.

```bash
# Build the dacpac
dotnet build src/FundingPlatform.Database

# Deploy to the Aspire-managed SQL Server (replace PORT and PASSWORD with values from Step 2)
sqlpackage /Action:Publish \
  /SourceFile:src/FundingPlatform.Database/bin/Debug/FundingPlatform.Database.dacpac \
  /TargetConnectionString:"Server=localhost,PORT;Database=fundingdb;User Id=sa;Password=PASSWORD;TrustServerCertificate=true"
```

### Step 4: Access the web application

The web app URL is shown in the Aspire Dashboard under the `webapp` resource (also printed in the console). Open it in a browser to register and log in.

On first startup before the dacpac is deployed, the app logs a warning: `"Database schema not ready — deploy the dacpac and restart."` After deploying the dacpac, either restart the AppHost or simply access the app — Identity roles are seeded automatically on startup.

### Important: Container lifecycle

Aspire recreates the SQL Server container each time the AppHost restarts. This means:
- **The dacpac must be redeployed after each restart** (the database is ephemeral)
- All registered users and data are lost on restart
- This is by design for local development — production uses a persistent SQL Server instance

## Running Tests

```bash
# Unit tests
dotnet test tests/FundingPlatform.Tests.Unit

# Integration tests
dotnet test tests/FundingPlatform.Tests.Integration

# E2E tests (install Playwright browsers first)
dotnet build tests/FundingPlatform.Tests.E2E
pwsh tests/FundingPlatform.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install
dotnet test tests/FundingPlatform.Tests.E2E
```

## Database Schema Strategy

This project uses a **hybrid approach**:
- **SQL Database Project (dacpac)** is the single source of truth for ALL database schema — including ASP.NET Identity tables
- **EF Core** is used for data access only (no migrations, no `EnsureCreated`)
- Schema changes are made by editing `.sql` files in `src/FundingPlatform.Database/Tables/`
- The dacpac handles deployment, diffing, and schema upgrades

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

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => { ... })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Identity roles (gracefully handles missing schema)
using (var scope = app.Services.CreateScope())
{
    try { await IdentityConfiguration.SeedRolesAsync(scope.ServiceProvider); }
    catch (SqlException) { /* dacpac not deployed yet — deploy and restart */ }
}

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

## Key Implementation Order

1. Solution structure + project references + NuGet packages
2. Database project: table definitions (domain + Identity) + seed data
3. Domain entities with validation logic
4. Infrastructure: DbContext + configurations + repositories
5. Application: services + commands + queries
6. Web: controllers + views + authentication
7. Aspire AppHost wiring
8. E2E tests with Playwright
