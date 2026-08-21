# Конвенция: вынос диалогов из доменных классов

## Статус

Рабочая конвенция для этапа 0, п. 4 плана `docs/tasks/web-migration.md`.
Зафиксирована на разборе представительных случаев; эталон реализован и
собирается (см. раздел 5). Дальше применяется к остальным местам партиями.

Ветка: `refactor/web-migration-stage0`.

## 1. Зачем

В `Client/Classes` **56 вызовов `ShowDialog` и 55 обращений к `DialogResult`
находятся внутри доменных классов** — 30 файлов. Доменный метод сам открывает
форму, блокируется на ней и по результату продолжает работу.

В браузере так нельзя: модальное окно не может блокировать серверный метод.
Поэтому каждое такое место разрезается на две половины — «что спросить у
пользователя» и «что сделать с ответом». Вторая половина не знает про UI и
одинаково работает и в десктопе, и в вебе.

Это **не переписывание бизнес-логики**: последовательность вызовов процедур,
пересчёты и тексты сообщений сохраняются один в один.

## 2. Куда что кладётся

| Что | Куда |
|---|---|
| Бизнес-половина (проверки, работа с данными, вызовы процедур) | остаётся в исходном файле, например `Client/Classes/Action.cs` |
| UI-половина (создание формы, `ShowDialog`, `DialogResult`, `UserMessage.*`, `Cursor`, `Application.DoEvents`) | новый файл `<Класс>.WinForms.cs` рядом |

Класс становится `partial`. Публичные и приватные имена, которые вызывают
снаружи, **сохраняются за UI-половиной** — тогда места вызова не меняются.

`Client/Client.csproj` — старого формата, новые файлы нужно **добавлять в него
вручную**, автоподхвата нет.

## 3. Пять форм, которые встречаются

### Форма 1. Ввод параметра

Диалог возвращает значение, которое дальше используется в вычислении.
Пример: `Client/Classes/Action.cs:383` — `FrmDateSelector` для периода
медиаплана, `Action.GetSelectedMonths()` — `FrmMonths`.

**Разрез:** бизнес-метод принимает значение аргументом; диалог остаётся в
UI-половине и вызывает бизнес-метод.

```csharp
// было: метод сам спрашивал период
// стало:
// ядро:  void ShowMediaPlanForPeriod(DateTime start, DateTime finish, bool selectively)
// UI:    открывает FrmDateSelector и вызывает ShowMediaPlanForPeriod(...)
```

### Форма 2. Выбор объектов и операция над ними

Самая частая и самая важная. Диалог возвращает набор выбранных объектов,
дальше идёт транзакционная операция с пересчётом.
Пример: `ActionOnMassmedia.SplitAction`, `SplitCampaign`, `MergeAction`,
`Campaign.SelectDays`, платежи.

**Разрез на три части:**

1. `GetXxxCandidates(out string messageKey)` — что показать в диалоге;
2. `ApplyXxx(<выбор>)` — что сделать с выбором (это и есть бизнес-логика);
3. UI-метод прежнего имени — показывает диалог между ними.

### Форма 3. Переход на другой экран

`ShowDialog` без разбора результата за пределами возврата `bool`/`void`:
открыли карточку/редактор и всё, вызывающий код после диалога ничего не
проверяет. Пример: `ActionOnMassmedia.ShowPassport` (открывает `ActionForm`),
`PackModulePricelist.EditContent`. Сюда же относятся переопределения
`ShowPassport` (`Agency.cs:207`, `Roller.cs:101`, `SponsorTariff.cs:51`) —
базовый `PresentationObject.ShowPassport` уже вынесен в UI-половину на
этапе 0.1. Все пять перенесены партией 1 (коммит после эталона).

**Разрез не нужен.** Метод целиком переезжает в UI-половину. В вебе это
навигация, а не диалог.

**Проверять по полному телу метода, не по строке с `ShowDialog`.**
`Campaign.cs:486` (`EditRollerIssues`), `Campaign.cs:499`
(`EditProgramIssues`), `ProgramPartOfSponsorCampaign.cs:89`,
`RollerPartOfSponsorCampaign.cs:62` изначально были размечены сюда по
однострочному grep — ошибочно: после `ShowDialog` там проверяется
`campaign.ChangeFlag` и вызывается `Refresh()`/`Recalculate()`, то есть
результат диалога **используется**. Это форма 3.1, см. ниже.

### Форма 3.1. Модальная редактирующая сессия

Открывает не диалог с результатом-выбором, а целую форму
редактирования (`CampaignForm`), которая сама делает все нужные вызовы
процедур во время своей модальной сессии. После закрытия вызывающий код
не получает данные из формы — он читает флаг `ChangeFlag` и по нему решает,
обновлять ли себя.

