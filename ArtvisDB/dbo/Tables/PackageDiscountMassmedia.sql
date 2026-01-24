CREATE TABLE [dbo].[PackageDiscountMassmedia] (
    [packageDiscountPriceListID] INT      NOT NULL,
    [massmediaID]                SMALLINT NOT NULL,
    [packageDiscountMassmediaID] INT      IDENTITY (1, 1) NOT NULL,
    [isForType1]                 BIT      CONSTRAINT [DF_PackageDiscountMassmedia_isForType1] DEFAULT ((0)) NOT NULL,
    [isForType2]                 BIT      CONSTRAINT [DF_PackageDiscountMassmedia_isForType11] DEFAULT ((0)) NOT NULL,
    [isForType3]                 BIT      CONSTRAINT [DF_PackageDiscountMassmedia_isForType11_1] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_PackageDiscountMassmedia] PRIMARY KEY NONCLUSTERED ([packageDiscountMassmediaID] ASC),
    CONSTRAINT [FK_PackageDiscountMassmedia_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackageDiscountMassmedia_PackageDiscountPriceList] FOREIGN KEY ([packageDiscountPriceListID]) REFERENCES [dbo].[PackageDiscountPriceList] ([packageDiscountPriceListID])
);


GO
ALTER TABLE [dbo].[PackageDiscountMassmedia] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [UX_packageDiscountMassmedia_packageDiscountPricelistID_massmediaID]
    ON [dbo].[PackageDiscountMassmedia]([packageDiscountPriceListID] ASC, [massmediaID] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Massmedia_PackageDiscount]
    ON [dbo].[PackageDiscountMassmedia]([packageDiscountPriceListID] ASC, [massmediaID] ASC);

