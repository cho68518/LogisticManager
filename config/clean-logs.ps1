# 로그 파일 정리 스크립트
# 중복된 로그 파일들을 정리하고 프로젝트 루트에만 남깁니다.

Write-Host "🧹 로그 파일 정리 시작..." -ForegroundColor Green

# 프로젝트 루트 디렉토리
$projectRoot = Get-Location
Write-Host "📁 프로젝트 루트: $projectRoot" -ForegroundColor Yellow

# 정리할 로그 파일들
$logFiles = @(
    "app.log",
    "kakaowork_debug.log",
    "star2_debug.log"
)

# 백업 파일도 정리
$backupFiles = @(
    "app.log.bak"
)

# bin/Debug 디렉토리의 로그 파일들 정리
$binDebugPath = Join-Path $projectRoot "bin\Debug\net8.0-windows\win-x64"
if (Test-Path $binDebugPath) {
    Write-Host "🗑️ bin\Debug 디렉토리의 로그 파일 정리 중..." -ForegroundColor Yellow
    
    foreach ($logFile in $logFiles) {
        $filePath = Join-Path $binDebugPath $logFile
        if (Test-Path $filePath) {
            Remove-Item $filePath -Force
            Write-Host "   삭제됨: $filePath" -ForegroundColor Red
        }
    }
}

# obj 디렉토리의 로그 파일들 정리
$objPath = Join-Path $projectRoot "obj"
if (Test-Path $objPath) {
    Write-Host "🗑️ obj 디렉토리의 로그 파일 정리 중..." -ForegroundColor Yellow
    
    foreach ($logFile in $logFiles) {
        $filePath = Join-Path $objPath $logFile
        if (Test-Path $filePath) {
            Remove-Item $filePath -Force
            Write-Host "   삭제됨: $filePath" -ForegroundColor Red
        }
    }
}

# 프로젝트 루트의 로그 파일들만 유지
Write-Host "✅ 프로젝트 루트의 로그 파일들 유지:" -ForegroundColor Green
foreach ($logFile in $logFiles) {
    $filePath = Join-Path $projectRoot $logFile
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length
        $sizeMB = [math]::Round($size / 1MB, 2)
        Write-Host "   📄 $logFile - $sizeMB MB" -ForegroundColor Cyan
    } else {
        Write-Host "   ❌ $logFile - 파일 없음" -ForegroundColor Gray
    }
}

# 백업 파일들도 정리
Write-Host "🗑️ 백업 파일들 정리:" -ForegroundColor Yellow
foreach ($backupFile in $backupFiles) {
    $filePath = Join-Path $projectRoot $backupFile
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length
        $sizeMB = [math]::Round($size / 1MB, 2)
        Write-Host "   🗑️ $backupFile - $sizeMB MB (삭제됨)" -ForegroundColor Red
        Remove-Item $filePath -Force
    }
}

Write-Host "🎉 로그 파일 정리 완료!" -ForegroundColor Green
Write-Host "💡 이제 프로젝트 루트 디렉토리의 로그 파일만 확인하면 됩니다." -ForegroundColor Yellow
