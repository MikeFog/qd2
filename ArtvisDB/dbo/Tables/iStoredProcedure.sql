CREATE TABLE [dbo].[iStoredProcedure] (
    [storedProcedureID]     INT           IDENTITY (1, 1) NOT NULL,
    [name]                  VARCHAR (128) NOT NULL,
    [procedureType]         VARCHAR (32)  CONSTRAINT [DF__storedPro__proce__50FB042B] DEFAULT ('RECORDSET') NOT NULL,
    [isTransactionRequired] BIT           CONSTRAINT [DF_StoredProcedure_isTransactionRequired] DEFAULT (0) NOT NULL,
    [cachingTime]           SMALLINT      CONSTRAINT [DF_iStoredProcedure_cachingTime] DEFAULT ((5)) NOT NULL,
    CONSTRAINT [PK_STOREDPROCEDURES] PRIMARY KEY CLUSTERED ([storedProcedureID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [ck_storedProcedures_procedure_type] CHECK ([procedureType] = 'RECORDSET' or [procedureType] = 'NO_DATA' or [procedureType] = 'XML_DATA')
);


GO
ALTER TABLE [dbo].[iStoredProcedure] SET (LOCK_ESCALATION = AUTO);

