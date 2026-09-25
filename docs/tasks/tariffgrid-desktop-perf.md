# Десктоп: производительность тарифных сеток (П-1…П-17)

Рабочий документ для разбора по пунктам. Сводная таблица и контекст —
[`docs/tariffgrid.md`](../tariffgrid.md) §8; здесь по каждому пункту: что именно происходит в
коде, сколько стоит, что предлагается, чем рискуем и как проверить.

Всё проверено чтением кода на master `cf0d61f` (23.09.2026), если не написано «гипотеза».
Замеры — лог прода `Logs/qd2.log` за 17–23.09.2026 (~19 пользователей). В этот лог SQL-вызовы
попадают только медленные (от ~100 мс), а операции `OperationScope` (`OnGridCellClick`,
`UpdateDB`, `AddIssue`, `ActionRecalculate`…) — все.

Ничего не исправлено. Порядок разбора и что брать — решение владельца.

| # | Коротко | Выигрыш | Работа | Риск |
|---|---|---|---|---|
| П-1 | пересчёт акции на каждое окно при Insert / комбо / шаблоне веера | N пересчётов → 1 | мала | низкий |
| П-2 | клик веера перечитывает всю неделю | ~0,8 с → ~0,1 с на клик | средняя | средний |
| П-2а | двойной тяжёлый джойн в `TariffWindowWithRange` | ~−0,5 с на каждый вызов | мала, SQL | низкий |
| П-3 | «Добавленные выпуски» веера строятся дважды и по N+1 | десятки запросов при открытии | средняя | средний |
| П-4 | ролики фирмы и ролик перечитываются на каждом обновлении сетки | 2–3 запроса на обновление | мала | средний (общий `SmartGrid`) |
| П-5 | выбор модуля — до трёх полных перезагрузок | ×3 → ×1 | мала | низкий |
| П-6 | модульный клик: `Refresh` каждого окна дня; `IsModuleExist` на колонку | десятки запросов → 1–2 | мала | низкий |
| П-7 | 165 мс из 231 мс клика — панели деталей формы | ~150 мс на клик | средняя | средний |
| П-8 | веер: двойная привязка «Добавленных выпусков», подписки копятся | процессор, растёт с кликами | мала | средний (общий `SmartGrid`) |
| П-9 | шаблон: ~110 мс на выпуск при вставке 4 мс | до ×5 на генерацию | средняя | средний |
| П-10 | `RecalculateAction()` перечитывает акцию после пересчёта | 1 запрос на операцию | мала | **есть оговорка** |
| П-11 | одиночные записи и `new Roller` в циклах массовых операций | N запросов | мала/средняя | низкий |
| П-12 | спонсорская сетка: N+1 на отрисовке, лишние `LoadIssues` | мало (сетка крошечная) | мала | низкий |
| П-13 | пакетная: детали окна дважды на клик | 1 запрос | мала | низкий |
| П-14 | комбо: все выпуски акции на каждом листании периода | ~2 с клиента на операцию | средняя | средний |
| П-15 | `ActionForm` после закрытия формы кампании — безусловные обновления | 2–3 запроса + пересчёт | мала | низкий |
| П-16 | трафик: «массово закрыть» грузит сетку дважды | ×2 → ×1 | мала | низкий |
| П-17 | удаление сгенерированных окон по одному дню | ~250 мс × дни | мала | уточнить замысел |

---

## П-1. Пересчёт акции на каждое окно в массовых добавлениях

**Где.**
- Insert по выделенным окнам (линейная): `CampaignForm.AddIssuesInSelectedWindows`
  ([CampaignForm.cs:957](../../Client/Forms/CampaignForm.cs:957)) → в цикле
  `RollerIssuesGrid3.AddIssueToWindow` → `AddIssueTransaction`
  ([RollerIssuesGrid3.cs:191](../../Client/Controls/RollerIssuesGrid3.cs:191)):
  `BeginTransaction` → `AddIssue` → **`RecalculateAction(false)`** → `Commit`. После цикла —
  ещё `_campaign.RecalculateAction()` (`CampaignForm.cs:978`). Комментарий над
  `AddIssueToWindow` («та же транзакция, что у одиночного клика … форма делает один
  RefreshGrid») про пересчёт молчит — пересчёт на каждое окно так и остался.
