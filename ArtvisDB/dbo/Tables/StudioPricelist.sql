CREATE TABLE [dbo].[StudioPricelist] (
    [pricelistID] SMALLINT IDENTITY (1, 1) NOT NULL,
    [studioID]    SMALLINT NOT NULL,
    [startDate]   DATETIME NOT NULL,
    [finishDate]  DATETIME NOT NULL,
    CONSTRAINT [PK_StudioPricelist] PRIMARY KEY CLUSTERED ([pricelistID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioPricelist_Studio] FOREIGN KEY ([studioID]) REFERENCES [dbo].[Studio] ([studioID]) ON DELETE CASCADE,
    CONSTRAINT [UIX_StudioPricelist_startDate] UNIQUE NONCLUSTERED ([studioID] ASC, [startDate] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[StudioPricelist] SET (LOCK_ESCALATION = AUTO);

