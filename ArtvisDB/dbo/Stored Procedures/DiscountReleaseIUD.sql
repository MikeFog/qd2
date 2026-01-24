


CREATE    PROCEDURE [dbo].[DiscountReleaseIUD]
(
@discountReleaseID smallint = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on
DECLARE 
	@Id int,
	@date datetime

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountRelease](massmediaID, startDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @isForType1, @isForType2, @isForType3)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	-- Set finish date for previous discount release
	SELECT TOP 1 
		@Id = discountReleaseID		
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	< @startDate
	ORDER BY 
		startDate DESC

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @Id
	SET @id = NULL 
	-- May be this discount release has finishDate 
	SELECT TOP 1 
		@Id = discountReleaseID,
		@date = startDate
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	> @startDate
	ORDER BY 
		startDate 

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @DiscountReleaseID


	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	SELECT @date = finishDate	FROM DiscountRelease
	WHERE	DiscountReleaseID = @DiscountReleaseID

	DELETE FROM [DiscountRelease] WHERE DiscountReleaseID = @DiscountReleaseID

	UPDATE DiscountRelease SET finishDate = @date 
	WHERE	massmediaID = @massmediaID AND finishDate = @startDate	
	
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[DiscountRelease]
	SET			
		startDate = @startDate,
		isForType1 = @isForType1,
		isForType2 = @isForType2,
		isForType3 = @isForType3
	WHERE		
		discountReleaseID = @discountReleaseID

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID

END






