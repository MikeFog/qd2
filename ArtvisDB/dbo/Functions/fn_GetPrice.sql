CREATE FUNCTION [dbo].[fn_GetPrice]
(
@startDate datetime, 
@finishDate datetime
)
RETURNS TABLE
AS
RETURN (
	SELECT -- Специальные акции
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		m.massmediaGroupID,
		NULL AS advertTypeID,
		c.price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN MassMedia m on m.massmediaID = c.massmediaID
	WHERE a.isSpecial = 1 
			AND (c.startDate BETWEEN @startDate AND @finishDate OR c.finishDate BETWEEN @startDate AND @finishDate)
	UNION ALL
	SELECT -- 1 - Линейная
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		m.massmediaGroupID,
		r.advertTypeID, 
		i.[tariffPrice] * i.[ratio] AS price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN MassMedia m ON m.massmediaID = c.massmediaID
		JOIN Issue i ON i.campaignID = c.campaignID
		JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
		JOIN roller r ON r.rollerID = i.rollerID
	WHERE a.isSpecial = 0 AND c.campaignTypeID = 1
			AND tw.dayOriginal BETWEEN @startDate AND @finishDate
	UNION ALL
	SELECT -- 2 - Спонсорская
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		m.massmediaGroupID,
		i.advertTypeID, 
		i.[tariffPrice] * i.[ratio] AS price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN MassMedia m ON m.massmediaID = c.massmediaID
		JOIN ProgramIssue i ON i.campaignID = c.campaignID
		JOIN SponsorTariff st ON i.tariffID = st.tariffID
		JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
	WHERE a.isSpecial = 0 AND c.campaignTypeID = 2
			AND CONVERT(datetime, 
					CONVERT(varchar(8), 
						DATEADD(mi, 
							-DATEPART(mi, pl.broadcastStart), 
							DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)
							), 
						112), 
					112) BETWEEN @startDate AND @finishDate
	UNION ALL
	SELECT -- 3 - Модульная
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		m.massmediaGroupID,
		r.advertTypeID, 
		i.[tariffPrice] * i.[ratio] AS price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN MassMedia m ON m.massmediaID = c.massmediaID
		JOIN ModuleIssue i ON i.campaignID = c.campaignID
		JOIN roller r ON r.rollerID = i.rollerID
	WHERE a.isSpecial = 0 AND c.campaignTypeID = 3
			AND i.issueDate BETWEEN @startDate AND @finishDate
	UNION ALL
	SELECT -- 4 - Пакетная (без Massmedia)
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		NULL,
		r.advertTypeID, 
		i.[tariffPrice] * i.[ratio] AS price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN PackModuleIssue i ON i.campaignID = c.campaignID
		JOIN roller r ON r.rollerID = i.rollerID
		LEFT JOIN MassMedia m ON m.massmediaID = c.massmediaID
	WHERE a.isSpecial = 0 AND c.campaignTypeID = 4
			AND i.issueDate BETWEEN @startDate AND @finishDate
			AND m.massmediaID IS NULL
	UNION ALL
	SELECT -- 4 - Пакетная
		c.campaignID, 
		c.actionID, 
		c.massmediaID, 
		c.PaymentTypeID, 
		c.campaignTypeID, 
		c.AgencyID,
		c.startDate,
		c.finishDate,
		c.finalPrice,
		a.userID,
		a.firmID,
		a.discount,
		m.massmediaGroupID,
		r.advertTypeID, 
		i.[tariffPrice] * i.[ratio] AS price
	FROM	
		Campaign c
		JOIN Action a ON c.ActionID = a.actionID AND a.isConfirmed = 1
		JOIN PaymentType ON c.PaymentTypeID = PaymentType.PaymentTypeID
		JOIN MassMedia m ON m.massmediaID = c.massmediaID
		JOIN PackModuleIssue i ON i.campaignID = c.campaignID
		JOIN roller r ON r.rollerID = i.rollerID
	WHERE a.isSpecial = 0 AND c.campaignTypeID = 4
			AND i.issueDate BETWEEN @startDate AND @finishDate
	)
