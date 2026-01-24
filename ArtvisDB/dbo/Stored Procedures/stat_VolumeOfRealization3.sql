CREATE   Procedure [dbo].[stat_VolumeOfRealization3]
(
    @StartDay DATETIME = NULL,
    @FinishDay DATETIME = NULL,
    @FirmID smallint = NULL, 
    @headCompanyID smallint = NULL, 
    @MassmediaID smallint = NULL, 
    @PaymentTypeID smallint = NULL,
    @CampaignTypeID tinyint = NULL,
    @ManagerID smallint = NULL,
    @AgencyID smallint = NULL,
    @massmediaGroupID int = NULL,
    @advertTypeID smallint = NULL,
    @IsGroupByMassmedia bit = 0,
    @IsGroupByPaymentType bit = 0,
    @IsGroupByCampaignType bit = 0,
    @IsGroupByManager bit = 0,
    @IsGroupByAgency bit = 0,
    @IsGroupByFirm bit = 0,
    @IsGroupByHeadCompany bit = 0,  -- НОВЫЙ ПАРАМЕТР
    @IsGroupByMassmediaGroupType bit = 0,
    @IsGroupByAdvertType bit = 0,
    @IsGroupByAdvertTypeTop bit = 0,
    @ShowWhite bit = 1,
    @ShowBlack bit = 1,
    @loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As
BEGIN
    SET NOCOUNT ON;

    IF @StartDay IS NULL OR @FinishDay IS NULL
    BEGIN
        RAISERROR('FilterStartFinishDays', 16, 1)
        RETURN
    END

    SET @StartDay  = dbo.ToShortDate(@StartDay)
    SET @FinishDay = dbo.ToShortDate(@FinishDay)

	DECLARE @IsFirmHeadCompanyOnly bit = 0;
	DECLARE @IsAdvertTypeTopOnly   bit = 0;

	IF @IsGroupByFirm = 1 AND @IsGroupByHeadCompany = 1
	   AND 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
			 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
			 + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	BEGIN
		SET @IsFirmHeadCompanyOnly = 1;
	END

	-- НОВОЕ: спец-режим только для AdvertType + AdvertTypeTop
	IF @IsGroupByAdvertType = 1 AND @IsGroupByAdvertTypeTop = 1
	   AND 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
			 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
			 + @IsGroupByFirm + @IsGroupByHeadCompany = 0
	BEGIN
		SET @IsAdvertTypeTopOnly = 1;
	END

    --------------------------------------------------------------------
    -- NEW: вместо fn_statGetPrice() создаём #Campaign и заполняем процедурой
    --------------------------------------------------------------------
    CREATE TABLE #Campaign
    (
        campaignID int NOT NULL, 
        advertTypeID smallint NULL,
        actionID int NULL, 
        massmediaID smallint NULL, 
        paymentTypeID smallint NULL, 
        campaignTypeID tinyint NULL, 
        agencyID smallint NULL,
        startDate datetime NULL,
        finishDate datetime NULL,
        finalPrice money NULL,
        userID smallint NULL,
        firmID smallint NULL,
        discount float NULL,
        massmediaGroupID int NULL,
        price money NULL
    );

    CREATE UNIQUE CLUSTERED INDEX IX_Campaign
        ON #Campaign (campaignID, massmediaID, advertTypeID);

    INSERT INTO #Campaign
    EXEC dbo.stat_GetPrice_proc
         @StartDay,
         @FinishDay,
         @loggedUserID;

-- ... после заполнения #Campaign

	DECLARE @SQLString NVARCHAR(MAX);
	DECLARE @JoinSql   NVARCHAR(MAX) = N'';
	DECLARE @WhereSql  NVARCHAR(MAX) = N' WHERE d.price <> 0 ';
	DECLARE @GroupJoinSql NVARCHAR(MAX) = N'';

	DECLARE @IsStarted int;

	-- Базовые join'ы для фильтров/группировок
	-- Firm понадобится почти всегда (у тебя часто фильтр по headCompanyID и group-by firm/headCompany)
	SET @JoinSql += N' INNER JOIN Firm f ON f.firmId = d.firmId ';

	-- AdvertType нужен если фильтруем по advertTypeID (для parentID)
	IF @advertTypeID IS NOT NULL
		SET @JoinSql += N' LEFT JOIN AdvertType atFilter ON atFilter.advertTypeID = d.advertTypeID ';

	-- PaymentType нужен если:
	--  1) фильтруем по ShowWhite/ShowBlack
	--  2) группируем по PaymentType
	--  3) фильтруем по PaymentTypeID (можно без join, но пусть будет единообразно)
	IF (@ShowWhite = 0 OR @ShowBlack = 0) OR (@IsGroupByPaymentType <> 0)
	BEGIN
		SET @JoinSql += N' INNER JOIN PaymentType pt ON pt.PaymentTypeID = d.PaymentTypeID ';
	END

	-- ========= ЕДИНЫЙ ФИЛЬТР (Where) =========

	-- ShowWhite/ShowBlack: каноничная логика
	-- (white = pt.isHidden = 0, black = pt.isHidden <> 0)
	-- Если оба 1 -> пропускаем всё, если оба 0 -> пусто
	IF (@ShowWhite = 0 OR @ShowBlack = 0)
	BEGIN
		SET @WhereSql += N'
	  AND (
			(@ShowWhite = 1 AND pt.isHidden = 0)
		 OR (@ShowBlack = 1 AND pt.isHidden <> 0)
	  )';
	END

	IF @FirmID IS NOT NULL
		SET @WhereSql += N' AND d.firmID = @FirmID';

	IF @headCompanyID IS NOT NULL
		SET @WhereSql += N' AND f.headCompanyID = @headCompanyID';

	IF @MassmediaID IS NOT NULL
		SET @WhereSql += N' AND d.massmediaID = @MassmediaID';

	IF @PaymentTypeID IS NOT NULL
		SET @WhereSql += N' AND d.paymentTypeID = @PaymentTypeID';

	IF @CampaignTypeID IS NOT NULL
		SET @WhereSql += N' AND d.campaignTypeID = @CampaignTypeID';

	IF @ManagerID IS NOT NULL
		SET @WhereSql += N' AND d.userID = @ManagerID';

	IF @AgencyID IS NOT NULL
		SET @WhereSql += N' AND d.agencyID = @AgencyID';

	IF @massmediaGroupID IS NOT NULL
		SET @WhereSql += N' AND d.massmediaGroupID = @massmediaGroupID';

	IF @advertTypeID IS NOT NULL
		SET @WhereSql += N' AND (d.advertTypeID = @advertTypeID OR atFilter.parentID = @advertTypeID)';

	-- ========= Сборка SQL =========
	SET @SQLString = N'
	DECLARE @Summa money = 0;

	SELECT @Summa = ISNULL(SUM(d.price), 0)
	FROM #Campaign d
	' + @JoinSql + CHAR(10) + @WhereSql + N';

	';

	IF @IsFirmHeadCompanyOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @res TABLE (
			RowNum int,
			sum4 money,
			sum1 money,
			firm varchar(256),
			head_company varchar(256),
			hc2 varchar(256),
			[percent] decimal(12,2),
			row_style varchar(20),
			INDEX i1 UNIQUE CLUSTERED (RowNum)
		);

		INSERT INTO @res(RowNum, sum1, firm, head_company, [percent])
		';
	END

	IF @IsAdvertTypeTopOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @resAdvert TABLE (
			RowNum int,
			sum4 money,
			sum1 money,
			adverttype varchar(256),
			topAdverttype varchar(256),
			top2 varchar(256),
			[percent] decimal(12,2),
			row_style varchar(20),
			INDEX i1 UNIQUE CLUSTERED (RowNum)
		);

		INSERT INTO @resAdvert(RowNum, sum1, adverttype, topAdverttype, [percent])
		';
	END
	-- ==== дальше твоя логика построения SELECT списка ====

	-- 1) Заголовок SELECT + поля группировки
	SET @SQLString += N'
	SELECT
		row_number() over(order by ISNULL(SUM(d.price),0)) as RowNum,
		ISNULL(SUM(d.price),0) as sum1,
	';

	IF @IsGroupByPaymentType <> 0
		SET @SQLString += N'  pt.Name as payment_type,';
	IF @IsGroupByCampaignType <> 0
		SET @SQLString += N'  iCampaignType.Name as campaign_type,';
	IF @IsGroupByMassmedia <> 0
		SET @SQLString += N'  vMassMedia.NameWithGroup as massmedia, vMassMedia.massmediaID,';
	IF @IsGroupByMassmediaGroupType <> 0
		SET @SQLString += N'  MassmediaGroup.Name as massmedia_group,';
	IF @IsGroupByFirm <> 0
		SET @SQLString += N'  f.Name as firm,';
	IF @IsGroupByHeadCompany <> 0
		SET @SQLString += N'  HeadCompany.Name as head_company,';
	IF @IsGroupByManager <> 0
		SET @SQLString += N'  [User].userName as manager,';
	IF @IsGroupByAgency <> 0
		SET @SQLString += N'  Agency.Name as agency,';
	IF @IsGroupByAdvertType <> 0
		SET @SQLString += N'  AdvertType.Name as adverttype,';
	IF @IsGroupByAdvertTypeTop <> 0
		SET @SQLString += N'  at.Name as topAdverttype,';

	IF 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm
		 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByHeadCompany
		 + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
		SET @SQLString += N'  max(''Все'') as [all],';

	-- 2) JOIN’ы для группировок (только если нужны, чтобы не тащить лишнее)
	-- ВНИМАНИЕ: Firm уже присоединён как f в @JoinSql для фильтра headCompany. Но для group-by firm/headcompany у тебя были другие алиасы.
	-- Чтобы не ломать существующую часть, оставим твою секцию JOIN'ов, но только добавим недостающее:
	IF @IsGroupByMassmediaGroupType <> 0 SET @GroupJoinSql  += N' inner join MassmediaGroup on d.massmediaGroupID = MassmediaGroup.massmediaGroupID ';
	IF @IsGroupByCampaignType <> 0 SET @GroupJoinSql += N' inner join iCampaignType on d.campaignTypeID = iCampaignType.CampaignTypeID';
	IF @IsGroupByMassmedia <> 0 SET @GroupJoinSql += N' inner join vMassMedia on d.massmediaID = vMassMedia.massmediaID';

	IF @IsGroupByHeadCompany <> 0 SET @GroupJoinSql += N' inner join HeadCompany on f.headCompanyID = HeadCompany.headCompanyID ';
	IF @IsGroupByManager <> 0 SET @GroupJoinSql += N' inner join [User] on d.userID = [User].UserID';
	IF @IsGroupByAgency <> 0 SET @GroupJoinSql += N' inner join Agency on d.AgencyID = Agency.AgencyID';
	IF @IsGroupByAdvertType <> 0 OR @IsGroupByAdvertTypeTop <> 0 SET @GroupJoinSql += N' left join AdvertType on d.advertTypeID = AdvertType.AdvertTypeID';
	IF @IsGroupByAdvertTypeTop <> 0 SET @GroupJoinSql += N' left join AdvertType at on AdvertType.parentID = at.AdvertTypeID';

	-- percent (теперь sum1 и @Summa на одном и том же фильтре)
	SET @SQLString += N'
		CASE @Summa
			WHEN 0 THEN 0
			ELSE CAST((ISNULL(SUM(d.price),0) * 100.0 / @Summa) as decimal(12,2))
		END as [percent]
	FROM #Campaign d
	' + @JoinSql + CHAR(10) + @GroupJoinSql + CHAR(10)  + @WhereSql + CHAR(10);

	-- 3) GROUP BY (как у тебя)
	IF 0 + @IsGroupByPaymentType + @IsGroupByCampaignType
		  + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType 
		  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop + @IsGroupByHeadCompany <> 0
	BEGIN
		SET @IsStarted = 0;
		SET @SQLString += N' GROUP BY ';

		IF @IsGroupByPaymentType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'pt.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByCampaignType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'iCampaignType.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByMassmedia <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'vMassMedia.NameWithGroup, vMassMedia.massmediaID';
			SET @IsStarted = 1;
		END

		IF @IsGroupByFirm <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'f.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByHeadCompany <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'HeadCompany.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByManager <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'[User].userName';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAgency <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'Agency.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAdvertType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'AdvertType.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAdvertTypeTop <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'at.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByMassmediaGroupType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'MassmediaGroup.Name';
			SET @IsStarted = 1;
		END
	END

	IF @IsFirmHeadCompanyOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @c int;
		SELECT @c = COUNT(*) FROM @res;

		INSERT INTO @res(RowNum, sum4, head_company, [percent], row_style)
		SELECT ROW_NUMBER() OVER (ORDER BY head_company) + @c,
			   SUM(sum1),
			   head_company,
			   SUM([percent]),
			   ''bold''
		FROM @res
		GROUP BY head_company
		HAVING COUNT(*) > 1;

		UPDATE @res SET hc2 = head_company;

		WITH DuplicatesCTE AS (
			SELECT head_company
			FROM @res
			GROUP BY head_company
			HAVING COUNT(*) > 1
		)
		UPDATE t
		SET t.head_company = NULL
		FROM @res t
		INNER JOIN DuplicatesCTE d ON t.head_company = d.head_company
		WHERE t.firm IS NOT NULL;

		UPDATE @res
		SET sum4 = sum1
		WHERE sum1 IS NOT NULL AND head_company IS NOT NULL;

		SELECT * FROM @res ORDER BY hc2, firm;
		';
	END

	IF @IsAdvertTypeTopOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @c2 int;
		SELECT @c2 = COUNT(*) FROM @resAdvert;

		-- добавляем "итоги" по верхнему уровню (topAdverttype) как жирные строки
		INSERT INTO @resAdvert(RowNum, sum4, topAdverttype, [percent], row_style)
		SELECT ROW_NUMBER() OVER (ORDER BY topAdverttype) + @c2,
			   SUM(sum1),
			   topAdverttype,
			   SUM([percent]),
			   ''bold''
		FROM @resAdvert
		GROUP BY topAdverttype
		HAVING COUNT(*) > 1;

		UPDATE @resAdvert SET top2 = topAdverttype;

		-- прячем повторяющийся верхний уровень в "детальных" строках (как head_company)
		WITH DuplicatesCTE AS (
			SELECT topAdverttype
			FROM @resAdvert
			GROUP BY topAdverttype
			HAVING COUNT(*) > 1
		)
		UPDATE t
		SET t.topAdverttype = NULL
		FROM @resAdvert t
		INNER JOIN DuplicatesCTE d ON t.topAdverttype = d.topAdverttype
		WHERE t.adverttype IS NOT NULL;

		-- sum4 = sum1 для жирных строк (как у фирм)
		UPDATE @resAdvert
		SET sum4 = sum1
		WHERE sum1 IS NOT NULL AND topAdverttype IS NOT NULL;

		SELECT * FROM @resAdvert ORDER BY top2, adverttype;
		';
	END

	--PRINT @SQLString  -- можно включить для отладки
	--return

	EXECUTE sp_executesql @SQLString,
	N'@startDate datetime, @finishDate datetime, @loggedUserID smallint,
	  @FirmID smallint, @MassmediaID smallint, @PaymentTypeID smallint, @CampaignTypeID tinyint,
	  @ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint,
	  @ShowWhite bit, @ShowBlack bit',
		@startDate = @StartDay,
		@finishDate = @FinishDay,
		@loggedUserID = @loggedUserID,
		@FirmID = @FirmID,
		@MassmediaID = @MassmediaID,
		@PaymentTypeID = @PaymentTypeID,
		@CampaignTypeID = @CampaignTypeID,
		@ManagerID = @ManagerID,
		@AgencyID = @AgencyID,
		@massmediaGroupID = @massmediaGroupID,
		@advertTypeID = @advertTypeID,
		@headCompanyID = @headCompanyID,
		@ShowWhite = @ShowWhite,
		@ShowBlack = @ShowBlack;

END
