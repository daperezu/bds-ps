# Data Model: Core Data Model & Application Submission

**Date**: 2026-04-15
**Branch**: `001-core-model-submission`

## Entity Relationship Overview

```
Applicant (1) ──── (*) Application (1) ──── (*) Item (1) ──── (*) Quotation (*) ──── (1) Supplier
                                                  │                    │
                                                  │                    └──── (1) Document
                                                  │
                                                  ├──── (1) Impact ──── (1) ImpactTemplate
                                                  │                          │
                                                  │                          └──── (*) ImpactTemplateParameter
                                                  │
                                                  └──── (1) Category

Application (1) ──── (*) VersionHistory

ImpactTemplate (1) ──── (*) ImpactTemplateParameter
Impact (1) ──── (*) ImpactParameterValue
```

## Entities

### Applicant

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| UserId | string | FK → AspNetUsers.Id, unique, required | ASP.NET Identity link |
| LegalId | string | required, unique | Cédula or legal identifier |
| FirstName | string | required, max 100 | |
| LastName | string | required, max 100 | |
| Email | string | required, max 256 | |
| Phone | string | max 20 | |
| PerformanceScore | decimal(5,2) | nullable | Manually managed for now |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |

### Application

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ApplicantId | int | FK → Applicants.Id, required | |
| State | int | required, default 0 | 0=Draft, 1=Submitted |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |
| SubmittedAt | datetime2 | nullable | Set on successful submission |
| RowVersion | rowversion | concurrency token | Optimistic concurrency |

**State transitions**:
- Draft → Submitted (via `Submit()` method with validation)
- No other transitions in this spec

**Domain methods**:
- `AddItem(item)`: Adds an item to the application
- `RemoveItem(itemId)`: Removes item and cascades to quotations/documents
- `Submit(minQuotations)`: Validates all rules, transitions to Submitted
- `Validate(minQuotations)`: Returns list of validation errors

### Item (Line)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ApplicationId | int | FK → Applications.Id, required | |
| ProductName | nvarchar(500) | required | Free text |
| CategoryId | int | FK → Categories.Id, required | |
| TechnicalSpecifications | nvarchar(max) | required | Structured text |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |

**Domain methods**:
- `AddQuotation(supplier, document, price, validity)`: Adds a supplier quotation, prevents duplicate suppliers
- `RemoveQuotation(quotationId)`: Removes quotation and associated document
- `SetImpact(impactTemplate, parameterValues)`: Sets the impact definition
- `HasMinimumQuotations(min)`: Returns bool
- `HasCompleteImpact()`: Returns bool

### Category

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| Name | nvarchar(200) | required, unique | |
| Description | nvarchar(500) | nullable | |
| IsActive | bit | required, default 1 | Soft delete |

### ImpactTemplate

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| Name | nvarchar(300) | required, unique | e.g., "Increase Production Capacity" |
| Description | nvarchar(1000) | nullable | |
| IsActive | bit | required, default 1 | |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |

### ImpactTemplateParameter

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ImpactTemplateId | int | FK → ImpactTemplates.Id, required | |
| Name | nvarchar(200) | required | e.g., "Percentage", "Timeframe" |
| DisplayLabel | nvarchar(300) | required | Shown to user |
| DataType | int | required | 0=Text, 1=Decimal, 2=Integer, 3=Date |
| IsRequired | bit | required, default 1 | |
| ValidationRules | nvarchar(max) | nullable | JSON: min, max, pattern, etc. |
| SortOrder | int | required, default 0 | Display order |

### Impact

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ItemId | int | FK → Items.Id, required, unique | One impact per item |
| ImpactTemplateId | int | FK → ImpactTemplates.Id, required | Template used |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |

### ImpactParameterValue

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ImpactId | int | FK → Impacts.Id, required | |
| ImpactTemplateParameterId | int | FK → ImpactTemplateParameters.Id, required | |
| Value | nvarchar(max) | nullable | Stored as string, parsed by DataType |

**Unique constraint**: (ImpactId, ImpactTemplateParameterId)

