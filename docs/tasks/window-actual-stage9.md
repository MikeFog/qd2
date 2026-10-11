# Этап 9 перехода на фактическое окно: чистка

Статус: разбор 11.10.2026, ничего не сделано. Решение Миши: этап 9 отложен, сначала задачи заказчика. Работу программы
он не меняет, пользователь ничего не заметит.

План перехода — `docs/tasks/window-actual-switch.md` (строка «9. Чистка» в §5.1), открытые вопросы —
`docs/tasks/window-actual-open-questions.md` (О-5, О-6). Сокращения в ссылках — как там: `SP/` = `ArtvisDB/dbo/Stored Procedures/`,
`Cls/` = `Client/Classes/`, `Ctl/` = `Client/Controls/`, `Frm/` = `Client/Forms/`, `Web/` = `FogSoft.Web/Infrastructure/`.

Как проверялось: каждый пункт разобран по коду, метаданным (`iEntity*`, `iStoredProcedure`/`iModuleProcedure` — конфигурация
`DoAction`), отчётам Crystal (`grep -a` по `*.rpt`), вебу, другим проектам решения, `sys.sql_modules` и
`sys.sql_expression_dependencies` на копиях ArtvisDev, Artvis и Tumen, копиям прод-лога. К каждому разбору — отдельная проверка,
искавшая пропущенных потребителей; главные выводы подтвердились. Копии Artvis (данные до 28.09) и Tumen (до 06.10) — снимки,
скриптов 24–34 в них нет; о реальных проде и Tumen — по `Deploy/README.md`.

## 1. Коротко

Пункты двух видов, и цена у них разная:
- **мёртвый код** — дешёвый, почти без риска, только SQL, клиент не нужен (п. 4–6);
- **переименования** — косметика, и все упираются в одно условие: на всех базах работает новый клиент (п. 1–3). Проверить это
  сейчас нельзя (§2).

| № | Пункт | Совет | Объём |
|---|---|---|---|
| 1 | `Grid`: двойная колонка (О-5) | позже, когда новый клиент везде; можно не делать — ничего не ломает | ~2 ч |
| 2 | `RangeSlotIssues`: `originalWindowID`/`windowDayOriginal` | поля C# — с ближайшей сборкой; SQL — при переезде веера в ядро для веба | 15 мин / 1–1,5 ч |
| 3 | «Незамененные ролики»: `windowDateOriginal` (сущность 212) | приложить к следующей правке `RollerSubstitute` по делу | ~15 мин попутно |
| 4 | Медиаплан: ветки `isFact = 0` | сделать: ветки убрать, параметр оставить | 1,5–2 ч |
| 5 | MediaPlus и Граммофон | сделать | 2–3 ч |
| 6 | `job_DeleteHistory` (О-6) | удалить после проверки планировщика на сервере | ~1 ч |

## 2. Общее условие для переименований: какие базы и какой клиент

- Баз не три. `Deploy/README.md` кроме Artvis, Tumen и Belgorod называет Artvis2 и Univer («и т. д.»). Belgorod там
  «(девелоперская)», локальная копия выключена (`OFFLINE`); про скрипты 07–35 на Belgorod записей нет. Список
  «что не выложено на Belgorod» (требование О-4) не ведётся.
- Версию клиента лог не пишет (`Cls/Launcher.cs`, `Logs/qd2.log`). У клиента одна строка подключения (`Client/app.config:32`),
  значит на каждую базу своя установка Merlin.exe (вывод, на сервере не проверено).
- Условие «новый клиент» надо понимать как «**перезапущены все работающие процессы**»: пользователи сидят по RDP, после замены exe
  открытые программы работают старой сборкой. Например, катить SQL на следующее утро после замены exe.
- Сигнатуры процедур не менять: `SqlHelperParameterCache` хранит набор параметров в запущенном процессе
  (`FogSoft.WinForm/DataAccess/DataAccessor.cs:813-818`), удалённый параметр даст ошибку 8145 до перезапуска. Лишний ключ,
  наоборот, безопасен: `DataAccessor` берёт параметры из процедуры и лишние отбрасывает.

## 3. Пункты из плана

### 3.1 `Grid`: двойная колонка (О-5)

**Сейчас.**
- Первая выборка отдаёт `originalWindowID` (`SP/Grid.sql:39`, исходное окно) и `actualWindowID` (`:40`, окно выхода); `SELECT *`
  наружу (`:75-77`). Проверено: кампания 411526, 12–18.10.2026, выпуск в 09:45 — original 13076962, actual 13076965.
- Одна точка вызова — `Cls/Massmedia.cs:190-203` (`GetRollerCells`, сущность 100 + Grid + Load → процедура 85). Её зовут только
  `Ctl/RollerIssuesGrid3.cs:302` (сетка кампании) и `Cls/WindowsSource.cs:187` (веб, «Рекламные окна», файл в `FogSoft.Core`).
  `TrafficGrid` Grid не вызывает (`base(false)`, `Ctl/TrafficGrid.cs:46`). `Grid.rpt` — это сетка вещания на `rpt_Grid_v3`.
