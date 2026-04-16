CREATE TABLE [dbo].[VersionHistory]
(
    [Id]            INT            IDENTITY(1,1) NOT NULL,
    [ApplicationId] INT            NOT NULL,
    [UserId]        NVARCHAR(450)  NOT NULL,
    [Action]        NVARCHAR(100)  NOT NULL,
    [Details]       NVARCHAR(MAX)  NULL,
    [Timestamp]     DATETIME2      NOT NULL CONSTRAINT [DF_VersionHistory_Timestamp] DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_VersionHistory] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_VersionHistory_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_VersionHistory_ApplicationId]
    ON [dbo].[VersionHistory] ([ApplicationId]);
GO
