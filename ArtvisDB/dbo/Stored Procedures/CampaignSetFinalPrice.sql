CREATE		Procedure [dbo].[CampaignSetFinalPrice]
(
@campaignId int, 
@finalPrice money,
@campaignTypeId TINYINT,
@loggedUserId INT,
@grantorUserId INT = NULL,
@todayDate datetime = null
)
WITH EXECUTE AS OWNER
As
Set NoCount On

Declare
	@fixedPrice money,
	@dayX datetime,
	@theDate datetime,
	@campaignStartDate datetime,
	@packDiscount float,
	@actionId int,
	@issuesPrice money,
	@campaignFinishDate datetime,
	@isAdmin bit

if @todayDate Is Null Set @todayDate = GETDATE()
set @theDate = [dbo].[ToShortDate](@todayDate)

Select	@dayX = convert(datetime, Convert(varchar(6), @todayDate, 112) + '01', 112) - 1
Set		@IsAdmin = dbo.f_IsAdmin(@loggedUserID)

Select
	@campaignStartDate = c.startDate,
	@packDiscount = a.discount,
	@actionId = a.actionId,
	@campaignFinishDate = c.finishDate
From
	Campaign c
	Inner Join [Action] a On a.actionId = c.actionId
Where
	c.campaignId = @campaignId

if /*@isAdmin = 0 And */ @campaignFinishDate <= @theDate
BEGIN
	Raiserror('CantChangeDiscount', 16, 1)
	Return
END

IF @campaignTypeId = 4
	SET @packDiscount = 1.0

Exec GetPriceByPeriod @campaignId, @campaignTypeId, @campaignStartDate,
	@dayX, @fixedPrice	out
	
set @fixedPrice = isnull(@fixedPrice, 0)

If /*@isAdmin = 0 And */ @fixedPrice > @finalPrice Begin
	Raiserror('FinalPriceIsTooLow', 16, 1)
	Return
END

DECLARE @managerDiscount FLOAT 
SELECT @managerDiscount = CAST(@finalPrice AS FLOAT) / ( CAST(price AS FLOAT) * CAST(@packDiscount AS FLOAT))
FROM [Campaign]
WHERE [campaignID] = @campaignId

IF (@grantorUserId IS NULL OR dbo.[fn_IsAcceptRatioForUser](@grantorUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0 ) 
	AND dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
	BEGIN
		RAISERROR('MaxRatioExcess', 16, 1)
		RETURN
	END

Update 
	Campaign
Set
	finalPrice = @finalPrice,
	managerDiscount = @managerDiscount
Where
	campaignID = @campaignId

--Exec ActionRecalculate @actionId, 0, @loggedUserId, @todayDate
