$out = Join-Path $PSScriptRoot 'db.json'   # выгрузка источников; в git не кладём
$cn = New-Object System.Data.SqlClient.SqlConnection 'Server=lpc:(local)\sqlexpress;Database=ArtvisDev;Integrated Security=true'
$cn.Open()
$queries = [ordered]@{
  menu      = "select name as t, name_es as es from iMenu"
  entity    = "select name as t from iEntity"
  attribute = "select distinct alias as t from iEntityAttribute"
  action    = "select distinct alias as t from iEntityAction"
  message   = "select message as t from iMessage"
  passport  = "select cast(passport as nvarchar(max)) as t from iEntity where passport is not null union all select cast(filter as nvarchar(max)) from iEntity where filter is not null union all select cast(passport as nvarchar(max)) from iPassport union all select cast(filter as nvarchar(max)) from iRelationScenario where filter is not null"
}
$result = [ordered]@{}
foreach ($k in $queries.Keys) {
  $cmd = $cn.CreateCommand(); $cmd.CommandText = $queries[$k]
  $r = $cmd.ExecuteReader(); $rows = @()
  while ($r.Read()) { $o = @{}; for ($i = 0; $i -lt $r.FieldCount; $i++) { $o[$r.GetName($i)] = if ($r.IsDBNull($i)) { $null } else { $r.GetValue($i).ToString() } }; $rows += $o }
  $r.Close(); $result[$k] = $rows
}
$cn.Close()
[IO.File]::WriteAllText($out, ($result | ConvertTo-Json -Depth 4 -Compress), (New-Object Text.UTF8Encoding $false))
"ok"
