CREATE TABLE [dbo].[ImpactParameterValues]
(
    [Id]                        INT           IDENTITY(1,1) NOT NULL,
    [ImpactId]                  INT           NOT NULL,
    [ImpactTemplateParameterId] INT           NOT NULL,
    [Value]                     NVARCHAR(MAX) NULL,

    CONSTRAINT [PK_ImpactParameterValues] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UX_ImpactParamValues_ImpactId_ParamId] UNIQUE ([ImpactId], [ImpactTemplateParameterId]),
    CONSTRAINT [FK_ImpactParamValues_Impacts] FOREIGN KEY ([ImpactId]) REFERENCES [dbo].[Impacts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ImpactParamValues_ImpactTemplateParams] FOREIGN KEY ([ImpactTemplateParameterId]) REFERENCES [dbo].[ImpactTemplateParameters] ([Id]) ON DELETE NO ACTION
);
