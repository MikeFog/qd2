# Ошибки бизнес-правил: `RAISERROR('Ключ', 16, 1)` + `iMessage`

Как в qd2 хранимая процедура отказывает пользователю и откуда берётся текст сообщения.
О логировании таких отказов — `docs/LOGGING.md` («Отказ по бизнес-правилу»).

## Механизм

1. Процедура (обычно `*IUD`) проверяет правило и делает
   ```sql
   RAISERROR('DurationExceedsTotal', 16, 1)
   RETURN
   ```
   Строка — **не текст, а ключ**.
2. В таблице `iMessage` (`name`, `message`) лежит строка с этим ключом и русским текстом.
   Параметры подстановки (`{0}`…) — в `iMessageParameter`. Есть ещё `iMessageToActivate` —
   сообщения для проверки перед активацией акции, это другой механизм.
3. `DataAccessor` ловит `SqlException` (`Number == 50000`, `Class == 16`), UI через
   `MessageAccessor.GetMessage(ключ)` показывает текст пользователю.
4. Если ключа в `iMessage` нет, `GetMessage` возвращает `null`: окно десктопа покажет общий текст
   «В программе произошла ошибка.», веб и журналы ошибок десктопа — сырой ключ (а журналы, которые пишут
   `MessageAccessor.GetMessage`, — пустую строку), а `DataAccessor` не сочтёт
   отказ «бизнесовым» и запишет его как `ERROR` со стеком. У нарушенного ограничения (547/2627/2601) без
   строки в `iMessage` и веб, и десктоп показывают общий текст (веб — с 19.10.2026).

## Как добавить новое правило

1. `RAISERROR('НовыйКлюч', 16, 1)` + `RETURN` в процедуре, **до** любых изменений данных
   (или внутри транзакции, которую откатит вызывающий код).
2. Строка в `iMessage` — идемпотентно, в деплой-скрипте:
   ```sql
   IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'НовыйКлюч')
       INSERT INTO [dbo].[iMessage] (name, message) VALUES ('НовыйКлюч', N'Текст. Операция прервана.');
   ```
   Формулировка по конвенции: что нельзя + «Операция прервана.» Примеры: `TariffInUse`, `TariffChainDamage`.
3. Найти **все** процедуры, которые пишут те же данные (см. ниже), а не только основную `*IUD`.
4. После наката на базу **клиент qd2 надо перезапустить**: `MessageAccessor` читает весь словарь
   `iMessage` (`MessageLoad`) один раз при старте (`FullLoadDictionaries`), а ключ, которого не было
   при старте, не подхватится. Сама проверка в процедуре действует сразу.

## Где лежат тексты и как искать

- Ключ в `.sql` ищется `Grep "RAISERROR('Ключ'"`; в скриптах `ArtvisDB/Scripts/*seed*.sql` —
  `iMessage` + ключ. Текст сообщения в репозитории есть только там (в `.sql` процедур его нет),
  поэтому реальный текст смотреть в базе: `SELECT message FROM iMessage WHERE name = '...'`.
- Чтобы узнать, откуда пришёл отказ пользователя, — ключ из лога (`WARN - Отклонено бизнес-правилом. Процедура: X — Ключ`).

## Важно: на таблицы CHECK не ставим

В боевых данных бывают исторические нарушения (например, `duration > duration_total`: на dev ~160 тарифов
и ~37 тыс. окон), поэтому правила держатся в процедурах и проверяют только **новые** значения.
Побочный эффект: правка старой «плохой» записи без исправления нарушенного поля тоже отклоняется.

## Реестр правил (примеры)

