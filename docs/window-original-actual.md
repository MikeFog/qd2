# Оригинал и факт: время окна и окно выпуска — справочник

**Состояние на 11.10.2026.** Первая версия — исследование 07.10.2026, до перехода на фактическое окно. Переписано после
этапов 0–7 перехода (`Deploy/24`–`35`). По `Deploy/README.md` все они на ArtvisDev и проде (Artvis); на Tumen подтверждены
28, 29, 31, 33–35 (26, 27, 32 — косвенно, через защиты 33 и 34), о 24, 25, 30 записей нет; Belgorod, Artvis2, Univer — нет сведений. Проверено чтением кода рабочей копии и `SELECT` на ArtvisDev, вживую сценарии не воспроизводились.
Процедуры общие у десктопа (qd2) и веба. Пути процедур — относительно `ArtvisDB/dbo/Stored Procedures/`.

В системе **два независимых** «оригинал/факт»:

| | Время окна | Окно выпуска |
|---|---|---|
| Колонки | `TariffWindow.windowDateOriginal` + `dayOriginal` / `windowDateActual` + `dayActual` | `Issue.originalWindowID` / `Issue.actualWindowID` |
| Смысл | время по расписанию (слот тарифа, пишется при создании окна) / время выхода (куда окно сдвинул трафик) | окно постановки / окно выхода (где выпуск реально выйдет) |
| Кто разводит | `TariffWindowMoveTime` (`:121–124`) и паспорт окна (`TariffWindowIUD` UpdateItem, `:136`). Пишут только время выхода, выпуски едут вместе с окном | только `IssueTransfer` — перенос выпуска трафиком (§3.2). «Перенос дня» и автоперенос при активации ставят оба окна в новое (§3.5) |
| Ограничение | только внутри дня (Р-2): MoveTime строит новое время от `dayOriginal` (`:44–47`), паспорт и «Добавить» отказывают `TariffWindowDayChange` (с Deploy/31). Итог: `dayOriginal = dayActual` у всех окон | приёмник — той же станции и без второго 4/44, 5/55, 7 по окну выхода (Р-12, с Deploy/27). День, месяц (Р-11) и прайс-лист не ограничены |
| Объём (ArtvisDev, копия прода ≈24.09.2026) | с 01.09.2025: 32 120 окон из 968 тыс. (3,3%), сдвиг до ±60 мин, у 2 042 меняется час, **день — ни у одного**; в таких окнах ~57 тыс. выпусков (07.10) | 1 207 из 3,42 млн выпусков (11.10): линейных 518, модульных 688, спонсорский 1, пакетных 0. На другой день — 247 (218 линейных, 29 модульных, в основном +1). Станция та же у всех, через границу месяца — 0. Будущих 2, оба тестовые (41707758, 41793772) |

**Правило (Миша, 07.10.2026).** Всё, что показывает, отбирает, проверяет и считает выпуск, работает по **окну выхода**
(`actualWindowID`) и его времени выхода (`windowDateActual`/`dayActual`): эфир, сетки, проверки, права, деньги, журналы,
отчёты. Исходные значения остаются только как ключ тарифного слота (Р-1), день модуля и пакета у модульных и пакетных
выпусков, история переносов и запасной тариф: у окна выхода без тарифа берутся тариф и прайс-лист окна постановки (Р-3 (2)) —
подробно §3.3. Карта мест, этапы и журнал решений —
[tasks/window-actual-switch.md](tasks/window-actual-switch.md); решения Р-* — [tasks/window-actual-open-questions.md](tasks/window-actual-open-questions.md).
Отображение (название окна, приписки, подписи паспорта) переход не менял, кроме приписки времени выхода в десктопной сетке
генерации (Р-1); остальное — вопросы заказчику,
[window-original-actual-customer.md](window-original-actual-customer.md) §10.

У выпуска нет своего времени: время и день берутся JOIN-ом к `TariffWindow` (у спонсорских программ время своё —
`ProgramIssue.issueDate`). Поэтому у каждого читателя две оси: **какое окно** и **какое время этого окна**. Норма —
`actualWindowID` + `windowDateActual`. Ось дня выбирать не нужно: по Р-2 `dayOriginal = dayActual`, поэтому
`tw.dayOriginal` **окна выхода** в коде после перехода (например, `IssueTransfer.sql:267`, `GetPriceByPeriod.sql:58`) — не ошибка.

**Осторожно со старыми именами.** Часть колонок несёт окно выхода под прежним именем — читать по смыслу, а не по имени:
- `RangeSlotIssues.originalWindowID` / `windowDayOriginal` (`:53–54`) — окно выхода и его день (с Deploy/34);
- `windowDateOriginal` в «Незамененных роликах» (`RollerSubstitute.sql:365`) — время выхода;
- `IssuesByDate.issueDateOriginal` (`:27`, `:55`) — время по расписанию **окна выхода** (слот для клона);
- параметр `@originalWindowID` у `RollerSubstitute` (`:29–35`) — только признак режима «один выпуск», значение не читается;
- `Grid` отдаёт обе колонки (`:39–40`); `originalWindowID` там — настоящее окно постановки, оставлено для старого клиента (О-5).

Переименование — этап 9, отложен: [tasks/window-actual-stage9.md](tasks/window-actual-stage9.md).

---

## 1. Как это выглядит в UI

**Перенос времени окна** — форма «Трафик-менеджмент» (`TrafficManagementForm` → `TrafficGrid`), только в пределах дня:
- меню строки «Отредактировать время выхода...» → `TrafficManagement.MoveTime` → `TariffWindowMoveTime`
  ([TrafficGrid.cs:547](../Client/Controls/TrafficGrid.cs)); в вебе — «Изменить окна…»
  ([TrafficManagement.cs:168](../Client/Classes/TrafficManagement.cs)), та же процедура;
- паспорт окна «Свойства» → «Время выхода реальное:» → `TariffWindowIUD` UpdateItem; на другой день — отказ
  `TariffWindowDayChange` (с Deploy/31).

**Перенос выпуска** — та же форма:
1. щелчок по окну-источнику — внизу его выпуски (`WindowIssuesRetrieve`, по **окну выхода**, `:34–36`);
2. галочка с датой окна (`chkDate`) включает режим переноса, источник красится фиолетовым
   ([TrafficGrid.cs:301](../Client/Controls/TrafficGrid.cs), подсветка — `:312`);
3. отметить выпуски галочкой;
4. щелчок по окну-приёмнику (жёлтое);
5. вопрос `IssuesTransferConfirmation` «Вы желаете перенести выбранные выпуски с … на …?».

На каждый отмеченный выпуск, каждый в своей транзакции ([TrafficGrid.cs:812](../Client/Controls/TrafficGrid.cs)):
`SimpleTransfer.Transfer` → `RollerIssue.Transfer` ([RollerIssue.cs:90](../Client/Classes/RollerIssue.cs)) → `DoAction`
(сущность 98, действие Transfer) → `IssueTransfer`. «Ролик уже есть в окне» и «ролик этой фирмы уже есть в окне» — только
вопрос пользователю ([IssueTransfer.cs:49](../Client/Classes/IssueTransfer.cs), `:59`); окно другой станции и второй
«Локальное СМИ», «Федеральное СМИ» или «Отбивка политической агитации» — отказ в SQL (с Deploy/27). В вебе — страница
«Трафик» ([Traffic.razor:709](../FogSoft.Web/Components/Pages/Traffic.razor) → `TrafficManagement.Transfer`,
[TrafficManagement.cs:383](../Client/Classes/TrafficManagement.cs)), предупреждения — одним вопросом на пачку.

**Строки сетки трафика** — время по расписанию вместе с ценой строки, то есть ключ слота (Р-1; `UseActualTime => false`,
[TrafficGrid.cs:65](../Client/Controls/TrafficGrid.cs)). Время выхода сдвинутого окна — приписка « (чч:мм)» в ячейке
(`ActualTimePostfix`, [TariffWindowGrid.cs:495](../Client/Controls/TariffWindowGrid.cs); веб —
[TariffWindowWeek.cs:269](../Client/Classes/TariffWindowWeek.cs)). Та же приписка с 07.10.2026 есть в десктопной сетке
генерации; в веб-генерации ([TariffWindowWeek.cs:456](../Client/Classes/TariffWindowWeek.cs)) и в модульной сетке кампании
её нет.

