/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE FUNCTION [dbo].[fn_GetPriceByPeriod]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime, 
@massmediaID INT = NULL,
@showBlack bit = 1,
@withTax bit = 0,
@rollerIDString VARCHAR(8000) = null
)
RETURNS @Result TABLE (advertTypeID smallint, price money, tariffPrice money, taxPrice money)
AS
BEGIN
	IF EXISTS(SELECT * 
				FROM [Campaign] c 
				INNER JOIN [Action] a ON c.[actionID] = a.[actionID] 
				inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
											and (@showBlack = 1 or pt.IsHidden = 0)
				WHERE c.[campaignID] = @campaignID AND a.isSpecial = 1
				)
	BEGIN
		INSERT @Result
		SELECT NULL as advertTypeID, c.price, c.tariffPrice, NULL AS taxPrice 
		FROM Campaign c
		WHERE c.campaignID = @campaignID

		RETURN
	END
	
	IF (@startDate IS NULL AND @finishDate IS NULL)
	BEGIN
		INSERT @Result
		SELECT NULL as advertTypeID, 0 AS price, NULL AS tariffPrice, NULL AS taxPrice 

		RETURN
	END

	SET @startDate = dbo.ToShortDate(@startDate)
	SET @finishDate = dbo.ToShortDate(@finishDate) 

	IF	@campaignTypeID = 1 

		INSERT @Result
		SELECT 
			r.advertTypeID, 
			SUM(i.[tariffPrice] * i.[ratio]) AS price, 
			SUM(i.[tariffPrice]) AS tariffPrice, 
			CASE
				WHEN @withTax = 1
				THEN SUM(
						CASE 
							WHEN at.divisor IS NULL 
							THEN 0 
							ELSE ((i.[tariffPrice] * i.[ratio])/at.divisor) 
						END
						) 
			END AS taxPrice 
		FROM Issue i
				JOIN Campaign c ON i.campaignID = c.campaignID
				JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
				JOIN PaymentType pt ON pt.PaymenttypeId = c.paymenttypeId
				JOIN roller r ON r.rollerID = i.rollerID
				LEFT JOIN fn_CreateTableFromString(@rollerIDString) rr ON i.rollerID = rr.ID
				LEFT JOIN dbo.AgencyTax at ON  @withTax = 1
												AND c.agencyID = at.agencyID
												AND tw.dayOriginal BETWEEN at.startDate AND at.finishDate
		WHERE
			i.campaignID = @campaignID
				AND tw.dayOriginal BETWEEN @startDate AND @finishDate
				AND (@rollerIDString IS NULL OR rr.ID IS NOT NULL)
				AND (@showBlack = 1 OR pt.IsHidden = 0)
		GROUP BY r.advertTypeID

	ELSE IF	@campaignTypeID = 2	

		INSERT @Result
		SELECT
			i.advertTypeID AS advertTypeID,
			ISNULL(SUM(i.[tariffPrice] * i.[ratio]), 0) AS price, 
			ISNULL(SUM(i.[tariffPrice]), 0) AS tarifPrice,
			CASE
				WHEN @withTax = 1
				THEN SUM(
						CASE 
							WHEN at.divisor IS NULL 
							THEN 0 
							ELSE ((i.[tariffPrice] * i.[ratio])/at.divisor) 
						END
						) 
			END AS taxPrice 
		FROM		
			ProgramIssue i 
				JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
				JOIN PaymentType pt ON pt.PaymenttypeId = c.paymenttypeId
				JOIN SponsorTariff st ON i.tariffID = st.tariffID
				JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
				LEFT JOIN dbo.AgencyTax at ON  @withTax = 1
												AND c.agencyID = at.agencyID
												AND CONVERT(datetime, 
														CONVERT(varchar(8), 
															DATEADD(mi, 
																	-DATEPART(mi, pl.broadcastStart), 
																	DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)
																	),
																112),
															112) between at.startDate and at.finishDate
		WHERE		
			i.campaignID = @campaignID 
				AND (@showBlack = 1 or pt.IsHidden = 0)
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
			SUM(i.[tariffPrice] * i.[ratio]) AS price, 
			SUM(i.[tariffPrice]) AS tariffPrice, 
			CASE
				WHEN @withTax = 1
				THEN SUM(
						CASE 
							WHEN at.divisor IS NULL 
							THEN 0 
							ELSE ((i.[tariffPrice] * i.[ratio])/at.divisor) 
						END
						) 
			END AS taxPrice 
		FROM ModuleIssue i 
			JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			JOIN PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			JOIN roller r ON r.rollerID = i.rollerID
			LEFT JOIN fn_CreateTableFromString(@rollerIDString) rr ON i.rollerID = rr.ID
			LEFT JOIN dbo.AgencyTax at ON  @withTax = 1
											AND c.agencyID = at.agencyID
											AND i.issueDate BETWEEN at.startDate AND at.finishDate
		WHERE		
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
			AND (@rollerIDString IS NULL OR rr.ID IS NOT NULL)
			AND (@showBlack = 1 OR pt.IsHidden = 0)
		GROUP BY r.advertTypeID

	ELSE IF @massmediaID IS NULL

		INSERT @Result
		SELECT
			r.advertTypeID, 
			SUM(i.[tariffPrice] * i.[ratio]) AS price, 
			SUM(i.[tariffPrice]) AS tariffPrice, 
			CASE
				WHEN @withTax = 1
				THEN SUM(
						CASE 
							WHEN COALESCE(at.divisor, 0) < 0.0000001 
							THEN 0 
							ELSE ((i.[tariffPrice] * i.[ratio])/at.divisor) 
						END
						) 
			END AS taxPrice 
		FROM PackModuleIssue i 
			JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			JOIN PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			JOIN roller r ON r.rollerID = i.rollerID
			LEFT JOIN fn_CreateTableFromString(@rollerIDString) rr ON i.rollerID = rr.ID
			LEFT JOIN dbo.AgencyTax at ON  @withTax = 1
											AND c.agencyID = at.agencyID
											AND i.issueDate BETWEEN at.startDate AND at.finishDate
		WHERE		
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
			AND (@rollerIDString IS NULL OR rr.ID IS NOT NULL)
			AND (@showBlack = 1 OR pt.IsHidden = 0)
		GROUP BY r.advertTypeID

	ELSE

		DECLARE @packModulePrice MONEY 
		DECLARE @tariffPricePM MONEY

		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice])
		FROM PackModuleIssue i 
			JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			JOIN PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			JOIN roller r ON r.rollerID = i.rollerID
			LEFT JOIN fn_CreateTableFromString(@rollerIDString) rr ON i.rollerID = rr.ID
		WHERE		
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
			AND (@rollerIDString IS NULL OR rr.ID IS NOT NULL)
			AND (@showBlack = 1 OR pt.IsHidden = 0)

		INSERT @Result
		SELECT
			r.advertTypeID, 
			@packModulePrice * SUM(CASE WHEN m.massmediaID = @massmediaID THEN mpl.price ELSE 0 END) / SUM(mpl.price) AS price, 
			@tariffPricePM * SUM(CASE WHEN m.massmediaID = @massmediaID THEN mpl.price ELSE 0 END) / SUM(mpl.price)  AS tariffPrice, 
			NULL AS taxPrice 
		FROM [PackModuleIssue] i 
			JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			JOIN PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			JOIN roller r ON r.rollerID = i.rollerID
			JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			LEFT JOIN fn_CreateTableFromString(@rollerIDString) rr ON i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	
			AND	i.issueDate between @startDate and @finishDate 
			AND (@rollerIDString IS NULL OR rr.ID IS NOT NULL)
			AND (@showBlack = 1 OR pt.IsHidden = 0)
		GROUP BY r.advertTypeID

	RETURN
END
