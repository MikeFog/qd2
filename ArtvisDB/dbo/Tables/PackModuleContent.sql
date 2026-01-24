CREATE TABLE [dbo].[PackModuleContent] (
    [packModuleContentID] SMALLINT IDENTITY (1, 1) NOT NULL,
    [pricelistID]         SMALLINT NOT NULL,
    [moduleID]            SMALLINT NOT NULL,
    [modulePriceListID]   SMALLINT NOT NULL,
    CONSTRAINT [PK_PackModuleContent] PRIMARY KEY CLUSTERED ([packModuleContentID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PackModuleContent_Module] FOREIGN KEY ([moduleID]) REFERENCES [dbo].[Module] ([moduleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackModuleContent_ModulePriceList] FOREIGN KEY ([modulePriceListID]) REFERENCES [dbo].[ModulePriceList] ([modulePriceListID]),
    CONSTRAINT [FK_PackModuleContent_PackModulePriceList] FOREIGN KEY ([pricelistID]) REFERENCES [dbo].[PackModulePriceList] ([priceListID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[PackModuleContent] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_PackModuleContent]
    ON [dbo].[PackModuleContent]([pricelistID] ASC, [moduleID] ASC) WITH (FILLFACTOR = 90);

