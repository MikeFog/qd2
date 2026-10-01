# Деплой веб-версии (FogSoft.Web) на прод

Черновик 2026-09-28. Места с `<…>` зависят от ответов про сервер — см. «Открытые вопросы» в конце.

Схема решена в `docs/tasks/web-migration.md`, раздел 7 п. 8: **Kestrel напрямую, служба Windows, без IIS**
(на сервере IIS нет). Среда .NET на сервер не ставится — публикуем self-contained: всё нужное лежит
в папке приложения.

## 0. Перед первым выкатом (один раз)

1. ✅ **Поддержка службы Windows** (2026-09-30): пакет `Microsoft.Extensions.Hosting.WindowsServices` и
   `builder.Services.AddWindowsService();` в `Program.cs`. Без неё `sc start` падал бы с ошибкой 1053.
   Вне службы (`dotnet run`, отладчик) ничего не меняется — проверено. Запуск именно службой проверяется
   на сервере при первой установке.
2. **SQL, которого ещё нет на проде** (всё, что влито в `master` после `d66dfcd`):
   - `ArtvisDB/Scripts/document-templates-deploy.sql` — таблица `DocumentTemplate` и 4 процедуры
     (экран «Шаблоны документов», договор по акции). Десктоп не затронут, идемпотентен.

   ```
   sqlcmd -S <сервер>\SQLEXPRESS -d Artvis -E -f 65001 -I -b -i document-templates-deploy.sql
   ```

## 1. Сборка (на машине разработчика)

Публиковать **из чистого коммита**, не из рабочей копии: в ней бывают незакоммиченные правки
(в том числе чужой сессии), и они попадут в сборку или сломают её.

```powershell
$commit = git rev-parse --short HEAD          # или нужный тег/коммит
$src = "C:\Work\AdvertAg\publish\src-$commit"
$out = "C:\Work\AdvertAg\publish\web-$commit"
New-Item -ItemType Directory -Force $src | Out-Null
git archive $commit FogSoft.Web FogSoft.Core FogSoft.WinForm Client ConnectionStringsProtector Lib Microsoft.ApplicationBlocks.Data | tar -x -C $src
dotnet publish "$src\FogSoft.Web\FogSoft.Web.csproj" -c Release -r win-x64 --self-contained true -o $out
Compress-Archive "$out\*" "$out.zip"
```

Проверено 2026-09-28 на `8c27f59`: публикация проходит, ~135 МБ; опубликованный `FogSoft.Web.exe` в режиме
Production стартует, отдаёт страницу, стили и `blazor.web.js`, пишет лог в `logs\qd2.log` рядом с exe
(даже если текущий каталог другой).

В сборку попадают `FogSoft.Web.dll.config` и `appsettings.json` **с настройками разработчика** — на сервере
их при обновлении не перезаписывать (см. п. 4).

## 2. Первая установка на сервер

Папка приложения — `<C:\qd2web>` (дальше так и пишется).

1. Распаковать архив в `C:\qd2web`.
2. **`FogSoft.Web.dll.config`** — строка подключения к проду и параметры установки:
   ```xml
   <add key="Language" value="ru" />
   <add key="Culture" value="ru-RU" />
   <add key="Title" value="АРТВИС" />
   ...
   <add name="Main" connectionString="user id=AdvertAgUser; password=<…>; server=lpc:.\SQLEXPRESS; database=Artvis" />
   ```
   `lpc:` — если веб на той же машине, что SQL (SQLBrowser отключён). Если на другой — `tcp:<сервер>,<порт>`.
   Вместо открытого пароля можно, как в десктопе, положить зашифрованную строку в
   `appSettings` ключом `ConnectionString_Main` (`ConfigurationUtil` понимает оба варианта) —
   тогда `connectionStrings/Main` удалить.
3. **`appsettings.json`** — адрес, на котором слушать (добавить ключ `Urls`):
   ```json
   {
     "Urls": "http://0.0.0.0:5051",
     "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
     "AllowedHosts": "*"
   }
   ```
   Только для работы на самом сервере (из RDP-сеанса) — `http://localhost:5051`, порт наружу не открывается.
4. Служба (PowerShell от администратора). Учётка — виртуальная `NT SERVICE\qd2web`: к базе ходим
   SQL-логином из конфига, Windows-права в SQL ей не нужны.
   ```powershell
   sc.exe create qd2web binPath= "C:\qd2web\FogSoft.Web.exe" start= delayed-auto obj= "NT SERVICE\qd2web" DisplayName= "qd2 web"
   sc.exe description qd2web "АРТВИС, веб-версия (FogSoft.Web)"
   sc.exe failure qd2web reset= 86400 actions= restart/60000/restart/60000/restart/60000
   New-Item -ItemType Directory -Force C:\qd2web\logs | Out-Null
   icacls C:\qd2web /grant "NT SERVICE\qd2web:(OI)(CI)RX"
   icacls C:\qd2web\logs /grant "NT SERVICE\qd2web:(OI)(CI)M"
   ```
5. Брандмауэр. **Пробный этап (решение 2026-09-30): только с одного внешнего адреса** — машины Миши;
   безопасность для промышленного запуска — отдельно (см. «Блокеры доступа из интернета»).
   ```powershell
   New-NetFirewallRule -DisplayName "qd2 web 5051 (test)" -Direction Inbound -Protocol TCP -LocalPort 5051 -RemoteAddress <IP Миши> -Action Allow -Profile Any
   ```
   Если адрес сменился (домашний IP бывает динамическим):
   `Set-NetFirewallAddressFilter -AssociatedNetFirewallRule (Get-NetFirewallRule -DisplayName "qd2 web 5051 (test)") -RemoteAddress <новый IP>`.
   Если перед сервером роутер с NAT — на нём ещё нужен проброс порта 5051 на сервер.
