# Сценарий: добавление рекламного выпуска в веерной кампании (range)

**Стартовая форма:** `EditIssuesForm : CampaignForm`
**Ключевой контрол:** `TariffWithRangeGrid`
**Триггер:** клик пользователя на ячейке тарифной сетки при `EditMode.Edit`
**Бизнес-смысл:** один клик добавляет выпуски **сразу на всех радиостанциях** текущей акции через SQL-курсор.

> Связанный сценарий (линейная кампания): `docs/scenarios/issue-add-click-to-db.md`

---

## Цепочка: клик → C# → DAL → SQL → БД → пересчёт → refresh UI

```
Пользователь кликает ячейку в TariffWithRangeGrid
  │
  ▼
TariffGrid.OnGridCellClick()                         [TariffGrid.cs:442]
  │   DataGridView.CellClick, e.RowIndex/ColumnIndex >= fixed
  ▼
TariffGrid.FireCellClicked(ITariffWindow tariffWindow)   [TariffGrid.cs:459]
  │   EditMode == Edit && GridCellTypes.Generic && updateDB != null
  ▼
updateDB(cell)  ← делегат, назначен в TariffWithRangeGrid.InitializeDelegates()
  │
  ▼
TariffWithRangeGrid.AddIssuesRange(DataGridViewCell cell)   [TariffWithRangeGrid.cs:179]
  │   GetTariffWindow(cell).WindowDate → windowDate
  ▼
TariffWithRangeGrid.AddIssuesRange(DateTime windowDate, bool ignoreWindowsWithTheSameFirmIssue = false)
  │                                                         [TariffWithRangeGrid.cs:114]
  │   Формируются параметры:
  │     actionID, issueDate, rollerID, rollerDuration,
  │     positionId, considerUnconfirmed (из ShowUnconfirmed),
  │     ignoreWindowsWithTheSameFirmIssue = false,
  │     [grantorID, если Grantor != null]
  │
  ├── DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters)
  │       │   isTransactionRequired = true (дефолт)
  │       │   _transaction == null → DataAccessor создаёт локальную
  │       │   SqlTransaction на всё время вызова AddRangeIssues
  │       ▼
  │   SQL: [dbo].[AddRangeIssues]                          [AddRangeIssues.sql]
  │       │
  │       │   CURSOR (local fast_forward):
  │       │   SELECT massmediaID, campaignID
  │       │   FROM Campaign WHERE actionID = @actionID
  │       │
  │       │   Для каждой кампании акции:
  │       │     SELECT TOP 1 @windowID, @price, @windowDateActual
  │       │     FROM TariffWindow
  │       │     WHERE massmediaID = @massmediaID
  │       │       AND windowDateOriginal BETWEEN @issueDate AND @issueDate+30min-1s
  │       │       AND maxCapacity = 0 AND isDisabled = 0
  │       │       AND (ignoreWindowsWithTheSameFirmIssue = 0
  │       │            OR NOT EXISTS (issue той же фирмы в этом окне))
  │       │     ORDER BY свободное время DESC
  │       │
  │       │   Если окно не найдено → raiserror и return
  │       │     'CannotAddRangeIssues'  — окно не найдено
  │       │     'IssueWithTheSameFirmExists' — занято той же фирмой
  │       │     (вся транзакция откатывается через DataAccessor)
  │       │
  │       └──► EXEC [dbo].[IssueIUD] @actionName = 'AddItem'
  │               (один вызов на каждую кампанию)
  │               @rollerID, @rollerDuration, @windowID,
  │               @tariffWindowPrice, @campaignID,
  │               @issueDate = windowDateActual,
  │               @positionId, @ratio = 1 (hardcode в SQL),
  │               @loggedUserId, @massmediaID, @grantorID
  │
  ├── _action.Recalculate()  [refreshFlag = true, по умолчанию]
  │       │                                               [ActionOnMassmedia.cs:564]
  │       │   isTransactionRequired = true → своя локальная транзакция
  │       ▼
  │   DataAccessor.ExecuteNonQuery("ActionRecalculate", {actionID})
  │       OUTPUT: @totalPrice → _action[TotalPrice]
  │   _action.Refresh()  ← перечитывает поля акции из БД
  │   Если TotalPrice уменьшился и IsConfirmed → CorrectPaymentAction()
  │       DataAccessor.ExecuteNonQuery("PaymentAction_CorrectByActionTotalPrice")
  │
  ├── AddedIssues.NewRow() + InsertIssueRowSorted(row)
  │     In-memory DataTable: добавляется строка (issueDate, rollerName, duration, position)
  │
  │  (возврат в AddIssuesRange(cell)):
  ├── MarkCellAsHavingCurrentCampaignIssues(cell)     ← покраска одной ячейки
  ├── ChangeIssuesCounter(cell.ColumnIndex, _massmediasCount)  ← счётчик дня
  └── RefreshGrid()  ← ПОЛНЫЙ перезаброс грида из БД
        populateGrid → DataAccessor.LoadDataSet("TariffWindowWithRange", {actionID, dateStart})
        onGridPopulated → MarkCells() — перекраска всех ячеек по флагам

  ▼  (TariffGrid.FireCellClicked продолжает после updateDB)
TariffGrid.onCellClicked(tariffWindow)                [TariffGrid.cs:470]
  ▼
CellClicked → CampaignForm.grid_CellClicked()         [CampaignForm.cs:468]
  │   Template branch — не срабатывает (EditMode.Edit)
  ▼
EditIssuesForm.ShowWindowIssues(tariffWindow) — ПЕРЕОПРЕДЕЛЁН [EditIssuesForm.cs:67]
  ▼
EditIssuesForm.TariffGridRefreshed()                  [EditIssuesForm.cs:78]
  ├── ShowCurrentIssues(grid)
  │     grdCurrentCampaignIssues.DataSource = grid.AddedIssues.DefaultView
  │     (in-memory, без SQL-запроса)
  └── _action.DisplayData(lstStat)
        lstStat: даты, кол-во выпусков, суммарная длительность,
        тарифная цена, итоговая цена
        (данные из _action после Recalculate/Refresh)
```

