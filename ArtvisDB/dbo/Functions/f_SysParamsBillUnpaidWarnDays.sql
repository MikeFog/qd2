-- =============================================
-- Настройка «Системные настройки» (iInternalVariable, 'BillUnpaidWarnDays'): за сколько РАБОЧИХ дней до
-- первого дня акции «Реестр счетов» начинает показывать оранжевый кружок у активированной, но не оплаченной акции.
-- Не задана — 4. Читает BillRegister; поле в паспорте сущности 214 («Настройки...» Протектора) — Deploy/81.
-- =============================================
CREATE FUNCTION [dbo].[f_SysParamsBillUnpaidWarnDays]
(
)
RETURNS int
AS
BEGIN
	RETURN COALESCE((SELECT TOP 1 CAST([value] AS int) FROM iInternalVariable WHERE [name] = 'BillUnpaidWarnDays'), 4)
END
