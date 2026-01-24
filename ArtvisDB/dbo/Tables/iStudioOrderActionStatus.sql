CREATE TABLE [dbo].[iStudioOrderActionStatus] (
    [statusID] INT           IDENTITY (1, 1) NOT NULL,
    [name]     NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_StudioOrderActionStatus] PRIMARY KEY CLUSTERED ([statusID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iStudioOrderActionStatus] SET (LOCK_ESCALATION = AUTO);

