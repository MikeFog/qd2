CREATE TABLE [dbo].[SponsorProgram] (
    [sponsorProgramID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [name]             NVARCHAR (32) NOT NULL,
    [massmediaID]      SMALLINT      NOT NULL,
    [isActive]         BIT           CONSTRAINT [DF_SponsorProgram_isActive] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_SponsorProgram] PRIMARY KEY NONCLUSTERED ([sponsorProgramID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_SponsorProgram_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID])
);


GO
ALTER TABLE [dbo].[SponsorProgram] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [UIX_SponsorProgram]
    ON [dbo].[SponsorProgram]([massmediaID] ASC, [name] ASC) WITH (FILLFACTOR = 90);

