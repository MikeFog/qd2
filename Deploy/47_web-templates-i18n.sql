-- Веб, страница акции, вкладка «Рекламные окна»: Шаблоны 1–3 («Шаблон», этап 5 форм акции, docs/tasks/web-action-forms.md) и
-- «Отменить последнее» — испанские переводы новых строк (56). Только строки интерфейса: процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта шаблоны и отмена у пользователей с испанским языком работают, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 47_web-templates-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'47: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Отменить', N'Deshacer'),
        (N'Отменить последнее добавление выпусков', N'Deshacer la última adición de emisiones'),
        (N'Отменить последнее добавление выпусков? ({0} шт.)', N'¿Deshacer la última adición de emisiones? ({0} uds.)'),
        (N'Акция подтверждена: цена акции будет пересчитана, привязки оплат могут быть сокращены.', N'La campaña está confirmada: el precio de la campaña se recalculará y las vinculaciones de pagos pueden reducirse.'),
        (N'Отменено выпусков: {0}, не отменено: {1}', N'Emisiones deshechas: {0}, no deshechas: {1}'),
        (N'Отменять можно только выпуски этой кампании.', N'Solo se pueden deshacer emisiones de esta pauta.'),
        (N'Выпуска уже нет.', N'La emisión ya no existe.'),
        (N'Выпуск изменили после размещения: другой ролик или окно.', N'La emisión se modificó tras colocarla: otro spot u otra ventana.'),
        (N'Отменено выпусков: {0}.', N'Emisiones deshechas: {0}.'),
        (N'Начало интервала:', N'Inicio del intervalo:'),
        (N'Окончание  интервала:', N'Fin del intervalo:'),
        (N'Начало интервала (время):', N'Inicio del intervalo (hora):'),
        (N'Окончание  интервала (время):', N'Fin del intervalo (hora):'),
        (N'Четный/Нечетный', N'Par/Impar'),
        (N'Нечётные дни', N'Días impares'),
        (N'Чётные дни', N'Días pares'),
        (N'Количество выходов в день:', N'Cantidad de emisiones por día:'),
        (N'С разбивкой на прайм и офф прайм', N'Con desglose en prime y fuera de prime'),
        (N'Выходов в прайм:', N'Emisiones en prime:'),
        (N'Выходов в офф прайм:', N'Emisiones fuera de prime:'),
        (N'Не использовать окна где есть ролики данной фирмы', N'No usar ventanas donde hay spots de esta empresa'),
        (N'Укажите время начала и окончания интервала.', N'Indique la hora de inicio y de fin del intervalo.'),
        (N'Количество выходов — целое число от 0 до 100 (в день без разбивки — от 1).', N'La cantidad de emisiones es un número entero de 0 a 100 (por día sin desglose, desde 1).'),
        (N'Шаблон', N'Plantilla'),
        (N'Добавлено выпусков: {0}.', N'Emisiones agregadas: {0}.'),
        (N'Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}.', N'No hay suficientes ventanas publicitarias de prime para colocar todas las emisiones. Ventanas: {0}, emisiones {1}.'),
        (N'Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}.', N'No hay suficientes ventanas publicitarias fuera de prime para colocar todas las emisiones. Ventanas: {0}, emisiones por agregar {1}.'),
        (N'Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}.', N'No hay suficientes ventanas publicitarias para colocar todas las emisiones por agregar. Ventanas: {0}, emisiones {1}.'),
        (N'Дата окончания периода слишком большая.', N'La fecha de fin del período es demasiado grande.'),
        (N'Вы выбрали режим заполнения рекламных окон с разбивкой на прайм и офф прайм. Пожалуйчта, введите данные о количестве выходов в прайм и/или офф прайм.', N'Eligió el modo de llenado de ventanas con desglose en prime y fuera de prime. Introduzca la cantidad de emisiones en prime y/o fuera de prime.'),
        (N'Запуск прерван: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.', N'Ejecución interrumpida: {0} Las emisiones de este día no se agregaron; las agregadas antes se conservan. Días sin procesar: {1}.'),
        (N'Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.', N'No se realizó el recálculo de la campaña: {0} El precio de la campaña se actualizará con el siguiente cambio de la colocación.'),
        (N'Отмена прервана: {0} Не снято выпусков: {1}.', N'Deshacer interrumpido: {0} Emisiones sin eliminar: {1}.'),
        (N'Дата начала периода слишком ранняя.', N'La fecha de inicio del período es demasiado temprana.'),
        (N'Период шаблона слишком длинный: не больше 10 лет.', N'El período de la plantilla es demasiado largo: no más de 10 años.'),
        (N'Шаблон №1 для внесения роликов', N'Plantilla N.º 1 para colocar spots'),
        (N'Шаблон №2 для внесения роликов', N'Plantilla N.º 2 para colocar spots'),
        (N'Шаблон для внесения роликов', N'Plantilla para colocar spots'),
        (N'Укажите время выхода.', N'Indique la hora de emisión.'),
        (N'Выпуск ставится в окно, накрывающее это время.', N'La emisión se coloca en la ventana que abarca esta hora.'),
        (N'Шаблон №3 для внесения роликов', N'Plantilla N.º 3 para colocar spots'),
        (N'Общее количество выходов:', N'Cantidad total de emisiones:'),
        (N'Распределено выходов:', N'Emisiones distribuidas:'),
        (N'Общий хронометраж:', N'Cronometraje total:'),
        (N'У ролика «{0}» не указано количество', N'El spot «{0}» no tiene cantidad'),
        (N'Отметьте хотя бы один ролик и укажите для него количество', N'Marque al menos un spot e indique su cantidad'),
        (N'Сумма количества по роликам ({0}) не совпадает с общим количеством выходов по настройкам интервала ({1})', N'La suma de las cantidades por spot ({0}) no coincide con la cantidad total de emisiones según la configuración del intervalo ({1})'),
        (N'Недостаточно рекламных окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.', N'No hay suficientes ventanas publicitarias en todo el período de la plantilla: no se pudieron colocar {0} emisión(es) de los spots seleccionados.'),
        (N'Недостаточно рекламных окон для размещения всех выпусков. Окон: {0}, выпусков: {1}.', N'No hay suficientes ventanas publicitarias para colocar todas las emisiones. Ventanas: {0}, emisiones: {1}.'),
        (N'Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.', N'No hay suficientes ventanas publicitarias de prime para colocar todas las emisiones. Ventanas: {0}, emisiones: {1}.'),
        (N'Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.', N'No hay suficientes ventanas publicitarias fuera de prime para colocar todas las emisiones. Ventanas: {0}, emisiones: {1}.'),
        (N'Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.', N'No hay suficientes franjas para colocar. Añadido: {0}, se requería: {1}.'),
        (N'Недостаточно окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.', N'No hay suficientes ventanas en todo el período de la plantilla: no se pudieron colocar {0} emisión(es) de los spots seleccionados.'),
        (N'Выпуск ставится в получасовой слот, накрывающий это время (время округляется вниз до получаса), на всех отмеченных станциях.', N'La emisión se coloca en la franja de media hora que cubre esta hora (la hora se redondea hacia abajo a la media hora), en todas las emisoras marcadas.'),
        (N'Отменять можно только выпуски отмеченных кампаний.', N'Solo se pueden deshacer emisiones de las pautas marcadas.'),
        (N'Выпуски этого дня поставлены, но «Отменить последнее» их не снимет: не удалось прочитать поставленное. Снять их можно через «Удалить выпуски».', N'Las emisiones de este día se colocaron, pero «Deshacer» no las quitará: no se pudo leer lo colocado. Se pueden quitar con «Eliminar emisiones».')
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
        (N'Отменить'),
        (N'Отменить последнее добавление выпусков'),
        (N'Отменить последнее добавление выпусков? ({0} шт.)'),
        (N'Акция подтверждена: цена акции будет пересчитана, привязки оплат могут быть сокращены.'),
        (N'Отменено выпусков: {0}, не отменено: {1}'),
        (N'Отменять можно только выпуски этой кампании.'),
        (N'Выпуска уже нет.'),
        (N'Выпуск изменили после размещения: другой ролик или окно.'),
        (N'Отменено выпусков: {0}.'),
        (N'Начало интервала:'),
        (N'Окончание  интервала:'),
        (N'Начало интервала (время):'),
        (N'Окончание  интервала (время):'),
        (N'Четный/Нечетный'),
        (N'Нечётные дни'),
        (N'Чётные дни'),
        (N'Количество выходов в день:'),
        (N'С разбивкой на прайм и офф прайм'),
        (N'Выходов в прайм:'),
        (N'Выходов в офф прайм:'),
        (N'Не использовать окна где есть ролики данной фирмы'),
        (N'Укажите время начала и окончания интервала.'),
        (N'Количество выходов — целое число от 0 до 100 (в день без разбивки — от 1).'),
        (N'Шаблон'),
        (N'Добавлено выпусков: {0}.'),
        (N'Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}.'),
        (N'Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}.'),
        (N'Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}.'),
        (N'Дата окончания периода слишком большая.'),
        (N'Вы выбрали режим заполнения рекламных окон с разбивкой на прайм и офф прайм. Пожалуйчта, введите данные о количестве выходов в прайм и/или офф прайм.'),
        (N'Запуск прерван: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.'),
        (N'Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.'),
        (N'Отмена прервана: {0} Не снято выпусков: {1}.'),
        (N'Дата начала периода слишком ранняя.'),
        (N'Период шаблона слишком длинный: не больше 10 лет.'),
        (N'Шаблон №1 для внесения роликов'),
        (N'Шаблон №2 для внесения роликов'),
        (N'Шаблон для внесения роликов'),
        (N'Укажите время выхода.'),
        (N'Выпуск ставится в окно, накрывающее это время.'),
        (N'Шаблон №3 для внесения роликов'),
        (N'Общее количество выходов:'),
        (N'Распределено выходов:'),
        (N'Общий хронометраж:'),
        (N'У ролика «{0}» не указано количество'),
        (N'Отметьте хотя бы один ролик и укажите для него количество'),
        (N'Сумма количества по роликам ({0}) не совпадает с общим количеством выходов по настройкам интервала ({1})'),
        (N'Недостаточно рекламных окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.'),
        (N'Недостаточно рекламных окон для размещения всех выпусков. Окон: {0}, выпусков: {1}.'),
        (N'Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.'),
        (N'Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.'),
        (N'Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.'),
        (N'Недостаточно окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.'),
        (N'Выпуск ставится в получасовой слот, накрывающий это время (время округляется вниз до получаса), на всех отмеченных станциях.'),
        (N'Отменять можно только выпуски отмеченных кампаний.'),
        (N'Выпуски этого дня поставлены, но «Отменить последнее» их не снимет: не удалось прочитать поставленное. Снять их можно через «Удалить выпуски».')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы шаблонов и отмены (es) на месте.';
    ELSE
        RAISERROR(N'47: часть переводов шаблонов не записана.', 16, 1);
END
GO
