# Сценарий: добавление рекламных выпусков по шаблону

Генерация множества выпусков за одно действие пользователя.
Работает для **линейной** кампании (оба пути). Для **веерной** — оба пути реализованы.

> Связанные сценарии:
> - `docs/scenarios/issue-add-click-to-db.md` — одиночный клик, линейная кампания
> - `docs/scenarios/range-issue-add-click-to-db.md` — одиночный клик, веерная кампания

---

## Точки входа в CampaignForm

### Path 1 — tbbTemplate (Simple шаблон)

Доступен для: линейной кампании, **веерной кампании**.

```
tbbTemplate_Click                                      [CampaignForm.cs]
  │   Открывает FrmTemplate → пользователь задаёт:
  │     StartDate, FinishDate
  │     WeekDays (чекбоксы) или OddEven (чётные/нечётные дни)
  │     IsModeAdd (добавить / удалить)
  │     IgnoreWindowsWithTheSameFirmIssue
  │   TemplateType = Simple (одно окно на дату)
  │   Результат: _template заполнен, EditMode → Template
  │
  ▼
Пользователь кликает ячейку в гриде
  ▼
TariffGrid.OnGridCellClick → FireCellClicked
  ▼
CampaignForm.grid_CellClicked
  │   EditMode == Template:
  │     _template.SetTime(tariffWindow.WindowDate)
  │       → StartDate/FinishDate получают time-компоненту кликнутого окна
  │     Показывает подтверждение (Globals.ShowQuestion)
  │
  ├── [IsRangeCampaign]
  │     new FrmGenerator(
  │       _template,
  │       updateDB:  windowDate => rangeGrid.AddIssuesRange(windowDate, ignoreFlag),
  │       deleteDB:  rangeGrid.DeleteIssuesRange)
  │
  └── [Simple/Linear]
        new FrmGenerator(
          _template, roller, RollerIssuesGrid.RollerPosition,
          _campaign, RollerIssuesGrid.Pricelist, module, grantorID)
```

### Path 2 — tbbTemplate2 (TimePeriod шаблон)

Доступен для: **линейной и веерной кампании**.
Кнопка видна в обоих типах форм; для `EditIssuesForm` строка `tbbTemplate2.Visible = false` удалена.

```
tbbTemplate2_Click                                     [CampaignForm.cs]
  │   Открывает FrmTemplate2 → пользователь задаёт:
  │     StartDate, FinishDate
  │     StartTime, FinishTime (диапазон времени, обе границы включительно)
  │     WeekDays
  │     Quantity (всего) — или раздельно:
  │       QuantityPrime / QuantityNonPrime (если cbSplitPrime.Checked)
  │     IgnoreWindowsWithTheSameFirmIssue
  │   IsModeAdd = true (всегда, удаление не предусмотрено)
  │   TemplateType = TimePeriod (несколько окон/слотов на дату)
  │
  ▼  Сразу, без клика на ячейку. Ветка по типу кампании:
  │
  ├── [IsRangeCampaign]
  │     slotsCache = new Dictionary<DateTime, List<TariffWindowWithRange>>()
  │     new FrmGenerator(
  │       _template,
  │       updateTimePeriodDB: date => rangeGrid.AddIssuesRangeTimePeriod(
  │           date, StartTime, FinishTime,
  │           Quantity, QuantityPrime, QuantityNonPrime,
  │           IgnoreWindowsWithTheSameFirmIssue, slotsCache),
  │       action: rangeGrid.Action)
  │
  └── [Simple/Linear]
        new FrmGenerator(
          _template, roller, RollerIssuesGrid.RollerPosition,
          _campaign, null, module, grantorID)
```

---

## FrmGenerator.Generate() — основной цикл

