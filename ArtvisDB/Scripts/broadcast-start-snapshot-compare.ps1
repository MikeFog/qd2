# Сравнение двух выводов broadcast-start-snapshot.sql (docs/broadcast-start.md, §7, шаг 3).
# Вывод режется на секции по строкам «#### …»; строки внутри секции сортируются (порядок строк
# у процедур без ORDER BY не гарантирован), пустые строки и пробелы по краям не учитываются.
# Печатает секции, которые есть только в одном файле, и секции с разным содержимым
# (первые строки «только в A» / «только в B»). Код выхода 0 — совпадает, 1 — есть различия.
#
#   .\broadcast-start-snapshot-compare.ps1 bs-before.txt bs-after.txt [-Show 5]

param(
    [Parameter(Mandatory = $true)][string]$A,
    [Parameter(Mandatory = $true)][string]$B,
    [int]$Show = 5
)

function Read-Sections([string]$path) {
    $sections = [ordered]@{}
    $name = '(header)'
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($raw in [System.IO.File]::ReadLines((Resolve-Path $path), [System.Text.Encoding]::UTF8)) {
        $line = $raw.Trim()
        if ($line -eq '') { continue }
        if ($line.StartsWith('#### ')) {
            if ($line.Contains('|')) {
                # маркер-строка вместе с данными (SELECT N'#### …', значения) — это данные своей секции
                $key = $line.Substring(0, $line.IndexOf('|')).Trim()
                if (-not $sections.Contains($key)) { $sections[$key] = New-Object System.Collections.Generic.List[string] }
                $sections[$key].Add($line)
                continue
            }
            $sections[$name] = $lines
            $name = $line
            # одинаковые маркеры подряд (повторные вызовы) склеиваются в одну секцию
            if ($sections.Contains($name)) { $lines = $sections[$name] } else { $lines = New-Object System.Collections.Generic.List[string] }
            continue
        }
        $lines.Add($line)
    }
    $sections[$name] = $lines
    return $sections
}

$sa = Read-Sections $A
$sb = Read-Sections $B
$diff = 0

foreach ($k in $sa.Keys) {
    if (-not $sb.Contains($k)) { Write-Output "ТОЛЬКО В A: $k"; $diff++ }
}
foreach ($k in $sb.Keys) {
    if (-not $sa.Contains($k)) { Write-Output "ТОЛЬКО В B: $k"; $diff++ }
}

foreach ($k in $sa.Keys) {
    if (-not $sb.Contains($k)) { continue }
    $la = @($sa[$k] | Sort-Object)
    $lb = @($sb[$k] | Sort-Object)
    if (($la -join "`n") -ceq ($lb -join "`n")) { continue }
    $diff++
    $onlyA = @(Compare-Object -ReferenceObject $la -DifferenceObject $lb -CaseSensitive | Where-Object SideIndicator -eq '<=' | ForEach-Object InputObject)
    $onlyB = @(Compare-Object -ReferenceObject $la -DifferenceObject $lb -CaseSensitive | Where-Object SideIndicator -eq '=>' | ForEach-Object InputObject)
    Write-Output "РАЗЛИЧИЕ: $k  (строк A=$($la.Count), B=$($lb.Count))"
    $onlyA | Select-Object -First $Show | ForEach-Object { Write-Output "   A: $_" }
    $onlyB | Select-Object -First $Show | ForEach-Object { Write-Output "   B: $_" }
}

$rows = 0; foreach ($k in $sa.Keys) { $rows += $sa[$k].Count }
Write-Output ("Секций: A={0}, B={1}; строк в A: {2}; различий: {3}" -f $sa.Count, $sb.Count, $rows, $diff)
if ($diff -gt 0) { exit 1 } else { exit 0 }