Пример:

```csharp
protected void EditRollerIssues(IWin32Window owner, TariffGrid tariffGrid)
{
    CampaignForm campaign = new CampaignForm(this, tariffGrid);
    campaign.ShowDialog(owner);
    Application.DoEvents();
    if (campaign.ChangeFlag)
    {
        Refresh();
        FireContainerRefreshed();
    }
}
```

Места: `Campaign.cs:486` (`EditRollerIssues`), `Campaign.cs:499`
(`EditProgramIssues`), `ProgramPartOfSponsorCampaign.cs:89`
(`EditProgramIssues`), `RollerPartOfSponsorCampaign.cs:62`
(`EditRollerIssues`).

**Разрез невозможен на этом этапе и не нужен.** Сигнатура сама принимает
UI-тип (`TariffGrid : UserControl`, `Client/Controls/TariffGrid.cs`) —
разделить такой метод на «ядро без UI» и «UI-обвязку» нельзя, не
переделав, чем параметризуется `CampaignForm`. Это не задача этапа 0:
`CampaignForm` — самый большой и сложный экран (`docs/tasks/web-migration.md`,
раздел 4.3, этап 3 п. 1). Метод переезжает **целиком, без изменений**, в
`<Класс>.WinForms.cs`, как форма 3, но не разбирается на составные части.
Когда до `CampaignForm` дойдёт очередь на этапе 3, эти четыре места
пересматриваются вместе с ней, не раньше.

### Форма 4. Вопрос «да/нет» и подтверждение правами

Уже решена на этапе 0.1: `FogSoft.WinForm/Classes/UserInteraction.cs`.
Домен спрашивает `UserInteraction.Confirm(text)`, обработчик подставляется
при старте приложения.

Особый случай — `Client/Classes/Utils.cs:36` `AskConfirmation`: `FrmConfirmation`
запрашивает вход администратора, чтобы авторизовать скидку, и возвращает
`(User, ManagerDiscountReasonId)`. **Решено не переносить в веб** (согласовано
с владельцем продукта 2026-08-21) — функцией не пользуются даже в десктопе.
Единственный живой вызов — `ManagerDiscountForm.cs:132`
(`CampaignForm.cs:1438` — второй вызов, но он внутри закомментированного
блока, мёртвый код, самостоятельного значения не имеет). Код не трогаем и не
разрезаем: разрез нужен только для будущего переиспользования в вебе, а тут
переиспользования не будет.

### Форма 4.1. Уведомление о результате (не вопрос)

Симметрично форме 4, но без ответа: ядро должно сказать пользователю
«готово, вот что изменилось», не дожидаясь решения.

Обнаружено на партии 2 (`CampaignPart.RecalculateAndShowPriceChange`):
метод вызывается из **8 мест в 6 файлах** (`Campaign`, `CampaignDay`,
`CampaignRoller`, `Issue`, `PackModuleIssue`, трижды сам `CampaignPart`) и
показывал сообщение о смене цены напрямую через `Globals.ShowCompleted`
(WinForms). Перенос такого метода в UI-половину потянул бы UI-зависимость
во все 8 вызывающих мест — большинство из них core-методы, которые иначе
были бы чистыми.

**Решение:** `UserInteraction` (этап 0.1) расширен методом `Notify(messageKey, parameters)`,
симметричным `Confirm`. Обработчик подставляется в `Launcher.Main` рядом с
`Confirm`. Ядро зовёт `UserInteraction.Notify(...)` вместо `Globals.ShowCompleted(...)`
— метод остаётся в ядре целиком, ни один вызывающий код не меняется.

**Когда применять:** если UI-обращение внутри метода — это ТОЛЬКО показ
сообщения (не выбор, не подтверждение, не открытие формы) и сам метод
вызывается из нескольких core-мест по имени (через наследование или
напрямую) — не переносить метод целиком, а завести/переиспользовать
`UserInteraction.Notify`. Если UI-обращение — это диалог с вводом или
выбором, это форма 1/2, не 4.1.

### Форма 5. Проверка с сообщением об ошибке

```csharp
if (dt.Rows.Count < 2)
{
    UserMessage.ShowInformation(MessageAccessor.GetMessage("CanNotSplitAction"));
    return;
}
```

**Разрез:** ядро возвращает признак и **ключ сообщения**, UI показывает.

```csharp
internal bool CanXxx(out string messageKey)
```

Ключ, а не текст: тексты живут в `MessageAccessor`/`Resources`, и в вебе они
берутся оттуда же. Никаких литералов в ядре.

## 4. Правила, которые нельзя нарушать

1. **Транзакция не пересекает диалог.** `BeginTransaction` … `Commit` целиком
   внутри `ApplyXxx`. Сейчас это соблюдено во всех 10 явных местах — проверено;
   не сломать при разрезе.
