CREATE TABLE [dbo].[ApplicantResponses]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [ApplicationId]     INT            NOT NULL,
    [CycleNumber]       INT            NOT NULL,
    [SubmittedAt]       DATETIME2 (7)  NOT NULL,
    [SubmittedByUserId] NVARCHAR (450) NOT NULL,

    CONSTRAINT [PK_ApplicantResponses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ApplicantResponses_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApplicantResponses_Users]
        FOREIGN KEY ([SubmittedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_ApplicantResponses_AppCycle] UNIQUE ([ApplicationId], [CycleNumber])
);
GO

CREATE NONCLUSTERED INDEX [IX_ApplicantResponses_ApplicationId]
    ON [dbo].[ApplicantResponses] ([ApplicationId]);
GO
