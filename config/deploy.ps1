# 배포 스크립트
# LogisticManager 프로젝트 배포 자동화

param(
    [string]$Environment = "Debug",
    [string]$TargetFramework = "net8.0-windows",
    [string]$Runtime = "win-x64"
)

Write-Host "🚀 LogisticManager 배포 시작..." -ForegroundColor Green
Write-Host "환경: $Environment" -ForegroundColor Yellow
Write-Host "타겟 프레임워크: $TargetFramework" -ForegroundColor Yellow
Write-Host "런타임: $Runtime" -ForegroundColor Yellow

# 1. 프로젝트 정리
Write-Host "📁 프로젝트 정리 중..." -ForegroundColor Blue
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 프로젝트 정리 실패" -ForegroundColor Red
    exit 1
}

# 2. NuGet 패키지 복원
Write-Host "📦 NuGet 패키지 복원 중..." -ForegroundColor Blue
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 패키지 복원 실패" -ForegroundColor Red
    exit 1
}

# 3. 프로젝트 빌드
Write-Host "🔨 프로젝트 빌드 중..." -ForegroundColor Blue
dotnet build --configuration $Environment --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 빌드 실패" -ForegroundColor Red
    exit 1
}

# 4. 테스트 실행 (있는 경우)
Write-Host "🧪 테스트 실행 중..." -ForegroundColor Blue
dotnet test --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️ 테스트 실패 (계속 진행)" -ForegroundColor Yellow
}

# 5. 게시
Write-Host "📤 프로그램 게시 중..." -ForegroundColor Blue
$publishPath = "publish-$Environment"
dotnet publish --configuration $Environment --runtime $Runtime --output $publishPath --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 게시 실패" -ForegroundColor Red
    exit 1
}

# 6. 배포 파일 복사
Write-Host "📋 배포 파일 복사 중..." -ForegroundColor Blue
$deployPath = "deploy-$Environment"

if (Test-Path $deployPath) {
    Remove-Item $deployPath -Recurse -Force
}

New-Item -ItemType Directory -Path $deployPath | Out-Null

# 실행 파일 및 의존성 복사
Copy-Item "$publishPath\*" -Destination $deployPath -Recurse -Force

# 설정 파일 복사
Copy-Item "config\*" -Destination "$deployPath\config\" -Recurse -Force

# 로그 디렉토리 생성
New-Item -ItemType Directory -Path "$deployPath\logs\current" -Force | Out-Null
New-Item -ItemType Directory -Path "$deployPath\logs\archive" -Force | Out-Null
New-Item -ItemType Directory -Path "$deployPath\logs\temp" -Force | Out-Null

# 7. 배포 완료
Write-Host "✅ 배포 완료!" -ForegroundColor Green
Write-Host "배포 경로: $deployPath" -ForegroundColor Cyan
Write-Host "게시 경로: $publishPath" -ForegroundColor Cyan

# 8. 배포 정보 출력
$deploySize = (Get-ChildItem $deployPath -Recurse | Measure-Object -Property Length -Sum).Sum
$deploySizeMB = [math]::Round($deploySize / 1MB, 2)
Write-Host "배포 크기: $deploySizeMB MB" -ForegroundColor Cyan

Write-Host "🎉 LogisticManager 배포가 성공적으로 완료되었습니다!" -ForegroundColor Green
