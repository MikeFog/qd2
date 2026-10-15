# Сценарий: добавление рекламных выпусков по шаблону

Генерация множества выпусков за одно действие пользователя.
Работает для **линейной** кампании (оба пути). Для **веерной** — оба пути реализованы.

> **Веб (с 15.10.2026).** В вебе шаблоны реализованы заново, а не портом `FrmGenerator`:
> `Client/Classes/TemplatePlanner.cs` (выбор окон дня), `PlacementTemplate.cs` (параметры Шаблонов 1–3),
> `TemplateSource.cs` (запуск на линейке), `TemplateRangeSource.cs` (запуск на веере),
> `RollerAllocationQueue.cs` (очередь роликов, общая с десктопом). Решения и отличия от десктопа —
> `docs/tasks/web-action-forms.md`, разделы «Этап 5 — Шаблоны 1–3 и «Отменить последнее»: решения» и «Этап 5.5».
> Ниже описан **десктоп**; спецификацией считается его код, а не эта карта (сверено с кодом 14.10.2026).

> Связанные сценарии:
> - `docs/scenarios/issue-add-click-to-db.md` — одиночный клик, линейная кампания
> - `docs/scenarios/range-issue-add-click-to-db.md` — одиночный клик, веерная кампания
> - `docs/scenarios/template3-roller-distribution.md` — Шаблон №3 (`tbbTemplate3`, `FrmTemplate3`); здесь он упомянут только там, где делит код с Шаблонами 1 и 2

Кнопки тулбара `CampaignForm`: `tbbTemplate` — Шаблон 1 («Шаблон для внесения роликов», режим «клик по окну»),
`tbbTemplate2` — Шаблон 2 (интервал времени), `tbbTemplate3` — Шаблон 3 (несколько роликов), `tbbTemplateUndo` («Отменить»).
Для модульных, пакетно-модульных и спонсорских кампаний все четыре скрыты (`CampaignForm.ProcessToolbar`).

---

## Точки входа в CampaignForm

### Path 1 — tbbTemplate (Simple шаблон)

Доступен для: линейной кампании, **веерной кампании**.

```
tbbTemplate_Click                                      [CampaignForm.cs]
  │   Если кнопка нажата (Checked), открывает FrmTemplate → пользователь задаёт:
  │     StartDate, FinishDate
  │     WeekDays (чекбоксы) или OddEven (чётные/нечётные дни)
  │     IsModeAdd (добавить / удалить) — пишется в _template по OK
  │     IgnoreWindowsWithTheSameFirmIssue — пишется по OK; при открытии диалога
  │       всегда отмечен (из _template не читается)
  │   TemplateType = Simple (одно окно на дату) — выставляет конструктор FrmTemplate
  │     сразу, ещё до OK, вместе с Day2AddMode = WeekDays
  │   OK: _template = form.Template, SetEditMode() → EditMode.Template;
  │   Отмена: кнопка снимается
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
  │         (WindowDate линейного окна = windowDateActual; у слота веера — начало получаса)
  │     Показывает подтверждение (Globals.ShowQuestion; ключ iMessage
  │       StartIssueGeneration, при IsModeAdd=false — StartIssuesDeleteByTemplate)
  │
  ├── [ProgramIssuesGrid2]  — ветка есть в коде, но tbbTemplate для спонсорских
  │     скрыта (ProcessToolbar), из интерфейса не достигается
  │     new FrmGenerator(_template, _campaign, SponsorProgram, tariffId, price, bonus)
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

Значения по умолчанию в `FrmTemplate`: период — завтра … ближайшее воскресенье (`GetDefaultPeriod`;
если `_template.StartDate`/`FinishDate` ещё не заданы), все дни недели отмечены, режим «Добавить»
(`rbModeAdd.Checked = _template.IsModeAdd`; `rbModeDelete` из шаблона не восстанавливается — после запуска
«Удалить» диалог открывается без выбранного режима, а OK без выбора даёт «Надо выбрать что вы собираетесь делать…»),
флаг «Не использовать окна где есть ролики данной фирмы» отмечен. Дата начала позже даты конца в `FrmTemplate`
не проверяется — `IssueTemplate.MoveNext` просто не даёт ни одной даты.
Веер работает по отмеченным в чек-листе линейным кампаниям (`TariffWithRangeGrid.SelectedCampaignIds` → `@campaignIDs`).

### Path 2 — tbbTemplate2 (TimePeriod шаблон)

Доступен для: **линейной и веерной кампании**.
Кнопка видна в обоих типах форм: видимость задаёт `CampaignForm.ProcessToolbar`, в `EditIssuesForm`
(`Client\Forms\CreateActionMaster\EditIssuesForm.cs`) отдельной настройки `tbbTemplate2` нет.

```
tbbTemplate2_Click                                     [CampaignForm.cs]
  │   tbbTemplate.Checked = false; SetEditMode()  (до 15.10.2026 SetEditMode не вызывался — Д-1, см. ниже)
  │   roller == null → сообщение RollerNotSelected, выход
  │   Открывает FrmTemplate2 → пользователь задаёт:
  │     StartDate, FinishDate
  │     StartTime, FinishTime (интервал времени: начало включается; конец включается
  │       у линейки и исключается у веера — см. «Отбор окон: линейка против веера»)
  │     WeekDays или OddEven (как в FrmTemplate)
  │     Quantity (всего) — или раздельно:
  │       QuantityPrime / QuantityNonPrime (если cbSplitPrime.Checked → Quantity = 0)
  │     IgnoreWindowsWithTheSameFirmIssue
  │   IsModeAdd = true — только если FrmTemplate2 создал НОВЫЙ IssueTemplate. Если взят
  │     существующий _template (StartTime и FinishTime в нём уже заданы, конструктор FrmTemplate2),
  │     IsModeAdd остаётся прежним, в том числе false после «Удалить» в Шаблоне 1 (см. «Риски» №10)
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

