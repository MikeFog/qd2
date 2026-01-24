CREATE TABLE [dbo].[UserDiscount] (
    [userID]     SMALLINT   NOT NULL,
    [startDate]  DATETIME   NOT NULL,
    [finishDate] DATETIME   NOT NULL,
    [maxRatio]   FLOAT (53) NOT NULL,
    [discountID] INT        IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_UserDiscount] PRIMARY KEY CLUSTERED ([discountID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_UserDiscount_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[UserDiscount] SET (LOCK_ESCALATION = AUTO);

