-- Списки фильтра «Реестр счетов» (сущность 1272, FilterPage Load). Таблицы названы по позиции в iTableAlias:
-- 1 managers — менеджеры, которых пользователь вправе выбирать (как у журнала оплат по менеджерам);
-- 2 bill_payment_status — статусы оплаты счёта (id — тот же @paymentStatusID, что у BillRegister).
CREATE PROCEDURE [dbo].[BillRegisterFilter]
(
	@loggedUserID smallint,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба; десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	-- 1. managers
	EXEC [UserListByRights] @loggedUserID = @loggedUserID

	-- 2. bill_payment_status
	SELECT CAST(1 AS tinyint) AS paymentStatusID, dbo.fn_Translate(@languageCode, N'Не оплачен') AS [name]
	UNION ALL SELECT 2, dbo.fn_Translate(@languageCode, N'Частично')
	UNION ALL SELECT 3, dbo.fn_Translate(@languageCode, N'Оплачен')
END