```
OnShown → Generate()                                   [FrmGenerator.cs]

  template.Reset()
  pbProgress.Maximum = template.DaysCount   ← полный проход итератора
  template.Reset()                          ← сброс снова (важно: два Reset)

  while (template.MoveNext()):              ← итерация по датам
  {
    try {
      pos = IsModeAdd ? AddIssues() : DeleteIssues()
      foreach po in pos: grdSuccess.AddRow(po)
    }
    catch { grdFail.AddRow(ошибка по дате) }
    pbProgress.Value++
    Application.DoEvents()
  }

  finally {
    if (_campaign != null)
      _campaign.RecalculateAction()         ← линейная: 1 раз после всего цикла
    else if (_action != null)
      _action.Recalculate()                 ← веерная TimePeriod: 1 раз после цикла
    // веерная Simple (Path 1): _campaign=null, _action=null →
    //   Recalculate вызывается внутри каждого AddIssuesRange (N раз)
  }
```

---

## AddIssues() — маршрутизация по типу

```
AddIssues()                                            [FrmGenerator.cs]
  │
  ├── _updateTimePeriodDB != null  (range + TimePeriod, Path 2 веерная)  ← новое
  │     TimePeriodAddResult result = _updateTimePeriodDB(currentDate)
  │       = rangeGrid.AddIssuesRangeTimePeriod(date, startTime, finishTime,
  │           quantity, quantityPrime, quantityNonPrime, ignoreFlag, slotsCache)
  │         │
  │         │  1. weekMonday = GetWeekMonday(date)
  │         │  2. slots = GetSlotsForWeek(weekMonday, slotsCache)
  │         │       → если нет в кеше: LoadDataSet("TariffWindowWithRange", ...)
  │         │         → new TariffWindowWithRange(row) для каждой строки
  │         │  3. фильтр: slot.WindowDate.Date == date.Date
  │         │             && TimeOfDay >= StartTime (включительно)
  │         │             && TimeOfDay <= FinishTime (включительно)
  │         │  4. OrderBy(slot.WindowDate) — хронологически
  │         │  5. если Quantity > 0:
  │         │       selectedSlots = slotsForDate.Take(quantity)
  │         │     иначе (prime split):
  │         │       prime    = slotsForDate.Where(IsPrime).Take(quantityPrime)
  │         │       nonPrime = slotsForDate.Where(!IsPrime).Take(quantityNonPrime)
  │         │  6. foreach slot: AddIssuesRange(slot.WindowDate, ignoreFlag,
  │         │                                  recalculate: false)
  │         │       → ExecuteNonQuery("AddRangeIssues", ...) → IssueIUD × N СМИ
  │         │       → AddedIssues.NewRow (in-memory)
  │         │       on error: result.Errors.Add(ex)  — не прерывает цикл
  │         │  7. return result (Rows + Errors + ExpectedCount)
  │     │
  │     foreach ex in result.Errors → AddErrorInfo → grdFail  (per slot)
  │     if result.Rows.Count < result.ExpectedCount → AddErrorInfo → grdFail (summary)
  │     return result.Rows.Select(r => new RollerIssue(r))
  │     [Recalculate — НЕ здесь, выполняется в finally]
  │
  ├── _updateDB != null  (range + Simple, Path 1 веерная)
  │     DataRow row = _updateDB(currentDate)
  │       = rangeGrid.AddIssuesRange(currentDate, ignoreFlag)
  │           → ExecuteNonQuery("AddRangeIssues", ...)
  │               SQL курсор → EXEC IssueIUD AddItem × N (по числу СМИ)
  │           → _action.Recalculate()          ← на каждую дату (recalculate=true)
  │           → AddedIssues.NewRow (in-memory)
  │     return new RollerIssue(row)
  │
  ├── TemplateType.Simple, module=null, program=null  (Path 1 линейная)
  │     AddSimpleIssue()
  │       → TariffWindowWithRollerIssues.GetWindowByDate(currentDate, massmedia)
  │           DataAccessor.DoAction → metadata SP → Load TariffWindow
  │       → if (!ignoreFlag || !window.IsRollerOfTheFirmExist(firmId))
  │             _campaign.AddIssue(roller, window, position, grantorID)
  │               → new RollerIssue(...) → issue.Update()
  │                   → DataAccessor.DoAction → IssueIUD AddItem
  │
  ├── TemplateType.TimePeriod, module=null, program=null  (Path 2 линейная)
  │     AddSimpleIssues()
  │       → GetPriceList(currentDate) + GetTariffWindows(currentDate, currentDate)
  │           свежий SQL на каждую дату, независимо от грида
  │       → фильтр по StartTime..FinishTime (обе включительно)
  │       → фильтр IgnoreWindowsWithTheSameFirmIssue (C#: IsRollerOfTheFirmExist)
  │       → если Quantity > 0:
  │             OrderBy(window) ThenBy(random), берём Quantity
  │             AddIssuesFromWindows → IssueIUD × Quantity
  │       → если Quantity == 0 (prime split):
  │             _maxPrices = massmedia.GetMaxPriceByDay(startDate, finishDate)
  │               → SQL: GetMaxPriceByDay (один раз перед циклом)
  │             prime-окна (price == maxPriceForDay)     → QuantityPrime
  │             non-prime  (price != maxPriceForDay)     → QuantityNonPrime
  │             AddIssuesFromWindows × 2 → IssueIUD × (QuantityPrime + QuantityNonPrime)
  │
  ├── module is Module     → AddModuleIssue()    → IssueIUD AddItem
  ├── module is PackModule → AddPackModuleIssue() → IssueIUD AddItem
  └── program != null      → AddProgramIssue()   → program SP
```

