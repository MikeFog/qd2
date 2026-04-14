CREATE TABLE [dbo].[ManagerDiscountHistory] (
    [managerDiscountHistoryID] INT              IDENTITY (1, 1) NOT NULL,
    [campaignID]               INT              NOT NULL,
    [userID]                   SMALLINT         NOT NULL,
    [managerDiscount]          DECIMAL (18, 10) NOT NULL,
    [discountSetTime]          DATETIME         NOT NULL,
    [managerDiscountReasonID]  SMALLINT         NULL,
    CONSTRAINT [PK_ManagerDiscountHistory] PRIMARY KEY CLUSTERED ([managerDiscountHistoryID] ASC),
    CONSTRAINT [FK_ManagerDiscountHistory_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]),
    CONSTRAINT [FK_ManagerDiscountHistory_ManagerDiscountReason] FOREIGN KEY ([managerDiscountReasonID]) REFERENCES [dbo].[ManagerDiscountReason] ([managerDiscountReasonID]),
    CONSTRAINT [FK_ManagerDiscountHistory_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);

