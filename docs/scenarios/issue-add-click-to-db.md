# Сценарий: добавление рекламного выпуска (линейная кампания)

**Стартовая форма:** `CampaignForm`  
**Условие:** `IsSimplelCampaign == true` (тип кампании `Simple`, либо `Sponsor` с `_tariffGrid is RollerIssuesGrid3`)  
**Ключевой контрол:** `RollerIssuesGrid3` (передаётся как `_tariffGrid`)  
**Триггер:** клик пользователя на ячейке тарифной сетки при `EditMode.Edit`

---

## Цепочка: клик → C# → DAL → SQL → БД → refresh UI

```
Пользователь кликает ячейку в _tariffGrid (RollerIssuesGrid3)
  │
  ▼
TariffGrid.OnGridCellClick()                             [TariffGrid.cs:442]
  │   DataGridView.CellClick event → e.RowIndex/ColumnIndex >= fixed
  ▼
TariffGrid.FireCellClicked(ITariffWindow tariffWindow)   [TariffGrid.cs:459]
  │   если EditMode == Edit && GridCellTypes.Generic && updateDB != null:
  ▼
updateDB(cell)  ← делегат, назначен в RollerIssuesGrid3.InitializeDelegates()
  │
  ▼
RollerIssuesGrid3.UpdateDB(DataGridViewCell cell)        [RollerIssuesGrid3.cs:88]
  │   GetTariffWindow(cell) → TariffWindowWithRollerIssues
  │   если module == null → AddIssue(cell, tariffWindow)
  │   иначе              → AddModuleIssue(cell, tariffWindow)
  ▼
RollerIssuesGrid3.AddIssue(cell, tariffWindow)           [RollerIssuesGrid3.cs:133]
  │
  ├── DataAccessor.BeginTransaction()
  │
  ├── CampaignOnSingleMassmedia.AddIssue(               [Campaign.cs:598]
  │     roller, tariffWindow, rollerPosition, grantorID)
  │       │
  │       ▼
  │     new RollerIssue(campaign, roller, tariffWindow,  [RollerIssue.cs:43]
  │                     position, isConfirmed, grantorID)
  │       │   Параметры, передаваемые в IssueIUD:
  │       │     rollerID, campaignID, windowID,
  │       │     tariffWindowPrice, issueDate,
  │       │     rollerDuration, isConfirmed,
  │       │     positionId, grantorID
  │       │     ratio = 1  (дефолт процедуры)
  │       ▼
  │     issue.Update()                                   [PresentationObject.cs:152]
  │       │   actionName = "AddItem"  (IsNew = true)
  │       │   DataAccessor.PrepareParameters(...)
  │       ▼
  │     DataAccessor.DoAction(parameters)                [DataAccessor.cs:142]
  │       │   ProcedureConfig ключ: entityId + "AddItem"
  │       ▼
  │     SQL: [dbo].[IssueIUD] @actionName = 'AddItem'
  │       │   OUTPUT: @issueID
  │       ▼
  │     outParams → issue[issueId] = OUTPUT-значение
  │
  ├── CampaignOnSingleMassmedia.RecalculateAction(false) [Campaign.cs:196]
  │       │   → Action.Recalculate(refreshFlag=false)   [ActionOnMassmedia.cs:564]
  │       ▼
  │     DataAccessor.ExecuteNonQuery("ActionRecalculate", ...)
  │       SQL: [dbo].[ActionRecalculate] @actionID, OUTPUT @totalPrice
  │       │   побочный эффект: если TotalPrice уменьшился и кампания
  │       │   подтверждена → CorrectPaymentAction()
  │
  └── DataAccessor.CommitTransaction()
  │
  ▼
RollerIssuesGrid3.UpdateDB (продолжение):
  ├── campaign.Action.Refresh()
  ├── Refresh()          ← перерисовка DataGridView
  └── FireCampaignStatusChanged()
  │
  ▼  (событие CampaignStatusChanged)
CampaignForm.CampaignStatusChanged()                     [CampaignForm.cs:439]
  ├── _campaign.Refresh()
  ├── _campaign.DisplayCampaignData(lstStat)             ← панель статистики
  ├── changeFlag = true
  └── RefreshDetails(_tariffGrid.CurrentTariffWindow)
  │
  ▼  (TariffGrid.FireCellClicked продолжает после updateDB)
TariffGrid.onCellClicked(tariffWindow)                   [TariffGrid.cs:470]
  ▼
CellClicked event → CampaignForm.grid_CellClicked()      [CampaignForm.cs:468]
  ▼
CampaignForm.ShowWindowIssues(tariffWindow)              [CampaignForm.cs:527]
  ├── grdIssues.DataSource          ← все выпуски в окне
  ├── grdCurrentCampaignIssues.DataSource ← выпуски текущей кампании
  └── SetStatus(tariffWindow) / RefreshDetails(tariffWindow)
```

