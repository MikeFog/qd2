# Семейство тарифных сеток (TariffGrid) — справочник

Справочник по десктопным сеткам, на которых стоит всё размещение рекламы: показ рекламных
окон, расстановка роликов, появление выпусков кампании. Читать перед любой правкой сеток,
форм размещения (`CampaignForm`, `EditIssuesForm`, `ComboModulePlacementForm`), генератора
по шаблону и перед переносом размещения в веб.

Проект веб-версии — отдельно: [`docs/tasks/web-tariffgrid.md`](tasks/web-tariffgrid.md).

Состояние на 23.09.2026 (master `f239535` плюс незакоммиченная работа по скидкам — сеток она
не касается). Замеры — из лога прода `Logs/qd2.log` за 17.09–23.09.2026 (~19 пользователей)
и из ArtvisDev.

Связанные документы (сценарные карты описывают путь от клика до процедуры подробнее этого
справочника, но частично устарели — см. §10):

- `docs/scenarios/campaign-edit-form-load.md` — открытие формы кампании;
- `docs/scenarios/issue-add-click-to-db.md` — клик в линейной сетке;
- `docs/scenarios/range-issue-add-click-to-db.md` — клик в веере;
- `docs/scenarios/template-issue-generation.md`, `template3-roller-distribution.md` — шаблоны;
- `docs/veer-editor-behavior.md` — поведение веера глазами пользователя;
- `docs/window-merging.md` — склейка окон; `docs/broadcast-start.md` — мёртвое поле `broadcastStart`;
- `docs/tasks/tariff-window-actual-time.md` — оригинальное и актуальное время окна.

---

## 1. Что это и сколько его

Под названием «тарифная сетка» в коде живут **два разных рода** экранов:

| Род | Назначение | Кто |
|---|---|---|
| **Сетки размещения** | поставить/снять ролик кампании в окно → появляются выпуски (`Issue`) | `RollerIssuesGrid3`, `ProgramIssuesGrid2`, `PackModuleGrid`, `TariffWithRangeGrid`, `ComboModuleGrid` |
| **Сетки управления окнами** | работа с самими окнами: цена, время, длительность, удаление, склейка, «день обработан», перенос выпусков между окнами | `TariffWindowGrid` (генерация окон прайс-листа), `TrafficGrid` (трафик-менеджмент) |

Общее у всех — **матрица «время × дни недели»**: две фиксированные строки сверху (даты,
число выпусков за день), одна–две фиксированные колонки слева (цена, время), в ячейке —
одно «окно» (`ITariffWindow`), текст ячейки — остаток свободного времени.

Файлы (`Client/Controls`, строк):

| Класс | Строк | Наследует | Что это |
|---|---:|---|---|
| `TariffGrid` | 805 | `UserControl` | abstract, каркас: неделя, фиксированные строки, делегаты, события, раскраска |
| `TariffGridWithCampaignIssues` | 51 | `TariffGrid` | abstract, + позиция ролика, предмет рекламы, `Campaign` |
| `TariffGridWithIssuesOnSingleMassmedia` | 65 | ↑ | abstract, + одна радиостанция, колонки «Цена / Время / Пн…Вс» |
| `TariffWindowGrid` | 465 | ↑ | окна прайс-листа из `TariffWindowRetrieve`; контекстное меню «цена / удалить окна» |
| `RollerIssuesGrid3` | 498 | `TariffWindowGrid` | **линейная и модульная кампании** (и роликовая часть спонсорской) |
| `TrafficGrid` | 807 | `RollerIssuesGrid3` | трафик-менеджмент (управление окнами и перенос выпусков) |
| `ProgramIssuesGrid2` | 176 | `…OnSingleMassmedia` | спонсорские программы, ячейки-чекбоксы |
| `PackModuleGrid` | 198 | `TariffGridWithCampaignIssues` | пакетный модуль: одна строка × 7 дней |
| `TariffWithRangeGrid` | 1263 | `TariffGrid` | **веер**: получасовые слоты сразу по всем станциям акции |
| `ComboModuleGrid` | 764 | `UserControl` (не член иерархии) | комбо-модули: строка = модуль, колонка = день, неделя или месяц |
| `IRollerGrid` | 22 | интерфейс | ролик / позиция / модуль / грантор / точечное обновление ячейки |