- Оба читателя берут из первой выборки только колонку окна и `rollerID`, с запасным путём «нет `actualWindowId` — бери
  `originalWindowId`» (`RollerIssuesGrid3.cs:25-27,307-309`, `WindowsSource.cs:229-234`).
- Колонки `rollerDuration`, `windowDateOriginal`, `timeString`, `campaignID`, `positionId` не читала ни одна версия клиента в git
  (с be12906). Это и есть «переименовать или убрать `:34,42,50,51`» из карты §2.1.
- Ветка `@campaignID = -1` (`:7,23,70,84`) мёртвая: без кампании до вызова не доходит, веб всегда передаёт кампанию; в логах
  620 + 45 вызовов, ни одного с -1. При фильтре `:70` строка `:71` (`OR i.campaignID = @campaignID`) всегда истинна —
  `@showUnconfirmed` на наборы 1–2 не влияет. `@position` (`:8`) в теле не используется.

**Что сломается.** Клиент, собранный до 177ba3a (08.10.2026), читает `originalWindowId` напрямую. Без колонки: при открытии формы
исключение ловит `CampaignForm_Load` — форма остаётся полуинициализированной (без модулей, кнопок окон, перетаскивания,
статистики) и показывает ошибку; при перелистывании недели — сообщение об ошибке и сетка без синих отметок и счётчиков
(`TariffGrid.cs:600-603`). Ошибка только если в неделе есть выпуск кампании. Процесс не падает.

**Правки.**
- SQL: убрать `originalWindowID` из `#res`, INCLUDE и INSERT (`:39,43,48-49,58`); по желанию — пять непрочитанных колонок и JOIN
  Roller (`FK_Issue_Roller` доверенный, `rollerID NOT NULL`), наружу явный список `actualWindowID, rollerID`; ветку -1
  (`:23,70-71,84`). Сигнатуру не трогать (`@campaignID int = -1`, `@position` остаются).
- Скрипт: защита — без текста Deploy/25 (`ON i.actualWindowID = tw.windowId AND tw.massmediaID = @massmediaID`) отказ; в конце
  `OBJECT_DEFINITION NOT LIKE '%i.originalWindowID%'`. В README: после него не перезапускать 25 и 11.
- Клиент и веб: убрать запасной путь (`RollerIssuesGrid3.cs:25-27,307-309`, `WindowsSource.cs:229-234`) и проверку четвёртого
  набора (`Tables.Count > 3`, `RollerIssuesGrid3.cs:329-331`, `WindowsSource.cs:256-257`). Константу
  `TariffWindow.ParamNames.OriginalWindowId` не трогать — она нужна замене ролика.
- Документы: `docs/tariffgrid.md:189,226-230`; `docs/scenarios/campaign-edit-form-load.md:97,141`; `docs/tasks/web-tariffgrid.md:230-231`;
  `docs/window-original-actual.md:164,205`; `Deploy/README.md:34`; О-5.

**Порядок.** 1) На базе все работающие клиенты не старше 177ba3a. 2) Скрипт SQL. 3) Клиент без запасного пути — когда скрипт
стоит на всех базах (иначе новый клиент упадёт на базе без Deploy/25 — возможно, Belgorod). Веб не ограничивает: прод-веб собран
без `WindowsSource.cs`, на Tumen и Belgorod веба нет; тестовые веб-сборки 06.10–08.10 (c2537e8…177ba3a) читали колонку
напрямую и упадут.

### 3.2 `RangeSlotIssues`: `originalWindowID` / `windowDayOriginal` + `SlotIssueRow`

**Сейчас.** После Deploy/34 (вариант А-9) в этих колонках окно выхода и день окна выхода (`SP/RangeSlotIssues.sql:18-21,44-54`).
По имени их читает одна строка маппинга — `GetSlotIssueRows` (`Ctl/TariffWithRangeGrid.cs:474-475`), по смыслу использует только
Ctrl+R в веере (`Frm/CreateActionMaster/EditIssuesForm.cs:721-722`). Веб процедуру не вызывает, другие объекты БД и `DoAction` —
тоже. Процедуру несут только Deploy/18, 27 и 34.

**Что сломается** при SQL без старых имён у старого клиента: в веере Ctrl+R и «Заменить ролики», «Удалить дубли», «До пересечения»
дают окно ошибки (ArgumentException в маппинге, только если выборка не пуста). В базу ничего не пишется. Del, перетаскивание,
номера, Н/Д не задеваются — они идут через `GetSlotIssueGroups`.

