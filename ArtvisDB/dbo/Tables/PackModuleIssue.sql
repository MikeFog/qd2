CREATE TABLE [dbo].[PackModuleIssue] (
    [packModuleIssueID] INT        IDENTITY (1, 1) NOT NULL,
    [campaignID]        INT        NOT NULL,
    [pricelistID]       SMALLINT   NOT NULL,
    [issueDate]         DATETIME   NOT NULL,
    [rollerID]          INT        NOT NULL,
    [ratio]             FLOAT (53) CONSTRAINT [DF_PackModuleIssue_ratio] DEFAULT (1) NOT NULL,
    [positionId]        SMALLINT   NOT NULL,
    [isConfirmed]       BIT        CONSTRAINT [DF_PackModuleIssue_isConfirmed] DEFAULT (0) NOT NULL,
    [tariffPrice]       MONEY      CONSTRAINT [DF_PackModuleIssue_tariffPrice] DEFAULT (0) NOT NULL,
    CONSTRAINT [PK_PackModuleIssue] PRIMARY KEY NONCLUSTERED ([packModuleIssueID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_PackModuleIssue_Campaign] FOREIGN KEY ([campaignID]) REFERENCES [dbo].[Campaign] ([campaignID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackModuleIssue_IssuePosition] FOREIGN KEY ([positionId]) REFERENCES [dbo].[iIssuePosition] ([positionId]),
    CONSTRAINT [FK_PackModuleIssue_PackModulePriceList] FOREIGN KEY ([pricelistID]) REFERENCES [dbo].[PackModulePriceList] ([priceListID]),
    CONSTRAINT [FK_PackModuleIssue_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID])
);


GO
ALTER TABLE [dbo].[PackModuleIssue] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [UX_PackModuleIssue_campaignID_packModuleIssueID]
    ON [dbo].[PackModuleIssue]([campaignID] ASC, [packModuleIssueID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PackModuleIssue_CampaignId]
    ON [dbo].[PackModuleIssue]([campaignID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_PackModuleIssue_issueDate_campaignID]
    ON [dbo].[PackModuleIssue]([issueDate] ASC, [campaignID] ASC)
    INCLUDE([pricelistID], [ratio], [rollerID], [tariffPrice]);