- Комбо: `ComboModulePlacementForm.AddIssuesInSelectedCells` (`:489`) → `AddModuleIssue` →
  `_action.Recalculate()` (`:333`, с `Refresh`) на каждую ячейку.
- Шаблон веера, путь Simple: `CampaignForm.grid_CellClicked`
  ([CampaignForm.cs:559](../../Client/Forms/CampaignForm.cs:559)) передаёт в `FrmGenerator`
  делегат `rangeGrid.AddIssuesRange(windowDate, …)` — перегрузка с `recalculate = true`, то есть
  `ActionRecalculate` + `Actions1` на **каждую дату** шаблона; в `FrmGenerator.finally` —
  ещё один.

**Цена.** Insert по N окнам = N+1 пересчётов всей акции (`ActionRecalculate` — два курсора по
кампаниям). На проде медиана пересчёта ~20 мс, p90 ~45 мс, но на больших акциях — сотни мс.

**Правка.** В `AddIssueToWindow` — транзакция без пересчёта (параметр или отдельный путь), в
форме один пересчёт после цикла (он уже есть). В комбо — то же. В шаблоне веера — передавать
`recalculate: false` (как уже сделано в `EditIssuesForm.cs:365` для «поставить в выделенные»).

**Риски.** Пересчёт внутри транзакции нужен, чтобы при ошибке вставки откатилось всё вместе.
Без него каждая вставка — своя транзакция, пересчёт в конце — отдельная; если пересчёт упадёт,
выпуски останутся без пересчитанных цен до следующего пересчёта. Так уже работает веер и
`FrmGenerator`, то есть это принятая в проекте модель.

**Проверка.** Лог: Insert на 10 окон — было 11 `ActionRecalculate`, должно стать 1. Сумма
акции после Insert — та же, что при добавлении кликами.

## П-2. Клик в веере перечитывает всю неделю ([RANGE-01])

**Где.** `TariffWithRangeGrid.AddIssuesRange(DataGridViewCell)`
([TariffWithRangeGrid.cs:640](../../Client/Controls/TariffWithRangeGrid.cs:640)):
`AddIssuesRange` → `MarkCellAsHavingCurrentCampaignIssues` → `ChangeIssuesCounter` →
**`RefreshGrid()`**. Раскраска и счётчик перед полной перезагрузкой — пустая работа.

**Цена.** Лог: 447 кликов за неделю, медиана 856 мс, p90 4,2 с; из них после записи —
медиана 782 мс (перечитывание `TariffWindowWithRange` ~500 мс + привязки формы, см. П-8).
Пример из лога: `AddIssuesRange 38ms` → `TariffWindowWithRange 542ms` → клик 842 мс.

**Правка.** Перечитывать только один слот: необязательный параметр в `TariffWindowWithRange`
(дата слота) и точечное обновление ячейки, по образцу `RollerIssuesGrid3.RefreshSingleCell`.
Полное перечитывание оставить для смены недели/фильтров.

**Риски.** Признаки соседних слотов могут зависеть от добавления (например, счётчики дня,
«частичный»/«полный» слот по кампаниям) — счётчик дня пересчитывается локально, остальное
касается только этого слота. Проверить режим номеров роликов (`RefreshPartialRollerGroups`).

**Проверка.** Клик в веере < 0,3 с; после клика и после полного обновления сетка одинакова.

## П-2а. Двойной тяжёлый джойн в `TariffWindowWithRange`

**Где.** `ArtvisDB/dbo/Stored Procedures/TariffWindowWithRange.sql`: раздел 7 (CTE
`all_issues`, признаки `HasIssues*`) и раздел 9 (`#otherIssues`, чужие акции фирмы по слотам)
выполняют **один и тот же** джойн `#res × TariffWindow × #mm × Issue × Campaign × Action`
с теми же условиями; комментарий раздела 9 так и говорит: «Тот же джойн, что и all_issues в п.7».

**Цена.** На dev (6 станций, тёплый кэш): процедура 1,5 с, каждый из двух проходов ~560 мс,
~111 тыс. чтений `TariffWindow` + ~124 тыс. `Issue`. На проде это процедура №1 по суммарному
времени во всём логе (751 медленный вызов, 437 с за неделю).

