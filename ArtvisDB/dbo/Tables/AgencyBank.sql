-- Расчётный счёт агентства в конкретном банке (до Deploy/90 был один на агентство — Agency.account).
-- В счетах и документах печатается счёт в текущем банке агентства (Agency.bankID) — sl_Agencies.
CREATE TABLE [dbo].[AgencyBank] (
    [agencyID] SMALLINT     NOT NULL,
    [bankID]   SMALLINT     NOT NULL,
    [account]  VARCHAR (32) NOT NULL,
    CONSTRAINT [PK_AgencyBank] PRIMARY KEY CLUSTERED ([agencyID] ASC, [bankID] ASC),
    CONSTRAINT [FK_AgencyBank_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
    CONSTRAINT [FK_AgencyBank_Bank] FOREIGN KEY ([bankID]) REFERENCES [dbo].[Bank] ([bankId]) ON DELETE CASCADE
);
