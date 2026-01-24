CREATE   PROC [dbo].[PackModules]
(
@packModuleID smallint = NULL,
@date DATETIME = null,
@loggedUserID smallint = null
)
AS
SET NOCOUNT ON
IF @date IS NULL 
	SELECT 
		pm.[packModuleID], pm.[name], pm.[path]
	FROM 
		[PackModule] pm
	WHERE 
		pm.packModuleID  = Coalesce(@packModuleID, pm.packModuleID)
	ORDER BY 
		[name]
else
begin 

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	SELECT m.*
	FROM 
		[PackModule] m
		INNER JOIN [PackModulePriceList] pl ON m.[packModuleID] = pl.[packModuleID]
	WHERE
		@date >= pl.[startDate] AND
		@date < pl.[finishDate] + 1
		and (select count(*)
			from PackModuleContent pmc 
				inner join Module m on pmc.moduleID = m.moduleID
				inner join @massmedias umm on umm.massmediaID = m.massmediaID
					and umm.myMassmedia = 1
				where pl.priceListID = pmc.pricelistID)
			= (select count(*)
			from PackModuleContent pmc 
				inner join Module m on pmc.moduleID = m.moduleID
				where pl.priceListID = pmc.pricelistID)
	ORDER BY 
		m.[name]
	
end