**Безопасный путь без «одного вечера».**
1. SQL временно отдаёт обе пары: `tw.windowId AS actualWindowID`, `tw.dayActual AS windowDayActual` рядом со старыми.
2. Клиент читает новое имя, а без него — старое. Проверку колонки делать один раз на таблицу, как в `RollerIssuesGrid3.cs:307-309`.
3. Когда везде новый клиент и скрипт — убрать старые колонки и запасное имя.

**Правки.** `TariffWithRangeGrid.cs:431-432,444-445,457,474-475` — поля `OriginalWindowId` → `ActualWindowId`, `WindowDayOriginal` →
`WindowDayActual`; `EditIssuesForm.cs:721-722`. Документы: `docs/tasks/window-actual-switch.md:83,193` (номера строк устарели:
сейчас 444-445, 474-475), `docs/window-original-actual.md:168` («RangeSlotIssues — исходное» — неверно после 27/34),
`docs/tariffgrid.md:228`.

**Осторожно.**
- День: `RollerSubstitute` сравнивает с `tw.dayOriginal` (`SP/RollerSubstitute.sql:38,139`). Брать `dayActual` в `RangeSlotIssues`
  безопасно только благодаря Р-2. Либо оставить `dayOriginal` в обеих, либо менять обе.
- Защита Deploy/34 ищет строку `INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId` (`Deploy/34_…:19,572`). Если новый скрипт
  запишет соединение иначе (`ON tw.windowId = i.actualWindowID`), повтор 34 остановится на защите. 18 и 27 имена всё равно откатят.

**Совет.** Поля C# переименовать с ближайшей сборкой (около 15 минут, маппинг оставить на `row["originalWindowID"]` с комментарием,
без деплоя). Если брать в сборку после 35 — заново проверить Ctrl+R, «Удалить дубли», «До пересечения». SQL — при переезде веера
в ядро для веба (`docs/tasks/web-tariffgrid.md:142-148`, `RangeSource`), до того как веб начнёт читать старые имена. С О-5 не
объединять: Grid ждёт ещё и веб, `RangeSlotIssues` — нет.

### 3.3 «Незамененные ролики»: `windowDateOriginal` (сущность 212)

**Сейчас.** Последняя выборка `RollerSubstitute` отдаёт `tw.windowDateActual as windowDateOriginal`
(`SP/RollerSubstitute.sql:363-371`, комментарий `:363-364`). Читатели — только `iEntityAttribute` сущности 212 (подпись «Дата
выпуска», проверено на трёх копиях) и две жёстко заданные колонки в вебе (`Web/ObjectActions.cs:1297,2041`). Подпись верна и для
факта. Сохранённых настроек колонок (`UserSetting`) и переводов с этим именем нет.

**Расхождение имён** не даёт ошибки: десктоп (`SmartGrid.cs:978`) и веб (`ObjectList.razor:259`) просто не покажут колонку даты.

**Правки.** Алиас в SQL; `update iEntityAttribute set name = 'windowDateActual' where entityID = 212 and name = 'windowDateOriginal'`
(1 строка) в той же транзакции; две строки в `ObjectActions.cs`. Клиент qd2 не пересобирается, имя берётся из метаданных
(после перезапуска). Веб — выложить в тот же вечер (прод-веб пробный, увидит только Миша).

**Почему не отдельно.** Ради одного слова придётся полностью перекатить `RollerSubstitute` (около 370 строк), а она пишет
`Issue.tariffPrice` и `TariffWindow.timeInUse*`. Плюс ещё одно звено в цепочке «не перезапускать после 34» (18, 26, 27, 29 и три
скрипта из `ArtvisDB/Scripts`). Защита 34 (`Deploy/34_…:18-20`) новый текст пропустит, и повтор 34 молча вернёт старый алиас —
новый скрипт должен ставить признак, который 34 не пропустит. До тех пор достаточно комментария у `ObjectActions.cs:1297,2041`.

**Не трогать** `RollerSubstitutionPassport:72`: id узла по `tw.windowDateOriginal` — сознательный ключ слота. Фактическое время у
двух окон одного дня может совпасть, а `RollerChangeAdvertTypeForm` разбирает id как дату.

**Вариант.** Веб может брать колонки «Незамененных» из метаданных (`EntityManager.GetEntity(212)`), а не из двух строк
`ObjectActions.cs` — тогда для любого будущего переименования хватит SQL и метаданных. Правка только веба.

### 3.4 Медиаплан: ветки `isFact = 0`

**Сейчас.** Параметр `@isFact` есть у четырёх процедур (`sys.parameters`): `MediaPlanRetrieve_v2` (ветка `:151-190`),
`GetMonthes` (`:13-32`, по умолчанию `@isFact = 0`), `GetUniqueMMsForAction` (`:34,40`), `GetUniqueMMsForPackModuleCampaign`
(`:15,19`). При 0 они читают исходное окно. У `GetAgenciesString` параметра нет, исходное окно зашито в join (`:21`).