**Правка.** Сначала заполнить `#otherIssues` (добавить в него `massmediaID` и `deleteDate`),
раздел 7 посчитать из `#otherIssues` группировкой. Наборы результатов не меняются — десктоп не
трогаем.

**Риски.** Малые: логика признаков та же, меняется источник. Сверить результаты процедуры до и
после на нескольких акциях (все 5 наборов побайтно).

**Проверка.** `SET STATISTICS IO/TIME` до и после; время процедуры −30…40%.

## П-3. «Добавленные выпуски» веера: двойная сборка и N+1

**Где.** Конструктор `TariffWithRangeGrid` (`:98`) → `InitAddedIssuesData` →
`Action.BuildAddedIssuesTable(null)`. Первый `EditIssuesForm.RefreshGrid` (`:232`) →
`SetSelectedCampaigns(checkedIds)`; `IsSameSelection(null, список)` = false → сборка
**заново**. Сама сборка ([Action.cs:393](../../Client/Classes/Action.cs:393)): `Campaigns()`,
потом `GetCampaigns` → `Campaign.GetCampaignById` **на каждую кампанию** (у модульных — два
запроса), потом `campaign.GetContent()` — **все выпуски кампании за весь период**, группировка
в памяти.

Та же сборка — на каждое изменение галочек кампаний (`EditIssuesForm.cs:192`) и на каждый
`RebuildAddedIssues`.

**Цена.** Для акции на 10 станций — ~25–30 лишних запросов при открытии, объём выпусков ×2.
`Campaigns` при открытии вызывается 4 раза (`TariffWithRangeGrid.cs:122`, `Action.cs:403`
дважды, `EditIssuesForm.cs:154`).

**Правка.** (1) Не строить в конструкторе, если форма сразу вызовет `SetSelectedCampaigns`
(или считать null и «все» одинаковым выбором). (2) Тип кампании уже есть в строках
`Campaigns()` — `GetCampaignById` не нужен. (3) Выпуски выбранных кампаний — одним запросом
(новая процедура чтения по `@actionID, @campaignIDs`), вместо `GetContent` на кампанию.

**Риски.** (3) — новая процедура; (1)–(2) — чисто клиентские.

**Проверка.** Лог при открытии веера: одна сборка, `Campaigns` — 1–2 раза.

## П-4. Ролики фирмы перечитываются на каждом обновлении сетки

**Где.** Лямбда `GridRefreshed` в `CampaignForm.SetEventHandlersFromGridEvents`
([CampaignForm.cs:423](../../Client/Forms/CampaignForm.cs:423)) → `InitRollersList` →
`SetRollerDataSource` → `grdRollers.DataSource = Firm.GetRollers().DefaultView` (запрос). Дальше
цепочка в общем `SmartGrid`:
- сеттер `DataSource` ([SmartGrid.cs:291](../../FogSoft.WinForm/Controls/SmartGrid.cs:291))
  в конце вызывает `FireObjectSelected()` → `grdRollers_ObjectSelected` →
  `rollerGrid.Roller = new Roller(id)` → конструктор `Roller(int)` делает `Refresh()` — запрос;
- `grdRollers.SelectedObject = selected` (`CampaignForm.cs:493`) → `Bm_PositionChanged`
  (`SmartGrid.cs:1311`) → `po.Equal(selectedObject)` → **ложь для одинаковых объектов**:
  `PresentationObject.Equal` ([PresentationObject.cs:490](../../FogSoft.WinForm/Classes/PresentationObject.cs:490))
  сравнивает `object[] IDs` оператором `!=`, то есть упакованные `int` — по ссылке → второй
  `FireObjectSelected` → второй `new Roller(id)`.

**Цена.** Каждый `RefreshGrid`: смена недели, позиции, фильтров, Insert, Del, шаблон, отмена;
в веере — **каждый клик**. 2–3 запроса и перепривязка списка роликов.

