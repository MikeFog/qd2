CREATE TABLE [dbo].[PaymentType] (
    [paymentTypeID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [name]          NVARCHAR (32) NOT NULL,
    [isHidden]      BIT           NOT NULL,
    [isActive]      BIT           CONSTRAINT [DF__paymentTy__isAct__02A839AF] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_paymentType] PRIMARY KEY CLUSTERED ([paymentTypeID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_PaymentType_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[PaymentType] SET (LOCK_ESCALATION = AUTO);