**Вызывающие.** 8 вызовов в C#, все передают `true`: `MediaPlanBuilder.cs:185,258,451`, `Action.cs:326`, `Campaign.WinForms.cs:121`,
`CampaignPackModule.cs:129`, `MediaPlanJob.cs:124`, `Documents/ClientDocuments.Files.cs:56`. Все, кроме `Campaign.WinForms.cs`,
общие с вебом. Других вызывающих нет (база, метаданные, `DoAction`, `.rpt`, соседние проекты, msdb, DBA_Tools). Прод-лог после
29.09: 195/30/9 вызовов с `isFact = 1`. Вызов `GetAgenciesString` (`Action.cs:413-423`, `MediaPlanBuilder.cs:864-867`)
недостижим: `Agency.GetAgencyByID` и `Campaign.Agency` не дают null.

**Вариант А (совет).** Убрать ветки в четырёх процедурах, в `GetAgenciesString` перевести join на `actualWindowID`, параметры
оставить с пометкой «не используется». Только `ALTER`, контракт не меняется, клиент и веб не трогаются, перезапуск не нужен,
можно днём. Сверка: для `isFact = 1` текст ветки не меняется; у `GetUniqueMMsForAction` первый SELECT без ORDER BY, а порядок
листов агентств берётся из порядка строк — сравнивать последовательность. В `MediaPlanRetrieve_v2` не потерять
`OPTION (RECOMPILE)` (без него ~2,3 с на вызов). Попутно поправить устаревшие комментарии (`MediaPlanRetrieve_v2.sql:99`,
`GetUniqueMMsForAction.sql:17-25`).

**Вариант Б.** Удалить и параметр, и 8 строк C#. Сначала SQL, потом клиент (иначе `GetMonthes` уйдёт в умолчание 0), вечером, с
перезапуском всех qd2 и веба. Выгода косметическая.

**Осторожно.**
- Полные тексты процедур с ветками есть в исторических скриптах: `ArtvisDB/Scripts/mediaplan-v2-perf-deploy.sql`,
  `multi-action-media-plan-procs-deploy.sql`, `getuniquemms-perf-deploy.sql`, `mediaplan-stage1-deploy.sql`, а также Deploy/07.
  Отстающие базы догоняют и ими — новый скрипт идёт после них.
- Клиенты до 4a766ad (28.06.2026) передавали в `MediaPlanRetrieve_v2` `isFact` из пункта меню, клиенты до 230db73 (27.09)
  вызывали `GetMonthes` без него. Для прода это не важно (там новее), для Artvis2, Univer, Belgorod не проверено: у них медиаплан
  станет фактическим, это совпадает с правилом.
- `ArtvisDB/Scripts/broadcast-start-snapshot.sql:424-446` гоняет `fact 0` и `1`: после варианта А секции `fact = 0` изменятся,
  после Б скрипт упадёт. Не делать между снимками «до» и «после» замера broadcastStart.
- В `MediaPlanRetrieve_v2` остаётся OUTER APPLY к `Pricelist.broadcastStart` (`:129-137`) — это шаг 4 broadcastStart, правка не
  тождественная (вернутся выпуски дней без прайс-листа). Планировать вместе, выкатывать двумя деплоями
  (`docs/broadcast-start.md:448-450,464-466`).
- В той же `GetMonthes` — открытый попутный дефект «лишний пустой месяц у пакетов» (`docs/tasks/side-defects.md`). Решить явно:
  чинить вместе или не трогать.
- Если удалять `GetAgenciesString`: строка перевода «Исполнители: {0}» станет сиротой (`Deploy/08_i18n-deploy.sql:1635`,
  `ArtvisDB/Scripts/i18n/sources.json:4426`, `es.tsv:1511`), убрать `Build Include` (`ArtvisDB/ArtvisDB.sqlproj:326`).

Документы: `docs/tasks/window-actual-switch.md:92-93,111`; `docs/mediaplan.md:179,183,187,214-215`;
`docs/window-original-actual.md:57-58,68,180`.

### 3.5 MediaPlus и Граммофон

**Что это.** Обмен с внешними медиапланерами (около 2008 г.): загрузка кампании из XML Media Plus и из Excel-сетки «Граммофона»,
выгрузка скидок и выпусков в файл Media Plus.

**Мёртвость.**
- 11 файлов (около 1 500 строк: `Client/Classes/Exchange/*`, `Client/Forms/CreateCampaign/CampaignImport{Grammofon,MediaPlus}Form.*`)
  не входят в `Client.csproj` ни в одной версии git; в собранных Merlin.exe пространства имён `Classes.Exchange` нет.
