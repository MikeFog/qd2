# Сценарий: Шаблон №3 — множественные ролики с квотами и распределением по дням

Бизнес-процесс: пользователь выбирает несколько роликов фирмы, задаёт каждому "Количество" (квоту
выходов за весь период), получает черновую оценку цены и генерирует реальные выпуски — с случайным,
но равномерным распределением роликов по дням (round-robin), без повторов одного ролика подряд внутри дня.

Работает для **линейной** и **веерной** кампании. В отличие от Шаблона 1/2 — несколько РАЗНЫХ роликов
за один запуск, а не один ролик на все даты.

> Связанные сценарии:
> - `docs/scenarios/template-issue-generation.md` — Шаблон 1/2, `FrmGenerator.Generate()`/`AddIssues()`,
>   транзакции, Recalculate — общий каркас, актуален и для Шаблона 3
> - `docs/scenarios/issue-add-click-to-db.md` — одиночный клик, линейная кампания
> - `docs/scenarios/range-issue-add-click-to-db.md` — одиночный клик, веерная кампания

---

## Точка входа

```
CampaignForm.tbbTemplate3_Click                        [CampaignForm.cs:1292]
  │
  ├── massmediaIds = GetTemplateMassmediaIds()          [CampaignForm.cs:1374]
  │     веерная: все СМИ акции (Action.Campaigns())
  │     линейная: одно СМИ текущей кампании
  │
  ├── action = IsRangeCampaign                          [CampaignForm.cs:1301]
  │       ? ((TariffWithRangeGrid)_tariffGrid).Action
  │       : _campaign.Action
  │     ⚠️ _campaign сам по себе — null для веерной (см. GetTemplateMassmediaIds — веерная
  │        вообще не трогает _campaign), поэтому Action нельзя достать через _campaign.Action
  │        внутри FrmTemplate3 — резолвится здесь и передаётся уже готовым
  │
  ├── new FrmTemplate3(Firm, _template, massmediaIds, position, _campaign, action)
  │     .ShowDialog() → пользователь выбирает ролики + квоты + расписание,
  │     жмёт "Рассчитать" (необязательно), жмёт OK
  │
  ▼  (DialogResult.OK)
  rollerQuantities = formTemplate.SelectedRollers
      .Select(r => (new Roller(r.RollerId), r.Quantity))
  │
  ├── [IsRangeCampaign] — веерная
  │     rollerQueue = new RollerAllocationQueue(rollerQuantities)
  │     new FrmGenerator(_template,
  │       date => rangeGrid.AddIssuesRangeTimePeriodMultiRoller(
  │           date, StartTime, FinishTime, Quantity, QuantityPrime, QuantityNonPrime,
  │           IgnoreWindowsWithTheSameFirmIssue, slotsCache, rollerQueue.TakeForToday),
  │       rangeGrid.Action)
  │     → если после генерации rollerQueue.Count > 0 — предупреждение "не удалось разместить N"
  │     → CampaignStatusChanged() НЕ вызывается (как и остальные range-пути)
  │
  └── [Linear] — линейная
        new FrmGenerator(_template, rollerQuantities, position,
            _campaign, RollerIssuesGrid.Pricelist, Module, grantorID)
        → CampaignStatusChanged() вызывается
```

Оба пути используют **один и тот же класс** `RollerAllocationQueue` для распределения роликов —
для веерной он создаётся прямо в `CampaignForm`, для линейной — внутри конструктора `FrmGenerator`.

---

## FrmTemplate3 — форма выбора роликов

### Грид роликов (`dgvRollers`)

Колонки: чекбокс `ColSelected` | Ролик (`name`) | Предмет рекламы (`advertTypeName`, растягивается) |
Продолжительность (`durationString`, read-only) | Количество (`ColQuantity`, редактируемая).

- Источник данных: `firm.GetRollers()` — активные ролики фирмы. По данным dev-БД: медиана 2,
  среднее ~3.89, 90-й перцентиль 8 роликов на фирму — поиск/фильтр по гриду сознательно не
  добавлен, простого списка с чекбоксами достаточно для подавляющего большинства фирм.
