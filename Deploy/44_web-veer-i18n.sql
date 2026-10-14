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
        (N'Дубли:', N'Duplicados:'),
        (N'ролик {0} ({1} шт.)', N'spot {0} ({1} uds.)'),
        (N'{0} ({1} шт.)', N'{0} ({1} uds.)'),
        (N'Показывать в окнах номера роликов отмеченных кампаний вместо остатка времени', N'Mostrar en las ventanas los números de spots de las pautas marcadas en lugar del tiempo libre'),
        (N'«Н» — число ролика не одинаково у кампаний, «Д» — дубли', N'«Н»: la cantidad del spot no es la misma en todas las pautas; «Д»: duplicados'),
        (N'Добавить до полного пересечения', N'Agregar hasta la intersección completa'),
        (N'Добавить ролики до полного пересечения', N'Agregar spots hasta la intersección completa'),
        (N'Удалить дубли', N'Eliminar duplicados'),
        (N'Удалить дубли роликов из выделенных окон', N'Eliminar los spots duplicados de las ventanas seleccionadas'),
        (N'Доступно только при включённом показе номеров роликов (кнопка "Номера роликов") — так видно, что меняется.', N'Disponible solo con los números de spots activados (botón "Números de spots"): así se ve qué cambia.'),
        (N'Удалять нечего: все окна с дублями пропущены.', N'No hay nada que eliminar: se omitieron todas las ventanas con duplicados.'),
        (N'В выделенных окнах дублей нет.', N'En las ventanas seleccionadas no hay duplicados.'),
        (N'Удалить дубли в выделенных окнах, оставив у каждой кампании по одному выпуску каждого ролика? ({0} шт.)', N'¿Eliminar los duplicados en las ventanas seleccionadas, dejando en cada pauta una emisión de cada spot? ({0} uds.)'),
        (N'Окон будет пропущено из-за разного позиционирования дублей: {0}.', N'Se omitirán ventanas por posicionamiento distinto de los duplicados: {0}.'),
        (N'Окно пропущено: у дублей разное позиционирование — {0}', N'Ventana omitida: los duplicados tienen posicionamiento distinto — {0}'),
        (N'ролик {0}', N'spot {0}'),
        (N'ролик «{0}»', N'spot «{0}»'),
        (N'Удалено лишних выпусков: {0}, не удалено: {1}', N'Emisiones sobrantes eliminadas: {0}, no eliminadas: {1}'),
        (N'Добавить недостающие ролики в выделенных окнах до полного пересечения? ({0} шт.)', N'¿Agregar los spots que faltan en las ventanas seleccionadas hasta la intersección completa? ({0} uds.)'),
        (N'В выделенных окнах количество роликов уже совпадает у всех выбранных кампаний.', N'En las ventanas seleccionadas la cantidad de spots ya coincide en todas las pautas marcadas.'),
        (N'Из них без позиционирования (позиция в окне занята): {0}.', N'De ellos sin posicionamiento (la posición en la ventana está ocupada): {0}.'),
        (N'Не удалено {0} шт.: {1}', N'No eliminadas {0} uds.: {1}'),
        (N'Окна выделяются щелчком (Shift — прямоугольник, Ctrl — добавить); ролик на все станции одним щелчком ставит «Режим добавления».', N'Las ventanas se seleccionan con un clic (Mayús: rectángulo, Ctrl: agregar); el «Modo de agregado» coloca el spot en todas las emisoras con un solo clic.'),
        (N'Разместить ролик в выбранных окнах по выбранным кампаниям? ({0} шт.)', N'¿Colocar el spot en las ventanas seleccionadas, en las pautas marcadas? ({0} uds.)'),
        (N'Удалить выпуски в выбранных окнах по выбранным кампаниям? ({0} шт.)', N'¿Eliminar las emisiones de las ventanas seleccionadas, en las pautas marcadas? ({0} uds.)'),
        (N'В выбранных окнах нет выпусков этой акции.', N'En las ventanas seleccionadas no hay emisiones de esta campaña.')
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
        (N'Дубли:'),
        (N'ролик {0} ({1} шт.)'),
        (N'{0} ({1} шт.)'),
        (N'Показывать в окнах номера роликов отмеченных кампаний вместо остатка времени'),
        (N'«Н» — число ролика не одинаково у кампаний, «Д» — дубли'),
        (N'Добавить до полного пересечения'),
        (N'Добавить ролики до полного пересечения'),
        (N'Удалить дубли'),
        (N'Удалить дубли роликов из выделенных окон'),
        (N'Доступно только при включённом показе номеров роликов (кнопка "Номера роликов") — так видно, что меняется.'),
        (N'Удалять нечего: все окна с дублями пропущены.'),
        (N'В выделенных окнах дублей нет.'),
        (N'Удалить дубли в выделенных окнах, оставив у каждой кампании по одному выпуску каждого ролика? ({0} шт.)'),
        (N'Окон будет пропущено из-за разного позиционирования дублей: {0}.'),
        (N'Окно пропущено: у дублей разное позиционирование — {0}'),
        (N'ролик {0}'),
        (N'ролик «{0}»'),
        (N'Удалено лишних выпусков: {0}, не удалено: {1}'),
        (N'Добавить недостающие ролики в выделенных окнах до полного пересечения? ({0} шт.)'),
        (N'В выделенных окнах количество роликов уже совпадает у всех выбранных кампаний.'),
        (N'Из них без позиционирования (позиция в окне занята): {0}.'),
        (N'Не удалено {0} шт.: {1}'),
        (N'Окна выделяются щелчком (Shift — прямоугольник, Ctrl — добавить); ролик на все станции одним щелчком ставит «Режим добавления».'),
        (N'Разместить ролик в выбранных окнах по выбранным кампаниям? ({0} шт.)'),
        (N'Удалить выпуски в выбранных окнах по выбранным кампаниям? ({0} шт.)'),
        (N'В выбранных окнах нет выпусков этой акции.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы веера (es) на месте.';
    ELSE
        RAISERROR(N'44: часть переводов веера не записана.', 16, 1);
END
GO
