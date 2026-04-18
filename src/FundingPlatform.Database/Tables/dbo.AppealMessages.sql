CREATE TABLE [dbo].[AppealMessages]
(
    [Id]           INT             IDENTITY (1, 1) NOT NULL,
    [AppealId]     INT             NOT NULL,
    [AuthorUserId] NVARCHAR (450)  NOT NULL,
    [Text]         NVARCHAR (4000) NOT NULL,
    [CreatedAt]    DATETIME2 (7)   NOT NULL,

    CONSTRAINT [PK_AppealMessages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_AppealMessages_Appeals]
        FOREIGN KEY ([AppealId]) REFERENCES [dbo].[Appeals] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppealMessages_Users]
        FOREIGN KEY ([AuthorUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_AppealMessages_TextNotEmpty] CHECK (LEN([Text]) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_AppealMessages_AppealId_CreatedAt]
    ON [dbo].[AppealMessages] ([AppealId], [CreatedAt]);
GO