2. **Тип сообщения сохраняется.** `ShowInformation`, `ShowExclamation` и
   `ShowQuestion` — разные вещи (разные иконки и кнопки). При переносе не
   заменять одно другим. В эталоне ниже одна проверка была `ShowInformation`,
   две другие — `ShowExclamation`; так и осталось.
3. **`Cursor.Current` и `Application.DoEvents()` едут в UI-половину**, а не
   удаляются. Их удаление — отдельная задача с отдельной проверкой: `DoEvents`
   после модального окна даёт диалогу закрыться до начала долгой операции.
4. **Порядок вызовов сохраняется дословно.** Разрез — это перенос строк, а не
   их переосмысление. Любое улучшение по дороге делает проверку невозможной:
   тестов нет, сверять нечем.
5. **Имя, которое зовут снаружи, остаётся за UI-половиной.** Тогда диff
   ограничен двумя файлами и `.csproj`.
6. **Ядро не ссылается на типы форм.** Ни `SelectionForm`, ни `IWin32Window`,
   ни `DialogResult` в сигнатурах бизнес-методов. Передавать данные:
   `IList<PresentationObject>`, `DataTable`, примитивы.

## 5. Эталон: `ActionOnMassmedia.SplitAction`

Выбран потому, что содержит сразу все пять форм: проверку прав с сообщением,
проверку данных с другим типом сообщения, диалог выбора с колбэком валидации,
транзакционную операцию с пересчётом и обновление экрана.

### Было (одним куском, `Client/Classes/ActionOnMassmedia.cs`)

```csharp
private bool IsSplitOrMergeEnabled(DateTime startDate)
{
    if (SecurityManager.LoggedUser.IsAdmin || ... ) return true;
    if (startDate <= DateTime.Today)
    {
        UserMessage.ShowExclamation(MessageAccessor.GetMessage("SplitAllowedByAdmin"));
        return false;
    }
    return true;
}

private void SplitAction()
{
    try
    {
        if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

        DataTable dt = Campaigns();
        if (dt.Rows.Count < 2)
        {
            UserMessage.ShowInformation(MessageAccessor.GetMessage("CanNotSplitAction"));
            return;
        }

        SelectionForm fSelector = new SelectionForm(..., CheckCampaignsSelectionResultForActionSplit);

        if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
        {
            Cursor.Current = Cursors.WaitCursor;
            ActionOnMassmedia newAction = CreateNewActionForSplit();
            foreach (var campaign in fSelector.AddedItems)
            {
                campaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                campaign.Update();
            }
            Recalculate();
            newAction.Recalculate();
            FireContainerRefreshed();
        }
    }
    finally { Cursor.Current = Cursors.Default; }
}

private bool CheckCampaignsSelectionResultForActionSplit(SelectionForm selectionForm)
{
    if (selectionForm.AddedItems.Count == this.Campaigns().Rows.Count)
    {
        UserMessage.ShowExclamation(MessageAccessor.GetMessage("TooManyCampaignsSelected"));
        return false;
    }
    ...
}
```

### Стало — ядро (`Client/Classes/ActionOnMassmedia.cs`)

Четыре метода, ни один не знает про UI:

- `CanSplitOrMerge(DateTime startDate, out string messageKey)` — форма 5;
- `GetCampaignsForSplit(out string messageKey)` — формы 2 и 5;
- `IsSplitSelectionValid(int selectedCount, out string messageKey)` — форма 5,
  принимает **число**, а не `SelectionForm`;
- `ApplySplitAction(IList<PresentationObject> campaignsToMove)` — форма 2, вся
  бизнес-логика деления акции.

### Стало — UI (`Client/Classes/ActionOnMassmedia.WinForms.cs`)

`IsSplitOrMergeEnabled`, `CheckCampaignsSelectionResultForActionSplit` и
`SplitAction` сохранили прежние имена и сигнатуры, поэтому места вызова
(`DoAction`, `SplitCampaign`, `MergeAction`) не менялись. Каждый из них теперь
делает ровно одно: спросил ядро — показал сообщение или открыл форму.

```csharp
private bool IsSplitOrMergeEnabled(DateTime startDate)
{
    if (CanSplitOrMerge(startDate, out string messageKey)) return true;

    UserMessage.ShowExclamation(MessageAccessor.GetMessage(messageKey));
    return false;
}
```

### Что это даёт вебу

`ApplySplitAction` вызывается из веб-обработчика напрямую, без изменений:
кандидаты берутся `GetCampaignsForSplit`, показываются в браузере, выбор
приходит следующим запросом, проверяется `IsSplitSelectionValid` и
применяется. Транзакционная граница при этом не пересекает обмен с браузером.

