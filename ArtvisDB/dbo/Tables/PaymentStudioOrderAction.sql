CREATE TABLE [dbo].[PaymentStudioOrderAction] (
    [paymentID] INT   NOT NULL,
    [actionID]  INT   NOT NULL,
    [summa]     MONEY NOT NULL,
    CONSTRAINT [PK_PaymentStudioOrderAction] PRIMARY KEY CLUSTERED ([paymentID] ASC, [actionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PaymentStudioOrderAction_PaymentStudioOrder] FOREIGN KEY ([paymentID]) REFERENCES [dbo].[PaymentStudioOrder] ([paymentID]),
    CONSTRAINT [FK_PaymentStudioOrderAction_StudioOrderAction] FOREIGN KEY ([actionID]) REFERENCES [dbo].[StudioOrderAction] ([actionID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[PaymentStudioOrderAction] SET (LOCK_ESCALATION = AUTO);

