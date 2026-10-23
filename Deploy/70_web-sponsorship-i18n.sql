-- Веб, вкладка «Спонсорство» страницы акции (этап 7 форм акции, docs/tasks/web-action-forms.md, 22.10.2026):
-- испанские переводы 29 новых строк интерфейса — полоса программ радиостанции на неделю (надписи места «Спонсирует:»,
-- «Макет:», «Макеты:», приписка «+N макет(а, ов)», легенда, подсказки), поле «Предмет рекламы» выпусков программ
-- («не выбран», «Убрать предмет рекламы»), строка «Программ» в статистике кампании, заголовок «Бонусные ролики:»,
-- отказы при щелчке по программе («Выпуск программы в этом месте уже стоит…», «Выпуска программы уже нет…», «Выпуск
-- изменили после размещения…», «Место не относится к спонсорской кампании вкладки…», «Нет права «Редактировать»…»)
-- и текст десктопа «Для редактирования активированной спонсорской кампании необходимо выбрать предмет рекламы.»
-- (ресурс AdvertTypeMastBeSelected — отказ у подтверждённой акции без предмета рекламы). Только строки интерфейса:
-- процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта вкладка у пользователей с испанским языком работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 70_web-sponsorship-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'70: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'+{0} макет', N'+{0} borrador'),
        (N'+{0} макета', N'+{0} borradores'),
        (N'+{0} макетов', N'+{0} borradores'),
        (N'Бонусные ролики: {0}', N'Spots de bonificación: {0}'),
        (N'В акции нет спонсорских рекламных кампаний.', N'La campaña no tiene pautas publicitarias de patrocinio.'),
        (N'Выпуск изменили после размещения: другая программа или время.', N'La emisión se modificó después de colocarla: otro programa u otra hora.'),
        (N'Выпуск программы в этом месте уже стоит. Обновите страницу.', N'Ya hay una emisión del programa en este lugar. Actualice la página.'),
        (N'Выпуска программы уже нет. Обновите страницу.', N'La emisión del programa ya no existe. Actualice la página.'),
        (N'Выпусков программ этой кампании за день', N'Emisiones de programas de esta pauta en el día'),
        (N'Макет', N'Borrador'),
        (N'Макет: {0}', N'Borrador: {0}'),
        (N'Макеты: {0}', N'Borradores: {0}'),
        (N'Место не относится к спонсорской кампании вкладки. Обновите страницу.', N'El lugar no pertenece a la pauta de patrocinio de la pestaña. Actualice la página.'),
        (N'На эту неделю у радиостанции нет спонсорских программ.', N'La emisora no tiene programas de patrocinio para esta semana.'),
        (N'Нет права «Редактировать» на эту акцию или «Редактировать в эфир рекламных роликов» спонсорской кампании.', N'No tiene el permiso «Editar» para esta campaña o «Editar salida al aire de spots publicitarios» para la pauta de patrocinio.'),
        (N'Предмет рекламы выпусков программ, которые ставит щелчок. Акция подтверждена — без него программу не поставить.', N'Rubro publicitario de las emisiones de programas que coloca el clic. La campaña está confirmada: sin él no se puede colocar el programa.'),
        (N'Предмет рекламы выпусков программ, которые ставит щелчок. У макета необязателен: перед активацией его спросят.', N'Rubro publicitario de las emisiones de programas que coloca el clic. En un borrador es opcional: se pedirá antes de la activación.'),
        (N'Программ', N'Programas'),
        (N'Программы', N'Programas'),
        (N'Программы и их прайс-листы ведутся в справочнике радиостанции.', N'Los programas y sus listas de precios se gestionan en la ficha de la emisora.'),
        (N'Спонсирует: {0}', N'Patrocina: {0}'),
        (N'Спонсорская кампания — программы её радиостанции на неделю и рекламные окна для бонусных роликов.', N'Pauta de patrocinio: los programas de su emisora en la semana y las ventanas publicitarias para los spots de bonificación.'),
        (N'Убрать предмет рекламы', N'Quitar el rubro publicitario'),
        (N'Щелчок по программе ставит или снимает её выпуск. Для бонусных роликов выберите ролик в списке «Ролики».', N'Un clic en el programa coloca o quita su emisión. Para los spots de bonificación, elija el spot en la lista «Spots».'),
        (N'выпуск программы этой кампании', N'emisión del programa de esta pauta'),
        (N'имя фирмы — программу спонсирует другая подтверждённая акция', N'nombre de la empresa: el programa lo patrocina otra campaña confirmada'),
        (N'макеты других акций', N'borradores de otras campañas'),
        (N'не выбран', N'no seleccionado'),
        (N'Для редактирования активированной спонсорской кампании необходимо выбрать предмет рекламы.', N'Para editar una pauta de patrocinio activada es necesario seleccionar el rubro publicitario.')
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
        (N'+{0} макет'),
        (N'+{0} макета'),
        (N'+{0} макетов'),
        (N'Бонусные ролики: {0}'),
        (N'В акции нет спонсорских рекламных кампаний.'),
        (N'Выпуск изменили после размещения: другая программа или время.'),
        (N'Выпуск программы в этом месте уже стоит. Обновите страницу.'),
        (N'Выпуска программы уже нет. Обновите страницу.'),
        (N'Выпусков программ этой кампании за день'),
        (N'Макет'),
        (N'Макет: {0}'),
        (N'Макеты: {0}'),
        (N'Место не относится к спонсорской кампании вкладки. Обновите страницу.'),
        (N'На эту неделю у радиостанции нет спонсорских программ.'),
        (N'Нет права «Редактировать» на эту акцию или «Редактировать в эфир рекламных роликов» спонсорской кампании.'),
        (N'Предмет рекламы выпусков программ, которые ставит щелчок. Акция подтверждена — без него программу не поставить.'),
        (N'Предмет рекламы выпусков программ, которые ставит щелчок. У макета необязателен: перед активацией его спросят.'),
        (N'Программ'),
        (N'Программы'),
        (N'Программы и их прайс-листы ведутся в справочнике радиостанции.'),
        (N'Спонсирует: {0}'),
        (N'Спонсорская кампания — программы её радиостанции на неделю и рекламные окна для бонусных роликов.'),
        (N'Убрать предмет рекламы'),
        (N'Щелчок по программе ставит или снимает её выпуск. Для бонусных роликов выберите ролик в списке «Ролики».'),
        (N'выпуск программы этой кампании'),
        (N'имя фирмы — программу спонсирует другая подтверждённая акция'),
        (N'макеты других акций'),
        (N'не выбран'),
        (N'Для редактирования активированной спонсорской кампании необходимо выбрать предмет рекламы.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы вкладки «Спонсорство» (es) на месте.';
    ELSE
        RAISERROR(N'70: часть переводов вкладки «Спонсорство» не записана.', 16, 1);
END
GO
