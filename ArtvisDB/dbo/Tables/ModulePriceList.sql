CREATE TABLE [dbo].[ModulePriceList] (
    [modulePriceListID]       SMALLINT IDENTITY (1, 1) NOT NULL,
    [moduleID]                SMALLINT NOT NULL,
    [priceListID]             SMALLINT NOT NULL,
    [price]                   MONEY    NOT NULL,
    [tariffCount]             INT      CONSTRAINT [DF_ModulePriceList_tariffCount] DEFAULT (0) NOT NULL,
    [startDate]               DATETIME NOT NULL,
    [finishDate]              DATETIME NOT NULL,
    [rollerID]                INT      NULL,
    [isStandAlone]            BIT      CONSTRAINT [DF_ModulePriceList_isStandAlone] DEFAULT (0) NOT NULL,
    [extraChargeFirstRoller]  TINYINT  CONSTRAINT [DF_ModulePriceList_extraChargeFirstRoller] DEFAULT ((0)) NOT NULL,
    [extraChargeSecondRoller] TINYINT  CONSTRAINT [DF_ModulePriceList_extraChargeSecondRoller] DEFAULT ((0)) NOT NULL,
    [extraChargeLastRoller]   TINYINT  CONSTRAINT [DF_ModulePriceList_extraChargeLastRoller] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ModulePriceList] PRIMARY KEY NONCLUSTERED ([modulePriceListID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ModulePriceList_Module] FOREIGN KEY ([moduleID]) REFERENCES [dbo].[Module] ([moduleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModulePriceList_Pricelist] FOREIGN KEY ([priceListID]) REFERENCES [dbo].[Pricelist] ([pricelistID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModulePriceList_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID])
);


GO
ALTER TABLE [dbo].[ModulePriceList] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [UIX_ModulePriceList]
    ON [dbo].[ModulePriceList]([moduleID] ASC, [priceListID] ASC) WITH (FILLFACTOR = 90);