- Ввод положительного `Количество` автоматически ставит чекбокс (`FrmTemplate3.cs:110-114`).
- Чекбокс коммитится сразу по `CurrentCellDirtyStateChanged` (иначе `CellValueChanged` не
  сработает до ухода с ячейки).

### Статистика и валидация (`RecalculateStats`, `FrmTemplate3.cs:153`)

Вызывается при ЛЮБОМ изменении расписания/количества/грида (см. подписки в `OnLoad`,
`FrmTemplate3.cs:583-589`, плюс `dgvRollers.CellValueChanged`, `clbWeekDays_ItemCheck`,
`groupButton_CheckChanged`, `cbSplitPrime_CheckedChanged`).

- **"Общее количество выходов"** — НЕ сумма по гриду, а `dailyQuantity * _template.DaysCount`
  (`GetExpectedTotalQuantity`, `FrmTemplate3.cs:225`) — то, что задают настройки интервала.
  ⚠️ вызывает `_template.Reset()` — см. риск "двойной Reset" ниже.
- **"Сумма по гриду роликов"** — сумма `Количество` по отмеченным строкам грида
  (`GetCheckedRollerAggregates`, `FrmTemplate3.cs:236`). Должна СОВПАСТЬ с ожидаемым —
  цвет метки зелёный/красный сигнализирует расхождение ещё до клика "Рассчитать".
- Каждый вызов `RecalculateStats()` в конце сбрасывает блок цены через `ResetPriceEstimate()`
  (`FrmTemplate3.cs:176`) → `DisplayBaselinePrices()` — НЕ на прочерки, а на текущее реальное
  состояние кампании/акции (см. "Блок Статистика" ниже). Смысл: если пользователь поменял
  параметры ПОСЛЕ расчёта, устаревшая ОЦЕНКА добавки не должна вводить в заблуждение — но
  реальные текущие цифры кампании показать не вредно, наоборот полезно.

### Оценка цены (`btnEstimatePrice_Click`, `FrmTemplate3.cs:269`)

Переиспользует `PriceCalculatorGrid` — тот же движок, что в реальном `PriceCalculatorForm`.

1. Валидация: хотя бы один ролик с количеством > 0; даты/время корректны; сумма по гриду
   ==  ожидаемому общему количеству (иначе `MessageBox.ShowExclamation` и `return`).
2. `durationSec = totalDurationSeconds / gridTotalQuantity` — **средняя длительность,
   взвешенная по количеству выходов** (не простое среднее), точная `decimal`, без округления
   (см. `PriceCalculatorGrid.ApplyCalculation` — сигнатура расширена с `int` до `decimal`
   специально под это).
3. `managerDiscount = GetEffectiveManagerDiscount(selectedDates.Min(), selectedDates.Max())` —
   см. "Блок Статистика" ниже за тем, откуда берётся это число для новой/существующей кампании.
   Передаётся в `calcGrid.ApplyCalculation(..., managerDiscount, managerDiscountModeSingle: true,
   userId: 0)`, но там влияет только на колонки `ManagerDiscount`/`TotalBeforePackage`, которые
   эта форма не читает (`TotalAmount` — сырой тариф без скидок) — по факту аргумент участвует
   только в ручном расчёте `grandTotal` ниже, не в `TotalAmount` из грида.
4. Объёмная скидка считается **по станциям** — `GetCompanyDiscount(massmediaId, ...)` вызывается
   в цикле по `targetRows`, каждой станции своя. Итоги: `combinedTariffSum` (без скидок) →
   `combinedDiscountedSum` (после объёмной, по-станционной) → пакетная скидка через SP →
   `grandTotal = combinedDiscountedSum * managerDiscount * packageDiscount`.
5. **"Пакетная скидка"** — `GetPackageDiscount` (`FrmTemplate3.cs:408`), прямой вызов
   `pc_PackageDiscountCalculateModel` (тот же метод, что `PriceCalculatorForm.GetPackageDiscount`,
   но он `private` в форме — пришлось продублировать вызов). TVP `pc_SelectedMassmedia.durationSec`
   имеет тип SQL `INT` — округление `(int)Math.Round(...)` только на этой границе, не в
   `PriceCalculatorGrid`.