## 6. Порядок обработки остальных мест

### Статус на конец партии 17 (коммит `70ea7c3`) — «обычная» работа завершена

Все места из исходного grep-списка (`ShowDialog` в `Client/Classes`),
кроме сознательно отложенных, разобраны. Разобрано 20 файлов за 17 партий:
`ActionOnMassmedia`, `Agency`, `Roller`, `SponsorTariff`,
`PackModulePricelist`, `CampaignPart`, `HeadCompany`, `CampaignDay`,
`CampaignModule`, `CampaignOnSingleMassmedia`, `CampaignPackModule`
(+ `CampaignPartPackModule`), `PackModuleIssue`, `ModulePricelist`,
`PackageDiscountPriceList`, `ActionRollerInStatJournal`,
`TariffWindowWithRollerIssues`, `PaymentCommon`, `PaymentStudioOrder`,
`Firm`, `Utils` (частично), `Action` (частично), `Pricelist`,
`ProgramPartOfSponsorCampaign`, `Campaign`, `MassmediaPricelist`,
`ActionRoller` (частично).

**Проверка на конец партии 18**: `grep -rln "ShowDialog" Client/Classes/*.cs`
(без `*.WinForms.cs`) — остался ровно один нерешённый пункт:

```
Action.cs                    — только комментарий, всё разобрано
ActionOnMassmedia.cs         — только комментарий, всё разобрано
CampaignRoller.cs            — РАЗОБРАН в партии 18 (§8 п.4 закрыт)
MediaPlan.cs:113,243         — обычная форма 1/2, но разрез сейчас
                                малополезен (см. ниже, п.1)
RollerPartOfSponsorCampaign.cs:62 — форма 3.1 (CampaignForm), отложена до этапа 3
SponsorPricelist.cs:35       — мёртвый закомментированный код
Utils.cs:36                  — AskConfirmation, решено НЕ переносить в веб
                                (закрыто 2026-08-21, см. форму 4)
```

**Методологический урок партии 14** (актуален и дальше): `ShowDialog` в
grep — не полный список UI-точек. Найдены и перенесены методы без единого
`ShowDialog`, но с `Globals.ShowSimpleJournal`/`UserMessage.` напрямую
(`ActionOnMassmedia.ShowRollers`/`CheckActionRollersAndProgramIssues`,
`Campaign.PrintTransfers`). После разреза основных мест файла
**обязательно** прогонять grep по ядру не только на `ShowDialog`, но и на
`Globals\.Show|Globals\.Set|UserMessage\.` (раздел 7, чек-лист партии).

Разметка по формам (кумулятивно):

| Форма | Статус |
|---|---|
| 3 — переход/чистый выбор, разрез не нужен | сделано везде, где встретилось |
| 3.1 — модальная редактирующая сессия (`CampaignForm`) | 4 места (`Campaign.cs` ×2, `ProgramPartOfSponsorCampaign.cs`, `RollerPartOfSponsorCampaign.cs`) — отложено до этапа 3 |
| Генерация отчёта (`PrintXxxInquire`/`PrintContract`/`PrintMediaPlan`/`PrintTransfers`) | перенесены целиком, не разрезаны — отдельная область, этап 4 |
| «глубоко переплетённые» с отображением (`ActivateAction`) | 1, перенесён целиком, сознательно не разрезан — не тиражировать этот случай |
| 5 — проверка с сообщением | сделано везде по ходу |
| 4.1 — уведомление (`UserInteraction.Notify`) | базовый метод сделан (партия 2) |
| 1/2 — ввод параметра / выбор и операция | сделано во всех обычных файлах |
| 4 — подтверждение | `AskConfirmation` — решено не переносить (закрыто), `UngroupWindows`/`DeactivateAction` переведены на `UserInteraction.Confirm` |
| кластер замены ролика (§8 п.4) | **разобран в партии 18**, ядро возвращает `DataTable`, UI показывает журнал |

### Что осталось — одна задача, и та низкоприоритетная

1. **`MediaPlan.cs`** (2 места) — обычная форма 1/2, **но разрезать сейчас
   малополезно**, см. разбор в §9.

~~2. Кластер замены ролика~~ — закрыто в партии 18 (`ddf1a3b`), см. §8 п.4.

~~3. `Utils.AskConfirmation`~~ — закрыто 2026-08-21: не переносится в веб;
   решение согласовано с владельцем продукта, см. §8 п.1.

Дальше по плану `docs/tasks/web-migration.md`: следующий пункт этапа 0 —
абстракция конфигурации (этап 0 п.6), либо вертикальный срез (раздел 10),
либо явный заход на один из трёх пунктов выше.

## 7. Проверка каждой партии

