CREATE PROCEDURE [dbo].[UserPassport]
(
	@userID int = null 
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @isAdmin bit
	set @isAdmin = dbo.f_IsAdmin(@userID)

    SELECT 
		mm.*,
		Cast(
			CASE
				when @isAdmin = 0 and umm.userID IS NULL THEN 0
				else 1
			END as Bit) AS isObjectSelected,
		rt.[name] as rolTypeName
	FROM 
		vMassMedia mm
		left join iRolType rt on rt.rolTypeID = mm.roltypeID
		left join UserMassmedia umm 
			on mm.massmediaID = umm.massmediaID 
				and umm.userID = @userID and umm.canWork = 1
	where mm.isActive = 1
	ORDER by isObjectSelected desc, mm.[name]
	
	SELECT 
		mm.*,
		Cast(
			CASE
				when @isAdmin = 0 and umm.userID IS NULL THEN 0
				else 1
			END as Bit) AS isObjectSelected,
		rt.[name] as rolTypeName
	FROM 
		vMassMedia mm
		left join iRolType rt on rt.rolTypeID = mm.roltypeID
		left join UserMassmedia umm 
			on mm.massmediaID = umm.massmediaID 
				and umm.userID = @userID and umm.canAdd = 1
	where mm.isActive = 1
	ORDER by isObjectSelected desc, mm.[name]
END