6. Для веерной (`_massmediaIds.Count > 1`) в UI не выводятся "Объёмная скидка" и "Менеджерская
   скидка" (прочерк) — см. "Блок Статистика" за объяснением; `grandTotal` при этом всё равно
   корректно учитывает по-станционную объёмную скидку (она уже посчитана в цикле п.4), просто
   не показывает промежуточное смешанное число.

### Блок "Статистика": шесть чисел цены — откуда что берётся

| Метка | Поле в коде | Линейная | Веерная |
|---|---|---|---|
| Цена по тарифам | `lblCampaignPriceValue` | сумма (одна кампания) | сумма по станциям |
| Объёмная скидка | `lblCompanyDiscountValue` | `discountedSum / tariffSum` | **"—"** |
| Цена с учётом объёмной скидки | `lblTotalBeforePackageValue` | `discountedSum` | **"—"** |
| Менеджерская скидка | `lblManagerDiscountValue` | `GetEffectiveManagerDiscount(...)` | **"—"** |
| Пакетная скидка | `lblPackageDiscountValue` | `1m` (см. ниже) | `_action.Discount` |
| Итого (все скидки) | `lblGrandTotalValue` | `discountedSum × managerDiscount × packageDiscount` | базис: `_action.TotalPrice`; после "Рассчитать": та же формула |

Два состояния блока, не путать:

- **Базис** (`LoadExistingCampaignState` → `DisplayBaselinePrices`, `FrmTemplate3.cs:181/462`) —
  текущее реальное состояние кампании/акции ДО этого запуска шаблона. Показывается при `OnLoad`
  и после любого сброса параметров (`ResetPriceEstimate`).
- **Финал** (`btnEstimatePrice_Click`, `FrmTemplate3.cs:269`) — то же самое ПЛЮС ролики,
  отмеченные в гриде, после клика "Рассчитать" (не "сколько добавит шаблон", а состояние
  кампании целиком).

Архитектурное решение остаётся тем же, что и раньше (не dry-run через транзакцию+rollback):
базис и оценку добавки показываем раздельно, а не сливаем в один псевдо-точный номер.

#### `_campaign` vs `_action` — почему две ссылки, а не одна

`_campaign` — конкретная кампания одного СМИ, **`null` для веерной** (см. "Точка входа":
`CampaignForm.GetTemplateMassmediaIds` для веерной вообще не трогает `_campaign`). `_action` —
акция, всегда не null, резолвится вызывающим кодом (`CampaignForm.tbbTemplate3_Click`) через
`IsRangeCampaign ? TariffWithRangeGrid.Action : _campaign.Action` и передаётся в конструктор
готовым. **Внутри `FrmTemplate3` не читать `Action` через `_campaign.Action`** — при открытии
на веерной кампании `_campaign` равен null, и это NRE. Сигнал "линейная это или веерная" —
`_massmediaIds.Count`, а не наличие `_campaign` (см. риск ниже про порядок инициализации в `OnLoad`).

#### Менеджерская скидка (`GetEffectiveManagerDiscount`, `FrmTemplate3.cs:216`)

`Campaign.managerDiscount` в БД имеет `DEFAULT 1` (`Campaign.sql`) и обновляется
`ActionRecalculate`-ом (Phase 1E) только на переходе количества выпусков 0 → не 0 — у кампании
без единого выпуска это поле ВСЕГДА голая единица, а не реальный коэффициент. Реальный
Калькулятор (`TemplateEditorControl.SetManagerDiscount`) для такого случая коэффициент из
кампании не читает вовсе — считает через `SecurityManager.User.GetDiscount(startDate,
finishDate)` (SP `GetUserDiscount`, отдельная от `fn_GetMaxUserDiscount`), с исключением для
админов (`IsAdmin` → всегда `1`). `GetEffectiveManagerDiscount` делает то же самое:

- Есть кампания с `IssuesCount > 0` (сама `_campaign` для линейной; первая найденная в цикле
  по `_action.Campaigns()` для веерной — коэффициент общий на пользователя/период, одной
  кампании достаточно) → берёт готовое `Campaign.ManagerDiscount`.
- Иначе → гипотетический расчёт через SP, как у Калькулятора.

