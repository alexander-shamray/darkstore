#!/usr/bin/env pwsh
# Dark Store — запуск документационного портала
# Использование:
#   .\docs-serve.ps1                    — localhost:8000 (только этот компьютер)
#   .\docs-serve.ps1 -Network           — 0.0.0.0:8000  (вся локальная сеть, телефон по Wi-Fi)
#   .\docs-serve.ps1 -Tunnel            — публичный HTTPS-URL через Cloudflare (интернет, любой телефон)
#   .\docs-serve.ps1 -Build             — сборка статического сайта в ./site/
#   .\docs-serve.ps1 -Build -Open       — сборка + открыть в браузере

param(
    [switch]$Build,
    [switch]$Open,
    [switch]$Network,   # доступ по LAN (одна Wi-Fi сеть)
    [switch]$Tunnel     # публичный URL через Cloudflare Tunnel (интернет)
)

$rootDir = $PSScriptRoot
Set-Location $rootDir

Write-Host ""
Write-Host "  ██████╗  █████╗ ██████╗ ██╗  ██╗    ███████╗████████╗ ██████╗ ██████╗ ███████╗" -ForegroundColor DarkOrange
Write-Host "  ██╔══██╗██╔══██╗██╔══██╗██║ ██╔╝    ██╔════╝╚══██╔══╝██╔═══██╗██╔══██╗██╔════╝" -ForegroundColor DarkOrange
Write-Host "  ██║  ██║███████║██████╔╝█████╔╝     ███████╗   ██║   ██║   ██║██████╔╝█████╗  " -ForegroundColor Orange
Write-Host "  ██║  ██║██╔══██║██╔══██╗██╔═██╗     ╚════██║   ██║   ██║   ██║██╔══██╗██╔══╝  " -ForegroundColor Orange
Write-Host "  ██████╔╝██║  ██║██║  ██║██║  ██╗    ███████║   ██║   ╚██████╔╝██║  ██║███████╗" -ForegroundColor Yellow
Write-Host "  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝   ╚══════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚══════╝" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Портал документации Dark Store" -ForegroundColor White
Write-Host ""

# Проверяем наличие mkdocs
if (-not (Get-Command mkdocs -ErrorAction SilentlyContinue)) {
    Write-Host "  [!] MkDocs не найден. Устанавливаем..." -ForegroundColor Yellow
    pip install mkdocs-material
}

# ---------- Вспомогательная функция: печать QR-кода в терминале ----------
function Show-QR {
    param([string]$Url)
    $hasPip = Get-Command pip -ErrorAction SilentlyContinue
    if ($hasPip) {
        $hasQr = python -c "import qrcode" 2>$null; $ok = $LASTEXITCODE -eq 0
        if (-not $ok) { pip install qrcode --quiet }
        python -c "
import qrcode, sys
qr = qrcode.QRCode(border=1)
qr.add_data(sys.argv[1])
qr.make(fit=True)
qr.print_ascii(invert=True)
" $Url 2>$null
    }
}

# ---------- СБОРКА ----------
if ($Build) {
    Write-Host "  [>] Сборка статического сайта..." -ForegroundColor Cyan
    python -m mkdocs build --clean
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "  [✓] Сайт собран в папку: $rootDir\site\" -ForegroundColor Green
        Write-Host "  [i] Откройте site\index.html в браузере или разверните на любом хостинге." -ForegroundColor Gray
        if ($Open) { Start-Process "$rootDir\site\index.html" }
    } else {
        Write-Host "  [✗] Ошибка при сборке. Проверьте вывод выше." -ForegroundColor Red
    }
    return
}

