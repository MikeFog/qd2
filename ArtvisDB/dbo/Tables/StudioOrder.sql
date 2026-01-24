CREATE TABLE [dbo].[StudioOrder] (
    [studioOrderID]  INT                  IDENTITY (1, 1) NOT NULL,
    [actionID]       INT                  NOT NULL,
    [studioID]       SMALLINT             NULL,
    [agencyID]       SMALLINT             NOT NULL,
    [rolstyleID]     SMALLINT             NULL,
    [paymentTypeID]  SMALLINT             NOT NULL,
    [name]           VARCHAR (64)         NULL,
    [price]          MONEY                NOT NULL,
    [rollerID]       INT                  NULL,
    [rollerDuration] [dbo].[timeDuration] NULL,
    [createDate]     DATETIME             CONSTRAINT [DF_StudioOrder_createDate] DEFAULT ([dbo].[ToShortDate](getdate())) NOT NULL,
    [isComplete]     BIT                  CONSTRAINT [DF_StudioOrder_isReady] DEFAULT (0) NOT NULL,
    [finishDate]     DATETIME             NULL,
    [ratio]          FLOAT (53)           CONSTRAINT [DF_StudioOrder_ratio] DEFAULT (1) NOT NULL,
    [tariffID]       INT                  NULL,
    [finalPrice]     AS                   (convert(money,([price] * [ratio]))),
    [userID]         SMALLINT             NOT NULL,
    CONSTRAINT [PK_StudioOrder] PRIMARY KEY CLUSTERED ([studioOrderID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioOrder_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]),
    CONSTRAINT [FK_StudioOrder_PaymentType] FOREIGN KEY ([paymentTypeID]) REFERENCES [dbo].[PaymentType] ([paymentTypeID]),
    CONSTRAINT [FK_StudioOrder_RolStyle] FOREIGN KEY ([rolstyleID]) REFERENCES [dbo].[RolStyle] ([rolStyleID]),
    CONSTRAINT [FK_StudioOrder_Studio] FOREIGN KEY ([studioID]) REFERENCES [dbo].[Studio] ([studioID]),
    CONSTRAINT [FK_StudioOrder_StudioOrderAction] FOREIGN KEY ([actionID]) REFERENCES [dbo].[StudioOrderAction] ([actionID]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudioOrder_StudioTariff] FOREIGN KEY ([tariffID]) REFERENCES [dbo].[StudioTariff] ([tariffID]),
    CONSTRAINT [FK_StudioOrder_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID])
);


GO
ALTER TABLE [dbo].[StudioOrder] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_StudioOrderName]
    ON [dbo].[StudioOrder]([name] ASC) WITH (FILLFACTOR = 90);