- Методы `ImportData`, которые зовут процедуры, не вызывает никто, даже сами мёртвые формы (`git grep '\.ImportData('` пусто):
  формы только читают файл через `LoadData`.
- Процедуры `ExportMediaPlusFormat`, `CampaignImportGrammofon`, `CampaignImportMediaPlus` (399 строк) никто не вызывает: ни SQL,
  ни метаданные и `DoAction`, ни меню, веб, отчёты, GRANT, msdb. Определения на трёх копиях совпадают.
- Сущность 217 «Импортированные выпуски»: 7 атрибутов, без действий, связей, процедур, сценариев.
- `ExcelLibrary`, на которую ссылается `Exchange/Grammofon/Import.cs:8`, — это `Lib/OfficeDoc.dll` (ссылка `Client/Client.csproj:206-209`);
  после чистки ссылка станет мёртвой.

**Польза.** Уходят последние ссылки класса «М» на исходное окно в этих процедурах; исчезают два пути вставки в `Issue` в обход
`hlp_IssueVerify` и `IssueIUD` (без проверок агитации, позиций, прошлого, с `grantorID = null`); 1 500 некомпилируемых строк
перестают всплывать в поиске; в `docs/IMPROVEMENTS.md` [SQL-04] читателей `DisabledWindow` станет 7 вместо 9, в [GRANT-01] — на
две процедуры меньше.

**Правки.**
- Скрипт по образцу `Deploy/20_broadcast-start-dead-objects-drop.sql` и `Deploy/03`: самопроверка ссылок (вырезать комментарии из
  `sys.sql_modules`, проверить `iStoredProcedure`), `DROP PROCEDURE IF EXISTS` трёх процедур, удаление сущности 217 и её 7
  атрибутов, `leftover_* = 0`, сразу COMMIT. На любой базе отработает благодаря IF EXISTS.
- Проект БД: удалить 3 файла и 3 строки `Build Include` (`ArtvisDB.sqlproj:255,573,590`).
- Клиент: удалить 11 файлов; константы `Action.cs:55` (`ImportCampaign`), `Campaign.cs:47` (`ExportMediaPlus`),
  `InternalConstants.cs:115` (`ImportedIssuesResult`); 4 ресурса (`Resources.resx:120,142,192,210`, `Resources.Designer.cs:130,148,157,240`).
  Эти файлы подключены и в `FogSoft.Core` (`FogSoft.Core.csproj:93,98,110,121,143`) — нужна контрольная сборка qd2.sln и
  `dotnet build FogSoft.Web`. По желанию — ссылка на `OfficeDoc` и `Lib/OfficeDoc.dll`/`.pdb`.
- Переводы-сироты сущности 217 — четыре: «Первая половина часа», «Проимпортировано», «Час выпуска», «Импортированные выпуски»
  (`Deploy/08_i18n-deploy.sql:1005,1025,1075,1102`; `ArtvisDB/Scripts/i18n/es.tsv:884,904,954,981`; `sources.json:7920,8016,8302,8458`;
  `web-i18n-es-seed.sql:902,922,999`). Безвредны; убрать из сборщика i18n.
- Документы: `docs/tasks/window-actual-switch.md:97,134`; `docs/action-forms.md:69-70,359,363`; `docs/tasks/web-action-forms.md:274,386`;
  `docs/tasks/web-migration.md:421` (счётчик «15 мест DoEvents/Cursor»); `docs/tasks/political-agitation-testing.md:348`; [SQL-04], [GRANT-01].

**Не в этом пункте (отдельное решение).** Колонки `MassMedia.mediaPlusMassmediaID` (коды Media Plus у 19 станций прода) и
`grammofonMistake`, `DF_MassMedia_grammofonMistake`, сообщение `MassmediaWithSameMediaPlusIdExist`, параметр `@mediaPlusMassmediaID`
в `MassmediaIUD` и `massmediaList`, `vMassmedia`. Это контракт живых процедур: паспорт станции передаёт значение и пишет его
обратно (`Logs/qd2.log`, 24.09.2026, станция 188). Нужен перезапуск qd2 и веба. Совет — не делать.

Похожие мёртвые выгрузки вне `Client.csproj`: `Client/Classes/GridExport/VectorBoxSerializer/*`, `VideoDJSerializer/*`
(`docs/window-original-actual.md:70`) — отдельный пункт.

### 3.6 `job_DeleteHistory` (О-6) и мелочи

**Что делает.** Разовая «свёртка истории»: подтверждённые акции, закончившиеся до `@date`, сворачивает в спецакции-остатки,
переносит оплаты, удаляет старые акции (каскадом со счетами), выпуски, журналы и окна. Без транзакции удаляет и пересоздаёт
индексы `Issue` и `TariffWindow`; четырёх из шести индексов в схеме уже нет, один из создаваемых — уникальный и неожиданный для
приложения. Окна, связанные склейкой (`FK_TariffWindow_TariffWindowNext`/`Union`, NO_ACTION), уронят DELETE с ошибкой 547
(`SP/job_DeleteHistory.sql:128-131`). Запустить её может любой пользователь программы (EXECUTE на базу у `AdvertAgMainRole`,
на схему у `AdvertagUser`).

