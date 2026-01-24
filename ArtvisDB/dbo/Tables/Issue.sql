CREATE TABLE [dbo].[Issue] (
    [issueID]           INT        IDENTITY (1, 1) NOT NULL,
    [rollerID]          INT        NOT NULL,
    [originalWindowID]  INT        NOT NULL,
    [actualWindowID]    INT        NOT NULL,
    [campaignID]        INT        NOT NULL,
    [positionId]        SMALLINT   CONSTRAINT [DF_Issue_position] DEFAULT (0) NOT NULL,
    [ratio]             FLOAT (53) CONSTRAINT [DF_Issue_ratio] DEFAULT (1) NOT NULL,
    [moduleIssueID]     INT        NULL,
    [isConfirmed]       BIT        CONSTRAINT [DF_Issue_isConfirmed] DEFAULT (0) NOT NULL,
    [packModuleIssueID] INT        NULL,
    [tariffPrice]       MONEY      CONSTRAINT [DF_Issue_tariffPrice] DEFAULT (0) NOT NULL,
    [grantorID]         SMALLINT   NULL,
    [activationDate]    DATETIME   NULL,
    CONSTRAINT [PK_Issue] PRIMARY KEY NONCLUSTERED ([issueID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Issue_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Issue_IssuePosition] FOREIGN KEY ([positionId]) REFERENCES [dbo].[iIssuePosition] ([positionId]),
    CONSTRAINT [FK_Issue_ModuleIssue] FOREIGN KEY ([moduleIssueID]) REFERENCES [dbo].[ModuleIssue] ([moduleIssueID]),
    CONSTRAINT [FK_Issue_PackModuleIssue] FOREIGN KEY ([packModuleIssueID]) REFERENCES [dbo].[PackModuleIssue] ([packModuleIssueID]),
    CONSTRAINT [FK_Issue_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID]),
    CONSTRAINT [FK_Issue_TariffWindow] FOREIGN KEY ([originalWindowID]) REFERENCES [dbo].[TariffWindow] ([windowId]),
    CONSTRAINT [FK_Issue_TariffWindow1] FOREIGN KEY ([actualWindowID]) REFERENCES [dbo].[TariffWindow] ([windowId]),
    CONSTRAINT [FK_Issue_User] FOREIGN KEY ([grantorID]) REFERENCES [dbo].[User] ([userID])
);


GO
ALTER TABLE [dbo].[Issue] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [UX_Issue_OriginalWindowID_campaignID_IssueID]
    ON [dbo].[Issue]([originalWindowID] ASC, [campaignID] ASC, [issueID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_PackModuleIssueId]
    ON [dbo].[Issue]([packModuleIssueID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_campaignID__rollerID_tariffPrice]
    ON [dbo].[Issue]([campaignID] ASC)
    INCLUDE([rollerID], [tariffPrice]);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_ModuleIssueId]
    ON [dbo].[Issue]([moduleIssueID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_ActualWindowID]
    ON [dbo].[Issue]([actualWindowID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_campaignID__rollerID_ratio_tariffPrice]
    ON [dbo].[Issue]([campaignID] ASC)
    INCLUDE([rollerID], [ratio], [tariffPrice]);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_Roller]
    ON [dbo].[Issue]([rollerID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_OriginalWindowID_isConfirmed_rollerID]
    ON [dbo].[Issue]([originalWindowID] ASC, [isConfirmed] ASC, [rollerID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Issue_campaignID_originalWindowID]
    ON [dbo].[Issue]([campaignID] ASC, [originalWindowID] ASC)
    INCLUDE([ratio], [rollerID], [tariffPrice]);