---

## Сводная таблица

| Слой | Элемент |
|---|---|
| **UI control** | `TariffWithRangeGrid` (в `EditIssuesForm`) |
| **C# entry point** | `TariffGrid.OnGridCellClick` → `FireCellClicked` → делегат `updateDB` |
| **Business method** | `TariffWithRangeGrid.AddIssuesRange(DateTime, bool)` |
| **DAL call** | `DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters)` |
| **SQL procedure** | `[dbo].[AddRangeIssues]` → внутри курсора `EXEC [dbo].[IssueIUD]` для каждой кампании |
| **Транзакция** | Нет явной C#-транзакции. `ExecuteNonQuery` создаёт локальную `SqlTransaction` (isTransactionRequired=true). Весь курсор `AddRangeIssues` атомарен: ошибка в любой радиостанции → откат всего. |
| **Пересчёт** | `_action.Recalculate()` → `ActionRecalculate` + `Refresh()` (refreshFlag=true). Отдельная транзакция. |
| **Refresh UI** | `RefreshGrid()` (полный), `grdCurrentCampaignIssues` ← `AddedIssues` (in-memory), `lstStat` ← `DisplayData` |

---

## Сравнение с линейным сценарием

| Аспект | Линейный (`issue-add-click-to-db.md`) | Веерный |
|---|---|---|
| Форма | `CampaignForm` | `EditIssuesForm : CampaignForm` |
| Грид | `RollerIssuesGrid3` | `TariffWithRangeGrid` |
| Один клик | 1 выпуск, 1 радиостанция | N выпусков, все радиостанции акции |
| C#-транзакция | `DataAccessor.BeginTransaction / Commit` (явная, обёртывает IssueIUD + Recalculate) | Нет явной; локальная SqlTransaction на `AddRangeIssues`, отдельная на `Recalculate` |
| DAL-метод | `DoAction` (через metadata mapping entityId + actionName) | `ExecuteNonQuery` (по имени напрямую) |
| Recalculate | `Campaign.RecalculateAction(false)` — без Refresh() | `_action.Recalculate()` — **с Refresh() по умолчанию** |
| Refresh грида | `RefreshSingleCell` (одна ячейка) | `RefreshGrid()` — полный перезаброс |
| Список выпусков | `ShowWindowIssues` → SQL-запрос | `ShowCurrentIssues` → `AddedIssues` (in-memory) |
| Повторный клик | Добавляет дубль (по дизайну) | Добавляет дубль (по дизайну) |

---

## Файлы и методы