Значения по умолчанию в `FrmTemplate2` (если создаётся новый шаблон): период — завтра … ближайшее воскресенье, интервал 06:00–21:00,
`Quantity` = 2 (`txtQuantity.Minimum` = 1), все дни недели, «Не использовать окна…» отмечен, `cbSplitPrime` снят
(`cbSplitPrime.Checked = Quantity == 0`). У `numQuantityPrime`/`numQuantityNonPrime`/`txtQuantity` в Designer нет `Maximum`,
действует умолчание `NumericUpDown` (100) — по знанию WinForms, в коде явно не задано.
Проверки OK (`btnOk_Click`): при делении на прайм/офф-прайм обе нормы нулевые — `PrimeAndNonPrimeQuantityMissing`;
`StartDate > FinishDate` — `TemplateStartFinishDateError`; `StartTime >= FinishTime` — `TemplateStartFinishTimeError`
(интервал нулевой ширины задать нельзя).

**Д-1** (`docs/action-forms.md`, **исправлено 15.10.2026**): `tbbTemplate2_Click` и `tbbTemplate3_Click` снимали кнопку `tbbTemplate`, но `SetEditMode()`
не звали (он вызывался только из `tbbStart_CheckedChanged` и `tbbTemplate_Click`) — грид оставался в `EditMode.Template`, и следующий
клик по окну снова запускал генератор с текущим `_template`. Теперь оба обработчика зовут `SetEditMode()` сразу после снятия кнопки.

---

## FrmGenerator.Generate() — основной цикл

```
OnShown → Generate()                                   [FrmGenerator.cs]

  template.Reset()
  pbProgress.Maximum = template.DaysCount   ← полный проход итератора (геттер сам кончается Reset())
  template.Reset()                          ← ещё один явный Reset (порядок не менять)

  while (template.MoveNext()):              ← итерация по датам
  {
    try {
      pos = IsModeAdd ? AddIssues() : DeleteIssues()
      foreach po in pos: grdSuccess.AddRow(po)
    }
    catch { grdFail.AddRow(ошибка по дате) }   ← текст: Globals.GetMessage(ex.Message, {issueDate})
    pbProgress.Value++
    Application.DoEvents()
  }

  if (_rollerQueue != null && _rollerQueue.Count > 0)   ← только Шаблон 3
    grdFail.AddRow("Недостаточно рекламных окон за весь период шаблона — …")

  finally {
    if (_campaign != null)
      _campaign.RecalculateAction()         ← линейная: 1 раз после всего цикла
    else if (_action != null)
      _action.Recalculate()                 ← веерная TimePeriod: 1 раз после цикла
    // веерная Simple (Path 1): _campaign=null, _action=null →
    //   Recalculate вызывается внутри каждого AddIssuesRange (N раз)
  }
```

- `OnLoad`: при `!_template.IsModeAdd` окно называется «Удаление рекламных выпусков по шаблону», таблицы — «Удаленные выпуски» / «Ошибки удаления».
  `OnClosing` не даёт закрыть окно, пока виден `pbProgress`.
- `AddedObjects` — выпуски, добавленные этим запуском (для «Отменить»): линейка — сами `Issue`, веер — `MasterIssue` по строке `AddedIssues`;
  `CampaignForm` после `ShowDialog` кладёт их в `_lastTemplateAddedIssues` (или `null`, если ничего не добавлено — в том числе после запуска «Удалить»,
  так что удаление по шаблону стирает возможность отменить предыдущее добавление) и включает `tbbTemplateUndo`.

---

## AddIssues() — маршрутизация по типу

