CREATE TABLE [dbo].[Tariff] (
    [tariffID]        INT                  IDENTITY (1, 1) NOT NULL,
    [pricelistID]     SMALLINT             NOT NULL,
    [time]            [dbo].[DF_TIME]      CONSTRAINT [DF_Tariff_time] DEFAULT ('01.01.1900') NOT NULL,
    [monday]          BIT                  CONSTRAINT [DF_Tariff_monday] DEFAULT ((1)) NOT NULL,
    [tuesday]         BIT                  CONSTRAINT [DF_Tariff_tuesday] DEFAULT ((1)) NOT NULL,
    [wednesday]       BIT                  CONSTRAINT [DF_Tariff_wednesday] DEFAULT ((1)) NOT NULL,
    [thursday]        BIT                  CONSTRAINT [DF_Tariff_thursday] DEFAULT ((1)) NOT NULL,
    [friday]          BIT                  CONSTRAINT [DF_Tariff_friday] DEFAULT ((1)) NOT NULL,
    [saturday]        BIT                  CONSTRAINT [DF_Tariff_saturday] DEFAULT ((1)) NOT NULL,
    [sunday]          BIT                  CONSTRAINT [DF_Tariff_sunday] DEFAULT ((1)) NOT NULL,
    [price]           MONEY                NOT NULL,
    [duration]        [dbo].[timeDuration] CONSTRAINT [DF_Tariff_duration] DEFAULT ((0)) NOT NULL,
    [comment]         NVARCHAR (32)        NULL,
    [isForModuleOnly] BIT                  NOT NULL,
    [suffix]          VARCHAR (16)         NULL,
    [needExt]         BIT                  CONSTRAINT [DF_Tariff_needExt] DEFAULT ((1)) NOT NULL,
    [maxCapacity]     SMALLINT             NOT NULL,
    [needInJingle]    BIT                  CONSTRAINT [DF_Tariff_needInJingle] DEFAULT ((1)) NOT NULL,
    [needOutJingle]   BIT                  CONSTRAINT [DF_Tariff_needOutJingle] DEFAULT ((1)) NOT NULL,
    [blockTypeID]     SMALLINT             NULL,
    [notEarly]        BIT                  CONSTRAINT [DF_Tariff_notEarly] DEFAULT ((0)) NOT NULL,
    [notLater]        BIT                  CONSTRAINT [DF_Tariff_notLater] DEFAULT ((0)) NOT NULL,
    [openBlock]       BIT                  CONSTRAINT [DF_Tariff_openBlock] DEFAULT ((0)) NOT NULL,
    [openPhonogram]   BIT                  CONSTRAINT [DF_Tariff_openPhonogram] DEFAULT ((0)) NOT NULL,
    [duration_total]  [dbo].[timeDuration] CONSTRAINT [DF_Tariff_duration_total] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Tariff] PRIMARY KEY NONCLUSTERED ([tariffID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Tariff_BlockType] FOREIGN KEY ([blockTypeID]) REFERENCES [dbo].[BlockType] ([blockTypeID]),
    CONSTRAINT [FK_Tariff_Pricelist] FOREIGN KEY ([pricelistID]) REFERENCES [dbo].[Pricelist] ([pricelistID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[Tariff] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [UIX_Tariff_Pricelist_time]
    ON [dbo].[Tariff]([pricelistID] ASC, [time] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Tariff_isForModuleOnly_tariffID]
    ON [dbo].[Tariff]([isForModuleOnly] ASC, [tariffID] ASC) WITH (FILLFACTOR = 90);