**Кто вызывает.** Никто: C#, Protector (там только `job_DeleteEmptyActions`, `FrmMain.cs:351`), веб, метаданные, другие процедуры,
скрипты в репозитории. SQL Agent на Express нет, автозапуска в master нет, шагов msdb нет. По копии прода (до 28.09.2026) с 2024
года не запускалась: обычных акций с `finishDate < 01.01.2024` нет, а она бы их оставила; 83 спецакции заведены руками
09.01–07.05.2024. На Tumen то же с 01.01.2026. Последний настоящий довод «за живую» — разовый скрипт finalPrice правил её
(`CREATE OR ALTER`, `:1422`); при случайном повторе он её воссоздаст.

**Не проверено.** Планировщик Windows на сервере (URANUS) и запуски после 28.09. Команды только на чтение:

```powershell
Get-ScheduledTask | % { $_.TaskName + ' | ' + ($_.Actions.Execute -join ';') + ' ' + ($_.Actions.Arguments -join ';') } | Select-String 'sqlcmd|osql|job_|\.sql|\.bat'
```

```sql
SELECT last_execution_time, execution_count FROM sys.dm_exec_procedure_stats
WHERE database_id = DB_ID('Artvis') AND object_id = OBJECT_ID('Artvis.dbo.job_DeleteHistory');
```

Если найдутся задачи — посмотреть `.sql`/`.bat`, на которые они ссылаются (там точно есть GoOptimize).

**Правки.** Скрипт: шапка, `SET NOCOUNT ON`, GO; защита «нет других ссылок в `sys.sql_modules`»; `DROP PROCEDURE IF EXISTS`; COMMIT.
Удалить файл (169 строк) и строку `ArtvisDB.sqlproj:571`. Документы: О-6; `docs/window-original-actual.md:196,247` (дефект №4);
`docs/tasks/window-actual-switch.md:134`; `docs/tasks/web-action-forms.md:602`; `docs/tasks/discount-applied-pricelist-id.md:73`;
`Deploy/README.md`. Если когда-нибудь понадобится свёртка старых лет — это новая процедура, эта непригодна.

**Мелочи из той же строки карты.**
- `SP/ActionActivate.sql:468` (сейчас `:471-479`) — закомментированный старый код; ещё комментарии «-- Укажи правильное имя
  процедуры!» `:608,628,648` и «-- параметр твоей процы» `:649`. Только попутно со следующей правкой активации.
- `SP/massmediaList.sql:36` — IF-ветка с датами (`:24-71`), в которую никто не передаёт даты (`DoAction` сущности 9, лукапы и
  objectPicker, сценарии `iRelationScenario`, веб `PickerData.cs`, `MassmediaIUD:133`; в логах 0 из 135 вызовов). Результат от выбора
  окна не зависит. По желанию: оставить тело ELSE (`:72-106`), около 48 строк, в нерабочее время (процедуру зовут почти все списки
  станций и проверка лицензии). Код с неверной колонкой (`:67`) уйдёт, но [IMPROVEMENTS.md:235] закрыть только наполовину:
  галочка «Не показывать скидки срок действия которых в прошлом» (сценарий 8, `HideDiscountsInThePast`) не работает уже сейчас —
  ELSE-ветка флаг не читает.
- Кандидат в мёртвые: `massmediaList_web` (создана 05.01.2026) — не найдена ни в C#, вебе, `iStoredProcedure`, docs; отчёты не
  проверялись.

## 4. Чего нет в строке этапа 9

**Документы (важнее всего).**
- `docs/window-original-actual.md` описывает состояние до перехода: §3.3 (деньги, права, учёт — «исходное»), §3.4 (`Grid`,
  `IssuesDays`, журналы, `RollerSubstitute` — «исходное»), §3.5, §6 («деньги — originalWindowID + dayOriginal»), `:179,188,231`
  (`RollerSubstitute` до этапа 5). `CLAUDE.md` велит читать его перед правкой любого читателя — агент по нему напишет неверный
  денежный код. Файл не в git и пришёл из другой сессии — правку согласовать с Мишей.
- Остальные: `docs/tariffgrid.md:189,224-230,244`; `docs/mass-window-operations.md:45-52`; `docs/scenarios/campaign-edit-form-load.md:97,141`;
  `docs/scenarios/range-issue-add-click-to-db.md:53`; `docs/window-merging.md:239` (#14, #15 — после Deploy/35 склейки по времени
  выхода); `docs/roller-types.md:328` (Н-7 закрыт Deploy/26 и Р-12; файл «не мой» — open-questions §8);
  `docs/tasks/tariff-window-actual-time.md:151-155`; `docs/tasks/political-agitation-review-findings.md:208-210`;
  `docs/IMPROVEMENTS.md:151` ([SQL-02] «по originalWindowID»); заголовки `docs/tasks/window-actual-open-questions.md:3,12-20`
  («код не правился») и `docs/tasks/window-actual-switch.md:3` («до правок»). Одним коммитом документации.

