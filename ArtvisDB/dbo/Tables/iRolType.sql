CREATE TABLE [dbo].[iRolType] (
    [rolTypeID]  SMALLINT      IDENTITY (1, 1) NOT NULL,
    [name]       NVARCHAR (50) NOT NULL,
    [isLoadable] BIT           NOT NULL,
    [isActive]   BIT           CONSTRAINT [DF__roltype__isActiv__01B41576] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_RolType] PRIMARY KEY NONCLUSTERED ([rolTypeID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_RolType_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iRolType] SET (LOCK_ESCALATION = AUTO);

