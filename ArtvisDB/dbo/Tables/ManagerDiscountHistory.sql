CREATE TABLE [dbo].[ManagerDiscountHistory] (
    [managerDiscountID] INT              IDENTITY (1, 1) NOT NULL,
    [campaignID]        INT              NOT NULL,
    [userID]            SMALLINT         NOT NULL,
    [managerDiscount]   DECIMAL (18, 10) NOT NULL,
    [discountSetTime]   DATETIME         NOT NULL,
    CONSTRAINT [PK_ManagerDiscountHistory] PRIMARY KEY CLUSTERED ([managerDiscountID] ASC),
    CONSTRAINT [FK_ManagerDiscountHistory_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ManagerDiscountHistory_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);

