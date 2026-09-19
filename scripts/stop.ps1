$ErrorActionPreference = 'Stop'
$stateFile = Join-Path $PSScriptRoot '.run\state.json'

if (-not (Test-Path -LiteralPath $stateFile)) {
    Write-Host 'Запуск через start.ps1 не найден.'
    exit 0
}

$state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json

# Сначала закрываем вход для новых запросов, затем обе копии приложения.
if (Test-Path -LiteralPath $state.NginxExe) {
    & $state.NginxExe -p $state.NginxPrefix -c $state.NginxConfig -s quit
    if ($LASTEXITCODE -ne 0) { Write-Warning 'Nginx не подтвердил остановку.' }
}

foreach ($processId in $state.AppPids) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process -and $process.ProcessName -eq 'Valuator') {
        Stop-Process -Id $processId
    }
}

Remove-Item -LiteralPath $stateFile
Write-Host 'Nginx и две копии Valuator остановлены.'