---

## DeleteIssues() — ветка удаления

Доступна только из **Path 1** (FrmTemplate, `IsModeAdd = false`).
FrmTemplate2 удаление не поддерживает (всегда `IsModeAdd = true`).

```
DeleteIssues()                                         [FrmGenerator.cs]
  │
  ├── _deleteDB != null  (range)
  │     rangeGrid.DeleteIssuesRange(currentDate)
  │       → DataAccessor.ExecuteNonQuery("MasterIssueDelete", ...)
  │           SQL курсор → EXEC IssueIUD DeleteItem × N (по числу СМИ)
  │       → _action.Recalculate()
  │       → удаление строки из AddedIssues (in-memory)
  │
  └── Simple
        GetIssuesForDate(currentDate)
          → DataAccessor.LoadDataSet("IssuesByDate", ...)
        foreach issue: issue.Delete(true)
          → IssueIUD DeleteItem × кол-во выпусков за дату
```

---

## Транзакции

| Путь | Транзакция |
|---|---|
| Simple: `IssueIUD` per window | Авто (DataAccessor `isTransactionRequired=true`) — per-call |
| Range: `AddRangeIssues` | Авто — весь курсор атомарен per-day (один слот) |
| **Весь шаблон целиком** | **Нет** — каждый день независим. Ошибка на дне 5 из 10 не откатывает дни 1–4 |

---

## Recalculate

| Путь | Когда | Сколько раз |
|---|---|---|
| Simple линейная | `FrmGenerator.finally`, `_campaign.RecalculateAction()` | **1 раз** — после всего цикла |
| Range Simple (Path 1) | Внутри каждого `AddIssuesRange` — `_action.Recalculate()` | **N раз** — по числу дат шаблона |
| **Range TimePeriod (Path 2)** | `FrmGenerator.finally`, `_action.Recalculate()` | **1 раз** — после всего цикла |
| `CorrectPaymentAction` | Внутри каждого `Recalculate` при уменьшении `TotalPrice` | Пропорционально числу Recalculate |

---

## Refresh UI после закрытия FrmGenerator

```
CampaignForm (после form.ShowDialog):

  if (_template.IsDateCovered(startDate, finishDate))
      RefreshGrid()                         ← всегда, если период пересекается

  // для range (Path 1 и Path 2): CampaignStatusChanged() не вызывается
  // EditIssuesForm.ShowWindowIssues (override) → TariffGridRefreshed()
  //   → grdCurrentCampaignIssues.DataSource = AddedIssues.DefaultView
  //   → _action.DisplayData(lstStat)

  // для simple (Path 1 и Path 2):
  if (!(_tariffGrid is TariffWithRangeGrid))
      CampaignStatusChanged()               ← Refresh + DisplayData
```

