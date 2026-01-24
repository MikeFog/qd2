/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE FUNCTION [dbo].[fn_GetPriceByPeriod1]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime
)
RETURNS @Result TABLE (advertTypeID smallint, price money)
AS
BEGIN
	IF EXISTS(SELECT * 
				FROM [Campaign] c 
				INNER JOIN [Action] a ON c.[actionID] = a.[actionID] 
				WHERE c.[campaignID] = @campaignID AND a.isSpecial = 1
				)
	BEGIN
		INSERT @Result
		SELECT NULL as advertTypeID, c.price
		FROM Campaign c
		WHERE c.campaignID = @campaignID

		RETURN
	END
	
	IF (@startDate IS NULL AND @finishDate IS NULL)
	BEGIN
		INSERT @Result
		SELECT NULL as advertTypeID, 0 AS price

		RETURN
	END

	SET @startDate = dbo.ToShortDate(@startDate)
	SET @finishDate = dbo.ToShortDate(@finishDate) 

	IF	@campaignTypeID = 1 

		INSERT @Result
		SELECT 
			r.advertTypeID, 
			SUM(i.[tariffPrice] * i.[ratio]) AS price
		FROM Issue i
				JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
				JOIN roller r ON r.rollerID = i.rollerID
		WHERE
			i.campaignID = @campaignID
				AND tw.dayOriginal BETWEEN @startDate AND @finishDate
		GROUP BY r.advertTypeID

	ELSE IF	@campaignTypeID = 2	

		INSERT @Result
		SELECT
			i.advertTypeID AS advertTypeID,
			ISNULL(SUM(i.[tariffPrice] * i.[ratio]), 0) AS price
		FROM		
			ProgramIssue i 
				JOIN SponsorTariff st ON i.tariffID = st.tariffID
				JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
		WHERE		
			i.campaignID = @campaignID 
				AND CONVERT(datetime, 
						CONVERT(varchar(8), 
							DATEADD(mi, 
								-DATEPART(mi, pl.broadcastStart), 
								DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)
								), 
							112), 
						112) BETWEEN @startDate AND @finishDate
		GROUP BY i.advertTypeID

	ELSE IF	@campaignTypeID = 3 

		INSERT @Result
		SELECT
			r.advertTypeID, 
			SUM(i.[tariffPrice] * i.[ratio]) AS price
		FROM ModuleIssue i 
			JOIN roller r ON r.rollerID = i.rollerID
		WHERE		
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
		GROUP BY r.advertTypeID

	ELSE 

		INSERT @Result
		SELECT
			r.advertTypeID, 
			SUM(i.[tariffPrice] * i.[ratio]) AS price
		FROM PackModuleIssue i 
			JOIN roller r ON r.rollerID = i.rollerID
		WHERE		
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
		GROUP BY r.advertTypeID

	RETURN
END
