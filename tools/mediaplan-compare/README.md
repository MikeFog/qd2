# Стенд сверки медиаплана «было / стало»

Строит один и тот же набор медиапланов двумя сборками qd2 и сравнивает xlsx.
Нужен при любом рефакторинге `MediaPlan` / `MediaPlanBuilder` и при переходе
на OpenXml (план — `docs/tasks/web-mediaplan.md`, §5.1).

- `MpHarness.cs` — консольная программа (x86, .NET Framework). Кладётся в папку
  со сборкой qd2 (копия `Client\bin\Debug`), грузит `Merlin.exe` рефлексией,
  входит пользователем по ID без пароля (`SecurityManager.GetUser(3)`, sveta),
  строит 18 сценариев (акции/кампании ArtvisDev: по станциям, по кампаниям,
  по месяцам, за период, пакетная, спонсорская, модульная, сводный план,
  второй набор настроек печати) и сохраняет `<сценарий>.xlsx` через Excel.
  Понимает и старую сборку (построение в `MediaPlan`), и новую (`_builder`).
- `compare.py` — сверка двух папок через openpyxl: листы, значения, шрифты,
  заливки, рамки, поворот текста, форматы, ширины колонок, ориентация.
- `media.py` — картинки подписей в файлах (md5).

Сценарии завязаны на ID из ArtvisDev на 27.09.2026 — при обновлении копии базы
проверить, что акции и кампании ещё существуют (скрипт падает с понятной ошибкой).

## Запуск (PowerShell)

```powershell
$W = "$env:TEMP\mp"   # рабочая папка
# 1. «было»: собрать qd2 из эталонного коммита и скопировать bin\Debug в $W\old
# 2. «стало»: текущий bin\Debug в $W\new
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -nologo -platform:x86 `
  -out:$W\MpHarness.exe -r:$W\new\FogSoft.WinForm.dll -r:System.Data.dll tools\mediaplan-compare\MpHarness.cs
foreach ($v in "old","new") {
  Copy-Item $W\MpHarness.exe $W\$v\
  Copy-Item $W\$v\Merlin.exe.config $W\$v\MpHarness.exe.config
  Push-Location $W\$v; .\MpHarness.exe $W\out\$v; Pop-Location   # второй аргумент — один сценарий
}
python tools\mediaplan-compare\compare.py $W\out\old $W\out\new
python tools\mediaplan-compare\media.py $W\out
```