Кэшируется в `_hasExistingIssues`/`_existingManagerDiscount` один раз в `LoadExistingCampaignState`
(не дёргать SP на каждый чих) и отдельно в `_baselineManagerDiscount` — но только для линейной:
для веерной это значение не выводится (см. ниже), лишний SP-вызов при открытии формы не делается.
`btnEstimatePrice_Click` вызывает `GetEffectiveManagerDiscount` заново на реальном периоде
шаблона (пользователь мог поменять даты в диалоге), не переиспользует `_baselineManagerDiscount`.

#### Почему у веерной нет "Объёмной скидки" и "Менеджерской скидки"

Веерная — несколько одновременно создаваемых кампаний (по одной на СМИ), и обе скидки —
**значения по станциям**, не одно число на всю акцию:

- Объёмная скидка — `hlp_CompanyDiscountCalculate` вызывается отдельно на каждое СМИ
  (`GetCompanyDiscount(massmediaId, ...)` в цикле — что в `LoadExistingCampaignState`, что в
  `btnEstimatePrice_Click`) — у трёх станций веера могут быть три разных значения.
- Менеджерская скидка — `ActionRecalculate` пишет `managerDiscount` отдельно в каждую строку
  `Campaign`, не в `Action` — формально тоже может отличаться по станциям (на практике почти
  всегда одинаковая — один пользователь/период на всю акцию, но гарантии нет).

Раньше форма всё равно выводила ОДНО число (взвешенное среднее для объёмной, значение первой
найденной кампании для менеджерской) — вводило в заблуждение, будто это единый коэффициент для
всей акции. По предложению заказчика для веера ограничили блок тем же набором, что уже
показывает `EditIssuesForm` (`ActionOnMassmedia.DisplayData`, `Client/Classes/ActionOnMassmedia.cs:811`):
тариф, пакетная скидка, итог — оба поля "—", вместо того чтобы выдавать одно из нескольких
разных значений за общее.

Пакетная скидка — Action-level величина по построению (`ActionRecalculate`: `discount=1` жёстко
при `@campaignCount<=1`, иначе через `hlp_ActionDiscountCalculate` на всю акцию сразу) — поэтому
она однозначна и показывается всегда, независимо от линейная/веерная.

"Итого" для базиса веерной акции = `_action.TotalPrice` напрямую (серверное значение уже
корректно учитывает разные объёмные/менеджерские скидки по станциям) — НЕ формула
`discountedSum × managerDiscount × packageDiscount`, в отличие от линейной и от финала после
"Рассчитать" (там формула — единственный вариант: гипотетическая добавка ещё не сохранена,
серверного числа для неё нет; `grandTotal` при этом всё равно корректно учитывает по-станционную
объёмную скидку — она уже посчитана в цикле `GetCompanyDiscount`, просто не выводится отдельно).

---

## RollerAllocationQueue — распределение роликов по дням

`Client/Classes/RollerAllocationQueue.cs`, `internal`, используется и линейной, и веерной
кампанией (единая реализация после рефакторинга).

### Конструктор — round-robin по раундам

```
quantities = rollerQuantities.Where(q > 0).ToList()   ← материализация ОДИН раз
                                                          (IEnumerable нельзя гонять Max()+Where()
                                                          в цикле без ToList — источник может быть
                                                          недетерминирован или дорог)
maxQuantity = quantities.Max(Quantity)

for round in 0..maxQuantity-1:
    roundItems = [roller for (roller,qty) in quantities if qty > round]  ← по одному экз. каждого
                                                                            ролика, у кого квота
                                                                            ещё не исчерпана
    shuffle(roundItems)   ← Fisher-Yates, ТОЛЬКО внутри раунда
    pool.AddRange(roundItems)

_queue = new LinkedList<Roller>(pool)
```

**Гарантия**: раунд N содержит не более одного экземпляра каждого ролика → квота сохраняется
точно, повтор одного ролика внутри одного раунда невозможен. Если дневная норма совпадает с
числом роликов (частый идеальный случай — квоты равны и делятся ровно на дни), **раунд = день**,
и каждый день гарантированно получает все разные ролики.

