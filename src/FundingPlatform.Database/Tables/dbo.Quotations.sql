CREATE TABLE [dbo].[Quotations]
(
    [Id]         INT           IDENTITY(1,1) NOT NULL,
    [ItemId]     INT           NOT NULL,
    [SupplierId] INT           NOT NULL,
    [Price]      DECIMAL(18,2) NOT NULL,
    [ValidUntil] DATE          NOT NULL,
    [DocumentId] INT           NOT NULL,
    [Currency]   NVARCHAR(3)   NULL,
    [CreatedAt]  DATETIME2     NOT NULL CONSTRAINT [DF_Quotations_CreatedAt] DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_Quotations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_Quotations_ItemId_SupplierId] UNIQUE ([ItemId], [SupplierId]),
    CONSTRAINT [FK_Quotations_Items] FOREIGN KEY ([ItemId]) REFERENCES [dbo].[Items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Quotations_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quotations_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents] ([Id]) ON DELETE NO ACTION
);