---

## Prime-окна

### Линейная кампания (TimePeriod, Path 2)

- `GetMaxPriceByDay` — один SQL-запрос на весь период шаблона перед циклом
- Для каждой даты: prime = окна с ценой, равной максимальной цене того дня
- Разбивка: `QuantityPrime` из prime-окон + `QuantityNonPrime` из non-prime-окон
- Выбор: `OrderBy(window) ThenBy(random)` — первичный детерминированный порядок

### Веерная кампания (TimePeriod, Path 2)

- `TariffWindowWithRange.IsPrime` — вычисляется в `TariffWindowWithRange.sql`:
  - Per-massmedia: `MAX(price)` по дню → prime если `price = MAX`
  - Агрегация: `MIN(isPrime)` по всем СМИ → слот prime только если **все** СМИ прайм
- Данные загружаются через `GetSlotsForWeek` → `LoadDataSet("TariffWindowWithRange", ...)`
- Кеш по неделям (`slotsCache`) — локальная переменная-замыкание, живёт один запуск генерации
- Выбор: `OrderBy(slot.WindowDate)` — хронологически
- **Path 1 (Simple шаблон)**: prime не используется — `AddRangeIssues` выбирает `TOP 1` по свободному времени

---

## Флаг IgnoreWindowsWithTheSameFirmIssue

| Путь | Где проверяется |
|---|---|
| Simple `AddSimpleIssue` | C#: `window.IsRollerOfTheFirmExist(firmId, true)` перед `AddIssue` |
| Simple `AddSimpleIssues` | C#: то же, при сборке `allWindows` |
| Range Simple `AddRangeIssues` | SQL: `NOT EXISTS (issue той же фирмы в окне)` внутри SP |
| **Range TimePeriod** | Передаётся через `AddIssuesRangeTimePeriod` → `AddIssuesRange` → SQL `AddRangeIssues` |

---

## Сравнение всех путей

| Аспект | Path 1, Simple, Linear | Path 1, Simple, Range | Path 2, TimePeriod, Linear | Path 2, TimePeriod, Range |
|---|---|---|---|---|
| Доступность | `tbbTemplate` | `tbbTemplate` | `tbbTemplate2` | `tbbTemplate2` |
| Клик на ячейку нужен | Да (задаёт время) | Да (задаёт время) | Нет | Нет |
| Окон/слотов на дату | 1 | 1 per СМИ | N (по диапазону) | N (по диапазону) |
| Prime-логика | Нет | Нет | Да | Да |
| IssueIUD вызовов per day | 1 | N СМИ | Qty или Prime+NonPrime | Qty×N или (P+NP)×N |
| Recalculate | 1 раз в finally | N раз per day | 1 раз в finally | **1 раз в finally** |
| Удаление | Да | Да | Нет | Нет |
| `_campaign` в FrmGenerator | Задан | `null` | Задан | `null` |
| `_action` в FrmGenerator | `null` | `null` | `null` | **Задан** |
| FrmGenerator конструктор | campaign | updateDB-делегат | campaign | **updateTimePeriodDB-делегат** |

---

## Файлы и методы