**Правка.** (1) Для линейной и веера список роликов от недели не зависит — грузить при открытии
и по явному «Обновить»; модульные (`RollersForModule` за период) — оставить. (2) В
`grdRollers_ObjectSelected` — `new Roller(row)` из строки списка вместо `new Roller(id)`
(гипотеза: в строке `ActionRollers` есть всё нужное, включая длительность — проверить).
(3) Отдельно — `PresentationObject.Equal`: сравнивать `Equals(IDs[i], po.IDs[i])`.

**Риски.** (3) — **общий код всего приложения**: `Equal` вызывается в `SmartGrid` при каждой
смене строки везде. Исправление уберёт повторные `ObjectSelected` во всех журналах — это
правильно, но где-то код мог (случайно) полагаться на повторное событие. Делать отдельным
шагом, с проверкой основных журналов. (1)–(2) — локальны.

**Проверка.** Лог: PgDn в линейной кампании — нет `Rollers`/`RollerPassport`; выбранный ролик
в списке и в сетке совпадают после PgDn (ролик в `IRollerGrid.Roller` не сбрасывается).

## П-5. Выбор модуля — до трёх перезагрузок сетки

**Где.** `CampaignForm.InitModule` ([CampaignForm.cs:340](../../Client/Forms/CampaignForm.cs:340)):
1. если позиция задана — `tbbPosition.DropDownItems[0].PerformClick()` →
   `tbbPosition_DropDownItemClicked` → сеттер `RollerPosition` → `RefreshGrid()` —
   **ещё со старым модулем**;
2. `if (fNeedRefresh) _tariffGrid.RefreshGrid()` — основной;
3. если `Pricelist.StartDate > Today` → `Jump2CurrentDate()` → `RefreshGrid()` ещё раз, хотя
   `CurrentDate` на ближайший прайс-лист уже выставлен выше — для будущих прайс-листов
   срабатывает всегда.

Каждое обновление модульной сетки: прайс-лист, `TariffWindowRetrieve`, `Grid`,
`IsModuleExist` на каждую полную колонку (П-6), затем `GridRefreshed` → `InitModulesList`
(`GetModules`) → `RollersForModule` → ролик (П-4).

**Правка.** Сбросить позицию без события (присвоить поле/кнопку, не `PerformClick`), выставить
модуль и дату, один `RefreshGrid`.

**Риски.** Низкие; проверить, что текст кнопки позиции и позиция в сетке остаются согласованы.

## П-6. Модульный клик: `Refresh` каждого окна дня

**Где.** `RollerIssuesGrid3.AddModuleIssue`
([RollerIssuesGrid3.cs:150](../../Client/Controls/RollerIssuesGrid3.cs:150)): после
`ModuleIssueIUD` + пересчёта — цикл по всем строкам колонки → `RefreshSingleCell` →
`tariffWindow.Refresh()` (`TariffWindowRetrieve @windowId`) на **каждое окно дня**. То же в
`RefreshCurrentCell` для модуля (после удаления). `MarkFullColumns` (`:273`) — `IsModuleExist`
на каждую полную колонку при каждом обновлении.

**Цена.** Окон модуля в день — десятки → десятки запросов на один клик (по ~3 мс).

**Правка.** Одно чтение окон дня (`GetTariffWindows(date, date, module, …)`) и обновление
колонки из него; `IsModuleExist` — один запрос на неделю (или признак прямо в
`TariffWindowRetrieve`/`Grid`).

## П-7. Клик линейной сетки: 165 мс на панели деталей ([UI-02])

**Где.** `TariffGrid.FireCellClicked` → после `updateDB` → `onCellClicked` →
`CampaignForm.grid_CellClicked` ([CampaignForm.cs:533](../../Client/Forms/CampaignForm.cs:533))
→ `ShowWindowIssues` (`:594`): очистка двух `SmartGrid`, клонирование сущностей,
`WindowIssuesRetrieve` (дважды, если «неподтверждённые» выключены), привязка двух таблиц;
плюс два `Application.DoEvents()` (в `FireCellClicked` и `grid_CellClicked`).

**Цена.** Лог: 5615 кликов за неделю; медиана клика 231 мс, из них запись (`UpdateDB`) 52 мс,
остальное ~165 мс; суммарно ~954 с за неделю по всем пользователям.