# ---------- TUNNEL (Cloudflare Quick Tunnel — публичный HTTPS, без регистрации) ----------
if ($Tunnel) {
    # Проверяем / скачиваем cloudflared
    $cfExe = "$env:LOCALAPPDATA\cloudflared\cloudflared.exe"
    if (-not (Test-Path $cfExe)) {
        Write-Host "  [>] Скачиваем cloudflared (Cloudflare Tunnel)..." -ForegroundColor Cyan
        $null = New-Item -ItemType Directory -Force -Path (Split-Path $cfExe)
        Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" `
            -OutFile $cfExe -UseBasicParsing
        Write-Host "  [✓] cloudflared загружен." -ForegroundColor Green
    }

    Write-Host "  [>] Запуск MkDocs + Cloudflare Tunnel..." -ForegroundColor Cyan
    Write-Host "  [i] Подождите ~10 секунд. Публичный URL появится ниже." -ForegroundColor Gray
    Write-Host ""

    # Запускаем MkDocs serve в фоновом задании
    $mkdocsJob = Start-Job -ScriptBlock {
        Set-Location $using:rootDir
        python -m mkdocs serve --dev-addr=127.0.0.1:8000 2>&1
    }
    Start-Sleep -Seconds 4

    # Запускаем cloudflared и перехватываем строку с URL
    $cfJob = Start-Job -ScriptBlock {
        & $using:cfExe tunnel --url http://127.0.0.1:8000 2>&1
    }

    # Ждём появления публичного URL (до 30 сек)
    $publicUrl = $null
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline -and -not $publicUrl) {
        Start-Sleep -Milliseconds 800
        $output = Receive-Job $cfJob -Keep 2>$null
        $line = $output | Select-String "trycloudflare.com" | Select-Object -Last 1
        if ($line) {
            $publicUrl = ($line -split '\s+' | Where-Object { $_ -like "https://*trycloudflare*" }) | Select-Object -First 1
        }
    }

    if ($publicUrl) {
        Write-Host ""
        Write-Host "  ╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "  ║  🌐  Публичный URL (работает с любого телефона/ПК):          ║" -ForegroundColor Green
        Write-Host "  ║                                                              ║" -ForegroundColor Green
        Write-Host "  ║  $publicUrl" -ForegroundColor Yellow
        Write-Host "  ║                                                              ║" -ForegroundColor Green
        Write-Host "  ╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        Write-Host "  QR-код для телефона:" -ForegroundColor Cyan
        Show-QR $publicUrl
        Write-Host ""
        Write-Host "  [i] Для остановки нажмите Ctrl+C" -ForegroundColor Gray
        Set-Clipboard $publicUrl
        Write-Host "  [✓] URL скопирован в буфер обмена." -ForegroundColor Green
    } else {
        Write-Host "  [!] Не удалось получить URL от cloudflared. Вывод:" -ForegroundColor Yellow
        Receive-Job $cfJob -Keep | Write-Host
    }

    # Держим живыми оба процесса до Ctrl+C
    try {
        while ($true) { Start-Sleep -Seconds 5 }
    } finally {
        Stop-Job $mkdocsJob, $cfJob -ErrorAction SilentlyContinue
        Remove-Job $mkdocsJob, $cfJob -Force -ErrorAction SilentlyContinue
    }
    return
}

# ---------- NETWORK (локальная сеть / Wi-Fi) ----------
if ($Network) {
    $localIP = (Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object { $_.PrefixOrigin -eq 'Dhcp' -or ($_.IPAddress -like "192.168.*" -and $_.IPAddress -notlike "192.168.75.*" -and $_.IPAddress -notlike "192.168.159.*") } |
        Sort-Object { [System.Version]$_.IPAddress } | Select-Object -Last 1).IPAddress

    if (-not $localIP) {
        $localIP = (Get-NetIPAddress -AddressFamily IPv4 |
            Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.*" } |
            Select-Object -First 1).IPAddress
    }

    $port = 8000
    $lanUrl = "http://${localIP}:${port}"

    # Открываем порт в Windows Firewall (нужны права админа)
    $ruleName = "MkDocs Dark Store Docs"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if (-not $existingRule) {
        Write-Host "  [>] Открываем порт $port в Windows Firewall..." -ForegroundColor Cyan
        try {
            New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP `
                -LocalPort $port -Action Allow -Profile Any -ErrorAction Stop | Out-Null
            Write-Host "  [✓] Правило фаервола добавлено." -ForegroundColor Green
        } catch {
            Write-Host "  [!] Не удалось добавить правило фаервола (нужны права администратора)." -ForegroundColor Yellow
            Write-Host "      Запустите скрипт от имени администратора или добавьте правило вручную:" -ForegroundColor Gray
            Write-Host "      netsh advfirewall firewall add rule name=`"MkDocs`" dir=in action=allow protocol=TCP localport=8000" -ForegroundColor Gray
        }
    }

    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║  📱  Адрес для телефона в той же Wi-Fi сети:                 ║" -ForegroundColor Cyan
    Write-Host "  ║                                                              ║" -ForegroundColor Cyan
    Write-Host "  ║  $lanUrl" -ForegroundColor Yellow
    Write-Host "  ║                                                              ║" -ForegroundColor Cyan
    Write-Host "  ╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  QR-код для телефона:" -ForegroundColor Cyan
    Show-QR $lanUrl
    Write-Host ""
    Write-Host "  [i] Телефон должен быть в той же Wi-Fi сети." -ForegroundColor Gray
    Write-Host "  [i] Для остановки нажмите Ctrl+C" -ForegroundColor Gray
    Write-Host ""
    Set-Clipboard $lanUrl
    Write-Host "  [✓] URL скопирован в буфер обмена." -ForegroundColor Green
    Write-Host ""

    python -m mkdocs serve --dev-addr=0.0.0.0:${port}
    return
}

# ---------- LOCALHOST (по умолчанию) ----------
Write-Host "  [>] Запуск live-сервера документации..." -ForegroundColor Cyan
Write-Host "  [i] Адрес: http://127.0.0.1:8000  (только этот компьютер)" -ForegroundColor Gray
Write-Host "  [i] Для доступа с телефона используйте: .\docs-serve.ps1 -Network" -ForegroundColor Gray
Write-Host "  [i] Для доступа из интернета:           .\docs-serve.ps1 -Tunnel" -ForegroundColor Gray
Write-Host "  [i] Для остановки нажмите Ctrl+C" -ForegroundColor Gray
Write-Host ""
Start-Process "http://127.0.0.1:8000" -ErrorAction SilentlyContinue
python -m mkdocs serve

