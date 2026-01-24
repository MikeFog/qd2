
CREATE PROCEDURE [dbo].[GetPackModuleIssueDetails]
(
	@windowDate DATETIME,
	@packModuleId INT,
	@showUnconfirmed BIT = 0
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @res table (c1 nvarchar(255), c8 nvarchar(64), c2 nvarchar(10), c3 int, c4 int, c5 bit, c6 bit, c7 bit)
	insert into @res 
	SELECT
		mm.[name],
		mm.groupName,
		dbo.[fn_Int2Time](MIN(tw.[duration])),
		MIN(tw.[maxCapacity]),
		CASE @showUnconfirmed
			WHEN 0 THEN MIN(tw.[maxCapacity] - tw.[capacityInUseConfirmed])
			ELSE MIN(tw.[maxCapacity] - tw.[capacityInUseUnConfirmed] - tw.[capacityInUseConfirmed])
		END,
		CAST(CASE
			WHEN (@showUnconfirmed = 1 AND MAX(CAST([firstPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST(isFirstPositionOccupied AS INT)) = 1 THEN 1
			ELSE 0
		END AS BIT), 
		CAST(CASE
			WHEN (@showUnconfirmed = 1 AND MAX(CAST([secondPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST([isSecondPositionOccupied] AS INT)) = 1 THEN 1
			ELSE 0
		END AS BIT),
		CAST(CASE
			WHEN (@showUnconfirmed = 1 AND MAX(CAST([lastPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST(isLastPositionOccupied AS INT)) = 1 THEN 1
			ELSE 0
		END AS BIT)
	FROM 
		[PackModuleContent]  pmc
		INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
		INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
		INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
		INNER JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID] 
		inner join Tariff t on tw.tariffID  = t.tariffID 
		inner join Pricelist pl on pl.pricelistID = t.priceListID
	WHERE 
		pmpl.[packModuleID] = @packModuleId
		AND tw.maxCapacity > 0
		AND tw.dayOriginal = @windowDate
	GROUP BY mm.[name], mm.groupName
	
	if exists(select * from @res)
		SELECT
			c1 AS 'Радиостанция',
			c8 AS 'Группа',
			c2 AS 'Продолжительность',
			c3 AS 'Максимальное Количество',
			c4 'Оставшееся количество',
			c5 AS 'Первый', 
			c6 AS 'Второй',
			c7 as 'Последний'
		FROM @res
		ORDER BY c1
	else
		SELECT
			mm.[name] AS 'Радиостанция',
			mm.groupName as 'Группа',
			dbo.[fn_Int2Time](MIN(tw.[duration])) AS 'Продолжительность',
			dbo.[fn_Int2Time](CASE @showUnconfirmed
				WHEN 0 THEN MIN(tw.duration - tw.timeInUseConfirmed)
				ELSE MIN(tw.duration - tw.timeInUseUnconfirmed - tw.timeInUseConfirmed)
			END) AS 'Остаток',
			CAST(CASE
				WHEN (@showUnconfirmed = 1 AND MAX(CAST([firstPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST(isFirstPositionOccupied AS INT)) = 1 THEN 1
				ELSE 0
			END AS BIT) AS 'Первый', 
			CAST(CASE
				WHEN (@showUnconfirmed = 1 AND MAX(CAST([secondPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST([isSecondPositionOccupied] AS INT)) = 1 THEN 1
				ELSE 0
			END AS BIT) AS 'Второй',
			CAST(CASE
				WHEN (@showUnconfirmed = 1 AND MAX(CAST([lastPositionsUnconfirmed] AS INT)) = 1) OR MAX(CAST(isLastPositionOccupied AS INT)) = 1 THEN 1
				ELSE 0
			END AS BIT) as 'Последний'
		FROM 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
			INNER JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID] 
			inner join Tariff t on tw.tariffID  = t.tariffID 
			inner join Pricelist pl on pl.pricelistID = t.priceListID
		WHERE 
			pmpl.[packModuleID] = @packModuleId
			and tw.dayOriginal = @windowDate
		GROUP BY mm.[name], mm.groupName
		ORDER BY mm.[name]
END

