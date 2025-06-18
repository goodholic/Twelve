# Unity 로그 실시간 모니터링 스크립트
param(
    [string]$LogPath = ".\Logs\unity-console.log",
    [int]$TailLines = 20,
    [switch]$ErrorsOnly
)

Write-Host "🎮 Unity 로그 모니터링 시작..." -ForegroundColor Green
Write-Host "📁 로그 파일: $LogPath" -ForegroundColor Cyan

if (-not (Test-Path $LogPath)) {
    Write-Host "❌ 로그 파일을 찾을 수 없습니다: $LogPath" -ForegroundColor Red
    Write-Host "💡 Unity에서 로그가 생성될 때까지 기다립니다..." -ForegroundColor Yellow
    
    # 로그 파일이 생성될 때까지 대기
    while (-not (Test-Path $LogPath)) {
        Start-Sleep -Seconds 1
    }
    Write-Host "✅ 로그 파일이 생성되었습니다!" -ForegroundColor Green
}

Write-Host "🔍 실시간 로그 모니터링 중... (Ctrl+C로 중지)" -ForegroundColor Yellow
Write-Host "=" * 60

try {
    if ($ErrorsOnly) {
        Get-Content -Path $LogPath -Wait -Tail $TailLines | Where-Object { 
            $_ -match "ERROR|WARNING|EXCEPTION|ASSERT" 
        } | ForEach-Object {
            $timestamp = Get-Date -Format "HH:mm:ss"
            if ($_ -match "ERROR|EXCEPTION") {
                Write-Host "[$timestamp] $_" -ForegroundColor Red
            } elseif ($_ -match "WARNING") {
                Write-Host "[$timestamp] $_" -ForegroundColor Yellow
            } else {
                Write-Host "[$timestamp] $_" -ForegroundColor Magenta
            }
        }
    } else {
        Get-Content -Path $LogPath -Wait -Tail $TailLines | ForEach-Object {
            $timestamp = Get-Date -Format "HH:mm:ss"
            if ($_ -match "ERROR|EXCEPTION") {
                Write-Host "[$timestamp] $_" -ForegroundColor Red
            } elseif ($_ -match "WARNING") {
                Write-Host "[$timestamp] $_" -ForegroundColor Yellow
            } elseif ($_ -match "INFO") {
                Write-Host "[$timestamp] $_" -ForegroundColor White
            } else {
                Write-Host "[$timestamp] $_" -ForegroundColor Gray
            }
        }
    }
} catch {
    Write-Host "❌ 로그 모니터링 중 오류 발생: $_" -ForegroundColor Red
}

Write-Host "`n🔚 로그 모니터링이 종료되었습니다." -ForegroundColor Green 