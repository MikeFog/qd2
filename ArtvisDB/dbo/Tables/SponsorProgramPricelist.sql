CREATE TABLE [dbo].[SponsorProgramPricelist] (
    [pricelistID]      SMALLINT             IDENTITY (1, 1) NOT NULL,
    [sponsorProgramID] SMALLINT             NOT NULL,
    [startDate]        [dbo].[DF_DATE]      NOT NULL,
    [finishDate]       DATETIME             NOT NULL,
    [bonus]            [dbo].[timeDuration] CONSTRAINT [DF_SponsorProgramPricelist_bonus] DEFAULT (0) NOT NULL,
    [broadcastStart]   [dbo].[DF_TIME]      CONSTRAINT [DF_SponsorProgramPricelist_broadcastStart] DEFAULT ('01.01.1900') NOT NULL,
    [isStandAlone]     BIT                  CONSTRAINT [DF_SponsorProgramPricelist_isStandAlone] DEFAULT (0) NOT NULL,
    CONSTRAINT [PK_SponsorProgramPricelist] PRIMARY KEY NONCLUSTERED ([pricelistID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_SponsorProgramPricelist_SponsorProgram] FOREIGN KEY ([sponsorProgramID]) REFERENCES [dbo].[SponsorProgram] ([sponsorProgramID]) ON DELETE CASCADE,
    CONSTRAINT [UIX_SponsorProgramPricelist] UNIQUE CLUSTERED ([sponsorProgramID] ASC, [startDate] DESC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[SponsorProgramPricelist] SET (LOCK_ESCALATION = AUTO);

