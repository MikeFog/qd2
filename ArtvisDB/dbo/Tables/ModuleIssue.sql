CREATE TABLE [dbo].[ModuleIssue] (
    [moduleIssueID]     INT        IDENTITY (1, 1) NOT NULL,
    [campaignID]        INT        NOT NULL,
    [moduleID]          SMALLINT   NOT NULL,
    [modulePricelistID] SMALLINT   NOT NULL,
    [issueDate]         DATETIME   NOT NULL,
    [rollerID]          INT        NOT NULL,
    [ratio]             FLOAT (53) CONSTRAINT [DF_ModuleIssue_ratio] DEFAULT (1) NOT NULL,
    [positionId]        SMALLINT   NOT NULL,
    [isConfirmed]       BIT        CONSTRAINT [DF_ModuleIssue_isConfirmed] DEFAULT (0) NOT NULL,
    [tariffPrice]       MONEY      CONSTRAINT [DF_ModuleIssue_tariffPrice] DEFAULT (0) NOT NULL,
    CONSTRAINT [PK_ModuleIssue] PRIMARY KEY CLUSTERED ([moduleIssueID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ModuleIssue_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModuleIssue_IssuePosition] FOREIGN KEY ([positionId]) REFERENCES [dbo].[iIssuePosition] ([positionId]),
    CONSTRAINT [FK_ModuleIssue_Module] FOREIGN KEY ([moduleID]) REFERENCES [dbo].[Module] ([moduleID]),
    CONSTRAINT [FK_ModuleIssue_ModulePriceList] FOREIGN KEY ([modulePricelistID]) REFERENCES [dbo].[ModulePriceList] ([modulePriceListID]),
    CONSTRAINT [FK_ModuleIssue_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID])
);


GO
ALTER TABLE [dbo].[ModuleIssue] SET (LOCK_ESCALATION = AUTO);


GO
CREATE NONCLUSTERED INDEX [IX_ModuleIssue_campaignID_issueDate]
    ON [dbo].[ModuleIssue]([campaignID] ASC, [issueDate] ASC);

