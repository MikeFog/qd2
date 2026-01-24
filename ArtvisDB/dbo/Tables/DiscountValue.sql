CREATE TABLE [dbo].[DiscountValue] (
    [discountValueID]   SMALLINT   IDENTITY (1, 1) NOT NULL,
    [discountReleaseID] SMALLINT   NOT NULL,
    [summa]             MONEY      NOT NULL,
    [discount]          FLOAT (53) NOT NULL,
    CONSTRAINT [PK_DiscountValue] PRIMARY KEY NONCLUSTERED ([discountValueID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_DiscountValue_DiscountRelease] FOREIGN KEY ([discountReleaseID]) REFERENCES [dbo].[DiscountRelease] ([discountReleaseID]) ON DELETE CASCADE,
    CONSTRAINT [UIX_DiscountValue_release_summa] UNIQUE CLUSTERED ([discountReleaseID] ASC, [summa] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[DiscountValue] SET (LOCK_ESCALATION = AUTO);

