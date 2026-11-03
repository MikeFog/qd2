-- Расчётный счёт агентства — в конкретном банке (03.11.2026, Миша). Раньше у агентства был один счёт (Agency.account),
-- не связанный с выбранным банком: сменили банк — счёт остался прежним.
-- 1. Таблица AgencyBank (агентство, банк, счёт); перенос пар Agency.bankID + Agency.account.
-- 2. AgencyIUD: параметры прежние; @account пишется в AgencyBank для пары (@agencyID, @bankID); банк выбран, а счёт пустой —
--    отказ AgencyBankAccountRequired. Без банка счёт не хранится. Счета в других банках остаются (вернут банк — вернётся счёт).
-- 3. sl_Agencies (agencies, ActionAgencies…): колонка account — счёт в текущем банке агентства. Счета, QR, документы читают
--    Agency.Account через неё — для них ничего не меняется.
-- 4. agencyPassport: вторая выборка — счета агентства по банкам (iTableAlias позиция 2 «studio» → «agencyBank»; studio
--    паспорт агентства не использовал). Паспорт подставляет счёт при выборе банка.
-- 5. Паспорт агентства (iEntity 8): у поля «Расчетный счет» type="string" — колонки в таблице больше нет; разделитель
--    перенесен из-под «Банк» под «Расчетный счет» — банк и счет теперь одна группа.
-- 6. DROP COLUMN Agency.account — после переноса и сверки.
-- 7. iMessage AgencyBankAccountRequired + испанский.
-- Клиент: новые Merlin.exe и веб (подстановка счёта при выборе банка). Старый клиент: сохранение работает, счёт при смене банка
-- не подставляется (сохранит введённый в поле для выбранного банка). После наката перезапустить qd2 и веб.
-- Перед переносом: агентство со счётом без банка — отказ (счёт пропал бы); на 03.11.2026 таких нет ни на Artvis, ни на Tumen.
-- Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 90_agency-bank-account.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

