$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Push-Location $repoRoot
try {
    docker compose down
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось остановить систему.' }
} finally {
    Pop-Location
}