```
AddIssues()                                            [FrmGenerator.cs]
  │
  ├── _updateTimePeriodDB != null  (range + TimePeriod, Path 2 веерная)
  │     TimePeriodAddResult result = _updateTimePeriodDB(currentDate)
  │       = rangeGrid.AddIssuesRangeTimePeriod(date, startTime, finishTime,
  │           quantity, quantityPrime, quantityNonPrime, ignoreFlag, slotsCache)
  │         │
  │         │  1. weekMonday = GetWeekMonday(date)
  │         │  2. slots = GetSlotsForWeek(weekMonday, slotsCache)
  │         │       → если нет в кеше: LoadDataSet("TariffWindowWithRange", ...)
  │         │         (dateStart = weekMonday, actionID, campaignIDs отмеченных кампаний)
  │         │         → new TariffWindowWithRange(row) для каждой строки
  │         │  3. фильтр: slot.WindowDate.Date == date.Date
  │         │             && TimeOfDay >= StartTime (начало включается)
  │         │             && TimeOfDay <  FinishTime (конец ИСКЛЮЧАЕТСЯ; XML-комментарий
  │         │                метода про «обе границы включительно» устарел — действует код)
  │         │             && (позиция Undefined || IsPositionAvailable(slot))
  │         │             && (!ignoreFlag || !HasFirmIssuesFlags(slot))
  │         │  4. OrderBy(slot) — IComparable: TimeWithUnConfirmed по убыванию (сначала
  │         │       самые свободные), ThenBy(new Random().Next()) — среди равных случайно
  │         │  5. если Quantity > 0:
  │         │       selectedSlots = slotsForDate.Take(quantity)
  │         │     иначе (prime split):
  │         │       prime    = slotsForDate.Where(IsPrime).Take(quantityPrime)
  │         │       nonPrime = slotsForDate.Where(!IsPrime).Take(quantityNonPrime)
  │         │  6. foreach slot: AddIssuesRange(slot.WindowDate, ignoreFlag,
  │         │                                  recalculate: false)
  │         │       → ExecuteNonQuery("AddRangeIssues", ...) → IssueIUD × N кампаний
  │         │         (линейные кампании акции, отобранные @campaignIDs)
  │         │       → AddedIssues.NewRow (in-memory)
  │         │       on error: result.Errors.Add(ex)  — не прерывает цикл
  │         │  7. return result (Rows + Errors + ExpectedCount)
  │     │
  │     foreach ex in result.Errors → AddErrorInfo → grdFail  (per slot)
  │     if result.Rows.Count < result.ExpectedCount → AddErrorInfo → grdFail (summary:
  │       «Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.»;
  │       при отказах слотов появляются и строки отказов, и эта строка)
  │     AddedObjects.Add(new MasterIssue(row)) для каждой строки Rows   ← для «Отменить»
  │     return result.Rows.Select(r => new RollerIssue(r))
  │     [Recalculate — НЕ здесь, выполняется в finally]
  │
  ├── _updateDB != null  (range + Simple, Path 1 веерная)
  │     DataRow row = _updateDB(currentDate)
  │       = rangeGrid.AddIssuesRange(currentDate, ignoreFlag)
  │           → ExecuteNonQuery("AddRangeIssues", ...)
  │               SQL курсор → EXEC IssueIUD AddItem × N (по линейным кампаниям акции,
  │               отобранным @campaignIDs; обычно по одной на станцию); окно каждой станции —
  │               TOP 1 в получасе [время; время+30 мин) по наибольшему свободному времени
  │           → _action.Recalculate()          ← на каждую дату (recalculate=true)
  │           → AddedIssues.NewRow (in-memory)
  │     AddedObjects.Add(new MasterIssue(row))
  │     return new RollerIssue(row)
  │
  ├── _rollerQueue != null  (Шаблон 3, линейная) → AddMultiRollerIssues()
  │     (до проверки типа шаблона; GetMaxPriceByDay грузится так же — см. template3-roller-distribution.md)
  │
  ├── TemplateType.Simple, module=null, program=null  (Path 1 линейная)
  │     AddSimpleIssue()
  │       → TariffWindowWithRollerIssues.GetWindowByDate(currentDate, massmedia)
  │           DataAccessor.DoAction → по параметрам TariffWindowRetrieve*, ветка @actualDate:
  │           TOP 1 окно, накрывающее момент (windowDateActual <= t <= windowDateActual + duration);
  │           disabled / «только для модулей» не отсекаются (заблокированное окно отклонит
  │           hlp_IssueVerify: DisabledInsertRoller); не найдено → TariffWindowNotFound
  │       → if (!ignoreFlag || !window.IsRollerOfTheFirmExist(firmId, true))
  │             иначе → IssueWithTheSameFirmExists
  │         позиция задана и занята ПОДТВЕРЖДЁННЫМ выпуском окна → FirstLastIssueError
  │             _campaign.AddIssue(roller, window, position, grantorID)
  │               → new RollerIssue(...) → issue.Update()
  │                   → DataAccessor.DoAction → IssueIUD AddItem
  │         AddedObjects.Add(issue)
  │
  ├── TemplateType.TimePeriod, module=null, program=null  (Path 2 линейная)
  │     (перед AddSimpleIssues: _maxPrices = Massmedia.GetMaxPriceByDay(StartDate, FinishDate),
  │      лениво на первой итерации — один запрос на весь период, и при Quantity > 0 тоже)
  │     AddSimpleIssues()
  │       → Massmedia.GetPriceList(currentDate) (прайс-листа нет → PriceListDoesntExist)
  │         + MassmediaPricelist.GetTariffWindows(currentDate, currentDate, null,
  │             showTrafficWindows:false, showDisabledWindows:false, useActualTime:true)
  │           свежий SQL на каждую дату, независимо от грида; у прайс-листа
  │           excludeSpecialWindows = excludeModuleTariffs = true (умолчание поля)
  │       → фильтр по StartTime..FinishTime: (hour*60+min) >= start И <= finish — обе включительно
  │         (колонки hour/min = windowDateActual при useActualTime)
  │       → фильтр IgnoreWindowsWithTheSameFirmIssue (C#: IsRollerOfTheFirmExist(firmId, true)
  │         на КАЖДОЕ окно — отдельный вызов БД) И позиция не занята подтверждённым выпуском
  │       → если Quantity != 0:
  │             OrderBy(window) ThenBy(random), берём Quantity
  │             AddIssuesFromWindows → IssueIUD × Quantity
  │       → если Quantity == 0 (prime split):
  │             prime-окна (price == maxPriceForDay)     → QuantityPrime
  │             non-prime  (price != maxPriceForDay)     → QuantityNonPrime
  │             нет цены на день → прайм-окон нет, всё идёт в non-prime
  │             AddIssuesFromWindows × 2 → IssueIUD × (QuantityPrime + QuantityNonPrime)
  │       AddIssuesFromWindows: на каждый выпуск берётся первое окно списка и убирается из него
  │         в любом случае (отказ записи окно не возвращает); отказ → строка в grdFail,
  │         нехватка → строка «Недостаточно рекламных окон [прайм / офф прайм] …»
  │         ({0} в тексте — число размещённых выпусков, а не окон)
  │
  ├── module is Module     → AddModuleIssue()    → IssueIUD AddItem
  ├── module is PackModule → AddPackModuleIssue() → IssueIUD AddItem
  └── program != null      → AddProgramIssue()   → program SP
      (три нижние ветки с шаблонами 1–3 из интерфейса не достигаются: tbbTemplate* скрыты
       для модульных, пакетно-модульных и спонсорских кампаний)
```

---

## DeleteIssues() — ветка удаления

