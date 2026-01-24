CREATE PROC [dbo].[Rollers]
(
@firmID smallint = NULL,
@rollerID int = NULL,
@createDateStart DATETIME = null,
@createDateFinish DATETIME = null,
@rollerName VARCHAR(32) = NULL,
@rollerCheckName VARCHAR(32) = NULL,
@ShowActive BIT = 1,
@ShowInactive BIT = 0,
@isCommonOnly BIT = 0,
@showSimpleRollers BIT = 1,
@withoutID int = null,
@showUsed BIT = 1,
@showUnused BIT = 1,
@advertTypeID smallint = NULL,
@showMuteRollers bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
CREATE TABLE #Roller(rollerID int)

If @rollerID Is Not Null
	INSERT INTO #Roller Values(@rollerID)
Else
	Begin
	if @showUnused != 0 or @showUsed != 0
	begin
		INSERT INTO #Roller
		SELECT 
			r.rollerID 
		FROM 
			[Roller] r
			LEFT JOIN Firm f ON f.firmID = r.firmID
			LEFT JOIN AdvertType advt on advt.advertTypeID = r.advertTypeID
		where
			r.rollerID = Coalesce(@rollerID, r.rollerID) AND
			((@ShowActive = 1 AND r.[isEnabled] = 1) OR (@ShowInactive = 1 AND r.[isEnabled] = 0)) and
			r.[createDate] >= COALESCE(@createDateStart, r.[createDate]) AND
			r.[createDate] <= DATEADD(DAY, 1, COALESCE(@createDateFinish, r.[createDate])) and
			r.NAME LIKE ISNULL(@rollerName, '%') + '%' AND 
			r.NAME LIKE '%' + ISNULL(@rollerCheckName, '%') + '%'
			And (@firmID IS NULL OR (r.[firmID] = @firmID))
			AND (@isCommonOnly = 0 OR r.[isCommon] = 1)
			AND (@showSimpleRollers = 1 OR r.[rolActionTypeID] <> 1)
			and (@withoutID is null or (r.rollerID <> @withoutID))
			And (@advertTypeID IS NULL OR (r.advertTypeID = @advertTypeID OR advt.parentID = @advertTypeID))
			And r.parentID Is Null
		ORDER BY 
			r.name
	end

	if @showUsed = 1 and @showUnused = 0
		begin
			delete from r
			from #Roller r
				left join (
					select distinct i.rollerId from Issue i where i.rollerID is not null
					union 
					select distinct mi.rollerId from ModuleIssue mi where mi.rollerID is not null
					union 
					select distinct mpl.rollerId from ModulePriceList mpl where mpl.rollerID is not null
					union 
					select distinct pmi.rollerId from PackModuleIssue pmi where pmi.rollerID is not null
					union 
					select distinct pmpl.rollerId from PackModulePriceList pmpl where pmpl.rollerID is not null
				) as x on r.rollerID = x.rollerID
			where x.rollerID is null 
		end
	else if @showUsed = 0 and @showUnused = 1
		begin
			delete from r
			from #Roller r
				left join (
					select distinct i.rollerId from Issue i where i.rollerID is not null
					union 
					select distinct mi.rollerId from ModuleIssue mi where mi.rollerID is not null
					union 
					select distinct mpl.rollerId from ModulePriceList mpl where mpl.rollerID is not null
					union 
					select distinct pmi.rollerId from PackModuleIssue pmi where pmi.rollerID is not null
					union 
					select distinct pmpl.rollerId from PackModulePriceList pmpl where pmpl.rollerID is not null
				) as x on r.rollerID = x.rollerID
			where x.rollerID is not null
		end
	End
/*
declare @showMuteRollers bit 
set @showMuteRollers = case when @rollerID is not null then 1 else 0 end 
*/
EXEC sl_Rollers @showMuteRollers = @showMuteRollers