**Название окна** (заголовок «Свойства: …», вопрос «Удалить …?») — «Рекламное окно 17.10.2026 10:42 (17.10.2026 10:20)»:
главное — время по расписанию, время выхода в скобках (`TariffWindowRetrieve.sql:134–138`; в ветке без особых окон
`:169–170` времени выхода нет). **Паспорт окна**: «Время выхода:» — расписание, «Время выхода реальное:» — время выхода.
Вариант «10:20 (по расписанию 10:42)», приписки и подписи — вопросы заказчику (§10 документа для него).

---

## 2. Время окна: где расписание, где факт

### 2.1. Эфир: выгрузки и печать — везде факт

Эфир шёл по факту и до перехода. С Deploy/33 по дню окна выхода считаются и суммы (эфирная справка, итоги медиаплана).

| Что | Вход | Процедура | Время | Окно выпуска | Отбор по дню |
|---|---|---|---|---|---|
| Выгрузка DJin | «Сетка вещания» → «Экспорт...», «Экспорт сеток вещания» | `rpt_Grid_v3` | `windowDateActual` (`:69`, через `fn_GetTimeString`) | `actualWindowID` (`:120`, `:123`; модули без выпуска — `:257`) | `dayActual` (`:93`, `:127`, `:261`) |
| Печать «Сетка вещания» (Crystal `Grid.rpt`; веб — `PrintGrid.razor`) | та же форма, «Обновить» | `rpt_Grid_v3` (подвал с заполняемостью убран 11.10.2026) | то же | `actualWindowID` | `dayActual` |
| Эфирная справка | действие у кампаний (сущности 91–93, 171), журнал актов (156) | `OnAirInquireReport`; сумма — `GetPriceByPeriod` | `windowDateActual` (`:21`) | `actualWindowID` (`:36`); сумма — тоже (`GetPriceByPeriod.sql:51`, `:66`) | `dayActual` (`:39`); сумма — день окна выхода (`GetPriceByPeriod.sql:58`) |
| Медиаплан («График размещения»), все варианты | акция, кампании, журнал актов, «по нескольким акциям» | `MediaPlanRetrieve_v2` | все вызовы (десктоп и веб, [MediaPlanBuilder.cs:185](../Client/Classes/MediaPlanBuilder.cs), `:451`) передают `isFact = 1` → `windowDateActual` (`:117`) | `actualWindowID` (`:124`); итоги — `GetPriceByPeriod` | `dayActual` (`:134–135`, `:144–145`) |
| Месяцы для справки, медиаплана и счёта по месяцам | — | `sl_Months`; `GetMonthes` (все вызовы — `isFact = 1`) | — | `actualWindowID` (`sl_Months.sql:13`, `GetMonthes.sql:19`) | `dayActual` (`sl_Months.sql:9`, `GetMonthes.sql:16`) |
| Обрамление агитации (влияет на DJin) | активация, деактивация, перенос трафиком, «Перенос дня», удаления | `AgitationFraming` | `windowDateActual` (`:48–50`) | `actualWindowID` (`:128`, `:161`, `:210`, `:234`) | — |

Примечания:
- Спонсорские программы в сетке и справке идут без окон: в сетке — `SponsorTariff.time` при отборе по `ProgramIssue.issueDate`
  (`rpt_Grid_v3.sql:313–360`), в справке — время самого `ProgramIssue.issueDate` (`OnAirInquireReport.sql:45–56`).
- `fn_GetTimeString` даёт «24:xx» только при начале эфирного дня не 00:00; с шага 1 удаления `broadcastStart` оно везде 00:00.
- DJin режет блоки по строке времени, а не по `windowId` ([BroadcastGridExport.cs:110](../Client/Classes/GridExport/BroadcastGridExport.cs),
  [ExportDocument.cs:84](../Client/Classes/GridExport/ExportDocument.cs)): два окна станции с одной минутой выхода сольются
  в один блок (не проверено вживую). С Deploy/35 «Создать новое окно…», шаблон и «Продлить окно» не создают окно в занятую
  минуту (Р-7 (в)), но перенос времени и паспорт окна по-прежнему могут свести два окна в одну минуту.
