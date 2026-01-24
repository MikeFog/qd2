CREATE TABLE [dbo].[Firm] (
    [firmID]        SMALLINT             IDENTITY (1, 1) NOT NULL,
    [name]          [dbo].[doubleString] NOT NULL,
    [address]       [dbo].[doubleString] NULL,
    [phone]         VARCHAR (32)         NULL,
    [fax]           VARCHAR (32)         NULL,
    [account]       VARCHAR (20)         NULL,
    [inn]           VARCHAR (20)         NULL,
    [okonh]         VARCHAR (20)         NULL,
    [okpo]          VARCHAR (20)         NULL,
    [bankID]        SMALLINT             NULL,
    [prefix]        NVARCHAR (16)        NULL,
    [isIdle]        BIT                  CONSTRAINT [DF_Firm_isIdle] DEFAULT ((1)) NOT NULL,
    [egrn]          VARCHAR (16)         NULL,
    [kpp]           VARCHAR (16)         NULL,
    [okved]         VARCHAR (16)         NULL,
    [email]         VARCHAR (256)        NULL,
    [director]      VARCHAR (50)         NULL,
    [reportString]  [dbo].[doubleString] NULL,
    [registration]  [dbo].[doubleString] NULL,
    [headCompanyID] INT                  NULL,
    CONSTRAINT [PK_Firm] PRIMARY KEY NONCLUSTERED ([firmID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Firm_Bank] FOREIGN KEY ([bankID]) REFERENCES [dbo].[Bank] ([bankId])
);


GO
ALTER TABLE [dbo].[Firm] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Firm_name]
    ON [dbo].[Firm]([name] ASC, [inn] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Firm_headCompanyID]
    ON [dbo].[Firm]([headCompanyID] ASC);