### Supplier

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| LegalId | nvarchar(50) | required, unique | Tax/legal identifier |
| Name | nvarchar(300) | required | |
| ContactName | nvarchar(200) | nullable | |
| Email | nvarchar(256) | nullable | |
| Phone | nvarchar(20) | nullable | |
| Location | nvarchar(500) | nullable | |
| HasElectronicInvoice | bit | required, default 0 | |
| ShippingDetails | nvarchar(500) | nullable | |
| WarrantyInfo | nvarchar(500) | nullable | |
| ComplianceStatus | nvarchar(100) | nullable | |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |
| UpdatedAt | datetime2 | required | |

### Quotation

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ItemId | int | FK → Items.Id, required | |
| SupplierId | int | FK → Suppliers.Id, required | |
| Price | decimal(18,2) | required | |
| ValidUntil | date | required | Quotation validity |
| DocumentId | int | FK → Documents.Id, required | Uploaded quotation file |
| CreatedAt | datetime2 | required, default GETUTCDATE() | |

**Unique constraint**: (ItemId, SupplierId) — prevents duplicate supplier per item

### Document

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| OriginalFileName | nvarchar(500) | required | |
| StoragePath | nvarchar(1000) | required | Local file system path |
| FileSize | bigint | required | Bytes |
| ContentType | nvarchar(100) | required | MIME type |
| UploadedAt | datetime2 | required, default GETUTCDATE() | |

### SystemConfiguration

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| Key | nvarchar(200) | required, unique | e.g., "MinQuotationsPerItem" |
| Value | nvarchar(max) | required | |
| Description | nvarchar(500) | nullable | |
| UpdatedAt | datetime2 | required | |

**Default seed data**:
- `MinQuotationsPerItem` = `"2"`
- `AllowedFileTypes` = `".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"`
- `MaxFileSizeMB` = `"10"`

### VersionHistory

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, identity | |
| ApplicationId | int | FK → Applications.Id, required | |
| UserId | string | FK → AspNetUsers.Id, required | Who made the change |
| Action | nvarchar(100) | required | e.g., "Created", "ItemAdded", "Submitted" |
| Details | nvarchar(max) | nullable | JSON with change details |
| Timestamp | datetime2 | required, default GETUTCDATE() | |

## Indexes

| Table | Index | Columns | Type |
|-------|-------|---------|------|
| Applications | IX_Applications_ApplicantId | ApplicantId | Nonclustered |
| Applications | IX_Applications_State | State | Nonclustered |
| Items | IX_Items_ApplicationId | ApplicationId | Nonclustered |
| Items | IX_Items_CategoryId | CategoryId | Nonclustered |
| Quotations | UX_Quotations_ItemId_SupplierId | ItemId, SupplierId | Unique |
| Impacts | UX_Impacts_ItemId | ItemId | Unique |
| ImpactParameterValues | UX_ImpactParamValues_ImpactId_ParamId | ImpactId, ImpactTemplateParameterId | Unique |
| ImpactTemplateParameters | IX_ImpactTemplateParams_TemplateId | ImpactTemplateId | Nonclustered |
| VersionHistory | IX_VersionHistory_ApplicationId | ApplicationId | Nonclustered |
| Applicants | UX_Applicants_UserId | UserId | Unique |
| Applicants | UX_Applicants_LegalId | LegalId | Unique |
| Suppliers | UX_Suppliers_LegalId | LegalId | Unique |
| SystemConfigurations | UX_SystemConfigurations_Key | Key | Unique |

## Cascade Rules

| Parent | Child | On Delete |
|--------|-------|-----------|
| Applicant | Application | Restrict |
| Application | Item | Cascade |
| Application | VersionHistory | Cascade |
| Item | Quotation | Cascade |
| Item | Impact | Cascade |
| Impact | ImpactParameterValue | Cascade |
| ImpactTemplate | ImpactTemplateParameter | Cascade |
| ImpactTemplate | Impact | Restrict |
| Quotation | Document | Restrict (manual cleanup) |
| Category | Item | Restrict |
| Supplier | Quotation | Restrict |
