/*
    Post-Deployment Script: SeedData.sql
    Idempotent seed data for the FundingPlatform database.
*/

-- =============================================================================
-- Identity Roles
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'APPLICANT')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Applicant', N'APPLICANT', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'ADMIN')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Admin', N'ADMIN', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'REVIEWER')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Reviewer', N'REVIEWER', NEWID());

-- =============================================================================
-- Categories
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Computing Equipment')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Computing Equipment', N'Computers, servers, networking equipment, and peripherals', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Laboratory Equipment')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Laboratory Equipment', N'Scientific instruments, lab apparatus, and research equipment', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Software')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Software', N'Software licenses, subscriptions, and development tools', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Office Equipment')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Office Equipment', N'Furniture, office supplies, and general office equipment', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Vehicles')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Vehicles', N'Transport vehicles and related equipment', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Construction')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive])
    VALUES (N'Construction', N'Building materials, construction equipment, and infrastructure', 1);

-- =============================================================================
-- System Configurations
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = N'MinQuotationsPerItem')
    INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
    VALUES (N'MinQuotationsPerItem', N'2', N'Minimum number of quotations required per item', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = N'AllowedFileTypes')
    INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
    VALUES (N'AllowedFileTypes', N'.pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx', N'Comma-separated list of allowed file extensions for uploads', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = N'MaxFileSizeMB')
    INSERT INTO [dbo].[SystemConfigurations] ([Key], [Value], [Description], [UpdatedAt])
    VALUES (N'MaxFileSizeMB', N'10', N'Maximum file size in megabytes for uploads', GETUTCDATE());

-- =============================================================================
-- Impact Templates
-- =============================================================================

-- Template: Increase Production Capacity
IF NOT EXISTS (SELECT 1 FROM [dbo].[ImpactTemplates] WHERE [Name] = N'Increase Production Capacity')
BEGIN
    INSERT INTO [dbo].[ImpactTemplates] ([Name], [Description], [IsActive], [UpdatedAt])
    VALUES (N'Increase Production Capacity', N'Measures the expected increase in production capacity resulting from the funded item', 1, GETUTCDATE());

    DECLARE @IncreaseCapacityId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ImpactTemplateParameters] ([ImpactTemplateId], [Name], [DisplayLabel], [DataType], [IsRequired], [SortOrder])
    VALUES
        (@IncreaseCapacityId, N'CurrentCapacity',    N'Current Capacity',     1, 1, 1),
        (@IncreaseCapacityId, N'ProjectedCapacity',   N'Projected Capacity',   1, 1, 2),
        (@IncreaseCapacityId, N'TimeframeInMonths',   N'Timeframe in Months',  2, 1, 3);
END;

-- Template: Job Creation
IF NOT EXISTS (SELECT 1 FROM [dbo].[ImpactTemplates] WHERE [Name] = N'Job Creation')
BEGIN
    INSERT INTO [dbo].[ImpactTemplates] ([Name], [Description], [IsActive], [UpdatedAt])
    VALUES (N'Job Creation', N'Measures the expected number of new jobs created as a result of the funded item', 1, GETUTCDATE());

    DECLARE @JobCreationId INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ImpactTemplateParameters] ([ImpactTemplateId], [Name], [DisplayLabel], [DataType], [IsRequired], [SortOrder])
    VALUES
        (@JobCreationId, N'CurrentEmployees',   N'Current Employees',    2, 1, 1),
        (@JobCreationId, N'ProjectedNewJobs',    N'Projected New Jobs',   2, 1, 2),
        (@JobCreationId, N'JobType',             N'Job Type',             0, 1, 3);
END;
GO