---

## Файлы и методы

| Файл | Методы / члены |
|---|---|
| `Client\Controls\TariffGrid.cs` | `OnGridCellClick`, `FireCellClicked`, `onCellClicked`, делегат `updateDB` |
| `Client\Controls\RollerIssuesGrid3.cs` | `InitializeDelegates`, `UpdateDB`, `AddIssue`, `AddModuleIssue`, `RefreshSingleCell`, `OnGridPopulated` |
| `Client\Controls\TariffWindowGrid.cs` | делегат `populateGrid`, `AddWindowCells`, `GetCellContent` |
| `Client\Controls\TariffGridWithIssuesOnSingleMassmedia.cs` | свойство `CampaignOnSingleMassmedia` (кастит `campaign` → `CampaignOnSingleMassmedia`) |
| `Client\Controls\TariffGridWithCampaignIssues.cs` | поля `rollerPosition`, `excludeSpecialTariffs`, `campaign` |
| `Client\Forms\CampaignForm.cs` | `IsSimplelCampaign`, `SetEventHandlersFromGridEvents`, `grid_CellClicked`, `CampaignStatusChanged`, `ShowWindowIssues` |
| `Client\Classes\Campaign.cs` | `AddIssue`, `AddModuleIssue`, `RecalculateAction` |
| `Client\Classes\RollerIssue.cs` | конструктор — формирует параметры для `IssueIUD` |
| `Client\Classes\ActionOnMassmedia.cs` | `Recalculate` → `ActionRecalculate`, `CorrectPaymentAction` |
| `FogSoft.WinForm\Classes\PresentationObject.cs` | `Update` — определяет `actionName`, вызывает `DoAction` |
| `FogSoft.WinForm\DataAccess\DataAccessor.cs` | `DoAction`, `PrepareParameters`, `BeginTransaction`, `CommitTransaction`, `RollbackTransaction` |

---

## SQL-процедуры

| Процедура | Когда вызывается | Примечание |
|---|---|---|
| `[dbo].[IssueIUD]` @actionName='AddItem' | `PresentationObject.Update()` | Создаёт выпуск; OUTPUT: `@issueID` |
| `[dbo].[ActionRecalculate]` | `ActionOnMassmedia.Recalculate()` | Пересчёт суммы кампании; OUTPUT: `@totalPrice` |

---

## Риски перед изменениями

| # | Риск | Описание |
|---|---|---|
| 1 | **Дубликат выпуска** | `IssueIUD` не имеет защиты от повторного `AddItem` на тот же `windowID + campaignID + rollerID` — добавляет второй выпуск. Двойной клик или re-entrancy могут создать дубль. |
| 2 | **Транзакция и несколько соединений** | `BeginTransaction` использует `[ThreadStatic] SqlTransaction`. Если `RecalculateAction` или `Action.Refresh()` откроет второе соединение — они окажутся вне транзакции. |
| 3 | **`CorrectPaymentAction` как скрытый побочный эффект** | `ActionRecalculate` при уменьшении `TotalPrice` у подтверждённой кампании вызывает `CorrectPaymentAction()` — изменяется платёжная запись. |
| 4 | **Re-entrancy в `UpdateDB`** | `Refresh()` в конце `UpdateDB` перерисовывает грид. Если в момент перерисовки ячейка остаётся `CurrentCell` и снова срабатывает `CurrentCellDirtyStateChanged` — возможен второй вход в `updateDB`. |
| 5 | **`ratio` не задаётся явно в C#** | В конструкторе `RollerIssue` параметр `ratio` отсутствует; значение `1` берётся из дефолта процедуры. При изменении контракта `IssueIUD` нужно учесть это явно. |
| 6 | **`EditMode` как условие записи** | `updateDB` вызывается только при `EditMode.Edit` (`tbbStart.Checked = true`). Клик при `EditMode.View` не добавляет выпуск — только показывает `ShowWindowIssues`. |

---

## Закрытые вопросы

| # | Вопрос | Ответ |
|---|---|---|
| 1 | Откуда `ratio = 1` в `IssueIUD` | Дефолт SQL-процедуры |
| 2 | Как работает `IsActiveCellSelected` | Проверяет, что `CurrentCell` не в фиксированных строках/столбцах (`RowIndex >= FIXED_ROWS && ColumnIndex >= FixedCols`) |
| 3 | Повторный клик на уже занятую ячейку | `IssueIUD` добавляет второй выпуск — защиты на уровне SQL нет |
