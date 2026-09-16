$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectDir = Join-Path $repoRoot 'Valuator'
$projectFile = Join-Path $projectDir 'Valuator.csproj'
$configFile = Join-Path $repoRoot 'nginx\conf\nginx.conf'
$runDir = Join-Path $PSScriptRoot '.run'
$stateFile = Join-Path $runDir 'state.json'

function Test-Port([int]$port) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $client.Connect('127.0.0.1', $port)
        return $true
    } catch {
        return $false
    } finally {
        $client.Dispose()
    }
}

function Wait-Port([int]$port) {
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if (Test-Port $port) { return }
        Start-Sleep -Milliseconds 250
    }
    throw "Порт $port не открылся. Проверьте логи в scripts/.run/."
}

# Если Nginx установлен в другом месте, задайте NGINX_HOME перед запуском.
if ($env:NGINX_HOME) {
    $nginxExe = Join-Path $env:NGINX_HOME 'nginx.exe'
} else {
    $localNginx = Join-Path (Split-Path $repoRoot -Parent) 'nginx-1.31.6\nginx.exe'
    if (Test-Path -LiteralPath $localNginx) {
        $nginxExe = $localNginx
    } else {
        $command = Get-Command nginx.exe -ErrorAction SilentlyContinue
        if (-not $command) { throw 'Nginx не найден. Задайте переменную NGINX_HOME.' }
        $nginxExe = $command.Source
    }
}
if (-not (Test-Path -LiteralPath $nginxExe)) { throw "Nginx не найден: $nginxExe" }

$prefix = ((Split-Path $nginxExe -Parent) -replace '\\', '/') + '/'
$config = $configFile -replace '\\', '/'

if (Test-Path -LiteralPath $stateFile) {
    $previous = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
    $alive = @($previous.AppPids | Where-Object {
        $process = Get-Process -Id $_ -ErrorAction SilentlyContinue
        $process -and $process.ProcessName -eq 'Valuator'
    }).Count
    if ($alive -eq 2 -and (Test-Port 8080)) {
        Write-Host 'Система уже запущена.'
        exit 0
    }
    throw 'Найден файл предыдущего запуска scripts/.run/state.json. Остановите систему через stop.ps1.'
}

if (-not (Test-Port 6379)) {
    throw 'Redis не отвечает на порту 6379. Запустите его: docker start valuator-redis'
}
foreach ($port in 5001, 5002, 8080) {
    if (Test-Port $port) {
        throw "Порт $port уже занят. Остановите процессы, запущенные вручную, и повторите запуск."
    }
}

& $nginxExe -t -p $prefix -c $config
if ($LASTEXITCODE -ne 0) { throw 'Конфигурация Nginx не прошла проверку.' }

dotnet build $projectFile -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Не удалось собрать Valuator.' }

$appExe = Join-Path $projectDir 'bin\Release\net8.0\Valuator.exe'
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
$apps = @()
$nginxStarted = $false

try {
    foreach ($port in 5001, 5002) {
        $app = Start-Process -FilePath $appExe `
            -ArgumentList @('--urls', "http://127.0.0.1:$port") `
            -WorkingDirectory $projectDir -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $runDir "valuator-$port.out.log") `
            -RedirectStandardError (Join-Path $runDir "valuator-$port.err.log")
        $apps += $app
        Wait-Port $port
    }

    $nginxArguments = "-p `"$prefix`" -c `"$config`""
    Start-Process -FilePath $nginxExe -ArgumentList $nginxArguments `
        -WorkingDirectory (Split-Path $nginxExe -Parent) -WindowStyle Hidden | Out-Null
    $nginxStarted = $true
    Wait-Port 8080
    $response = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/' -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -ne 200) { throw 'Nginx запущен, но приложение не ответило успешно.' }

    [pscustomobject]@{
        AppPids = @($apps | ForEach-Object { $_.Id })
        NginxExe = $nginxExe
        NginxPrefix = $prefix
        NginxConfig = $config
    } | ConvertTo-Json | Set-Content -LiteralPath $stateFile -Encoding UTF8

    Write-Host 'Valuator запущен на 5001 и 5002, Nginx — на 8080.'
    Write-Host 'Откройте http://localhost:8080/'
} catch {
    if ($nginxStarted) { & $nginxExe -p $prefix -c $config -s quit 2>$null | Out-Null }
    foreach ($app in $apps) { Stop-Process -Id $app.Id -ErrorAction SilentlyContinue }
    throw
}