**Не гарантирует**: равномерное распределение "сильных" (с большой квотой) роликов по ВСЕМУ
интервалу. Пример: квоты `14/6/4/4`, 4 в день — первые 4 раунда содержат все 4 ролика, раунды
4-5 только два "сильных", раунды 6-13 — только самый сильный (14). Последние дни интервала
почти/полностью состоят из одного ролика. **Это ожидаемое поведение, не дефект** — соответствует
исходному пожеланию заказчика "ролики с большим количеством должны выходить чаще".

### TakeForToday(count) — забор порции + защита от повтора подряд

```
batch = queue.PopFront(count)          ← просто следующие count элементов ленты
return ArrangeNoAdjacentRepeats(batch)  ← подстраховка, не основной механизм
```

`ArrangeNoAdjacentRepeats` — жадный алгоритм (аналог классической "reorganize string"/task
scheduler задачи): на каждом шаге берёт ролик, которого в остатке порции больше всего, но не
совпадающий с только что поставленным; тай-брейк — более длинный ролик. Нужен для краевых
случаев, когда:
- дневная норма не совпадает ровно с границей раунда (например, если `count` не делится на
  число ролика в раунде из-за смены недели/`quantity=0` у части роликов);
- часть роликов вернулась через `PutBackToFront` (см. ниже) и создала дубль внутри порции.

Гарантирует ноль повторов подряд, ЕСЛИ самый частый ролик в порции ≤ половины её размера —
иначе математически неизбежен хотя бы один повтор, алгоритм просто минимизирует их число.

⚠️ Важно: если раунд уже не содержит дублей (обычный случай при точном совпадении раунда и дня),
`ArrangeNoAdjacentRepeats` вырождается в сортировку по убыванию длительности (тай-брейк
срабатывает всегда, т.к. условие "не равен последнему" никогда не конфликтует) — порядок внутри
дня в этом случае НЕ полностью случаен, а частично детерминирован длительностью роликов.

### PutBackToFront(rollers) — возврат остатка

Не хватило окон сегодня → недостающие ролики возвращаются в НАЧАЛО очереди (сохраняя их
взаимный порядок), чтобы быть предложенными в один из следующих дней.

---

## Линейный путь размещения

```
FrmGenerator.AddIssues()                                [FrmGenerator.cs]
  │  else if (_rollerQueue != null)                      ← Шаблон 3, линейная
  ▼
AddMultiRollerIssues()                                   [FrmGenerator.cs:449]
  │  аналог AddSimpleIssues, но на каждый день — несколько роликов вместо одного _roller
  │  окна дня: allWindows (фильтр по времени/фирме/позиции), затем OrderWindowsRandomly
  │            (сортировка по "свободности" TimeWithUnConfirmed, затем случайно)
  ▼
AddIssuesForRollers(targetCount, windows, errorTemplate)  [FrmGenerator.cs:522]
  │  rollers = _rollerQueue.TakeForToday(targetCount)
  │  zip(rollers, windows) → _campaign.AddIssue(roller, window, position, grantorID) для каждой пары
  │  остаток (окон меньше, чем роликов) → _rollerQueue.PutBackToFront(rollers)
```

Порядок роликов (из `TakeForToday`) сопоставляется с порядком окон (из `OrderWindowsRandomly`)
позиционно — окна отсортированы по свободности (сначала самые свободные), ролики — по
"анти-повторной" логике. Recalculate — 1 раз в конце всего `Generate()` (см.
`template-issue-generation.md`, не переопределяется Шаблоном 3).

---

## Веерный путь размещения

```
TariffWithRangeGrid.AddIssuesRangeTimePeriodMultiRoller(...)   [TariffWithRangeGrid.cs:547]
  │  аналог AddIssuesRangeTimePeriod, но вместо одного Roller — колбэк takeRollersForToday
  │  slotsForDate = недельные слоты (кеш slotsCache) отфильтрованные по времени/позиции/фирме
  │  quantity > 0:          selectedSlots = slotsForDate.Take(quantity)
  │  prime split:           primeSlots/nonPrimeSlots раздельно
  ▼
PlaceRollersInSlots(slots, takeRollersForToday, ignoreFlag, result)   [TariffWithRangeGrid.cs:603]
  │  rollers = takeRollersForToday(slots.Count)     ← колбэк = rollerQueue.TakeForToday,
  │                                                    ПЕРЕДАЁТСЯ реальное число найденных слотов,
  │                                                    не запрошенная дневная норма — если слотов
  │                                                    сегодня меньше нормы, из очереди берётся
  │                                                    меньше, остаток естественно уходит на
  │                                                    следующий день (без явного PutBackToFront)
  │  zip(slots, rollers) → AddIssuesRange(slot.WindowDate, roller, ignoreFlag, recalculate:false)
  │      → SQL AddRangeIssues → IssueIUD × N СМИ веера
```