```
UserControl
├── TariffGrid (abstract)
│   ├── TariffGridWithCampaignIssues (abstract)
│   │   ├── TariffGridWithIssuesOnSingleMassmedia (abstract)
│   │   │   ├── TariffWindowGrid ─────────── TariffWindowGenerationForm
│   │   │   │   └── RollerIssuesGrid3 : IRollerGrid ── CampaignForm (линейная, модульная, ролики спонсорской)
│   │   │   │       └── TrafficGrid ───────── TrafficManagementForm
│   │   │   └── ProgramIssuesGrid2 ───────── CampaignForm (спонсорские программы)
│   │   └── PackModuleGrid : IRollerGrid ─── CampaignForm (пакетный модуль)
│   └── TariffWithRangeGrid : IRollerGrid ── EditIssuesForm : CampaignForm (веер)
└── ComboModuleGrid ──────────────────────── ComboModulePlacementForm
```

Главное, что надо понимать про иерархию: **наследование склеило четыре независимые оси**
— откуда брать строки и ячейки, как писать в базу, как красить, что показывать на форме.
Поэтому `TrafficGrid` (управление окнами) наследует `RollerIssuesGrid3` (размещение
роликов) и выключает его поведение (`base(false)`, `UseActualTime => false`,
пустые/переопределённые меню), а `TariffWithRangeGrid` реализует `IRollerGrid` с
заглушками (`Module` — пусто, `RefreshCurrentCell` бросает `NotImplementedException`).
Самый новый член семейства, `ComboModuleGrid`, сознательно не наследуется (комментарий в
классе: «колонок не всегда семь, строка — не окно») и устроен проще всех: один запрос на
весь период, состояние ячейки — в модели `ComboModuleDay`, а не в цвете.

## 2. Где создаются и кто хостит

Сетки размещения создаются в **бизнес-классах** и передаются в форму конструктором
(`CampaignForm(campaign, grid)` → `_tariffGrid`), кроме веера и комбо.

| Кампания (`campaignTypeID`) | Сетка | Где создаётся | Форма, кто открывает |
|---|---|---|---|
| Линейная (1) | `RollerIssuesGrid3` | `Campaign.WinForms.cs:36` → `EditRollerIssues` | `CampaignForm`; `ActionForm.cs:240` |
| Модульная (3) | тот же `RollerIssuesGrid3`, модуль задаётся в `CampaignForm.InitModule` (`:340`) | `Campaign.WinForms.cs:36` | `CampaignForm` |
| Спонсорская (2), ролики | `RollerIssuesGrid3` | `RollerPartOfSponsorCampaign.WinForms.cs:49`, `Campaign.WinForms.cs:36` | `CampaignForm` |
| Спонсорская (2), программы | `ProgramIssuesGrid2` | `Campaign.WinForms.cs:181`, `ProgramPartOfSponsorCampaign.WinForms.cs:59` | `CampaignForm`; `ActionForm.cs:272` |
| Пакетная (4) | `PackModuleGrid` | `CampaignPackModule.WinForms.cs:23` | `CampaignForm` |
| Веер (все линейные кампании акции) | `TariffWithRangeGrid(action, n)` | `EditIssuesForm.cs:39` | `EditIssuesForm`; `ActionForm.cs:543` («Массовое редактирование»), `MDIForm.cs:518` (мастер акции) |
| Комбо-модуль | `ComboModuleGrid` | Designer | `ComboModulePlacementForm`; `ActionForm.cs:520/523`, `MDIForm.cs:549` |
| — (окна прайс-листа) | `TariffWindowGrid` | Designer | `TariffWindowGenerationForm`; `MDIForm.cs:343` |
| — (трафик) | `TrafficGrid` | Designer | `TrafficManagementForm`; `MDIForm.cs:671` |

**Связность с формой.** `CampaignForm` различает сетки проверками типа (`is`/`as`) и
кастами `(IRollerGrid)` без проверки — около 20 проверок и 20 кастов (строки 73, 135, 148,
165, 176, 181, 244–251, 292, 343, 352, 385, 390, 468–479, 525, 547–565, 602, 709, 831–837,
930–933, 1434–1826). Компилятор о пропущенном месте не предупредит: при добавлении нового
типа сетки нужно пройти их все. Свойства формы `IsSimplelCampaign`, `IsRangeCampaign`,
`IsSponsorPrograEditing` смешивают тип кампании и тип сетки.

