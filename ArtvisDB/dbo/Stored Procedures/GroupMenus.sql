CREATE PROC [dbo].[GroupMenus]
(
@groupID smallint
)
as
set nocount on
SELECT @groupID as groupID, m.[menuID], 'tick_circle.png' as img
from
	[iMenu] m 
	left join [GroupMenu] gm on m.menuID = gm.menuID and gm.groupID = @groupID
	left join [Group] g on @groupID = g.groupID 
WHERE gm.groupID is not null and m.isObsolete=0