1. Сборка решения: MSBuild из
   `C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin`
   (системный MSBuild 4.0 решение не собирает — падает на `AxImp.exe`).
2. Ноль ошибок; предупреждения — только те два, что были до этапа 0
   (`MDIForm.cs` CS0162, `Globals.cs` CS0649).
3. `git diff` читать целиком: разрез не должен менять ни одной строки, кроме
   переноса и подстановки ключа сообщения.
4. Запустить приложение и пройти затронутый сценарий руками — сборка не ловит
   перепутанный тип сообщения или потерянный `FireContainerRefreshed`.
5. **После разреза основных мест — grep по ядру не только на `ShowDialog`.**
   `ShowDialog` ловит не весь UI: `Globals.ShowSimpleJournal` (модальный и
   немодальный журнал), `Globals.SetWaitCursor`/`SetDefaultCursor`,
   `UserMessage.` могут быть в методах без единого диалога (найдено на
   партии 14 — `ShowRollers`, `CheckActionRollersAndProgramIssues`).
   Команда (**`System.Drawing` обязательно, см. §10**):
   ```bash
   grep -n "System\.Windows\.Forms\|System\.Drawing\|Bitmap\|IWin32Window\|DialogResult\|ShowDialog\|Cursor\.\|Application\.\|Globals\.Show\|Globals\.Set\|UserMessage\.\|SelectionForm\|SelectCampaignsForm\|Merlin\.Forms\|Merlin\.Controls\|Merlin\.Reports" Client/Classes/<Файл>.cs
   ```
   Совпадения только в комментариях или в методах, сознательно оставленных
   в ядре с явной причиной (как `DisplayData(ListBox)` в `ActionOnMassmedia.cs`)
   — норма. Любое другое совпадение — недосмотренное место.

6. **Grep — вспомогательный инструмент, а не доказательство.** Настоящая
   проверка — компиляция файла вне проекта `Client`, см. §10. Grep ошибается
   в обе стороны: даёт ложные срабатывания на комментариях (`CampaignPart.cs`
   числился «грязным» из-за двух комментариев, упоминающих `IWin32Window` и
   `Globals.ShowSimpleJournal`) и ложные пропуски, если шаблон неполон
   (`Organization.cs` с `System.Drawing.Bitmap` проходил как чистый).

## 8. Открытые вопросы

1. ~~`Utils.AskConfirmation`~~ — **закрыто 2026-08-21.** Согласовано с
   владельцем продукта: функция (авторизация скидки логином админа/грантора
   прямо в диалоге) в веб не переносится — ей не пользуются даже в
   десктопе. Код в `Utils.cs` не трогаем и не разрезаем: разрез готовит к
   переиспользованию в вебе, а тут переиспользования не будет. Единственный
   живой вызов — `ManagerDiscountForm.cs:132`; второй, в
   `CampaignForm.cs:1438`, — мёртвый код внутри закомментированного блока.
2. Форма 3 в вебе — навигация. Нужно ли сохранять модальность (возврат на
   исходный экран после закрытия) или это обычный переход по адресу.
3. **Структурное ограничение, не решается тиражированием.** `DoAction(string
   actionName, IWin32Window owner, InterfaceObjects interfaceObject)` —
   сигнатура `IActionHandler` (`FogSoft.WinForm/Classes/Interfaces.WinForms.cs`,
   этап 0.1), общая для всех сущностей. Параметр `owner` — `IWin32Window`,
   то есть **сам тип интерфейса несёт UI-зависимость**. Любой `override DoAction`
   в `Client/Classes` обязан повторить эту сигнатуру и поэтому не может стать
   полностью ядровым файлом без правки самого интерфейса — только целиком
   переехать в `<Класс>.WinForms.cs` (так сделано для `PresentationObject`,
   `ObjectContainer` на этапе 0.1, для `CampaignPart` на партии 2). Это не
   баг разреза, это существующая архитектура: `DoAction` смешивает «что
   сделать» и «куда прикрепить модальное окно». Полное решение — редизайн
   `IActionHandler`, чтобы резолвинг действия не нёс окно-владельца; это
   отдельная, более крупная задача, вероятно уровня этапа 2 (когда выбрана
   веб-технология и понятно, чем `owner` заменяется в браузере), не этапа 0.
   Пока просто: **`DoAction`-переопределения переезжают целиком, не разбираются**.
