# Автологин веб-версии для отладки (только Development) — см. WebLogin.TryDevAutoLogin.
#
# Спрашивает пароли скрытым вводом и кладёт их в user-secrets проекта:
# %APPDATA%\Microsoft\UserSecrets\fogsoft-web-qd2\secrets.json (профиль Windows,
# в репозиторий не попадает). Пустой ввод — пароль пользователя не меняется.
#
#   powershell -ExecutionPolicy Bypass -File FogSoft.Web\dev-autologin.ps1
#
# По умолчанию входит первый из списка; другой — адресом http://localhost:5051/?devuser=fog

param(
	[string[]] $Users = @('sveta', 'fog')   # sveta — администратор, fog — обычный пользователь
)

$dir = Join-Path $env:APPDATA 'Microsoft\UserSecrets\fogsoft-web-qd2'
$file = Join-Path $dir 'secrets.json'
New-Item -ItemType Directory -Force $dir | Out-Null

$secrets = @{}
if (Test-Path $file) {
	(Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json).PSObject.Properties |
		ForEach-Object { $secrets[$_.Name] = $_.Value }
}

foreach ($user in $Users) {
	$secure = Read-Host "Пароль для $user (Enter — оставить как есть)" -AsSecureString
	$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
	try { $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
	finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
	if ($plain) { $secrets["DevAutoLogin:Passwords:$user"] = $plain }
}
$secrets['DevAutoLogin:User'] = $Users[0]

$secrets | ConvertTo-Json | Set-Content $file -Encoding UTF8

Write-Host ''
Write-Host "Сохранено в $file. Пароли заданы для:"
$secrets.Keys | Where-Object { $_ -like 'DevAutoLogin:Passwords:*' } | Sort-Object |
	ForEach-Object { Write-Host ('  ' + $_.Substring('DevAutoLogin:Passwords:'.Length)) }
Write-Host "По умолчанию входит: $($Users[0])"
