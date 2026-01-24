CREATE TABLE [dbo].[Agency] (
    [agencyID]            SMALLINT             IDENTITY (1, 1) NOT NULL,
    [name]                NVARCHAR (32)        NOT NULL,
    [address]             NVARCHAR (128)       NULL,
    [phone]               VARCHAR (32)         NULL,
    [fax]                 VARCHAR (32)         NULL,
    [account]             VARCHAR (32)         NULL,
    [inn]                 VARCHAR (32)         NULL,
    [okpo]                VARCHAR (32)         NULL,
    [okonh]               VARCHAR (32)         NULL,
    [bankID]              SMALLINT             NULL,
    [director]            VARCHAR (32)         NULL,
    [bookkeeper]          VARCHAR (32)         NULL,
    [prefix]              VARCHAR (64)         NULL,
    [isActive]            BIT                  CONSTRAINT [DF_Agency_isActive] DEFAULT (1) NOT NULL,
    [directorSignature]   VARCHAR (256)        NULL,
    [bookkeeperSignature] VARCHAR (256)        NULL,
    [fullPrefix]          [dbo].[doubleString] NULL,
    [reportString]        [dbo].[doubleString] NULL,
    [registration]        [dbo].[doubleString] NULL,
    [egrn]                VARCHAR (16)         NULL,
    [kpp]                 VARCHAR (16)         NULL,
    [okved]               VARCHAR (16)         NULL,
    [email]               VARCHAR (256)        NULL,
    [painting]            IMAGE                NULL,
    [reportPlace]         VARCHAR (64)         NULL,
    CONSTRAINT [PK_Agency] PRIMARY KEY NONCLUSTERED ([agencyID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Agency_Bank] FOREIGN KEY ([bankID]) REFERENCES [dbo].[Bank] ([bankId]),
    CONSTRAINT [UIX_Agency_name] UNIQUE CLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[Agency] SET (LOCK_ESCALATION = AUTO);