## 3. Каркас `TariffGrid`

**Шаблонный метод через делегаты.** Конкретная сетка в конструкторе назначает:

| Делегат | Когда | Кто назначает |
|---|---|---|
| `loadPricelist` | в начале `RefreshGrid` | `RollerIssuesGrid3`, `ProgramIssuesGrid2`, `PackModuleGrid`, `TariffWithRangeGrid` (фиктивный прайс-лист с Min/MaxValue), `TrafficGrid` |
| `populateGrid` | внутри `DisplayGridData`, **без проверки на null** | `TariffWindowGrid`, `ProgramIssuesGrid2`, `PackModuleGrid`, `TariffWithRangeGrid` |
| `onGridPopulated` (multicast `+=`) | после привязки `DataSource` | `TariffWindowGrid` (контекстное меню) + `RollerIssuesGrid3` (выпуски, раскраска) и т. д. |
| `updateDB(cell)` | клик в `EditMode.Edit` (или смена чекбокса) | `RollerIssuesGrid3`, `ProgramIssuesGrid2`, `PackModuleGrid`, `TariffWithRangeGrid` |

`RefreshGrid()` = `Clear` → `loadPricelist` → `DisplayGridData` (новая `DataTable`,
строки дат и счётчиков, `populateGrid`, привязка, `onGridPopulated`, фиксация строк/колонок,
восстановление текущей ячейки) → `SetColumnsWidth` → событие `GridRefreshed`.
**Любая смена параметра — полная перезагрузка**: неделя (стрелки, PgUp/PgDn),
позиция (`RollerPosition` setter), предмет рекламы (`SetAdvertTypePresence`),
«показывать неподтверждённые», переход на дату.

**События наружу:** `CellClicked(ITariffWindow)`, `CampaignStatusChanged`, `GridRefreshed`.
Подписка — `CampaignForm.SetEventHandlersFromGridEvents` (`:419`).

**Режимы** (`EditMode`): `View`, `Edit` (клик пишет в базу), `Template` (клик задаёт
время для генератора по шаблону), `TransferIssue` (только трафик). Режим виден цветом шапки.

**Период** — всегда **одна неделя** (Пн–Вс), обрезанная границами прайс-листа; у веера —
неделя по `dateStart`; у комбо — неделя или месяц.

**Где живёт состояние.** Окна — в массиве `ITariffWindow[строка, день]`; текст — в
`DataTable dtGrid`; а признаки «есть мои выпуски / выпуски акции / чужой фирмы» — **в цвете
текста ячейки**: `CellHasCurrentCampaignIssues` и соседние методы читают
`Style.ForeColor == Color.Blue` и т. п. Любой перенос должен держать это в модели.

## 4. Модель ячейки: что показывается

**Строка** линейной сетки = уникальная тройка «час, минута, цена» (тарифное время);
**колонка** = день недели по `windowDateOriginal.DayOfWeek`, а время строки для линейной
расстановки берётся из `windowDateActual` (`@useActualTime`, гибридная раскладка — см.
`tariff-window-actual-time.md`). Ночные часы раскладываются с учётом `broadcastStart`
(`TariffWindowGrid.DefineColumnIndex`), хотя поле мёртвое (все прайс-листы 00:00).

| Сетка | Строка | Ячейка = | Текст ячейки |
|---|---|---|---|
| линейная / модульная | тарифное время + цена | `TariffWindowWithRollerIssues` (окно дня) | остаток времени `mm:ss`, у штучных `[осталось/всего]`, при трафике `(актуальное время)`; в режиме номеров — номера роликов |
| спонсорская | тариф программы | `TariffWindowWithProgramIssue` (тариф × дата) | чекбокс «моя», либо название чужой фирмы; подсказка `[N]` |
| пакетная | одна строка «цена» | `TariffWindowPackModule` (день) | длительность `[свободно]` |
| веер | получасовой слот `hh:mm–hh:mm` | `TariffWindowWithRange` (слот × все станции) | минимальный по станциям остаток времени |
| комбо | модуль (станция, модуль) | `ComboModuleDay` | остаток в самом заполненном окне модуля `[осталось/всего]` |

**Цвет текста** (кто уже стоит в ячейке):

