CREATE PROCEDURE [dbo].[MassmediaIUD]
(
@massmediaID smallint = NULL,
@name nvarchar(32) = NULL,
@prefix nvarchar(32) = NULL,
@roltypeID smallint = NULL,
@deadLine datetime = NULL,
@isActive bit = NULL,
@director varchar(256) = NULL,
@fullPrefix varchar(256) = NULL,
@reportString varchar(256) = NULL,
@actionName varchar(32),
@rollerEnterPath NVARCHAR(255) = NULL,
@rollerExitPath NVARCHAR(255) = NULL,
@rollerEtcPath NVARCHAR(255) = NULL,
@rollerPath NVARCHAR(255) = NULL,
@rollerEnterMax smallint = NULL,
@rollerExitMax smallint = NULL,
@rollerEtcMax smallint = null,
@rollerEnterMin smallint = NULL,
@rollerExitMin smallint = NULL,
@rollerEtcMin smallint = null,
@massmediaGroupID int = null,
@exportName nvarchar(255) = null,
@loggedUserID smallint,
@mediaPlusMassmediaID smallint = null,
@painting image = null,
@certificateIssued varchar(256),
@volume_c decimal(5,2),
@volume_n decimal(5,2),
@volume_p decimal(5,2),
@volume_m decimal(5,2),
@volume_j decimal(5,2)
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

if @mediaPlusMassmediaID = 0 
	set @mediaPlusMassmediaID = null

if @actionName in ('AddItem', 'UpdateItem') and @mediaPlusMassmediaID is not null
begin 
	if exists(select * from MassMedia mm where (@massmediaID is null or @massmediaID <> mm.massmediaID) and mm.mediaPlusMassmediaID = @mediaPlusMassmediaID)
	begin 
		raiserror('MassmediaWithSameMediaPlusIdExist', 16, 1)
		return
	end 
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Massmedia](roltypeID, deadLine, isActive, rollerEnterPath, 
		rollerExitPath, rollerEtcPath, rollerPath, rollerEnterMax, rollerExitMax, rollerEtcMax, rollerEnterMin, rollerExitMin, rollerEtcMin, massmediaGroupID, 
		exportName,mediaPlusMassmediaID, [name], director, painting, prefix, [fullPrefix], [reportString], certificateIssued,
		volume_c, volume_n, volume_p, volume_m, volume_j)
	VALUES(@roltypeID, @deadLine, @isActive, @rollerEnterPath, @rollerExitPath, 
		@rollerEtcPath, @rollerPath, @rollerEnterMax, @rollerExitMax, @rollerEtcMax, @rollerEnterMin, @rollerExitMin, @rollerEtcMin, @massmediaGroupID, 
		@exportName,@mediaPlusMassmediaID, @name, @director, @painting, @prefix, @fullPrefix, @reportString, @certificateIssued,
		@volume_c, @volume_n, @volume_p, @volume_m, @volume_j)
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @massmediaID = SCOPE_IDENTITY()

	--Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID

	insert into UserMassmedia (userID,	massmediaID, canWork, canAdd) 
	values (@loggedUserID, @massmediaID, 1, 1) 

	select * from MassMedia where massmediaID = @massmediaID
END
ELSE IF @actionName = 'DeleteItem' begin
	DELETE FROM [Massmedia] WHERE massmediaID = @massmediaID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Massmedia]
	SET
		[name] = @name,
		roltypeID = @roltypeID, 
		deadLine = @deadLine, 
		isActive = Coalesce(@isActive, isActive),
		rollerEnterPath = @rollerEnterPath, 
		rollerExitPath = @rollerExitPath, 
		rollerEtcPath = @rollerEtcPath, 
		rollerPath = @rollerPath, 
		rollerEnterMax = @rollerEnterMax, 
		rollerExitMax = @rollerExitMax, 
		rollerEtcMax = @rollerEtcMax,
		rollerEnterMin = @rollerEnterMin, 
		rollerExitMin = @rollerExitMin, 
		rollerEtcMin = @rollerEtcMin,
		massmediaGroupID = @massmediaGroupID, 
		exportName = @exportName,
		mediaPlusMassmediaID = @mediaPlusMassmediaID,
		director = @director,
		painting = @painting,
		prefix=@prefix,
		[fullPrefix] = @fullPrefix,
		[reportString] = @reportString,
		certificateIssued = @certificateIssued,
		volume_c = @volume_c, 
		volume_n = @volume_n, 
		volume_p = @volume_p, 
		volume_m = @volume_m, 
		volume_j = @volume_j
	WHERE		
		massmediaID = @massmediaID

	Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID
END
