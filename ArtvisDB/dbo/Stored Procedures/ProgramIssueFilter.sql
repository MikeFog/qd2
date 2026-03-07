

CREATE   Proc [dbo].[ProgramIssueFilter]
(
@massmediaID smallint
)
WITH EXECUTE AS OWNER
as
set nocount on 

-- sponsorProgram
Exec dbo.SponsorPrograms @massmediaID

-- firm
Exec dbo.Firms

-- user
Exec Users
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[ProgramIssueFilter] TO PUBLIC
    AS [dbo];