| Цвет | Смысл |
|---|---|
| синий | выпуски текущей кампании (в веере — выпуск акции есть у **каждой** выбранной кампании) |
| красный | веер: выпуски акции есть, но не во всех выбранных кампаниях («частичный» слот) |
| бирюзовый | выпуски другой акции той же фирмы (в веере — на всех станциях) |
| оранжевый | веер: выпуски другой акции фирмы на части станций |

**Фон:** розоватый — недоступное окно (`isDisabled`, «Показывать недоступные»), стальной —
помеченное (`isMarked`), сиреневый — прайм (максимальная цена окна в этот день, считает C#:
`TariffWindowGrid.ProcessPrimeWindows`), персиковый — модульная колонка, где модуль выходит
целиком / трафик: день до «даты обработки», зелёный/бирюзовый — склеенные окна (трафик),
красный — переполнение (трафик).

**Жирный шрифт** — выбранная позиция ролика свободна и/или условие «предмет рекламы есть/нет»
выполняется (фильтры тулбара). Счётчик в строке 2 — число выпусков кампании за день
(в веере × число кампаний).

## 5. Данные: процедуры

Имя процедуры либо видно в C#, либо разрешается ключом «сущность / действие / модуль»
(`DoAction`, см. `project_sp_resolution` в памяти агента / `docs/ARCHITECTURE.md`).

**Чтение**

| Сетка | Процедуры |
|---|---|
| линейная / модульная / трафик | `PricelistByDate` (или `ModulePricelistByDate`) → `TariffWindowRetrieve` (неделя: наборы `time` и `data`) → `Grid` (выпуски кампании по `originalWindowID`, счётчики по дням, окна с выпусками других акций фирмы); по фильтру — `TariffWindowWithAdvertTypeRetrieve`; модульная — `IsModuleExist` на каждую колонку |
| одно окно после клика | `TariffWindowRetrieve @windowId` (~3 мс) |
| панели под сеткой | `WindowIssuesRetrieve` (по `actualWindowID`), `Campaigns` (`ReloadData`), `GetPackModuleIssueDetails` |
| спонсорская | `SponsorPricelistByDate`, `SponsorTariffList`, `ProgramIssues` |
| пакетная | `PackModulePricelistByDate`, `PackModuleTariffWindowsRetrieve`, `PackModuleIssueRetrieve` |
| веер | `TariffWindowWithRange` (5 наборов: слоты, границы вещания, строки времени, чужие акции фирмы, их ролики), `RangeSlotIssues`, `RangeSlotFirmConflict`; «Добавленные выпуски» — `Action.BuildAddedIssuesTable` |
| комбо | `ComboModuleContentRetrieve` / `ComboModuleActionModulesRetrieve`, `ComboModuleFreeTimeRetrieve`, `ComboModuleIssuesRetrieve` |

**Запись** — только через IUD-процедуры: они же ведут счётчики занятости прямо в строке
`TariffWindow` (`timeInUse*`, `capacityInUse*`, `is*PositionOccupied`, `*PositionsUnconfirmed`),
триггеров нет. Любой новый путь записи обязан идти через них.

| Сетка | Запись | Пересчёт |
|---|---|---|
| линейная | `IssueIUD` (AddItem/DeleteItem) | `ActionRecalculate(false)` в той же транзакции |
| модульная | `IsModuleExist` + `ModuleIssueIUD` (сам ставит `Issue` во все окна модуля за день) | то же |
| спонсорская | `ProgramIssueIUD` | то же |
| пакетная | `PackModuleIssueID` (так и называется) | то же |
| веер | `AddRangeIssues` (курсор по кампаниям, `IssueIUD` на станцию, окно — `TOP 1` по свободному времени в получасе), `MasterIssueDelete` | `ActionRecalculate` + `Actions1` **отдельной** транзакцией |
| комбо | `IsModuleExist` + `ModuleIssueIUD` | `ActionRecalculate` в транзакции |

`ActionRecalculate` пересчитывает **всю акцию** (два курсора по кампаниям).

**Размеры** (ArtvisDev, 09.2025–09.2026): окон на станцию в день — медиана 44, p90 69,
максимум 96; линейная неделя — в среднем 37 строк (до 78) × 7 ≈ 260 ячеек (до ~550);
веер — до 48 × 7 = 336 слотов; комбо — в среднем 25 модулей (до 37) × 7 или 31 день
(до ~1100 ячеек); спонсорская — 1–3 строки. Линейная кампания — в среднем 80 выпусков
(медиана 59, p90 150), ~5 в день. У 64% акций больше одной линейной кампании (в среднем 3,9).

