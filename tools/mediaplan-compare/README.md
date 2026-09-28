# Стенд сверки медиаплана «было / стало»

Строит один и тот же набор медиапланов двумя сборками qd2 (или двумя способами
записи) и сравнивает xlsx. Нужен при любом рефакторинге `MediaPlanBuilder` /
`OpenXmlExportDocument` (план — `docs/tasks/web-mediaplan.md`, §5.1).

- `MpHarness.cs` — консольная программа (x86, .NET Framework). Кладётся в папку
  со сборкой qd2 (копия `Client\bin\Debug`), грузит `Merlin.exe` рефлексией,
  входит пользователем по ID без пароля (`SecurityManager.GetUser(3)`, sveta),
  строит 18 сценариев (акции/кампании ArtvisDev: по станциям, по кампаниям,
  по месяцам, за период, пакетная, спонсорская, модульная, сводный план,
  второй набор настроек печати) и сохраняет `<сценарий>.xlsx`.
  Понимает сборку до этапа 2 (построение в `MediaPlan`), этапа 2 (`_builder` +
  Excel через COM) и после этапа 3 (только OpenXml).
  Аргументы: `<папка> [--openxml] [--lang es] [сценарий]`.
- `compare.py` — сверка двух папок через openpyxl: листы, значения, шрифты,
  заливки, рамки, поворот текста, форматы, ширины, ориентация. `--loose` —
  для COM против OpenXml (рамки по видимым граням, шрифт пустых ячеек не
  сравнивается, картинки — по ячейке и размеру; ширины/высоты — через geometry).
- `geometry.ps1` + `geometry.py` — видимые ширины колонок и высоты строк в
  пунктах, как их показывает Excel на этой машине.
- `media.py` — картинки подписей в файлах (md5).

Сценарии завязаны на ID из ArtvisDev на 27.09.2026 — при обновлении копии базы
проверить, что акции и кампании ещё существуют (скрипт падает с понятной ошибкой).

## Запуск (PowerShell)

```powershell
$W = "$env:TEMP\mp"   # рабочая папка
# 1. «было»: сборка эталонного коммита — копия bin\Debug в $W\old
# 2. «стало»: текущий bin\Debug в $W\new
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -nologo -platform:x86 `
  -out:$W\MpHarness.exe -r:$W\new\FogSoft.WinForm.dll -r:System.Data.dll tools\mediaplan-compare\MpHarness.cs
foreach ($v in "old","new") {
  Copy-Item $W\MpHarness.exe $W\$v\
  Copy-Item $W\$v\Merlin.exe.config $W\$v\MpHarness.exe.config
  Push-Location $W\$v; .\MpHarness.exe $W\out\$v; Pop-Location
}
python tools\mediaplan-compare\compare.py $W\out\old $W\out\new            # --loose, если old — COM
powershell -File tools\mediaplan-compare\geometry.ps1 -A $W\out\old -B $W\out\new -Out $W\geom.txt
python tools\mediaplan-compare\geometry.py $W\geom.txt 0.4
python tools\mediaplan-compare\media.py $W\out
```
