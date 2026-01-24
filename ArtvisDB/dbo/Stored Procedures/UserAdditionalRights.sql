-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 20.10.2008
-- Description:	
-- =============================================
CREATE procedure [dbo].[UserAdditionalRights] 
(
@userID smallint = 0
)
as 
begin 
	set nocount on;

	declare @isAdmin bit 
	
	set @isAdmin = dbo.f_IsAdmin(@userID)
	
    select distinct 
		ea.entityActionID,
		e.name as entityName,
		IsNull(ea2.alias+ '\','') + ea.[alias] as alias,
		Cast(
			CASE
				WHEN @isAdmin = 0 and ((gRights.isSelected is null and (ur.isGrant is null or ur.isGrant = 0)) 
					or (gRights.isSelected is not null and ur.isGrant = 0)) THEN 0
				ELSE 1
			END as Bit)
		 as isObjectSelected,
		cast(case when ur.isGrant is not null then 1 else 0 end as bit) as isUserRight
	from 
		[iEntityAction] ea
		left join iEntityAction ea2 on ea.parentID = ea2.entityActionID
		inner join iEntity e on e.entityID = ea.entityID
		left join UserAdditionRight ur on ea.entityActionID = ur.entityActionID
			and ur.userID = @userID
		left join 
		(
			select gr.entityActionID, cast(case when count(*) > 0 then 1 else 0 end as bit) as isSelected from 
				GroupRight gr 
				inner join GroupMember gm on gr.groupID = gm.groupID
			where gm.userID = @userID
			group by gr.entityActionID
		) as gRights on ea.entityActionID = gRights.entityActionID
	where
		e.isGrantingAllowed = 1 And
		ea.isGrantingAllowed = 1 And
		not ea.name is null
		And e.[isObsolete] = 0
	order by
		e.name,
		alias

end