**Деплой.** Старые скрипты в `ArtvisDB/Scripts` с полными текстами процедур — ловушки: повторный запуск молча откатывает этапы и
выводит «ГОТОВО», после переименований ещё и сломает клиент. `issue-price-from-db-deploy.sql`, `promo-position-forbidden-deploy.sql`,
`roller-substitute-same-duration-skip-deploy.sql`, `duration-not-above-total-deploy.sql`, `window-chain-guards-deploy.sql`,
`local-promo-roller-types-deploy.sql` и скрипты медиаплана из §3.4 (`Deploy/README.md:35,43,44`). Пометить устаревшими или
убрать в архив — в git они останутся.

**Ещё старые имена с новым смыслом.**
- Параметр `@originalWindowID` в `RollerSubstitute` (`SP/RollerSubstitute.sql:17,23,29,33-35`) — теперь флаг режима, значение не
  читается. Передают: `Cls/CampaignPart.cs:226-229`, `Cls/CampaignRoller.cs:92-111`, `Cls/RollerIssueChange.cs:66,79`,
  `Cls/WindowsSource.cs:691,710`, `Frm/CampaignForm.cs:1159-1160,1236`. Условие можно свести к «`#days` нет и `@issueID` задан».
  Сначала SQL — старый клиент не сломается.
- `IssuesByDate.issueDateOriginal` (`SP/IssuesByDate.sql:27,31,55,59`) — с Deploy/27 время по расписанию окна **выхода**.
  Читатель один: `Cls/RollerIssue.cs:27,65-67` (клон, `Cls/ActionOnMassmedia.cs:571`). То же по смыслу —
  `@issueDateOriginal` в `SP/CampaignTransferDay.sql:33,107,143-183`. Переименовывать вместе с предыдущим: `IssuesByDate` отдаёт
  `i.*`, и через это `originalWindowID` попадает в `CampaignPart` как тот самый флаг.

**Совместимость, которую можно снять вместе с колонкой Grid** (когда 25/26/34 и новый клиент на всех базах):
- запасные ветки клиента и веба на `originalWindowID` и проверка четвёртого набора Grid (§3.1);
- `Issue.SetPosition` (`Cls/Issue.cs:62-74,79-88`) — нужна только базам без Deploy/26. Учесть Т-2: `IssueIUD` UpdateItem по-прежнему
  принимает любой `@windowID` (`SP/IssueIUD.sql:376`) — либо оставить явную передачу, либо сделать отказ.

**Мёртвое после Р-2** (день окна менять нельзя).
- Ветки «окно на другой день»: `Ctl/TariffWindowGrid.cs:492-500` (`ActualTimePostfix`), `Cls/TariffWindowWeek.cs:287-288,350`;
  алиасы `windowDateBroadcast`/`windowDateActualBroadcast` (`SP/TariffWindowRetrieve.sql:141-142,173-174`,
  `Ctl/TariffGrid.cs:90-91`, `Cls/TariffWindow.cs:16,68-70`, `Ctl/RollerIssuesGrid3.cs:161,368`). Безвредны как страховка;
  пересекается с шагом 4 broadcastStart.
- `@needaddday` в `SP/TariffWindowMoveTime.sql:26-36,45,53`, `SP/TariffWindowChangePrice.sql:14-22,34`,
  `SP/TariffWindowChangeDuration.sql:31-39,52`. Условие ложно, пока начало эфирного дня везде 00:00. Если бы ветка ожила, MoveTime
  записал бы окно на день +1 в обход `TariffWindowDayChange` (запрет есть только в `TariffWindowIUD`). Формально шаг 4
  broadcastStart (`docs/broadcast-start.md:194`), правка тождественная — свой замер «0 различий».
- `Cls/SpecialTariffWindow.cs:9,11-15,32` (`time < broadcastStart` → +1 день; `MassmediaPricelist.WinForms.cs:263`) — то же, файл
  общий с вебом.

**Прочее.**
- Таблица `bak_SponsorProgramPricelist_broadcastStart` (ArtvisDev 21.09, Artvis 22.09, Tumen 10.10;
  `ArtvisDB/Scripts/broadcast-start-neutralize-deploy.sql:46-61,97-98` — «не удалять, пока решение не окончательно»). После
  Deploy/29 откат шага 1 почти ничего не вернёт. Других служебных объектов на ArtvisDev нет (сверены 506 объектов с `ArtvisDB/dbo`).