Разница с линейным путём: колбэк вызывается с `slots.Count` (фактически найденное), а не с
запрошенным `count` — поэтому здесь нет отдельного `PutBackToFront` внутри `PlaceRollersInSlots`;
недобор просто не запрашивается из очереди и остаётся там сам. Явный `PutBackToFront` в этом
пути не нужен и не вызывается.

Recalculate — `rangeGrid.Action` передаётся в `FrmGenerator`, пересчёт 1 раз в конце (как Path 2
Range TimePeriod в `template-issue-generation.md`).

---

## Файлы и методы

| Файл | Методы / члены |
|---|---|
| `Client\Forms\CampaignForm.cs` | `tbbTemplate3_Click` (резолвит `action` для конструктора, см. "Точка входа"), `GetTemplateMassmediaIds`, `IsRangeCampaign` |
| `Client\Forms\FrmTemplate3.cs` / `.Designer.cs` | Конструктор `(firm, template, massmediaIds, position, campaign, action)`, `RecalculateStats`, `ResetPriceEstimate`, `DisplayBaselinePrices`, `GetEffectiveManagerDiscount`, `LoadExistingCampaignState`, `btnEstimatePrice_Click`, `GetCompanyDiscount`, `GetPackageDiscount`, `SelectedRollers`, `SaveData2Template` |
| `Client\Classes\RollerAllocationQueue.cs` | Конструктор (round-robin), `TakeForToday`, `ArrangeNoAdjacentRepeats`, `PutBackToFront`, `Count` |
| `Client\Forms\FrmGenerator.cs` | Конструктор `(template, rollerQuantities, position, campaign, pricelist, module, grantorID)`, `AddMultiRollerIssues`, `AddIssuesForRollers`, `OrderWindowsRandomly` |
| `Client\Controls\TariffWithRangeGrid.cs` | `AddIssuesRangeTimePeriodMultiRoller`, `PlaceRollersInSlots`, `AddIssuesRange(date, roller, ignoreFlag, recalculate)` — перегрузка с явным `roller`, `Action` (используется и `CampaignForm`, и `FrmTemplate3` косвенно) |
| `Client\Controls\PriceCalculatorGrid.cs` | `ApplyCalculation` (decimal `durationSec`), `CalculateCampaignTariffPrice`, `SafeDecimal`, `GetTotalSeconds`, `TotalAmount` (сырой тариф, без скидок — единственная колонка, которую читает `FrmTemplate3`) |
| `Client\Classes\Campaign.cs` | `ManagerDiscount`, `IssuesCount`, `TariffPrice`, `Price`, `Action` (лениво кэшированный `ActionOnMassmedia`, но НЕ использовать из `FrmTemplate3` — см. `_campaign` vs `_action`) |
| `Client\Classes\Action.cs` (базовый класс) | `TariffPrice`, `Discount`, `TotalPrice` (все три через `ParseHelper.GetDecimalFromObject(..., 0)` — не кидают на DBNull/missing key), `Campaigns()` |
| `Client\Classes\ActionOnMassmedia.cs` | `Refresh`, `Recalculate` → `ActionRecalculate`, `GetActionById` (всегда возвращает объект, никогда null), `DisplayData` — тот же набор данных (тариф/пакетная скидка/итог), что теперь показывает `FrmTemplate3` для веера |
| `FogSoft.WinForm\Classes\SecurityManager.cs` | `User.GetDiscount(startDate, finishDate)` (SP `GetUserDiscount` — MIN по `UserDiscount`, НЕ `fn_GetMaxUserDiscount`), `User.IsAdmin`, `LoggedUser` |

