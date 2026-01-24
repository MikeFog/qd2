CREATE DEFAULT [dbo].[DF_getdate]
    AS getdate();


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_getdate]', @objname = N'[dbo].[LogDeletedIssue].[date]';

