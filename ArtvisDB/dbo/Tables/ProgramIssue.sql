CREATE TABLE [dbo].[ProgramIssue] (
    [issueID]      INT        IDENTITY (1, 1) NOT NULL,
    [campaignID]   INT        NOT NULL,
    [programID]    SMALLINT   NOT NULL,
    [tariffID]     INT        NOT NULL,
    [issueDate]    DATETIME   NOT NULL,
    [ratio]        FLOAT (53) CONSTRAINT [DF_ProgramIssue_ratio] DEFAULT (1) NOT NULL,
    [isConfirmed]  BIT        CONSTRAINT [DF_ProgramIssue_isConfirmed] DEFAULT (1) NOT NULL,
    [tariffPrice]  MONEY      CONSTRAINT [DF_ProgramIssue_tariffPrice] DEFAULT (0) NOT NULL,
    [advertTypeID] SMALLINT   NULL,
    CONSTRAINT [PK_ProgramIssue] PRIMARY KEY NONCLUSTERED ([issueID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ProgramIssue_AdvertType] FOREIGN KEY ([advertTypeID]) REFERENCES [dbo].[AdvertType] ([advertTypeID]),
    CONSTRAINT [FK_ProgramIssue_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProgramIssue_SponsorTariff] FOREIGN KEY ([tariffID]) REFERENCES [dbo].[SponsorTariff] ([tariffID])
);


GO
ALTER TABLE [dbo].[ProgramIssue] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [UIX_ProgramIssue]
    ON [dbo].[ProgramIssue]([campaignID] ASC, [issueDate] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_ProgramIssue_campaignID_issueDate]
    ON [dbo].[ProgramIssue]([campaignID] ASC, [issueDate] ASC)
    INCLUDE([tariffPrice], [ratio], [advertTypeID], [isConfirmed]);