4. **Кластер замены ролика — не тиражировать по шаблону, разбирать отдельно.**
   Найден на партии 3. Четыре файла вокруг одной операции:
   - `CampaignRoller.cs`: `SubstituteRoller` (экземплярный, оборачивает
     `Substitute` делегатом-колбэком для пересчёта) и `Substitute`/`Subtitute`
     (оба `static`; `Substitute` показывает диалог и вызывает `Subtitute`;
     `Subtitute` — сам DB-вызов плюс `Globals.ShowSimpleJournal` в конце,
     если есть незаменённые ролики — единственная UI-точка внутри статического
     метода, который в остальном чистый);
   - `ActionRoller.cs`: `SubstituteRoller` — свой диалог (`SelectionForm`),
     затем цикл по кампаниям действия с диспетчеризацией по типу кампании
     (`Simple`/`Sponsor` → `CampaignRoller.Subtitute` напрямую;
     `Module`/`PackModule` → тот же `Subtitute`, но по каждому модулю/пакету
     отдельно) — **три прямых вызова `CampaignRoller.Subtitute`, минуя
     `Substitute`**, то есть `Subtitute` уже сегодня вызывается и с диалогом,
     и без;
   - `CampaignModuleRollerInsideDay.SubstituteRoller` (в `CampaignModule.cs`) —
     обёртка над тем же `CampaignRoller.SubstituteRoller`;
   - `PackModuleIssue.cs:99` — свой вызов `CampaignRoller.Substitute` (с диалогом).

   Сложность не в объёме, а в том, что `Subtitute` — общая точка правды для
   четырёх разных путей вызова, и его единственная UI-зависимость
   (`Globals.ShowSimpleJournal`) должна либо остаться в нём (тогда он не
   станет ядровым, и это нормально — как `RecalculateAndShowPriceChange`
   решилось через форму 4.1, здесь возможен тот же приём, но `ShowSimpleJournal`
   показывает таблицу, а не сообщение по ключу — `UserInteraction.Notify` не
   подходит без расширения), либо вынестись, и тогда все 4 точки вызова должны
   единообразно решить, что делать с «незаменёнными роликами». Разбирать
   вместе, одной партией, не по одному файлу.

   **РЕШЕНО в партии 18 (коммит `ddf1a3b`).** Выбран второй путь: метод
   записи возвращает `DataTable` незаменённых роликов и ничего не
   показывает, журнал показывает вызывающий UI-код через
   `CampaignRoller.ShowUnsubstitutedRollers`. Обоснование: тот же паттерн
   уже применён в четырёх местах этапа (`ApplyPositionChanges`,
   `ApplyIssuesDelete`, `ApplyMassClone`, `ApplyClone`) — расширять
   `UserInteraction` под показ таблицы не потребовалось.

   Три уточнения, всплывшие при реализации:
   - путей вызова оказалось **пять**, а не четыре: неквалифицированный
     вызов `Substitute(...)` внутри самого `CampaignRoller` (строка 50)
     не ловился grep-ом по `"\.Substitute("`;
   - `Subtitute` переименован в `ApplyRollerSubstitutionForDays` **как
     страховка, а не косметика**: смена типа возврата `void` → `DataTable`
     не ломает компиляцию вызовов-операторов, поэтому без переименования
     три вызова в `ActionRoller` молча собрались бы и молча перестали
     показывать журнал. Переименование заставило компилятор указать на
     каждое место. Имя с суффиксом `ForDays` — потому что
     `CampaignPart.ApplyRollerSubstitution` уже существует (партия 2), а
     `CampaignRoller` от него наследуется: совпадение имени скрыло бы
     унаследованный член (CS0108);
   - в `ActionRoller` запись идёт в цикле, поэтому журнал показывается по
     разу на итерацию — при незаменённых роликах в нескольких кампаниях
     будет несколько окон подряд. Поведение существующее, сохранено
     намеренно; сведение в одно окно — изменение поведения, вне разреза.

## 9. Разбор MediaPlan.cs: потоков там уже нет

Проведён 2026-08-21 по просьбе владельца продукта («подозрение, что там
глобально навёрнуто больше, чем нужно; может, отдельные потоки и не нужны»).
Подозрение подтвердилось. Код по итогам разбора **не менялся** — сознательно,
см. «Что делать» ниже.

### Исправление предыдущей оценки

В партиях 11–17 этот файл был помечен как «НЕ ТРОГАТЬ без отдельного ревью:
межпотоковый Invoke/InvokeRequired, риск зависания EXCEL.EXE». **Оценка была
неверной.** Она опиралась на комментарий в самом коде
(`MediaPlan.cs:119`), который описывает **уже вылеченную** проблему:

> Экспорт идёт синхронно на UI-потоке (STA): Excel создаётся и освобождается
> на одном апартаменте — без маршалинга между потоками, из-за которого
> процесс EXCEL.EXE раньше зависал в памяти.

То есть многопоточность из экспорта убрали раньше, когда чинили зависание.
Флаг риска был снят.

### Что показала проверка