| Файл | Методы / члены |
|---|---|
| `Client\Forms\CampaignForm.cs` | `tbbTemplate_Click`, `tbbTemplate2_Click` (ветки Linear/Range), `grid_CellClicked`, `IsRangeCampaign` |
| `Client\Forms\EditIssuesForm.cs` | `OnLoad` — `tbbTemplate2` visible через `base.ProcessToolbar()` |
| `Client\Forms\FrmTemplate.cs` | `Template` property: StartDate, FinishDate, WeekDays, IsModeAdd, IgnoreWindowsWithTheSameFirmIssue |
| `Client\Forms\FrmTemplate2.cs` | `SaveData2Template`: StartTime, FinishTime, Quantity, QuantityPrime, QuantityNonPrime |
| `Client\Forms\FrmGenerator.cs` | `Generate`, `AddIssues` (4 ветки), `DeleteIssues`, `AddSimpleIssue`, `AddSimpleIssues`, `AddIssuesFromWindows`, `GetPrimePriceForDate` |
| `Client\Classes\IssueTemplate.cs` | `MoveNext`, `Reset`, `SetTime`, `IsDateCovered`, `TemplateType`, `IgnoreWindowsWithTheSameFirmIssue`, `Quantity`, `QuantityPrime`, `QuantityNonPrime`, `StartTime`, `FinishTime` |
| `Client\Controls\TariffWithRangeGrid.cs` | `AddIssuesRange(DateTime, bool, recalculate=true)`, `AddIssuesRangeTimePeriod`, `GetSlotsForWeek`, `GetWeekMonday`, `DeleteIssuesRange` |
| `Client\Controls\TimePeriodAddResult.cs` | `Rows`, `Errors`, `ExpectedCount` |
| `Client\Classes\TariffWindowWithRange.cs` | `IsPrime`, `WindowDate` |
| `Client\Classes\Campaign.cs` | `AddIssue`, `RecalculateAction` |
| `Client\Classes\CampaignOnSingleMassmedia.cs` | `GetIssuesForDate` (delete simple) |
| `Client\Classes\TariffWindowWithRollerIssues.cs` | `GetWindowByDate`, `IsRollerOfTheFirmExist` |
| `Client\Classes\Massmedia.cs` | `GetMaxPriceByDay` |

---

## SQL-процедуры

| Процедура | Путь | Роль |
|---|---|---|
| `[dbo].[IssueIUD]` AddItem | Simple per-window; Range: внутри `AddRangeIssues` | Создаёт один выпуск |
| `[dbo].[IssueIUD]` DeleteItem | Simple delete; Range: внутри `MasterIssueDelete` | Удаляет один выпуск |
| `[dbo].[AddRangeIssues]` | Range add (Path 1 и Path 2) | Курсор → IssueIUD × N СМИ |
| `[dbo].[MasterIssueDelete]` | Range delete (Path 1) | Курсор → IssueIUD DeleteItem × N |
| `[dbo].[ActionRecalculate]` | Simple: 1 раз в finally; Range Simple: per-day; Range TimePeriod: 1 раз в finally | Пересчёт суммы акции |
| `[dbo].[GetMaxPriceByDay]` | TimePeriod линейная, один раз перед циклом | Max price per day → определение prime |
| `[dbo].[TariffWindowWithRange]` | Загрузка грида и `GetSlotsForWeek` (per-week) | Слоты с `isPrime` для веерного грида и TimePeriod генерации |

---

## Риски

| # | Риск |
|---|---|
| 1 | **Нет транзакции на весь шаблон** — каждый день независим. Ошибка на дне 5 из 10 оставляет дни 1–4 записанными |
| 2 | **`IssueTemplate.Reset()` вызывается дважды** в `Generate()`. Код между ними (`DaysCount`) полностью проходит итератор. Не нарушать этот порядок |
| 3 | **Recalculate N раз для Range Simple** — на каждую дату шаблона. Range TimePeriod этой проблемы лишён (1 Recalculate в finally) |
| 4 | **Повторный запуск шаблона** — дубли не блокируются ни для simple, ни для range |
| 5 | **Range Simple (Path 1): prime не учитывается** — `AddRangeIssues` выбирает окно по свободному времени |
| 6 | **Ночное вещание в Range TimePeriod** — слоты после полуночи имеют `WindowDate.Date` следующего дня. Фильтр `slot.WindowDate.Date == date.Date` их пропустит |
| 7 | **`IgnoreWindowsWithTheSameFirmIssue`: разные уровни** — C# для simple, SQL для range |
| 8 | **`CampaignStatusChanged` не вызывается для range** после FrmGenerator |
| 9 | **`slotsCache` не переиспользуется** между разными запусками FrmTemplate2 — это намеренно; при каждом запуске данные перезагружаются |
