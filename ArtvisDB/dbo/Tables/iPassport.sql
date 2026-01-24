CREATE TABLE [dbo].[iPassport] (
    [codeName] VARCHAR (32)   NOT NULL,
    [passport] VARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_Passport] PRIMARY KEY CLUSTERED ([codeName] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iPassport] SET (LOCK_ESCALATION = AUTO);

