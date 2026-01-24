CREATE TABLE [dbo].[AgencyBillNo] (
    [agencyID]   SMALLINT NOT NULL,
    [year]       SMALLINT NOT NULL,
    [billNo]     SMALLINT CONSTRAINT [DF_AgencyBillNo_billNo] DEFAULT (1) NOT NULL,
    [contractNo] SMALLINT CONSTRAINT [DF_AgencyBillNo_contractNo] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_AgencyBillNo] PRIMARY KEY CLUSTERED ([agencyID] ASC, [year] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_AgencyBillNo_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[AgencyBillNo] SET (LOCK_ESCALATION = AUTO);

