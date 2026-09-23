$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Push-Location $repoRoot
try {
    docker compose up --build --detach
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось запустить систему.' }
    Write-Host 'Система запущена: http://localhost:8080/'
    Write-Host 'RabbitMQ UI: http://localhost:15672/ (lab / lab)'
} finally {
    Pop-Location
}
