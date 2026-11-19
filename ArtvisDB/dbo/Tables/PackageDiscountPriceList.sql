CREATE TABLE [dbo].[PackageDiscountPriceList] (
    [packageDiscountID]          INT             NOT NULL,
    [packageDiscountPriceListID] INT             IDENTITY (1, 1) NOT NULL,
    [value]                      DECIMAL (18, 2) NOT NULL,
    [startDate]                  DATETIME        NOT NULL,
    [finishDate]                 DATETIME        NOT NULL,
    [discount]                   DECIMAL (9, 4)  NOT NULL,
    [eachVolume]                 DECIMAL (9, 4)  NOT NULL,
    [maxPrimePercent]            TINYINT         CONSTRAINT [DF_PackageDiscountPriceList_maxPrimePercent] DEFAULT ((100)) NOT NULL, -- максимальная доля прайма кампании, % (docs/tasks/package-discount-prime-limit.md)
    CONSTRAINT [PK_PackageDiscountPriceList] PRIMARY KEY CLUSTERED ([packageDiscountPriceListID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PackageDiscountPriceList_PackageDiscount] FOREIGN KEY ([packageDiscountID]) REFERENCES [dbo].[PackageDiscount] ([packageDiscountId]) ON DELETE CASCADE
);





GO
ALTER TABLE [dbo].[PackageDiscountPriceList] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_PackageDiscount_StartDate]
    ON [dbo].[PackageDiscountPriceList]([startDate] ASC, [packageDiscountID] ASC) WITH (FILLFACTOR = 90);