6. `sc.exe start qd2web`.

## 3. Проверка после установки/обновления

1. `sc.exe query qd2web` — `RUNNING`.
2. `C:\qd2web\logs\qd2.log` — без `ERROR` при старте.
3. Браузер: `http://<сервер>:5051` → вход под обычным пользователем qd2 (логин и пароль те же, что в десктопе).
   Автологина разработчика в Production нет (`WebLogin.TryDevAutoLogin` работает только в Development).
4. Открыть 2–3 журнала (Акции, Оплаты), сетку вещания; переключить язык на es и обратно.
5. Под пользователем без прав администратора — меню урезано так же, как в десктопе.

## 4. Обновление

```powershell
sc.exe stop qd2web
Rename-Item C:\qd2web C:\qd2web-prev-<дата>          # откат = обратное переименование
Expand-Archive web-<commit>.zip C:\qd2web
Copy-Item C:\qd2web-prev-<дата>\FogSoft.Web.dll.config, C:\qd2web-prev-<дата>\appsettings.json C:\qd2web -Force
New-Item -ItemType Directory -Force C:\qd2web\logs | Out-Null
icacls C:\qd2web\logs /grant "NT SERVICE\qd2web:(OI)(CI)M"
sc.exe start qd2web
```

- Перезапуск службы **разлогинивает всех** открытых пользователей веба (сеанс живёт в circuit) — выкатывать
  вне рабочего времени или предупреждать.
- SQL-скрипты из `Deploy/` катятся до обновления веба; после скриптов с метаданными или переводами
  (`iTranslation`) веб нужно перезапустить — кэши метаданных и переводов статические.
- Откат: `sc stop`, вернуть `C:\qd2web-prev-<дата>` на место, `sc start`. Если вместе с вебом катился SQL —
  откатывать по шапке соответствующего скрипта.

## 5. Что сознательно не сделано

- **HTTPS в инструкции пока нет.** По HTTP пароль при входе идёт открытым текстом по сети. Допустимо только
  внутри периметра; для доступа из интернета — см. «Блокеры доступа из интернета» ниже.
  Предупреждение `Failed to determine the https port for redirect` в консоли — следствие этого, безвредно.
- **Ключи Data Protection** не закреплены: после перезапуска старые вкладки просто переподключаются
  со входом заново (так и так из-за circuit).
- **Память**: каждое открытое окно веба держит состояние на сервере, а SQL Express там же берёт ~1,4 ГБ.
  Оценки на пользователя пока нет (раздел 7 п. 10 `web-migration.md`).

## Сервер (ответы Миши, 2026-09-30)

- Веб — **на том же сервере**, где SQL и RDP-десктоп → в строке подключения `lpc:.\SQLEXPRESS`.
- **Windows Server 2022 Standard, 21H2, сборка 20348.502** (обновлений с начала 2022 нет). .NET 10 и
  виртуальные учётки служб (`NT SERVICE\…`) поддерживаются; self-contained сборке ничего доустанавливать не нужно.
- Память 32 ГБ — с запасом (SQL Express больше ~1,4 ГБ буфера не берёт).
- **Три базы: `Artvis`, `Tumen`, `Belgorod`.** Веб знает одну строку подключения (`Main`), поэтому —
  три копии: папка, служба и порт на базу, например `C:\qd2web\artvis` / `qd2web-artvis` / 5051,
  `…\tumen` / `qd2web-tumen` / 5052, `…\belgorod` / `qd2web-belgorod` / 5053. Сборка одна, различаются
  только `FogSoft.Web.dll.config` (база) и `appsettings.json` (порт). Шаги п. 2–4 повторяются на каждую.
- Строка подключения открытая (как у десктопа).
- **~20 одновременных пользователей** на все базы — по памяти без проблем.
- **Доступ — из интернета.** Это меняет п. 5: HTTP наружу недопустим, см. «Блокеры доступа из интернета».

## Блокеры доступа из интернета (решить до выката)

1. **HTTPS обязателен.** Иначе пароли qd2 идут открытым текстом через интернет. Нужен сертификат на имя
   (домен), которое смотрит на внешний адрес сервера; Kestrel берёт его из `appsettings.json`
   (`Kestrel:Endpoints:Https`, файл `.pfx`).
2. **Защиты от перебора паролей нет.** `WebLogin.Login` → `SecurityManager.Login`: ни задержки, ни блокировки
   после неудачных попыток. Внутри офиса это было неважно, из интернета — первое, что начнут перебирать.
   Нужна правка в вебе (задержка/временная блокировка по логину и адресу).
3. **Windows и SQL без обновлений** (сборка 20348.502, SQL RTM). Наружу открывается только порт веба, но
   обновления до выхода в интернет стоит поставить.
4. **Наружу — только порты веба.** SQL (1433/1434) и RDP не пробрасывать.

Альтернатива, снимающая 1–4: если у агентства уже есть VPN (как сейчас ходят на RDP?), веб пускать только
через него — тогда это «офисная сеть», и хватает HTTP.

## Открытые вопросы (ответы подставить в `<…>`)

1. Как пользователи сейчас попадают на RDP из дома — через VPN или RDP открыт в интернет напрямую?
2. Есть ли у агентства домен (можно завести поддомен вроде `qd2.<домен>`) и постоянный внешний IP сервера?
   Кто управляет роутером/брандмауэром перед сервером (проброс портов)?
3. Порт 5051 (и 5052, 5053) на сервере свободен? Папка `C:\qd2web` подходит?