- Склейки (`windowPrevId`/`windowNextId` — один блок DJin) с Deploy/35 проверяются по времени выхода, интервал открытый
  (Р-8 (а)): `f_CheckLinkedTariffWindows` отказывает новому окну внутри эфирного промежутка пары (`TariffWindowIUD.sql:174`,
  `GenerateTariffWindowByTemplate.sql:91`), `CheckLinkedWindows` после генерации рвёт объединение, если между звеньями
  выходит чужое окно (`:16–37`). Перенос времени и паспорт новых отказов не дают. В [window-merging.md](window-merging.md)
  (#14, #15) это ещё не отражено.
- Ветки `isFact = 0` (окно постановки) мёртвые: `MediaPlanRetrieve_v2.sql:151–190`, `GetMonthes.sql:23–32` (умолчание
  параметра — 0, вызывающие передают 1), `GetUniqueMMsForAction.sql:34`, `:40`, `GetUniqueMMsForPackModuleCampaign.sql:15`, `:19`.
  Чистка — этап 9 ([tasks/window-actual-stage9.md](tasks/window-actual-stage9.md) §3.4). Мёртвые выгрузки: обмен с Media Plus
  и Граммофоном (`Client/Classes/Exchange/*`, по окну постановки) — stage9 §3.5; `VectorBoxSerializer`, `VideoDJSerializer` и
  `rpt_ActionPrograms` (вызывает только `ActionAgreementReport.cs`) — по факту, не в `Client.csproj`, в этап 9 не входят.

### 2.2. Тарифные гриды форм кампании

Строка сетки — либо время выхода, либо время по расписанию (слот, Р-1); во втором случае время выхода показывает приписка
(в трафике и генерации; в модульной сетке её нет).
День окна один (Р-2), поэтому в колонке дня ось выбирать не нужно.

| Сетка | Строка (время суток) | Колонка (день) | Текст ячейки |
|---|---|---|---|
| Линейная (`RollerIssuesGrid3`, `module == null`), роликовая часть спонсорской | **факт** (`UseActualTime => module == null`, [RollerIssuesGrid3.cs:50](../Client/Controls/RollerIssuesGrid3.cs)) | день окна ([TariffWindowGrid.cs:459](../Client/Controls/TariffWindowGrid.cs)) | остаток; приписки нет — строка и так по факту |
| Модульная (тот же класс, `module != null`) | расписание | день окна | остаток; **время выхода не видно**: приписка только при `showTrafficWindows` ([RollerIssuesGrid3.cs:377](../Client/Controls/RollerIssuesGrid3.cs), `:382`) — вопрос заказчику (§10 п. 3). Будущих сдвинутых окон модульных тарифов — 4 912 (11.10) |
| Трафик (`TrafficGrid`) | расписание (`UseActualTime => false`) | день окна | остаток + «(чч:мм)» времени выхода |
| Генерация окон (`TariffWindowGrid`) | расписание | день окна | длительность [вместимость] + «(чч:мм)» ([TariffWindowGrid.cs:484](../Client/Controls/TariffWindowGrid.cs)), с 07.10.2026 |
| Веер (`TariffWithRangeGrid`) | **факт**, получасовой слот (`TariffWindowWithRange.sql:119–143`, `:180`) | день слота | минимальный по станциям остаток (`:162–201`); синие ячейки (`:307`) и ячейки выпусков фирмы (`:241`) — по окну выхода |
| Пакетная (`PackModuleGrid`), комбо (`ComboModuleGrid`) и веб-вкладка «Модули» (`ModuleDaySource.cs:734`) | строк времени нет | день окна (`PackModuleTariffWindowsRetrieve.sql:48`, `:73`; `ComboModuleFreeTimeRetrieve.sql:145`, `:200–202`) | остаток; «Предметы рекламы» в комбо и «Модулях» — по окну выхода (с Deploy/26) |
| Спонсорская (программы) | окна не используются | — | — |

- `ActualTimePostfix` ([TariffWindowGrid.cs:495](../Client/Controls/TariffWindowGrid.cs)) — общая для трафика и генерации.
  Ветка «(дд.ММ.гг чч:мм)» при `dayActual ≠ dayOriginal` (`:499–500`) после Р-2 недостижима (этап 9).
- Отметки выпусков в линейной и модульной сетке (синие ячейки, номера роликов, счётчик дня, бирюзовые окна) — по окну
  выхода с Deploy/25 (`Grid`, §3.4).
- Флаг `UseActualTime` → `MassmediaPricelist.GetTariffWindows` ([MassmediaPricelist.cs:129](../Client/Classes/MassmediaPricelist.cs))
  → `TariffWindowRetrieve @useActualTime`: меняет только час и минуту строки (`:139–140`, `:171–172`), неделя отбирается по
  `dayOriginal` (`:76–77`, `:105–106`) — по Р-2 это день окна. [tasks/tariff-window-actual-time.md](tasks/tariff-window-actual-time.md)
  описывает первую версию флага, его строки о шаблонах устарели.
- Если у двух окон одного дня совпадают время выхода и цена, они попадают в одну ячейку линейной сетки, последнее затирает
  первое ([TariffWindowGrid.cs:465](../Client/Controls/TariffWindowGrid.cs)). На ArtvisDev 390 таких пар (12.2025), все на
  тарифах «только для модулей» — в линейную сетку не попадают. По расписанию совпадений 0.

**Почему трафик и генерация по расписанию (Р-1).** Операции над строкой берут время из её подписи и ищут окна по
`windowDateOriginal`, то есть по ключу слота: «Отредактировать время выхода» — `TariffWindowMoveTime.sql:52`; «Изменить
продолжительность» — `TariffWindowChangeDuration.sql:52`, `TariffWindowChangeDurationInDay.sql:17–18`, `:30–31`; «Удалить...» —
`TariffWindowRetrieveByDate.sql:12` ([TrafficGrid.cs:654](../Client/Controls/TrafficGrid.cs)); «Удалить сгенерированные окна» —
`TariffWindowMassDelete.sql:27–28`; «Изменить цену» — `TariffWindowChangePrice.sql:34`. По расписанию пара (станция, время)
уникальна, по времени выхода — нет.

**Поиск окна при добавлении:**
- линейная — по `windowId` ячейки ([RollerIssuesGrid3.cs:201](../Client/Controls/RollerIssuesGrid3.cs) → `IssueIUD`), время не участвует;
- модульная — по дню окна ([RollerIssuesGrid3.cs:161](../Client/Controls/RollerIssuesGrid3.cs) → `ModuleIssueIUD`); пакетная — по дню;
- веер — `AddRangeIssues.sql:67–71`: окно, у которого `windowDateActual` в получасе слота; конфликт фирмы — по окну выхода (`:80`, `:119–123`).

**Генерация по шаблону — везде по времени выхода** (расхождение «Simple по факту, TimePeriod по оригиналу» снято 07.10.2026):

| Путь | Поиск окна |
|---|---|
| Simple (клик в режиме «Шаблон»), линейная | время шаблона — время выхода щёлкнутого окна ([CampaignForm.cs:587](../Client/Forms/CampaignForm.cs)); на каждый день `TariffWindowRetrieve @actualDate` (`:57–66`) — `TOP 1` окно, чей интервал по `windowDateActual` накрывает это время. Без фильтра по цене, тарифу, `isDisabled` |
| TimePeriod (`FrmTemplate2`) и Шаблон 3, линейная | `GetTariffWindows(..., useActualTime: true)` ([FrmGenerator.cs:370](../Client/Forms/FrmGenerator.cs), `:454`), с клиента 20448e0; отключённые окна не берутся |
| Модульная, пакетная | по дню шаблона ([FrmGenerator.cs:584](../Client/Forms/FrmGenerator.cs), `:592`) |
| Веер: Simple, TimePeriod, Шаблон 3 | слоты `TariffWindowWithRange`, постановка — `AddRangeIssues` |
| Удаление по шаблону, линейная | `IssuesByDate` за день (окно выхода, с Deploy/27), затем выпуски, у которых время окна выхода равно времени шаблона ([CampaignOnSingleMassmedia.cs:122–137](../Client/Classes/CampaignOnSingleMassmedia.cs)) |
| Удаление по шаблону, веер | `MasterIssueDelete.sql:43–46` — окно выхода, получас по `windowDateActual` |

### 2.3. Прочие экраны

| Что | Процедура | Как сейчас |
|---|---|---|
| Дерево журнала: дни; ролики дня (сущности 94, 95, 97, 136, 154, 215) | `IssuesDays`, `CampaignRollers @issueDate` | окно выхода (`IssuesDays.sql:23`, `CampaignRollers.sql:42`); день — модуля или пакета, иначе окна выхода (`IssuesDays.sql:26`), с Deploy/27 |
| Дерево журнала: выпуски дня (98, 216); «Добавленные выпуски» веера; клон акции | `IssuesByDate` | окно выхода (`:31`, `:59`), его `windowDateActual` (`:26`, `:54`), день — как в дереве (`:68`), с Deploy/27. `issueDateOriginal` (`:27`, `:55`) — слот окна выхода, его читает только клон ([ActionOnMassmedia.cs:571](../Client/Classes/ActionOnMassmedia.cs)) |
| «Удалить» у дня или ролика в дереве | `CampaignsIssueDelete` | окно выхода (`:37`), день — как в дереве (`:44`), с Deploy/27 |
| Содержимое модуля (155) и пакета (176) | `ModuleIssueContentRetrieve`, `PackModuleIssueContentRetrieve` | окно выхода (`:23`; `:22`) и его время (`:16`; `:12`), с Deploy/27. Веб, панель места вкладки «Модули»: окна выхода своего выпуска модуля — та же `ModuleIssueContentRetrieve` (`ModuleDaySource.cs:1677-1681`, `:1735-1740`); у дня без выпуска — окна тарифов модуля за день, время — фактическое (`TariffWindowRetrieve @useActualTime = 1`, `:1746-1757`) |
| «Дни кампании», «Изменить позиционирование» | `CampaignDaysTreePassport` | факт, так было и до перехода: окно выхода, `dayActual`, `windowDateActual` (`:21–27`, `:38–40`, `:54`); модули и пакеты — свой день (`:68`, `:110`). Комментарий «It must be original day» (`:14`) устарел. Из-за `INNER JOIN Tariff`/`Pricelist` (`:55–56`) выпуск в окне выхода без тарифа виден как день, но не под днём — 40 линейных выпусков 2024 г. (те же, что в Р-18) |
| «Переносы» кампании (206) | `CampaignIssuesTransfers` | история: обе оси рядом, `twA.windowDateActual` и `twO.windowDateOriginal` (`:15–16`). Отбор — время выхода ≠ расписанию окна постановки (`:19–21`), поэтому видны и переносы выпусков (1 207), и выпуски из сдвинутых окон (56 857). Вид журнала (Р-14) не решён; комментарий [Campaign.cs:313](../Client/Classes/Campaign.cs) с отбором не совпадает |
| «Клонированные выходы в эфир…» | `ActionIssues` | окно выхода (`:23`) и его время (`:16`, `:30`), с Deploy/27 |

Журналы акций, журнал удалённых выпусков, «Использование роликов», деньги и отчёты — §3.3 и §3.4: время у всех — время окна выхода.

---

## 3. Окно выпуска: жизненный цикл пары

### 3.1. Таблица

`Issue.originalWindowID` и `actualWindowID` (`Tables/Issue.sql:4–5`): `INT NOT NULL`, без default и CHECK; обе — FK на
`TariffWindow` без каскада (`:21–22`). При создании выпуска обе указывают на одно окно.

| Индекс | Ключ | INCLUDE |
|---|---|---|
| `UX_Issue_OriginalWindowID_campaignID_IssueID`, кластерный (`:44`) | `originalWindowID, campaignID, issueID` | — |
| `IX_Issue_OriginalWindowID_isConfirmed_rollerID` (`:76`) | `originalWindowID, isConfirmed, rollerID` | — |
| `IX_Issue_campaignID_originalWindowID` (`:81`) | `campaignID, originalWindowID` | `ratio, rollerID, tariffPrice` |
| `IX_Issue_ActualWindowID_isConfirmed_COVERING` (`:87`) | `actualWindowID, isConfirmed` | `campaignID, rollerID` |
| `IX_Issue_campaignID_covering` (`:93`) | `campaignID, originalWindowID` | `rollerID, issueID, moduleIssueID, packModuleIssueID, ratio, tariffPrice, actualWindowID` (окно выхода — с Deploy/32, для денег по кампании) |

- Ключ по окну выхода — только у одного индекса. Кластерный остался по окну постановки: запись `originalWindowID` двигает
  строку, курсор по `Issue` в такой процедуре должен быть `static` (`CampaignTransferDay.sql:98–101`).
- Нужны ли сейчас некластерные индексы с ключом `originalWindowID` — не проверено. Комментарий
  `TariffWindowWithAdvertTypeRetrieve.sql:13–31` дополнен (`:27–28` — переход на `actualWindowID` и
  `IX_Issue_ActualWindowID_isConfirmed_COVERING`), но `:20–22` и «ВАЖНО» (`:30–31`) по-прежнему называют критичным
  `IX_Issue_OriginalWindowID_isConfirmed_rollerID`.
- `FK_TransferLog_Issue` и `FK_Issue_Campaign` — `ON DELETE CASCADE` (`Tables/TransferLog.sql:10`, `Tables/Issue.sql:16`):
  удаление выпуска или кампании стирает историю переносов.
- Окно не удаляется, пока оно окно постановки или выхода хотя бы у одного выпуска (`TariffWindowIUD.sql:53–58`,
  `TariffWindowMassDelete.sql:20–28`) — висящих ссылок нет.

`TransferLog` — журнал переносов, в нём только время: `oldDate` — `windowDateActual` источника (`IssueTransfer.sql:115–116`),
`newDate` — время выхода приёмника от клиента. Пишут `IssueTransfer` (`:318–319`) и автоперенос при активации
(`ActionActivate.sql:575–577`); «Перенос дня» не пишет. Чистит `DeleteHistory` по параметру `DaysHistorySave`
(`f_SysParamsDateHistorySave`, по умолчанию 365 дней, на копии прода — 30; Р-6 не решён). На ArtvisDev — 217 строк при
1 207 перенесённых выпусках.

Позиции −15/−5/5 — «то же, что −20/−10/10, но при переносе»: цена как у основной позиции (`Functions/fn_GetIssuePrice.sql:19–21`),
флаги занятости окна их не учитывают (`IssueTransfer.sql:150–179`). Их ставит клиент, если позиция в приёмнике занята
(десктоп — [RollerIssue.cs:113](../Client/Classes/RollerIssue.cs), веб — [TrafficManagement.cs:401](../Client/Classes/TrafficManagement.cs)).

### 3.2. `IssueTransfer`

Вызывается по разу на выпуск, у каждого вызова своя транзакция (`iStoredProcedure.isTransactionRequired = 1`): пачка целиком
не откатывается.

| | |
|---|---|
| Меняет | `actualWindowID`, `positionId` (`:187–190`). Счётчики: −окно, где выпуск стоит (`:123–185`), +приёмник (`:192–254`). Даты кампании и акции по дням выхода (`:256–311`, Р-10, с Deploy/33): у линейных — первый и последний день окон выхода; у спонсорских — то же по окнам с прайс-листом вместе с программами (у окна без тарифа — тариф окна постановки, `:272–279`); модульные и пакетные не трогаются. Пишет `TransferLog` (`:318–319`). Переносит обвязку агитации у подтверждённого типа 6 (`:321–334`) |
| Не меняет | `originalWindowID`; `tariffPrice` (Р-3); `ratio`, скидки, `finalPrice`, `Action.modDate` (Р-10) |
| Проверяет | Станцию: приёмник той же станции, что окно, где выпуск стоит сейчас (`:23–34`, `TransferOtherMassmedia`, Р-12, с Deploy/27). В приёмнике нет второго 4/44, 5/55 или 7 по `actualWindowID` (`:36–58`, Р-12). Закрытый период `DisabledWindow` по `windowDateActual` приёмника — для всех (`:60–69`). По приёмнику (`:76–100`): дедлайн станции по `dayActual` (кроме админа и трафик-менеджера), `isDisabled`, типы 1/8/9 в штучное окно, позиции −20/−10/10, вместимость (если подтверждён и нет права «в минус») |
| **Не проверяет** | День: любой, в том числе через границу месяца (Р-11). «Прошлое»: `@rightToGoBack` получен (`:74`) и не используется. Прайс-лист и тариф приёмника, в том числе «только для модулей» и особое окно без тарифа (Р-18). Запрет позиции у агитации и промо. Склеенные окна. Дедлайн и закрытый период источника. `@massmediaID` и `@isConfirmed` не используются. Ролик или фирма уже в окне — только вопрос клиента |

Других `UPDATE` окон выпуска нет (поиск 11.10.2026):
- `IssueIUD UpdateItem` («Сделать первым/…») пишет `actualWindowID := @windowID` (`:376`). Без `@windowID` с Deploy/26 берёт
  текущее окно выхода (`:72–73`, `:332`) — перенос сохраняется. Любой другой `@windowID` процедура тоже примет: проверки
  пройдут по нему, сверки с текущим окном нет, `TransferLog` не пишется (Т-2 закрыт наполовину). Клиент передаёт окно выхода,
  перечитанное из базы ([Issue.cs:59–77](../Client/Classes/Issue.cs)).
- Автоперенос при активации (`ActionActivate.sql:518–521`) и «Перенос дня» (`CampaignTransferDay.sql:297`) ставят оба окна в
  новое (§3.5).
- Остальные писатели окна выпуска не трогают: замена ролика, смена позиции модулей и пакетов, деактивация, восстановление,
  «Разделить акцию», объединение акций.

### 3.3. Правило: какое окно где

Правило 07.10.2026; сделано скриптами Deploy/24–35 ([tasks/window-actual-switch.md](tasks/window-actual-switch.md) §5.1, §7).
День окна — `dayOriginal` или `dayActual`, они равны (Р-2; на ArtvisDev 0 различий из 2 011 204 окон).

| Ось | Окно, день | Где |
|---|---|---|
| Занятость окна (`timeInUse*`, `capacityInUse*`, флаги позиций) | фактическое у всех писателей, так было и до 07.10 | `IssueIUD.sql:226,447`, `IssueTransfer.sql:184,253`, `CampaignTransferDay.sql:267,362`, `ActionActivate.sql:516,573`, `job_DeleteEmptyActions.sql:58,96`. Сверка 11.10: подтверждённые счётчики у 1 831 из 1 831 окна перенесённых выпусков совпадают с пересчётом по факту; неподтверждённые расходятся примерно в 250 окнах независимо от оси (обычный дрейф, перенесённый неподтверждённый выпуск один). *Уточнение 19.10.2026* (`ArtvisDB/Scripts/window-counters-check.sql`, все окна 2024–2026): подтверждённые совпадают во всех окнах Artvis, ArtvisDev и Tumen; неподтверждённые завышены (только вверх) в 78 755 окнах Artvis — дни 07.07–28.10.2025 и с 12.01.2026, окна до ~марта 2026; новых почти нет (на ArtvisDev после 28.09 — одно окно, 13047668); на Tumen 1 600 окон 20.04–31.05.2026 |
| Эфир: сетка вещания, DJin, справка, медиаплан, обрамление агитации | фактическое, так было и до 07.10 | §2.1 |
| Экраны и проверки: сетка кампании, веер, дерево журнала, замена ролика, «Предметы рекламы», 4/44, 5/55, 7, позиция в акции | фактическое (Deploy/25–27, 34) | §3.4 |
| Деньги: даты кампании, распределение по месяцам, суммы за период (через них счёт, акт, договор, балансы, итоги медиаплана), «Разделить акцию» | день окна выхода (с Deploy/33) | `ActionRecalculate.sql:104–107`, `:149–152`; `GetIssuesPrice.sql:32–33`; `SetIssueRatio.sql:30–31`; `GetPriceByPeriod.sql:44–78`; `rpt_GenericBill.sql:71–73`; `MoveIssues2NewCampaign.sql:18–21`. После переноса трафиком даты пересчитывает сама `IssueTransfer` |
| Деньги модульных и пакетных | день модуля или пакета, как было | `GetPriceByPeriod.sql:112–197`, `rpt_GenericBill.sql:76–78`. У спонсорских деньги — по программам (`ProgramIssue`) |
| Наценки спонсора (бонусное время) | прайс-лист окна выхода; у окна без тарифа — окна постановки (с Deploy/33) | `ActionRecalculate.sql:147,153–163`, `ActionActivate.sql:159–170` |
| Цена выпуска `tariffPrice` | цена продажи, хранится в выпуске; перенос трафиком её не меняет (Р-3). На ArtvisDev у 138 из 158 перенесённых линейных с разной ценой окон — цена окна постановки | пересчёты — ниже |
| Права на удаление («прошлое», дедлайн), уведомление админам | день окна выхода (с Deploy/24); модули и пакеты — свой день (`ModuleIssueIUD`, `CampaignModuleIssueDelete`, `CampaignPackDayDelete`, `PackModuleIssueID`). Но «Удалить» ролика модульной кампании в дне (сущность 215 → `CampaignsIssueDelete`) проверяет и дочерние выпуски модуля по дню окна выхода | `IssueIUD.sql:87–106,313–318`, `CampaignsIssueDelete.sql:46–64,138–142`; только «прошлое» — `CampaignIUD.sql:23–27`, `ActionIUD.sql:21–26`; при постановке и замене — `hlp_IssueVerify.sql:182–183`, `RollerSubstitute.sql:189–196` |
| Журнал «Удаленные рекламные выпуски» | время окна выхода у всех 8 писателей (с Deploy/26); старые строки — по расписанию окна постановки (Р-15) | §3.4 |
| Журналы акций (отбор «День» и «Время рекламного выпуска», три уровня), «Использование роликов» | окно выхода, его день и время, у модульных тоже (время — с Deploy/24, окно — с Deploy/28) | §3.4 |
| Отчёты: журнал актов, бонусы, «Объём реализации» (сводный, по месяцам, в секундах), факторный анализ | день окна выхода (с Deploy/33); модули и пакеты в деньгах — свой день; секунды модулей — день выхода (решение 10.10: это эфир; расходятся 29 выпусков) | §3.4 |

**Пересчёты цены выпуска** — от окна выхода (Р-3 (2)):
- смена позиции («Сделать первым»): цена окна выхода и наценки его прайс-листа (`IssueIUD.sql:75–84,338,349,369–370`).
  Отката на окно постановки нет: в окне выхода без тарифа цена с позицией станет 0 (40 выпусков 2024 г., все в прошлом);
- замена ролика другой длины: по окну выхода, у окна без тарифа — цена и наценки окна постановки (`RollerSubstitute.sql:127–133`,
  `:218–223`, с Deploy/34). У спонсорских такая замена пишет цену 0 ([tasks/side-defects.md](tasks/side-defects.md) №1);
- автоперенос при активации и «Перенос дня» — по новому окну (`ActionActivate.sql:522–529`; `CampaignTransferDay.sql:283–298`, Р-9).

**Исключения** — исходные значения остаются только здесь:

| Исключение | Что берётся | Где |
|---|---|---|
| Ключ тарифного слота (Р-1) | `windowDateOriginal` — время по расписанию | строки трафик-сетки и сетки генерации и операции по строке (§2.2); поиск окна на новой дате при клоне и «Переносе дня» (`TariffWindowRetrieveByDate.sql:12`; `CampaignTransferDay.sql:107,147–170`: у линейных и спонсорских — слот окна выхода, у модулей и пакетов — окна постановки); отсев дублей при генерации (`sl_GenerateTariffWindowsDay.sql:17,68`) |
| День модуля или пакета | `ModuleIssue.issueDate` / `PackModuleIssue.issueDate` | деньги и права модульных и пакетных; день выпуска в дереве, «Удалить», замене ролика и «Переносе дня» — `coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)` (`IssuesDays.sql:26`, `IssuesByDate.sql:68`, `CampaignRollers.sql:56`, `CampaignsIssueDelete.sql:44`, `RollerSubstitute.sql:139`, `CampaignTransferDay.sql:131`). Уведомление админам при удалении модуля или пакета — по дню окна постановки (`CampaignModuleIssueDelete.sql:134–138`, `CampaignPackDayDelete.sql:135–139`): тот же день, кроме 446 пакетов «день +1» (О-8) |
| История | `originalWindowID`, `TransferLog` | §3.1; «Переносы» кампании (§2.3) |
| Запасной тариф (Р-3 (2)) | тариф и прайс-лист окна постановки, когда у окна выхода тарифа нет | цена и наценки при замене ролика (`RollerSubstitute.sql:127–133`), наценки спонсора (`ActionRecalculate.sql:153–163`, `ActionActivate.sql:163–170`), даты спонсорской кампании (`IssueTransfer.sql:272–279`), прайс-лист слота в «Переносе дня» (`CampaignTransferDay.sql:112`). В `IssueIUD UpdateItem` такого отката нет (пересчёты выше) |
| Без эффекта | любое окно | берут только станцию или вместимость: `ActionRollers.sql:37`, `ActionRollerStats.sql:64`, `GetAgenciesString.sql:21`, `massmediaList.sql:36`, `CampaignTransferDay.sql:62`, `PackModuleIssueID.sql:280`, `CampaignsForActJournalRetrieve.sql:179`. Попутно не переводить: кластерный индекс `Issue` — по `originalWindowID` |

### 3.4. Читатели по окну выпуска

Рабочая копия совпадает с ArtvisDev (`OBJECT_DEFINITION` 28 процедур, 11.10.2026). Окно — везде фактическое, если не сказано иное.

| Процедура / место | Строки | Комментарий |
|---|---|---|
| **Сетки и списки окна** | | |
| `Grid` — сетка кампании (Д `RollerIssuesGrid3`, В «Рекламные окна»): синие ячейки, номера роликов, счётчик дня, бирюзовые окна | `:63` (со станцией), `:88`, `:104`; день — `:55` | с Deploy/25. В выдаче обе колонки (О-5); клиент берёт `actualWindowID`, без неё — `originalWindowID` ([RollerIssuesGrid3.cs:307](../Client/Controls/RollerIssuesGrid3.cs), [WindowsSource.cs:232](../Client/Classes/WindowsSource.cs)) |
| `WindowIssuesRetrieve` — список под сеткой, Del, Ctrl+R, перетаскивание; панель трафика | `:34–36`, `:66–68` | так было и до перехода |
| `ModuleIssueRetrieve @windowID` — модули окна | `:42` | с Deploy/25 |
| `TariffWindowWithAdvertTypeRetrieve` — «Предметы рекламы» (Д и В); `ComboModuleFreeTimeRetrieve` — то же в комбо и веб-вкладке «Модули» | `:51`, `:72`; `:178`, `:190` (день — модуля, `:145`) | Deploy/25; Deploy/26 (номера строк — после Deploy/50) |
| `TariffWindowWithRange` — сетка веера | `:241`, `:307`, `:335`; получас — по `windowDateActual` | так было и до перехода |
| `RangeSlotIssues`, `MasterIssueDelete` — веер: состав получаса, Н/Д, Del, Ctrl+R, «Удалить дубли», «До пересечения», перетаскивание | `RangeSlotIssues.sql:58–60`, `MasterIssueDelete.sql:43–45` | с Deploy/27; `MasterIssueDelete` пропускает кампанию без выпуска (`:48–51`, ошибки 515 больше нет). Колонки для `#days` замены ролика (`RangeSlotIssues.sql:53–54`) — окно выхода под старыми именами (с Deploy/34) |
| `RangeSlotFirmConflict`, `AddRangeIssues` — конфликт фирмы и поиск окна в веере | `RangeSlotFirmConflict.sql:33`, `AddRangeIssues.sql:80`, `:119` | так было и до перехода |
| **Дерево журнала и операции над днём** | | |
| `IssuesDays`, `IssuesByDate`, `CampaignRollers`, `CampaignsIssueDelete`, `ModuleIssueContentRetrieve`, `PackModuleIssueContentRetrieve`, `ActionIssues` | §2.3 | с Deploy/27 |
| `CampaignDaysTreePassport` — «Дни кампании», «Изменить позиционирование» | `:21–27`, `:54` | так было и до перехода; 40 выпусков в окнах без тарифа скрыты (§2.3) |
| `CampaignTransferDay` — «Перенести» день | отбор `:119`, `:131`; слот на новой дате `:107`, `:147–170` | с Deploy/27 (Р-5, Р-9), §3.5 |
| **Проверки при постановке, переносе, активации** | | |
| `hlp_IssueVerify` — один 4/44, 5/55, 7 в окне | `:74`, `:89`, `:121` | с Deploy/26. Вместимость, позиции, «прошлое», дедлайн — по самому `@windowID` (`:179–194`) |
| `hlp_IssueVerify` — окна модуля и пакета за день (постановка и смена позиции) | модуль `:218`; пакет `:45`, `:244`, `:269` | по `dayOriginal` с Deploy/50 (было `dayActual` без индекса, модуль — `:215`, Д-21); по Р-2 эквивалентно (0 различий из 2 011 204 окон). Окна — слота модуля, а не выхода дочерних строк (Д-24 в `docs/action-forms.md` §10) |
| `IssueIUD` — «позиция занята в этой акции»; UpdateItem без `@windowID` | `:256`; `:73` | с Deploy/26 |
| `IssueTransfer` — станция, один 4/44, 5/55, 7 в приёмнике | `:26–30`, `:44–51` | с Deploy/27 (Р-12) |
| `ActionActivate` — проверки; «избегать окон с роликами фирмы»; бонус спонсора; кандидаты автопереноса | `:96`; `:311`; `:163–170` (у окна без тарифа — окно постановки); `:270`, `:284` (тот же день выхода) | `:311` — Deploy/26, `:163` — Deploy/33. Автоперенос — §3.5 |
| `TariffIUD` — «у тарифа есть немодульные выпуски» | `:224` | с Deploy/26 |
| `CreateActionMuteRollersAlerts` | `:24` (`dayActual`) | так было и до перехода |
| **Права и уведомления при удалении** | | |
| «Прошлое» и «дедлайн»: `IssueIUD:87–106`, `CampaignsIssueDelete:46–64`; только «прошлое» — `CampaignIUD:23–27`, `ActionIUD:21–26` | день окна выхода | с Deploy/24. Модули и пакеты — свой день (`ModuleIssueIUD:290–302`, `CampaignModuleIssueDelete:47–60`, `CampaignPackDayDelete:40–57`, `PackModuleIssueID:267–286`) |
| Уведомление админам «удалены выпуски, до выхода которых ≤ N дней» | линейные — `IssueIUD:313`, `CampaignsIssueDelete:141`, `CampaignIUD:142`, `ActionIUD:104 (до деактивации — Deploy/58)`; модули и пакеты — **окно постановки** (`CampaignModuleIssueDelete:137`, `CampaignPackDayDelete:138`); `ModuleIssueIUD:309` — день модуля, `PackModuleIssueID:293` — день пакета | линейные — с Deploy/24; модульные не трогались (§3.3, исключение «день модуля») |
| **Журналы** | | |
| `Actions1`, `FirmWithActions1`, `HeadCompaniesWithActions` — отбор журналов акций по дню и времени выпуска | окно `:127`, `:65`, `:108`; время `:134`, `:72`, `:113`; день `:135`, `:73`, `:114` | время — Deploy/24, окно — Deploy/28. У модульных тоже день окна выхода (решение 10.10) |
| `stat_RollerStatistic`, `ActionsForRollerStatistic` — «Использование роликов» | `:59`, `:35`; день `:78`, `:47` | с Deploy/28 |
| Журнал удалённых — 8 писателей `LogDeletedIssue`: `IssueIUD:310`, `CampaignsIssueDelete:131–135`, `CampaignIUD:134–137`, `ActionIUD:91–96 (до деактивации — Deploy/58)`, `CampaignModuleIssueDelete:127–131`, `CampaignPackDayDelete:128–132`, `ModuleIssueIUD:306–307`, `PackModuleIssueID:290–291` | `windowDateActual` окна выхода | с Deploy/26 |
| **Деньги** (Deploy/33, индекс — Deploy/32) | | |
| `ActionRecalculate` — даты кампании; наценки спонсора | линейные `:104–107`, спонсорские `:149–152`; наценки `:153–163` | модули и пакеты — свой день |
| `IssueTransfer` — даты после переноса | `:256–311` (расчёт `:266–292`, запись `:294–311`) | Р-10: только даты |
| `GetIssuesPrice:32–33`, `SetIssueRatio:30–31`, `GetPriceByPeriod:51`, `:66` (НДС `:71`), `rpt_GenericBill:71–73`, `MoveIssues2NewCampaign:18–21` | день окна выхода | через `GetPriceByPeriod` — счёт, акт, договор, итоги медиаплана |
| `CampaignsForActJournalRetrieve` — журнал актов | `:219–221` | модульные — день модуля; пакетная ветка (`:179`) берёт из окна постановки только станцию и `maxCapacity`, отбор — по дню пакета (`:188`) |
| `stat_GetPrice_proc:127`, `stat_GetPriceByMonth_proc:132`, `Stat_VolumeOfRealizationSec:77`, `statFactorAnalysis:130`, `:362`, `stat_Bonuses:266` | день окна выхода | у модулей и пакетов в деньгах — свой день; в секундах — день выхода |
| **Замена ролика** (Deploy/34; клиент и веб не менялись) | | |
| `RollerSubstitutionPassport` — дни и окна паспорта | `:54`, `:80`; день — модуля, пакета или окна выхода (`:58`) | id узла окна — `dayOriginal` + `windowDateOriginal` окна выхода (`:72`): ключ слота без дублей, его разбирает `RollerChangeAdvertTypeForm`; не переименовывать |
| `RollerSubstitute` | курсор `:113`, `:127`, `:139–140`; для одного выпуска `#days` — из окна выхода (`:37–43`); 4/5/7 — `:167` | цена — §3.3; «прошлое» и дедлайн — по дню выхода (`:189–197`); модули и пакеты — свой день (`:252`, `:291`). «Незамененные» — `windowDateActual` под именем `windowDateOriginal` (`:363–365`; читают атрибут сущности 212 и [ObjectActions.cs:1297](../FogSoft.Web/Infrastructure/ObjectActions.cs)) |
| **История и без эффекта** | | |
| `CampaignIssuesTransfers`, `TransferLog` | оба окна | §2.3, §3.1 |
| `GetAgenciesString:21`, `ActionRollers:37`, `ActionRollerStats:64`, `massmediaList:36`, `PackModuleIssueID:280`, `CampaignTransferDay:62` | окно постановки | только станция: она у обоих окон одна (Р-12); `ActionRollers` окно вообще не использует (только в условии неиспользуемого LEFT JOIN, `:43`) |
| `GetUniqueMMsForAction`, `GetUniqueMMsForPackModuleCampaign` | по `@isFact`; все вызовы передают 1 | ветка 0 мёртвая (этап 9) |

### 3.5. Что происходит с перенесённым выпуском

«Оба := новое» — `originalWindowID = actualWindowID`, признак переноса снят (из «Переносов» выпуск уходит, если новое окно
не сдвинуто по времени). Счётчики занятости и журнал удалённых у всех операций — по окну выхода; права на удаление — по дню
окна выхода, у модулей и пакетов — по их дню.

| Операция | Пара | Счётчики | Цена (`tariffPrice`) | Права, журналы |
|---|---|---|---|---|
| `IssueIUD DeleteItem` (через неё же Del в веере, перетаскивание, удаление при активации) | удаляется; `TransferLog` — каскадом | −окно выхода (`:163–228`) | — | «прошлое» и дедлайн станции — по дню выхода (`:86–106`); журнал удалённых — время выхода (`:309–311`); уведомление админам (`:313–318`); обвязка агитации снимается в окне выхода (`:323–327`) |
| `IssueIUD UpdateItem`: «Сделать первым / вторым / последним», «Убрать порядок», массовая смена позиции | сохраняется: без `@windowID` — окно выхода (`:72–73`, с Deploy/26; раньше выпуск молча возвращался в окно постановки). Пишется только `actualWindowID` (`:376`) | −/+ окно выхода (`:163–228`, `:384–449`) | при смене позиции — от окна выхода (`:75–84`, `:338`, `:369–370`); в окне без тарифа — 0 | проверки — по окну выхода (`hlp_IssueVerify.sql:179–194`) |
| `RollerSubstitute`: у выпуска, у ролика кампании, во всей акции; Ctrl+R в линейной сетке и веере | сохраняется: пишутся `rollerID` и цена (`:327–336`). Ключ — окно выхода и его день, у модулей и пакетов — их день (`:36–43`, `:127`, `:139–140`; с Deploy/34) | ±разница длительностей на окне выхода (`:338–360`) | при другой длине — §3.3; при той же — не меняется (`:323–330`) | 4/44, 5/55, 7 — в окне выхода; «прошлое» и дедлайн — по дню выхода |
| `ModuleIssueIUD`, `PackModuleIssueID` | сохраняется; смена позиции пишет позицию всем дочерним без проверки их окон выхода (`ModuleIssueIUD.sql:364`, `PackModuleIssueID.sql:347`; Р-16 не решён) | −/+ окна выхода дочерних | у дочерних 0; цена модуля или пакета — по их прайс-листу | при удалении права и уведомление — по дню модуля или пакета; журнал удалённых — время окна выхода каждого дочернего (с Deploy/26) |
| `ActionDeactivate`, `ActionRestore` | сохраняется | на окне выхода (`ActionDeactivate.sql:35–94`, `ActionRestore.sql:9–44`) | — | обвязка агитации снимается в окне выхода |
| `ActionActivate` | сохраняется у прошедших проверки (по окну выхода, `:68–96`). Автоперенос не прошедших — только линейные вне модулей (`:244–250`), та же станция и день выхода, ближайшее время (`:259–285`): оба := новое (`:518–521`), Р-4 «нет» | −/+ (`:481–516`, `:538–573`); подтверждение — на окне выхода | при автопереносе — от нового окна (`:522–534`) | `TransferLog` пишется (`:575–577`); не нашедшие окна удаляются через `IssueIUD`; в конце `ActionRecalculate` |
| `MoveIssues2NewCampaign` («Разделить акцию»), `MergeActions` | сохраняется, меняется `campaignID`; деление по периоду — по дню выхода (`:11–21`, с Deploy/33) | — | не меняется; затем пересчёт акций | — |
| `CampaignTransferDay` («Перенести» день, Д и В) | **сбрасывается**: оба := новое окно (`:297`, Р-5, с Deploy/27). Отбор — по дню выхода, у модулей и пакетов — по их дню (`:131`); окно на новой дате — слот (§3.3), ищется заново для каждого выпуска (`:142`); нет окна — `TransferError`, день не переносится (`:365–370`) | −окно выхода / +новое | у линейных — от нового окна (`:283–298`, Р-9); у спонсорских, модульных, пакетных не меняется | `TransferLog` не пишется; проверки постановки для нового окна (`:177–204`); затем пересчёт акции. Выпуск, перенесённый трафиком в окно без тарифа, так обычно не перенести — слот не находится |
| Перетаскивание: линейная сетка ([CampaignForm.cs:1578](../Client/Forms/CampaignForm.cs)), веер ([EditIssuesForm.cs:1673](../Client/Forms/CreateActionMaster/EditIssuesForm.cs)), веб ([WindowsSource.cs:790](../Client/Classes/WindowsSource.cs)) | **теряется**: удаление и новый выпуск (оба := приёмник) в одной транзакции с пересчётом; `TransferLog` уходит каскадом | как при удалении и постановке | окна-приёмника | как при удалении |
| Клонирование акции ([ActionOnMassmedia.cs:564–590](../Client/Classes/ActionOnMassmedia.cs)) | не переносится: клон встаёт в слот, где выпуск **выходил** (`IssuesByDate.issueDateOriginal` + сдвиг → `TariffWindowRetrieveByDate`); модули и пакеты — их день + сдвиг | у клона | нового окна | — |
| Удаление окна: `TariffWindowIUD`, `TariffWindowMassDelete` | запрещено, пока окно — окно выхода или постановки хоть у одного выпуска: поштучное отказывает (`TariffWindowIUD.sql:53–59`), массовое молча пропускает (`TariffWindowMassDelete.sql:20–26`) | — | — | `job_DeleteHistory` — по тому же правилу, но мёртвая (О-6, этап 9) |
| `TariffWindowIUD UpdateItem`, `TariffWindowMoveTime`, `TariffWindowChangePrice` | не трогается: выпуски едут с окном; день окна менять нельзя (с Deploy/31) | — | не пересчитывается | — |
| `IssueTransfer` повторно | меняются окно выхода и позиция, окно постановки — первое; возврат в окно постановки сводит пару | −старое окно выхода / +новое | не меняется (Р-3) | новая строка `TransferLog`; даты кампании и акции — по дням выхода (Р-10) |

Удаление подтверждённой акции не писало журнал удалённых выпусков ([tasks/side-defects.md](tasks/side-defects.md) №2) — исправлено Deploy/58 (19.10.2026, не на проде).
Макет может быть перенесён трафиком ещё до активации (выпуск 41793772 на ArtvisDev); если он проходит проверки, пара сохраняется.

---

## 4. Несогласованности

### 4.1. Пункты исследования 07.10.2026

Из 12 пунктов устранены 1–3, 5, 7, 8, 10–12; у 4 и 6 есть остаток; 9 оставлен по решению.

| № | Было 07.10 | Сейчас |
|---|---|---|
| 1 | Линейная сетка: отметки — по окну постановки (`Grid`), список, Del, Ctrl+R — по окну выхода | **Устранено** (Deploy/25 + клиент): `Grid.sql:63,88,104` |
| 2 | Веер: подсветка — по факту, состав, Н/Д, Del, Ctrl+R — по окну постановки | **Устранено** (Deploy/27, 34): `RangeSlotIssues.sql:58–60`, `MasterIssueDelete.sql:43–45`, `IssuesByDate.sql:59` |
| 3 | Линейная: строка по факту, колонка по расписанию — безопасно, пока окна не уходят через полночь | **Безопасно всегда** (Р-2, Deploy/31): на ArtvisDev 0 окон с другим днём из 2 011 204 |
| 4 | Модульная — время выхода не видно; два списка под сеткой с разными правилами | Списки **устранены** (Deploy/25, `ModuleIssueRetrieve.sql:42`). Приписки времени выхода нет — **вопрос заказчику** (§4.2) |
| 5 | Генерация: Simple — по факту, TimePeriod и Шаблон 3 — по расписанию | **Устранено** клиентом 20448e0: [FrmGenerator.cs:370](../Client/Forms/FrmGenerator.cs), `:454` |
| 6 | Счётчик дня в сетке и «Дни кампании» расходятся у перенесённых на другой день | **Устранено** (Deploy/25, `Grid.sql:55`). Остаток — модульные, перенесённые на другой день (§4.2) |
| 7 | 4/44, 5/55, 7: уникальность — по окну постановки, перенос её не проверяет | **Устранено**: постановка и замена — Deploy/26 (`hlp_IssueVerify.sql:74,89,121`, `RollerSubstitute.sql:167`), перенос — Deploy/27 (`IssueTransfer.sql:44–58`). На ArtvisDev окон с двумя роликами одной группы — 0 |
| 8 | Журнал акций по дню и времени выхода находит по времени продажи | **Устранено** (Deploy/24 — время, Deploy/28 — окно) |
| 9 | «Переносы» кампании отбирают по времени окон, а не по паре окон | **Оставлено по решению** (история и переноса выпуска, и сдвига окна; карта §2.2, класс «И»). Р-14 не решён, комментарий [Campaign.cs:313](../Client/Classes/Campaign.cs) не поправлен |
| 10 | «Предметы рекламы» — по окну постановки | **Устранено** (Deploy/25, 26) |
| 11 | `RollerSubstitute`: перебор по факту, тип и цена — по окну постановки | **Устранено** (Deploy/26 — тип, Deploy/34 — ключ и цена) |
| 12 | Счёт по месяцам и акты — по дню постановки, справка и медиаплан — по дню выхода | **Устранено** (Deploy/33). Перенос через границу месяца не запрещён (Р-11): деньги идут за днём выхода |

### 4.2. Что осталось

**Отображение времени окна — вопросы заказчику** ([window-original-actual-customer.md](window-original-actual-customer.md) §10),
этап 7 сделал только ошибки:
- название окна: главное — расписание, время выхода в скобках (§1);
- итог «Перенесено окон: N, не перенесено: M»: окно — по расписанию, сосед — по расписанию с временем выхода в скобках, если
  оно другое (`TariffWindowMoveTime.sql:130–139`, [TrafficManagement.cs:260](../Client/Classes/TrafficManagement.cs), `:279–283`);
- приписки времени выхода нет в модульной сетке и веб-генерации (§2.2);
- подписи паспорта окна «Время выхода:» / «Время выхода реальное:» (Р-13).

**Старые имена с новым смыслом** — этап 9, отложен (список — в начале документа).

**День модуля или пакета против дня выхода** (так решено: журнал карты §7; по пакетам — О-8, open-questions §9):
- 29 модульных выпусков, перенесённых трафиком на другой день (все в прошлом). По дню модуля — дерево журнала, «Дни кампании»,
  «Перенос дня», замена ролика, деньги; по дню выхода — эфир, журналы акций, «Использование роликов», «Объём реализации
  (в секундах)», счётчик дня в сетке (`Grid.sql:55`).
- 446 дочерних выпусков пакетов с днём окна ≠ дню пакета (О-8: станция 135, не переносы): 445 — «день +1»
  (11.01.2024–31.12.2025), 1 — день −2 (выпуск 38330369). Дерево и «Перенос дня» — день пакета, журналы и эфир — день окна.
  Принято, данные не правятся.

**Выпуск без отметки в сетке кампании** (Р-18, решения нет). `IssueTransfer` пускает в окно, которого нет в сетке кампании
(особое окно без тарифа или тариф «только для модулей»): отметки нет нигде, счётчик дня выпуск считает (`Grid.sql:63` без
соединения с `Tariff`). На ArtvisDev 44: 40 — кампания 356647, 25.10–03.11.2024 (их же скрывают «Дни кампании», §2.3);
4 — тарифы «только для модулей». В модульной сетке из 688 перенесённых модульных выпусков отметка в окне модуля пропала у 675
(Р-8а, только сообщить; все в прошлом).

**Два окна в одну эфирную минуту** — §2.1: создание с Deploy/35 отказывает, перенос времени и паспорт — нет. На ArtvisDev
390 таких минут, все в прошлом.

**Исключения правила, не несогласованности:** строки трафика и генерации по расписанию (Р-1); перенос трафиком не меняет
цену (Р-3); автоперенос (Р-4) и «Перенос дня» (Р-5) пишут оба окна — пометка о переносе пропадает, `TransferLog` есть только
у автопереноса.

---

## 5. Дефекты

| № | Где и что (07.10) | Статус 11.10.2026 |
|---|---|---|
| 1 | `CampaignTransferDay`: `@windowId` не сбрасывается в цикле, выпуск молча встаёт в окно предыдущего | **Исправлено** (Deploy/27, Т-5): сброс `:142`; ненайденное окно — `TransferError` на весь день (`:365–369`), транзакция откатывается (`isTransactionRequired = 1`) |
| 2 | Права «прошлое» и дедлайн при удалении — по дню окна постановки | **Исправлено** (Deploy/24) |
| 3 | `IssueIUD` UpdateItem: при смене позиции цена — от окна выхода | **Теперь это правило** (Р-3 (2)). Остаток: нет отката на окно постановки для окна выхода без тарифа — цена 0 (§3.3) |
| 4 | `job_DeleteHistory.sql:94`: `DROP INDEX` несуществующего индекса (на ArtvisDev нет индексов из `:94`, `:95`, `:96`, `:98`) | **Не менялось.** Процедура мёртвая; удалить после проверки планировщика на сервере ([tasks/window-actual-stage9.md](tasks/window-actual-stage9.md) §3.6) |

Найдено 07.10 и перепроверено 11.10:
- `CampaignTransferDay` не пересчитывал `tariffPrice` — **исправлено** (Deploy/27, Р-9).
- `job_DeleteEmptyActions` (Protector, «Пересчитать остатки») — **исправлено 21.10.2026, Deploy/65** (О-7): было — неподтверждённые
  пересчитывались без учёта `Action.deleteDate` (после `ActionRestore` счёт двойной), подтверждённые — через INNER JOIN, опустевшее
  окно не обнулялось. Теперь одна выборка по окну выхода без удалённых акций и `LEFT JOIN` по всем окнам — то же правило, что
  `ArtvisDB/Scripts/window-counters-check.sql`.
- `TariffWindowMoveTime` пишет `windowDateActual`, но не `dayActual` (`:121–124`) — **не проявляется**: новое время строится от
  `dayOriginal`, ветка `@needaddday` (`:26–36`) мертва при начале эфирного дня 00:00. Если оживёт — окно уедет на день +1 в обход
  `TariffWindowDayChange` (запрет есть только в `TariffWindowIUD`; stage9 §4).

Ещё по коду, решений нет:
- `IssueIUD` UpdateItem принимает любой `@windowID` и пишет его в `actualWindowID` (`:376`) без проверок `IssueTransfer` и без
  `TransferLog` (Т-2, вторая половина). Вызывающий передаёт текущее окно выхода ([Issue.cs:71](../Client/Classes/Issue.cs)).
- Смена позиции модуля ставит позицию всем дочерним, в том числе перенесённым в чужие окна, без проверки там занятости
  (`ModuleIssueIUD.sql:363–364`, Р-16). Такие выпуски сейчас все в прошлом.

Открытые попутные дефекты, связанные с окнами, — [tasks/side-defects.md](tasks/side-defects.md): №1 (замена ролика другой
длины обнуляет цену спонсорского выпуска; деньги не задеты, разбор 19.10.2026) и раздел «Найдено попутно» (завышенные
неподтверждённые счётчики окон — скрипт `ArtvisDB/Scripts/window-counters-check.sql`, ремонт и О-7 ждут решения; апостроф в уведомлении о разрыве объединения, «Добавить» в последний день
прайс-листа, пустой месяц пакетов в `GetMonthes`, цена 0 при «Сделать первым» в окне без тарифа, «Дни кампании» без
выпусков в окнах без тарифа). Исправлены 19.10.2026 (не на проде): №2 — Deploy/58, №4 — Deploy/59, №6 — Deploy/61, №8 и «Продлить окно» с
дня самого окна — новая сборка qd2.

---

## 6. Что учесть при изменениях

**Чтение.** Новое чтение — где и когда выходит выпуск, сколько стоит, кто вправе удалить, что попадает в журнал или отчёт —
по `Issue.actualWindowID` → `TariffWindow.windowDateActual`, день — день окна выхода. `dayOriginal = dayActual` (Р-2), поле
дня выбирать по индексу (`UX_TariffWindow_dayOriginal_windowID`, `IX_TariffWindow_TariffID_DayOriginal`). Прежнее правило
«деньги — по `originalWindowID` + `dayOriginal`» отменено Deploy/33.

**Исключения — только четыре** (§3.3): ключ тарифного слота, день модуля или пакета, история и запасной тариф окна
постановки для окна выхода без тарифа. Если новое место не подходит ни под одно — оно по окну выхода. Колонки со старыми именами (начало документа) читать по смыслу.

**Запись выпусков.**
- Новый писатель при создании ставит оба окна в одно (как `IssueIUD.sql:276–277`) и ведёт счётчики занятости на окне выхода.
- Новая постановка взамен выпуска (как «Перенос дня» или автоперенос) ставит оба окна в новое, цена — по новому окну.
- Развести пару может только `IssueTransfer`. Новый путь переноса обязан повторить её проверки (§3.2), пересчитать даты
  кампании и акции (`:256–311`), записать `TransferLog` и не трогать цену (Р-3).
- Пересчёт цены — от окна выхода; у окна без тарифа — цена и наценки окна постановки, как в `RollerSubstitute.sql:127–133`.
- `IssueIUD UpdateItem` вызывать только с явным `@windowID` = текущее окно выхода, перечитанное из базы
  ([Issue.cs:59–77](../Client/Classes/Issue.cs)): процедура примет любое окно и молча переставит выпуск без `TransferLog`.
- Если писатель меняет `originalWindowID`, курсор по `Issue` — `static` (кластерный индекс, §3.1).

**Окна.**
- День окна менять нельзя: `TariffWindowIUD` отказывает `TariffWindowDayChange` (Р-2). CHECK на таблице нет — новый писатель
  `TariffWindow` сам держит `dayOriginal = dayActual`. Окно после полуночи — особое окно следующего дня.
- «Создать новое окно…», шаблон и «Продлить окно» отказывают при занятой эфирной минуте или слоте (Deploy/35).
- Отображение не менять до ответов заказчика ([window-original-actual-customer.md](window-original-actual-customer.md) §10).

**Деплой.** Одна правка процедуры за раз, у каждой свой замер «до/после» ([tasks/window-actual-switch.md](tasks/window-actual-switch.md)
§5.5). Правку строить на текущем тексте процедуры из рабочей копии. Перед накатом сверять пометки «не перезапускать после …»
в [Deploy/README.md](../Deploy/README.md): скрипты 07, 13, 14, 18, 24, 26, 27, 29, 31 и часть `ArtvisDB/Scripts` несут старые
полные тексты процедур и при повторном запуске откатывают этапы. Новый ключ сообщения — по [business-errors.md](business-errors.md).

**Связанные документы:** [tasks/window-actual-switch.md](tasks/window-actual-switch.md) (карта, этапы, журнал решений),
[tasks/window-actual-open-questions.md](tasks/window-actual-open-questions.md) (решения Р-*, итоги по шагам §10),
[tasks/window-actual-stage9.md](tasks/window-actual-stage9.md) (чистка), [window-original-actual-customer.md](window-original-actual-customer.md)
(для заказчика), [tasks/side-defects.md](tasks/side-defects.md), [tariffgrid.md](tariffgrid.md) (сетки),
[window-merging.md](window-merging.md) (склейки), [mass-window-operations.md](mass-window-operations.md) (Del / Ctrl+R),
[mediaplan.md](mediaplan.md), [broadcast-start.md](broadcast-start.md).
