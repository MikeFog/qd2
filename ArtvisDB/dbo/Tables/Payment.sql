CREATE TABLE [dbo].[Payment] (
    [paymentID]     INT      IDENTITY (1, 1) NOT NULL,
    [firmID]        SMALLINT NOT NULL,
    [summa]         MONEY    CONSTRAINT [DF_Payment_SUMMA] DEFAULT (0) NOT NULL,
    [paymentDate]   DATETIME NOT NULL,
    [userID]        SMALLINT NOT NULL,
    [paymentTypeID] SMALLINT NOT NULL,
    [agencyID]      SMALLINT NOT NULL,
    [isEnabled]     BIT      CONSTRAINT [DF_Payment_IS_ENABLED] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED ([paymentID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Payment_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]),
    CONSTRAINT [FK_Payment_Firm] FOREIGN KEY ([firmID]) REFERENCES [dbo].[Firm] ([firmID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Payment_PaymentType] FOREIGN KEY ([paymentTypeID]) REFERENCES [dbo].[PaymentType] ([paymentTypeID]),
    CONSTRAINT [FK_Payment_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID])
);


GO
ALTER TABLE [dbo].[Payment] SET (LOCK_ESCALATION = AUTO);

