CREATE TABLE [dbo].[Bill] (
    [billNo]   INT      NOT NULL,
    [billDate] DATETIME NOT NULL,
    [actionID] INT      NOT NULL,
    [agencyID] SMALLINT NOT NULL,
    CONSTRAINT [PK_Bill_1] PRIMARY KEY CLUSTERED ([actionID] ASC, [agencyID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Bill_Action] FOREIGN KEY ([actionID]) REFERENCES [dbo].[Action] ([actionID]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [FK_Bill_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]),
    CONSTRAINT [UIX_Bill_Action] UNIQUE NONCLUSTERED ([actionID] ASC, [agencyID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[Bill] SET (LOCK_ESCALATION = AUTO);