| Ключ | Где проверяется | Смысл |
|---|---|---|
| `DurationExceedsTotal` | `TariffIUD` (Add/Update/Clone), `TariffWindowIUD` (Add/Update), `TariffWindowChangeDuration`, `TariffWindowChangeDurationInDay`, `GenerateTariffWindowByTemplate` | `duration` не больше `duration_total` — всегда: с 12.10.2026 (`Deploy/40`) исключения «`duration_total = 0` — не задана» нет, пустая полная при ненулевой продолжительности — отказ. Окно 0/0 — так заказчик помечает **профилактику передатчика** — правилу удовлетворяет; «Изменить продолжительность по дням» (`TariffWindowChangeDurationInDay`, меняет только продолжительность) и веб «Изменить окна» с одной из двух продолжительностей окна 0/0 пропускают — массовой правкой профилактика не снимается; правка обеих (`TariffWindowChangeDuration`) применяется и к ним. В `TariffIUD` UpdateItem пустые продолжительность и полная — прежние значения тарифа; при смене полной проверяются и окна тарифа, которым она уходит (у окна продолжительность бывает своя). Деплой: `ArtvisDB/Scripts/duration-not-above-total-deploy.sql`, `Deploy/13_tariff-total-change-windows.sql`, `Deploy/40_duration-total-not-below-duration.sql` |
| `TariffInUse` | `TariffIUD` Update | у тарифа уже есть окна, менять время/цену/дни/длительность нельзя |
| `TariffAlreadyExists`, `TariffConflictsWithSponsorTariff`, `TariffChainDamage` | `TariffIUD` | дубль, конфликт со спонсорским, разрыв цепочки `TariffUnion` |
| `DiscountReleaseInUse` | `DiscountReleaseIUD` Delete | по набору объёмной скидки уже посчитаны кампании (`Campaign.discountReleaseID`). Правки использованной скидки не запрещены, а подтверждаются в qd2 окном `DiscountAffectedActionsForm` (процедура `DiscountChangeAffectedActions`). Деплой: `ArtvisDB/Scripts/discount-applied-pricelist-id-deploy.sql` |
| `PackageDiscountInUse` | `PackageDiscountPriceListIUD` Delete, `PackageDiscountIUD` Delete | по прайс-листу пакетной скидки (или любому прайс-листу пакета) уже посчитаны акции (`Action.packageDiscountPriceListID`); там же |
| `LinkedWindowsWrongOrder`, `UnitedTariffWindowsWrongOrder` | `TariffWindowIUD` Update | перенос времени выхода окна нарушил бы порядок объединённых окон (цепочка `windowPrevId`/`windowNextId`) или окон объединённых тарифов (`TariffUnion`). Массовый перенос `TariffWindowMoveTime` не отказывает, а пропускает такие окна и возвращает их списком (журнал в клиенте). `docs/window-merging.md` §3; деплой `Deploy/14_united-tariff-windows-order.sql` |
| `TariffWindowDayChange` | `TariffWindowIUD` Update и Add | день окна менять нельзя, только время выхода в пределах дня (решение Р-2 от 07.10.2026, `docs/tasks/window-actual-open-questions.md`): «Изменить» — день нового времени выхода должен совпасть с `dayOriginal` окна, «Добавить» — время выхода и время по расписанию в одном дне. Групповой перенос `TariffWindowMoveTime` день не меняет по построению. Деплой `Deploy/31_tariff-window-day-fixed.sql` |
| `PromoPositionForbidden` | `hlp_IssueVerify` (msgError → `RAISERROR` в `IssueIUD`/`ModuleIssueIUD`/`PackModuleIssueID`), `RollerSubstitute` (строка журнала, `iMessageToSubtitute`) | ролик локального промо (типы 8/9) с позиционированием. Деплой: `ArtvisDB/Scripts/promo-position-forbidden-deploy.sql` |
| `AgitationServiceRollerManual` | `hlp_IssueVerify` (там же, где `PromoPositionForbidden`), `RollerSubstitute` (`RAISERROR` до замены) | человек ставит ролик «Отбивка политической агитации» (7), «Локальное СМИ (агитация)» (44) или «Федеральное СМИ (агитация)» (55) — их ставит только система (`AgitationFraming`, мимо проверки); замена на ролик того же типа разрешена. Решение заказчика 11.10.2026. Деплой: `Deploy/38_agitation-service-rollers-manual.sql` |
| `RolType45PositionForbidden` | там же, где `PromoPositionForbidden` | ролик «Локальное СМИ» (4) или «Федеральное СМИ» (5) с позиционированием (заказчик, 11.10.2026). Деплой: `Deploy/37_smi-position-forbidden.sql` |
| `FirmInnFormat`, `FirmKppFormat`, `FirmOgrnFormat` | `FirmIUD` AddItem/UpdateItem | формат реквизитов фирмы (с 25.10.2026, `Deploy/78`): ИНН 10/12 цифр, КПП 9 знаков (цифры, на 5–6 позициях ещё заглавные латинские буквы), ОГРН 13/15 цифр; пустое значение допустимо. Правит и старую «грязную» карточку: пока поле не исправлено, не сохраняется ничего. `FirmInnUnique` (дубль ИНН+КПП+счёт) сравнивает `fn_FirmKey` — без пробелов и знаков — и при пустом ИНН не срабатывает |
