CREATE TABLE [dbo].[StudioTariff] (
    [tariffID]     INT      IDENTITY (1, 1) NOT NULL,
    [pricelistID]  SMALLINT NOT NULL,
    [rolStyleID]   SMALLINT NOT NULL,
    [summa]        MONEY    NOT NULL,
    [tariffTypeID] SMALLINT CONSTRAINT [DF_StudioTariff_tariffTypeID] DEFAULT (2) NOT NULL,
    CONSTRAINT [PK_StudioTariff] PRIMARY KEY CLUSTERED ([tariffID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioTariff_RolStyle] FOREIGN KEY ([rolStyleID]) REFERENCES [dbo].[RolStyle] ([rolStyleID]),
    CONSTRAINT [FK_StudioTariff_StudioPricelist] FOREIGN KEY ([pricelistID]) REFERENCES [dbo].[StudioPricelist] ([pricelistID]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudioTariff_StudioTariffType] FOREIGN KEY ([tariffTypeID]) REFERENCES [dbo].[iStudioTariffType] ([tariffTypeID]),
    CONSTRAINT [UIX_StudioTariff_Pricelist_RolStyle] UNIQUE NONCLUSTERED ([pricelistID] ASC, [rolStyleID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[StudioTariff] SET (LOCK_ESCALATION = AUTO);

