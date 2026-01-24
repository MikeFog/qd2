CREATE TABLE [dbo].[DiscountRelease] (
    [discountReleaseID] SMALLINT IDENTITY (1, 1) NOT NULL,
    [startDate]         DATETIME NOT NULL,
    [finishDate]        DATETIME NULL,
    [massmediaID]       SMALLINT NOT NULL,
    [isForType1]        BIT      CONSTRAINT [DF_DiscountRelease_isForType1] DEFAULT ((0)) NOT NULL,
    [isForType2]        BIT      CONSTRAINT [DF_DiscountRelease_isForType11] DEFAULT ((0)) NOT NULL,
    [isForType3]        BIT      CONSTRAINT [DF_DiscountRelease_isForType11_1] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_DiscountRelease] PRIMARY KEY NONCLUSTERED ([discountReleaseID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_DiscountRelease_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[DiscountRelease] SET (LOCK_ESCALATION = AUTO);

