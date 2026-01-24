CREATE TABLE [dbo].[iRollerActionType] (
    [rolActionTypeId] TINYINT       IDENTITY (1, 1) NOT NULL,
    [name]            NVARCHAR (32) NOT NULL
);


GO
ALTER TABLE [dbo].[iRollerActionType] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [PK_RollerActionType]
    ON [dbo].[iRollerActionType]([rolActionTypeId] ASC) WITH (FILLFACTOR = 90);

