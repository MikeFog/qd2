CREATE TABLE [dbo].[SponsorTariff] (
    [tariffID]      INT                  IDENTITY (1, 1) NOT NULL,
    [pricelistID]   SMALLINT             NOT NULL,
    [time]          [dbo].[DF_TIME]      NOT NULL,
    [monday]        BIT                  CONSTRAINT [DF_SponsorTariff_monday] DEFAULT ((1)) NOT NULL,
    [tuesday]       BIT                  CONSTRAINT [DF_SponsorTariff_tuesday] DEFAULT ((1)) NOT NULL,
    [wednesday]     BIT                  CONSTRAINT [DF_SponsorTariff_wednesday] DEFAULT ((1)) NOT NULL,
    [thursday]      BIT                  CONSTRAINT [DF_SponsorTariff_thursday] DEFAULT ((1)) NOT NULL,
    [friday]        BIT                  CONSTRAINT [DF_SponsorTariff_friday] DEFAULT ((1)) NOT NULL,
    [saturday]      BIT                  CONSTRAINT [DF_SponsorTariff_saturday] DEFAULT ((1)) NOT NULL,
    [sunday]        BIT                  CONSTRAINT [DF_SponsorTariff_sunday] DEFAULT ((1)) NOT NULL,
    [price]         MONEY                CONSTRAINT [DF_SponsorTariff_price] DEFAULT ((0)) NOT NULL,
    [duration]      [dbo].[timeDuration] CONSTRAINT [DF_SponsorTariff_duration] DEFAULT ((0)) NOT NULL,
    [comment]       NVARCHAR (32)        NULL,
    [isAlive]       BIT                  NOT NULL,
    [path]          NVARCHAR (255)       NULL,
    [suffix]        NVARCHAR (32)        NULL,
    [needExt]       BIT                  CONSTRAINT [DF_SponsorTariff_needExt] DEFAULT ((1)) NOT NULL,
    [needInJingle]  BIT                  CONSTRAINT [DF_SponsorTariff_needInJingle] DEFAULT ((1)) NOT NULL,
    [needOutJingle] BIT                  CONSTRAINT [DF_SponsorTariff_needOutJingle] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_SponsorTariff] PRIMARY KEY NONCLUSTERED ([tariffID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_SponsorTariff_SponsorProgramPricelist] FOREIGN KEY ([pricelistID]) REFERENCES [dbo].[SponsorProgramPricelist] ([pricelistID])
);


GO
ALTER TABLE [dbo].[SponsorTariff] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [IX_SponsorTariff_Pricelist]
    ON [dbo].[SponsorTariff]([pricelistID] ASC, [time] ASC) WITH (FILLFACTOR = 90);