**Модель данных, которую нельзя забывать:**

- у выпуска нет своего времени — только `originalWindowID` и `actualWindowID`; при вставке
  они равны, `IssueTransfer` и `ActionActivate` их разводят (на dev 1205 выпусков);
- **несогласованность original/actual:** отметка ячейки и счётчики (`Grid`) — по
  `originalWindowID`; список выпусков окна (`WindowIssuesRetrieve`), занятость и сетка веера —
  по `actualWindowID`; а `RangeSlotIssues`/`MasterIssueDelete` веера — снова по
  `originalWindowID`. Перенесённый выпуск подсвечен в одном окне, а виден и удаляется — в
  другом;
- у 1,7% окон `windowDateOriginal ≠ windowDateActual`, все сдвиги — внутри дня;
- склейка окон `windowPrevId/windowNextId` — 0,11% окон за год на dev (см. `window-merging.md`);
- `MasterIssue` — не таблица, а понятие веера «один ролик в слоте на всех станциях».

## 6. Потоки и цена по обращениям к БД

| Сценарий | Линейная | Веер |
|---|---|---|
| открытие формы | ~9 запросов (кампания, акция, фирма, станция, прайс-лист, окна, `Grid`, ролики фирмы, ролик) | см. П-3: «Добавленные выпуски» собираются дважды, `Campaigns` — 4 раза |
| клик-добавление | 5–6: `IssueIUD` + `ActionRecalculate` (транзакция) → `TariffWindowRetrieve @windowId` → `Campaigns` → `WindowIssuesRetrieve` ×1–2 | 6–8: `AddRangeIssues` → `ActionRecalculate`+`Actions1` → **весь** `TariffWindowWithRange` → (`RangeSlotIssues` по неделе в режиме номеров) → ролики фирмы + ролик |
| неделя / позиция / фильтр | полный `RefreshGrid`: 3 запроса сетки + 2–3 в `GridRefreshed` (ролики) | то же с `TariffWindowWithRange` |
| Insert по выделенным окнам | `IssueIUD` **+ `ActionRecalculate` на каждое окно** + ещё один в конце (П-1) | `AddRangeIssues` в цикле, пересчёт один |
| Del по выделенным окнам | `WindowIssuesRetrieve` на окно + `IssueIUD` на выпуск, пересчёт один | `MasterIssueDelete` на выпуск/группу |
| шаблон (`FrmGenerator`) | на день: прайс-лист + окна; на выпуск: `IssueIUD` + `issue.Refresh()`, проверки фирмы на окно-кандидат; пересчёт один в `finally` | путь Simple: `AddIssuesRange` **с пересчётом на каждую дату** |
| отмена шаблона | `IssueIUD` на выпуск, пересчёт один | `MasterIssueDelete` на слот, пересчёт один |

## 7. Замеры с прода (лог 17–23.09.2026)

| Операция | Число | Медиана | p90 | Сумма |
|---|---:|---:|---:|---:|
| клик-добавление, линейная | 5615 | 231 мс | 357 мс | — |
| … из них запись (`UpdateDB`: `IssueIUD`+пересчёт) | | 52 мс | 189 мс | |
| … после записи, на форме (детали окна, привязки) | | **165 мс** | 227 мс | 954 с |
| клик-добавление, веер | 447 | **856 мс** | 4,2 с | — |
| … после записи (полная перезагрузка + привязки) | | 782 мс | 4,0 с | 764 с |
| `TariffWindowWithRange` (все вызовы >100 мс) | 751 | 436 мс | 1,0 с | 437 с — **первая процедура по суммарному времени во всём логе** |
| генерация по шаблону | 324 запуска / 3993 выпуска | ~110 мс на выпуск, из них `IssueIUD` ~4 мс | | 452 с |

На dev: `TariffWindowRetrieve` за неделю — 39 мс, `Grid` — 28 мс, `TariffWindowWithRange`
на 6 станций — 1,5 с (тёплый кэш).

## 8. Дефекты производительности

Подробный разбор каждого пункта (код, правка, риски, проверка) —
[`docs/tasks/tariffgrid-desktop-perf.md`](tasks/tariffgrid-desktop-perf.md).

