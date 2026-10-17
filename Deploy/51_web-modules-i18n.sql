-- Веб, страница акции, вкладка «Модули» (этап 6 форм акции, docs/tasks/web-action-forms.md: сетка модулей, «Показать модули»,
-- постановка кликом, «Разместить ролик» и «Удалить выпуски» по выделению, действия над выпуском модуля, «Отменить последнее»,
-- мастер «Размещение комбо-модулями») — испанские переводы новых строк (60). Только строки интерфейса: процедуры, таблицы
-- и метаданные не меняются.
--
-- Без этого скрипта модули у пользователей с испанским языком работают, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 51_web-modules-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'51: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Предыдущий месяц', N'Mes anterior'),
        (N'Следующий месяц', N'Mes siguiente'),
        (N'Модулей нет', N'No hay módulos'),
        (N'Строки — модули, размещённые в отмеченных модульных кампаниях; у кампании без выпусков — модули её радиостанции, действующие в этом периоде.', N'Las filas son los módulos colocados en las pautas modulares marcadas; en una pauta sin emisiones, los módulos de su emisora vigentes en este período.'),
        (N'Процедура ComboModuleFreeTimeRetrieve в базе старая: нет набора модулей @moduleIDs. Нужно установить Deploy/50.', N'El procedimiento ComboModuleFreeTimeRetrieve de la base de datos está desactualizado: no admite el conjunto de módulos @moduleIDs. Hay que instalar Deploy/50.'),
        (N'Место не относится к отмеченным модульным кампаниям. Обновите страницу.', N'La celda no pertenece a las pautas modulares marcadas. Actualice la página.'),
        (N'Отметить все модульные кампании акции', N'Marcar todas las pautas modulares de la campaña'),
        (N'Строки — модули отмеченных кампаний, колонки — дни недели или месяца, в ячейке — остаток самого заполненного окна модуля за день.', N'Filas: los módulos de las pautas marcadas; columnas: los días de la semana o del mes; en la celda, el tiempo libre de la ventana más ocupada del módulo en el día.'),
        (N'В акции нет модульных рекламных кампаний.', N'La campaña no tiene pautas publicitarias modulares.'),
        (N'Выпусков модулей за день по отмеченным кампаниям', N'Emisiones de módulos en el día, en las pautas marcadas'),
        (N'Показывать неделю или месяц', N'Mostrar la semana o el mes'),
        (N'Неделя', N'Semana'),
        (N'Месяц', N'Mes'),
        (N'выпуски модуля в этот день', N'emisiones del módulo en este día'),
        (N'модуль в этот день не выходит целиком', N'el módulo no sale completo este día'),
        (N'Выпуски модуля', N'Emisiones del módulo'),
        (N'В этот день модуль в кампании не стоит.', N'Este día el módulo no está colocado en la pauta.'),
        (N'Окна выхода', N'Ventanas de emisión'),
        (N'Окна модуля', N'Ventanas del módulo'),
        (N'Модули: {0}', N'Módulos: {0}'),
        (N'Модули: отмечено кампаний — {0}', N'Módulos: pautas marcadas — {0}'),
        (N'Остаток времени', N'Tiempo libre'),
        (N'Прайс-лист модуля «{0}» на {1:dd.MM.yyyy} изменился. Обновите страницу.', N'La lista de precios del módulo «{0}» para el {1:dd.MM.yyyy} cambió. Actualice la página.'),
        (N'Ролик «{0}» нельзя поставить в модуль: в модули ставятся только ролики типов «Реклама», «Новость» и «Программа».', N'El spot «{0}» no se puede colocar en un módulo: en los módulos solo se colocan spots de los tipos «Publicidad», «Noticia» y «Programa».'),
        (N'Модуль «{0}» ({1}) не выходит {2:dd.MM.yyyy} целиком, выпуск не создан.', N'El módulo «{0}» ({1}) no sale completo el {2:dd.MM.yyyy}; no se creó la emisión.'),
        (N'Ролик, который ставится в модули, — выбран в списке «Ролики»', N'Spot que se coloca en los módulos: el elegido en la lista «Spots»'),
        (N'Модулей: {0}', N'Módulos: {0}'),
        (N'Выберите, какие модули показать.', N'Seleccione qué módulos mostrar.'),
        (N'У радиостанций нет агентства, их модули показаны не будут: {0}.', N'Las emisoras no tienen agencia, sus módulos no se mostrarán: {0}.'),
        (N'Рекламная кампания на радиостанции с этим типом оплаты и агентством будет создана при первой постановке модуля, если её ещё нет в акции.', N'La pauta publicitaria en la emisora con este tipo de pago y esta agencia se creará al colocar el primer módulo, si aún no existe en la campaña.'),
        (N'Модули радиостанции', N'Módulos de la emisora'),
        (N'Модули рекламной кампании', N'Módulos de la pauta publicitaria'),
        (N'На эту дату у радиостанции нет модулей с прайс-листом.', N'En esta fecha la emisora no tiene módulos con lista de precios.'),
        (N'Отмечено модулей: {0}', N'Módulos marcados: {0}'),
        (N'Выберите рекламную кампанию.', N'Seleccione la pauta publicitaria.'),
        (N'Отметьте хотя бы один модуль.', N'Marque al menos un módulo.'),
        (N'Выберите комбо-модуль.', N'Seleccione el módulo combo.'),
        (N'Выберите радиостанцию.', N'Seleccione la emisora.'),
        (N'Ни у одной радиостанции нет агентства — показывать нечего.', N'Ninguna de las emisoras tiene agencia: no hay nada que mostrar.'),
        (N'Модульных рекламных кампаний в акции нет', N'La campaña no tiene pautas publicitarias modulares'),
        (N'Добавьте строки кнопкой «Показать модули» — модули комбо-модуля или радиостанции: рекламная кампания на радиостанции будет создана при первой постановке модуля.', N'Agregue filas con el botón «Mostrar módulos» (los módulos de un módulo combo o de una emisora): la pauta publicitaria en la emisora se creará al colocar el primer módulo.'),
        (N'Добавить в сетку модули комбо-модуля, радиостанции или отмеченной рекламной кампании', N'Agregar a la cuadrícula los módulos de un módulo combo, de una emisora o de la pauta publicitaria marcada'),
        (N'Отметьте модульную рекламную кампанию слева.', N'Marque una pauta publicitaria modular a la izquierda.'),
        (N'Показано модулей: {0}, не показано радиостанций: {1}', N'Módulos mostrados: {0}, emisoras no mostradas: {1}'),
        (N'Модули: строки без рекламной кампании', N'Módulos: filas sin pauta publicitaria'),
        (N'Размещение прервано: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.', N'Colocación interrumpida: {0} Las emisiones de este día no se agregaron; las agregadas antes se conservan. Días sin procesar: {1}.'),
        (N'Удаление прервано: {0} Выпуски этого дня не удалены, выпуски прежних дней удалены. Не обработано дней: {1}.', N'Eliminación interrumpida: {0} Las emisiones de este día no se eliminaron; las de los días anteriores sí se eliminaron. Días sin procesar: {1}.'),
        (N'Выделено ячеек: {0}', N'Celdas seleccionadas: {0}'),
        (N'Разместить ролик в выбранных ячейках? ({0} шт.)', N'¿Colocar el spot en las celdas seleccionadas? ({0} uds.)'),
        (N'В выбранных ячейках нет выпусков этой акции.', N'En las celdas seleccionadas no hay emisiones de esta campaña.'),
        (N'Какие ролики удалить в выбранных ячейках?', N'¿Qué spots eliminar en las celdas seleccionadas?'),
        (N'Удалить выпуски в выбранных ячейках? ({0} шт.)', N'¿Eliminar las emisiones de las celdas seleccionadas? ({0} uds.)'),
        (N'Нет права удалять выпуски модулей.', N'No tiene permiso para eliminar emisiones de módulos.'),
        (N'Щелчок по ячейке — выпуски и окна модуля за день (Shift — прямоугольник, Ctrl — добавить), в «Режиме добавления» — постановка модуля на весь день.', N'Clic en una celda: las emisiones y las ventanas del módulo en el día (Mayús: rectángulo, Ctrl: agregar); en el «Modo de agregado», colocar el módulo para todo el día.'),
        (N'Ролик выпуска изменили после вопроса — выпуск не удалён.', N'El spot de la emisión se cambió después de la pregunta: la emisión no se eliminó.'),
        (N'Модуль снимается на весь день — окон: {0}.', N'El módulo se quita para todo el día: ventanas: {0}.'),
        (N'Выпуск изменили после размещения: другой ролик, модуль или день.', N'La emisión se modificó tras colocarla: otro spot, otro módulo u otro día.'),
        (N'У радиостанций нет агентства, их модули добавлены не будут: {0}.', N'Las emisoras no tienen agencia, sus módulos no se agregarán: {0}.'),
        (N'В комбо-модуле нет модулей.', N'El módulo combo no tiene módulos.'),
        (N'Ни у одной радиостанции нет агентства — разместить комбо-модуль нельзя.', N'Ninguna emisora tiene agencia: no se puede colocar el módulo combo.')
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
        (N'Предыдущий месяц'),
        (N'Следующий месяц'),
        (N'Модулей нет'),
        (N'Строки — модули, размещённые в отмеченных модульных кампаниях; у кампании без выпусков — модули её радиостанции, действующие в этом периоде.'),
        (N'Процедура ComboModuleFreeTimeRetrieve в базе старая: нет набора модулей @moduleIDs. Нужно установить Deploy/50.'),
        (N'Место не относится к отмеченным модульным кампаниям. Обновите страницу.'),
        (N'Отметить все модульные кампании акции'),
        (N'Строки — модули отмеченных кампаний, колонки — дни недели или месяца, в ячейке — остаток самого заполненного окна модуля за день.'),
        (N'В акции нет модульных рекламных кампаний.'),
        (N'Выпусков модулей за день по отмеченным кампаниям'),
        (N'Показывать неделю или месяц'),
        (N'Неделя'),
        (N'Месяц'),
        (N'выпуски модуля в этот день'),
        (N'модуль в этот день не выходит целиком'),
        (N'Выпуски модуля'),
        (N'В этот день модуль в кампании не стоит.'),
        (N'Окна выхода'),
        (N'Окна модуля'),
        (N'Модули: {0}'),
        (N'Модули: отмечено кампаний — {0}'),
        (N'Остаток времени'),
        (N'Прайс-лист модуля «{0}» на {1:dd.MM.yyyy} изменился. Обновите страницу.'),
        (N'Ролик «{0}» нельзя поставить в модуль: в модули ставятся только ролики типов «Реклама», «Новость» и «Программа».'),
        (N'Модуль «{0}» ({1}) не выходит {2:dd.MM.yyyy} целиком, выпуск не создан.'),
        (N'Ролик, который ставится в модули, — выбран в списке «Ролики»'),
        (N'Модулей: {0}'),
        (N'Выберите, какие модули показать.'),
        (N'У радиостанций нет агентства, их модули показаны не будут: {0}.'),
        (N'Рекламная кампания на радиостанции с этим типом оплаты и агентством будет создана при первой постановке модуля, если её ещё нет в акции.'),
        (N'Модули радиостанции'),
        (N'Модули рекламной кампании'),
        (N'На эту дату у радиостанции нет модулей с прайс-листом.'),
        (N'Отмечено модулей: {0}'),
        (N'Выберите рекламную кампанию.'),
        (N'Отметьте хотя бы один модуль.'),
        (N'Выберите комбо-модуль.'),
        (N'Выберите радиостанцию.'),
        (N'Ни у одной радиостанции нет агентства — показывать нечего.'),
        (N'Модульных рекламных кампаний в акции нет'),
        (N'Добавьте строки кнопкой «Показать модули» — модули комбо-модуля или радиостанции: рекламная кампания на радиостанции будет создана при первой постановке модуля.'),
        (N'Добавить в сетку модули комбо-модуля, радиостанции или отмеченной рекламной кампании'),
        (N'Отметьте модульную рекламную кампанию слева.'),
        (N'Показано модулей: {0}, не показано радиостанций: {1}'),
        (N'Модули: строки без рекламной кампании'),
        (N'Размещение прервано: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.'),
        (N'Удаление прервано: {0} Выпуски этого дня не удалены, выпуски прежних дней удалены. Не обработано дней: {1}.'),
        (N'Выделено ячеек: {0}'),
        (N'Разместить ролик в выбранных ячейках? ({0} шт.)'),
        (N'В выбранных ячейках нет выпусков этой акции.'),
        (N'Какие ролики удалить в выбранных ячейках?'),
        (N'Удалить выпуски в выбранных ячейках? ({0} шт.)'),
        (N'Нет права удалять выпуски модулей.'),
        (N'Щелчок по ячейке — выпуски и окна модуля за день (Shift — прямоугольник, Ctrl — добавить), в «Режиме добавления» — постановка модуля на весь день.'),
        (N'Ролик выпуска изменили после вопроса — выпуск не удалён.'),
        (N'Модуль снимается на весь день — окон: {0}.'),
        (N'Выпуск изменили после размещения: другой ролик, модуль или день.'),
        (N'У радиостанций нет агентства, их модули добавлены не будут: {0}.'),
        (N'В комбо-модуле нет модулей.'),
        (N'Ни у одной радиостанции нет агентства — разместить комбо-модуль нельзя.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы модулей (es) на месте.';
    ELSE
        RAISERROR(N'51: часть переводов модулей не записана.', 16, 1);
END
GO
