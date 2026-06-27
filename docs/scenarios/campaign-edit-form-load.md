# Сценарий: загрузка данных при инициализации формы редактирования кампании

**Форма:** `CampaignForm` (и наследник `EditIssuesForm` для веера)
**Контрол сетки:** `TariffGrid` → `TariffWindowGrid` → `RollerIssuesGrid3` (линейная/простая кампания)
**Что описывает:** полную цепочку загрузки данных от открытия формы до полностью наполненной тарифной сетки, списков роликов, выпусков и статистики.

> Это базовая карта инициализации. Сценарии добавления выпусков (`issue-add-click-to-db.md`,
> `range-issue-add-click-to-db.md`, `template-issue-generation.md`) стартуют уже **после** этой загрузки.

---

## Точки входа

| Откуда | Создание |
|---|---|
| `Campaign.EditRollerIssues(owner, new RollerIssuesGrid3())` | [Campaign.cs:311](../../Client/Classes/Campaign.cs), [Campaign.cs:486](../../Client/Classes/Campaign.cs) |
| `RollerPartOfSponsorCampaign` | [RollerPartOfSponsorCampaign.cs:61](../../Client/Classes/RollerPartOfSponsorCampaign.cs) — `new RollerIssuesGrid3()` |
| `ProgramPartOfSponsorCampaign` | [ProgramPartOfSponsorCampaign.cs:88](../../Client/Classes/ProgramPartOfSponsorCampaign.cs) — `new ProgramIssuesGrid2()` |

Конструктор `CampaignForm(campaign, tariffGrid)` ([CampaignForm.cs:45](../../Client/Forms/CampaignForm.cs)):
- `_campaign.Refresh()` — подтягивает поля кампании из БД;
- `_firm = campaign.Action.Firm`;
- сетка сохраняется в `_tariffGrid`, **на форму ещё не добавлена**.

---

## Цепочка `CampaignForm_Load`

```
CampaignForm_Load                                       [CampaignForm.cs:57]
  │
  ├── ProcessToolbar()        — только видимость кнопок, без данных
  ├── SetFormCaption()        — только заголовок
  │
  ├── SetTariffGrid()                                   [CampaignForm.cs:327]
  │     ├── splitContainer5.Panel1.Controls.Add(_tariffGrid)
  │     ├── SetEventHandlersFromGridEvents(_tariffGrid)  ← подписка на 3 события
  │     │     CampaignStatusChanged, CellClicked, GridRefreshed
  │     └── if (_tariffGrid is TariffGridWithCampaignIssues grid):
  │           grid.Campaign = _campaign
  │           if (IsSimplelCampaign || IsSponsorCampaign):
  │             grid.RefreshGrid()   ◄══════════ ОСНОВНАЯ ЗАГРУЗКА СЕТКИ
  │
  ├── HideGridWithCurrentIssues()   — если грид не IRollerGrid
  ├── InitModulesList()             — только модульная/пакетная кампания
  ├── InitSponsorProgramList()      — только редактирование спонсорских программ
  ├── RollerIssuesGrid config       — IsPopUpMenuAllowed, ExcludeSpecialTariffs, ShowUnconfirmed
  ├── EnableWindowSelectionDelete() — только простая кампания (Del по выделению)
  └── _campaign.DisplayCampaignData(lstStat)  ◄═══ панель статистики (кампания уже Refresh'нута в ctor)
```

---

## `TariffGrid.RefreshGrid()` — ядро загрузки сетки

```
TariffGrid.RefreshGrid()                                [TariffGrid.cs:238]
  │
  ├── Clear()
  │
  ├── loadPricelist()        ← делегат RollerIssuesGrid3.LoadPricelist  [RollerIssuesGrid3.cs:70]
  │     module == null:
  │       pricelist = _massmedia.GetPriceList(_currentDate)             [Massmedia.cs:187]
  │         → DataAccessor.DoAction (Pricelist + Grid + Load)
  │         → SQL [dbo].[PricelistByDate]
  │       clamp startDate/finishDate в границы прайс-листа
  │
  └── DisplayGridData()                                 [TariffGrid.cs:276]
        ├── CreateGridTable()
        └── if (pricelist != null):
              ├── SetGridCaptions()     ← строит заголовки недели,
              │                           вычисляет monday/startDate/finishDate/weekDates[7]
              ├── SetNavigationCaption()
              │
              ├── populateGrid()        ← делегат TariffWindowGrid.populateGrid [TariffWindowGrid.cs:223]
              │     dsWindows = PricelistOnMassmedia.GetTariffWindows(
              │                   startDate, finishDate, module, showTrafficWindows, ShowDisabledWindows)
              │       → SQL [dbo].[TariffWindowRetrieve]
              │         таблица "time":  DISTINCT hour, min, price   (hour/min от windowDateOriginal!)
              │         таблица "Data":  все окна недели
              │     PopulateGridTable():
              │       foreach строка time → AddGridRow → AddWindowCells:
              │         фильтр Data по (price, hour, min)            [TariffWindowGrid.cs:392]
              │         столбец = windowDateOriginal.DayOfWeek       [TariffWindowGrid.cs:404]
              │         _tariffWindows[row,col] = new TariffWindowWithRollerIssues(row)
              │       ProcessPrimeWindows()  — IsPrime по макс. цене дня
              │
              ├── RawDataGridView.DataSource = dtGrid
              │
              ├── onGridPopulated()     ← база: SetContextMenu
              │     + RollerIssuesGrid3.OnGridPopulated()           [RollerIssuesGrid3.cs:160]
              │         AddIssues2Grid():                            [RollerIssuesGrid3.cs:245]
              │           ds = _massmedia.GetRollerCells(pricelist, startDate, finishDate,
              │                  module, ShowUnconfirmed, campaign, rollerPosition)  [Massmedia.cs:211]
              │             → DataAccessor.DoAction (GridCell + Grid + Load)
              │             → SQL [dbo].[Grid]
              │               рез.1: выпуски (originalWindowID, windowDateOriginal, weekday)
              │               рез.2: счётчики выпусков по дням недели
              │               рез.3: окна с выпуском текущей фирмы
              │           красит ячейки: текущая кампания (синий), текущая фирма (морская волна)
              │           проставляет ChangeIssuesCounter по дням
              │         + RefreshWindowsColors() / MarkCellsWithPositionAndAdvType()
              │
              └── SetFrozenRowsAndColumns()
  │
  ▼  (после RefreshGrid)
GridRefreshed event → обработчик из SetEventHandlersFromGridEvents [CampaignForm.cs:369]
  ├── InitRollersList()                                 [CampaignForm.cs:391]
  │     grdRollers.Entity = ActionRollers
  │     SetRollerDataSource() → Firm.GetRollers()  (или RollersForModule для модулей)
  ├── grdCurrentCampaignIssues.Clear() / grdIssues.Clear()
  ├── SetStatus(null)
  └── MarkPrimeWindows / MarkMarkedWindows (простая кампания)
```

