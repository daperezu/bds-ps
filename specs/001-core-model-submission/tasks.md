# Tasks: Core Data Model & Application Submission

**Input**: Design documents from `/specs/001-core-model-submission/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/mvc-routes.md, research.md, quickstart.md

**Tests**: Playwright e2e tests are REQUIRED for every user story (per FR-024 and SC-007).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the solution structure, project references, and NuGet packages

- [ ] T001 Create solution file and all 9 project directories per plan.md (`dotnet new sln`, project creation commands from quickstart.md)
- [ ] T002 Add project references following the dependency rule per quickstart.md (Domain ← Application ← Infrastructure, Web → Application + Infrastructure + ServiceDefaults, AppHost → Web)
- [ ] T003 [P] Add NuGet packages to Infrastructure project: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- [ ] T004 [P] Add NuGet packages to Web project: `Aspire.Microsoft.EntityFrameworkCore.SqlServer`
- [ ] T005 [P] Add NuGet packages to AppHost project: `Aspire.Hosting.SqlServer`
- [ ] T006 [P] Add NuGet packages to E2E test project: `Microsoft.Playwright.NUnit`, `Aspire.Hosting.Testing`
- [ ] T007 [P] Add NuGet packages to Unit and Integration test projects: `NUnit`, `NSubstitute` (or `Moq`), project references to Domain/Application/Infrastructure

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database Project

- [ ] T008 Create SQL Server Database Project using Microsoft.Build.Sql SDK in `src/FundingPlatform.Database/FundingPlatform.Database.sqlproj`
- [ ] T009 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Categories.sql` per data-model.md
- [ ] T010 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Applicants.sql` per data-model.md
- [ ] T011 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Applications.sql` per data-model.md (includes RowVersion column)
- [ ] T012 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Items.sql` per data-model.md
- [ ] T013 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.ImpactTemplates.sql` per data-model.md
- [ ] T014 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.ImpactTemplateParameters.sql` per data-model.md
- [ ] T015 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Impacts.sql` per data-model.md
- [ ] T016 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.ImpactParameterValues.sql` per data-model.md
- [ ] T017 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Suppliers.sql` per data-model.md
- [ ] T018 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Quotations.sql` per data-model.md (unique constraint on ItemId + SupplierId)
- [ ] T019 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.Documents.sql` per data-model.md
- [ ] T020 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.SystemConfigurations.sql` per data-model.md
- [ ] T021 [P] Create table definition `src/FundingPlatform.Database/Tables/dbo.VersionHistory.sql` per data-model.md
- [ ] T022 Create post-deployment seed data script `src/FundingPlatform.Database/PostDeployment/SeedData.sql` (default categories, system config: MinQuotationsPerItem=2, AllowedFileTypes, MaxFileSizeMB=10, sample impact templates)

### Domain Layer

- [ ] T023 [P] Create enums `src/FundingPlatform.Domain/Enums/ApplicationState.cs` (Draft=0, Submitted=1) and `ParameterDataType.cs` (Text=0, Decimal=1, Integer=2, Date=3)
- [ ] T024 [P] Create entity `src/FundingPlatform.Domain/Entities/Category.cs` with properties per data-model.md
- [ ] T025 [P] Create entity `src/FundingPlatform.Domain/Entities/Document.cs` with properties per data-model.md
- [ ] T026 [P] Create entity `src/FundingPlatform.Domain/Entities/SystemConfiguration.cs` with properties per data-model.md
- [ ] T027 [P] Create entity `src/FundingPlatform.Domain/Entities/VersionHistory.cs` with properties per data-model.md
- [ ] T028 [P] Create entity `src/FundingPlatform.Domain/Entities/ImpactTemplateParameter.cs` with properties per data-model.md
- [ ] T029 [P] Create entity `src/FundingPlatform.Domain/Entities/ImpactTemplate.cs` with properties and collection of parameters per data-model.md
- [ ] T030 [P] Create entity `src/FundingPlatform.Domain/Entities/ImpactParameterValue.cs` with properties per data-model.md
- [ ] T031 [P] Create entity `src/FundingPlatform.Domain/Entities/Impact.cs` with properties, reference to ImpactTemplate, collection of parameter values per data-model.md
- [ ] T032 [P] Create entity `src/FundingPlatform.Domain/Entities/Supplier.cs` with properties per data-model.md
- [ ] T033 [P] Create entity `src/FundingPlatform.Domain/Entities/Quotation.cs` with properties, references to Supplier and Document per data-model.md
- [ ] T034 Create entity `src/FundingPlatform.Domain/Entities/Item.cs` with properties, collections (Quotations, Impact), and domain methods: `AddQuotation()`, `RemoveQuotation()`, `SetImpact()`, `HasMinimumQuotations()`, `HasCompleteImpact()` per data-model.md
- [ ] T035 [P] Create entity `src/FundingPlatform.Domain/Entities/Applicant.cs` with properties per data-model.md
- [ ] T036 Create entity `src/FundingPlatform.Domain/Entities/Application.cs` with properties, collection of Items, RowVersion, and domain methods: `AddItem()`, `RemoveItem()`, `Submit()`, `Validate()` per data-model.md
- [ ] T037 [P] Create repository interfaces in `src/FundingPlatform.Domain/Interfaces/`: `IApplicationRepository.cs`, `ICategoryRepository.cs`, `IImpactTemplateRepository.cs`, `ISupplierRepository.cs`, `ISystemConfigurationRepository.cs`
- [ ] T038 [P] Create `src/FundingPlatform.Domain/Interfaces/IFileStorageService.cs` with methods: `SaveFileAsync()`, `DeleteFileAsync()`, `GetFileAsync()`

### Infrastructure Layer

- [ ] T039 Create `src/FundingPlatform.Infrastructure/Persistence/AppDbContext.cs` inheriting `IdentityDbContext`, registering all entity DbSets
- [ ] T040 [P] Create EF Core configurations in `src/FundingPlatform.Infrastructure/Persistence/Configurations/`: `ApplicantConfiguration.cs`, `ApplicationConfiguration.cs` (with RowVersion as concurrency token), `ItemConfiguration.cs`, `CategoryConfiguration.cs`
- [ ] T041 [P] Create EF Core configurations: `ImpactConfiguration.cs`, `ImpactTemplateConfiguration.cs`, `ImpactTemplateParameterConfiguration.cs`, `ImpactParameterValueConfiguration.cs` in `src/FundingPlatform.Infrastructure/Persistence/Configurations/`
- [ ] T042 [P] Create EF Core configurations: `SupplierConfiguration.cs`, `QuotationConfiguration.cs` (unique index on ItemId+SupplierId), `DocumentConfiguration.cs`, `SystemConfigurationConfiguration.cs`, `VersionHistoryConfiguration.cs` in `src/FundingPlatform.Infrastructure/Persistence/Configurations/`
- [ ] T043 [P] Create repository implementations in `src/FundingPlatform.Infrastructure/Persistence/Repositories/`: `ApplicationRepository.cs`, `CategoryRepository.cs`, `ImpactTemplateRepository.cs`, `SupplierRepository.cs`, `SystemConfigurationRepository.cs`
- [ ] T044 [P] Create `src/FundingPlatform.Infrastructure/FileStorage/LocalFileStorageService.cs` implementing `IFileStorageService` — saves files to configured local path, deletes old files on replace
- [ ] T045 Create `src/FundingPlatform.Infrastructure/DependencyInjection.cs` with `AddInfrastructure()` extension method registering repositories, file storage, Identity

### Application Layer

- [ ] T046 Create `src/FundingPlatform.Application/DependencyInjection.cs` with `AddApplication()` extension method registering application services
- [ ] T047 Create DTOs in `src/FundingPlatform.Application/DTOs/` for all view models used by controllers (ApplicationDto, ItemDto, SupplierDto, QuotationDto, ImpactDto, ImpactTemplateDto, CategoryDto, SystemConfigurationDto)

### Aspire Orchestration

- [ ] T048 Configure `src/FundingPlatform.AppHost/Program.cs` — add SQL Server resource with database, add Web project with reference and external HTTP endpoints per quickstart.md
- [ ] T049 Configure `src/FundingPlatform.ServiceDefaults/Extensions.cs` — `AddServiceDefaults()` with OpenTelemetry, health checks, resilience; `MapDefaultEndpoints()` for health/alive routes

### Web Project Bootstrap

- [ ] T050 Configure `src/FundingPlatform.Web/Program.cs` — AddServiceDefaults, AddSqlServerDbContext, AddApplication, AddInfrastructure, Identity, authentication, authorization, MapDefaultEndpoints, default MVC route per quickstart.md
- [ ] T051 Create `src/FundingPlatform.Web/Views/Shared/_Layout.cshtml` with navigation (Home, Applications, Admin), authentication status, Bootstrap styling
- [ ] T052 Create `src/FundingPlatform.Web/Controllers/HomeController.cs` and `src/FundingPlatform.Web/Views/Home/Index.cshtml` — landing page with login/register links

### E2E Test Infrastructure

- [ ] T053 Create `tests/FundingPlatform.Tests.E2E/Fixtures/AspireFixture.cs` — starts AppHost via `DistributedApplicationTestingBuilder`, exposes base URL, handles teardown
- [ ] T054 Create `tests/FundingPlatform.Tests.E2E/Fixtures/AuthenticatedTestBase.cs` — inherits `PageTest`, uses AspireFixture, handles login/storage state reuse

**Checkpoint**: Foundation ready — all entities, database schema, DbContext, Identity, Aspire, and test infrastructure in place. User story implementation can now begin.

---

## Phase 3: User Story 8 - Registration & Authentication (Priority: P2, but foundational)

**Goal**: Users can register, log in, and access protected pages. Authentication gates all other functionality.

**Independent Test**: Register a user, log in, access a protected page, log out, verify redirect to login.

### Implementation for User Story 8

- [ ] T055 [US8] Create `src/FundingPlatform.Web/Controllers/AccountController.cs` with Register (GET/POST), Login (GET/POST), Logout (POST) actions per contracts/mvc-routes.md
- [ ] T056 [P] [US8] Create `src/FundingPlatform.Web/Views/Account/Register.cshtml` — registration form (email, password, first name, last name, legal ID)
- [ ] T057 [P] [US8] Create `src/FundingPlatform.Web/Views/Account/Login.cshtml` — login form (email, password)
- [ ] T058 [US8] Create `src/FundingPlatform.Web/ViewModels/RegisterViewModel.cs` and `LoginViewModel.cs` with validation attributes
- [ ] T059 [US8] Wire Identity role seeding (Applicant, Admin roles) in `src/FundingPlatform.Infrastructure/Identity/IdentityConfiguration.cs` and call from Program.cs
- [ ] T060 [US8] Create Applicant profile on registration — link Identity user to Applicant entity in AccountController.Register

### E2E Tests for User Story 8

- [ ] T061 [P] [US8] Create `tests/FundingPlatform.Tests.E2E/PageObjects/LoginPage.cs` and `RegisterPage.cs` — page object model classes
- [ ] T062 [US8] Create `tests/FundingPlatform.Tests.E2E/Tests/AuthenticationTests.cs` — test registration, login, logout, protected page redirect per US8 acceptance scenarios

**Checkpoint**: Users can register and log in. All subsequent stories require authentication.

---

## Phase 4: User Story 3 - Manage Items Within an Application (Priority: P1)

**Goal**: Applicants can create an application and add, edit, and remove line items with categories and technical specs.

**Independent Test**: Create application, add multiple items with different categories, edit one, remove another, verify state.

### Implementation for User Story 3

- [ ] T063 [US3] Create `src/FundingPlatform.Application/Applications/Commands/CreateApplicationCommand.cs` — creates draft application for authenticated applicant
- [ ] T064 [US3] Create `src/FundingPlatform.Application/Applications/Commands/AddItemCommand.cs` — adds item to draft application with product name, category, tech specs
- [ ] T065 [P] [US3] Create `src/FundingPlatform.Application/Applications/Commands/UpdateItemCommand.cs` — updates item product name, category, tech specs
- [ ] T066 [P] [US3] Create `src/FundingPlatform.Application/Applications/Commands/RemoveItemCommand.cs` — removes item and cascades to quotations/documents
- [ ] T067 [US3] Create `src/FundingPlatform.Application/Applications/Queries/GetApplicationQuery.cs` and `GetApplicationsForApplicantQuery.cs` — retrieve application(s) with items
- [ ] T068 [US3] Create `src/FundingPlatform.Application/Services/ApplicationService.cs` — orchestrates commands and queries for application/item management
- [ ] T069 [US3] Create `src/FundingPlatform.Web/Controllers/ApplicationController.cs` with Index, Create (GET/POST), Details, Edit actions per contracts/mvc-routes.md
- [ ] T070 [US3] Create `src/FundingPlatform.Web/Controllers/ItemController.cs` with Add (GET/POST), Edit (GET/POST), Delete (POST) actions per contracts/mvc-routes.md
- [ ] T071 [P] [US3] Create view models `src/FundingPlatform.Web/ViewModels/ApplicationViewModel.cs`, `CreateApplicationViewModel.cs`, `ApplicationListViewModel.cs`
- [ ] T072 [P] [US3] Create view models `src/FundingPlatform.Web/ViewModels/AddItemViewModel.cs`, `EditItemViewModel.cs`
- [ ] T073 [US3] Create views `src/FundingPlatform.Web/Views/Application/Index.cshtml` (list), `Create.cshtml`, `Details.cshtml`, `Edit.cshtml`
- [ ] T074 [US3] Create views `src/FundingPlatform.Web/Views/Item/Add.cshtml`, `Edit.cshtml` — forms with category dropdown, product name, tech specs fields

### E2E Tests for User Story 3

- [ ] T075 [P] [US3] Create `tests/FundingPlatform.Tests.E2E/PageObjects/ApplicationPage.cs` and `ItemPage.cs` — page object model classes
- [ ] T076 [US3] Create `tests/FundingPlatform.Tests.E2E/Tests/ItemManagementTests.cs` — test add/edit/remove items, category selection per US3 acceptance scenarios

**Checkpoint**: Applicants can create applications and manage items within them.

---

## Phase 5: User Story 4 - Attach Supplier Quotations (Priority: P1)

**Goal**: Applicants can add suppliers to items and upload quotation documents with price and validity.

**Independent Test**: Add suppliers to an item, upload quotation files, verify duplicate prevention, verify file replacement.

### Implementation for User Story 4

- [ ] T077 [US4] Create `src/FundingPlatform.Application/Applications/Commands/AddSupplierQuotationCommand.cs` — adds supplier + quotation + uploaded document to an item, prevents duplicate supplier per item
- [ ] T078 [P] [US4] Create `src/FundingPlatform.Application/Applications/Commands/ReplaceQuotationDocumentCommand.cs` — replaces quotation document, deletes old file from disk
- [ ] T079 [US4] Extend `ApplicationService` to handle supplier/quotation commands including file upload via `IFileStorageService`
- [ ] T080 [US4] Create `src/FundingPlatform.Web/Controllers/SupplierController.cs` with Add (GET/POST) per contracts/mvc-routes.md
- [ ] T081 [US4] Create `src/FundingPlatform.Web/Controllers/QuotationController.cs` with Add (GET/POST), Replace (POST), Delete (POST) per contracts/mvc-routes.md
- [ ] T082 [P] [US4] Create view models `src/FundingPlatform.Web/ViewModels/AddSupplierViewModel.cs`, `AddQuotationViewModel.cs`
- [ ] T083 [US4] Create views `src/FundingPlatform.Web/Views/Supplier/Add.cshtml` — supplier form (legal ID, name, contact, location, invoice, shipping, warranty, compliance)
- [ ] T084 [US4] Create views `src/FundingPlatform.Web/Views/Quotation/Add.cshtml` — quotation form with file upload (multipart/form-data), price, validity date
- [ ] T085 [US4] Add file type and size validation to `QuotationController` — reads `AllowedFileTypes` and `MaxFileSizeMB` from `SystemConfiguration` via repository

### E2E Tests for User Story 4

- [ ] T086 [P] [US4] Create `tests/FundingPlatform.Tests.E2E/PageObjects/SupplierPage.cs` and `QuotationPage.cs` — page object model classes
- [ ] T087 [US4] Create `tests/FundingPlatform.Tests.E2E/Tests/SupplierQuotationTests.cs` — test add supplier, upload quotation, duplicate prevention, file replacement per US4 acceptance scenarios

**Checkpoint**: Applicants can add suppliers with quotation documents to items. File uploads work.

---

## Phase 6: User Story 5 - Impact Definition Using Dynamic Templates (Priority: P2)

**Goal**: Applicants select an impact template and fill in dynamic parameters for each item.

**Independent Test**: Select different impact templates, fill parameters, verify data saved, verify required parameter validation.

### Implementation for User Story 5

- [ ] T088 [US5] Create `src/FundingPlatform.Application/Applications/Queries/GetImpactTemplatesQuery.cs` — retrieves active impact templates with their parameter definitions
- [ ] T089 [US5] Create `src/FundingPlatform.Application/Applications/Commands/SetItemImpactCommand.cs` — saves impact template selection and parameter values for an item, validates required parameters
- [ ] T090 [US5] Extend `ApplicationService` to handle impact template queries and set-impact commands
- [ ] T091 [US5] Create `src/FundingPlatform.Web/ViewModels/ImpactViewModel.cs` — view model for dynamic impact form (template selection + parameter inputs)
- [ ] T092 [US5] Implement dynamic form rendering in `src/FundingPlatform.Web/Views/Item/Impact.cshtml` — render input fields based on selected template's parameter definitions (data type drives input type: text, number, date)
- [ ] T093 [US5] Add Impact GET/POST actions to `src/FundingPlatform.Web/Controllers/ItemController.cs` per contracts/mvc-routes.md — load templates, render form, save parameter values
- [ ] T094 [US5] Add AJAX endpoint or partial view for template parameter loading — when user selects a different template, update the parameter form fields dynamically

### E2E Tests for User Story 5

- [ ] T095 [US5] Create `tests/FundingPlatform.Tests.E2E/Tests/ImpactTemplateTests.cs` — test template selection, parameter filling, required validation, optional parameters per US5 acceptance scenarios

**Checkpoint**: Applicants can define structured impact for each item using dynamic templates.

---

## Phase 7: User Story 1 - Create and Submit Application (Priority: P1)

**Goal**: Full submission flow — validate all business rules (min quotations, complete impact, required fields) and transition from Draft to Submitted.

**Independent Test**: Create complete application with items/suppliers/quotations/impact, submit successfully. Then test submission failures with missing quotations and incomplete impact.

### Implementation for User Story 1

- [ ] T096 [US1] Create `src/FundingPlatform.Application/Applications/Commands/SubmitApplicationCommand.cs` — calls `Application.Submit()`, reads `MinQuotationsPerItem` from `SystemConfiguration`, collects all validation errors
- [ ] T097 [US1] Implement `Application.Validate(minQuotations)` domain method in `src/FundingPlatform.Domain/Entities/Application.cs` — validates: at least one item, each item has min quotations, each item has complete impact, all required fields populated
- [ ] T098 [US1] Add Submit (POST) action to `src/FundingPlatform.Web/Controllers/ApplicationController.cs` — calls SubmitApplicationCommand, redirects on success, re-renders with all errors on failure per FR-012
- [ ] T099 [US1] Create `src/FundingPlatform.Web/Views/Application/Details.cshtml` — show full application with all items, suppliers, quotations, impact; submit button for Draft applications; validation error summary
- [ ] T100 [US1] Add version history recording — log "Submitted" action to `VersionHistory` when application transitions to Submitted per FR-019
- [ ] T101 [US1] Add optimistic concurrency handling — catch `DbUpdateConcurrencyException` in ApplicationRepository, surface conflict warning to user per FR-020

### E2E Tests for User Story 1

- [ ] T102 [US1] Create `tests/FundingPlatform.Tests.E2E/Tests/ApplicationSubmissionTests.cs` — test successful submission, validation failures (missing quotations, incomplete impact, no items), error message display per US1 acceptance scenarios

**Checkpoint**: Full submission flow works with all business rule validation.

---

## Phase 8: User Story 2 - Save Draft and Return Later (Priority: P1)

**Goal**: Applicants can save progress on a draft application and return later to continue editing.

**Independent Test**: Create application, add partial data, log out, log back in, verify all data persisted.

### Implementation for User Story 2

- [ ] T103 [US2] Verify and enhance draft persistence across all controllers — ensure every Add/Edit action saves to database immediately (not just on submit) per FR-014
- [ ] T104 [US2] Add version history recording for draft changes — log "ItemAdded", "ItemUpdated", "ItemRemoved", "SupplierAdded", "QuotationUploaded" actions per FR-019
- [ ] T105 [US2] Ensure Application/Index view shows all draft and submitted applications with status indicators and last-modified dates

### E2E Tests for User Story 2

- [ ] T106 [US2] Create `tests/FundingPlatform.Tests.E2E/Tests/DraftPersistenceTests.cs` — test save partial data, logout, login, verify data intact, continue editing, submit per US2 acceptance scenarios

**Checkpoint**: Draft persistence verified end-to-end.

---

## Phase 9: User Story 7 - Admin Manages Impact Templates (Priority: P2)

**Goal**: Administrators can create, edit, and manage impact templates with parameter definitions.

**Independent Test**: Create new template with parameters, verify it appears for applicant selection, modify template and verify validation impact on existing drafts.

### Implementation for User Story 7

- [ ] T107 [US7] Create `src/FundingPlatform.Application/Admin/Commands/CreateImpactTemplateCommand.cs` — creates template with parameter definitions (name, display label, data type, required, validation rules, sort order)
- [ ] T108 [P] [US7] Create `src/FundingPlatform.Application/Admin/Commands/UpdateImpactTemplateCommand.cs` — updates template and parameters, handles adding/removing parameters
- [ ] T109 [P] [US7] Create `src/FundingPlatform.Application/Admin/Queries/GetImpactTemplatesQuery.cs` — retrieves all templates with parameters for admin listing
- [ ] T110 [US7] Create `src/FundingPlatform.Application/Services/AdminService.cs` — orchestrates admin commands and queries
- [ ] T111 [US7] Add ImpactTemplates, CreateTemplate (GET/POST), EditTemplate (GET/POST) actions to `src/FundingPlatform.Web/Controllers/AdminController.cs` per contracts/mvc-routes.md
- [ ] T112 [P] [US7] Create view models `src/FundingPlatform.Web/ViewModels/ImpactTemplateAdminViewModel.cs`, `CreateImpactTemplateViewModel.cs`, `EditImpactTemplateViewModel.cs`
- [ ] T113 [US7] Create views `src/FundingPlatform.Web/Views/Admin/ImpactTemplates.cshtml` (list), `CreateTemplate.cshtml`, `EditTemplate.cshtml` — forms with dynamic parameter definition rows

### E2E Tests for User Story 7

- [ ] T114 [P] [US7] Create `tests/FundingPlatform.Tests.E2E/PageObjects/AdminPage.cs` — page object for admin views
- [ ] T115 [US7] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminImpactTemplateTests.cs` — test create/edit templates, verify availability for applicants, verify validation impact on drafts per US7 acceptance scenarios