Только то, что стоит времени пользователю (двойные загрузки, одиночные операции там, где
возможна пачка, лишняя работа из-за подписок). Некрасивый, но быстрый код сюда не входит.
Приоритет — по пользе на единицу работы. Уже записанное в `docs/IMPROVEMENTS.md` помечено
его идентификатором.

| # | Где | Что не так | Цена сейчас | Лечение | Цена работы |
|---|---|---|---|---|---|
| **П-1** | `CampaignForm.AddIssuesInSelectedWindows` → `RollerIssuesGrid3.AddIssueToWindow` → `AddIssueTransaction` | Insert по N окнам: `ActionRecalculate` на **каждое** окно, потом ещё один (комментарий «пересчёт один после пакета» не соответствует коду). То же: `ComboModulePlacementForm.AddIssuesInSelectedCells` (`:489`, пересчёт с `Refresh` на ячейку); шаблон веера путь Simple (`CampaignForm.cs:559`, пересчёт на каждую дату + ещё один в `FrmGenerator.finally`) | N × пересчёт всей акции | флаг «без пересчёта» (как `recalculate:false` в веере), один пересчёт в конце | мала |
| **П-2** | `TariffWithRangeGrid.AddIssuesRange(cell)` → `RefreshGrid` — [RANGE-01] | каждый клик веера перечитывает всю неделю по всем станциям | ~0,8 с на клик, p90 4 с | точечное перечитывание слота (параметр даты слота в процедуре) | средняя |
| **П-2а** | `TariffWindowWithRange.sql`, разделы 7 и 9 | один и тот же тяжёлый джойн `TariffWindow × Issue × Campaign × Action` по всем станциям считается **дважды** (раздел 9 сам пишет «тот же джойн, что в п.7») | ~0,5 с из ~1,5 с на dev | раздел 7 считать из `#otherIssues` (добавить туда `massmediaID`, `deleteDate`) | мала, только SQL |
| **П-3** | веер, открытие: `TariffWithRangeGrid` ctor (`BuildAddedIssuesTable(null)`) → первый `EditIssuesForm.RefreshGrid` → `SetSelectedCampaigns(список)` (`IsSameSelection(null, список)` = false) | «Добавленные выпуски» собираются **дважды**; сборка — `GetCampaignById` на каждую кампанию (N+1) + `GetContent` **всех выпусков кампании за весь период**; `Campaigns` вызывается 4 раза. Та же пересборка — на каждую галочку чек-листа кампаний | ~25–30 лишних запросов на 10 станций | собирать один раз; тип кампании уже есть в строках `Campaigns()`; выпуски акции одним запросом по `@campaignIDs` | средняя |
| **П-4** | `CampaignForm` лямбда `GridRefreshed` → `InitRollersList` → `Firm.GetRollers()`; `SmartGrid.DataSource` → `FireObjectSelected` → `new Roller(id)`; `SelectedObject = selected` → ещё раз (`PresentationObject.Equal` сравнивает упакованные ID по ссылке) | **каждый** `RefreshGrid` (неделя, Insert, Del, шаблон, отмена; в веере — каждый клик) перечитывает список роликов фирмы и 1–2 раза грузит ролик | 2–3 запроса + перепривязка на каждое обновление | список роликов от недели не зависит (кроме модульных) — грузить при открытии; `new Roller(row)` вместо `new Roller(id)` | мала |
| **П-5** | `CampaignForm.InitModule` (`:340–367`) | выбор модуля: до **трёх** полных `RefreshGrid` подряд (сеттер позиции ещё со старым модулем; основной; `Jump2CurrentDate` для будущих прайс-листов — всегда) | ×3 к открытию модуля | один `RefreshGrid` после выставления всех параметров | мала |
| **П-6** | `RollerIssuesGrid3.AddModuleIssue` / `RefreshCurrentCell` (модуль) | после модульного клика `TariffWindow.Refresh()` на **каждое окно дня** (десятки запросов); `MarkFullColumns` — `IsModuleExist` на каждую колонку | десятки запросов на клик | один `GetTariffWindows(date, date)`; `IsModuleExist` пачкой на неделю | мала |
| **П-7** | `CampaignForm.grid_CellClicked` → `ShowWindowIssues` — [UI-02] | после каждого клика-добавления перестраиваются две таблицы деталей окна (1–2 × `WindowIssuesRetrieve`, клонирование сущностей, привязка) | 165 мс медиана × 5615 кликов за неделю | детали окна — по требованию, не на клик расстановки | средняя (порядок `DoEvents`, SmartGrid) |
| **П-8** | `EditIssuesForm`: клик веера | `updateDB` → `RefreshGrid` → `GridRefreshed` → `TariffGridRefreshed`, затем `grid_CellClicked` → `ShowWindowIssues` → `TariffGridRefreshed` **ещё раз**; SmartGrid делает `bm.PositionChanged +=` без `-=` → **+2 подписки на каждый клик** (сбрасываются только при пересборке `AddedIssues`) | лишняя привязка + растущая работа на смену строки | одна привязка; `-=` перед `+=` в `SmartGrid` | мала |
| **П-9** | `FrmGenerator` | ~110 мс на выпуск при `IssueIUD` ~4 мс: `issue.Refresh()` на выпуск, `IsRollerOfTheFirmExist` / `WindowIssuesRetrieve` на каждое окно-кандидат, окна перечитываются на каждый день, `grdSuccess.AddRow` на выпуск | 452 с за неделю по всем пользователям | окна периода одним запросом; «окна с выпуском фирмы» пачкой (есть в 3-м наборе `Grid`); замерить остаток клиента | средняя |
| П-10 | `RecalculateAction()` с `refreshFlag=true` (`CampaignForm.cs:819, 978, 1071, 1165, 1672`, `FrmGenerator.cs:238`); `ComboModulePlacementForm` (`:333/792` + `ShowStatistics`) | лишняя загрузка акции (`Actions1`) после пересчёта — `TotalPrice` уже приходит из OUTPUT; **но** `tariffPrice`, `iCount` и т. п. освежает только этот `Refresh` (кнопка «Цена акции», статистика веера/комбо) | +1 запрос на операцию | вернуть `tariffPrice` OUTPUT-параметром, потом `RecalculateAction(false)`; в комбо убрать второй `Refresh` | мала |
| П-11 | `CampaignForm.DeleteIssuesInSelectedWindows`, отмена шаблона, `MoveIssuesToWindow`, веер: удаление дублей, «до пересечения», Del | одиночные `IssueIUD`/`MasterIssueDelete`/`AddRangeIssues` в цикле (пересчёт уже один); `MoveIssuesToWindow` и `MoveRangeIssuesToSlot` — `new Roller` на каждую строку | N запросов по 4–30 мс | ролики кэшировать по id; чтение выпусков одним запросом по списку окон. Процедуры массовой записи — только если N реально сотни | мала / средняя |
| П-12 | `ProgramIssuesGrid2` | N+1 на отрисовке: `Issue.Campaign.Action.FirmName` → `GetCampaignById` + `GetActionById` на каждую чужую ячейку; `LoadIssues` выбрасывается сразу после загрузки (свой `GridRefreshed` раньше формы); клик по чекбоксу вызывает `ShowWindowIssues` дважды (`CellClick` и `CurrentCellDirtyStateChanged`) | сетка крошечная — терпимо | имя фирмы в `ProgramIssues`; одна подписка | мала |
| П-13 | пакетная: `RefreshDetails` в `CampaignStatusChanged` и в `ShowWindowIssues` | `GetPackModuleIssueDetails` дважды на клик | +1 запрос | вызывать один раз | мала |
| П-14 | `ComboModulePlacementForm.OnGridRefreshed` — [UI-03] | на каждом листании периода перечитываются **все** выпуски акции (от периода не зависят); после удаления — дважды | ~2 с клиентской перестройки на операцию | выпуски — один раз и после записи | средняя |
| П-15 | `ActionForm` после закрытия формы кампании (`:241–243`, `:545–546`) | `RefreshActionStats(true)` + `LoadCampaigns` безусловно, даже без изменений; спонсорская — лишние `Action.Recalculate()` (`Campaign.WinForms.cs:186`, `ProgramPartOfSponsorCampaign.WinForms.cs:64`) | 2–3 запроса + пересчёт | только при `ChangeFlag` | мала |
| П-16 | `TrafficManagementForm.TsbMassClose_Click` (`:78–92`) | `DataSource` → `ObjectSelected` → `RefreshGrid`, затем явный `RefreshGrid` | двойная загрузка | убрать явный | мала |
| П-17 | `TariffWindowGrid.DeleteGeneratedTariffWindows`, `MassmediaPricelist.DeleteGeneratedTariffWindows` | `TariffWindowMassDelete` по одному дню (в логе 480 медленных вызовов, 167 с за неделю) | ~250 мс × дни | по неделям/месяцам; дробление по дням, вероятно, сознательное (прогресс, отмена, короткие блокировки) — сначала уточнить | мала |

