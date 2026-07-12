# SmartGrid — справочник по контролу

`FogSoft.WinForm\Controls\SmartGrid.cs` (+ `.Designer.cs`), `namespace FogSoft.WinForm.Controls`,
`public partial class SmartGrid : UserControl, IObjectControl`. Обёртка над внутренним
`DataGridView` (`dataGrid`), которая знает про `Entity`/`PresentationObject` — умеет сама строить
колонки по атрибутам сущности, резолвить объект под курсором, показывать контекстное меню CRUD,
чекбоксы, мультивыбор, подсветку по колонке, quick-search и связку "родитель/зависимый грид".

Используется в ~15+ формах (`CampaignForm.grdRollers`, `RadiostationListWithGroup`,
`CampaignNewForm`, `RollerStatisticForm`, `TrafficManagementForm`, `FrmGenerator`,
`PaymentCandidatesForm`, `SelectionForm`, `TreeViewMultiSelector`, `ExplorerForm`, `JournalForm`,
`MasterDetailForm`, `GraphForm` и др.). Один из ключевых контролов приложения — правки в файле
задевают почти весь UI.

> Другие семейства гридов в проекте — **не SmartGrid**: `PriceCalculatorGrid`
> (`Client\Controls\PriceCalculatorGrid.cs`) и `FrmTemplate3.dgvRollers` — самостоятельные
> `DataGridView` с продублированной логикой чекбокс-колонки. Если задача касается их — этот
> документ не применим напрямую, паттерны придётся переносить руками (см. риск ниже).

---

## Основной паттерн: "атрибутивная" опциональная колонка

Часть колонок не связана с данными сущности — это функции самого контрола, включаемые булевым
свойством, по аналогии с чекбоксами. Общая схема:

1. private-поле + public-свойство (без `[Browsable]`/`[Category]` — попадают в дизайнер как есть).
2. Флаг читается в `SetColumnHeaders(DataColumnCollection)` [`SmartGrid.cs:868`] — единственном
   месте, где строится порядок колонок. Порядок вызовов внутри метода = порядок колонок в гриде:
   ```
   if (checkboxes)      AddMultiSelectColumn();   // чекбокс — если включён, всегда первый
   if (showRowNumbers)   AddRowNumberColumn();     // нумерация — сразу после чекбокса
   if (icon != null)     AddImageColumn(icon);     // иконка сущности (не флаг, см. ниже)
   foreach (...)         AddColumn(entityAttribute);
   ```
3. `SetColumnHeaders` вызывается только из сеттера `DataSource` [`SmartGrid.cs:211`], и только в
   ветке `entity != null` — то есть все опциональные колонки работают исключительно в
   entity-driven режиме. Если `entity == null`, включается `dataGrid.AutoGenerateColumns = true`
   [`SmartGrid.cs:266`], и ни одна из этих колонок не появится, независимо от флагов.

Три существующих флага:

| Свойство | Поле | Строка | Что добавляет |
|---|---|---|---|
| `CheckBoxes` | `checkboxes` | `125` | Колонку `DataGridViewCheckBoxColumn`, привязанную к синтетической `DataColumn COL_IsSelected` (добавляется в таблицу тут же, в сеттере `DataSource`, если её ещё нет) |
| `ShowMultiselectColumn` | `showMultiselectColumn` | `131` | Не колонку, а чекбокс "выбрать всё" — overlay `CheckBox` поверх заголовка колонки 0 (`AddCheckBox2ColumnHeader`, `963`). Независим от `CheckBoxes`: можно иметь чекбоксы в строках без чекбокса в шапке |
| `ShowRowNumbers` | `showRowNumbers` | `137` | Колонку `№` (`AddRowNumberColumn`, `939`). Данные НЕ хранятся — номер вычисляется на лету в `DataGrid_RowNumberCellFormatting` (`87`) как `e.RowIndex + 1`, поэтому корректно следует за сортировкой/фильтром |

Если понадобится ещё одна такая колонка — добавлять по этой же схеме: поле + свойство рядом с
существующими (`125-140`), `AddXxxColumn()` рядом с `AddMultiSelectColumn`/`AddRowNumberColumn`
(`928-951`), вызов внутри `SetColumnHeaders` в нужном месте порядка.

Колонка иконки сущности (`AddImageColumn`, `896`) добавляется **безусловно**, если
`Globals.IconLoader != null` и у сущности задан `IconName` — публичного флага для неё нет, это не
аналогичный паттерн, а всегда включённая фича.

