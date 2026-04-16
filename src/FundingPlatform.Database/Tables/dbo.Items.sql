CREATE TABLE [dbo].[Items]
(
    [Id]                      INT            IDENTITY(1,1) NOT NULL,
    [ApplicationId]           INT            NOT NULL,
    [ProductName]             NVARCHAR(500)  NOT NULL,
    [CategoryId]              INT            NOT NULL,
    [TechnicalSpecifications] NVARCHAR(MAX)  NOT NULL,
    [CreatedAt]               DATETIME2      NOT NULL CONSTRAINT [DF_Items_CreatedAt] DEFAULT (GETUTCDATE()),
    [UpdatedAt]               DATETIME2      NOT NULL,

    CONSTRAINT [PK_Items] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Items_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Items_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX [IX_Items_ApplicationId]
    ON [dbo].[Items] ([ApplicationId]);
GO

CREATE NONCLUSTERED INDEX [IX_Items_CategoryId]
    ON [dbo].[Items] ([CategoryId]);
GO
