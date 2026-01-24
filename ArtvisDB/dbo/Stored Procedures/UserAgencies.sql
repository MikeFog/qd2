create Proc [dbo].[UserAgencies]
(
@loggedUserId int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON


declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

CREATE TABLE #agency (agencyID smallint)
Insert Into #agency
Select distinct am.agencyID
	from @massmedias m
	Inner Join AgencyMassmedia am On am.massmediaID = m.massmediaID and m.myMassmedia = 1
	Inner Join Agency a On a.agencyID = am.agencyID
Where
	a.isActive = 1

EXEC sl_agencies