---

## Публичное API (сгруппировано)

**Данные / привязка**
`Entity` `[Browsable(false)]` — целевая сущность; `DataSource` (`DataView`) `[Browsable(false)]`
— главная точка входа, пересобирает колонки и чекбоксы при каждом присваивании; `InternalGrid`
`[Browsable(false)]` — доступ к сырому `DataGridView`, если API контрола не хватает; `ItemsCount`.

**CRUD извне** (используются формами, которые держат `SmartGrid` как деталь-грид)
`AddRow(po)` `652`, `UpdateRow(po)` `672`, `DeleteRow(po, withFireEvent)` `712`, `Clear()` `763`,
`Contains(po)` `1441`.

**Выбор**
`SelectedObject` `638` (get/set), `SelectObject()` `1447` (private, есть развилка сортированный/
несортированный `DataView` — см. риски).

**Опциональные колонки** — `CheckBoxes`, `ShowMultiselectColumn`, `ShowRowNumbers` (см. таблицу
выше); нативный `MultiSelect` `1866` — **другое понятие**, см. риски.

**Подсветка строк** — `IsNeedHighlight`, `ColumnNameHighlight`, `ColorHighlight` (`enum`, `1873`:
`Red/Green/Blue`), `IsHighlightInvertColor`, `HighlightRows()` `295` (публичный, можно дёргать
вручную после ручного изменения данных).

**Массовое удаление** — `DeleteCurrentObject()` `1570`, `DeleteSelectedObjects()` `1584`,
`RaiseObjectsDeleted(list)` `1320` (публичная обёртка для внешних сценариев массового удаления,
которые сами формируют список удалённых объектов, минуя штатный `DeleteSelectedObjects`),
`ShowDeleteErrors(table)` / `CreateDeleteErrorsTable()` / `AddDeleteError(...)` — `1686-1719`,
`public static`, переиспользуются вне класса для единого UI ошибок.

