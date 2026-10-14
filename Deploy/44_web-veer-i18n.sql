-- Веб, страница акции, вкладка «Рекламные окна»: веер (этап 4а форм акции, docs/tasks/web-action-forms.md) — испанские переводы
-- новых строк. Только строки интерфейса: процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта веер у пользователей с испанским языком работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 44_web-veer-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'44: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
            (N'Рекламная акция удалена. Обновите страницу.', N'La campaña publicitaria fue eliminada. Actualice la página.'),
        (N'На этой неделе нет слотов, общих для всех отмеченных станций', N'Esta semana no hay franjas comunes a todas las emisoras marcadas'),
        (N'Слот есть только там, где окно есть у каждой отмеченной станции. Снимите галочку с лишней станции или перейдите к другой неделе.', N'Hay franja solo donde cada emisora marcada tiene una ventana. Desmarque la emisora que sobra o vaya a otra semana.'),
        (N'Нет прайс-листа на эту неделю: {0}.', N'No hay lista de precios para esta semana: {0}.'),
        (N'не подтверждена', N'no confirmada'),
        (N'Модульная кампания этой акции ({0})', N'Pauta modular de esta campaña ({0})'),
        (N'Акция №{0} ({1})', N'Campaña n.º {0} ({1})'),
        (N'В этом окне:', N'En esta ventana:'),
        (N'Отсутствуют:', N'Faltan:'),
        (N'Отметить все', N'Marcar todos'),
        (N'Отметить все линейные кампании акции — веер по всем станциям', N'Marcar todas las pautas lineales de la campaña: abanico por todas las emisoras'),
        (N'Одна кампания — неделя рекламных окон её радиостанции: ставить ролики, смотреть выпуски окна, переносить их. Несколько — веер: слоты по всем отмеченным станциям.', N'Una pauta: la semana de ventanas publicitarias de su emisora: colocar spots, ver las emisiones de la ventana y trasladarlas. Varias: abanico, con franjas de todas las emisoras marcadas.'),
        (N'Выпусков акции за день по отмеченным кампаниям', N'Emisiones de la campaña en el día, en las pautas marcadas'),
        (N'Рекламные окна, веер: отмечено кампаний — {0}', N'Ventanas publicitarias, abanico: pautas marcadas — {0}'),
        (N'выпуски акции у всех отмеченных кампаний', N'emisiones de la campaña en todas las pautas marcadas'),
        (N'не у всех', N'no en todas'),
        (N'выпуски фирмы в других акциях на всех станциях', N'emisiones de la empresa en otras campañas en todas las emisoras'),
        (N'на части станций', N'en parte de las emisoras'),
        (N'выпуски акции не у всех отмеченных кампаний', N'emisiones de la campaña no en todas las pautas marcadas'),
        (N'выпуски фирмы в других акциях на части станций', N'emisiones de la empresa en otras campañas en parte de las emisoras'),
        (N'Нет права «Редактировать» на одну из отмеченных кампаний.', N'No tiene el permiso «Editar» para una de las pautas marcadas.'),
        (N'Ролик на все отмеченные станции ставится в «Режиме добавления»: щёлкните слот.', N'El spot se coloca en todas las emisoras marcadas en el «Modo de agregado»: haga clic en la franja.')
        ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
BEGIN
    DECLARE @missing int = (SELECT COUNT(*) FROM (VALUES
        (N'Рекламная акция удалена. Обновите страницу.'),
        (N'На этой неделе нет слотов, общих для всех отмеченных станций'),
        (N'Слот есть только там, где окно есть у каждой отмеченной станции. Снимите галочку с лишней станции или перейдите к другой неделе.'),
        (N'Нет прайс-листа на эту неделю: {0}.'),
        (N'не подтверждена'),
        (N'Модульная кампания этой акции ({0})'),
        (N'Акция №{0} ({1})'),
        (N'В этом окне:'),
        (N'Отсутствуют:'),
        (N'Отметить все'),
        (N'Отметить все линейные кампании акции — веер по всем станциям'),
        (N'Одна кампания — неделя рекламных окон её радиостанции: ставить ролики, смотреть выпуски окна, переносить их. Несколько — веер: слоты по всем отмеченным станциям.'),
        (N'Выпусков акции за день по отмеченным кампаниям'),
        (N'Рекламные окна, веер: отмечено кампаний — {0}'),
        (N'выпуски акции у всех отмеченных кампаний'),
        (N'не у всех'),
        (N'выпуски фирмы в других акциях на всех станциях'),
        (N'на части станций'),
        (N'выпуски акции не у всех отмеченных кампаний'),
        (N'выпуски фирмы в других акциях на части станций'),
        (N'Нет права «Редактировать» на одну из отмеченных кампаний.'),
        (N'Ролик на все отмеченные станции ставится в «Режиме добавления»: щёлкните слот.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы веера (es) на месте.';
    ELSE
        RAISERROR(N'44: часть переводов веера не записана.', 16, 1);
END
GO
