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

`ShowDialog` без разбора результата: открыли карточку/редактор и всё.
Пример: `ActionOnMassmedia.cs:96`, `Campaign.cs:486`, `Campaign.cs:499`,
`PackModulePricelist.cs:80`, `ProgramPartOfSponsorCampaign.cs:89`,
`RollerPartOfSponsorCampaign.cs:62` — шесть мест.

**Разрез не нужен.** Метод целиком переезжает в UI-половину. В вебе это
навигация, а не диалог. Сюда же относятся переопределения `ShowPassport`
(`Agency.cs:207`, `Roller.cs:101`, `SponsorTariff.cs:51`) — базовый
`PresentationObject.ShowPassport` уже вынесен в UI-половину на этапе 0.1.

### Форма 4. Вопрос «да/нет» и подтверждение правами

Уже решена на этапе 0.1: `FogSoft.WinForm/Classes/UserInteraction.cs`.
Домен спрашивает `UserInteraction.Confirm(text)`, обработчик подставляется
при старте приложения.

Особый случай — `Client/Classes/Utils.cs:36` `AskConfirmation`: `FrmConfirmation`
запрашивает вход администратора, чтобы авторизовать скидку, и возвращает
`(User, ManagerDiscountReasonId)`. Это не «да/нет», а сбор данных, поэтому
обрабатывается **по форме 2**: UI собирает, ядро применяет.

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

Разметка по формам (уточняется при обработке конкретного файла):

| Форма | Мест | Трудоёмкость |
|---|---|---|
| 3 — переход на экран, разрез не нужен | ~9 | тривиально, только перенос |
| 5 — проверка с сообщением | сопутствует остальным | тривиально |
| 1 — ввод параметра | ~8 | низкая |
| 2 — выбор и операция | ~35 | основная работа |
| 4 — подтверждение | 2 | `UserInteraction` уже есть |

Рекомендуемый порядок: начать с формы 3 (девять мест закрываются переносом
без изменения логики, дают привычку к раскладке файлов), затем форма 1, затем
форма 2 по возрастанию числа мест в файле. Файлы с одним вызовом
(`HeadCompany`, `CampaignDay`, `CampaignRoller`, `ModulePricelist`,
`PackageDiscountPriceList`, `PackModuleIssue`, `CampaignModule`,
`ActionRollerInStatJournal`, `TariffWindowWithRollerIssues`) — раньше, чем
`Campaign.cs` (7 мест), `ActionOnMassmedia.cs` (осталось 5) и
`MassmediaPricelist.cs` (5).

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

## 8. Открытые вопросы

1. `Utils.AskConfirmation` (`FrmConfirmation`, вход администратора для скидки) —
   в вебе это отдельный экран аутентификации внутри операции. Решить, как он
   выглядит, до разреза этого места.
2. Форма 3 в вебе — навигация. Нужно ли сохранять модальность (возврат на
   исходный экран после закрытия) или это обычный переход по адресу.