-- 1. Таблица и перенос (ссылки на Agency.account — динамическим SQL: при повторном запуске колонки уже нет)
IF COL_LENGTH('dbo.Agency', 'account') IS NOT NULL
	EXEC (N'IF EXISTS (SELECT 1 FROM dbo.Agency WHERE bankID IS NULL AND NULLIF(LTRIM(RTRIM(account)), '''') IS NOT NULL)
	BEGIN
		SELECT agencyID, name, account FROM dbo.Agency WHERE bankID IS NULL AND NULLIF(LTRIM(RTRIM(account)), '''') IS NOT NULL;
		RAISERROR(N''90: у агентств выше есть расчетный счет без банка — укажите банк в паспорте и запустите скрипт снова.'', 16, 1);
	END');
GO

IF OBJECT_ID('dbo.AgencyBank', 'U') IS NULL
	CREATE TABLE [dbo].[AgencyBank] (
		[agencyID] SMALLINT     NOT NULL,
		[bankID]   SMALLINT     NOT NULL,
		[account]  VARCHAR (32) NOT NULL,
		CONSTRAINT [PK_AgencyBank] PRIMARY KEY CLUSTERED ([agencyID] ASC, [bankID] ASC),
		CONSTRAINT [FK_AgencyBank_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
		CONSTRAINT [FK_AgencyBank_Bank] FOREIGN KEY ([bankID]) REFERENCES [dbo].[Bank] ([bankId]) ON DELETE CASCADE
	);
GO

IF COL_LENGTH('dbo.Agency', 'account') IS NOT NULL
	EXEC (N'INSERT INTO dbo.AgencyBank (agencyID, bankID, account)
		SELECT a.agencyID, a.bankID, LTRIM(RTRIM(a.account))
		FROM dbo.Agency a
		WHERE a.bankID IS NOT NULL AND NULLIF(LTRIM(RTRIM(a.account)), '''') IS NOT NULL
			AND NOT EXISTS (SELECT 1 FROM dbo.AgencyBank ab WHERE ab.agencyID = a.agencyID AND ab.bankID = a.bankID);
	PRINT N''90: перенесено пар агентство-банк: '' + CAST(@@ROWCOUNT AS nvarchar(10));');
GO

-- 2–4. Процедуры


CREATE OR ALTER PROCEDURE [dbo].[AgencyIUD]
(
@agencyID smallint = null out,
@name nvarchar(32) = NULL,
@address nvarchar(128) = NULL,
@phone varchar(32) = NULL,
@fax varchar(32) = NULL,
@email varchar(256) = NULL,
@account varchar(32) = NULL,
@inn varchar(32) = NULL,
@kpp varchar(16) = NULL,
@okpo varchar(32) = NULL,
@okonh varchar(32) = NULL,
@egrn varchar(16) = NULL,
@okved varchar(16) = NULL,
@bankID smallint = NULL,
@director nvarchar(32) = NULL,
@bookkeeper nvarchar(32) = NULL,
@prefix nvarchar(64) = NULL,
@isActive tinyint = 1,
@directorSignature nvarchar(255) = NULL,
@bookkeeperSignature nvarchar(255) = NULL,
@fullPrefix nvarchar(256) = NULL,
@reportString nvarchar(256) = NULL,
@registration nvarchar(256) = NULL,
@actionName varchar(32),
@withResultset bit = 1,
@painting image = null,
@reportPlace nvarchar(64),
@path2proposalTemplate varchar(128) = null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON

-- Расчётный счёт — счёт агентства в выбранном банке (AgencyBank), без банка не хранится.
SET @account = NULLIF(LTRIM(RTRIM(@account)), '')
IF @actionName IN ('AddItem', 'UpdateItem') AND @bankID IS NOT NULL AND @account IS NULL
begin
	raiserror('AgencyBankAccountRequired', 16, 1)
	return
end

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Agency](name, address, phone, fax, email, inn, okpo, okonh, bankID, director,
		bookkeeper, prefix, isActive, directorSignature, bookkeeperSignature, fullPrefix, reportString, 
		registration, kpp, egrn, okved, painting, reportPlace, path2proposalTemplate)
	VALUES(@name, @address, @phone, @fax, @email, @inn, @okpo, @okonh, @bankID, @director,
		@bookkeeper, @prefix, @isActive, @directorSignature, @bookkeeperSignature, @fullPrefix, @reportString, 
		@registration, @kpp, @egrn, @okved, @painting, @reportPlace, @path2proposalTemplate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @agencyID = SCOPE_IDENTITY()

	IF @bankID IS NOT NULL
		INSERT INTO [AgencyBank](agencyID, bankID, account) VALUES(@agencyID, @bankID, @account)

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [Agency] WHERE agencyID = @agencyID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Agency]
	SET			
		name = @name, 
		address = @address, 
		phone = @phone, 
		fax = @fax, 
		email = @email,
		inn = @inn, 
		kpp = @kpp,
		okpo = @okpo, 
		okonh = @okonh, 
		egrn = @egrn,
		okved = @okved,
		bankID = @bankID, 
		director = @director, 
		bookkeeper = @bookkeeper, 
		prefix = @prefix, 
		isActive = @isActive, 
		directorSignature = @directorSignature, 
		bookkeeperSignature = @bookkeeperSignature, 
		fullPrefix = @fullPrefix, 
		reportString = @reportString, 
		registration = @registration,
		painting = @painting,
		reportPlace = @reportPlace,
		path2proposalTemplate = @path2proposalTemplate
	WHERE
		AgencyID = @AgencyID

	-- счета в других банках остаются: вернут банк — вернётся и счёт
	IF @bankID IS NOT NULL
	BEGIN
		UPDATE [AgencyBank] SET account = @account WHERE agencyID = @agencyID AND bankID = @bankID
		IF @@ROWCOUNT = 0
			INSERT INTO [AgencyBank](agencyID, bankID, account) VALUES(@agencyID, @bankID, @account)
	END

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END

GO







CREATE OR ALTER PROCEDURE [dbo].[sl_Agencies]
as
SET NOCOUNT ON
SELECT 
	ag.*,
	ag.agencyID as id,
	bn.name as bankName,
	ab.account
FROM
	[Agency] ag
	INNER JOIN #Agency ta ON ta.agencyID = ag.agencyID
	LEFT JOIN bank bn ON bn.bankID = ag.bankID
	LEFT JOIN AgencyBank ab ON ab.agencyID = ag.agencyID AND ab.bankID = ag.bankID
ORDER BY
	ag.[name]

GO

CREATE OR ALTER PROC [dbo].[agencyPassport]
(
@agencyID smallint = NULL
)
AS

SET NOCOUNT ON

SELECT 
	mm.isActive,
	mm.[massmediaID], 
	mm.[name],
	mm.groupName as groupName2,	
	Cast(
		CASE 
			WHEN am.massmediaID Is NULL then 0
			ELSE 1
		END As Bit) isObjectSelected
FROM 
	[vMassmedia] mm
	LEFT JOIN AgencyMassmedia am ON am.massmediaID = mm.massmediaID
		AND am.agencyID = @agencyID
ORDER BY
	isObjectSelected desc, mm.[name]

-- счета агентства по банкам: паспорт подставляет счёт при выборе банка
SELECT
	ab.bankID,
	ab.account
FROM
	AgencyBank ab
WHERE
	ab.agencyID = @agencyID
GO

DECLARE @sp int = (SELECT storedProcedureID FROM dbo.iStoredProcedure WHERE name = 'agencyPassport');
IF @sp IS NULL
	RAISERROR(N'90: в iStoredProcedure нет agencyPassport.', 16, 1);
ELSE IF EXISTS (SELECT 1 FROM dbo.iTableAlias WHERE storedProcedureID = @sp AND position = 2)
	UPDATE dbo.iTableAlias SET name = N'agencyBank' WHERE storedProcedureID = @sp AND position = 2;
ELSE
	INSERT INTO dbo.iTableAlias (storedProcedureID, position, name) VALUES (@sp, 2, N'agencyBank');
GO

-- 5. Паспорт агентства: type="string" у счета и разделитель после счета (первый вариант 90 — только type="string")
DECLARE @nl nvarchar(4) = CHAR(13) + CHAR(10) + CHAR(9) + CHAR(9);
DECLARE @old  nvarchar(max) = N'<separator />' + @nl + N'<field caption="Расчетный счет:" name="account" />' + @nl + N'<field caption="ИНН:"';
DECLARE @old2 nvarchar(max) = N'<separator />' + @nl + N'<field caption="Расчетный счет:" name="account" type="string" />' + @nl + N'<field caption="ИНН:"';
DECLARE @new  nvarchar(max) = N'<field caption="Расчетный счет:" name="account" type="string" />' + @nl + N'<separator />' + @nl + N'<field caption="ИНН:"';
DECLARE @p nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 8);
IF @p IS NULL
	RAISERROR(N'90: у iEntity 8 нет паспорта.', 16, 1);
ELSE IF CHARINDEX(@new, @p) = 0
BEGIN
	IF CHARINDEX(@old, @p) > 0
		UPDATE dbo.iEntity SET passport = REPLACE(@p, @old, @new) WHERE entityID = 8;
	ELSE IF CHARINDEX(@old2, @p) > 0
		UPDATE dbo.iEntity SET passport = REPLACE(@p, @old2, @new) WHERE entityID = 8;
	ELSE
		RAISERROR(N'90: в паспорте iEntity 8 не найден блок «Банк — Расчетный счет — ИНН».', 16, 1);
END
GO

-- 6. Колонка Agency.account — только когда все счета с банком перенесены
IF COL_LENGTH('dbo.Agency', 'account') IS NOT NULL
	EXEC (N'IF EXISTS (SELECT 1 FROM dbo.Agency a
			   WHERE NULLIF(LTRIM(RTRIM(a.account)), '''') IS NOT NULL
				 AND NOT EXISTS (SELECT 1 FROM dbo.AgencyBank ab
								 WHERE ab.agencyID = a.agencyID AND ab.bankID = a.bankID AND ab.account = LTRIM(RTRIM(a.account))))
		RAISERROR(N''90: не все счета агентств перенесены в AgencyBank, колонка Agency.account не удалена.'', 16, 1);
	ELSE
		ALTER TABLE dbo.Agency DROP COLUMN account;');
GO

-- 7. Сообщение об отказе
DECLARE @msg NVARCHAR(4000) = N'Не указан расчетный счет агентства в выбранном банке. Операция прервана.';
DECLARE @es  NVARCHAR(4000) = N'No se indicó la cuenta corriente de la agencia en el banco elegido. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'AgencyBankAccountRequired')
	UPDATE dbo.iMessage SET [message] = @msg WHERE name = 'AgencyBankAccountRequired';
ELSE
	INSERT INTO dbo.iMessage (name, [message]) VALUES ('AgencyBankAccountRequired', @msg);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
	MERGE dbo.iTranslation AS dst
	USING (SELECT 'es' AS lang, '' AS context, @msg AS [source], @es AS [text]) AS src
	   ON dst.lang = src.lang AND dst.context = src.context
	  AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
	WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
		UPDATE SET [text] = src.[text]
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

IF COL_LENGTH('dbo.Agency', 'account') IS NULL
	AND OBJECT_ID('dbo.AgencyBank', 'U') IS NOT NULL
	AND (SELECT COUNT(*) FROM dbo.Agency WHERE bankID IS NOT NULL) = (SELECT COUNT(*) FROM dbo.Agency a JOIN dbo.AgencyBank ab ON ab.agencyID = a.agencyID AND ab.bankID = a.bankID)
	AND EXISTS (SELECT 1 FROM dbo.iTableAlias ta JOIN dbo.iStoredProcedure sp ON sp.storedProcedureID = ta.storedProcedureID
				WHERE sp.name = 'agencyPassport' AND ta.position = 2 AND ta.name = N'agencyBank')
	AND (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 8) LIKE N'%name="account" type="string" />%<separator />%name="inn"%'
	AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'AgencyBankAccountRequired')
	PRINT N'ГОТОВО: расчетный счет агентства — по банкам (AgencyBank).';
ELSE
	RAISERROR(N'90: проверка после наката не прошла.', 16, 1);
GO