Выбирается по `_template.IsModeAdd == false`. Из интерфейса задаётся только в **Path 1** (FrmTemplate, «Удалить выбранные»);
FrmTemplate2 режима удаления не предлагает, но `IsModeAdd` в нём не сбрасывается, если он взял уже существующий `_template`
(`IsModeAdd = true` ставится лишь новому шаблону) — см. «Риски» №10.

```
DeleteIssues()                                         [FrmGenerator.cs]
  │
  ├── _deleteDB != null  (range, Path 1)
  │     rangeGrid.DeleteIssuesRange(currentDate)
  │       → DataAccessor.ExecuteNonQuery("MasterIssueDelete", ...)
  │           параметры: ВЫБРАННЫЙ ролик (Roller.RollerId) и позиция, @campaignIDs отмеченных кампаний
  │           SQL курсор → EXEC IssueIUD DeleteItem × N: по одному выпуску на кампанию
  │           в получасе [время; время+30 мин), пропуская кампании без такого выпуска
  │       → _action.Recalculate()
  │       → удаление строки из AddedIssues (in-memory)
  │
  └── Simple (линейная; _deleteDB == null)
        ((CampaignOnSingleMassmedia)_campaign).GetIssuesForDate(currentDate)
          → DataAccessor.LoadDataSet("IssuesByDate", {campaignID, issueDate = date.Date, massmediaID})
          → SQL берёт день по tw.dayOriginal, C# оставляет выпуски с issueDate (windowDateActual) == currentDate —
            точное совпадение с временем кликнутого окна (SetTime); ролик и позиция НЕ фильтруются
        foreach issue: issue.Delete(true)
          → IssueIUD DeleteItem × кол-во выпусков за дату
        (для Path 2 с протёкшим IsModeAdd=false currentDate = полночь, т.к. FrmTemplate2 пишет
         StartDate = date.Date; для веерного Path 2 _deleteDB и _campaign равны null → NRE на каждую дату → grdFail)
```

---

## Транзакции

| Путь | Транзакция |
|---|---|
| Simple: `IssueIUD` per window | Авто по флагу `isTransactionRequired` метаданных процедуры (`ProcedureConfig`) — per-call; значение флага для `IssueIUD` в БД не проверялось |
| Range: `AddRangeIssues` | `DataAccessor.ExecuteNonQuery(name, params)` всегда с `isTransactionRequired = true`: один вызов = один слот на всех отмеченных станциях, `RAISERROR` на любой станции → исключение → `Rollback` всего вызова (при умолчании настройки `IsUseCustomTransaction = false`). Слоты одного дня (Path 2) независимы |
| **Весь шаблон целиком** | **Нет** — каждый день независим. Ошибка на дне 5 из 10 не откатывает дни 1–4 |

---

## Recalculate

| Путь | Когда | Сколько раз |
|---|---|---|
| Simple линейная | `FrmGenerator.finally`, `_campaign.RecalculateAction()` | **1 раз** — после всего цикла |
| Range Simple (Path 1) | Внутри каждого `AddIssuesRange` — `_action.Recalculate()` | **N раз** — по числу дат шаблона |
| **Range TimePeriod (Path 2)** | `FrmGenerator.finally`, `_action.Recalculate()` | **1 раз** — после всего цикла |
| Range, удаление (Path 1) | `DeleteIssuesRange` — `_action.Recalculate()` | **N раз** — по числу дат шаблона |
| `CorrectPaymentAction` | Внутри каждого `Recalculate` (`ActionOnMassmedia.Recalculate`), если `TotalPrice` уменьшился и акция подтверждена | Пропорционально числу Recalculate |

---

## Refresh UI после закрытия FrmGenerator

```
CampaignForm (после form.ShowDialog):

  _lastTemplateAddedIssues = form.AddedObjects.Count > 0 ? form.AddedObjects : null
  tbbTemplateUndo.Enabled  = _lastTemplateAddedIssues != null

  if (_template.IsDateCovered(_tariffGrid.StartDate, _tariffGrid.FinishDate))
      RefreshGrid()                         ← всегда, если период пересекается

  // для range (Path 1 и Path 2): CampaignStatusChanged() не вызывается; список и статистику обновляет
  // EditIssuesForm.TariffGridRefreshed():
  //   → ShowCurrentIssues: grdCurrentCampaignIssues.DataSource = AddedIssues.DefaultView
  //     (перед этим FillAddedIssuesRollerNumbers — номера роликов в колонку «№»)
  //   → _action.DisplayData(lstStat)
  // Путь к нему разный: в Path 1 — EditIssuesForm.ShowWindowIssues (override, зовётся в конце grid_CellClicked);
  // в Path 2 ShowWindowIssues не вызывается — приходит событие TariffGrid.GridRefreshed из RefreshGrid()
  // (подписка в EditIssuesForm.OnLoad), то есть только когда сработал IsDateCovered(...)

  // для simple Path 1 (grid_CellClicked):
  if (!(_tariffGrid is TariffWithRangeGrid))
      CampaignStatusChanged()               ← _campaign.ReloadData() + DisplayCampaignData(lstStat)
  ShowWindowIssues(tariffWindow)            ← в конце grid_CellClicked, всегда

  // для simple Path 2 (tbbTemplate2_Click, ветка Linear):
  CampaignStatusChanged()                   ← без проверки типа грида (ветка Range его не зовёт)
```

Отмена (`tbbTemplateUndo_Click` → `UndoLastTemplateAdd`): после подтверждения «Отменить последнее добавление выпусков по шаблону? ({0} шт.)»
каждый объект из `_lastTemplateAddedIssues` удаляется по одному через `Delete(true)` с накоплением ошибок; запись одна и сбрасывается
при отмене и перезаписывается каждым следующим запуском. Для веера это `MasterIssue.Delete` → `MasterIssueDelete`. Подробности и решения для веба —
`docs/tasks/web-action-forms.md`, этапы 5.1 и 5.5.