Мелочи на процессоре (без запросов): `TariffGrid.GetCell(ITariffWindow)` — линейный проход по
всем ячейкам, `AddIssues2Grid` зовёт его на каждый выпуск (O(выпуски × ячейки));
`RefreshWindowsColors` три раза за одно обновление линейной сетки; `AddWindowCells` —
`DataTable.Select` на каждую строку времени. Сами по себе не заметны; словарь
`windowId → (строка, колонка)` снимает первое.

## 9. Попутные дефекты (не производительность)

- `TariffGrid.cs:296`: `grid is IRollerGrid` проверяет внутренний контрол, а не сетку —
  всегда false, сообщение «нет прайс-листа на дату» не показывается никогда.
- `TrafficGrid.cs:772–773`: в цикле переноса используется `SelectedIssue` (текущая строка), а
  не переменная цикла.
- `PresentationObject.Equal` сравнивает `object[] IDs` оператором `!=` (упакованные `int` по ссылке) — одинаковые объекты «не равны»; используется `SmartGrid` при каждой смене строки во всём приложении (корень П-4).
- `PresentationObject.Equals` сравнивает **хеши** (сумма хешей ID + сущность) — коллизия даст
  ложное равенство.
- `TariffWindowRetrieve`: `LEFT JOIN TariffUnion ON (… OR …)` — у 88 тарифов на dev окна
  приходят дублями (C# перезаписывает ту же ячейку, вреда нет).
- `MasterIssueDelete`: если у кампании нет подходящего выпуска, `IssueIUD` зовётся с
  `@issueID = NULL` и молча ничего не делает (по чтению, не проверено).
- Гипотезы, не проверены: `grdCurrentCampaignIssues_ObjectChanged` в веере может дойти до
  `TariffWithRangeGrid.RefreshCurrentCell` → `NotImplementedException`; `InitModulesList` из
  `GridRefreshed` может вызвать `InitModule(null)` и очистить только что загруженную сетку.

## 10. Где сценарные карты разошлись с кодом (23.09.2026)

- `issue-add-click-to-db.md`: `UpdateDB` больше не вызывает `campaign.Action.Refresh()` и
  `Refresh()`; транзакция вынесена в `AddIssueTransaction`; `CampaignStatusChanged` делает
  `ReloadData`; транзакция в `DataAccessor` — `AsyncLocal`, не `[ThreadStatic]`; дубль позиции
  `IssueIUD` ловит (`PositionErrorForTheSameAction`), для позиции 0 — нет; номера строк сдвинуты.
- `range-issue-add-click-to-db.md`: курсор `AddRangeIssues` — `campaignTypeID = 1` и
  `@campaignIDs`; окно ищется по `windowDateActual` вперёд на 30 минут (не ± и не original);
  синий цвет — по признаку из SQL, ролики могут быть разными; не описан двойной ребинд (П-8)
  и риск original/actual (§5).
- `campaign-edit-form-load.md`: точки входа — в `*.WinForms.cs`; `EnableWindowSelectionActions`
  + `EnableIssueDragDrop`; время строки — по `@useActualTime`; нет `PackModuleGrid`,
  `TrafficGrid`, `TariffWindowGrid`.
- `template-issue-generation.md`: у веерного TimePeriod конец интервала не включается,
  сортировка — по свободному времени, затем случайно; в линейных путях есть проверка позиции;
  линейный TimePeriod фильтрует окна по оригинальному времени, а сетка показывает актуальное.
- `template3-roller-distribution.md`: веер берёт только выбранные в чек-листе кампании.
- `action-activate-transfer-plan.md`: план полностью реализован (плюс `@avoidFirmRollerWindows`);
  перенос меняет **оба** `originalWindowID` и `actualWindowID` и пересчитывает `tariffPrice`.
- `tariff-window-actual-time.md` §4: «у окна с переносом `originalWindowID == actualWindowID`»
  неверно для 1205 выпусков.

Сами карты не правились: при следующей работе с конкретным сценарием — обновить его карту.
