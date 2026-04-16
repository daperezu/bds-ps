CREATE TABLE [dbo].[ImpactTemplates]
(
    [Id]          INT            IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(300)  NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_ImpactTemplates_IsActive] DEFAULT (1),
    [CreatedAt]   DATETIME2      NOT NULL CONSTRAINT [DF_ImpactTemplates_CreatedAt] DEFAULT (GETUTCDATE()),
    [UpdatedAt]   DATETIME2      NOT NULL,

    CONSTRAINT [PK_ImpactTemplates] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_ImpactTemplates_Name] UNIQUE ([Name])
);
