CREATE TYPE [dbo].[pc_CalcRow] AS TABLE (
    [massmediaID] SMALLINT        NOT NULL PRIMARY KEY,
    [tariffPrice] DECIMAL (18, 2) NOT NULL,
    [durationSec] INT             NOT NULL,
    [isSelected]  BIT             NOT NULL);