| Файл | Методы / члены |
|---|---|
| `Client\Forms\CreateActionMaster\EditIssuesForm.cs` | Конструктор, `OnLoad`, `ShowWindowIssues` (override), `TariffGridRefreshed`, `ShowCurrentIssues`, `DeleteIssue` |
| `Client\Controls\TariffWithRangeGrid.cs` | `InitializeDelegates` (делегат `updateDB`), `AddIssuesRange(cell)`, `AddIssuesRange(DateTime, bool)`, `DeleteIssuesRange`, `DeleteIssue`, `MarkCells`, `InsertIssueRowSorted`, `InitAddedIssuesData` |
| `Client\Controls\TariffGrid.cs` | `OnGridCellClick`, `FireCellClicked`, `onCellClicked`, делегат `updateDB` |
| `Client\Classes\ActionOnMassmedia.cs` | `Recalculate`, `CorrectPaymentAction`, `DisplayData`, `BuildAddedIssuesTable` |
| `Client\Classes\Action.cs` | `BuildAddedIssuesTable`, `CreateAddedIssuesTable`, `GroupIssuesBySlot`, `BuildCommonSlots` |
| `FogSoft.WinForm\DataAccess\DataAccessor.cs` | `ExecuteNonQuery` (с авто-транзакцией при `isTransactionRequired=true`), `LoadDataSet` |
| `ArtvisDB\dbo\Stored Procedures\AddRangeIssues.sql` | Основная SP: курсор, выбор окна, вызов `IssueIUD` |

---

## SQL-процедуры

| Процедура | Вызов | Роль |
|---|---|---|
| `[dbo].[AddRangeIssues]` | `DataAccessor.ExecuteNonQuery` из `AddIssuesRange` | Курсор по кампаниям акции, выбирает `TOP 1` окно по `issueDate ± 30 мин` с наибольшим свободным временем, вызывает `IssueIUD` для каждой |
| `[dbo].[IssueIUD]` @actionName='AddItem' | Внутри курсора `AddRangeIssues` | Создаёт один выпуск на одной радиостанции (`@ratio=1` hardcode) |
| `[dbo].[ActionRecalculate]` | `ActionOnMassmedia.Recalculate` | Пересчёт суммы акции; OUTPUT: `@totalPrice` |
| `[dbo].[PaymentAction_CorrectByActionTotalPrice]` | `ActionOnMassmedia.CorrectPaymentAction` | Побочный: корректирует платёж, если `TotalPrice` уменьшился у подтверждённой акции |
| `[dbo].[TariffWindowWithRange]` | `TariffWithRangeGrid.populateGrid` | Загрузка тарифной сетки по всем радиостанциям акции на неделю |

---

## Риски перед изменением

| # | Риск | Описание |
|---|---|---|
| 1 | **Атомарность только внутри `AddRangeIssues`** | Весь курсор `AddRangeIssues` атомарен (локальная транзакция DataAccessor). Но `Recalculate()` — отдельная транзакция. Если пересчёт упадёт, выпуски уже записаны. |
| 2 | **`CorrectPaymentAction` как скрытый побочный эффект** | После `Recalculate` при уменьшении `TotalPrice` у подтверждённой акции вызывается `PaymentAction_CorrectByActionTotalPrice`. Срабатывает при каждом добавлении, если цена пересчиталась вниз. |
| 3 | **Полный `RefreshGrid()` после каждого клика** | В линейном сценарии обновляется одна ячейка. Здесь — полный перечёт грида (`TariffWindowWithRange`). При большом числе радиостанций или широком диапазоне дат блокирует UI. |
| 4 | **`AddedIssues` — in-memory, не синхронизируется с БД при RefreshGrid** | `AddedIssues` пополняется вручную строками после каждого клика, но не перестраивается при `RefreshGrid()`. При ошибке в `AddRangeIssues` (raiserror + откат) `AddedIssues` тем не менее не изменится — но если исключение не было поймано в C#, строка не добавится (исключение прокинется до `ErrorManager.PublishError`). |
| 5 | **`MarkCellAsHavingCurrentCampaignIssues` до `RefreshGrid()`** | Ячейка визуально помечается как занятая до того, как `RefreshGrid()` отработал. Если `RefreshGrid()` упадёт — визуальное состояние грида расходится с БД до следующего открытия формы. |
| 6 | **`ignoreWindowsWithTheSameFirmIssue = false` hardcode** | В `AddIssuesRange(cell)` (вызов по клику) флаг всегда `false`. Публичный перегруз с `bool` используется только из `FrmGenerator`. Смысл этого ограничения уточняется. |

---

## Открытые вопросы

| # | Вопрос |
|---|---|
| 1 | **`ignoreWindowsWithTheSameFirmIssue`** — почему при кликах из грида флаг жёстко `false`, а `true` используется только из `FrmGenerator`? Это намеренное ограничение интерфейса или упущение? *(ответ будет позже)* |
| 2 | **Актуальность `AddedIssues` при повторном открытии `EditIssuesForm`** — `BuildAddedIssuesTable` вычисляет пересечение слотов всех кампаний. Если кампании менялись вне этой формы между сессиями, данные устаревают. Есть ли принудительный rebuild? |
