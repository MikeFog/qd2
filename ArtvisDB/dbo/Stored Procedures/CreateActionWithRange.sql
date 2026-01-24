-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 16.04.2009
-- Description:	Create Action With Range
-- =============================================
CREATE PROCEDURE [dbo].[CreateActionWithRange]
(
	@massmediaString varchar(8000),
	@agencyString varchar(8000),
	@loggedUserId smallint,
	@firmID smallint,
	@paymentTypeID smallint,
	@actionID int output
)
WITH EXECUTE AS OWNER
AS
BEGIN
	set nocount on;

	declare @massmedias table(rowNum [smallint] IDENTITY(1,1) NOT NULL, id smallint)
	insert into @massmedias(id)
	select * from dbo.fn_CreateTableFromString(@massmediaString)

	declare @agencies table(rowNum [smallint] IDENTITY(1,1) NOT NULL, id smallint)
	insert into @agencies(id)
	select * from dbo.fn_CreateTableFromString(@agencyString)

	-- create action 
    INSERT INTO [Action](firmID, userID, isConfirmed)
	VALUES(@firmID, @loggedUserId, 0)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 
	
	SET @actionID = SCOPE_IDENTITY()

	INSERT INTO [Campaign](actionID, campaignTypeID, massmediaID, paymentTypeID, agencyID, modUser)
	select @actionID, 1, mm.id, @paymentTypeID, ag.id, @loggedUserId
	from @massmedias mm inner join @agencies ag on mm.rowNum = ag.rowNum
	
	exec dbo.ActionRecalculate
		@actionID = @actionID, --  int
		@needShow = 0, --  bit
		@loggedUserID = @loggedUserID --  int
END