Grep по `MediaPlan.cs` на `Thread|Invoke|BackgroundWorker|Task|async|lock`:
ни одного создания потока. Осталось два следа прежней архитектуры:

1. **`Invoke`-обёртка в `SelectRollers`** (`MediaPlan.cs:240-262`): диалог
   выбора роликов завёрнут в лямбду, дальше
   `if (Globals.MdiParent.InvokeRequired) Invoke(...) else вызвать напрямую`.
   Прослежены все пути к `MediaPlan.Show()`: `Action.PrintMediaPlan`,
   `Campaign.PrintMediaPlan`, `ActJournalRow` — все три через `DoAction`,
   который срабатывает только от меню и контекстных меню, то есть с
   UI-потока. Единственная фоновая работа в каркасе (`JournalForm`,
   `GraphForm`, `MasterDetailForm` — `ThreadPool.QueueUserWorkItem`) грузит
   данные и делает `Invoke` обратно, до `DoAction` не доходит.
   **`InvokeRequired` здесь всегда `false`.**

2. **Сохранение/восстановление `CurrentCulture`** (`MediaPlan.cs:100` и `:143`):
   в самом файле культура нигде не меняется. Глубже меняется по-настоящему —
   `MSExportDocument.OnAppQuit` ставит `en-US` — но там же корректно
   восстанавливает в `finally`. Подстраховка поверх работающей подстраховки.

### Мёртвый код в слое экспорта (найден попутно, не тронут)

- `FogSoft.WinForm/Classes/Export/MSExcel/MSExportDocument.cs:60` —
  `GetNewSheet` захватывает `oldCulture` первой строкой и **больше нигде её
  не использует**: ни `finally`, ни восстановления. Мёртвая переменная.
- `FogSoft.WinForm/Classes/Export/ExportManager.cs:130-131` — сохраняет
  культуру и восстанавливает в `finally`, но сама смена культуры
  **закомментирована**. Сохранение и восстановление неизменённого значения.

### Что делать

**Ничего не трогать** (решение владельца продукта, 2026-08-21):

- `Invoke`-обёртку **не снимать**: в рантайме она ничего не стоит, а вывод
  «всегда false» — статический анализ; если какой-то путь не увиден, снятие
  защиты даёт падение с cross-thread exception. Выигрыша нет, хвостовой риск
  есть.
- Мёртвые переменные в слое экспорта не удалять: они в слое, который по
  плану заменяется целиком (`docs/tasks/web-migration.md`, этап 4, COM
  Interop → OpenXml).
- **Разрез самого `MediaPlan.cs` сейчас малополезен.** Это по сути и есть
  Excel-экспорт, а весь слой Excel заменяется в этапе 4. Плюс `SelectRollers`
  показывает диалог, то есть уезжает в UI-половину целиком вместе со своей
  `Invoke`-обёрткой — она ничего не блокирует. Возвращаться к файлу имеет
  смысл вместе с этапом 4, а не в рамках этапа 0.

## 10. Мост в FogSoft.Core: попытка и её результаты (2026-08-21)

Цель: подключить разрезанные доменные классы `Client/Classes/*.cs` в проект
`FogSoft.Core` (net8.0) ссылками `Compile`/`Link` — чтобы **компилятор**, а не
grep, подтвердил, что разрез этапа 0.4 действительно достиг цели.

**Мост пока не закрыт**, но своё дело сделал: нашёл то, что grep пропускал.

### Что нашёл компилятор

1. **`Organization.cs` тянет `System.Drawing.Bitmap`.** Поле `_bitmap`,
   свойство `Signature` (картинка подписи для отчётов, используется в
   `AgencyPassportForm`, `MassmediaPassport`, `GenericReport`). По grep файл
   проходил как чистый: шаблон проверял `System.Windows.Forms`, но не
   `System.Drawing`. **Дыра в чек-листе, исправлена** — см. §7 п.5.
   Рядом в том же классе уже есть байтовый `SignatureBytes` (им пользуется
   `MediaPlan`), то есть `Signature` — надстройка над ним для UI.

2. **Два мёртвых `using`, блокировавших сборку вне `Client`** (убраны,
   коммит `8fc787d`):
   - `Pricelist.cs` — `using CrystalDecisions.CrystalReports.Engine`,
     единственное упоминание Crystal в файле;
   - `TariffWindowPackModule.cs` — `using System.Runtime.Remoting.Messaging`,
     ничего из `Messaging` не используется. По тексту ошибки выглядело как
     несовместимость с .NET 8 — **это не так**, просто мёртвая строка.

3. **Пакет `DocumentFormat.OpenXml`** нужен ядру из-за `CpOneDocGenerator.cs`
   (20 ошибок из 45 были от него). Версия должна совпадать с
   `Client/packages.config` — 3.4.1. В `csproj` ядра пока не добавлен:
   добавлять вместе с закрытием моста.

