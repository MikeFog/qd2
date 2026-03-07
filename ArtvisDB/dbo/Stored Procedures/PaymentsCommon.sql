CREATE PROC [dbo].[PaymentsCommon]
(
	@paymentID int = Null,
	@startOfInterval datetime = Null,
	@endOfInterval datetime = Null,
	@firmID smallint = Null,
	@headCompanyId smallint = Null,
	@paymentTypeID smallint = Null,
	@agencyID smallint  = Null,
	@isNotClosedOnly bit = 0,
	@agenciesIDString varchar(1024) = null,
	@actionID INT = NULL,
	@isHideWhite BIT = 0,
	@isHideBlack BIT = 0,
	@filterAgencies bit = 0,
	@loggedUserID int,
	@showBlack bit = 1,
	@showWhite bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #PaymentsCommon(paymentID int)

-- Проверка роли пользователя
DECLARE @isAdmin bit = 0
DECLARE @isBookKeeper bit = 0

SELECT 
	@isAdmin = IsAdmin,
	@isBookKeeper = IsBookKeeper
FROM [dbo].[user]
WHERE userID = @loggedUserID

IF @agenciesIDString is not Null 
BEGIN -- баланс для фирмы
	CREATE TABLE #Agency (agencyID smallint)

	Exec hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString
	
	INSERT INTO #PaymentsCommon(paymentID)
	SELECT DISTINCT 
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN HeadCompany hc ON hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID 
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
			AND pa.[actionID] = COALESCE(@actionID, pa.[actionID])
	WHERE
		-- Фильтр по фирме и головной компании
		p.firmID = Coalesce(@firmID, p.firmID)
		AND f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		
		-- Фильтр по периоду
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		
		-- Фильтр по списку агентств
		AND p.agencyID IN (SELECT agencyID From #Agency)
		
		-- Фильтр по типу платежа (белый/черный)
		AND (
			(pt.isHidden = 0 OR @isHideWhite = 0) 
			AND (pt.isHidden = 1 OR @isHideBlack = 0)
			AND (
				(pt.IsHidden = 1 AND @showBlack = 1)  
				OR (pt.IsHidden = 0 AND @showWhite = 1)
			)
		)
		
		-- Фильтр по правам доступа пользователя к агентству
		AND (
			@isAdmin = 1  -- Админу показываем все агентства
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все агентства
			OR @filterAgencies = 0  -- Флаг отключен - показываем все агентства
			OR (
				-- Флаг включен и пользователь не админ/бухгалтер - проверяем права доступа
				EXISTS(
					SELECT 1 
					FROM Campaign cam 
					INNER JOIN [Action] act ON cam.actionID = act.actionID 
					WHERE a.agencyID = cam.agencyID 
						AND @loggedUserID = act.userID
				) 
				OR EXISTS(
					SELECT 1 
					FROM Payment pay 
					WHERE a.agencyID = pay.agencyID 
						AND @loggedUserID = pay.userID
				)
			)
		)
		
		-- Фильтр видимости платежей для обычных пользователей
		AND (
			@isAdmin = 1  -- Админу показываем все платежи
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все платежи
			OR p.summa > ISNULL((SELECT SUM(pa2.[summa]) FROM [PaymentAction] pa2 WHERE pa2.[paymentID] = p.[paymentID]), 0)  -- Платеж не полностью распределен
			OR EXISTS(
				-- Платеж распределен, но есть Action пользователя
				SELECT 1 
				FROM [PaymentAction] pa2
				INNER JOIN [Action] act ON pa2.actionID = act.actionID
				WHERE pa2.[paymentID] = p.[paymentID] 
					AND act.userID = @loggedUserID
			)
		)
END
ELSE 
BEGIN
	INSERT INTO #PaymentsCommon(paymentID)
	SELECT DISTINCT
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN HeadCompany hc ON hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
	WHERE
		-- Фильтр по фирме и головной компании
		p.firmID = Coalesce(@firmID, p.firmID)
		AND f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		
		-- Фильтр по типу платежа и агентству
		AND p.paymentTypeID = Coalesce(@paymentTypeID, p.paymentTypeID)
		AND p.agencyID = Coalesce(@agencyID, p.agencyID)
		
		-- Фильтр по незакрытым платежам
		AND (
			@isNotClosedOnly = 0 
			OR p.summa > ISNULL((SELECT SUM(pa.[summa]) FROM [PaymentAction] pa WHERE pa.[paymentID] = p.[paymentID]), 0)
		)
		
		-- Фильтр по периоду
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		
		-- Фильтр по конкретному платежу
		AND p.paymentID = Coalesce(@paymentID, p.paymentID)
		
		-- Фильтр по действию
		AND (@actionID IS NULL OR pa.[actionID] = @actionID)
		
		-- Фильтр по типу платежа (белый/черный)
		AND (
			(pt.isHidden = 0 OR @isHideWhite = 0) 
			AND (pt.isHidden = 1 OR @isHideBlack = 0)
			AND (
				(pt.IsHidden = 1 AND @showBlack = 1)  
				OR (pt.IsHidden = 0 AND @showWhite = 1)
			)
		)
		
		-- Фильтр по правам доступа пользователя к агентству
		AND (
			@isAdmin = 1  -- Админу показываем все агентства
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все агентства
			OR @filterAgencies = 0  -- Флаг отключен - показываем все агентства
			OR (
				-- Флаг включен и пользователь не админ/бухгалтер - проверяем права доступа
				EXISTS(
					SELECT 1 
					FROM Campaign cam 
					INNER JOIN [Action] act ON cam.actionID = act.actionID 
					WHERE a.agencyID = cam.agencyID 
						AND @loggedUserID = act.userID
				) 
				OR EXISTS(
					SELECT 1 
					FROM Payment pay 
					WHERE a.agencyID = pay.agencyID 
						AND @loggedUserID = pay.userID
				)
			)
		)
		
		-- Фильтр видимости платежей для обычных пользователей
		AND (
			@isAdmin = 1  -- Админу показываем все платежи
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все платежи
			OR p.summa > ISNULL((SELECT SUM(pa2.[summa]) FROM [PaymentAction] pa2 WHERE pa2.[paymentID] = p.[paymentID]), 0)  -- Платеж не полностью распределен
			OR EXISTS(
				-- Платеж распределен, но есть Action пользователя
				SELECT 1 
				FROM [PaymentAction] pa2
				INNER JOIN [Action] act ON pa2.actionID = act.actionID
				WHERE pa2.[paymentID] = p.[paymentID] 
					AND act.userID = @loggedUserID
			)
		)
END

Exec sl_PaymentsCommon