**Прочее** — `Caption`/`CaptionVisible`, `QuickSearchVisible`, `DependantGrid` `162`,
`AdjustColumnsWidth(maxWidth)` `559` / `AdjustColumnsWidthExt(maxWidth)` `436` (два разных
алгоритма автоширины — второй новее, с переносом заголовка по словам), `MenuEnabled`,
`Added2Checked`/`RemovedFromChecked` `[Browsable(false)]` (сырые списки diff'а чекбоксов).

## События

| Событие | Когда стреляет | Пример подписчика |
|---|---|---|
| `ObjectSelected` | Смена текущей строки (`Bm_PositionChanged`) и в конце сеттера `DataSource` | `TrafficGrid.cs:302`, `MasterDetailForm.Designer.cs` |
| `ObjectCreated` / `ObjectChanged` / `ObjectDeleted` | `AddRow`/`UpdateRow`/`DeleteRow` либо CRUD через контекстное меню | — |
| `ObjectsDeleted` | Один раз в конце `DeleteSelectedObjects`/`RaiseObjectsDeleted` | внешние сценарии массового удаления (`CampaignForm`) |
| `ObjectChecked` | Изменение чекбокса в строке | `TreeViewMultiSelector.cs:163`, `FrmFirmBalance.cs:89`, `SelectMassmediasStep.Designer.cs` |
| `ColumnSelected` | Клик по заголовку колонки, резолвится валидный `currentColumn` | `ExplorerForm`, `JournalForm`, `GraphForm`, `MasterDetailForm` (Designer-подписки) |
| `RecordCountChanged` | После каждого символа в quick-search — передаёт число строк после фильтра | `ExplorerForm.cs:22`, `JournalForm.cs:46` (просто ретранслируют выше) |
| `EntityParentChanged`, `ContainerRefreshed`, `DblClick`, `RefreshAll` | Ретрансляция действий из контекстного меню / `IVisualContainer` | — |

---

## Риски / известные ограничения

| # | Риск | Описание |
|---|---|---|
| 1 | **`MultiSelect` ≠ `CheckBoxes`** | `MultiSelect` (`1866`) — нативный `DataGridView.MultiSelect` (ctrl/shift-клик по строкам, использует `dataGrid.SelectedRows`). `CheckBoxes` — своя колонка `COL_IsSelected` со своим учётом (`added2Checked`/`removedFromChecked`). Если включены оба — `DataGrid_CellContentClick` (`1476`) синхронизирует чекбокс → `Row.Selected` в одну сторону, под флагом `_lockMultiSelect`, чтобы не зациклиться с `DataGrid_CellValueChanged` |
| 2 | **`Clear()` не сохраняет чекбоксы** | `Clear()` (`763`) сбрасывает `added2Checked`/`removedFromChecked` в новые пустые списки без восстановления. Сохранение/восстановление чекбоксов по PK (`SaveCheckedKeys`/`RestoreCheckedKeys`, `830/846`) обёрнуто ТОЛЬКО вокруг сеттера `DataSource` — прямой вызов `Clear()` в обход этого сеттера теряет отмеченные строки без предупреждения |
| 3 | **`isFirstLoad` в сеттере `DataSource`** | При первом присвоении `DataSource` серверные значения `COL_IsSelected` не трогаются; при повторном — все `COL_IsSelected` сбрасываются в `false` перед тем, как `RestoreCheckedKeys` заново проставит сохранённые PK (`209-240`). Перепутать порядок — либо утечка старых чекбоксов, либо затирание свежих серверных |
| 4 | **`RestoreCheckedKeys` идёт по `table.DefaultView`, не по `table.Rows`** | Намеренно (`846`, есть коммент) — чтобы учитывать активный `RowFilter`. "Упрощение" до `table.Rows` восстановит чекбоксы и на отфильтрованных строках |
| 5 | **`SelectObject()` — разная арифметика для sorted/unsorted `DataView`** | (`1447`) При пустом `dataView.Sort` используется `Table.Rows`, а не `DefaultView.Count`, потому что `DefaultView.Count` отражает только видимые строки. Перепутать — `IndexOutOfRange` либо неверная позиция `bm.Position` |
| 6 | **`AddCheckBox2ColumnHeader` — хрупкий геометрический код** | (`963`) Ручной расчёт центрирования overlay-чекбокса поверх заголовка колонки 0 через `GetCellDisplayRectangle`, с отладочными `Debug.WriteLine` и закомментированной более старой версией `RepositionCheckBoxHeader` прямо в файле (`~1020-1040`). Любое изменение отступов/рамок `DataGridView` в теме/стиле потребует повторной подгонки пиксельных офсетов |
| 7 | **`EscapeLikeValue` не экранирует `]`** | (`1433`) Экранирование `[`, `%`, `_` есть, `]` — закомментировано. Текст поиска с `]` в сочетании с уже подставленным `[` потенциально ломает сгенерированный `LIKE`-фильтр в `QuickSearch()` |
| 8 | **`dataGrid_RowPrePaint` глотает исключения молча** | (`1792`) В отличие от остальных обработчиков файла (которые зовут `ErrorManager.PublishError`), здесь ошибки уходят только в `Debug.WriteLine` — баг в покраске строки будет незаметен в продакшн-сборке |
| 9 | **Массовое удаление снимает снимок ДО удаления** | `DeleteSelectedObjects()` (`1584`) сначала собирает `(PresentationObject, name)` по всем выбранным строкам, и только потом удаляет по одной — потому что `DataBoundItem`/индексы `DataView` инвалидируются по ходу удаления. Ошибки по каждой строке копятся отдельно (soft-fail), не fail-fast |
| 10 | **Опциональные колонки работают только при `entity != null`** | Если сущность не задана и включён `AutoGenerateColumns` (`266`), ни `CheckBoxes`, ни `ShowRowNumbers`, ни подсветка колонок не применяются — `SetColumnHeaders` в этой ветке вообще не вызывается |
| 11 | **`PriceCalculatorGrid` и `FrmTemplate3.dgvRollers` — не SmartGrid** | У них своя, независимо продублированная реализация чекбокс-колонки и overlay-заголовка (см. заметку в начале документа). Паттерн "атрибутивной колонки", описанный здесь, там просто отсутствует как класс — нужно реализовывать отдельно, вручную повторяя решение |

---

## Файлы

| Файл | Роль |
|---|---|
| `FogSoft.WinForm\Controls\SmartGrid.cs` | Вся логика контрола |
| `FogSoft.WinForm\Controls\SmartGrid.Designer.cs` | Дизайнер-обвязка: `dataGrid`, `lblCaption`, `panelSearch`/`txQuickSearch` |
| `Client\Forms\CampaignForm.Designer.cs` | Пример реального использования: `grdRollers` (`CheckBoxes=false`, `ShowRowNumbers=true`), `grdIssues`, `grdCurrentCampaignIssues` |
