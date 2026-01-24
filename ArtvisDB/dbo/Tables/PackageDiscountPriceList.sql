CREATE TABLE [dbo].[PackageDiscountPriceList] (
    [packageDiscountID]          INT        NOT NULL,
    [packageDiscountPriceListID] INT        IDENTITY (1, 1) NOT NULL,
    [value]                      MONEY      NOT NULL,
    [startDate]                  DATETIME   NOT NULL,
    [finishDate]                 DATETIME   NOT NULL,
    [discount]                   FLOAT (53) NOT NULL,
    [eachVolume]                 FLOAT (53) NOT NULL,
    CONSTRAINT [PK_PackageDiscountPriceList] PRIMARY KEY CLUSTERED ([packageDiscountPriceListID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PackageDiscountPriceList_PackageDiscount] FOREIGN KEY ([packageDiscountID]) REFERENCES [dbo].[PackageDiscount] ([packageDiscountId]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[PackageDiscountPriceList] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_PackageDiscount_StartDate]
    ON [dbo].[PackageDiscountPriceList]([startDate] ASC, [packageDiscountID] ASC) WITH (FILLFACTOR = 90);

