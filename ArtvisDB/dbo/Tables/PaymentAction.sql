CREATE TABLE [dbo].[PaymentAction] (
    [paymentID] INT   NOT NULL,
    [actionID]  INT   NOT NULL,
    [summa]     MONEY NOT NULL,
    CONSTRAINT [PK_PaymentAction] PRIMARY KEY CLUSTERED ([paymentID] ASC, [actionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PaymentAction_Action] FOREIGN KEY ([actionID]) REFERENCES [dbo].[Action] ([actionID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PaymentAction_Payment] FOREIGN KEY ([paymentID]) REFERENCES [dbo].[Payment] ([paymentID])
);


GO
ALTER TABLE [dbo].[PaymentAction] SET (LOCK_ESCALATION = AUTO);

