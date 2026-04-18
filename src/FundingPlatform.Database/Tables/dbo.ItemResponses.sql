CREATE TABLE [dbo].[ItemResponses]
(
    [Id]                  INT IDENTITY (1, 1) NOT NULL,
    [ApplicantResponseId] INT NOT NULL,
    [ItemId]              INT NOT NULL,
    [Decision]            INT NOT NULL,

    CONSTRAINT [PK_ItemResponses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ItemResponses_ApplicantResponses]
        FOREIGN KEY ([ApplicantResponseId]) REFERENCES [dbo].[ApplicantResponses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItemResponses_Items]
        FOREIGN KEY ([ItemId]) REFERENCES [dbo].[Items] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_ItemResponses_ResponseItem] UNIQUE ([ApplicantResponseId], [ItemId])
);
GO

CREATE NONCLUSTERED INDEX [IX_ItemResponses_ItemId]
    ON [dbo].[ItemResponses] ([ItemId]);
GO
