CREATE TABLE [dbo].[iMenu] (
    [menuID]          SMALLINT       IDENTITY (1, 1) NOT NULL,
    [name]            NVARCHAR (128) NOT NULL,
    [parentID]        SMALLINT       NULL,
    [position]        TINYINT        NOT NULL,
    [codeName]        VARCHAR (32)   NULL,
    [hotKey]          VARCHAR (30)   NULL,
    [align]           VARCHAR (5)    CONSTRAINT [DF_menu_align] DEFAULT ('Left') NOT NULL,
    [imgResourcePath] VARCHAR (50)   NULL,
    [isPublic]        BIT            CONSTRAINT [DF_menu_isPublic] DEFAULT ((0)) NOT NULL,
    [isObsolete]      BIT            CONSTRAINT [DF_iMenu_isObsolete] DEFAULT ((0)) NOT NULL,
    [name_es]         NVARCHAR (128) NULL,
    CONSTRAINT [PK_menu] PRIMARY KEY CLUSTERED ([menuID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_menu_menu] FOREIGN KEY ([parentID]) REFERENCES [dbo].[iMenu] ([menuID])
);


GO
ALTER TABLE [dbo].[iMenu] SET (LOCK_ESCALATION = AUTO);

