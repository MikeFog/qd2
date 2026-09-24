CREATE TABLE [dbo].[iMonthName] (
    [number] TINYINT      NOT NULL,
    [name]   NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_MonthName] PRIMARY KEY CLUSTERED ([number] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iMonthName] SET (LOCK_ESCALATION = AUTO);

