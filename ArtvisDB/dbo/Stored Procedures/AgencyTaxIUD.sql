
CREATE  PROCEDURE [dbo].[AgencyTaxIUD]
(
@agencyTaxID smallint = NULL,
@agencyID smallint = NULL,
@taxID nchar(10) = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@divisor float = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @actionName IN('AddItem', 'UpdateItem') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('TariffStartFinishDateError', 16, 1)
		RETURN
		END

	IF EXISTS(
		SELECT * FROM agencyTax	
		WHERE 
			(@startDate between startDate and finishDate Or 
			@finishDate between startDate and finishDate) and
			(IsNull(@agencyTaxID, 0) <> agencyTaxID OR @actionName <> 'UpdateItem') and
			agencyID = @agencyID
		) BEGIN
		RAISERROR('TariffPeriodIntersection', 16, 1)
		RETURN
		END
	END

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [agencyTax](agencyID, taxID, startDate, finishDate, divisor)
	VALUES(@agencyID, @taxID, @startDate, @finishDate, @divisor)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @agencyTaxID = SCOPE_IDENTITY()

	Exec AgencyTaxRetrieve @agencyTaxID = @agencyTaxID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [agencyTax] WHERE agencyTaxID = @agencyTaxID
ELSE IF @actionName = 'UpdateItem' Begin
	UPDATE	
		[agencyTax]
	SET
		agencyID = @agencyID, 
		taxID = @taxID, 
		startDate = @startDate, 
		finishDate = @finishDate, 
		divisor = @divisor
	WHERE		
		agencyTaxID = @agencyTaxID

	Exec AgencyTaxRetrieve @agencyTaxID = @agencyTaxID
End



