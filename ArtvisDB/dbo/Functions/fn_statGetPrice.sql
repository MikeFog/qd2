CREATE FUNCTION [dbo].[fn_statGetPrice]
(
@startDate datetime, 
@finishDate datetime,
@loggedUserID smallint
)
RETURNS @Result TABLE (
			campaignID int, 
			advertTypeID smallint,
			actionID int, 
			massmediaID smallint, 
			paymentTypeID smallint, 
			campaignTypeID tinyint, 
			agencyID smallint,
			startDate datetime,
			finishDate datetime,
			finalPrice money,
			userID smallint,
			firmID smallint,
			discount float,
			massmediaGroupID int,
			price money,
			INDEX i1 UNIQUE CLUSTERED (campaignID, massmediaID, advertTypeID)
			)
AS
BEGIN

	DECLARE @moduleMassmediaPrice TABLE (campaignID int, massmediaId smallint, moduleMassmediaPrice money, PRIMARY KEY (campaignID, massmediaId))
	DECLARE @modulePrice TABLE (campaignID int primary key, modulePrice money)

	INSERT @moduleMassmediaPrice
	SELECT 
		i.campaignID, 
		m.massmediaID,
		SUM(mpl.price) as moduleMassmediaPrice
	FROM PackModuleIssue i
		JOIN Campaign c ON c.campaignID = i.campaignID
		JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
		JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
		JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
	WHERE c.campaignTypeID=4
			AND i.issueDate between @startDate and @finishDate
	GROUP BY i.campaignID, m.massmediaID

	INSERT @modulePrice
	SELECT campaignID,
			SUM(moduleMassmediaPrice) as modulePrice
	FROM @moduleMassmediaPrice
	GROUP BY campaignID


	INSERT @Result
	SELECT a.*
	FROM (
		SELECT -- Специальные акции
			c.campaignID, 
			NULL AS advertTypeID,
			c.actionID, 
			c.massmediaID, 
			c.paymentTypeID, 
			c.campaignTypeID, 
			c.agencyID,
			c.startDate,
			c.finishDate,
			c.finalPrice,
			a.userID,
			a.firmID,
			a.discount,
			m.massmediaGroupID,
			c.price
		FROM	
			Campaign c
			JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
			JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
			JOIN MassMedia m on m.massmediaID = c.massmediaID
		WHERE a.isSpecial = 1 
				AND c.startDate <= @finishDate 
				AND c.finishDate >= @startDate
		UNION ALL
		SELECT -- 1 - Линейная
			c.campaignID, 
			r.advertTypeID, 
			MAX(c.actionID) AS actionID, 
			MAX(c.massmediaID) AS massmediaID, 
			MAX(c.paymentTypeID) AS paymentTypeID, 
			MAX(c.campaignTypeID) AS campaignTypeID, 
			MAX(c.agencyID) AS agencyID,
			MAX(c.startDate) AS startDate,
			MAX(c.finishDate) AS finishDate,
			MAX(c.finalPrice) AS finalPrice,
			MAX(a.userID) AS userID,
			MAX(a.firmID) AS firmID,
			MAX(a.discount) AS discount,
			MAX(m.massmediaGroupID) AS massmediaGroupID,
			CAST(ROUND(SUM(i.[tariffPrice] * i.[ratio]),2) AS money) AS price
		FROM	
			Campaign c
			JOIN Action a ON a.ActionID = c.actionID AND a.isConfirmed = 1
			JOIN MassMedia m ON m.massmediaID = c.massmediaID
			JOIN Issue i ON i.campaignID = c.campaignID
			JOIN roller r ON r.rollerID = i.rollerID
		WHERE a.isSpecial = 0 AND c.campaignTypeID = 1
				AND c.startDate <= @finishDate 
				AND c.finishDate >= @startDate
				AND EXISTS(
						SELECT 1
						FROM TariffWindow tw 
						WHERE tw.windowId = i.originalWindowID
							AND tw.dayOriginal BETWEEN @startDate AND @finishDate
						)
		GROUP BY c.campaignID, r.advertTypeID
		UNION ALL
		SELECT -- 2 - Спонсорская
			c.campaignID, 
			i.advertTypeID, 
			MAX(c.actionID) AS actionID, 
			MAX(c.massmediaID) AS massmediaID, 
			MAX(c.paymentTypeID) AS paymentTypeID, 
			MAX(c.campaignTypeID) AS campaignTypeID, 
			MAX(c.agencyID) AS agencyID,
			MAX(c.startDate) AS startDate,
			MAX(c.finishDate) AS finishDate,
			MAX(c.finalPrice) AS finalPrice,
			MAX(a.userID) AS userID,
			MAX(a.firmID) AS firmID,
			MAX(a.discount) AS discount,
			MAX(m.massmediaGroupID) AS massmediaGroupID,
			CAST(ROUND(SUM(i.[tariffPrice] * i.[ratio]),2) AS money) AS price
		FROM	
			Campaign c
			JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
			JOIN MassMedia m ON m.massmediaID = c.massmediaID
			JOIN ProgramIssue i ON i.campaignID = c.campaignID
		WHERE a.isSpecial = 0 AND c.campaignTypeID = 2
				AND c.startDate <= @finishDate 
				AND c.finishDate >= @startDate
				AND EXISTS (
						SELECT 1
						FROM SponsorTariff st
							JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
						WHERE i.tariffID = st.tariffID
							AND CONVERT(datetime, 
									CONVERT(varchar(8), 
										DATEADD(mi, 
											-DATEPART(mi, pl.broadcastStart), 
											DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)
											), 
										112), 
									112) BETWEEN @startDate AND @finishDate
							)
		GROUP BY c.campaignID, i.advertTypeID
		UNION ALL
		SELECT -- 3 - Модульная
			c.campaignID, 
			r.advertTypeID, 
			MAX(c.actionID) AS actionID, 
			MAX(c.massmediaID) AS massmediaID, 
			MAX(c.paymentTypeID) AS paymentTypeID, 
			MAX(c.campaignTypeID) AS campaignTypeID, 
			MAX(c.agencyID) AS agencyID,
			MAX(c.startDate) AS startDate,
			MAX(c.finishDate) AS finishDate,
			MAX(c.finalPrice) AS finalPrice,
			MAX(a.userID) AS userID,
			MAX(a.firmID) AS firmID,
			MAX(a.discount) AS discount,
			MAX(m.massmediaGroupID) AS massmediaGroupID,
			CAST(ROUND(SUM(i.[tariffPrice] * i.[ratio]),2) AS money) AS price
		FROM	
			Campaign c
			JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
			JOIN MassMedia m ON m.massmediaID = c.massmediaID
			JOIN ModuleIssue i ON i.campaignID = c.campaignID
			JOIN roller r ON r.rollerID = i.rollerID
		WHERE a.isSpecial = 0 AND c.campaignTypeID = 3
				AND c.startDate <= @finishDate 
				AND c.finishDate >= @startDate
				AND i.issueDate BETWEEN @startDate AND @finishDate
		GROUP BY c.campaignID, r.advertTypeID
		UNION ALL
		SELECT -- 4 - Пакетная
			c.campaignID, 
			r.advertTypeID, 
			MAX(c.actionID) AS actionID, 
			mmp.massmediaID AS massmediaID, 
			MAX(c.paymentTypeID) AS paymentTypeID, 
			MAX(c.campaignTypeID) AS campaignTypeID, 
			MAX(c.agencyID) AS agencyID,
			MAX(c.startDate) AS startDate,
			MAX(c.finishDate) AS finishDate,
			MAX(c.finalPrice) AS finalPrice,
			MAX(a.userID) AS userID,
			MAX(a.firmID) AS firmID,
			MAX(a.discount) AS discount,
			MAX(m.massmediaGroupID) AS massmediaGroupID,
			CAST(ROUND(SUM(i.tariffPrice * i.ratio * mmp.moduleMassmediaPrice / mp.modulePrice),2) AS money) AS price
		FROM	
			Campaign c
			JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
			JOIN PackModuleIssue i ON i.campaignID = c.campaignID
			JOIN roller r ON r.rollerID = i.rollerID
			JOIN @moduleMassmediaPrice mmp ON mmp.campaignID = c.campaignID
			JOIN @modulePrice mp ON mp.campaignID = c.campaignID
			JOIN MassMedia m ON m.massmediaID = mmp.massmediaID
		WHERE a.isSpecial = 0 AND c.campaignTypeID = 4
				AND c.startDate <= @finishDate 
				AND c.finishDate >= @startDate
				AND i.issueDate BETWEEN @startDate AND @finishDate
		GROUP BY c.campaignID, mmp.massmediaID, r.advertTypeID
		) a
	WHERE EXISTS (
			SELECT 1
			FROM fn_GetMassmediasForUser(@loggedUserID) b 
			WHERE b.massmediaID = a.massmediaID 
					AND ((a.userID = @loggedUserID AND b.myMassmedia = 1) OR (a.userID <> @loggedUserID and b.foreignMassmedia = 1))
			)
		AND (a.userID = @loggedUserID 
				OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1 
				OR (
					dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1 
					AND EXISTS (
						SELECT 1 
						FROM GroupMember gm 
							JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
						WHERE a.userID = gm.userID
						)
					)
				)

	RETURN
END
