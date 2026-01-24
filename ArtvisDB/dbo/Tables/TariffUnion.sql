CREATE TABLE [dbo].[TariffUnion] (
    [tariffID]      INT NOT NULL,
    [tariffUnionID] INT NOT NULL,
    CONSTRAINT [PK_TariffUnion] PRIMARY KEY CLUSTERED ([tariffID] ASC, [tariffUnionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_TariffUnion_Tariff] FOREIGN KEY ([tariffID]) REFERENCES [dbo].[Tariff] ([tariffID]) ON DELETE CASCADE,
    CONSTRAINT [FK_TariffUnion_Tariff1] FOREIGN KEY ([tariffUnionID]) REFERENCES [dbo].[Tariff] ([tariffID])
);


GO
ALTER TABLE [dbo].[TariffUnion] SET (LOCK_ESCALATION = AUTO);

