CREATE TABLE [dbo].[Impacts]
(
    [Id]               INT       IDENTITY(1,1) NOT NULL,
    [ItemId]           INT       NOT NULL,
    [ImpactTemplateId] INT       NOT NULL,
    [CreatedAt]        DATETIME2 NOT NULL CONSTRAINT [DF_Impacts_CreatedAt] DEFAULT (GETUTCDATE()),
    [UpdatedAt]        DATETIME2 NOT NULL,

    CONSTRAINT [PK_Impacts] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_Impacts_ItemId] UNIQUE ([ItemId]),
    CONSTRAINT [FK_Impacts_Items] FOREIGN KEY ([ItemId]) REFERENCES [dbo].[Items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Impacts_ImpactTemplates] FOREIGN KEY ([ImpactTemplateId]) REFERENCES [dbo].[ImpactTemplates] ([Id]) ON DELETE NO ACTION
);
