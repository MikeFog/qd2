-- =============================================
-- Дата, сдвинутая на @days рабочих дней (отрицательное число — назад). Рабочие дни — понедельник–пятница;
-- праздничного календаря в системе нет, поэтому выходные — только суббота и воскресенье (появится таблица
-- праздников — учитывать здесь). Как WORKDAY в Excel: от выходного дня назад первый рабочий — пятница.
-- День недели считается от понедельника 01.01.1900, а не DATEPART(weekday), чтобы не зависеть от SET DATEFIRST.
-- Читают: BillRegister («Реестр счетов», светофор по акциям, не оплаченным к выходу в эфир).
-- =============================================
CREATE FUNCTION [dbo].[fn_AddWorkDays]
(
	@date date,
	@days int
)
RETURNS date
AS
BEGIN
	DECLARE @step int = CASE WHEN @days < 0 THEN -1 ELSE 1 END,
		@left int = CASE WHEN ABS(@days) > 366 THEN 366 ELSE ABS(@days) END

	WHILE @left > 0
	BEGIN
		SET @date = DATEADD(day, @step, @date)
		IF DATEDIFF(day, '19000101', @date) % 7 < 5 -- 0 = пн ... 4 = пт
			SET @left = @left - 1
	END

	RETURN @date
END
