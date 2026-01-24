CREATE TABLE [dbo].[Pricelist] (
    [pricelistID]             SMALLINT        IDENTITY (1, 1) NOT NULL,
    [massmediaID]             SMALLINT        NOT NULL,
    [startDate]               [dbo].[DF_DATE] NOT NULL,
    [finishDate]              [dbo].[DF_DATE] NOT NULL,
    [broadcastStart]          [dbo].[DF_TIME] CONSTRAINT [DF_Pricelist_broadcastStart] DEFAULT ('01.01.1900') NOT NULL,
    [extraChargeFirstRoller]  TINYINT         CONSTRAINT [DF_Pricelist_extraChargeFirstRoller] DEFAULT ((0)) NOT NULL,
    [extraChargeSecondRoller] TINYINT         CONSTRAINT [DF_Pricelist_extraChargeSecondRoller] DEFAULT ((0)) NOT NULL,
    [extraChargeLastRoller]   TINYINT         CONSTRAINT [DF_Pricelist_extraChargeLastRoller] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Pricelist] PRIMARY KEY NONCLUSTERED ([pricelistID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Pricelist_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]),
    CONSTRAINT [UIX_Pricelist_massmedia_startDate] UNIQUE CLUSTERED ([massmediaID] ASC, [startDate] DESC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[Pricelist] SET (LOCK_ESCALATION = AUTO);

