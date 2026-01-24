CREATE TABLE [dbo].[Roller] (
    [rollerID]          INT                  IDENTITY (1, 1) NOT NULL,
    [name]              NVARCHAR (64)        NOT NULL,
    [duration]          [dbo].[timeDuration] NOT NULL,
    [firmID]            SMALLINT             NULL,
    [rolTypeID]         SMALLINT             NULL,
    [rolStyleID]        SMALLINT             NULL,
    [path]              NVARCHAR (1024)      NULL,
    [createDate]        DATETIME             CONSTRAINT [DF_Roller_createDate] DEFAULT (getdate()) NOT NULL,
    [isEnabled]         BIT                  CONSTRAINT [DF_Roller_isEnabled] DEFAULT (1) NOT NULL,
    [rolActionTypeID]   TINYINT              CONSTRAINT [DF_Roller_rolActionTypeID] DEFAULT (1) NOT NULL,
    [isCommon]          BIT                  NOT NULL,
    [isMute]            BIT                  CONSTRAINT [DF_Roller_isMute] DEFAULT ((0)) NOT NULL,
    [compositionName]   NVARCHAR (512)       NULL,
    [compositionAuthor] NVARCHAR (512)       NULL,
    [advertTypeID]      SMALLINT             NULL,
    [parentID]          INT                  NULL,
    CONSTRAINT [PK_Roller] PRIMARY KEY NONCLUSTERED ([rollerID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Roller_AdvertType] FOREIGN KEY ([advertTypeID]) REFERENCES [dbo].[AdvertType] ([advertTypeID]),
    CONSTRAINT [FK_Roller_Firm] FOREIGN KEY ([firmID]) REFERENCES [dbo].[Firm] ([firmID]),
    CONSTRAINT [FK_Roller_Roller] FOREIGN KEY ([parentID]) REFERENCES [dbo].[Roller] ([rollerID]),
    CONSTRAINT [FK_Roller_RollerActionType] FOREIGN KEY ([rolActionTypeID]) REFERENCES [dbo].[iRollerActionType] ([rolActionTypeId])
);


GO
ALTER TABLE [dbo].[Roller] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [IX_Roller_FirmID]
    ON [dbo].[Roller]([firmID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Roller_name]
    ON [dbo].[Roller]([rollerID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Roller_Parent_AdvertType]
    ON [dbo].[Roller]([parentID] ASC, [advertTypeID] ASC);

