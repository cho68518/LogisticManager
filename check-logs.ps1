# 로그 확인 PowerShell 스크립트
param(
    [string]$Action = "recent",
    [string]$Keyword = "",
    [int]$Lines = 20
)

$LogPath = "C:\Work\LogisticManager\app.log"

if (!(Test-Path $LogPath)) {
    Write-Host "❌ 로그 파일이 존재하지 않습니다: $LogPath" -ForegroundColor Red
    exit 1
}

switch ($Action.ToLower()) {
    "recent" {
        Write-Host "📄 최근 $Lines 줄 로그:" -ForegroundColor Green
        Get-Content $LogPath -Tail $Lines
    }
    "search" {
        if ([string]::IsNullOrEmpty($Keyword)) {
            Write-Host "❌ 검색할 키워드를 입력해주세요." -ForegroundColor Red
            exit 1
        }
        Write-Host "🔍 키워드 '$Keyword' 검색 결과:" -ForegroundColor Green
        Get-Content $LogPath | Select-String $Keyword -Context 2
    }
    "errors" {
        Write-Host "⚠️ 오류 로그:" -ForegroundColor Yellow
        Get-Content $LogPath | Select-String -Pattern "오류|error|exception|실패|❌" -Context 1
    }
    "info" {
        $fileInfo = Get-Item $LogPath
        $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        $lineCount = (Get-Content $LogPath | Measure-Object -Line).Lines
        
        Write-Host "📊 로그 파일 정보:" -ForegroundColor Cyan
        Write-Host "   📁 파일 경로: $LogPath"
        Write-Host "   📏 파일 크기: $sizeMB MB"
        Write-Host "   📄 총 라인 수: $lineCount 줄"
        Write-Host "   🕒 마지막 수정: $($fileInfo.LastWriteTime)"
    }
    "clear" {
        $response = Read-Host "⚠️ 로그 파일을 클리어하시겠습니까? (y/n)"
        if ($response -eq "y" -or $response -eq "yes") {
            $backupPath = "$LogPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Copy-Item $LogPath $backupPath
            Clear-Content $LogPath
            Write-Host "✅ 로그 파일이 클리어되었습니다." -ForegroundColor Green
            Write-Host "   📁 백업 파일: $backupPath" -ForegroundColor Yellow
        } else {
            Write-Host "❌ 로그 파일 클리어가 취소되었습니다." -ForegroundColor Red
        }
    }
    default {
        Write-Host "사용법:" -ForegroundColor Cyan
        Write-Host "  .\check-logs.ps1 -Action recent -Lines 20"
        Write-Host "  .\check-logs.ps1 -Action search -Keyword '송장'"
        Write-Host "  .\check-logs.ps1 -Action errors"
        Write-Host "  .\check-logs.ps1 -Action info"
        Write-Host "  .\check-logs.ps1 -Action clear"
    }
}
