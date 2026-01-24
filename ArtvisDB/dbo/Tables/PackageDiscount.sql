CREATE TABLE [dbo].[PackageDiscount] (
    [packageDiscountId] INT           IDENTITY (1, 1) NOT NULL,
    [name]              NVARCHAR (32) NOT NULL,
    [count]             TINYINT       NOT NULL,
    CONSTRAINT [PK_PackageDiscount] PRIMARY KEY NONCLUSTERED ([packageDiscountId] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[PackageDiscount] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [UIX_PackageDicountId]
    ON [dbo].[PackageDiscount]([packageDiscountId] ASC) WITH (FILLFACTOR = 90);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_PackageDiscountName]
    ON [dbo].[PackageDiscount]([name] ASC) WITH (FILLFACTOR = 90);