---

## SQL-процедуры инициализации (линейная/простая кампания)

| Процедура | Источник вызова | Роль | Резолвинг |
|---|---|---|---|
| `[dbo].[PricelistByDate]` | `Massmedia.GetPriceList` | Прайс-лист на дату (неделю) | Pricelist + Grid(3) + Load(4) |
| `[dbo].[TariffWindowRetrieve]` | `PricelistOnMassmedia.GetTariffWindows` | Структура сетки: сетка часов + окна недели | по имени |
| `[dbo].[Grid]` | `Massmedia.GetRollerCells` | Выпуски в окнах + счётчики по дням + окна фирмы | GridCell(85) + Grid(3) + Load(4) |
| `[dbo].[ActionRollers]` | `grdRollers.Entity` (InitRollersList) | Список роликов акции | по сущности |
| Campaign passport / `DisplayCampaignData` | конструктор + Load-шаг | Поля кампании, статистика в `lstStat` | — |

> Резолвинг «сущность + интерфейс + действие → имя процедуры» — см. `memory/project_sp_resolution.md`.

---

## Где в загрузке участвует ОРИГИНАЛЬНОЕ время окна

Связано с активной задачей «original → actual» (см. ниже). Точки, где раскладка/счётчики
сейчас завязаны на оригинал:

| Точка | Поле | Эффект |
|---|---|---|
| `TariffWindowRetrieve`, таблица "time" | `DATEPART(.., windowDateOriginal)` | строки-заголовки времени = оригинал |
| `TariffWindowGrid.AddWindowCells` [:404] | `windowDateOriginal.DayOfWeek` | столбец (день) = оригинал |
| `Grid` рез.1/рез.2 [:50,:52] | `windowDateOriginal`, `DATEPART(dw, dayOriginal)` | список выпусков и счётчики по дням = оригинал |
| C# `TariffWindow.WindowDate` | `windowDateActual` | **объектная модель уже отдаёт актуал** |

Маркировка занятых ячеек (`AddIssues2Grid`) ищет ячейку по `WindowId`, не по времени —
к переезду окна устойчива.

---

## Ключевые члены формы

| Член | Назначение |
|---|---|
| `_campaign` | Редактируемая кампания (Refresh в ctor) |
| `_tariffGrid` | Тарифная сетка (внедряется снаружи) |
| `_firm` | Фирма акции (`campaign.Action.Firm`) |
| `_template` | Шаблон генерации (заполняется по кнопкам tbbTemplate/tbbTemplate2) |
| `grdRollers` | Список роликов акции |
| `grdCurrentCampaignIssues` | Выпуски текущей кампании в выбранном окне |
| `grdIssues` | Все выпуски в выбранном окне |
| `lstStat` | Панель статистики кампании |

---

## Ветвление по типу кампании

| Тип | `IsSimplelCampaign` | Грид | Особенности загрузки |
|---|---|---|---|
| Simple | да | `RollerIssuesGrid3` | RefreshGrid в SetTariffGrid; EnableWindowSelectionDelete |
| Sponsor (ролики) | да (`_tariffGrid is RollerIssuesGrid3`) | `RollerIssuesGrid3` | как Simple |
| Sponsor (программы) | нет | `ProgramIssuesGrid2` | InitSponsorProgramList вместо роликов |
| Module | нет | грид с `Module` | InitModulesList; RefreshGrid только после выбора модуля |
| PackModule | нет | `PackModuleGrid` | InitModulesList + InitDetailsGrid (Panel2) |
| Range (веер) | нет | `TariffWithRangeGrid` (в `EditIssuesForm`) | RefreshGrid грузит `TariffWindowWithRange` |