**Checkpoint**: Administrators can manage impact templates.

---

## Phase 10: User Story 6 - Admin Manages System Configuration (Priority: P2)

**Goal**: Administrators can view and modify system-wide settings (min quotations, file types, max file size).

**Independent Test**: Change min quotation setting, verify next submission validates against new value.

### Implementation for User Story 6

- [ ] T116 [US6] Create `src/FundingPlatform.Application/Admin/Commands/UpdateSystemConfigurationCommand.cs` — updates key-value settings
- [ ] T117 [P] [US6] Create `src/FundingPlatform.Application/Admin/Queries/GetSystemConfigurationQuery.cs` — retrieves all settings for admin display
- [ ] T118 [US6] Add Configuration (GET/POST) actions to `src/FundingPlatform.Web/Controllers/AdminController.cs` per contracts/mvc-routes.md
- [ ] T119 [US6] Create views `src/FundingPlatform.Web/Views/Admin/Configuration.cshtml` — editable settings list with descriptions
- [ ] T120 [US6] Create view model `src/FundingPlatform.Web/ViewModels/SystemConfigurationViewModel.cs`

### E2E Tests for User Story 6

- [ ] T121 [US6] Create `tests/FundingPlatform.Tests.E2E/Tests/AdminConfigurationTests.cs` — test change min quotations, verify submission validation respects new value per US6 acceptance scenarios

