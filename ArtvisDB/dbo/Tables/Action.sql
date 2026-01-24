CREATE TABLE [dbo].[Action] (
    [actionID]            INT        IDENTITY (1, 1) NOT NULL,
    [firmID]              SMALLINT   NOT NULL,
    [startDate]           DATETIME   NULL,
    [finishDate]          DATETIME   NULL,
    [discount]            FLOAT (53) CONSTRAINT [DF_Action_discount] DEFAULT ((1)) NOT NULL,
    [userID]              SMALLINT   NULL,
    [tariffPrice]         MONEY      CONSTRAINT [DF_Action_tariffPrice] DEFAULT ((0)) NOT NULL,
    [priceSumByCampaigns] MONEY      CONSTRAINT [DF_Action_price] DEFAULT ((0)) NOT NULL,
    [createDate]          DATETIME   CONSTRAINT [DF_Action_createDate] DEFAULT (getdate()) NOT NULL,
    [modDate]             DATETIME   CONSTRAINT [DF_Action_modDate] DEFAULT (getdate()) NOT NULL,
    [isSpecial]           TINYINT    CONSTRAINT [DF_Action_isSpecial] DEFAULT ((0)) NOT NULL,
    [isConfirmed]         BIT        CONSTRAINT [DF_Action_isFake] DEFAULT ((0)) NOT NULL,
    [totalPrice]          MONEY      CONSTRAINT [DF_Action_totalPrice] DEFAULT ((0)) NOT NULL,
    [isAlerted]           BIT        CONSTRAINT [DF_Action_isAlerted] DEFAULT ((0)) NOT NULL,
    [deleteDate]          DATETIME   NULL,
    CONSTRAINT [PK_Action] PRIMARY KEY NONCLUSTERED ([actionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Action_Firm] FOREIGN KEY ([firmID]) REFERENCES [dbo].[Firm] ([firmID]),
    CONSTRAINT [FK_Action_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID])
);


GO
ALTER TABLE [dbo].[Action] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [IX_Action_Firm]
    ON [dbo].[Action]([firmID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [UIX_Action_startDate]
    ON [dbo].[Action]([startDate] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Action_DeletedDate]
    ON [dbo].[Action]([deleteDate] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Action_FinishDate]
    ON [dbo].[Action]([finishDate] ASC);


GO
CREATE NONCLUSTERED INDEX [UX_Action_ActionID_isConfirmed_isSpecial]
    ON [dbo].[Action]([actionID] ASC, [isConfirmed] ASC, [isSpecial] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Action_UserID]
    ON [dbo].[Action]([userID] ASC);


GO
CREATE NONCLUSTERED INDEX [Action_isConfirmed]
    ON [dbo].[Action]([isConfirmed] ASC)
    INCLUDE([actionID], [discount], [userID], [isSpecial]);


GO
CREATE NONCLUSTERED INDEX [Action_isSpecial__actionID_startDate_finishDate_userID_modDate_isConfirmed_deleteDate]
    ON [dbo].[Action]([isSpecial] ASC)
    INCLUDE([actionID], [startDate], [finishDate], [userID], [modDate], [isConfirmed], [deleteDate]);


GO
CREATE NONCLUSTERED INDEX [IX_Action_isSpecial_modDate__actionID_startDate_finishDate_userID_isConfirmed_deleteDate]
    ON [dbo].[Action]([isSpecial] ASC, [modDate] ASC)
    INCLUDE([actionID], [startDate], [finishDate], [userID], [isConfirmed], [deleteDate]);

