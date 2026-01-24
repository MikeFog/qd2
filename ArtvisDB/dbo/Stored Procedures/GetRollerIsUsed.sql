CREATE PROCEDURE [dbo].[GetRollerIsUsed]
(
	@rollerID INT,
	@isUsed BIT = NULL OUT
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SET @isUsed = 0
		
    IF EXISTS(SELECT * FROM [Issue] WHERE [rollerID] = @rollerID) 
		or exists(select * from ModulePriceList where rollerID = @rollerID) 
		or exists(select * from PackModulePriceList where rollerID = @rollerID)
		SET @isUsed = 1
END

