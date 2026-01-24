CREATE TABLE [dbo].[AdvertType] (
    [advertTypeID] SMALLINT             IDENTITY (1, 1) NOT NULL,
    [name]         [dbo].[doubleString] NOT NULL,
    [parentID]     SMALLINT             NULL,
    [isParent]     BIT                  NOT NULL,
    [isHidden]     BIT                  NOT NULL,
    CONSTRAINT [PK_AdvertType] PRIMARY KEY CLUSTERED ([advertTypeID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_AdvertType_AdvertType] FOREIGN KEY ([parentID]) REFERENCES [dbo].[AdvertType] ([advertTypeID])
);


GO
ALTER TABLE [dbo].[AdvertType] SET (LOCK_ESCALATION = AUTO);


GO
CREATE NONCLUSTERED INDEX [UIX_AdvertType_Parent_Name]
    ON [dbo].[AdvertType]([parentID] ASC, [name] ASC);