**Checkpoint**: Administrators can manage system configuration. All user stories complete.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T122 [P] Add `[Authorize]` attribute enforcement on all controllers — verify Applicant vs Admin role restrictions per contracts/mvc-routes.md
- [ ] T123 [P] Add anti-forgery token validation on all POST actions per contracts/mvc-routes.md notes
- [ ] T124 [P] Add application-owns-check on all applicant controllers — verify the application belongs to the authenticated user before allowing access
- [ ] T125 Add missing `SystemConfiguration` default handling — if key is missing, use sensible default and log warning per spec error handling section
- [ ] T126 [P] Style all views with consistent Bootstrap layout, form validation feedback, error summary display
- [ ] T127 Run quickstart.md validation — follow the quickstart from scratch, verify solution builds, Aspire starts, dacpac deploys, app runs, e2e tests pass
- [ ] T128 Verify all Playwright e2e tests pass against the running Aspire-orchestrated stack

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US8 Auth (Phase 3)**: Depends on Foundational — BLOCKS all subsequent stories
- **US3 Items (Phase 4)**: Depends on US8 — enables item management
- **US4 Quotations (Phase 5)**: Depends on US3 — adds suppliers/documents to items
- **US5 Impact (Phase 6)**: Depends on US3 — adds impact to items (parallel with US4)
- **US1 Submit (Phase 7)**: Depends on US3, US4, US5 — ties everything together with validation
- **US2 Draft (Phase 8)**: Depends on US3, US4, US5 — verifies persistence across flows
- **US7 Admin Templates (Phase 9)**: Depends on US5 — admin side of impact system
- **US6 Admin Config (Phase 10)**: Depends on US1 — verifies config affects submission
- **Polish (Phase 11)**: Depends on all user stories

