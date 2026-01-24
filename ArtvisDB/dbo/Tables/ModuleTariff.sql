CREATE TABLE [dbo].[ModuleTariff] (
    [modulePriceListID] SMALLINT NOT NULL,
    [tariffID]          INT      NOT NULL,
    CONSTRAINT [PK_ModuleTariff] PRIMARY KEY CLUSTERED ([modulePriceListID] ASC, [tariffID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ModuleTariff_ModulePriceList] FOREIGN KEY ([modulePriceListID]) REFERENCES [dbo].[ModulePriceList] ([modulePriceListID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModuleTariff_Tariff] FOREIGN KEY ([tariffID]) REFERENCES [dbo].[Tariff] ([tariffID])
);


GO
ALTER TABLE [dbo].[ModuleTariff] SET (LOCK_ESCALATION = AUTO);

