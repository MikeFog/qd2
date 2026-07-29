-- Разбор интервалов-исключений политической агитации из карточки радиостанции.
-- Формат строки: "16:00-16:55; 18:00-19:00" (разделитель ';', перевод строки
-- считается тем же разделителем, пробелы игнорируются).
-- Для некорректного элемента startMin/finishMin возвращаются NULL - на этом
-- строится валидация при сохранении карточки (MassmediaIUD).
-- Время - минуты от начала суток; границы интервала включаются.
CREATE FUNCTION [dbo].[fn_AgitationExcludeIntervals] (@intervals varchar(256))
RETURNS @res TABLE
(
	item varchar(64) NOT NULL,
	startMin int NULL,
	finishMin int NULL
)
AS
BEGIN
	DECLARE @s varchar(600), @item varchar(64), @pos int,
		@h1 int, @m1 int, @h2 int, @m2 int

	SET @s = REPLACE(REPLACE(REPLACE(IsNull(@intervals, ''), CHAR(13), ''), CHAR(10), ';'), ' ', '')

	WHILE LEN(@s) > 0
	BEGIN
		SET @pos = CHARINDEX(';', @s)

		IF @pos = 0
		BEGIN
			SET @item = @s
			SET @s = ''
		END
		ELSE
		BEGIN
			SET @item = LEFT(@s, @pos - 1)
			SET @s = SUBSTRING(@s, @pos + 1, LEN(@s))
		END

		IF @item <> ''
		BEGIN
			SET @h1 = NULL

			IF @item LIKE '[0-2][0-9]:[0-5][0-9]-[0-2][0-9]:[0-5][0-9]'
			BEGIN
				SET @h1 = CAST(SUBSTRING(@item, 1, 2) AS int)
				SET @m1 = CAST(SUBSTRING(@item, 4, 2) AS int)
				SET @h2 = CAST(SUBSTRING(@item, 7, 2) AS int)
				SET @m2 = CAST(SUBSTRING(@item, 10, 2) AS int)

				-- часы 24+ и интервал "задом наперёд" (в т.ч. через полночь) не принимаем
				IF @h1 > 23 OR @h2 > 23 OR (@h1 * 60 + @m1) >= (@h2 * 60 + @m2)
					SET @h1 = NULL
			END

			INSERT INTO @res (item, startMin, finishMin)
			VALUES (@item,
				CASE WHEN @h1 IS NULL THEN NULL ELSE @h1 * 60 + @m1 END,
				CASE WHEN @h1 IS NULL THEN NULL ELSE @h2 * 60 + @m2 END)
		END
	END

	RETURN
END
