
CREATE  PROC [dbo].[MenuItems]
(
@menuID smallint = NULL
)
as
SET NOCOUNT on
SELECT 
	[iMenu].*,
	cast(case when exists(select * from [iMenu] mchild where mchild.parentID = iMenu.menuID) then 1 else 0 end as bit)  as isHasChilds
FROM 
	[iMenu]
where
	[iMenu].isObsolete = 0 and
	[iMenu].isPublic = 0 and (
	[iMenu].parentID = @menuID OR 
	([iMenu].parentID IS Null AND @menuID IS Null))
ORDER BY
	[iMenu].position

