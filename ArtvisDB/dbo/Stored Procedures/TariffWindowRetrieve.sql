CREATE PROC [dbo].[TariffWindowRetrieve]
(
--@advertTypeId smallint,
@pricelistId smallint = null,
@broadcastStart datetime = null,
@startDate datetime = null,
@finishDate datetime = null,
@moduleId smallint = null,
@windowId int = null,
@actualDate datetime = NULL,
@windowDateActual DATETIME = NULL,
@windowDateOriginal DATETIME = NULL,
@excludeSpecialWindows BIT = 0,
@excludeModuleTariffs BIT = 0,
@massmediaID INT = NULL,
@showTrafficWindows BIT = 0
)
AS
Set Nocount On

Declare @broadcasrStartHour tinyint
If Not @broadcastStart Is Null 
	Set @broadcasrStartHour = DatePart(hh, @broadcastStart)
Else
	Set @broadcasrStartHour = 0

Declare @tmpWindow Table (windowId int)

IF @windowDateActual IS NOT NULL AND @windowDateOriginal IS NOT NULL AND @massmediaID IS NOT NULL 
	INSERT INTO @tmpWindow
	SELECT windowID 
	FROM [TariffWindow]
	WHERE [windowDateActual] = @windowDateActual 
		AND [windowDateOriginal] = @windowDateOriginal
		AND massmediaID = @massmediaID
ELSE If @windowId Is Not Null
	Insert Into @tmpWindow Values(@windowId)
Else If @actualDate Is Not Null
	Insert Into @tmpWindow
	Select windowId
	From TariffWindow
	where @actualDate between windowDateActual and DATEADD(s, duration, windowDateActual)
		AND massmediaID = @massmediaID 
Else If @moduleId Is Null
	Insert Into @tmpWindow
	Select
		tw.windowId
	From
		TariffWindow tw
		Inner Join Tariff t On t.tariffId = tw.tariffId
			AND (@excludeModuleTariffs = 0 OR t.[isForModuleOnly] = 0)
	Where
		t.pricelistId = Coalesce(@pricelistId, t.pricelistId)
		And tw.dayOriginal >= Coalesce(@startDate, tw.dayOriginal)
		And tw.dayOriginal <= Coalesce(@finishDate, tw.dayOriginal)
	UNION all
	SELECT DISTINCT
		tw.windowId
	From
		TariffWindow tw
		INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID 
			and tw.dayOriginal between pl.startDate and pl.finishDate
	WHERE
		tw.tariffID IS NULL AND @showTrafficWindows = 1 AND @excludeSpecialWindows = 0 AND 
		pl.pricelistId = Coalesce(@pricelistId, pl.pricelistId)
		And tw.dayOriginal >= Coalesce(@startDate, tw.dayOriginal)
		And tw.dayOriginal <= Coalesce(@finishDate, tw.dayOriginal)
Else
	Insert Into @tmpWindow
	Select
		tw.windowId
	From
		TariffWindow tw
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
	Where
		mpl.moduleId = @moduleId
		And mpl.pricelistId = @pricelistId
		and mpl.startDate <= @finishDate and mpl.finishDate >= @startDate
		And tw.dayOriginal >= Coalesce(@startDate, tw.dayOriginal)
		And tw.dayOriginal <= Coalesce(@finishDate, tw.dayOriginal)
	UNION all
	SELECT DISTINCT
		tw.windowId
	From
		TariffWindow tw
		INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID
			and tw.dayOriginal between pl.startDate and pl.finishDate
		INNER JOIN [Module] m ON tw.massmediaID = m.[massmediaID] AND m.moduleId = @moduleId
		Inner Join ModulePriceList mpl On mpl.priceListID = pl.priceListID
	WHERE
		tw.tariffID IS NULL AND @showTrafficWindows = 1  AND @excludeSpecialWindows = 0 AND 
		pl.pricelistId = Coalesce(@pricelistId, pl.pricelistId)
		and mpl.startDate <= @finishDate and mpl.finishDate >= @startDate
		And tw.dayOriginal >= Coalesce(@startDate, tw.dayOriginal)
		And tw.dayOriginal <= Coalesce(@finishDate, tw.dayOriginal)
				
IF @excludeSpecialWindows = 0	
	BEGIN
	Select
		tw.*,
		'Рекламное окно ' + Convert(varchar(10), windowDateOriginal, 104) + ' ' + 
			Convert(varchar(5), windowDateOriginal, 108)
			+ case when windowDateOriginal != windowDateActual then ' (' + Convert(varchar(10), windowDateOriginal, 104) + ' ' + Convert(varchar(5), windowDateActual, 108) + ')' else '' end as [name] ,
		DatePart(hh, windowDateOriginal) as [hour],
		DatePart(mi, windowDateOriginal) as [min],
		dayOriginal AS windowDateBroadcast,
		dayActual  AS windowDateActualBroadcast
	INTO #tmp1	
	From
		@tmpWindow ttw
		Inner Join TariffWindow tw On ttw.windowId = tw.windowId
	Order By
		tw.windowDateOriginal Desc	
	
	SELECT DISTINCT	
		DatePart(hh, windowDateOriginal) AS [hour],
		DatePart(mi, windowDateOriginal) as [min],
		price,
		Case
			When DatePart(hh, windowDateOriginal) >= @broadcasrStartHour Then 0
			Else 1
		End as flag
	From #tmp1 
		Order By flag, [hour], [min]	

	SELECT #tmp1.*, 
		case 
			when tu.tariffID is null then 0
			else 1
		end as IsTariffUnited 
		FROM 
			[#tmp1] Left Join TariffUnion tu On (#tmp1.tariffId = tu.tariffID or #tmp1.tariffId = tu.tariffUnionID)
	END
ELSE	
	BEGIN

	Select
		tw.*,
		'Рекламное окно ' + Convert(varchar(10), windowDateOriginal, 104) + ' ' + 
			Convert(varchar(5), windowDateOriginal, 108) as [name],
		DatePart(hh, windowDateOriginal) as [hour],
		DatePart(mi, windowDateOriginal) as [min],
		dayOriginal AS windowDateBroadcast,
		dayActual AS windowDateActualBroadcast
	INTO
		#tmp2			
	From
		@tmpWindow ttw
		Inner Join TariffWindow tw On ttw.windowId = tw.windowId
	Order By
		tw.windowDateOriginal Desc
		
	SELECT DISTINCT	
		DatePart(hh, windowDateOriginal) AS [hour],
		DatePart(mi, windowDateOriginal) as [min],
		price,
		Case
			When DatePart(hh, windowDateOriginal) >= @broadcasrStartHour Then 0
			Else 1
		End as flag
	From #tmp2 
		Order By flag, [hour], [min]
	
	SELECT * FROM [#tmp2]
	END