- Комментарий `SP/TariffWindowWithAdvertTypeRetrieve.sql:13-31` о «критичном» индексе `IX_Issue_OriginalWindowID_isConfirmed_rollerID` —
  с Deploy/25 процедура ищет по `actualWindowID`. Сам индекс, возможно, дублирует кластерный (`T/Issue.sql:44,76`) — решать только
  по статистике прода, на ArtvisDev счётчики загрязнены сверками.
- **Не включать:** соединения по `originalWindowID` в местах класса «О», где берётся только станция или день модуля
  (`GetAgenciesString:21`, `ActionRollers:37`, `ActionRollerStats:64`, `CampaignsForActJournalRetrieve:179`, `PackModuleIssueID:280`,
  `CampaignTransferDay:62,118`, `CampaignModuleIssueDelete:137`, `CampaignPackDayDelete:138`) — результат от окна не зависит, а
  кластерный индекс `Issue` стоит по `originalWindowID`, планы могут поменяться. Разве что попутно в процедурах, которые и так
  правятся.

**Шаги 4–5 broadcastStart.** Того же рода, можно вместе с этапом 9: `@needaddday`, алиасы `*Broadcast`, `SpecialTariffWindow.cs`,
таблица `bak_`. Не того рода: размер веера `MinBroadCast`/`MaxBroadCast` (`Ctl/TariffWithRangeGrid.cs:172-203`), `rpt_Grid_v3` и имя
файла DJin [DJin-01] — внешний интерфейс; ветвления генерации и цепочек (`sl_GenerateTariffWindowsDay`,
`GenerateTariffWindowByTemplate`, `TariffIUD`, `fn_FindTariffIDForChain`, `stat_*Loading`); шаг 5 — `DROP COLUMN` в двух таблицах,
атрибут `broadcastStartString` сущности 80, параметры `Pricelists`/`ModulePriceLists`/`PricelistIUD`/`sponsorPLIUD` — изменение
схемы на всех базах.

## 5. Порядок, если делать

1. **Без клиента, когда угодно** — один скрипт мёртвого кода: MediaPlus и Граммофон (§3.5), `job_DeleteHistory` после проверки
   планировщика (§3.6), ветки медиаплана, вариант А (§3.4). Только SQL, старый клиент совместим. Базы: ArtvisDev → прод → Tumen
   (`sqlcmd -b`, явный `-d`), остальные — в список невыложенного. Номер скрипта — следующий свободный в `Deploy/`. Тем же заходом:
   коммит документации (§4) и пометка старых скриптов `ArtvisDB/Scripts`.
2. **С ближайшей сборкой клиента** — удалить файлы обмена, формы, константы и ресурсы (§3.5); переименовать поля `SlotIssueRow` (§3.2).
3. **Когда новый клиент везде** (§2) — `Grid` и запасные ветки (§3.1), `Issue.SetPosition`, `RollerSubstitute @originalWindowID` +
   `IssuesByDate.issueDateOriginal`, колонки `RangeSlotIssues` в SQL (§3.2, при переезде веера в ядро).
4. **Попутно с правками по делу** — алиас «Незамененных» (§3.3), `ActionActivate` и `massmediaList` (§3.6), ветки Р-2 и
   `@needaddday` (§4, вместе с шагом 4 broadcastStart или отдельно со своим замером).

## 6. Открытые вопросы к Мише

1. На каких базах работает Merlin.exe (Artvis, Tumen, Belgorod, Artvis2, Univer) и какие сборки? Belgorod — девелоперская или база
   заказчика? Без этого не начать шаг 3.
2. Планировщик на URANUS и `sys.dm_exec_procedure_stats` на проде (§3.6).
3. `MassMedia.mediaPlusMassmediaID` и `grammofonMistake` — оставить (совет) или удалить отдельным шагом?
4. Можно ли переписать `docs/window-original-actual.md` под состояние после перехода?
5. Медиаплан: вариант А или Б; в `GetMonthes` сменить умолчание `@isFact` на 1? `GetAgenciesString` только перевести или удалить вместе с C#?
6. «Незамененные»: переименовывать вообще или оставить алиас насовсем (тогда переписать комментарий `RollerSubstitute.sql:363-364`
   «переименование — этап 9» при следующем ALTER)?

## 7. Попутно найдено

- `docs/tasks/side-defects.md` №8 — «Незамененные ролики» после массовой замены: при двух и более незамененных — ошибка и пустой
  журнал вместо списка (повтор `RowNum`, ключ сущности 212). На проде с 11.09 (веер) и 25.09 (линейка). Правка в C#.
- О-7 (`job_DeleteEmptyActions`: пересчёт остатков, `docs/tasks/window-actual-open-questions.md` §3) не перенесён в
  `docs/tasks/side-defects.md`.
