CREATE TABLE [dbo].[StudioOrderAction] (
    [actionID]      INT                  IDENTITY (1, 1) NOT NULL,
    [firmID]        SMALLINT             NOT NULL,
    [userID]        INT                  NULL,
    [createDate]    DATETIME             CONSTRAINT [DF_StudioOrderAction_createDate] DEFAULT (getdate()) NOT NULL,
    [finishDate]    DATETIME             NULL,
    [tariffPrice]   MONEY                CONSTRAINT [DF_StudioOrderAction_price] DEFAULT (0) NOT NULL,
    [totalPrice]    MONEY                CONSTRAINT [DF_StudioOrderAction_finalPrice] DEFAULT (0) NOT NULL,
    [contacts]      [dbo].[doubleString] NULL,
    [orderStatusID] INT                  CONSTRAINT [DF_StudioOrderAction_orderStatusID] DEFAULT (1) NOT NULL,
    [isSpecial]     BIT                  CONSTRAINT [DF_StudioOrderAction_isSpecial] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_StudioOrderActionID] PRIMARY KEY CLUSTERED ([actionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioOrderAction_Firm] FOREIGN KEY ([firmID]) REFERENCES [dbo].[Firm] ([firmID]),
    CONSTRAINT [FK_StudioOrderAction_StudioOrderStatus] FOREIGN KEY ([orderStatusID]) REFERENCES [dbo].[iStudioOrderActionStatus] ([statusID])
);


GO
ALTER TABLE [dbo].[StudioOrderAction] SET (LOCK_ESCALATION = AUTO);


GO
CREATE NONCLUSTERED INDEX [IX_StudioOrderAction_firmID]
    ON [dbo].[StudioOrderAction]([firmID] ASC) WITH (FILLFACTOR = 90);

