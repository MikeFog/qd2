CREATE TABLE [dbo].[PackModulePriceList] (
    [priceListID]             SMALLINT IDENTITY (1, 1) NOT NULL,
    [packModuleID]            SMALLINT NOT NULL,
    [startDate]               DATETIME NOT NULL,
    [finishDate]              DATETIME NOT NULL,
    [price]                   MONEY    CONSTRAINT [DF_PackModulePriceList_price] DEFAULT (0) NOT NULL,
    [extraChargeFirstRoller]  TINYINT  CONSTRAINT [DF_PackModulePriceList_extraChargeFirstRoller] DEFAULT (0) NOT NULL,
    [extraChargeSecondRoller] TINYINT  CONSTRAINT [DF_PackModulePriceList_extraChargeSecondRoller] DEFAULT (0) NOT NULL,
    [extraChargeLastRoller]   TINYINT  CONSTRAINT [DF_PackModulePriceList_extraChargeLastRoller] DEFAULT (0) NOT NULL,
    [rollerID]                INT      NULL,
    CONSTRAINT [PK_PackModulePriceList] PRIMARY KEY NONCLUSTERED ([priceListID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PackModulePriceList_PackModule] FOREIGN KEY ([packModuleID]) REFERENCES [dbo].[PackModule] ([packModuleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackModulePriceList_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID])
);


GO
ALTER TABLE [dbo].[PackModulePriceList] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [IX_PackModulePriceList_Module_StartDate]
    ON [dbo].[PackModulePriceList]([packModuleID] ASC, [startDate] ASC) WITH (FILLFACTOR = 90);

