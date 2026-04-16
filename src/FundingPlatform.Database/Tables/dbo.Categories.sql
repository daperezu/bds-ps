CREATE TABLE [dbo].[Categories]
(
    [Id]          INT            IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(200)  NOT NULL,
    [Description] NVARCHAR(500)  NULL,
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT (1),

    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_Categories_Name] UNIQUE ([Name])
);