---

## SQL-процедуры

| Процедура | Когда вызывается | Роль |
|---|---|---|
| `[dbo].[IssueIUD]` AddItem | Внутри `_campaign.AddIssue` (линейная) и `AddRangeIssues` (веерная) | Создаёт один выпуск |
| `[dbo].[AddRangeIssues]` | `TariffWithRangeGrid.AddIssuesRange` | Курсор → `IssueIUD` × N СМИ веера |
| `[dbo].[ActionRecalculate]` | `Action.Recalculate()` — 1 раз в конце `Generate()`. ⚠️ НЕ вызывается `Action.Refresh()` (тот в `FrmTemplate3.OnLoad` для `_action`) — `Refresh()` это пассивный `SELECT` (`PresentationObject.Refresh`, метадата-запрос по `InterfaceObjects.SimpleJournal`), а не пересчёт. `_action.TotalPrice`/`.Discount` в базисе — то, что реально лежит в БД с последнего РЕАЛЬНОГО `Recalculate()`, не свежий пересчёт на момент открытия формы | Пересчёт реальной суммы акции со всеми скидками |
| `[dbo].[pc_GetStationsPriceModel]` | Внутри `PriceCalculatorGrid.CalculateCampaignTariffPrice` | Prime/non-prime цены по `DENSE_RANK` — см. риск ниже |
| `[dbo].[pc_PackageDiscountCalculateModel]` | `FrmTemplate3.GetPackageDiscount` (черновая оценка) и `PriceCalculatorForm.GetPackageDiscount` (реальный калькулятор) | Пакетная скидка по TVP `pc_SelectedMassmedia` |

---

## Риски / известные ограничения