---

## Prime-окна

### Линейная кампания (TimePeriod, Path 2)

- `GetMaxPriceByDay` — один SQL-запрос на весь период `StartDate..FinishDate`; грузится лениво на первой итерации (`AddIssues`),
  для любого TimePeriod-запуска, в том числе при `Quantity != 0`, где прайм не нужен. `MAX(tw.price)` по **всем** окнам станции за день
  (`dayActual`), без фильтров по `isDisabled`, `maxCapacity`, «только для модулей»
- Для каждой даты: prime = окна-кандидаты с `Price` (цена самого окна), равной максимальной цене того дня; нет цены на день — прайм-окон нет,
  всё идёт в non-prime
- Разбивка: `QuantityPrime` из prime-окон + `QuantityNonPrime` из non-prime-окон
- Выбор: `OrderBy(window)` — `TimeLeftWithUnconfirmed` (= `Duration − TimeInUseConfirmed − TimeInUseUnconfirmed`) по убыванию, сначала самые
  свободные; `ThenBy(random)` — среди равных случайный порядок (`new Random()` без зерна на каждый день). Порядок розыгрыша: сначала весь прайм-список
  получает жребий и сортируется, затем офф-прайм

### Веерная кампания (TimePeriod, Path 2)

- `TariffWindowWithRange.IsPrime` — вычисляется в `TariffWindowWithRange.sql`:
  - Per-massmedia: `MAX(price)` по календарному дню `windowDateActual` среди окон недельной выгрузки (`maxCapacity = 0`, `isDisabled = 0`,
    тариф не `isForModuleOnly`) → окно prime, если `price = MAX`
  - Внутри станции: `MIN(isPrime)` по окнам получаса; по всем СМИ: `MIN(isPrime)` → слот prime только если **все** СМИ прайм
    (слот вообще попадает в выгрузку только если окно есть у каждой отмеченной станции — `HAVING COUNT(*) = @mmCnt`)
- Данные загружаются через `GetSlotsForWeek` → `LoadDataSet("TariffWindowWithRange", ...)`
- Кеш по неделям (`slotsCache`) — локальная переменная-замыкание, живёт один запуск генерации
- Сортировка окон — до деления на prime/non-prime, одним списком на весь день (для обоих случаев — quantity > 0 и prime split):
  - Первичная: `OrderBy(w)` — `IComparable<TariffWindowWithRange>` по `TimeWithUnConfirmed` (убывание: **сначала самые свободные**)
  - Вторичная: `ThenBy(rand)` — среди одинаково свободных — **рандомный порядок**
  - Затем `Where(IsPrime).Take(P)` и `Where(!IsPrime).Take(NP)`. У линейки наоборот: сначала деление, потом сортировка каждой части — распределение
    то же, последовательность обращений к `Random` другая (комментарий в `TemplatePlanner.PickDay`)
- **Path 1 (Simple шаблон)**: prime не используется — `AddRangeIssues` выбирает `TOP 1` по свободному времени (при позиции — сначала окна без отказа по позиции, Д-15)

---

## Отбор окон: линейка против веера (Шаблон 2; для Шаблона 3 те же фильтры)

Правила **разные**, спецификация — код (`FrmGenerator.AddSimpleIssues` и `TariffWithRangeGrid.AddIssuesRangeTimePeriod`).

| Правило | Линейка (`FrmGenerator.AddSimpleIssues`) | Веер (`TariffWithRangeGrid.AddIssuesRangeTimePeriod`) |
|---|---|---|
| Откуда кандидаты | `Massmedia.GetPriceList(день)` → `MassmediaPricelist.GetTariffWindows(день, день, null, showTrafficWindows:false, showDisabledWindows:false, useActualTime:true)`; у прайс-листа `excludeSpecialWindows` и `excludeModuleTariffs` по умолчанию `true` — без «особых» окон, тарифов «только для модулей» и заблокированных. Свежий SQL на каждую дату. День окна — по `dayOriginal` (`TariffWindowRetrieve`), часы интервала — из `windowDateActual`; ограничения по `maxCapacity` нет — окно с лимитом отклонит `IssueIUD` (`hlp_IssueVerify` → `DisabledInsertSimpleRoller` для типов роликов 1, 8, 9) | Недельная выгрузка `TariffWindowWithRange` (по отмеченным кампаниям) в `slotsCache`; слот — получасовой блок, общий для всех отмеченных станций; в выгрузку попадают только окна с `maxCapacity = 0` и не заблокированные; день — календарный по времени слота (`slot.WindowDate.Date == date.Date`) |
| Начало интервала | включается: `(hour*60 + min) >= start` | включается: `TimeOfDay >= tsStart` |
| **Конец интервала** | **включается**: `(hour*60 + min) <= finish` | **исключается**: `TimeOfDay < tsFinish` |
| Порядок | `OrderBy(w)` по `TimeLeftWithUnconfirmed` ↓ (самые свободные первыми), `ThenBy(random)` | `OrderBy(w)` по `TimeWithUnConfirmed` ↓, `ThenBy(random)`; `Random` — `new Random()` без зерна на каждый день, у обоих |
| «В день» (`Quantity != 0`) | первые N окон | первые N слотов |
| Прайм / офф-прайм | прайм = `Price` окна равна `GetMaxPriceByDay` за день; сначала деление на прайм и офф-прайм, потом сортировка каждой части (прайм первым) | признак слота `IsPrime`; сортировка всего дня, потом деление |
| Позиция | отбрасываются окна, где позиция занята **подтверждённым** выпуском (`IsFirst/Second/LastPositionOccupied`); макеты не смотрятся | `IsPositionAvailable`: подтверждённые, а при «Учитывать макеты» (`ShowUnconfirmed`) ещё и отсутствие неподтверждённых на позиции; слот — по всем станциям сразу |
| Флаг «Не использовать окна…» | `IsRollerOfTheFirmExist(firm, true)` на каждое окно (вызов БД) | признаки слота `HasIssues \|\| HasCurrentActionIssues \|\| (ShowUnconfirmed && HasIssuesUnconfirmed)`; плюс повторная проверка в SQL `AddRangeIssues` |
| Единица записи | окно → `IssueIUD` | слот → `AddRangeIssues` (выпуск на каждой отмеченной станции) |
| Нехватка кандидатов | строка «Недостаточно рекламных окон …» (общая / прайм / офф прайм) | строка «Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.» |