**Правка.** В режиме расстановки не перестраивать детали на каждый клик — обновлять их при
выборе окна в режиме просмотра или по отдельному действию; либо перепривязывать только список
текущей кампании.

**Риски.** Пользователи могут смотреть на панель выпусков окна во время расстановки — это
решение про поведение, спросить заказчика.

## П-8. Веер: двойная привязка «Добавленных выпусков», подписки копятся

**Где.** Клик веера: `updateDB` → `RefreshGrid` → `GridRefreshed` → `TariffGridRefreshed`
([EditIssuesForm.cs:286](../../Client/Forms/CreateActionMaster/EditIssuesForm.cs:286)) →
`grdCurrentCampaignIssues.DataSource = AddedIssues.DefaultView`; затем `onCellClicked` →
`grid_CellClicked` → переопределённый `ShowWindowIssues` (`:253`) → `TariffGridRefreshed`
**ещё раз**. В сеттере `SmartGrid.DataSource` (`:279–280`):
`bm = BindingContext[dataGrid.DataSource]; bm.PositionChanged += …` — без `-=`. Для того же
`DataView` `BindingContext` отдаёт тот же `CurrencyManager` → **+2 подписки на каждый клик**,
сбрасываются только при пересоздании `AddedIssues`. Плюс `FillAddedIssuesRollerNumbers` на
каждую привязку.

**Цена.** Процессор: лишняя привязка и с каждым кликом всё больше обработчиков на смену строки
(каждый из них из-за П-4 создаёт объект и поднимает `ObjectSelected`).

**Правка.** (1) В `SmartGrid` — `bm.PositionChanged -= …` перед `+=` (общий код, но правка
безопасная: повторная подписка того же обработчика никогда не нужна). (2) В веере —
`ShowWindowIssues` не дублировать `TariffGridRefreshed`, если сетка только что обновилась.

## П-9. Генерация по шаблону: ~110 мс на выпуск

**Где.** `FrmGenerator` ([FrmGenerator.cs:185](../../Client/Forms/FrmGenerator.cs:185)), на
каждый выпуск линейного шаблона:
- путь Simple (`AddSimpleIssue`, `:342`): `GetWindowByDate` (запрос) → при «не ставить к
  выпускам фирмы» `IsRollerOfTheFirmExist` (запрос) → `AddIssue` (`IssueIUD`) →
  **`issue.Refresh()`** (запрос) → `grdSuccess.AddRow` (`SmartGrid`: `AcceptChanges`,
  `SelectedObject`, события);
- путь TimePeriod (`AddSimpleIssues`, `:365`): на каждый день `PricelistByDate` +
  `TariffWindowRetrieve` за день, и `IsRollerOfTheFirmExist` на **каждое окно-кандидат** дня
  (~44 окна) при включённой галочке;
- после каждого дня `Application.DoEvents()`.

**Цена.** Лог: 324 генерации, 3993 выпуска, 452 с за неделю — ~110 мс на выпуск, при этом
`CampaignAddIssue` (`IssueIUD`) — ~4 мс. Интервалы между вставками 40–110 мс.

**Правка.** Окна периода — одним `TariffWindowRetrieve` на весь период шаблона (а не на день);
«окна с выпусками фирмы» — одним запросом на период (третий набор процедуры `Grid` уже это
умеет); `issue.Refresh()` — только если строка реально нужна для `grdSuccess` (гипотеза:
можно собрать строку без запроса). **Сначала замер**: где именно 100 мс — запросы или
`SmartGrid.AddRow`; без этого не трогать.

## П-10. `RecalculateAction()` перечитывает акцию после пересчёта

**Где.** `ActionOnMassmedia.Recalculate(refreshFlag = true)`
([ActionOnMassmedia.cs:529](../../Client/Classes/ActionOnMassmedia.cs:529)): `ActionRecalculate`
отдаёт `@totalPrice` OUTPUT-параметром, сумма кладётся в объект; при `refreshFlag` — ещё
`Refresh()` (`Actions1`), то есть вся строка акции заново. Вызовы с `true` по умолчанию:
`CampaignForm.cs:819` (удаление из списка), `:978` (Insert), `:1071` (Del), `:1165` (отмена
шаблона), `:1672` (изменение выпуска), `FrmGenerator.cs:238` (конец шаблона);
`ComboModulePlacementForm.cs:333/792` — там после пересчёта ещё и `ShowStatistics` сам делает
`_action.Refresh()`.

