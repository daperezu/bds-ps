CREATE TABLE [dbo].[ImpactTemplateParameters]
(
    [Id]               INT            IDENTITY(1,1) NOT NULL,
    [ImpactTemplateId] INT            NOT NULL,
    [Name]             NVARCHAR(200)  NOT NULL,
    [DisplayLabel]     NVARCHAR(300)  NOT NULL,
    [DataType]         INT            NOT NULL,
    [IsRequired]       BIT            NOT NULL CONSTRAINT [DF_ImpactTemplateParams_IsRequired] DEFAULT (1),
    [ValidationRules]  NVARCHAR(MAX)  NULL,
    [SortOrder]        INT            NOT NULL CONSTRAINT [DF_ImpactTemplateParams_SortOrder] DEFAULT (0),

    CONSTRAINT [PK_ImpactTemplateParameters] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ImpactTemplateParams_ImpactTemplates] FOREIGN KEY ([ImpactTemplateId]) REFERENCES [dbo].[ImpactTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ImpactTemplateParams_TemplateId]
    ON [dbo].[ImpactTemplateParameters] ([ImpactTemplateId]);
GO
