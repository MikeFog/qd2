CREATE TABLE [dbo].[iStudioTariffType] (
    [tariffTypeID] SMALLINT      NOT NULL,
    [name]         NVARCHAR (32) NOT NULL,
    CONSTRAINT [PK_StudioTariffType] PRIMARY KEY CLUSTERED ([tariffTypeID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_StudioTariffType_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iStudioTariffType] SET (LOCK_ESCALATION = AUTO);

