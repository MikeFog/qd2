CREATE TABLE [dbo].[StudioOrderBill] (
    [billNo]   INT      NOT NULL,
    [billDate] DATETIME NOT NULL,
    [actionID] INT      NOT NULL,
    [agencyID] SMALLINT NOT NULL,
    CONSTRAINT [PK_StudioOrderBill] PRIMARY KEY CLUSTERED ([actionID] ASC, [agencyID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioOrderBill_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]),
    CONSTRAINT [FK_StudioOrderBill_StudioOrderAction] FOREIGN KEY ([actionID]) REFERENCES [dbo].[StudioOrderAction] ([actionID]) ON DELETE CASCADE,
    CONSTRAINT [UIX_StudioOrderBill_Action] UNIQUE NONCLUSTERED ([actionID] ASC, [agencyID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[StudioOrderBill] SET (LOCK_ESCALATION = AUTO);

