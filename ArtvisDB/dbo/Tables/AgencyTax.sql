CREATE TABLE [dbo].[AgencyTax] (
    [agencyTaxID] SMALLINT        IDENTITY (1, 1) NOT NULL,
    [agencyID]    SMALLINT        NOT NULL,
    [taxID]       NCHAR (10)      NOT NULL,
    [startDate]   DATETIME        NOT NULL,
    [finishDate]  DATETIME        NOT NULL,
    [divisor]     DECIMAL (10, 6) NOT NULL,
    CONSTRAINT [PK_AgencyTax] PRIMARY KEY CLUSTERED ([agencyTaxID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_AgencyTax_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
    CONSTRAINT [FK_AgencyTax_Tax] FOREIGN KEY ([taxID]) REFERENCES [dbo].[iTax] ([taxId])
);


GO
ALTER TABLE [dbo].[AgencyTax] SET (LOCK_ESCALATION = AUTO);

