CREATE TABLE [dbo].[Suppliers]
(
    [Id]                    INT            IDENTITY(1,1) NOT NULL,
    [LegalId]               NVARCHAR(50)   NOT NULL,
    [Name]                  NVARCHAR(300)  NOT NULL,
    [ContactName]           NVARCHAR(200)  NULL,
    [Email]                 NVARCHAR(256)  NULL,
    [Phone]                 NVARCHAR(20)   NULL,
    [Location]              NVARCHAR(500)  NULL,
    [HasElectronicInvoice]  BIT            NOT NULL CONSTRAINT [DF_Suppliers_HasElectronicInvoice] DEFAULT (0),
    [ShippingDetails]       NVARCHAR(500)  NULL,
    [WarrantyInfo]          NVARCHAR(500)  NULL,
    [ComplianceStatus]      NVARCHAR(100)  NULL,
    [CreatedAt]             DATETIME2      NOT NULL CONSTRAINT [DF_Suppliers_CreatedAt] DEFAULT (GETUTCDATE()),
    [UpdatedAt]             DATETIME2      NOT NULL,

    CONSTRAINT [PK_Suppliers] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_Suppliers_LegalId] UNIQUE ([LegalId])
);
