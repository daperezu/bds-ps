CREATE TABLE [dbo].[SigningReviewDecisions]
(
    [Id]               INT            IDENTITY(1,1) NOT NULL,
    [SignedUploadId]   INT            NOT NULL,
    [Outcome]          INT            NOT NULL,
    [ReviewerUserId]   NVARCHAR(450)  NOT NULL,
    [Comment]          NVARCHAR(2000) NULL,
    [DecidedAtUtc]     DATETIME2(3)   NOT NULL,

    CONSTRAINT [PK_SigningReviewDecisions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SigningReviewDecisions_SignedUploads]
        FOREIGN KEY ([SignedUploadId]) REFERENCES [dbo].[SignedUploads]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SigningReviewDecisions_AspNetUsers]
        FOREIGN KEY ([ReviewerUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_SigningReviewDecisions_SignedUploadId] UNIQUE ([SignedUploadId]),
    CONSTRAINT [CK_SigningReviewDecisions_Outcome_Range] CHECK ([Outcome] BETWEEN 0 AND 1),
    CONSTRAINT [CK_SigningReviewDecisions_RejectComment]
        CHECK ([Outcome] <> 1 OR ([Comment] IS NOT NULL AND LTRIM(RTRIM([Comment])) <> N''))
);
GO

CREATE NONCLUSTERED INDEX [IX_SigningReviewDecisions_ReviewerUserId]
    ON [dbo].[SigningReviewDecisions] ([ReviewerUserId]);
GO
