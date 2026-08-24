-- Разбор интервалов-исключений политической агитации из карточки радиостанции.
-- Формат: одна строка поля - один набор интервалов, внутри строки интервалы
-- разделяются ';'. Строка может начинаться с указания дней недели:
--     пн-пт 16:00-16:55; 18:00-19:00
--     сб,вс 10:00-11:00
--     12:00-12:30            <- строка без дней = все дни недели
-- Дни: пн вт ср чт пт сб вс; допускаются диапазон ('пн-пт'), перечисление
-- ('пн,ср,пт') и их сочетание ('пн-ср,сб'). Пробелы игнорируются, ':' после
-- дней допускается ('пн-пт: 16:00-16:55').
-- dayMask - битовая маска дней: бит 0 = пн ... бит 6 = вс, 127 = все дни.
-- Для некорректного элемента dayMask/startMin/finishMin возвращаются NULL - на
-- этом строится валидация при сохранении карточки (MassmediaIUD).
-- Время - минуты от начала суток; границы интервала включаются.
CREATE FUNCTION [dbo].[fn_AgitationExcludeIntervals] (@intervals varchar(256))
RETURNS @res TABLE
(
	item varchar(64) NOT NULL,
	dayMask int NULL,
	startMin int NULL,
	finishMin int NULL
)
AS
BEGIN
	DECLARE @all varchar(600), @line varchar(300), @rest varchar(300),
		@days varchar(64), @daysOrig varchar(64), @token varchar(64), @item varchar(64),
		@mask int, @pos int, @d1 int, @d2 int, @i int,
		@h1 int, @m1 int, @h2 int, @m2 int

	-- перевод строки - разделитель наборов, ';' - разделитель интервалов внутри набора
	SET @all = REPLACE(REPLACE(IsNull(@intervals, ''), CHAR(13), CHAR(10)), ' ', '')

	WHILE LEN(@all) > 0
	BEGIN
		SET @pos = CHARINDEX(CHAR(10), @all)

		IF @pos = 0
		BEGIN
			SET @line = @all
			SET @all = ''
		END
		ELSE
		BEGIN
			SET @line = LEFT(@all, @pos - 1)
			SET @all = SUBSTRING(@all, @pos + 1, LEN(@all))
		END

		IF @line = '' CONTINUE

		-- дни недели - всё до первой цифры
		SET @days = ''
		SET @rest = @line
		WHILE LEN(@rest) > 0 AND LEFT(@rest, 1) NOT LIKE '[0-9]'
		BEGIN
			SET @days = @days + LEFT(@rest, 1)
			SET @rest = SUBSTRING(@rest, 2, LEN(@rest))
		END
		SET @days = REPLACE(@days, ':', '')
		SET @daysOrig = @days

		IF @days = ''
			SET @mask = 127
		ELSE
		BEGIN
			SET @mask = 0

			WHILE LEN(@days) > 0 AND @mask IS NOT NULL
			BEGIN
				SET @pos = CHARINDEX(',', @days)

				IF @pos = 0
				BEGIN
					SET @token = @days
					SET @days = ''
				END
				ELSE
				BEGIN
					SET @token = LEFT(@days, @pos - 1)
					SET @days = SUBSTRING(@days, @pos + 1, LEN(@days))
				END

				SET @d1 = NULL
				SET @d2 = NULL

				IF LEN(@token) = 2
				BEGIN
					SELECT @d1 = num FROM (VALUES (N'пн', 0), (N'вт', 1), (N'ср', 2), (N'чт', 3),
						(N'пт', 4), (N'сб', 5), (N'вс', 6)) d(nm, num) WHERE nm = @token
					SET @d2 = @d1
				END
				ELSE IF LEN(@token) = 5 AND SUBSTRING(@token, 3, 1) = '-'
				BEGIN
					SELECT @d1 = num FROM (VALUES (N'пн', 0), (N'вт', 1), (N'ср', 2), (N'чт', 3),
						(N'пт', 4), (N'сб', 5), (N'вс', 6)) d(nm, num) WHERE nm = LEFT(@token, 2)
					SELECT @d2 = num FROM (VALUES (N'пн', 0), (N'вт', 1), (N'ср', 2), (N'чт', 3),
						(N'пт', 4), (N'сб', 5), (N'вс', 6)) d(nm, num) WHERE nm = RIGHT(@token, 2)
				END

				-- диапазон "задом наперёд" (пт-вт) не принимаем
				IF @d1 IS NULL OR @d2 IS NULL OR @d1 > @d2
					SET @mask = NULL
				ELSE
				BEGIN
					SET @i = @d1
					WHILE @i <= @d2
					BEGIN
						SET @mask = @mask | CAST(POWER(2, @i) AS int)
						SET @i = @i + 1
					END
				END
			END
		END

		-- дни указаны, а интервалов в строке нет - это ошибка формата
		IF @rest = ''
		BEGIN
			INSERT INTO @res (item, dayMask, startMin, finishMin)
			VALUES (LEFT(@line, 64), NULL, NULL, NULL)
			CONTINUE
		END

		WHILE LEN(@rest) > 0
		BEGIN
			SET @pos = CHARINDEX(';', @rest)

			IF @pos = 0
			BEGIN
				SET @item = @rest
				SET @rest = ''
			END
			ELSE
			BEGIN
				SET @item = LEFT(@rest, @pos - 1)
				SET @rest = SUBSTRING(@rest, @pos + 1, LEN(@rest))
			END

			IF @item <> ''
			BEGIN
				SET @h1 = NULL

				IF @mask IS NOT NULL AND @item LIKE '[0-2][0-9]:[0-5][0-9]-[0-2][0-9]:[0-5][0-9]'
				BEGIN
					SET @h1 = CAST(SUBSTRING(@item, 1, 2) AS int)
					SET @m1 = CAST(SUBSTRING(@item, 4, 2) AS int)
					SET @h2 = CAST(SUBSTRING(@item, 7, 2) AS int)
					SET @m2 = CAST(SUBSTRING(@item, 10, 2) AS int)

					-- часы 24+ и интервал "задом наперёд" (в т.ч. через полночь) не принимаем
					IF @h1 > 23 OR @h2 > 23 OR (@h1 * 60 + @m1) >= (@h2 * 60 + @m2)
						SET @h1 = NULL
				END

				INSERT INTO @res (item, dayMask, startMin, finishMin)
				VALUES (LEFT(CASE WHEN @daysOrig = '' THEN @item ELSE @daysOrig + ' ' + @item END, 64),
					CASE WHEN @h1 IS NULL THEN NULL ELSE @mask END,
					CASE WHEN @h1 IS NULL THEN NULL ELSE @h1 * 60 + @m1 END,
					CASE WHEN @h1 IS NULL THEN NULL ELSE @h2 * 60 + @m2 END)
			END
		END
	END

	RETURN
END
