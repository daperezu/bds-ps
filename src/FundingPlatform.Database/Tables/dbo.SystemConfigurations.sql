CREATE TABLE [dbo].[SystemConfigurations]
(
    [Id]          INT            IDENTITY(1,1) NOT NULL,
    [Key]         NVARCHAR(200)  NOT NULL,
    [Value]       NVARCHAR(MAX)  NOT NULL,
    [Description] NVARCHAR(500)  NULL,
    [UpdatedAt]   DATETIME2      NOT NULL,

    CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_SystemConfigurations_Key] UNIQUE ([Key])
);
