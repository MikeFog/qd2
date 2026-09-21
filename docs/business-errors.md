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
4. Если ключа в `iMessage` нет, `GetMessage` возвращает `null` — пользователь увидит сырой ключ,
   а `DataAccessor` не сочтёт отказ «бизнесовым» и запишет его как `ERROR` со стеком.

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
| `DurationExceedsTotal` | `TariffIUD` (Add/Update/Clone), `TariffWindowIUD` (Add/Update), `TariffWindowChangeDuration`, `TariffWindowChangeDurationInDay`, `GenerateTariffWindowByTemplate` | `duration` не больше `duration_total`; `duration_total = 0` — «не задана», проверки нет. Деплой: `ArtvisDB/Scripts/duration-not-above-total-deploy.sql` |
| `TariffInUse` | `TariffIUD` Update | у тарифа уже есть окна, менять время/цену/дни/длительность нельзя |
| `TariffAlreadyExists`, `TariffConflictsWithSponsorTariff`, `TariffChainDamage` | `TariffIUD` | дубль, конфликт со спонсорским, разрыв цепочки `TariffUnion` |
