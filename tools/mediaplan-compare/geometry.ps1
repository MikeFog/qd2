# Ширины колонок и высоты строк так, как их показывает Excel (в пунктах), для
# двух папок xlsx. Числа в файле зависят от шрифта «Обычный» (Aptos/Calibri),
# поэтому сравнивать надо видимую геометрию. Выход: <сценарий>|<лист>|C<n>|<пт>|R<n>|<пт>
param([string]$A, [string]$B, [int]$Cols = 45, [string]$Out)
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
$lines = New-Object System.Collections.Generic.List[string]
try {
  foreach ($dir in $A, $B) {
    foreach ($f in Get-ChildItem (Join-Path $dir '*.xlsx')) {
      $wb = $xl.Workbooks.Open($f.FullName, 0, $true)
      foreach ($ws in $wb.Worksheets) {
        $used = $ws.UsedRange
        $lastRow = $used.Row + $used.Rows.Count - 1
        for ($c = 1; $c -le $Cols; $c++) { $lines.Add("$dir|$($f.Name)|$($ws.Name)|C$c|$($ws.Columns.Item($c).Width)") }
        for ($r = 1; $r -le $lastRow; $r++) { $lines.Add("$dir|$($f.Name)|$($ws.Name)|R$r|$($ws.Rows.Item($r).Height)") }
      }
      $wb.Close($false)
    }
  }
} finally {
  $xl.Quit()
  [System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl) | Out-Null
}
[IO.File]::WriteAllLines($Out, $lines, (New-Object System.Text.UTF8Encoding $false))