### Почему мост не закрыт

Упирается не во множество проблем, а в **несколько центральных доменных
классов, которые до сих пор несут по кусочку UI**. От них зависит почти всё
остальное, поэтому обрезка «до зелёной сборки» схлопывает мост почти в ноль —
такой мост коммитить бессмысленно.

Список хвостов (он же — что осталось от этапа 0.4):

| Файл | Что мешает |
|---|---|
| `Campaign.cs` | `DisplayCampaignData(ListBox)`, пустая заглушка `PrintOnAirInquire(Form)` |
| `Massmedia.cs` | `DoAction(IWin32Window)`, `using Merlin.Forms` — файла не было в списке `ShowDialog`, поэтому его не трогали |
| `ActionOnMassmedia.cs` | `DisplayData(ListBox)` |
| `Organization.cs` | `Signature` (`System.Drawing.Bitmap`) |
| `MassmediaPricelist.cs`, `PackModulePricelist.cs`, `SponsorPricelist.cs`, `Tariff.cs`, `PackModuleIssue.cs`, `TariffWindowWithRollerIssues.cs` | проверить тем же расширенным grep-ом, объём не измерен |

Все хвосты однотипны: метод принимает или возвращает UI-тип (`ListBox`,
`Form`, `Bitmap`, `IWin32Window`) и переезжает в `<Класс>.WinForms.cs`
целиком, по уже отработанной схеме.

### Что сделано по хвостам (партия 19, коммит `99f3e45`)

Закрыты все перечисленные выше файлы: `Campaign` (`DisplayCampaignData(ListBox)`,
заглушка `PrintOnAirInquire(Form)`), `ActionOnMassmedia` (`DisplayData(ListBox)`),
`Organization` (`Signature`/`Bitmap`), `Massmedia`, `Tariff`,
`PackModulePricelist` (`DoAction(IWin32Window)` и родня), плюс найденные
сборкой по ходу `override GetPassportForm(DataSet) -> PassportForm` в
`Massmedia` и `Tariff`.

Отдельно стоит отметить: `Massmedia.cs` и `Tariff.cs` **вообще не попадали в
работу этапа 0.4** — в них нет `ShowDialog`, поэтому они не вошли в исходный
список из 56 мест. Ещё один довод, что `ShowDialog` не был достаточным
критерием отбора.

### Хвост длиннее, чем казалось: чек-лист снова неполон

После партии 19 мост дошёл с 45 ошибок до 34, но **не закрылся**. Причина —
в чек-листе (§7 п.5) по-прежнему нет ещё нескольких UI-неймспейсов, которые
всплыли только при компиляции:

- `FogSoft.WinForm.Controls` — типы `SmartGrid`, `LookUp` (например,
  `Massmedia.cs:323` — метод принимает их параметрами);
- `FogSoft.WinForm.Passport.Forms` — строка `using` не содержит подстроки
  `PassportForm`, поэтому шаблон её не ловит;
- `Merlin.License`.

**Вывод, который важнее самого списка:** дополнять шаблон grep-а можно
бесконечно, и он всё равно будет отставать. Единственный надёжный критерий —
компиляция вне проекта `Client`. Поэтому мост стоит доводить не «сначала
вычистим по grep, потом подключим», а наоборот: подключать файл в
`FogSoft.Core` и чинить ровно то, на что укажет компилятор.

### Состояние на конец сессии

Мост **не закоммичен**: он не собирается, а коммитить красную сборку нельзя.
В `FogSoft.Core.csproj` остаётся прежний набор — только файлы из
`FogSoft.WinForm`. Все улучшения кода, найденные мостом, закоммичены
отдельно (`8fc787d`, `99f3e45`) и в сборку десктопа входят.

Попытка автоматически «обрезать мост до зелёного» скриптом провалилась
дважды и оба раза из-за экранирования обратного слэша в `grep`/`sed`:
шаблон молча не находил совпадений, скрипт рапортовал об успехе, а один раз
вычистил из проекта и файлы ядра. **Обрезать вручную либо не обрезать
вовсе** — подключать по одному файлу и сразу чинить.

### Предупреждение тому, кто будет доделывать

При обрезке моста скриптом легко зацепить лишнее: путь в `csproj` содержит
обратные слэши (`..\Client\Classes\X.cs`), и неаккуратный шаблон
`Classes.X.cs` совпадает **и** с `..\FogSoft.WinForm\Classes\X.cs`. На этом
из проекта каскадом вылетели `SecurityManager`, `ConfigurationUtil`,
`EntityManager`. Проверять результат обрезки не только сборкой, но и числом
записей `<Compile>`.