| # | Риск | Описание |
|---|---|---|
| 1 | **2-уровневая prime/non-prime модель** | `pc_GetStationsPriceModel` определяет prime/non-prime через `DENSE_RANK` (топ-1 и топ-2 уникальные цены дня) — если у прайслиста реально 3+ уровня тарифа, часть цен будет посчитана неверно. Известное ограничение, не пофикшено, out of scope |
| 2 | **Round-robin не размазывает "сильные" ролики по всему интервалу** | При сильно неравных квотах (см. пример `14/6/4/4`) последние дни почти целиком состоят из одного ролика — задокументированное, ожидаемое поведение |
| 3 | **`ArrangeNoAdjacentRepeats` — best effort** | Не может убрать повтор подряд, если единственный оставшийся тип ролика в порции — один и тот же (например, все окна дня заняты остатком одного ролика после `PutBackToFront`) |
| 4 | **`ArrangeNoAdjacentRepeats` частично детерминирует порядок длительностью** | Когда дублей в порции нет (обычный случай), тай-брейк по длительности эффективно сортирует порцию — порядок внутри дня не полностью случаен |
| 5 | **`IssueTemplate.Reset()` вызывается в нескольких местах** | `GetExpectedTotalQuantity()` и `BuildTemplateDates()` оба дергают `Reset()`/`MoveNext()` — см. общий риск "двойной Reset" в `template-issue-generation.md`. Не менять порядок вызовов без проверки `DaysCount` |
| 6 | ~~"Оценка добавляемых роликов" не учитывает существующие выпуски~~ — **устарело, исправлено** | Текущий код учитывает: `existingTariffPrice + rowAmount` перед вызовом `GetCompanyDiscount` (`FrmTemplate3.cs:342-343`), а базис (`LoadExistingCampaignState`) явно показывает текущее состояние кампании/акции. См. "Блок Статистика" выше |
| 7 | **Веерный путь: явного `PutBackToFront` нет** | Недобор в `PlaceRollersInSlots` просто не запрашивается из очереди (колбэк вызывается с фактическим `slots.Count`) — отличие от линейного пути, где `PutBackToFront` вызывается явно |
| 8 | **`FrmTemplate3.SelectedRollers` — комментарий устарел** | В коде написано "пока не используется генератором выпусков" (`FrmTemplate3.cs:515-516`) — на самом деле используется (`CampaignForm.tbbTemplate3_Click:1309`). Мелкая правка, не влияющая на поведение |
| 9 | **Порядок инициализации в `OnLoad`: `cbSplitPrime.Checked = ...` может выстрелить `DisplayBaselinePrices` раньше `LoadExistingCampaignState`** | `cbSplitPrime.CheckedChanged` подписан в Designer (активен с конструктора), а `cbSplitPrime.Checked = _template.Quantity == 0` в `OnLoad` (`FrmTemplate3.cs:568`) идёт РАНЬШЕ `LoadExistingCampaignState()` (`FrmTemplate3.cs:594`) — синхронно триггерит `RecalculateStats → DisplayBaselinePrices` на ещё не заполненных `_baselineXxx`. Сейчас безвредно: все поля — `decimal`/`bool` с безопасными дефолтами (0/false), `DisplayBaselinePrices` больше не трогает `_campaign`/`_action` напрямую. ⚠️ Если добавлять в `DisplayBaselinePrices` новое поле, читающее `_campaign`/`_action` напрямую — получите NRE именно на этом преждевременном вызове (так и родился баг из риска #10) |
| 10 | **Веерная: `_campaign` — не тот же null-check, что "новая кампания"** | `_campaign == null` означает "форма открыта для веерной" (см. `_campaign` vs `_action` в "Блок Статистика"), а НЕ "кампания новая/без выпусков" — это разные оси (линейная/веерная × новая/существующая, 4 комбинации). Не путать `_massmediaIds.Count` (линейная/веерная) с `_hasExistingIssues` (новая/существующая) |

---

## Закрытые вопросы

| # | Вопрос | Ответ |
|---|---|---|
| 1 | Почему базис (текущее состояние акции) и оценка добавляемых роликов — два разных числа, а не одно точное? | Точный вариант потребовал бы временной записи реальных выпусков в транзакции + `ActionRecalculate` + rollback — риск для транзакционной целостности ради предпросмотра. Выбран более простой и безопасный вариант — см. обсуждение с заказчиком |
| 2 | Почему `managerDiscountModeSingle` всегда `true`, а не `false` (period-based)? | Period-based режим завязан на `userId` и таблицу периодов конкретного менеджера — в контексте Шаблона 3 просто нужен фиксированный коэффициент, `GetEffectiveManagerDiscount(...)` (реальный для существующей кампании или гипотетический через `SecurityManager.User.GetDiscount` для новой — см. "Блок Статистика") |
| 3 | Почему `RollerAllocationQueue` общий для линейной и веерной, а остальная логика (`FrmGenerator`/`TariffWithRangeGrid`) — нет? | Изначально было осознанное решение не шарить код между путями ("не трогать протестированный рабочий код"). После того как анти-дублирующий фикс пришлось вносить в двух местах вручную дважды подряд, очередь всё-таки унифицировали — риск рассинхронизации оказался выше цены рефакторинга |
| 4 | Почему `FrmTemplate3` не может просто читать `Action` через `_campaign.Action`, как остальной код (`Campaign.cs:182`)? | `Campaign.Action` — рабочее свойство, но требует непустой `_campaign`. У веерной кампании `_campaign`, переданная в `FrmTemplate3`, сама равна `null` (см. `CampaignForm.GetTemplateMassmediaIds` — веерная берёт СМИ из `TariffWithRangeGrid.Action.Campaigns()`, не из кампании). Поэтому `Action` резолвится один раз в `CampaignForm.tbbTemplate3_Click` (там уже есть ветвление `IsRangeCampaign`) и передаётся в `FrmTemplate3` явным параметром `_action` |
| 5 | Почему для веерной "Менеджерская скидка" берётся с ПЕРВОЙ кампании акции с выпусками, а не проверяется на совпадение по всем? | Коэффициент завязан на пользователя и период (`fn_GetMaxUserDiscount(@loggedUserID, @startDate, @finishDate)`), общие для всей акции — на практике одно и то же значение у всех кампаний акции. Проверка "совпадают ли все" усложнила бы код ради теоретического случая без известных прецедентов; к тому же в UI для веера это значение всё равно не выводится (см. "Почему у веерной нет Объёмной/Менеджерской скидки") — используется только внутри `grandTotal` при "Рассчитать" |