### User Story Dependencies

```
Setup → Foundational → US8 (Auth)
                          ↓
                        US3 (Items)
                        ↙       ↘
                US4 (Quotations)  US5 (Impact)
                        ↘       ↙
                        US1 (Submit)
                            ↓
                        US2 (Draft)
                        
                US5 → US7 (Admin Templates)
                US1 → US6 (Admin Config)
```

### Parallel Opportunities

- **Phase 2**: All table definitions (T009-T021) can run in parallel. All domain entities (T023-T038) can run in parallel. All EF configurations (T040-T042) can run in parallel.
- **Phase 4-5**: US4 (Quotations) and US5 (Impact) can run in parallel after US3 completes — they modify different files and entities.
- **Phase 9-10**: US7 (Admin Templates) and US6 (Admin Config) can run in parallel — different controllers and views.

---

## Parallel Example: Foundational Phase

```bash
# Launch all database table definitions together (T009-T021):
Task: "Create dbo.Categories.sql"
Task: "Create dbo.Applicants.sql"
Task: "Create dbo.Applications.sql"
# ... all 13 tables in parallel

# Launch all domain entities together (T023-T038):
Task: "Create ApplicationState enum"
Task: "Create Category entity"
Task: "Create Document entity"
# ... all entities in parallel (except Item and Application which have domain methods depending on other entities)
```

---

## Implementation Strategy

### MVP First (US8 + US3 + US4 + US5 + US1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US8 Authentication
4. Complete Phase 4: US3 Item Management
5. Complete Phase 5-6: US4 Quotations + US5 Impact (parallel)
6. Complete Phase 7: US1 Application Submission
7. **STOP and VALIDATE**: Test full submission flow end-to-end
8. Deploy/demo if ready — this is the MVP

### Incremental Delivery

1. Setup + Foundational + Auth → Foundation ready
2. Add US3 (Items) → Test independently → First demo (create apps, manage items)
3. Add US4 (Quotations) + US5 (Impact) → Test independently → Second demo (full item composition)
4. Add US1 (Submit) → Test independently → **MVP Release** (full submission flow!)
5. Add US2 (Draft) → Verify persistence → Enhanced reliability
6. Add US7 + US6 (Admin) → Admin capabilities → **Full Release**
7. Polish → Production-ready

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- E2E tests are REQUIRED for every user story (FR-024)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Database schema managed via dacpac — build and deploy after T022
