# Массовые операции по выделенным окнам тарифной сетки

Выделить окна мышью (протяжка / Ctrl) **в режиме просмотра** («Старт» отжата — в режиме
редактирования клик добавляет выпуск, `SetEditMode` держит `MultiSelect` только в `View`),
затем клавиша или кнопка тулбара. Пользовательское описание веера —
`docs/veer-editor-behavior.md` (§5, §6, §9) и справка `Client/Help/veer.html`.

## Где работает

| Форма / грид | Включение | Операции |
|---|---|---|
| Простая линейная кампания, `CampaignForm` + `RollerIssuesGrid3` (`IsSimplelCampaign`, в т.ч. спонсорская с роликовой сеткой) | `EnableWindowSelectionActions()` в `CampaignForm_Load` | Del, Insert, Ctrl+R, PgUp/PgDn |
| Веер, `EditIssuesForm` + `TariffWithRangeGrid` | `EnableWindowSelectionActions()` в `EditIssuesForm.OnLoad` | Del, Insert, Ctrl+R, «Удалить дубли», «Добавить до полного пересечения», PgUp/PgDn |
| Модульная кампания | `EnableWeekNavigationKeys()` | только PgUp/PgDn (выпуск модуля — по всем окнам столбца, другая семантика) |
| Спонсорская (`ProgramIssuesGrid2`), пакетная (`PackModuleGrid`) | — | не сделано |

Клавиши ловит `CampaignForm.TariffGrid_KeyDown`; операции — `protected virtual` в
`CampaignForm` (линейка), override в `EditIssuesForm` (веер):
`DeleteIssuesInSelectedWindows`, `AddIssuesInSelectedWindows`, `ReplaceRollerInSelectedWindows`.
Окна выделения — `TariffGrid.GetSelectedTariffWindows()`.

## Общие правила (линейка и веер одинаково)

- **Чек-лист роликов.** Если в выделенных окнах несколько разных роликов, Del и Ctrl+R
  показывают `CampaignForm.SelectRollers(rollerIds, caption)` — `SelectionForm` по сущности
  Roller с колонкой «№» из списка «Ролики» (`BuildRollerNumbersMap`). Выбор в чек-листе
  заменяет вопрос-подтверждение; при одном ролике — обычный вопрос «… (N шт.)?».
- **Замена только при «Номерах роликов».** Кнопка «Заменить ролики» (`tbbReplaceRoller`)
  включена только при нажатой `btnShowRollerNumbers`; Ctrl+R проверяет то же самое сам.
  Ролик, совпадающий с новым, пропускается.
- **Частичный успех.** Каждая запись — отдельный вызов; ошибки копятся в
  `SmartGrid.CreateDeleteErrorsTable` / `ShowDeleteErrors` (модально), незаменённые по
  бизнес-правилам (прошлое, дедлайн, агитация…) — `CampaignRoller.ShowUnsubstitutedRollers`.
- **Пересчёт один раз в конце**, затем полный `RefreshGrid`. Для замены пересчёт акции —
  только если длина старого и нового ролика различается (ролик той же длины цену не меняет,
  `RollerSubstitute` при `@diffDuration = 0` меняет только `rollerID`).
- Массовые операции **не кормят** кнопку «Отменить» (её область — последний шаблон).

## Различия реализации

| | Линейка (`CampaignForm`) | Веер (`EditIssuesForm`) |
|---|---|---|
| Источник выпусков | `LoadCurrentCampaignIssueRows` → `WindowIssuesRetrieve` на окно, фильтр `campaignId` (`[dbo].[Grid]` issueID не отдаёт) | синие — in-memory `AddedIssues`; красные (частичные) — `RangeSlotIssues` одним батчем |
| Del | `IssueIUD` Delete на выпуск; рефреш напрямую, **без** `ObjectsDeleted` (иначе `RefreshCurrentCell` красит окно под курсором) | `MasterIssueDelete` (синие, по всем выбранным кампаниям) + `DeleteSlotIssueGroup` (красные, несколько проходов `SplitIntoDeletePasses`); завершение через `ObjectsDeleted` → `ProcessCurrentCampaignIssuesDelete` |
| Ctrl+R | `CampaignRoller.ApplyRollerSubstitutionForIssue` — `RollerSubstitute` **на выпуск** (`@issueID` + `@originalWindowID`, `#days` процедура строит сама) | `CampaignRoller.ApplyRollerSubstitutionForDays` — `RollerSubstitute` на пару (кампания, старый ролик) с таблицей `#days (windowID, issueDate)` |
| Insert | `RollerIssuesGrid3.AddIssueToWindow` (транзакция на окно, П-1: пересчёт на каждое окно) | `AddIssuesRange(date, false, recalculate:false)` в цикле |

**Почему линейка меняет по выпуску, а не по дням, как веер.** Для `campaignTypeID` 1/2
`RollerSubstitute` находит выпуски по `#days`: `tw.dayOriginal = d.issueDate AND d.windowID =
i.originalWindowID`, где `tw` — **исходное** окно выпуска. `WindowIssuesRetrieve` отдаёт
`issueDate = windowDateActual` фактического окна, а `dayOriginal` исходного — нет; у
перенесённого выпуска окна разные. Поэтому вызов по одному выпуску — без правки SQL.

## Связанное

- `docs/tariffgrid.md` §6 — число обращений к БД по сценариям; `docs/tasks/tariffgrid-desktop-perf.md` П-11 — одиночные вызовы в циклах.
- `docs/scenarios/range-issue-add-click-to-db.md` — модель слотов веера (`AddedIssues`, красные/синие).