**Цена.** +1 запрос на операцию (десятки мс).

**Оговорка — почему нельзя просто поставить `false` везде.** OUTPUT отдаёт только
`totalPrice`. Остальные поля акции (`tariffPrice`, `iCount`, `duration`, скидка) освежает как
раз `Refresh()`:
- `CampaignForm` держит кнопку «Цена акции» по `CampaignAction.TariffPrice != 0` (`:96`, `:438`),
  а `ActionForm.SetActionPrice` отказывает при `TariffPrice == 0`. У новой акции (цена по
  тарифам 0) после первых выпусков без `Refresh` кнопка останется серой до переоткрытия формы.
  **Для одиночного клика так уже сейчас** (`RollerIssuesGrid3` зовёт `RecalculateAction(false)`)
  — возможно, это действующий мелкий дефект, стоит проверить руками;
- в веере и комбо статистика акции (`DisplayData`: выпуски, длительность, цены) рисуется из
  объекта акции — там `Refresh` нужен (в `ComboModulePlacementForm` лишний как раз второй,
  в `ShowStatistics`).

**Правка.** Вернуть из `ActionRecalculate` ещё и `tariffPrice` (OUTPUT) — тогда `Refresh` не
нужен в `CampaignForm`; в комбо убрать один из двух `Refresh`. Выигрыш маленький — пункт
последний в очереди.

## П-11. Одиночные записи и загрузки в циклах массовых операций

| Место | Что на итерацию |
|---|---|
| `CampaignForm.DeleteIssuesInSelectedWindows` (`:997`) | `WindowIssuesRetrieve` на окно + `IssueIUD` Delete на выпуск (пересчёт один) |
| `CampaignForm.UndoLastTemplateAdd` (`:1115`) | `IssueIUD` Delete на выпуск |
| `CampaignForm.MoveIssuesToWindow` (`:1334`) | Delete + `sourceIssue.Roller` (`new Roller` → запрос) + `AddIssue` |
| `EditIssuesForm.ReplaceRollerInSelectedWindows` (`:713`) | `GetCampaignById` + `new Roller` на группу — оба не нужны (данные есть в `_campaignsView` и `SlotIssueRow.Duration`) |
| `CampaignForm.ReplaceRollerInSelectedWindows` (линейка) | `WindowIssuesRetrieve` на окно + `RollerSubstitute` на выпуск (по дням, как веер, нельзя — см. `docs/mass-window-operations.md`) |
| `CampaignForm.SelectRollers` (чек-лист замены/удаления, общий для линейки и веера) | загрузка ролика на каждый id |
| `EditIssuesForm` удаление дублей / «до пересечения» / Del / перенос (`:1101`, `:1186`, `:495`, `:1724`) | `MasterIssueDelete` / `AddRangeIssues` на строку, `new Roller` на строку |
| `ComboModulePlacementForm` Del / отмена (`:703`, `:591`) | `ModuleIssueIUD` на выпуск |
| `TrafficGrid.TransferIssue` (`:764`) | `SelectedIssue.Roller` — новая загрузка на каждую итерацию; **и** берётся текущая строка, а не переменная цикла (дефект, см. `docs/tariffgrid.md` §9) |
| `TrafficManagementForm` «массово закрыть» (`:81`) | `SetDeadLine` на станцию |

**Правка.** Дешёвое: кэш роликов по id на время операции; чтение выпусков по списку окон одним
запросом; выкинуть ненужные `GetCampaignById`. Процедуры массовой записи — только если N на
практике сотни (IUD ~4–30 мс).

## П-12. Спонсорская сетка

- Отрисовка ([ProgramIssuesGrid2.cs:127](../../Client/Controls/ProgramIssuesGrid2.cs:127)):
  `tariffWindow.Issue.Campaign.Action.FirmName` для ячейки чужой фирмы → `CampaignPart.Campaign`
  → `GetCampaignById` → `Campaign.Action` → `GetActionById` — 2–3 запроса на ячейку, без кэша.
