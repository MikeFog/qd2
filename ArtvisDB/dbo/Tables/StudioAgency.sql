CREATE TABLE [dbo].[StudioAgency] (
    [studioID] SMALLINT NOT NULL,
    [agencyID] SMALLINT NOT NULL,
    CONSTRAINT [PK_StudioAgency] PRIMARY KEY CLUSTERED ([studioID] ASC, [agencyID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_StudioAgency_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudioAgency_Studio] FOREIGN KEY ([studioID]) REFERENCES [dbo].[Studio] ([studioID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[StudioAgency] SET (LOCK_ESCALATION = AUTO);