Для Шаблона 1 (один клик) выбор окна иной: линейка берёт окно, накрывающее время клика (`GetWindowByDate`), веер — `TOP 1` окно каждой станции в получасе
(SQL `AddRangeIssues`): при позиции сначала окна, где её не отклонят (занята подтверждённым выпуском — `hlp_IssueVerify` → `FirstLastIssueError`;
выпуском этой же акции — `IssueIUD` → `PositionErrorForTheSameAction`; затем окна, куда ролик не помещается, — чтобы отказ по позиции не подменялся `WindowOverflow` тесного окна; Д-15, Deploy/48), затем по наибольшему
свободному времени. Префильтра по позиции и флагу фирмы на C# у Шаблона 1 веера нет — окно выбирает и отклоняет SQL: позицию — `AddRangeIssues`/`IssueIUD`, фирму — `AddRangeIssues`.

---

## Флаг IgnoreWindowsWithTheSameFirmIssue

| Путь | Где проверяется |
|---|---|
| Simple `AddSimpleIssue` | C#: `window.IsRollerOfTheFirmExist(firmId, true)` перед `AddIssue`; иначе `IssueWithTheSameFirmExists` |
| Simple `AddSimpleIssues` | C#: то же, при сборке `allWindows` — вызов БД на каждое окно |
| Range Simple `AddRangeIssues` | Только SQL (префильтра на C# нет): `NOT EXISTS (issue той же фирмы в окне)` внутри SP по каждой кампании. Считает подтверждённые выпуски, а неподтверждённые — только при `@considerUnconfirmed` («Учитывать макеты»). Нет окна — `IssueWithTheSameFirmExists` (если оно отсечено флагом) или `CannotAddRangeIssues` |
| **Range TimePeriod** | C#-префильтр слота `HasIssues \|\| HasCurrentActionIssues \|\| (ShowUnconfirmed && HasIssuesUnconfirmed)` в `AddIssuesRangeTimePeriod`; плюс флаг передаётся через `AddIssuesRange` в SQL `AddRangeIssues` (повторная проверка) |

У линейки неподтверждённые выпуски фирмы считаются всегда (`showUnconfirmed: true`), у веера — по «Учитывать макеты». `IsRollerOfTheFirmExist` идёт через
`DataAccessor.DoAction` (`TariffWindow`, `LoadIssues`) и по именам параметров соответствует `WindowIssuesRetrieve`*: выпуски акций этой фирмы в окне
(`a.firmID`), при `showUnconfirmed = 1` — любые в не удалённых акциях (текущая акция входит), иначе только подтверждённые.

---

## Сравнение всех путей

| Аспект | Path 1, Simple, Linear | Path 1, Simple, Range | Path 2, TimePeriod, Linear | Path 2, TimePeriod, Range |
|---|---|---|---|---|
| Доступность | `tbbTemplate` | `tbbTemplate` | `tbbTemplate2` | `tbbTemplate2` |
| Клик на ячейку нужен | Да (задаёт время) | Да (задаёт время) | Нет | Нет |
| Окон/слотов на дату | 1 | 1 слот (окно на каждой отмеченной станции) | N (по диапазону) | N слотов (по диапазону) |
| Prime-логика | Нет | Нет | Да | Да |
| Конец интервала времени | — | — | включается | **исключается** |
| Порядок кандидатов | — (одно окно, накрывающее время клика) | `TOP 1` в получасе: при позиции сначала без отказа по позиции, затем по свободному времени (SQL, Д-15) | самые свободные, затем случайно | самые свободные, затем случайно |
| Позиция | C#: подтверждённые выпуски окна | только `IssueIUD` | C#: подтверждённые выпуски окна | C#: `IsPositionAvailable` (макеты при «Учитывать макеты») + `IssueIUD` |
| Флаг «Не использовать окна…» | C# | SQL | C# | C# (признаки слота) + SQL |
| IssueIUD вызовов per day | 1 | N кампаний (отмеченные линейные) | Qty или Prime+NonPrime | Qty×N или (P+NP)×N |
| Recalculate | 1 раз в finally | N раз per day | 1 раз в finally | **1 раз в finally** |
| Удаление | Да (все ролики и позиции кампании в окне клика) | Да (только выбранные ролик и позиция, по одному выпуску на кампанию) | Нет (но см. «Риски» №10) | Нет (но см. «Риски» №10) |
| `_campaign` в FrmGenerator | Задан | `null` | Задан | `null` |
| `_action` в FrmGenerator | `null` | `null` | `null` | **Задан** |
| FrmGenerator конструктор | campaign | updateDB-делегат | campaign | **updateTimePeriodDB-делегат** |

---

## Файлы и методы

| Файл | Методы / члены |
|---|---|
| `Client\Forms\CampaignForm.cs` | `tbbTemplate_Click`, `tbbTemplate2_Click` (ветки Linear/Range), `grid_CellClicked`, `IsRangeCampaign`, `ProcessToolbar` (видимость `tbbTemplate*`), `SetEditMode`, `CampaignStatusChanged`, `tbbTemplateUndo_Click`, `UndoLastTemplateAdd` |
| `Client\Forms\CreateActionMaster\EditIssuesForm.cs` | `OnLoad` (`tbbTemplate`/`tbbTemplateUndo` visible), `ProcessToolbar` (вызывает `base.ProcessToolbar()`), `ShowWindowIssues` (override), `TariffGridRefreshed`, `ShowCurrentIssues` |
| `Client\Forms\FrmTemplate.cs` | конструктор (`Day2AddMode`, `TemplateType = Simple` на общем `_template`), `Template` (StartDate, FinishDate, IsOdd, Day2AddMode), `btnOk_Click` (IsModeAdd, IgnoreWindowsWithTheSameFirmIssue), `clbWeekDays_ItemCheck` (WeekDays) |
| `Client\Forms\FrmTemplate2.cs` | конструктор (новый шаблон / повторное использование), `SaveData2Template`: StartDate, FinishDate, StartTime, FinishTime, Quantity, QuantityPrime, QuantityNonPrime, TemplateType, IgnoreWindowsWithTheSameFirmIssue; `btnOk_Click` (проверки) |
| `Client\Forms\FrmGenerator.cs` | `Generate`, `AddIssues` (маршрутизация: `_updateTimePeriodDB`, `_updateDB`, `_rollerQueue`, simple, program, module), `DeleteIssues`, `AddSimpleIssue`, `AddSimpleIssues`, `AddIssuesFromWindows`, `IsPositionOccupied`, `GetPrimePriceForDate`; `AddMultiRollerIssues` — Шаблон 3 |
| `Client\Classes\IssueTemplate.cs` | `MoveNext`, `Reset`, `DaysCount`, `SetTime`, `IsDateCovered`, `IsModeAdd`, `TemplateType`, `IgnoreWindowsWithTheSameFirmIssue`, `Quantity`, `QuantityPrime`, `QuantityNonPrime`, `StartTime`, `FinishTime` |
| `Client\Controls\TariffWithRangeGrid.cs` | `AddIssuesRange(DateTime, bool, recalculate=true)`, `AddIssuesRangeTimePeriod`, `GetSlotsForWeek`, `GetWeekMonday`, `DeleteIssuesRange`, `IsPositionAvailable`, `HasFirmIssuesFlags`; `AddIssuesRangeTimePeriodMultiRoller` — Шаблон 3 |
| `Client\Controls\TimePeriodAddResult.cs` | `Rows`, `Errors`, `ExpectedCount` |
| `Client\Classes\TariffWindowWithRange.cs` | `IsPrime`, `WindowDate`, `CompareTo` (по `TimeWithUnConfirmed` ↓) |
| `Client\Classes\Domain\MasterIssue.cs` | `Delete` → `MasterIssueDelete` (отмена и `AddedObjects` у веера) |
| `Client\Classes\Campaign.cs` | `AddIssue`, `RecalculateAction` |
| `Client\Classes\ActionOnMassmedia.cs` | `Recalculate`, `CorrectPaymentAction` |
| `Client\Classes\CampaignOnSingleMassmedia.cs` | `GetIssuesForDate` (delete simple) |
| `Client\Classes\TariffWindowWithRollerIssues.cs` | `GetWindowByDate`, `IsRollerOfTheFirmExist`, `CompareTo` (по `TimeLeftWithUnconfirmed` ↓), `IsFirst/Second/LastPositionOccupied` |
| `Client\Classes\MassmediaPricelist.cs` | `GetTariffWindows` (поля `excludeSpecialWindows`, `excludeModuleTariffs` по умолчанию `true`) |
| `Client\Classes\Massmedia.cs` | `GetPriceList`, `GetMaxPriceByDay` |
| Веб: `Client\Classes\TemplatePlanner.cs`, `PlacementTemplate.cs`, `TemplateSource.cs`, `TemplateRangeSource.cs`; общий `RollerAllocationQueue.cs` | см. абзац «Веб» в начале; не часть десктопного сценария |

---

## SQL-процедуры

| Процедура | Путь | Роль |
|---|---|---|
| `[dbo].[IssueIUD]` AddItem | Simple per-window; Range: внутри `AddRangeIssues` | Создаёт один выпуск |
| `[dbo].[IssueIUD]` DeleteItem | Simple delete; Range: внутри `MasterIssueDelete` | Удаляет один выпуск |
| `[dbo].[AddRangeIssues]` | Range add (Path 1 и Path 2) | Курсор по линейным кампаниям акции (`campaignTypeID = 1`, отбор `@campaignIDs`) → `TOP 1` окно станции в получасе (не заблокированное, `maxCapacity = 0`, тариф не «только для модулей»; при позиции — сначала окна без отказа по позиции и с местом под ролик (Д-15), затем по свободному времени ↓; с `@ignoreWindowsWithTheSameFirmIssue` — без окон фирмы) → IssueIUD × N. Нет окна — `IssueWithTheSameFirmExists` / `CannotAddRangeIssues` |
| `[dbo].[MasterIssueDelete]` | Range delete (Path 1); «Отменить» у веера (`MasterIssue.Delete`) | Курсор по тем же кампаниям → один выпуск кампании с заданными роликом и позицией в получасе → IssueIUD DeleteItem; кампании без такого выпуска пропускаются |
| `[dbo].[IssuesByDate]` | Simple delete (Path 1 линейная), `GetIssuesForDate` | Выпуски кампании за день (`tw.dayOriginal`), без фильтра по ролику |
| `[dbo].[TariffWindowRetrieve]`* | `GetWindowByDate` (Path 1 линейная, ветка `@actualDate`); `MassmediaPricelist.GetTariffWindows` (Path 2 линейная) | Окно, накрывающее момент; окна дня прайс-листа с колонками `hour`/`min` |
| `[dbo].[WindowIssuesRetrieve]`* | `IsRollerOfTheFirmExist` (линейная, флаг фирмы) | Выпуски в окне; фильтр `@firmID`, `@showUnconfirmed` |
| `[dbo].[ActionRecalculate]` | Simple: 1 раз в finally; Range Simple: per-day; Range TimePeriod: 1 раз в finally | Пересчёт суммы акции |
| `[dbo].[GetMaxPriceByDay]` | TimePeriod линейная, лениво на первой итерации цикла, один раз | Max price per day (`dayActual`, все окна станции) → определение prime |
| `[dbo].[TariffWindowWithRange]` | Загрузка грида и `GetSlotsForWeek` (per-week) | Слоты с `isPrime` для веерного грида и TimePeriod генерации |

\* Привязка вызова к процедуре идёт через метаданные БД (`DataAccessor.DoAction`); в репозитории её нет, процедура выбрана по именам параметров — не проверено в метаданных.

---

## Риски

| # | Риск |
|---|---|
| 1 | **Нет транзакции на весь шаблон** — каждый день независим. Ошибка на дне 5 из 10 оставляет дни 1–4 записанными |
| 2 | **`IssueTemplate.Reset()` в `Generate()`** вызывается явно до и после `DaysCount`; сам геттер `DaysCount` тоже заканчивается `Reset()` (второй явный вызов избыточен). Код между ними полностью проходит итератор; сеттеры `IsOdd` и `Day2AddMode` тоже сбрасывают итератор. Порядок не нарушать |
| 3 | **Recalculate N раз для Range Simple** — на каждую дату шаблона (и для `DeleteIssuesRange`). Range TimePeriod этой проблемы лишён (1 Recalculate в finally) |
| 4 | **Повторный запуск шаблона** — защищает только флаг «Не использовать окна, где есть ролики данной фирмы» (по умолчанию отмечен в обоих диалогах; он считает и выпуски самой акции — у веера `HasCurrentActionIssues`, у линейки через `IsRollerOfTheFirmExist`, см. оговорку выше). Со снятым флагом повтор ставит выпуски снова: среди сообщений `hlp_IssueVerify` общего отказа «такой ролик в окне уже есть» нет; позицию ограничивает `PositionErrorForTheSameAction` в `IssueIUD` |
| 5 | **Range Simple (Path 1): prime не учитывается** — `AddRangeIssues` выбирает окно по свободному времени (при позиции — сначала без отказа по позиции, Д-15) |
| 6 | **Слоты до `broadcastStart` в Range TimePeriod** — недельная выгрузка `TariffWindowWithRange` идёт от понедельника `broadcastStart` до понедельника следующей недели, а фильтр `slot.WindowDate.Date == date.Date` календарный: слоты понедельника раньше `broadcastStart` лежат в выгрузке предыдущей недели и для понедельника не находятся. На данных после шага 1 удаления `broadcastStart` (везде `00:00`, `docs/broadcast-start.md`) не проявляется; по чтению кода и SQL, не воспроизводилось |
| 7 | **`IgnoreWindowsWithTheSameFirmIssue`: разные уровни** — линейка проверяет в C# на каждое окно (вызов БД), веерный Шаблон 2 — C#-префильтр по признакам слота плюс SQL, веерный Шаблон 1 — только SQL; неподтверждённые выпуски линейка считает всегда, веер — по «Учитывать макеты» |
| 8 | **`CampaignStatusChanged` не вызывается для range** после FrmGenerator |
| 9 | **`slotsCache` не переиспользуется** между запусками: `tbbTemplate2_Click` и `tbbTemplate3_Click` на каждый запуск создают новый `Dictionary`, поэтому недели каждый раз читаются заново (намеренно ли — по коду не видно) |
| 10 | **Режим удаления Шаблона 1 протекает в Шаблоны 2 и 3.** `FrmTemplate` пишет `IsModeAdd = false` в общий `CampaignForm._template` по OK; `FrmTemplate2` (и `FrmTemplate3`) берут этот же объект, если `StartTime`/`FinishTime` в нём уже заданы, и `IsModeAdd = true` ставят лишь новому шаблону. Последовательность: Шаблон 2 или 3 → Шаблон 1 «Удалить» → снова Шаблон 2 или 3: `FrmGenerator` открывается как «Удаление рекламных выпусков по шаблону» и вызывает `DeleteIssues`. Линейка: `GetIssuesForDate` с `currentDate` = полночь удаляет выпуски кампании, стоящие ровно в 00:00 (любого ролика); веер: NRE на каждую дату (`_campaign` и `_deleteDB` равны `null`) в `grdFail`. По чтению кода, вживую не воспроизводилось; это не Д-1 (см. №11) |
| 11 | ~~**Д-1: после Шаблонов 2 и 3 грид остаётся в `EditMode.Template`**~~ — **исправлено 15.10.2026** (`SetEditMode()` в `tbbTemplate2_Click` и `tbbTemplate3_Click`); раньше клик по окну снова запускал генератор с текущим `_template` |
| 12 | **Удаление по шаблону в линейке не различает ролики.** `GetIssuesForDate` не фильтрует по ролику и позиции — удаляются все выпуски кампании в окне клика; у веера `DeleteIssuesRange` удаляет только выбранные ролик и позицию |
| 13 | **Граница конца интервала различается:** линейка включает (`<=`), веер исключает (`<`); XML-комментарий `AddIssuesRangeTimePeriod` («обе границы включительно») устарел |