- Сетка подписана на свой `GridRefreshed` (`:40`) раньше формы → `onCellClicked` →
  `ShowWindowIssues` → `LoadIssues`, а форма следом делает `grdIssues.Clear()` — запрос
  выброшен.
- Клик по чекбоксу поднимает и `CellClick`, и `CurrentCellDirtyStateChanged`
  (`TariffGrid.cs:255–267`) — `ShowWindowIssues` дважды.

Сетка крошечная (1–3 строки) — терпимо. Правка: имя фирмы в наборе `ProgramIssues`.

## П-13. Пакетная: детали дважды на клик

`RefreshDetails` (`GetPackModuleIssueDetails`) вызывается и в `CampaignStatusChanged`
(`CampaignForm.cs:512`), и в `ShowWindowIssues` (`:632`) — они идут подряд после каждого клика.
Правка: убрать вызов из `CampaignStatusChanged`.

## П-14. Комбо: все выпуски акции на каждом листании ([UI-03])

`ComboModulePlacementForm.OnGridRefreshed` (`:1129`) на каждое обновление сетки, включая
листание недели/месяца, вызывает `LoadActionModuleIssues()` — все выпуски акции, от периода не
зависящие. После удаления — дважды (`:814` и `OnGridRefreshed`). Плюс ~2 с клиентской
перестройки на операцию (замер 30.08.2026, [UI-03]). Правка: выпуски — при открытии и после
записи; листание их не перечитывает.

## П-15. `ActionForm` после закрытия формы кампании

`ActionForm.cs:241–245` (редактирование кампании) — `RefreshActionStats(true)` + `LoadCampaigns`
безусловно, даже если ничего не менялось (у формы есть `ChangeFlag`, `CampaignForm.cs:168`, — `Campaign.EditRollerIssues` им уже пользуется, `Campaign.WinForms.cs:171`); `:545–546` (веер) — то же.
Спонсорская: `Campaign.WinForms.cs:186` и `ProgramPartOfSponsorCampaign.WinForms.cs:64` —
лишние `Recalculate` (каждый клик уже пересчитал). Правка: обновлять по `ChangeFlag`.

## П-16. Трафик: «массово закрыть» грузит сетку дважды

`TrafficManagementForm.TsbMassClose_Click` (`:78–92`): `LoadRadiostationsByGroup` ставит
`DataSource` станций → `ObjectSelected` → `TrafficGrid.RefreshGrid` (`TrafficGrid.cs:348`), затем
явный `grdTariffWindow.RefreshGrid()`. Правка: убрать явный.

## П-17. Удаление сгенерированных окон по одному дню

`TariffWindowGrid.DeleteGeneratedTariffWindows` (`:169`, по одному времени) и
`MassmediaPricelist.DeleteGeneratedTariffWindows` (`MassmediaPricelist.WinForms.cs:287`, всё
окно) вызывают `TariffWindowMassDelete` в цикле по дням. Лог: 480 медленных вызовов, 167 с за
неделю, по ~250 мс. Генерация окон при этом идёт неделями (`GenerateTariffWindows`, шаг 7 дней).

**Прежде чем менять — уточнить замысел.** Дробление, вероятно, сознательное: прогресс и отмена
(`BackgroundWorker`), короткие блокировки `TariffWindow`/`Issue`. Если так — шаг в неделю, как у
генерации, даст ×7 меньше вызовов без потери прогресса.

Ещё дефект того же цикла: `while (startDate < finishDate)` с перекрытием границ — последний день
порции идёт первым днём следующей, а интервал из одного дня не обрабатывается вовсе.

**В вебе (23.09.2026) сделано неделями без перекрытий** (`PricelistWindows.Weeks`), с прогрессом
и остановкой между порциями: удаление всех окон прайс-листа за год — 53 вызова, на проверке
~1 с. Десктоп не менялся.

## Попутно, не производительность

См. `docs/tariffgrid.md` §9: мёртвая проверка `grid is IRollerGrid` (сообщение «нет
прайс-листа» не показывается никогда), `TrafficGrid.TransferIssue` берёт не ту строку,
`PresentationObject.Equals` по хешам.
