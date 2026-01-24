


CREATE    PROCEDURE [dbo].[sponsorProgramIUD]
(
@sponsorProgramID smallint OUT,
@name nvarchar(32) = NULL,
@massmediaID smallint = NULL,
@isActive bit = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [SponsorProgram](name, massmediaID, isActive)
	VALUES(@name, @massmediaID, @isActive)	

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @sponsorProgramID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [SponsorProgram] WHERE SponsorProgramID = @SponsorProgramID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	[SponsorProgram]
	SET			name = @name, 
					massmediaID = @massmediaID, 
					isActive = @isActive
	WHERE		SponsorProgramID = @SponsorProgramID







