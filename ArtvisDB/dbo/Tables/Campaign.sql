CREATE TABLE [dbo].[Campaign] (
    [campaignID]      INT              IDENTITY (1, 1) NOT NULL,
    [actionID]        INT              NOT NULL,
    [startDate]       DATETIME         NULL,
    [finishDate]      DATETIME         NULL,
    [discount]        DECIMAL (9, 4)   CONSTRAINT [DF_Campaign_discount] DEFAULT ((1)) NOT NULL,
    [tariffPrice]     DECIMAL (18, 2)  CONSTRAINT [DF_Campaign_tariffPrice] DEFAULT ((0)) NOT NULL,
    [price]           AS               ([tariffPrice]*[discount]),
    [finalPrice]      DECIMAL (18, 2)  CONSTRAINT [DF_Campaign_finalPrice] DEFAULT ((0)) NOT NULL,
    [paymentTypeID]   SMALLINT         NOT NULL,
    [massmediaID]     SMALLINT         NULL,
    [campaignTypeID]  TINYINT          NOT NULL,
    [issuesCount]     SMALLINT         CONSTRAINT [DF_Campaign_issuesCount] DEFAULT ((0)) NOT NULL,
    [issuesDuration]  INT              CONSTRAINT [DF_Campaign_issuesDuration] DEFAULT ((0)) NOT NULL,
    [modTime]         DATETIME         CONSTRAINT [DF_Campaign_modTime] DEFAULT (getdate()) NOT NULL,
    [modUser]         SMALLINT         NOT NULL,
    [agencyID]        SMALLINT         NOT NULL,
    [timeBonus]       INT              CONSTRAINT [DF_Campaign_timeBonus] DEFAULT ((0)) NOT NULL,
    [programsCount]   INT              CONSTRAINT [DF_Campaign_programsCount] DEFAULT ((0)) NOT NULL,
    [billNo]          INT              NULL,
    [billDate]        DATETIME         NULL,
    [managerDiscount] DECIMAL (18, 10) CONSTRAINT [DF_Campaign_managerDiscount] DEFAULT ((1)) NOT NULL,
    [contractNo]      INT              NULL,
    CONSTRAINT [PK_Campaign] PRIMARY KEY NONCLUSTERED ([campaignID] ASC),
    CONSTRAINT [FK_Campaign_Action] FOREIGN KEY ([actionID]) REFERENCES [dbo].[Action] ([actionID]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [UIX_Campaign] UNIQUE NONCLUSTERED ([actionID] ASC, [massmediaID] ASC, [campaignTypeID] ASC, [paymentTypeID] ASC, [agencyID] ASC)
);








GO
ALTER TABLE [dbo].[Campaign] SET (LOCK_ESCALATION = AUTO);


GO



GO
CREATE NONCLUSTERED INDEX [IX_Campaign_campaignTypeID__campaignID]
    ON [dbo].[Campaign]([campaignTypeID] ASC)
    INCLUDE([campaignID]);


GO
CREATE NONCLUSTERED INDEX [IX_Campaign_Massmedia]
    ON [dbo].[Campaign]([massmediaID] ASC, [startDate] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Campaign_actionID_massmediaID]
    ON [dbo].[Campaign]([actionID] ASC, [massmediaID] ASC) WITH (FILLFACTOR = 90);

