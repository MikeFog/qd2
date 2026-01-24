CREATE TABLE [dbo].[AgencyMassmedia] (
    [agencyID]    SMALLINT NOT NULL,
    [massmediaID] SMALLINT NOT NULL,
    CONSTRAINT [PK_AgencyMassmedia] PRIMARY KEY CLUSTERED ([agencyID] ASC, [massmediaID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_AgencyMassmedia_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
    CONSTRAINT [FK_AgencyMassmedia_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[AgencyMassmedia] SET (LOCK_ESCALATION = AUTO);

