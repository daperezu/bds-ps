CREATE TABLE [dbo].[Appeals]
(
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ApplicationId]        INT            NOT NULL,
    [ApplicantResponseId]  INT            NOT NULL,
    [OpenedAt]             DATETIME2 (7)  NOT NULL,
    [OpenedByUserId]       NVARCHAR (450) NOT NULL,
    [Status]               INT            NOT NULL CONSTRAINT [DF_Appeals_Status] DEFAULT (0),
    [Resolution]           INT            NULL,
    [ResolvedAt]           DATETIME2 (7)  NULL,
    [ResolvedByUserId]     NVARCHAR (450) NULL,
    [RowVersion]           ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Appeals] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Appeals_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appeals_ApplicantResponses]
        FOREIGN KEY ([ApplicantResponseId]) REFERENCES [dbo].[ApplicantResponses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appeals_OpenedByUser]
        FOREIGN KEY ([OpenedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appeals_ResolvedByUser]
        FOREIGN KEY ([ResolvedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Appeals_ResolutionConsistency] CHECK (
        ([Status] = 0 AND [Resolution] IS NULL AND [ResolvedAt] IS NULL AND [ResolvedByUserId] IS NULL)
        OR
        ([Status] = 1 AND [Resolution] IS NOT NULL AND [ResolvedAt] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL)
    )
);
GO

CREATE NONCLUSTERED INDEX [IX_Appeals_ApplicationId]
    ON [dbo].[Appeals] ([ApplicationId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Appeals_OneOpenPerApplication]
    ON [dbo].[Appeals] ([ApplicationId]) WHERE [Status] = 0;
GO
