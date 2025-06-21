# Unity Log Monitoring Script for WSL/PowerShell
param(
    [string]$LogPath = ".\Logs\unity-console.log",
    [int]$TailLines = 20,
    [switch]$ErrorsOnly,
    [switch]$Help
)

function Write-ColorizedLog {
    param([string]$Line, [string]$Timestamp)
    $formattedLine = "[$Timestamp] $Line"
    if ($Line -match "ERROR|EXCEPTION") {
        Write-Host $formattedLine -ForegroundColor Red
    } elseif ($Line -match "WARNING") {
        Write-Host $formattedLine -ForegroundColor Yellow
    } elseif ($Line -match "INFO") {
        Write-Host $formattedLine -ForegroundColor White
    } else {
        Write-Host $formattedLine -ForegroundColor Gray
    }
}

Write-Host " Unity Log Monitoring Starting..." -ForegroundColor Green
Write-Host " Monitoring: $LogPath" -ForegroundColor Cyan

if (-not (Test-Path $LogPath)) {
    Write-Host " Waiting for log file..." -ForegroundColor Yellow
    do { Start-Sleep 1 } while (-not (Test-Path $LogPath))
}

$content = Get-Content $LogPath -Tail $TailLines
foreach ($line in $content) {
    if ($ErrorsOnly -and $line -notmatch "ERROR|WARNING|EXCEPTION") { continue }
    Write-ColorizedLog -Line $line -Timestamp (Get-Date -Format "HH:mm:ss")
}

Get-Content $LogPath -Wait -Tail 0 | ForEach-Object {
    if ($ErrorsOnly -and $_ -notmatch "ERROR|WARNING|EXCEPTION") { return }
    Write-ColorizedLog -Line $_ -Timestamp (Get-Date -Format "HH:mm:ss.fff")
}
